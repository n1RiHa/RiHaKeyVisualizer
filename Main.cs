using System;
using System.Collections.Generic;
using HarmonyLib;
using RIHaKeyVisualizer.Languages;
using RIHaKeyVisualizer.Utils;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace RIHaKeyVisualizer
{
	/// <summary>
	/// Точка входа мода: загрузка, окно настроек Unity Mod Manager
	/// и применение настроек к оверлею.
	/// </summary>
	public static class Main
	{
		public static Harmony harmony;
		public static bool Enabled = false;
		public static int GameVersion = 0;
		public static string ModPath = "";
		public static UnityModManager.ModEntry Entry;

		public static Config RiHaConfig;
		public static GameObject RiHaCanvas;
		public static RIHaKeyVisualizer RiHa;

		/// <summary>true, пока пользователь таскает риха мышью.</summary>
		public static bool IsEditingPos;

		/// <summary>
		/// Какой список клавиш сейчас "слушает" окно настроек:
		/// 0 — никакой, 1 — левые клавиши, 2 — правые клавиши.
		/// Идея взята из AdofaiTweaks (KeyViewerSettings.IsListening).
		/// </summary>
		private static int listeningSlot = 0;

		public static void Load(UnityModManager.ModEntry modEntry)
		{
			ADOStartup.ModWasAdded(modEntry.Info.Id);
#if DEBUG
			RiHaLogger.Setup(modEntry.Logger, RiHaLogger.LogLevel.Debug);
#else
			RiHaLogger.Setup(modEntry.Logger, RiHaLogger.LogLevel.Info);
#endif
			GameVersion = (int)AccessTools.Field(typeof(Releases), "releaseNumber").GetValue(null);
			RiHaLogger.Info("Game Version: " + GameVersion);

			ModPath = modEntry.Path;
			harmony = new Harmony(modEntry.Info.Id);
			Entry = modEntry;

			RiHaConfig = UnityModManager.ModSettings.Load<Config>(modEntry);

			// 1) стандартные PNG из папки мода, 2) пользовательские картинки.
			Assets.Load(ModPath);
			Assets.ReloadCustomSprites(RiHaConfig);

			SetupCanvas();
			RiHa = CreateRiHa();
			ApplyConfig();

			modEntry.OnToggle = (entry, value) =>
			{
				Enabled = value;
				RiHaCanvas.SetActive(value);

				if (value)
				{
					ApplyConfig();
				}
				else
				{
					harmony.UnpatchAll(entry.Info.Id);
				}

				return true;
			};

			modEntry.OnShowGUI = (entry) =>
			{
				IsEditingPos = false;
			};

			modEntry.OnHideGUI = (entry) =>
			{
				listeningSlot = 0;
			};

			modEntry.OnGUI = DrawGUI;

			modEntry.OnSaveGUI = (entry) =>
			{
				UnityModManager.ModSettings.Save<Config>(RiHaConfig, entry);
			};
		}

		// ============================================================
		// ОКНО НАСТРОЕК
		// ============================================================

		private static void DrawGUI(UnityModManager.ModEntry entry)
		{
			// --- 0. Язык ---
			DrawLanguageSelector();

			GUILayout.Space(10f);

			// --- 1. Положение, размер, прозрачность ---
			GUILayout.Label(PositionAndView);

			GUILayoutUtils.DrawFloatWithSlider(ref RiHaConfig.x, 0f, 1f, X);
			GUILayoutUtils.DrawFloatWithSlider(ref RiHaConfig.y, 0f, 1f, Y);
			GUILayoutUtils.DrawFloatWithSlider(ref RiHaConfig.size, 0.25f, 5f, Size);
			GUILayoutUtils.DrawFloatWithSlider(ref RiHaConfig.opacity, 0f, 1f, Opacity);

			// Кнопка перетаскивания мышью: прячем окно UMM, включаем
			// GraphicRaycaster и ловим OnDrag на самой картинке.
			if (GUILayout.Button(SetPositionWithMouse, GUILayout.Width(220f)))
			{
				IsEditingPos = true;
				RiHa.SetVisible(true);
				RiHa.SetRaycast();
				((UnityModManager.UI)AccessTools.Field(typeof(UnityModManager.UI), "mInstance").GetValue(null))
					.ToggleWindow(false);
				Cursor.visible = true;
			}

			GUILayout.Space(10f);

			// --- 2. Где показывать ---
			GUILayout.Label(Display);

			DrawDisplayModeSelector();

			GUILayoutUtils.DrawToggle(ref RiHaConfig.useOutline, OutlineInsteadOfShadow);

			GUILayout.Space(10f);

			// --- 3. Клавиши ---
			GUILayout.Label(Keys);
			DrawKeyList(RiHaConfig.leftKeys, 1, LeftImage);
			DrawKeyList(RiHaConfig.rightKeys, 2, RightImage);
			GUILayout.Label(BothGroupsHint);

			// Пока идёт "прослушивание", ловим первое нажатие клавиши.
			if (listeningSlot != 0
				&& Event.current.isKey
				&& Event.current.type == EventType.KeyDown
				&& Event.current.keyCode != KeyCode.None)
			{
				List<KeyCode> target = listeningSlot == 1 ? RiHaConfig.leftKeys : RiHaConfig.rightKeys;
				KeyCode pressed = Event.current.keyCode;

				// Одна и та же клавиша не должна быть в обеих группах.
				RiHaConfig.leftKeys.Remove(pressed);
				RiHaConfig.rightKeys.Remove(pressed);
				target.Add(pressed);

				listeningSlot = 0;
			}

			GUILayout.Space(10f);

			// --- 4. Свои картинки ---
			GUILayout.Label(CustomImages);
			GUILayout.Label(CustomImagesHint);

			DrawImagePathField("rihaOff    ", ref RiHaConfig.customOffPath);
			DrawImagePathField("rihaLeft   ", ref RiHaConfig.customLeftPath);
			DrawImagePathField("rihaRight ", ref RiHaConfig.customRightPath);
			DrawImagePathField("rihaOn     ", ref RiHaConfig.customOnPath);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(ApplyImages, GUILayout.Width(180f)))
			{
				Assets.ReloadCustomSprites(RiHaConfig);
				RiHa.RefreshSprite();
			}
			if (GUILayout.Button(ResetImages, GUILayout.Width(200f)))
			{
				RiHaConfig.customOffPath = "";
				RiHaConfig.customLeftPath = "";
				RiHaConfig.customRightPath = "";
				RiHaConfig.customOnPath = "";
				Assets.ReloadCustomSprites(RiHaConfig);
				RiHa.RefreshSprite();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			// Применяем всё, что поменялось в этом кадре.
			ApplyConfig();
		}

		private static void DrawLanguageSelector()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(Language, GUILayout.ExpandWidth(false));
			GUILayout.Space(5f);

			string[] names =
			{
				LanguageRussian,
				LanguageEnglish,
				LanguageKorean
			};

			int selected = (int)RiHaConfig.language;
			if (UnityModManager.UI.PopupToggleGroup(ref selected, names, Language, 0, null, GUILayout.ExpandWidth(false)))
			{
				RiHaConfig.language = (Config.Language)selected;
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private static void DrawDisplayModeSelector()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(ShowLabel, GUILayout.ExpandWidth(false));
			GUILayout.Space(5f);

			string[] names =
			{
				DisplayModeAlways,
				DisplayModeOnlyGameplay
			};

			int selected = (int)RiHaConfig.displayMode;
			if (UnityModManager.UI.PopupToggleGroup(ref selected, names, ShowLabel, 0, null, GUILayout.ExpandWidth(false)))
			{
				RiHaConfig.displayMode = (Config.DisplayMode)selected;
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		/// <summary>Рисует одну группу клавиш: список + кнопки удаления + кнопка добавления.</summary>
		private static void DrawKeyList(List<KeyCode> keys, int slot, string title)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(title, GUILayout.Width(220f));

			// Клик по клавише в списке удаляет её.
			for (int i = keys.Count - 1; i >= 0; i--)
			{
				if (GUILayout.Button(KeyCodeUtils.GetKeyName(keys[i]) + " ✕", GUILayout.Width(80f)))
				{
					keys.RemoveAt(i);
				}
			}

			if (listeningSlot == slot)
			{
				GUILayout.Label(PressKey);
				if (GUILayout.Button(Cancel, GUILayout.Width(80f))) listeningSlot = 0;
			}
			else
			{
				if (GUILayout.Button(AddKey, GUILayout.Width(100f))) listeningSlot = slot;
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		/// <summary>Поле ввода пути к своей картинке (приём из MyOshiOverlay).</summary>
		private static void DrawImagePathField(string label, ref string path)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width(80f));
			path = GUILayout.TextField(path ?? "", GUILayout.Width(400f));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private static string Language
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Language;
					case Config.Language.Korean: return Korean.Language;
					default: return Russian.Language;
				}
			}
		}

		private static string PositionAndView
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.PositionAndView;
					case Config.Language.Korean: return Korean.PositionAndView;
					default: return Russian.PositionAndView;
				}
			}
		}

		private static string X
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.X;
					case Config.Language.Korean: return Korean.X;
					default: return Russian.X;
				}
			}
		}

		private static string Y
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Y;
					case Config.Language.Korean: return Korean.Y;
					default: return Russian.Y;
				}
			}
		}

		private static string Size
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Size;
					case Config.Language.Korean: return Korean.Size;
					default: return Russian.Size;
				}
			}
		}

		private static string Opacity
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Opacity;
					case Config.Language.Korean: return Korean.Opacity;
					default: return Russian.Opacity;
				}
			}
		}

		private static string SetPositionWithMouse
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.SetPositionWithMouse;
					case Config.Language.Korean: return Korean.SetPositionWithMouse;
					default: return Russian.SetPositionWithMouse;
				}
			}
		}

		private static string Display
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Display;
					case Config.Language.Korean: return Korean.Display;
					default: return Russian.Display;
				}
			}
		}

		private static string ShowLabel
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Show;
					case Config.Language.Korean: return Korean.Show;
					default: return Russian.Show;
				}
			}
		}

		private static string OutlineInsteadOfShadow
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.OutlineInsteadOfShadow;
					case Config.Language.Korean: return Korean.OutlineInsteadOfShadow;
					default: return Russian.OutlineInsteadOfShadow;
				}
			}
		}

		private static string Keys
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Keys;
					case Config.Language.Korean: return Korean.Keys;
					default: return Russian.Keys;
				}
			}
		}

		private static string LeftImage
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.LeftImage;
					case Config.Language.Korean: return Korean.LeftImage;
					default: return Russian.LeftImage;
				}
			}
		}

		private static string RightImage
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.RightImage;
					case Config.Language.Korean: return Korean.RightImage;
					default: return Russian.RightImage;
				}
			}
		}

		private static string BothGroupsHint
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.BothGroupsHint;
					case Config.Language.Korean: return Korean.BothGroupsHint;
					default: return Russian.BothGroupsHint;
				}
			}
		}

		private static string PressKey
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.PressKey;
					case Config.Language.Korean: return Korean.PressKey;
					default: return Russian.PressKey;
				}
			}
		}

		private static string Cancel
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.Cancel;
					case Config.Language.Korean: return Korean.Cancel;
					default: return Russian.Cancel;
				}
			}
		}

		private static string AddKey
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.AddKey;
					case Config.Language.Korean: return Korean.AddKey;
					default: return Russian.AddKey;
				}
			}
		}

		private static string CustomImages
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.CustomImages;
					case Config.Language.Korean: return Korean.CustomImages;
					default: return Russian.CustomImages;
				}
			}
		}

		private static string CustomImagesHint
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.CustomImagesHint;
					case Config.Language.Korean: return Korean.CustomImagesHint;
					default: return Russian.CustomImagesHint;
				}
			}
		}

		private static string ApplyImages
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.ApplyImages;
					case Config.Language.Korean: return Korean.ApplyImages;
					default: return Russian.ApplyImages;
				}
			}
		}

		private static string ResetImages
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.ResetImages;
					case Config.Language.Korean: return Korean.ResetImages;
					default: return Russian.ResetImages;
				}
			}
		}

		private static string DisplayModeAlways
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.DisplayModeAlways;
					case Config.Language.Korean: return Korean.DisplayModeAlways;
					default: return Russian.DisplayModeAlways;
				}
			}
		}

		private static string DisplayModeOnlyGameplay
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.DisplayModeOnlyGameplay;
					case Config.Language.Korean: return Korean.DisplayModeOnlyGameplay;
					default: return Russian.DisplayModeOnlyGameplay;
				}
			}
		}

		private static string LanguageRussian
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.LanguageRussian;
					case Config.Language.Korean: return Korean.LanguageRussian;
					default: return Russian.LanguageRussian;
				}
			}
		}

		private static string LanguageEnglish
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.LanguageEnglish;
					case Config.Language.Korean: return Korean.LanguageEnglish;
					default: return Russian.LanguageEnglish;
				}
			}
		}

		private static string LanguageKorean
		{
			get
			{
				switch (RiHaConfig.language)
				{
					case Config.Language.English: return English.LanguageKorean;
					case Config.Language.Korean: return Korean.LanguageKorean;
					default: return Russian.LanguageKorean;
				}
			}
		}

		// ============================================================
		// ПРИМЕНЕНИЕ НАСТРОЕК
		// ============================================================

		/// <summary>Канвас-контейнер, на котором лежит риха.</summary>
		public static void SetupCanvas()
		{
			RiHaCanvas = new GameObject("RIHaKeyVisualizer");
			UnityEngine.Object.DontDestroyOnLoad(RiHaCanvas);

			Canvas canvas = RiHaCanvas.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 1004;

			// Виртуальное разрешение 1280x720: координаты x/y из настроек
			// пересчитываются именно в него, поэтому оверлей одинаково
			// выглядит на любом мониторе.
			CanvasScaler scaler = RiHaCanvas.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280, 720);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0;
			scaler.referencePixelsPerUnit = 100;
		}

		public static RIHaKeyVisualizer CreateRiHa()
		{
			GameObject rihaObject = new GameObject("RIHaKeyVisualizer");
			rihaObject.transform.SetParent(RiHaCanvas.transform);
			rihaObject.AddComponent<RectTransform>();
			return rihaObject.AddComponent<RIHaKeyVisualizer>();
		}

		/// <summary>
		/// Переносит значения из Config в реальный оверлей.
		/// Вызывается при старте и при каждом кадре окна настроек.
		/// и из патчей входа/выхода из геймплея.
		/// </summary>
		public static void ApplyConfig()
		{
			if (RiHa == null || RiHaConfig == null) return;

			// 0..1 -> координаты внутри канваса 1280x720 (центр канваса — 0,0).
			RiHa.GetComponent<RectTransform>().LocalMoveXY(
				RiHaConfig.x * 1280 - 640,
				RiHaConfig.y * 720 - 360);

			RiHa.transform.ScaleXY(RiHaConfig.size * 2.25f);

			RiHa.SetOpacity(RiHaConfig.opacity);
			RiHa.SetOutline(RiHaConfig.useOutline);

			UpdateVisibility();
		}

		/// <summary>Идёт ли сейчас именно геймплей (не меню, не пауза в редакторе).</summary>
		public static bool IsInGameplay()
		{
			scrController controller = scrController.instance;
			if (controller == null || !controller.gameworld) return false;
			if (scnEditor.instance != null && controller.paused) return false;
			return true;
		}
		
		/// <summary>
		/// true, если редактор сам скрывает игровой UI во время автоплея.
		/// В этот момент риха тоже лучше скрыть, чтобы он не мешал записи.
		/// </summary>
		private static bool IsEditorAutoplayHidingGameplayUi()
		{
			scnEditor editor = scnEditor.instance;
			return editor != null && editor.shouldHideGameplayUIForAutoplay;
		}

		/// <summary>
		/// Решает, видно ли риха прямо сейчас:
		/// Always — видно всегда, OnlyGameplay — только на уровне.
		/// </summary>
		public static void UpdateVisibility()
		{
			if (RiHa == null || RiHaConfig == null) return;

			// Во время перетаскивания мышью риха должен быть виден
			// независимо от режима, иначе его нечем двигать.
			bool show = IsEditingPos
				|| (!IsEditorAutoplayHidingGameplayUi()
					&& (RiHaConfig.displayMode == Config.DisplayMode.Always
						|| IsInGameplay()));

			RiHa.SetVisible(show);
		}

		/// <summary>Вызывается патчами при входе в геймплей.</summary>
		public static void Show()
		{
			UpdateVisibility();
		}

		/// <summary>Вызывается патчами при выходе из геймплея.</summary>
		public static void Hide()
		{
			UpdateVisibility();
		}

		/// <summary>Перетаскивание риха мышью: пиксели экрана -> доли 0..1.</summary>
		public static void MovePosition(Vector2 position)
		{
			RiHaConfig.x = Mathf.Clamp01(position.x / Screen.width);
			RiHaConfig.y = Mathf.Clamp01(position.y / Screen.height);

			// С зажатым Shift положение округляется до сотых — удобно
			// выставлять ровные значения вроде 0.5.
			if (RDInput.holdingShift)
			{
				RiHaConfig.x = (float)Math.Round(RiHaConfig.x, 2);
				RiHaConfig.y = (float)Math.Round(RiHaConfig.y, 2);
			}

			RiHa.GetComponent<RectTransform>().LocalMoveXY(
				RiHaConfig.x * 1280 - 640,
				RiHaConfig.y * 720 - 360);
		}
	}
}
