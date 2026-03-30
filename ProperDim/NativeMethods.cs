using System;
using System.Runtime.InteropServices;

namespace ProperDim;

// --- NATIVE STRUCTS & DELEGATES ---
[StructLayout(LayoutKind.Sequential)]
public struct LUID { public uint LowPart; public int HighPart; }

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
	public uint type;
	public uint size;
	public LUID adapterId;
	public uint id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
	public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
	public uint flags;
	public uint outputTechnology;
	public ushort edidManufactureId;
	public ushort edidProductCodeId;
	public uint connectorInstance;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
	public string monitorFriendlyDeviceName;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string monitorDevicePath;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
	public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string viewGdiDeviceName;
}

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
	public LUID adapterId;
	public uint id;
	public uint modeInfoIdx;
	public uint statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_TARGET_INFO
{
	public LUID adapterId;
	public uint id;
	public uint modeInfoIdx;
	public uint outputTechnology;
	public uint rotation;
	public uint scaling;
	public Rational refreshRate;
	public uint scanlineOrdering;
	public bool targetAvailable;
	public uint statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct Rational { public uint Numerator; public uint Denominator; }

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_PATH_INFO
{
	public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
	public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
	public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_MODE_INFO
{
	public uint infoType;
	public uint id;
	public LUID adapterId;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
	public byte[] modeInfo;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct RAMP
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public ushort[] Red;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public ushort[] Green;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public ushort[] Blue;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DISPLAY_DEVICE
{
	public int cb;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string DeviceName;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string DeviceString;
	public int StateFlags;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string DeviceID;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
	public string DeviceKey;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct MONITORINFOEX
{
	public int cbSize;
	public RectStruct rcMonitor;
	public RectStruct rcWork;
	public int dwFlags;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	public string szDevice;
}

[StructLayout(LayoutKind.Sequential)]
public struct RectStruct
{
	public int Left;
	public int Top;
	public int Right;
	public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINTStruct
{
	public int X;
	public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
{
	public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
	public uint value;
	public uint colorEncoding;
	public uint bitsPerColorChannel;
}

[StructLayout(LayoutKind.Sequential)]
public struct MAGCOLOREFFECT
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
	public float[] transform;
}

public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
	public POINTStruct pt;
	public int mouseData;
	public int flags;
	public int time;
	public IntPtr dwExtraInfo;
}

// --- NATIVE METHODS & CONSTANTS ---
#pragma warning disable SYSLIB1054

internal static class NativeMethods
{
	[DllImport("Magnification.dll", ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool MagInitialize();

	[DllImport("Magnification.dll", ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool MagUninitialize();

	[DllImport("Magnification.dll", ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT pEffect);

	public const int GWL_EXSTYLE = -20;
	public const int WS_EX_TRANSPARENT = 0x00000020;
	public const int WS_EX_LAYERED = 0x00080000;
	public const int WS_EX_TOOLWINDOW = 0x00000080;

	public const int HOTKEY_ID_UP = 9000;
	public const int HOTKEY_ID_DOWN = 9001;
	public const int MOD_ALT = 0x0001;
	public const int MOD_CTRL = 0x0002;
	public const int MOD_SHIFT = 0x0004;
	public const int MOD_WIN = 0x0008;

	public const int WM_DISPLAYCHANGE = 0x007E;
	public const int WM_SETTINGCHANGE = 0x001A;
	public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
	public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
	public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
	public const int MONITORINFOF_PRIMARY = 0x00000001;

	public static readonly IntPtr HWND_TOPMOST = new(-1);
	public const uint SWP_NOMOVE = 0x0002;
	public const uint SWP_NOSIZE = 0x0001;
	public const uint SWP_NOACTIVATE = 0x0010;
	public const uint SWP_SHOWWINDOW = 0x0040;

	public const int MDT_EFFECTIVE_DPI = 0;
	public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	public const int DWMWCP_ROUND = 2;
	public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	[DllImport("dwmapi.dll")]
	internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	[DllImport("user32.dll")]
	internal static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

	[DllImport("user32.dll")]
	internal static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements, [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr topologyId);

	[DllImport("user32.dll")]
	internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

	[DllImport("user32.dll")]
	internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);

	[DllImport("user32.dll")]
	internal static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO deviceName);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("shcore.dll")]
	internal static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetCursorPos(out POINTStruct lpPoint);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("user32.dll")]
	internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
	internal static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

	[DllImport("user32.dll")]
	internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

	public const int WH_MOUSE_LL = 14;
	public const int WM_MOUSEWHEEL = 0x020A;

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	internal static extern IntPtr GetModuleHandle(string lpModuleName);

	public static void PrepareWindowForOS(System.Windows.Window window, string win11BackgroundHex = "#2D2D2D")
	{
		if (Environment.OSVersion.Version.Build < 22000)
		{
			window.AllowsTransparency = true;
			window.Background = System.Windows.Media.Brushes.Transparent;
		}
		else
		{
			window.AllowsTransparency = false;
			window.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(win11BackgroundHex));
		}
	}

	public static void ApplyWindows10Shadow(System.Windows.Window window)
	{
		if (Environment.OSVersion.Version.Build >= 22000) return;
		if (!System.Windows.SystemParameters.DropShadow) return;

		if (window.Content is System.Windows.FrameworkElement root)
		{
			// Margin expanded to 28 to accommodate BlurRadius (24) + ShadowDepth (3)
			root.Margin = new System.Windows.Thickness(28);

			var shadow = new System.Windows.Media.Effects.DropShadowEffect
			{
				BlurRadius = window.IsActive ? 24 : 14,
				ShadowDepth = window.IsActive ? 3 : 1,
				Opacity = window.IsActive ? 0.70 : 0.60,
				Direction = 270
			};
			root.Effect = shadow;

			var duration = TimeSpan.FromMilliseconds(150);

			window.Activated += (s, e) =>
			{
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, new System.Windows.Media.Animation.DoubleAnimation(24, duration));
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ShadowDepthProperty, new System.Windows.Media.Animation.DoubleAnimation(3, duration));
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0.70, duration));
			};

			window.Deactivated += (s, e) =>
			{
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, new System.Windows.Media.Animation.DoubleAnimation(14, duration));
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.ShadowDepthProperty, new System.Windows.Media.Animation.DoubleAnimation(1, duration));
				shadow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0.60, duration));
			};

			if (!double.IsNaN(window.Width)) window.Width += 56;
			if (!double.IsNaN(window.Height)) window.Height += 56;
			if (window.MinWidth > 0) window.MinWidth += 56;
			if (window.MinHeight > 0) window.MinHeight += 56;
			if (window.MaxWidth > 0 && window.MaxWidth < double.PositiveInfinity) window.MaxWidth += 56;
			if (window.MaxHeight > 0 && window.MaxHeight < double.PositiveInfinity) window.MaxHeight += 56;
		}
	}
}
#pragma warning restore SYSLIB1054
