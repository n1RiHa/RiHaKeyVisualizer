using System.Collections.Generic;
using UnityEngine;

namespace RIHaKeyVisualizer
{
	/// <summary>
	/// Мостик для мода реплеев (ReplayMod). Во время просмотра реплея
	/// настоящая клавиатура не нажимается, поэтому нажатия приходят сюда,
	/// а RIHaKeyVisualizer.Update() их разбирает.
	///
	/// Такой же приём используется в AdofaiTweaks (Tweaks/KeyViewer/ReplayInput.cs).
	/// </summary>
	public static class ReplayInput
	{
		/// <summary>true, пока проигрывается реплей.</summary>
		public static bool IsReplay = false;

		/// <summary>Клавиши, нажатые с прошлого кадра.</summary>
		public static List<KeyCode> KeyDownList = new List<KeyCode>();

		/// <summary>Клавиши, отпущенные с прошлого кадра.</summary>
		public static List<KeyCode> KeyUpList = new List<KeyCode>();

		public static void OnStartInputs()
		{
			IsReplay = true;
			KeyDownList.Clear();
			KeyUpList.Clear();
		}

		public static void OnEndInputs()
		{
			IsReplay = false;
			KeyDownList.Clear();
			KeyUpList.Clear();
		}

		public static void OnKeyPressed(KeyCode keyCode)
		{
			KeyDownList.Add(keyCode);
		}

		public static void OnKeyReleased(KeyCode keyCode)
		{
			KeyUpList.Add(keyCode);
		}
	}
}
