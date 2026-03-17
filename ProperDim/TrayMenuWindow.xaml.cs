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

	public TrayMenuWindow(MainWindow mainWindow)
	{
		InitializeComponent();
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

		// Default to full virtual screen bounds
		double screenTop = SystemParameters.VirtualScreenTop;
		double screenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
		double screenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;

		// Pinpoint the exact monitor the cursor is on using MainWindow's tracked monitors
		foreach (var m in _mainWindow.Monitors)
		{
			if (pt.X >= m.Bounds.Left && pt.X <= m.Bounds.Right &&
				pt.Y >= m.Bounds.Top && pt.Y <= m.Bounds.Bottom)
			{
				screenTop = m.Bounds.Top / dpiY;
				screenBottom = m.Bounds.Bottom / dpiY;
				screenRight = m.Bounds.Right / dpiX;
				break;
			}
		}

		double newLeft = cursorX - 10;
		double newTop = cursorY - this.ActualHeight;

		// If the taskbar is at the top, rendering upwards pushes the menu off-screen.
		// Render downwards from the cursor instead.
		if (newTop < screenTop)
		{
			newTop = cursorY;
		}

		// Safety clamp to ensure it doesn't bleed off the bottom or right edges
		if (newTop + this.ActualHeight > screenBottom)
		{
			newTop = screenBottom - this.ActualHeight;
		}

		if (newLeft + this.ActualWidth > screenRight)
		{
			newLeft = screenRight - this.ActualWidth - 5;
		}

		this.Left = newLeft;
		this.Top = newTop;

		this.Activate();
		this.Focus();

		var anim = new System.Windows.Media.Animation.DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(150));
		this.BeginAnimation(Window.OpacityProperty, anim);
	}

	protected override void OnClosed(EventArgs e)
	{
		_mainWindow.GlobalBrightnessChanged -= OnGlobalBrightnessChanged;
		base.OnClosed(e);
	}

	private void Window_Deactivated(object sender, EventArgs e)
	{
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

	private void MenuSlider_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		double step = 0.05;
		if (e.Delta > 0) MenuSlider.Value += step;
		else MenuSlider.Value -= step;
	}

	private void MenuSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is System.Windows.Controls.Slider slider)
		{
			slider.CaptureMouse();
			UpdateSliderFromMouse(slider, e);
			_isDragging = true;
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