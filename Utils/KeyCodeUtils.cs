using System;
using System.Collections.Generic;
using UnityEngine;

namespace RIHaKeyVisualizer.Utils
{
	public static class KeyCodeUtils
	{
		public static Dictionary<KeyCode, string> Keys;

		static KeyCodeUtils()
		{
			Keys = new Dictionary<KeyCode, string>();

			foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
			{
				string str = "";
				switch (key)
				{
					case KeyCode.Alpha0:
						str = "0";
						break;
					case KeyCode.Alpha1:
						str = "1";
						break;
					case KeyCode.Alpha2:
						str = "2";
						break;
					case KeyCode.Alpha3:
						str = "3";
						break;
					case KeyCode.Alpha4:
						str = "4";
						break;
					case KeyCode.Alpha5:
						str = "5";
						break;
					case KeyCode.Alpha6:
						str = "6";
						break;
					case KeyCode.Alpha7:
						str = "7";
						break;
					case KeyCode.Alpha8:
						str = "8";
						break;
					case KeyCode.Alpha9:
						str = "9";
						break;
					case KeyCode.Backslash:
						str = "\\";
						break;
					case KeyCode.Comma:
						str = ",";
						break;
					case KeyCode.Delete:
						str = "Del";
						break;
					case KeyCode.Equals:
						str = "=";
						break;
					case KeyCode.Escape:
						str = "Esc";
						break;
					case KeyCode.Insert:
						str = "Ins";
						break;
					case KeyCode.Minus:
						str = "-";
						break;
					case KeyCode.Numlock:
						str = "Num";
						break;
					case KeyCode.Period:
						str = ".";
						break;
					case KeyCode.Print:
						str = "PrtSc";
						break;
					case KeyCode.Quote:
						str = "'";
						break;
					case KeyCode.Return:
						str = "Enter";
						break;
					case KeyCode.Semicolon:
						str = ";";
						break;
					case KeyCode.Slash:
						str = "/";
						break;
					case KeyCode.BackQuote:
						str = "`";
						break;
					case KeyCode.DownArrow:
						str = "Down";
						break;
					case KeyCode.KeypadDivide:
						str = "Keypad/";
						break;
					case KeyCode.KeypadEnter:
						str = "Enter";
						break;
					case KeyCode.KeypadEquals:
						str = "Keypad=";
						break;
					case KeyCode.KeypadMinus:
						str = "Keypad-";
						break;
					case KeyCode.KeypadMultiply:
						str = "Keypad*";
						break;
					case KeyCode.KeypadPeriod:
						str = "Keypad.";
						break;
					case KeyCode.KeypadPlus:
						str = "Keypad+";
						break;
					case KeyCode.LeftAlt:
						str = "LAlt";
						break;
					case KeyCode.LeftMeta:
						if (ADOBase.platform == Platform.Mac) str = "LCmd";
						else if (ADOBase.platform == Platform.Linux) str = "LMeta";
						else str = "LWin";
						break;
					case KeyCode.LeftArrow:
						str = "Left";
						break;
					case KeyCode.LeftBracket:
						str = "[";
						break;
					case KeyCode.LeftControl:
						str = "LCtrl";
						break;
					case KeyCode.LeftShift:
						str = "LShift";
						break;
					case KeyCode.LeftWindows:
						str = "LWin";
						break;
					case KeyCode.PageDown:
						str = "PgDown";
						break;
					case KeyCode.PageUp:
						str = "PgUp";
						break;
					case KeyCode.RightAlt:
						str = "RAlt";
						break;
					case KeyCode.RightApple:
						if (ADOBase.platform == Platform.Mac) str = "RCmd";
						else if (ADOBase.platform == Platform.Linux) str = "RMeta";
						else str = "RWin";
						break;
					case KeyCode.RightArrow:
						str = "Right";
						break;
					case KeyCode.RightBracket:
						str = "]";
						break;
					case KeyCode.RightControl:
						str = "RCtrl";
						break;
					case KeyCode.RightShift:
						str = "RShift";
						break;
					case KeyCode.RightWindows:
						str = "RWin";
						break;
					case KeyCode.ScrollLock:
						str = "ScrLk";
						break;
					case KeyCode.UpArrow:
						str = "Up";
						break;
					default:
						str = key.ToString();
						break;
				}

				if (Keys.ContainsKey(key))
					RiHaLogger.Warn(key.ToString() + " already added.");
				else
					Keys.Add(key, str);
			}
		}

		public static string GetKeyName(KeyCode keyCode)
		{
			return Keys[keyCode];
		}
	}
}
