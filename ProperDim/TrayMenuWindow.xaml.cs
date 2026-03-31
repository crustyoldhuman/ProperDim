using System;
using System.Windows;
using System.Windows.Input;
using static ProperDim.NativeMethods;

namespace ProperDim;

public partial class TrayMenuWindow : Window
{
	private readonly MainWindow _mainWindow;
	private bool _isSyncing = true;
	private DateTime _lastUserInteraction = DateTime.MinValue;

	// --- HOOK FIELDS ---
	private IntPtr _hookID = IntPtr.Zero;
	private LowLevelMouseProc _proc; // Must be kept alive in memory so the GC doesn't collect the callback

	private static IntPtr SetHook(LowLevelMouseProc proc)
	{
		using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
		using var curModule = curProcess.MainModule;
		return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
	}

	private void RemoveHook()
	{
		if (_hookID != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_hookID);
			_hookID = IntPtr.Zero;
		}
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
		{
			MSLLHOOKSTRUCT hookStruct = System.Runtime.InteropServices.Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

			// Extract the wheel delta (high-order word of mouseData)
			int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

			Dispatcher.Invoke(() =>
			{
				int currentPercent = (int)Math.Round(MenuSlider.Value * 100);
				int newPercent;

				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
				{
					if (delta > 0) newPercent = currentPercent + 1;
					else newPercent = currentPercent - 1;
				}
				else
				{
					if (delta > 0)
					{
						newPercent = ((currentPercent + 4) / 5) * 5;
						if (newPercent == currentPercent) newPercent += 5;
					}
					else
					{
						newPercent = ((currentPercent - 1) / 5) * 5;
					}
				}

				int minPercent = (int)Math.Round(MenuSlider.Minimum * 100);
				int maxPercent = (int)Math.Round(MenuSlider.Maximum * 100);

				newPercent = Math.Max(minPercent, Math.Min(maxPercent, newPercent));

				MenuSlider.Value = newPercent / 100.0;
			});

			// Return 1 to "eat" the message so the window underneath doesn't also scroll
			return (IntPtr)1;
		}
		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	public TrayMenuWindow(MainWindow mainWindow)
	{
		InitializeComponent();

		if (!SystemParameters.DropShadow && this.Content is FrameworkElement root)
		{
			root.Effect = null;
		}

		_mainWindow = mainWindow;
		MenuSlider.Value = ConfigManager.Settings.LastOpacity;
		_mainWindow.GlobalBrightnessChanged += OnGlobalBrightnessChanged;
		_isSyncing = false;
	}

	private void OnGlobalBrightnessChanged(double newBrightness)
	{
		if ((DateTime.Now - _lastUserInteraction).TotalMilliseconds < 300) return;

		_isSyncing = true;
		MenuSlider.Value = newBrightness;
		_isSyncing = false;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		GetCursorPos(out POINTStruct pt);

		var source = PresentationSource.FromVisual(this);
		double dpiX = 1.0, dpiY = 1.0;
		if (source?.CompositionTarget != null)
		{
			dpiX = source.CompositionTarget.TransformToDevice.M11;
			dpiY = source.CompositionTarget.TransformToDevice.M22;
		}

		double cursorX = pt.X / dpiX;
		double cursorY = pt.Y / dpiY;

		RectStruct workArea = new() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
		RectStruct monitorArea = workArea;

		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
		{
			if (pt.X >= lprcMonitor.Left && pt.X <= lprcMonitor.Right && pt.Y >= lprcMonitor.Top && pt.Y <= lprcMonitor.Bottom)
			{
				MONITORINFOEX mi = new() { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFOEX>() };
				if (GetMonitorInfo(hMonitor, ref mi))
				{
					workArea = mi.rcWork;
					monitorArea = mi.rcMonitor;
				}
			}
			return true;
		}, IntPtr.Zero);

		double workBottom = workArea.Bottom / dpiY;
		double workRight = workArea.Right / dpiX;
		double workTop = workArea.Top / dpiY;
		double workLeft = workArea.Left / dpiX;

		bool isTop = workArea.Top > monitorArea.Top;
		bool isLeft = workArea.Left > monitorArea.Left;
		bool isRight = workArea.Right < monitorArea.Right;

		// If the cursor clicked inside the Work Area, the icon is inside the Overflow Menu flyout, not on the Taskbar.
		bool isInOverflow = pt.X >= workArea.Left && pt.X <= workArea.Right && pt.Y >= workArea.Top && pt.Y <= workArea.Bottom;

		// The XAML has a 10px margin around the border for the drop shadow.
		// To make the visible UI physically flush with the taskbar, we pull the window into the taskbar boundary by 10px.
		double edgeOffset = -28;

		double newLeft, newTop;

		if (isInOverflow)
		{
			int cursorClearance = 2; // Prevents the transparent shadow margin from intercepting the toggle click

			if (isRight)
			{
				// Position the window so its bottom-right visual corner sits left and up from the cursor
				newLeft = cursorX - this.ActualWidth - edgeOffset - cursorClearance;
				newTop = cursorY - this.ActualHeight - edgeOffset - cursorClearance;
			}
			else if (isTop)
			{
				// Position the window so its top-left visual corner sits right and down from the cursor
				newLeft = cursorX + edgeOffset + cursorClearance;
				newTop = cursorY + edgeOffset + cursorClearance;
			}
			else
			{
				// Default (Bottom or Left Taskbar): Position bottom-left visual corner right and up from the cursor
				newLeft = cursorX + edgeOffset + cursorClearance;
				newTop = cursorY - this.ActualHeight - edgeOffset - cursorClearance;
			}
		}
		else if (isLeft)
		{
			newLeft = workLeft + edgeOffset;
			newTop = cursorY - this.ActualHeight + 20; // Align bottom of menu upwards from cursor
		}
		else if (isRight)
		{
			newLeft = workRight - this.ActualWidth - edgeOffset;
			newTop = cursorY - this.ActualHeight + 20; // Align bottom of menu upwards from cursor
		}
		else if (isTop)
		{
			newTop = workTop + edgeOffset;
			newLeft = cursorX - (this.ActualWidth / 2);
		}
		else // Bottom (or auto-hide)
		{
			newTop = workBottom - this.ActualHeight - edgeOffset;
			newLeft = cursorX - (this.ActualWidth / 2);
		}

		// Final safety clamps must use the raw Monitor Area so the shadow bounding box can safely enter the taskbar space
		double screenRight = monitorArea.Right / dpiX;
		double screenBottom = monitorArea.Bottom / dpiY;
		double screenLeft = monitorArea.Left / dpiX;
		double screenTop = monitorArea.Top / dpiY;

		newLeft = Math.Max(screenLeft, Math.Min(screenRight - this.ActualWidth, newLeft));
		newTop = Math.Max(screenTop, Math.Min(screenBottom - this.ActualHeight, newTop));

		this.Left = newLeft;
		this.Top = newTop;

		this.Activate();
		this.Focus();

		if (SystemParameters.ClientAreaAnimation)
		{
			var anim = new System.Windows.Media.Animation.DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(150));
			this.BeginAnimation(Window.OpacityProperty, anim);
		}
		else
		{
			this.Opacity = 1.0;
		}

		_proc = HookCallback;
		_hookID = SetHook(_proc);
	}

	protected override void OnClosed(EventArgs e)
	{
		RemoveHook();
		_mainWindow.GlobalBrightnessChanged -= OnGlobalBrightnessChanged;
		base.OnClosed(e);
	}

	private void Window_Deactivated(object sender, EventArgs e)
	{
		RemoveHook();
		this.Close();
	}

	private void Controls_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.ShowControlPanel();
		// Removing this.Close() prevents double-closure crashes. 
		// Focusing the Control Panel automatically triggers Window_Deactivated, which safely handles the closure.
	}

	private void Exit_Click(object sender, RoutedEventArgs e)
	{
		_mainWindow.ShutdownApp();
	}

	private bool _isDragging = false;

	private void MenuSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_isSyncing) return;

		_lastUserInteraction = DateTime.Now;
		ConfigManager.Settings.LastOpacity = e.NewValue;

		if (_isDragging)
		{
			_mainWindow.ApplyBrightness(e.NewValue);
		}
		else
		{
			_mainWindow.ApplyBrightnessAnimated(e.NewValue);
			_mainWindow.TriggerSave();
		}
	}

	private void MenuSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is System.Windows.Controls.Slider slider)
		{
			slider.CaptureMouse();
			_isDragging = true;

			// Only snap to the pointer if clicking the track, ignore if clicking the Thumb itself
			if (!(e.OriginalSource is FrameworkElement fe && (fe is System.Windows.Controls.Primitives.Thumb || fe.TemplatedParent is System.Windows.Controls.Primitives.Thumb)))
			{
				UpdateSliderFromMouse(slider, e);
			}
		}
	}

	private void MenuSlider_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isDragging && sender is System.Windows.Controls.Slider slider && slider.IsMouseCaptured)
		{
			UpdateSliderFromMouse(slider, e);
		}
	}

	private void MenuSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging && sender is System.Windows.Controls.Slider slider)
		{
			slider.ReleaseMouseCapture();
			_isDragging = false;
			_mainWindow?.TriggerSave();
		}
	}

	private static void UpdateSliderFromMouse(System.Windows.Controls.Slider slider, MouseEventArgs e)
	{
		if (slider.Template.FindName("PART_Track", slider) is System.Windows.Controls.Primitives.Track track && track.ActualHeight > 0)
		{
			Point p = e.GetPosition(track);
			// Invert the Y calculation because the slider is oriented vertically
			double ratio = 1.0 - (p.Y / track.ActualHeight);
			ratio = Math.Max(0.0, Math.Min(1.0, ratio));
			slider.Value = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
		}
	}
}