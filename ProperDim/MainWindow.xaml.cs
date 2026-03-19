using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using H.NotifyIcon.Core;
using Microsoft.Win32;
using static ProperDim.NativeMethods;

namespace ProperDim
{
	public partial class MainWindow : Window
	{
#pragma warning disable SYSLIB1045 // Regex source generators can fail in WPF code-behind
		private static readonly Regex _parenthesesRegex = new(@"\(([^)]*)\)", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

		public event Action<double> GlobalBrightnessChanged;
		public event Action ScheduleTriggered;
		public event Action ScheduleEvaluated;

		// --- FIELDS ---
		private readonly DispatcherTimer _scheduleTimer = new();
		private readonly DispatcherTimer _saveDebouncer;
		private readonly DispatcherTimer _debounceTimer;

		// OPTIMIZATION: Track integers instead of strings for schedule to avoid allocs
		private int _lastTriggeredHour = -1;
		private int _lastTriggeredMinute = -1;

		private readonly GammaAnimator _gammaAnimator;
		private bool _isUpdatingFromAnimator = false;
		private readonly GlobalHotkeyService _hotkeyService;
		private readonly HardwareGammaService _gammaService;
		private DateTime _lastHotkeyTime = DateTime.MinValue;

		// OPTIMIZATION: Static keyword list to avoid reallocation
		private static readonly string[] _virtualKeywords = [
			"indirect", "virtual", "mirror", "spacedesk", "displaylink", "iddsample",
			"superdisplay", "parsec", "duet", "splashtop", "airdisplay", "idisplay",
			"twomon", "wired xdisplay", "luna", "amyuni", "spacedisplay",
			"secondscreen", "miracast", "project", "rdp", "citrix", "vmware",
			"vnc", "remotedisplay", "usb", "widi", "wifi", "root", "umb"
		];

		// --- MONITOR TRACKING ---
		public event Action MonitorsChanged;
		public class MonitorContext
		{
			public IntPtr Handle { get; set; }
			public string DeviceName { get; set; }
			public string FriendlyName { get; set; }
			public double LastAppliedGamma { get; set; } = -1;
			public RectStruct Bounds { get; set; }
			public bool GammaSupported { get; set; } = true;
			public bool IsPrimary { get; set; } = false;
			public bool IsHdrEnabled { get; set; } = false;
		}

		public void HardResetDisplays()
		{
			// 1. Reset hardware gamma and magnification
			foreach (var m in Monitors)
			{
				_gammaService.SetTargetGamma(m.DeviceName, 1.0);
			}
			_gammaService.SetGlobalMagnification(1.0);

			// 2. Clear out the tracked monitors and animation states
			Monitors.Clear();

			// 3. Force a clean re-detection
			RefreshMonitors();

			// 4. Reset the saved state memory so new tray menus spawn at 100%
			ConfigManager.Settings.LastOpacity = 1.0;
			ConfigManager.Settings.Save();

			// 5. Apply 100% brightness instantly to the new clean state
			ApplyBrightness(1.0);
		}

		public ObservableCollection<MonitorContext> Monitors { get; private set; } = [];
		public ObservableCollection<DimSchedule> ActiveSchedules { get; set; } = [];

		public MainWindow()
		{
			// Initialize JSON config before anything else
			ConfigManager.Load();

			// Initialize this BEFORE InitializeComponent(). 
			// This prevents a crash when the XAML parser coerces the slider value and triggers ValueChanged.
			_gammaAnimator = new();

			InitializeComponent();

			// Load raw multi-resolution icon stream to prevent WPF bitmap flattening and blurriness
			var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/ProperDimIcon.ico"))?.Stream;
			if (iconStream != null)
			{
				TrayIcon.Icon = new System.Drawing.Icon(iconStream);
			}

			this.Visibility = Visibility.Hidden;
			this.ShowInTaskbar = false;

			SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
			SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
			SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

			LoadSchedules();
			// Pre-parse offsets once on startup
			SetupTimer();

			_gammaService = new HardwareGammaService();
			_gammaService.Start();

			_hotkeyService = new GlobalHotkeyService();
			_hotkeyService.HotkeyPressed += OnHotkeyPressed;

			_saveDebouncer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			_saveDebouncer.Tick += (s, e) =>
			{
				_saveDebouncer.Stop();
				ConfigManager.Settings.Save();
			};

			_debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
			_debounceTimer.Tick += (s, e) =>
			{
				_debounceTimer.Stop();
				RefreshMonitors();
				ApplyBrightness(ConfigManager.Settings.LastOpacity);
			};
			RefreshMonitors();

			ApplyBrightness(ConfigManager.Settings.LastOpacity);
		}

		private static bool IsVirtualDisplay(params string[] hardwareStrings)
		{
			return _virtualKeywords.Any(keyword =>
				hardwareStrings.Any(hw => !string.IsNullOrEmpty(hw) && hw.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
		}

		private static string GetMonitorFriendlyName(string deviceName, DISPLAYCONFIG_PATH_INFO[] paths, uint pathCount, out string devicePath)
		{
			devicePath = "";
			string resultName = "Display";
			if (paths == null) return resultName;

			try
			{
				for (int i = 0; i < pathCount; i++)
				{
					var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
					sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
					sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
					sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
					sourceName.header.id = paths[i].sourceInfo.id;

					if (DisplayConfigGetDeviceInfo(ref sourceName) == 0)
					{
						if (sourceName.viewGdiDeviceName == deviceName)
						{
							var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
							targetName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
							targetName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
							targetName.header.adapterId = paths[i].targetInfo.adapterId;
							targetName.header.id = paths[i].targetInfo.id;

							if (DisplayConfigGetDeviceInfo(ref targetName) == 0)
							{
								devicePath = targetName.monitorDevicePath ?? "";
								string rawName = targetName.monitorFriendlyDeviceName;
								if (!string.IsNullOrEmpty(rawName))
								{
									Match match = _parenthesesRegex.Match(rawName);
									if (match.Success)
									{
										string content = match.Groups[1].Value.Trim();
										if (!string.IsNullOrEmpty(content)) resultName = content;
									}
									else
									{
										string clean = rawName.Replace("Generic PnP Monitor", "").Replace("Generic Monitor", "").Trim();
										resultName = string.IsNullOrEmpty(clean) ? rawName : clean;
									}
								}
							}
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error retrieving monitor name: {ex.Message}");
			}

			return resultName;
		}

		private static bool IsMonitorHdr(string deviceName, DISPLAYCONFIG_PATH_INFO[] paths, uint pathCount)
		{
			if (paths == null) return false;

			try
			{
				for (int i = 0; i < pathCount; i++)
				{
					var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
					sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
					sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
					sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
					sourceName.header.id = paths[i].sourceInfo.id;

					if (DisplayConfigGetDeviceInfo(ref sourceName) == 0)
					{
						if (sourceName.viewGdiDeviceName == deviceName)
						{
							var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
							colorInfo.header.type = 9; // DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO
							colorInfo.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
							colorInfo.header.adapterId = paths[i].targetInfo.adapterId;
							colorInfo.header.id = paths[i].targetInfo.id;

							if (DisplayConfigGetDeviceInfo(ref colorInfo) == 0)
							{
								// Bit 1 indicates if Advanced Color (HDR) is actively enabled
								return (colorInfo.value & 2) != 0;
							}
							break;
						}
					}
				}
			}
			catch { }
			return false;
		}

		private double _currentGlobalBrightness = 1.0;

		public void TriggerSave()
		{
			_saveDebouncer?.Stop();
			_saveDebouncer?.Start();
		}

		public void ApplyBrightness(double globalBrightness, bool animate = false, bool linear = false, int durationMs = 200)
		{
			// Clamp input to safe bounds (5% to 100%)
			double brightness = Math.Max(0.05, Math.Min(1.0, globalBrightness));

			if (animate && Math.Abs(brightness - _currentGlobalBrightness) > 0.001)
			{
				_gammaAnimator.Stop();
				_gammaAnimator.Start(_currentGlobalBrightness, brightness, TimeSpan.FromMilliseconds(durationMs), (val) =>
				{
					_isUpdatingFromAnimator = true;
					_currentGlobalBrightness = val;
					ApplyBrightness(val, animate: false);
					_isUpdatingFromAnimator = false;
				},
				() =>
				{
					_currentGlobalBrightness = brightness;
					ApplyBrightness(brightness, animate: false);
				}, linear);
				return;
			}

			if (!_isUpdatingFromAnimator) _gammaAnimator.Stop();

			_currentGlobalBrightness = brightness;

			GlobalBrightnessChanged?.Invoke(_currentGlobalBrightness);

			if (TrayIcon != null)
			{
				TrayIcon.ToolTipText = $"ProperDim: {Math.Round(_currentGlobalBrightness * 100)}%";
			}

			// --- APPLY GLOBAL BRIGHTNESS ---
			foreach (var m in Monitors)
			{
				_gammaService.SetTargetGamma(m.DeviceName, _currentGlobalBrightness);
			}

			// Delegate dimming below 50% to the global Magnification API
			_gammaService.SetGlobalMagnification(_currentGlobalBrightness);
		}

		public void ApplyBrightnessAnimated(double opacity)
		{
			ApplyBrightness(opacity, animate: true, linear: false);
		}
		private void SystemEvents_DisplaySettingsChanged(object _, EventArgs __)
		{
			Dispatcher.Invoke(() =>
			{
				RefreshMonitors();
			});
		}
		public void RefreshMonitors()
		{
			List<MonitorContext> activeMonitors = [];

			uint pathCount = 0;
			DISPLAYCONFIG_PATH_INFO[] paths = null;
			try
			{
				if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out uint modeCount) == 0)
				{
					paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
					var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
					if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
					{
						paths = null;
					}
				}
			}
			catch { }

			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
			{
				MONITORINFOEX mi = new() { cbSize = Marshal.SizeOf<MONITORINFOEX>() };

				if (GetMonitorInfo(hMonitor, ref mi))
				{
					string hardwareName = GetMonitorFriendlyName(mi.szDevice, paths, pathCount, out string devicePath);

					// Perform a hard API test to see if the driver has physical LUT memory
					bool hardwareGammaSupported = HardwareGammaService.TestGammaSupport(mi.szDevice);

					DISPLAY_DEVICE adapter = new() { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
					string adapterString = "";
					string adapterId = "";
					string monitorString = "";
					string monitorId = "";

					if (EnumDisplayDevices(mi.szDevice, 0, ref adapter, 0))
					{
						adapterString = adapter.DeviceString ?? "";
						adapterId = adapter.DeviceID ?? "";

						DISPLAY_DEVICE monitor = new() { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
						if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0))
						{
							monitorString = monitor.DeviceString ?? "";
							monitorId = monitor.DeviceID ?? "";
						}
					}

					bool isVirtual = IsVirtualDisplay(mi.szDevice, hardwareName, devicePath, adapterString, adapterId, monitorString, monitorId);

					if (adapterId.Contains("ROOT", StringComparison.OrdinalIgnoreCase) ||
											monitorId.Contains("ROOT", StringComparison.OrdinalIgnoreCase) ||
											adapterId.Contains("CITRIX", StringComparison.OrdinalIgnoreCase) ||
											adapterId.Contains("USB", StringComparison.OrdinalIgnoreCase))
					{
						isVirtual = true;
					}

					bool isPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
					bool isHdr = IsMonitorHdr(mi.szDevice, paths, pathCount);

					activeMonitors.Add(new MonitorContext
					{
						Handle = hMonitor,
						DeviceName = mi.szDevice,
						FriendlyName = hardwareName,
						Bounds = mi.rcMonitor,
						GammaSupported = hardwareGammaSupported && !isVirtual,
						IsPrimary = isPrimary,
						IsHdrEnabled = isHdr
					});
				}
				return true;
			}, IntPtr.Zero);

			bool listChanged = false;

			var toRemove = Monitors.Where(m => !activeMonitors.Any(a => a.DeviceName == m.DeviceName)).ToList();
			foreach (var item in toRemove)
			{
				Monitors.Remove(item);
				listChanged = true;
			}

			foreach (var active in activeMonitors)
			{
				var existing = Monitors.FirstOrDefault(m => m.DeviceName == active.DeviceName);

				if (existing == null)
				{
					var newCtx = new MonitorContext
					{
						Handle = active.Handle,
						DeviceName = active.DeviceName,
						FriendlyName = active.FriendlyName,
						Bounds = active.Bounds,
						GammaSupported = active.GammaSupported,
						IsPrimary = active.IsPrimary,
						IsHdrEnabled = active.IsHdrEnabled
					};

					Monitors.Add(newCtx);
					listChanged = true;
				}
				else
				{
					existing.Handle = active.Handle;
					existing.Bounds = active.Bounds;
					existing.GammaSupported = active.GammaSupported;
					existing.IsPrimary = active.IsPrimary;
					existing.IsHdrEnabled = active.IsHdrEnabled;
				}
			}
			if (listChanged) MonitorsChanged?.Invoke();
		}

		public void HardResetOverlays()
		{
			// 1. Reset hardware gamma and magnification
			foreach (var m in Monitors)
			{
				_gammaService.SetTargetGamma(m.DeviceName, 1.0);
			}
			_gammaService.SetGlobalMagnification(1.0);

			// 2. Clear out the tracked monitors and animation states
			Monitors.Clear();

			// 4. Force a clean re-detection
			RefreshMonitors();

			// 5. Apply 100% brightness instantly to the new clean state
			ApplyBrightness(1.0);
		}

		// OPTIMIZATION: Parse once to dictionary, then read from it

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			var helper = new WindowInteropHelper(this);
			IntPtr hwnd = helper.Handle;
			int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED);

			RegisterGlobalHotkeys();
		}

		private IntPtr HwndHook(IntPtr _1, int msg, IntPtr _2, IntPtr _3, ref bool _4)
		{
			if (msg == WM_DISPLAYCHANGE || msg == WM_SETTINGCHANGE)
			{
				RefreshMonitors();
				ApplyBrightness(ConfigManager.Settings.LastOpacity);
			}
			return IntPtr.Zero;
		}

		// ---------------------------------------------------------
		//  HOTKEYS
		// ---------------------------------------------------------
		public void RegisterGlobalHotkeys()
		{
			_hotkeyService?.RegisterGlobalHotkeys(
				ConfigManager.Settings.HotkeysEnabled,
				ConfigManager.Settings.IncreaseHotkey,
				ConfigManager.Settings.DecreaseHotkey
			);
		}
		private void OnHotkeyPressed(int id)
		{
			if (!ConfigManager.Settings.HotkeysEnabled) return;

			double baseValue = _gammaAnimator.IsActive
				? _gammaAnimator.TargetValue
				: ConfigManager.Settings.LastOpacity;

			int currentPercent = (int)Math.Round(baseValue * 100.0);
			int remainder = currentPercent % 5;
			int snappedPercent = (remainder > 2)
				? currentPercent + (5 - remainder)
				: currentPercent - remainder;

			int targetPercent = snappedPercent;
			if (id == HOTKEY_ID_UP) targetPercent += 5;
			else if (id == HOTKEY_ID_DOWN) targetPercent -= 5;

			if (targetPercent > 100) targetPercent = 100;
			if (targetPercent < 5) targetPercent = 5;

			double finalTarget = targetPercent / 100.0;

			ConfigManager.Settings.LastOpacity = finalTarget;

			_saveDebouncer.Stop();
			_saveDebouncer.Start();

			bool isHeldDown = (DateTime.Now - _lastHotkeyTime).TotalMilliseconds < 150;
			_lastHotkeyTime = DateTime.Now;

			int animDuration = isHeldDown ? 50 : 200;

			ApplyBrightness(finalTarget, animate: true, linear: true, durationMs: animDuration);
		}

		public void SaveSchedules()
		{
			try
			{
				ConfigManager.Settings.SavedSchedules = [.. ActiveSchedules];
				ConfigManager.Settings.Save();
			}
			catch { }
		}

		public void LoadSchedules()
		{
			var list = ConfigManager.Settings.SavedSchedules ?? [];
			ActiveSchedules = [.. list];
		}

		private void SetupTimer()
		{
			_scheduleTimer.Interval = TimeSpan.FromSeconds(10);
			_scheduleTimer.Tick += ScheduleTimer_Tick;
			_scheduleTimer.Start();
		}

		private void ScheduleTimer_Tick(object _, EventArgs __)
		{
			ScheduleEvaluated?.Invoke();

			if (!ConfigManager.Settings.ScheduleToggle) return;

			DateTime now = DateTime.Now;
			int h = now.Hour;
			int m = now.Minute;

			if (_lastTriggeredHour == h && _lastTriggeredMinute == m) return;

			string currentFullDay = now.DayOfWeek.ToString();
			string currentShortDay = GetShortDay(now.DayOfWeek);

			var triggeredSchedule = ActiveSchedules.FirstOrDefault(s =>
				s.Time.Hours == h &&
				s.Time.Minutes == m &&
				(string.IsNullOrEmpty(s.Days) || s.Days.Contains(currentFullDay) || s.Days.Contains(currentShortDay)));

			if (triggeredSchedule == null) return;

			_lastTriggeredHour = h;
			_lastTriggeredMinute = m;

			double val = triggeredSchedule.Brightness;
			ApplyBrightnessAnimated(val);

			ConfigManager.Settings.LastOpacity = val;
			ConfigManager.Settings.Save();
			ScheduleTriggered?.Invoke();
		}

		private static readonly string[] _shortDays = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
		private static string GetShortDay(DayOfWeek d)
		{
			return _shortDays[(int)d];
		}

		private void SystemEvents_SessionSwitch(object _, SessionSwitchEventArgs e)
		{
			if (e.Reason == SessionSwitchReason.SessionUnlock)
			{
				Dispatcher.Invoke(() =>
				{
					ApplyBrightness(ConfigManager.Settings.LastOpacity);

					if (TrayIcon != null)
					{
						// Force the Win32 shell to re-register the tray icon after lock screen suspension
						var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/ProperDimIcon.ico"))?.Stream;
						if (iconStream != null)
						{
							TrayIcon.Icon = new System.Drawing.Icon(iconStream);
						}
					}
				});
			}
		}

		private void SystemEvents_PowerModeChanged(object _, PowerModeChangedEventArgs e)
		{
			if (e.Mode == PowerModes.Resume)
			{
				Dispatcher.Invoke(() =>
				{
					ApplyBrightness(ConfigManager.Settings.LastOpacity);
				});
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
			SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
			SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
			_hotkeyService?.Dispose();
			_gammaService?.Dispose();
			base.OnClosed(e);
		}

		public class GammaAnimator
		{
			private long _startTime;
			private double _from;
			public double TargetValue { get; private set; }
			public bool IsActive { get; private set; }

			private TimeSpan _duration;
			private Action<double> _onUpdate;
			private Action _onComplete;
			private bool _isLinear;

			public GammaAnimator()
			{
			}

			public void Start(double from, double to, TimeSpan duration, Action<double> onUpdate, Action onComplete, bool linear = false)
			{
				Stop();

				_from = from;
				TargetValue = to;
				_duration = duration;
				_onUpdate = onUpdate;
				_onComplete = onComplete;
				_isLinear = linear;
				_startTime = Stopwatch.GetTimestamp();

				IsActive = true;
				CompositionTarget.Rendering += OnRendering;
			}

			private void OnRendering(object sender, EventArgs e)
			{
				if (!IsActive)
				{
					CompositionTarget.Rendering -= OnRendering;
					return;
				}

				long now = Stopwatch.GetTimestamp();
				double elapsedSeconds = (double)(now - _startTime) / Stopwatch.Frequency;
				double progress = elapsedSeconds / _duration.TotalSeconds;

				if (progress >= 1.0)
				{
					progress = 1.0;
					IsActive = false;
					CompositionTarget.Rendering -= OnRendering;
				}

				double easedProgress;
				if (_isLinear)
				{
					easedProgress = progress;
				}
				else
				{
					easedProgress = progress < 0.5 ?
						2 * progress * progress : 1 - Math.Pow(-2 * progress + 2, 2) / 2;
				}

				double currentVal = _from + (TargetValue - _from) * easedProgress;

				_onUpdate?.Invoke(currentVal);

				if (progress >= 1.0)
				{
					_onComplete?.Invoke();
				}
			}

			public void Stop()
			{
				if (IsActive)
				{
					IsActive = false;
					CompositionTarget.Rendering -= OnRendering;
				}
			}
		}

		// --- TRAY MENU HANDLERS ---
		private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
		{
			if (ConfigManager.Settings.SwapTrayIconClicks)
			{
				HandleTrayMenuToggle();
			}
			else
			{
				ToggleControlPanel();
			}
		}

		private TrayMenuWindow _currentTrayMenu;
		private DateTime _lastTrayMenuCloseTime = DateTime.MinValue;

		private void TrayIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
		{
			if (ConfigManager.Settings.SwapTrayIconClicks)
			{
				ToggleControlPanel();
			}
			else
			{
				HandleTrayMenuToggle();
			}
		}

		private void HandleTrayMenuToggle()
		{
			if (_currentTrayMenu != null)
			{
				try { _currentTrayMenu.Close(); } catch { }
				_currentTrayMenu = null;
				return;
			}

			if ((DateTime.Now - _lastTrayMenuCloseTime).TotalMilliseconds < 200)
			{
				return;
			}

			_currentTrayMenu = new TrayMenuWindow(this);
			_currentTrayMenu.Closed += (s, ev) =>
			{
				_currentTrayMenu = null;
				_lastTrayMenuCloseTime = DateTime.Now;
			};
			_currentTrayMenu.Show();
		}

		public void ToggleControlPanel()
		{
			ControlPanel cp = Application.Current.Windows.OfType<ControlPanel>().FirstOrDefault();
			if (cp?.IsVisible == true)
			{
				cp.Hide();
			}
			else
			{
				ShowControlPanel();
			}
		}

		public void ShowControlPanel()
		{
			ControlPanel cp = Application.Current.Windows.OfType<ControlPanel>().FirstOrDefault();
			if (cp == null)
			{
				cp = new ControlPanel(this);
				cp.ShowControlPanel();
			}
			else
			{
				if (!cp.IsVisible)
				{
					cp.ShowControlPanel();
				}
				cp.Activate();
				cp.Focus();
			}
		}

		public void ShutdownApp()
		{
			foreach (var m in Monitors)
			{
				_gammaService.SetTargetGamma(m.DeviceName, 1.0);
			}
			_gammaService.SetGlobalMagnification(1.0);
			SaveSchedules();

			TrayIcon?.Dispose();
			_hotkeyService?.Dispose();
			Application.Current.Shutdown();
		}
	}
}