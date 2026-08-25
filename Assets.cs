using System.Collections.Generic;
using System.IO;
using RIHaKeyVisualizer.Utils;
using UnityEngine;

namespace RIHaKeyVisualizer
{
	/// <summary>
	/// Загрузка спрайтов.
	///
	/// Логика двухслойная:
	///  1) из папки мода грузятся 4 стандартных PNG-файла
	///     (riha_on.png, riha_left.png, riha_right.png, riha_off.png);
	///  2) поверх них могут быть подгружены пользовательские PNG/JPG с диска
	///     (механизм взят из MyOshiOverlay: File.ReadAllBytes +
	///     Texture2D.LoadImage), и тогда свойства On/Left/Right/Off
	///     возвращают именно их.
	///
	/// AssetBundle "riha" больше не используется — все картинки лежат
	/// рядом с dll обычными файлами.
	/// </summary>
	public static class Assets
	{
		// --- стандартные спрайты из PNG-файлов папки мода ---
		private static Sprite defaultOn;
		private static Sprite defaultLeft;
		private static Sprite defaultRight;
		private static Sprite defaultOff;

		// --- пользовательские спрайты (null, если путь не задан/файл не найден) ---
		private static Sprite customOn;
		private static Sprite customLeft;
		private static Sprite customRight;
		private static Sprite customOff;

		/// <summary>
		/// Текстуры/спрайты, созданные из пользовательских файлов. Храним их,
		/// чтобы при повторной загрузке освободить память (Object.Destroy).
		/// Стандартные спрайты сюда не попадают — они живут всё время работы мода.
		/// </summary>
		private static readonly List<Object> createdObjects = new List<Object>();

		// Свойства, которые использует RIHaKeyVisualizer: пользовательская
		// картинка имеет приоритет, при её отсутствии — стандартная.
		public static Sprite On => customOn ?? defaultOn;
		public static Sprite Left => customLeft ?? defaultLeft;
		public static Sprite Right => customRight ?? defaultRight;
		public static Sprite Off => customOff ?? defaultOff;

		/// <summary>Путь к папке мода — нужен для относительных путей к картинкам.</summary>
		private static string modPath = "";

		/// <summary>
		/// Загружает стандартные спрайты из PNG-файлов папки мода.
		/// Вызывается один раз при старте мода.
		/// </summary>
		public static void Load(string path)
		{
			modPath = path;

			defaultOn = LoadSpriteFromFile("riha_on.png", false);
			defaultLeft = LoadSpriteFromFile("riha_left.png", false);
			defaultRight = LoadSpriteFromFile("riha_right.png", false);
			defaultOff = LoadSpriteFromFile("riha_off.png", false);
		}

		/// <summary>
		/// Перечитывает пользовательские картинки по путям из настроек.
		/// Вызывается при старте мода и по кнопке "Применить картинки".
		/// </summary>
		public static void ReloadCustomSprites(Config config)
		{
			// Сначала освобождаем ранее созданные текстуры/спрайты,
			// иначе при каждом нажатии "Применить" будет течь память.
			foreach (Object obj in createdObjects)
			{
				if (obj != null) Object.Destroy(obj);
			}
			createdObjects.Clear();

			customOn = LoadSpriteFromFile(config.customOnPath, true);
			customLeft = LoadSpriteFromFile(config.customLeftPath, true);
			customRight = LoadSpriteFromFile(config.customRightPath, true);
			customOff = LoadSpriteFromFile(config.customOffPath, true);
		}

		/// <summary>
		/// Читает картинку с диска и превращает её в Sprite.
		/// Возвращает null, если путь пустой или файла нет.
		/// trackForCleanup: true — спрайт пользовательский и будет
		/// уничтожен при следующем ReloadCustomSprites.
		/// </summary>
		private static Sprite LoadSpriteFromFile(string path, bool trackForCleanup)
		{
			if (string.IsNullOrEmpty(path)) return null;

			// Убираем кавычки и пробелы — пользователь часто копирует путь
			// через "Копировать как путь" в проводнике (приём из MyOshiOverlay).
			path = path.Trim().Trim('"');

			// Относительный путь ищем внутри папки мода.
			if (!Path.IsPathRooted(path)) path = Path.Combine(modPath, path);

			if (!File.Exists(path))
			{
				RiHaLogger.Warn("Файл картинки не найден: " + path);
				return null;
			}

			try
			{
				byte[] data = File.ReadAllBytes(path);

				// mipChain: false — картинка рисуется 1:1 в UI, мипмапы не нужны.
				Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

				// LoadImage сам определяет PNG/JPG и подгоняет размер текстуры.
				if (!texture.LoadImage(data))
				{
					RiHaLogger.Warn("Не удалось прочитать картинку (поддерживаются PNG и JPG): " + path);
					Object.Destroy(texture);
					return null;
				}

				texture.filterMode = FilterMode.Bilinear;
				texture.wrapMode = TextureWrapMode.Clamp;

				Sprite sprite = Sprite.Create(
					texture,
					new Rect(0f, 0f, texture.width, texture.height),
					new Vector2(0.5f, 0.5f), // точка привязки — центр картинки
					100f);                   // pixels per unit

				if (trackForCleanup)
				{
					createdObjects.Add(texture);
					createdObjects.Add(sprite);
				}

				RiHaLogger.Info("Загружена картинка: " + path + " (" + texture.width + "x" + texture.height + ")");
				return sprite;
			}
			catch (System.Exception e)
			{
				RiHaLogger.Error("Ошибка загрузки картинки " + path + ": " + e);
				return null;
			}
		}
	}
}
