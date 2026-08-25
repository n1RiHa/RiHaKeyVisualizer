using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace RIHaKeyVisualizer
{
	/// <summary>
	/// Настройки мода. Unity Mod Manager сам сериализует этот класс в
	/// Settings.xml (через XmlSerializer), поэтому все поля должны быть
	/// public и иметь тип, который умеет сериализоваться (примитивы,
	/// enum, string, List&lt;T&gt;).
	/// </summary>
	public class Config : UnityModManager.ModSettings
	{
		// ============================================================
		// 0. ЯЗЫК ИНТЕРФЕЙСА
		// ============================================================

		/// <summary>Язык окна настроек.</summary>
		public Language language = Language.English;

		/// <summary>Доступные языки интерфейса.</summary>
		public enum Language
		{
			Russian,
			English,
			Korean
		}

		// ============================================================
		// 1. ПОЛОЖЕНИЕ / РАЗМЕР / ПРОЗРАЧНОСТЬ
		//    x и y хранятся в долях экрана (0..1), чтобы картинка
		//    оставалась на том же месте при смене разрешения.
		// ============================================================

		/// <summary>Положение по горизонтали: 0 = левый край, 1 = правый край.</summary>
		public float x = 0.5f;

		/// <summary>Положение по вертикали: 0 = низ экрана, 1 = верх экрана.</summary>
		public float y = 0.1f;

		/// <summary>Масштаб картинки. 1 = "родной" размер риха.</summary>
		public float size = 1f;

		/// <summary>
		/// Прозрачность оверлея: 0 = полностью прозрачно,
		/// 1 = полностью непрозрачно. Реализована через CanvasGroup.alpha.
		/// </summary>
		public float opacity = 1f;

		// ============================================================
		// 2. КЛАВИШИ, НА КОТОРЫЕ РЕАГИРУЕТ РИХА
		//    Списки, а не одиночные KeyCode — так можно назначить
		//    несколько клавиш на одну "сторону" (например E и D слева).
		//    По умолчанию: E — левое ухо, P — правое ухо.
		// ============================================================

		/// <summary>Клавиши "левого уха" — показывают спрайт rihaLeft.</summary>
		public List<KeyCode> leftKeys = new List<KeyCode>() { KeyCode.E };

		/// <summary>Клавиши "правого уха" — показывают спрайт rihaRight.</summary>
		public List<KeyCode> rightKeys = new List<KeyCode>() { KeyCode.P };

		// ============================================================
		// 3. ЗАМЕНА КАРТИНОК НА СВОИ
		//    Пустая строка = используется картинка из встроенного
		//    AssetBundle "riha". Путь может быть абсолютным
		//    (C:\Images\my_riha.png) или относительным — тогда файл
		//    ищется в папке мода. Поддерживаются PNG и JPG.
		// ============================================================

		/// <summary>Путь к своей картинке для состояния "обе клавиши нажаты".</summary>
		public string customOnPath = "";

		/// <summary>Путь к своей картинке для состояния "нажата только левая клавиша".</summary>
		public string customLeftPath = "";

		/// <summary>Путь к своей картинке для состояния "нажата только правая клавиша".</summary>
		public string customRightPath = "";

		/// <summary>Путь к своей картинке для состояния "ничего не нажато".</summary>
		public string customOffPath = "";

		// ============================================================
		// 4. ГДЕ ПОКАЗЫВАТЬ РИХА
		// ============================================================

		/// <summary>Всегда на экране или только во время геймплея.</summary>
		public DisplayMode displayMode = DisplayMode.Always;

		/// <summary>Обводка вместо тени у картинки и текста.</summary>
		public bool useOutline = false;

		/// <summary>
		/// Режим отображения оверлея.
		/// Аналог настройки "Show only in gameplay" из AdofaiTweaks (KeyViewer).
		/// </summary>
		public enum DisplayMode
		{
			/// <summary>Показывать везде, включая главное меню и редактор.</summary>
			Always,

			/// <summary>Показывать только во время игры (на уровне).</summary>
			OnlyGameplay
		}

		/// <summary>Сохранение настроек (вызывается Unity Mod Manager'ом).</summary>
		public override void Save(UnityModManager.ModEntry modEntry)
		{
			Save(this, modEntry);
		}
	}
}
