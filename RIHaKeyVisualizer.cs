using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityModManagerNet;

namespace RIHaKeyVisualizer
{
	/// <summary>
	/// Компонент, который рисует риха на экране и меняет его картинку
	/// в зависимости от того, какие клавиши зажаты.
	///
	/// Таблица состояний:
	///
	///   зажата левая  + зажата правая  -> Assets.On     (rihaOn)
	///   зажата только левая            -> Assets.Left   (rihaLeft)
	///   зажата только правая           -> Assets.Right  (rihaRight)
	///   ничего не зажато               -> Assets.Off    (rihaOff)
	/// </summary>
	public class RIHaKeyVisualizer : MonoBehaviour, IDragHandler, IEndDragHandler
	{
		/// <summary>Четыре состояния картинки.</summary>
		public enum RiHaState
		{
			Off,
			Left,
			Right,
			On
		}

		// Базовая высота картинки в пикселях канваса (канвас настроен на
		// 1280x720). Ширина считается из пропорций спрайта, чтобы свои
		// картинки любого размера не растягивались.
		private const float BASE_HEIGHT = 100f;

		private GameObject imageObject;

		private RectTransform imageRect;
		private Outline imageOutline;
		private Shadow imageShadow;

		private Image rihaImage;

		/// <summary>Отвечает за общую прозрачность картинки.</summary>
		private CanvasGroup canvasGroup;

		/// <summary>Клавиши, зажатые во время просмотра реплея (заполняется из ReplayInput).</summary>
		private readonly HashSet<KeyCode> replayKeys = new HashSet<KeyCode>();

		private RiHaState currentState = RiHaState.Off;
		private Sprite currentSprite;
		private bool visible = true;

		/// <summary>Создание всех объектов оверлея.</summary>
		private void Awake()
		{
			// CanvasGroup на корневом объекте — один параметр alpha
			// управляет прозрачностью картинки.
			canvasGroup = gameObject.AddComponent<CanvasGroup>();
			canvasGroup.alpha = 1f;
			canvasGroup.interactable = false;

			// --- картинка риха ---
			imageObject = new GameObject("Image");
			imageObject.transform.SetParent(transform);

			rihaImage = imageObject.AddComponent<Image>();
			rihaImage.preserveAspect = true; // страховка от растягивания своих картинок
			imageRect = imageObject.GetComponent<RectTransform>();

			// GraphicRaycaster включается только на время перетаскивания
			// мышью (кнопка "Задать положение мышью").
			imageObject.AddComponent<GraphicRaycaster>().enabled = false;

			imageOutline = imageObject.AddComponent<Outline>();
			imageOutline.effectColor = Color.black;
			imageOutline.effectDistance = new Vector2(1.25f, -1.25f);
			imageOutline.useGraphicAlpha = true;
			imageOutline.enabled = false;

			imageShadow = imageObject.AddComponent<Shadow>();
			imageShadow.effectColor = Color.black.WithAlpha(0.5f);
			imageShadow.effectDistance = new Vector2(3f, -3f);
			imageShadow.useGraphicAlpha = true;

			transform.localScale = new Vector3(0.73f, 0.73f, transform.localScale.z);

			ApplySprite(Assets.Off);
		}

		/// <summary>
		/// Каждый кадр опрашиваем клавиши и обновляем картинку.
		/// Нужно именно текущее удержание, поэтому Input.GetKey в Update —
		/// это проще и надёжнее (тот же подход, что в AdofaiTweaks:
		/// keyState[code] = Input.GetKey(code)).
		/// </summary>
		private void Update()
		{
			if (Main.RiHaConfig == null) return;

			bool left, right;

			if (ReplayInput.IsReplay)
			{
				// Во время реплея реальная клавиатура молчит, состояние
				// приходит от ReplayMod через ReplayInput.
				foreach (KeyCode key in ReplayInput.KeyDownList) replayKeys.Add(key);
				ReplayInput.KeyDownList.Clear();

				foreach (KeyCode key in ReplayInput.KeyUpList) replayKeys.Remove(key);
				ReplayInput.KeyUpList.Clear();

				left = AnyHeld(Main.RiHaConfig.leftKeys, true);
				right = AnyHeld(Main.RiHaConfig.rightKeys, true);
			}
			else
			{
				if (replayKeys.Count > 0) replayKeys.Clear();

				left = AnyHeld(Main.RiHaConfig.leftKeys, false);
				right = AnyHeld(Main.RiHaConfig.rightKeys, false);
			}

			// Та самая таблица состояний из описания класса.
			RiHaState state;
			if (left && right) state = RiHaState.On;
			else if (left) state = RiHaState.Left;
			else if (right) state = RiHaState.Right;
			else state = RiHaState.Off;

			SetState(state);

			// Видимость теперь обновляется здесь, поэтому отдельный файл
			// Patch/HideRiHa.cs больше не нужен.
			Main.UpdateVisibility();
		}

		/// <summary>Проверяет, зажата ли хоть одна клавиша из списка.</summary>
		private bool AnyHeld(List<KeyCode> keys, bool fromReplay)
		{
			if (keys == null) return false;

			foreach (KeyCode key in keys)
			{
				if (key == KeyCode.None) continue;

				bool pressed = fromReplay ? replayKeys.Contains(key) : Input.GetKey(key);
				if (pressed) return true;
			}
			return false;
		}

		/// <summary>Меняет состояние и, если нужно, спрайт.</summary>
		private void SetState(RiHaState state)
		{
			currentState = state;
			ApplySprite(GetSpriteForState(state));
		}

		/// <summary>Принудительно перечитать спрайт (после замены картинок в настройках).</summary>
		public void RefreshSprite()
		{
			ApplySprite(GetSpriteForState(currentState));
		}

		private Sprite GetSpriteForState(RiHaState state)
		{
			switch (state)
			{
				case RiHaState.On: return Assets.On;
				case RiHaState.Left: return Assets.Left;
				case RiHaState.Right: return Assets.Right;
				default: return Assets.Off;
			}
		}

		/// <summary>
		/// Ставит спрайт и подгоняет размер прямоугольника под его пропорции.
		/// Без этого своя картинка (например 512x512) была бы вписана
		/// в стандартный прямоугольник и выглядела бы сплющенной.
		/// </summary>
		private void ApplySprite(Sprite sprite)
		{
			if (sprite == null || sprite == currentSprite) return;

			currentSprite = sprite;
			rihaImage.sprite = sprite;

			float height = sprite.rect.height;
			float width = sprite.rect.width;
			if (height <= 0f) return;

			// Высота всегда BASE_HEIGHT, ширина — по пропорциям картинки.
			imageRect.sizeDelta = new Vector2(BASE_HEIGHT * (width / height), BASE_HEIGHT);
		}

		// ============================================================
		// Настройки, применяемые из Main.ApplyConfig()
		// ============================================================

		/// <summary>Прозрачность всего оверлея (0..1).</summary>
		public void SetOpacity(float opacity)
		{
			canvasGroup.alpha = Mathf.Clamp01(opacity);
		}

		/// <summary>Тень или обводка у картинки.</summary>
		public void SetOutline(bool useOutline)
		{
			imageOutline.enabled = useOutline;
			imageShadow.enabled = !useOutline;
		}

		/// <summary>
		/// Показать/скрыть риха целиком. Используется для настройки
		/// "только во время геймплея" (аналог ViewerOnlyGameplay в AdofaiTweaks).
		/// </summary>
		public void SetVisible(bool value)
		{
			visible = value;
			imageObject.SetActive(value);
		}

		// ============================================================
		// Перетаскивание мышью (режим "Задать положение")
		// ============================================================

		/// <summary>Включает приём кликов мышью по картинке.</summary>
		public void SetRaycast()
		{
			imageObject.GetComponent<GraphicRaycaster>().enabled = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (Main.IsEditingPos)
			{
				Main.MovePosition(eventData.position);
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (Main.IsEditingPos)
			{
				Main.IsEditingPos = false;
				imageObject.GetComponent<GraphicRaycaster>().enabled = false;

				// Возвращаем окно Unity Mod Manager обратно на экран.
				((UnityModManager.UI)AccessTools.Field(typeof(UnityModManager.UI), "mInstance").GetValue(null))
					.ToggleWindow(true);
			}
		}
	}
}
