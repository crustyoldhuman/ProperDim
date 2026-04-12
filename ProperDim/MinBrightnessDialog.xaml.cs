using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ProperDim;

public partial class MinBrightnessDialog : Window
{
	public MinBrightnessDialog()
	{
		NativeMethods.PrepareWindowForOS(this, "#2D2D2D");
		InitializeComponent();
		NativeMethods.ApplyWindows10Shadow(this);

		if (SystemParameters.ClientAreaAnimation)
		{
			this.Opacity = 0;
			this.Loaded += (s, e) =>
			{
				System.Windows.Media.Animation.DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
				this.BeginAnimation(Window.OpacityProperty, anim);
			};

			this.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Escape)
				{
					this.Close();
					e.Handled = true;
				}
			};
		}
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		IntPtr hwnd = new WindowInteropHelper(this).Handle;

		// Enforce Windows 11 rounded corners on the hardware drop shadow
		int preference = NativeMethods.DWMWCP_ROUND;
		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

		// Poll OS for Dark Mode to ensure the shadow renders correctly
		int darkMode = 0;
		if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int isLight && isLight == 0)
		{
			darkMode = 1;
		}
		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	private bool _isDragging = false;

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		MinSlider.Value = ConfigManager.Settings.GlobalMinimum;
	}

	// --- SLIDER PREVIEW LOGIC ---

	private void MinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		// Stripped: Value changes no longer trigger live previews automatically.
	}

	private void MinSlider_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		double step = 0.01;
		if (e.Delta > 0) MinSlider.Value = Math.Min(MinSlider.Maximum, MinSlider.Value + step);
		else MinSlider.Value = Math.Max(MinSlider.Minimum, MinSlider.Value - step);
		e.Handled = true;
	}

	private void MinSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is System.Windows.Controls.Slider slider)
		{
			slider.CaptureMouse();
			_isDragging = true;

			bool clickedTrack = !(e.OriginalSource is FrameworkElement fe && (fe is System.Windows.Controls.Primitives.Thumb || fe.TemplatedParent is System.Windows.Controls.Primitives.Thumb));

			if (clickedTrack)
			{
				UpdateSliderFromMouse(slider, e);
			}

			// If clicking the track, smoothly transition to the new spot. If grabbing the thumb, instant is fine.
			if (Owner is ControlPanel cp) cp.ApplyPreview(slider.Value, clickedTrack);
		}
	}

	private void MinSlider_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isDragging && sender is System.Windows.Controls.Slider slider && slider.IsMouseCaptured)
		{
			UpdateSliderFromMouse(slider, e);
			// Dragging must remain instantaneous (animate: false) so it perfectly tracks the mouse without rubber-banding
			if (Owner is ControlPanel cp) cp.ApplyPreview(slider.Value, false);
		}
	}

	private void MinSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging && sender is System.Windows.Controls.Slider slider)
		{
			slider.ReleaseMouseCapture();
			_isDragging = false;

			if (Owner is ControlPanel cp) cp.EndPreview(true);
		}
	}

	private static void UpdateSliderFromMouse(System.Windows.Controls.Slider slider, MouseEventArgs e)
	{
		if (slider.Template.FindName("PART_Track", slider) is System.Windows.Controls.Primitives.Track track && track.ActualWidth > 0)
		{
			Point p = e.GetPosition(track);
			double ratio = p.X / track.ActualWidth;
			ratio = Math.Max(0.0, Math.Min(1.0, ratio));
			slider.Value = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
		}
	}

	// --- PREVIEW BUTTON LOGIC ---

	private void StartPreview()
	{
		if (Owner is ControlPanel cp) cp.ApplyPreview(MinSlider.Value, true);
	}

	private void StopPreview()
	{
		if (Owner is ControlPanel cp) cp.EndPreview(true);
	}

	private void PreviewButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		StartPreview();
	}

	private void PreviewButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		StopPreview();
	}

	private void PreviewButton_MouseLeave(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			StopPreview();
		}
	}

	private void LooksGood_Click(object sender, RoutedEventArgs e)
	{
		ConfigManager.Settings.GlobalMinimum = MinSlider.Value;
		ConfigManager.Settings.Save();

		this.DialogResult = true;
		this.Close();
	}

	private void NeverMind_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = false;
		this.Close();
	}
	private void PreviewButton_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.IsRepeat) return;

		if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Space)
		{
			StartPreview();
			e.Handled = true;
		}
	}

	private void PreviewButton_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Space)
		{
			StopPreview();
			e.Handled = true;
		}
	}
}