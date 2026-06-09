/*
 * Copyright 2026 Kevin Stanislawski
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * As an exception, this software is subject to the Commons Clause License Condition v1.0.
 * You may not Sell the Software. For the full text of the Commons Clause, see the LICENSE file.
 */

using System;
using System.Windows.Input;
using System.Windows.Interop;
using static ProperDim.NativeMethods;

namespace ProperDim;

public class GlobalHotkeyService : IDisposable
{
	private readonly HotkeyWindow _hotkeyWindow;
	public event Action<int> HotkeyPressed;
	private static readonly string[] _hotkeySeparator = [" + "];

	public GlobalHotkeyService()
	{
		_hotkeyWindow = new HotkeyWindow();
		_hotkeyWindow.HotkeyPressed += (id) => HotkeyPressed?.Invoke(id);
	}

	public void RegisterGlobalHotkeys(bool isEnabled, string increaseHotkey, string decreaseHotkey)
	{
		if (_hotkeyWindow == null) return;
		IntPtr handle = _hotkeyWindow.Handle;

		UnregisterHotKey(handle, HOTKEY_ID_UP);
		UnregisterHotKey(handle, HOTKEY_ID_DOWN);

		if (!isEnabled) return;

		RegisterSingleHotkey(handle, HOTKEY_ID_UP, increaseHotkey);
		RegisterSingleHotkey(handle, HOTKEY_ID_DOWN, decreaseHotkey);
	}

	private static void RegisterSingleHotkey(IntPtr handle, int id, string hotkeyString)
	{
		if (string.IsNullOrEmpty(hotkeyString)) return;
		int modifiers = 0;
		Key key = Key.None;

		string[] parts = hotkeyString.Split(_hotkeySeparator, StringSplitOptions.None);
		foreach (var part in parts)
		{
			string p = part.Trim();
			switch (p)
			{
				case "Ctrl": modifiers |= MOD_CTRL; break;
				case "Alt": modifiers |= MOD_ALT; break;
				case "Shift": modifiers |= MOD_SHIFT; break;
				case "Win": modifiers |= MOD_WIN; break;
				default: if (!Enum.TryParse<Key>(p, true, out key)) key = Key.None; break;
			}
		}
		if (key != Key.None)
		{
			int vkey = KeyInterop.VirtualKeyFromKey(key);
			RegisterHotKey(handle, id, modifiers, vkey);
		}
	}

	public void Dispose()
	{
		_hotkeyWindow?.Dispose();
		GC.SuppressFinalize(this);
	}

	private class HotkeyWindow : IDisposable
	{
		private HwndSource _source;
		private const int WM_HOTKEY = 0x0312;
		public event Action<int> HotkeyPressed;

		public HotkeyWindow()
		{
			var parameters = new HwndSourceParameters("HotkeyHost")
			{
				Width = 0,
				Height = 0,
				PositionX = 0,
				PositionY = 0,
				WindowStyle = unchecked((int)0x80000000),
				ParentWindow = new IntPtr(-3)
			};
			_source = new HwndSource(parameters);
			_source.AddHook(WndProc);
		}

		public IntPtr Handle => _source.Handle;

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_HOTKEY)
			{
				handled = true;
				HotkeyPressed?.Invoke(wParam.ToInt32());
			}
			return IntPtr.Zero;
		}

		public void Dispose()
		{
			_source?.RemoveHook(WndProc); _source?.Dispose(); _source = null;
			GC.SuppressFinalize(this);
		}
	}
}