using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProperDim;

public partial class OffsetControls : Window
{
	private readonly MainWindow _mainWindow;
	private bool _settingsSaved = false;
	public static bool IsSyncEnabled { get; set; } = true;
	private readonly System.Windows.Threading.DispatcherTimer _ellipsisTimer;
	private int _ellipsisCount = 0;
	private readonly bool _isPrimarySliderDragging = false;
	private bool _isAnySliderDragging = false;

	// Flag to prevent loop: Slider Changed -> Main Apply -> Event Fired -> Slider Changed again
	private bool _isUpdatingUi = false;
	private DateTime _lastPrimaryInteraction = DateTime.MinValue;

	public OffsetControls(MainWindow mainWindow)
	{
		InitializeComponent();
		_ellipsisTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
		_ellipsisTimer.Tick += EllipsisTimer_Tick;

		_mainWindow = mainWindow;
		_mainWindow.MonitorsChanged += OnMonitorsChanged;

		// SUBSCRIBE to the new global event
		_mainWindow.GlobalBrightnessChanged += OnGlobalBrightnessChanged;

		_mainWindow.RefreshMonitors();
		MonitorsItemsControl.ItemsSource = _mainWindow.Monitors;
		GenerateUI();
	}

	private void OnGlobalBrightnessChanged(double newBrightness)
	{
		if (IsSyncEnabled) return;

		// Ignore updates if user clicked primary slider recently
		if ((DateTime.Now - _lastPrimaryInteraction).TotalMilliseconds < 500) return;

		if (_isPrimarySliderDragging) return;

		_isUpdatingUi = true;

		try
		{
			for (int i = 0; i < MonitorsItemsControl.Items.Count; i++)
			{
				if (MonitorsItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is DependencyObject container)
				{
					var slider = FindVisualChild<Slider>(container);
					if (slider != null && slider.Tag is MainWindow.MonitorContext mon && mon.IsPrimary)
					{
						if (slider.IsMouseCaptured) continue;
						slider.Value = newBrightness;
					}
				}
			}
		}
		finally
		{
			_isUpdatingUi = false;
		}
	}

	// --- HORIZONTAL SCROLLING LOGIC ---

	private void ScrollBar_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is ScrollBar scrollBar && scrollBar.TemplatedParent is ScrollViewer sv)
		{
			// TUNE THIS VALUE: Distance in pixels to move per scroll wheel tick
			double scrollDistance = 60.0;

			// Calculate the new target offset based on the current scroll position
			double targetOffset = sv.HorizontalOffset;

			// Inverted Logic: Scroll Up (Delta > 0) moves Right, Scroll Down moves Left
			if (e.Delta > 0)
			{
				targetOffset += scrollDistance;
			}
			else
			{
				targetOffset -= scrollDistance;
			}

			// Ensure the target offset doesn't exceed the boundaries of the ScrollViewer
			if (targetOffset < 0) targetOffset = 0;
			if (targetOffset > sv.ScrollableWidth) targetOffset = sv.ScrollableWidth;

			// Trigger the existing animation method
			AnimateHorizontalScroll(sv, targetOffset);

			// Mark handled so it doesn't bubble up to the window
			e.Handled = true;
		}
	}

	private bool _isHorizontalScrollBarDragging = false;

	private void ScrollBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		// Ignore clicks on the Thumb itself so standard dragging handles it
		if (e.OriginalSource is FrameworkElement fe && (fe is Thumb || fe.TemplatedParent is Thumb))
			return;

		if (sender is ScrollBar scrollBar && scrollBar.Orientation == Orientation.Horizontal)
		{
			scrollBar.CaptureMouse();
			_isHorizontalScrollBarDragging = true;
			UpdateHorizontalScrollFromMouse(scrollBar, e);
			e.Handled = true; // Prevents the default PageLeft/PageRight jump
		}
	}

	private void ScrollBar_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isHorizontalScrollBarDragging && sender is ScrollBar scrollBar && scrollBar.IsMouseCaptured)
		{
			UpdateHorizontalScrollFromMouse(scrollBar, e);
		}
	}

	private void ScrollBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isHorizontalScrollBarDragging && sender is ScrollBar scrollBar)
		{
			scrollBar.ReleaseMouseCapture();
			_isHorizontalScrollBarDragging = false;
			e.Handled = true;
		}
	}

	private static void UpdateHorizontalScrollFromMouse(ScrollBar scrollBar, MouseEventArgs e)
	{
		if (scrollBar.Template.FindName("PART_Track", scrollBar) is Track track)
		{
			double value = track.ValueFromPoint(e.GetPosition(track));
			if (!double.IsNaN(value))
			{
				if (scrollBar.TemplatedParent is ScrollViewer scrollViewer)
				{
					// Explicitly destroy any holding smooth-scroll animations to prevent snapping
					scrollViewer.BeginAnimation(ScrollViewerBehavior.HorizontalOffsetProperty, null);

					// Scroll the viewer directly
					scrollViewer.ScrollToHorizontalOffset(value);

					// Keep the smooth scroll attached property in sync
					ScrollViewerBehavior.SetHorizontalOffset(scrollViewer, value);
				}
			}
		}
	}

	private static void AnimateHorizontalScroll(ScrollViewer sv, double target)
	{
		DoubleAnimation scrollAnimation = new()
		{
			To = target,
			Duration = TimeSpan.FromMilliseconds(300),
			EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
		};

		// Use the attached property to animate the read-only HorizontalOffset
		sv.BeginAnimation(ScrollViewerBehavior.HorizontalOffsetProperty, scrollAnimation);
	}

	// --- ATTACHED PROPERTY HELPER ---
	// Add this class at the bottom of your namespace
	public static class ScrollViewerBehavior
	{
		public static readonly DependencyProperty HorizontalOffsetProperty =
			DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollViewerBehavior),
				new PropertyMetadata(0.0, OnHorizontalOffsetChanged));

		public static void SetHorizontalOffset(FrameworkElement target, double value) => target.SetValue(HorizontalOffsetProperty, value);
		public static double GetHorizontalOffset(FrameworkElement target) => (double)target.GetValue(HorizontalOffsetProperty);

		private static void OnHorizontalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			if (target is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToHorizontalOffset((double)e.NewValue);
			}
		}
	}
	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (this.IsLoaded && e.HeightChanged && e.PreviousSize.Height > 0)
		{
			double heightDifference = e.NewSize.Height - e.PreviousSize.Height;
			this.Top -= heightDifference;
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		this.WindowStartupLocation = WindowStartupLocation.Manual;
	}

	private void OnMonitorsChanged()
	{
		Dispatcher.Invoke(() => GenerateUI());
	}

	private void SyncBrightness_Checked(object sender, RoutedEventArgs e)
	{
		if (InstructionText is { } it) it.Visibility = Visibility.Visible;
		if (InstructionTextIndependent is { } iti) iti.Visibility = Visibility.Collapsed;

		if (IsSyncEnabled) return;
		IsSyncEnabled = true;

		// --- Save to Settings ---
		ConfigManager.Settings.SyncBrightness = true;
		ConfigManager.Settings.Save();

		double global = ConfigManager.Settings.LastOpacity;
		if (global < 0.05) global = 0.05;

		// Recalculate relative offsets based on current global brightness
		foreach (var m in _mainWindow.Monitors)
		{
			m.PreSyncOffset = m.Offset;
			m.Offset = Math.Min(1.0, m.Offset / global);
		}

		GenerateUI();
	}

	private void SyncBrightness_Unchecked(object sender, RoutedEventArgs e)
	{
		if (InstructionText is { } it) it.Visibility = Visibility.Collapsed;
		if (InstructionTextIndependent is { } iti) iti.Visibility = Visibility.Visible;

		if (!IsSyncEnabled) return;
		IsSyncEnabled = false;

		// --- Save to Settings ---
		ConfigManager.Settings.SyncBrightness = false;
		ConfigManager.Settings.Save();

		// Get the current global brightness
		double global = (double)ConfigManager.Settings.LastOpacity;

		foreach (var m in _mainWindow.Monitors)
		{
			// OPTIMIZATION: Use the cached IsPrimary flag
			if (m.IsPrimary)
			{
				m.Offset = global;
				m.PreSyncOffset = null;
			}
			else
			{
				if (m.PreSyncOffset.HasValue)
				{
					m.Offset = m.PreSyncOffset.Value;
					m.PreSyncOffset = null;
				}
				else
				{
					m.Offset = Math.Min(1.0, m.Offset * global);
				}
			}
		}

		_mainWindow.ApplyBrightness(global);
		GenerateUI();
	}

	private void Reset_Click(object sender, RoutedEventArgs e)
	{
		foreach (var monitor in _mainWindow.Monitors)
		{
			monitor.Offset = 1.0;
		}

		double currentGlobalOpacity = (double)ConfigManager.Settings.LastOpacity;
		_mainWindow.ApplyBrightness(currentGlobalOpacity);

		GenerateUI();
	}

	protected override void OnClosed(EventArgs e)
	{
		_mainWindow.MonitorsChanged -= OnMonitorsChanged;
		_mainWindow.GlobalBrightnessChanged -= OnGlobalBrightnessChanged; // Unsubscribe
		_ellipsisTimer.Stop();

		if (!_settingsSaved)
		{
			_mainWindow.LoadOffsets();
			double currentGlobalOpacity = (double)ConfigManager.Settings.LastOpacity;
			_mainWindow.ApplyBrightness(currentGlobalOpacity);
		}

		base.OnClosed(e);
	}

	private void Apply_Click(object sender, RoutedEventArgs e)
	{
		_settingsSaved = true;
		_mainWindow.SaveOffsets();
		this.Close();
	}

	private void GenerateUI()
	{
		// --- 1. Dynamic Window Resizing ---
		// 280px fits 2 sliders. 390px fits 3 sliders.
		// Any more than 3 will trigger the scrollbar.

		// DEBUG SIMULATION: Creating a temporary list with dummy monitors
		var displayList = _mainWindow.Monitors.ToList();

#if DEBUG
		// Add 3 dummy displays to force the >3 monitor layout and scrollbar
		for (int i = 1; i <= 3; i++)
		{
			displayList.Add(new MainWindow.MonitorContext
			{
				FriendlyName = $"DummyDummyDerpDerp {i}",
				Offset = 1.0,
				IsPrimary = false
			});
		}
#endif

		// Update the ItemsSource to use our (potentially) expanded list
		MonitorsItemsControl.ItemsSource = displayList;

		if (displayList.Count > 2)
		{
			this.Width = 390;
		}
		else
		{
			this.Width = 280;
		}

		// --- 2. Sync Checkbox Setup ---
		if (SyncBrightnessCheckBox != null)
		{
			SyncBrightnessCheckBox.Checked -= SyncBrightness_Checked;
			SyncBrightnessCheckBox.Unchecked -= SyncBrightness_Unchecked;
			SyncBrightnessCheckBox.IsChecked = IsSyncEnabled;

			if (InstructionText is { } it)
				it.Visibility = IsSyncEnabled ? Visibility.Visible : Visibility.Collapsed;

			if (InstructionTextIndependent is { } iti)
				iti.Visibility = IsSyncEnabled ? Visibility.Collapsed : Visibility.Visible;

			SyncBrightnessCheckBox.Checked += SyncBrightness_Checked;
			SyncBrightnessCheckBox.Unchecked += SyncBrightness_Unchecked;
		}

		// --- 3. Content Generation ---
		if (displayList.Count <= 1)
		{
			if (!_ellipsisTimer.IsEnabled) _ellipsisTimer.Start();
			NoDisplayView.Visibility = Visibility.Visible;
			MultiDisplayView.Visibility = Visibility.Collapsed;
		}
		else
		{
			_ellipsisTimer.Stop();
			MultiDisplayView.Visibility = Visibility.Visible;
			NoDisplayView.Visibility = Visibility.Collapsed;
		}
	}

	private void MonitorColumn_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is StackPanel sp && FindVisualChild<Slider>(sp) is Slider slider)
		{
			double change = slider.SmallChange;
			if (e.Delta > 0) slider.Value += change;
			else slider.Value -= change;
			e.Handled = true;
		}
	}

	private void MonitorSlider_DragStarted(object sender, DragStartedEventArgs e) => _isAnySliderDragging = true;
	private void MonitorSlider_DragCompleted(object sender, DragCompletedEventArgs e) => _isAnySliderDragging = false;

	private void MonitorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_isUpdatingUi || sender is not Slider slider || slider.Tag is not MainWindow.MonitorContext monitor) return;

		bool isPrimary = monitor.IsPrimary;

		if (!IsSyncEnabled)
		{
			if (isPrimary)
			{
				_lastPrimaryInteraction = DateTime.Now;

				if (_isAnySliderDragging)
				{
					monitor.Offset = e.NewValue;
					_mainWindow.ApplyBrightness(e.NewValue);
				}
				else
				{
					_mainWindow.ApplyBrightnessAnimated(e.NewValue);
				}

				ConfigManager.Settings.LastOpacity = e.NewValue;
				ConfigManager.Settings.Save();
			}
			else
			{
				if (_isAnySliderDragging) _mainWindow.SetMonitorOffset(monitor, e.NewValue);
				else _mainWindow.ApplyMonitorOffsetAnimated(monitor, e.NewValue);
			}
		}
		else
		{
			if (_isAnySliderDragging) _mainWindow.SetMonitorOffset(monitor, e.NewValue);
			else _mainWindow.ApplyMonitorOffsetAnimated(monitor, e.NewValue);
		}
	}

	private void MonitorName_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is TextBlock name && name.Parent is Canvas canvas)
		{
			// Force measurement of the raw string to get its true unclipped width
			name.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			double trueWidth = name.DesiredSize.Width;

			// Stop any previous animations from a previous GenerateUI call
			name.BeginAnimation(Canvas.LeftProperty, null);
			name.RenderTransform = null;

			if (trueWidth > canvas.Width)
			{
				// --- Handle Scrolled Ticker (Long Names) ---
				string originalText = name.Text;
				string gapText = "      "; // Minimal gap between repeating names

				// Create a dummy TextBlock to measure exactly one loop segment
				TextBlock measureBlock = new()
				{
					Text = originalText + gapText,
					FontFamily = name.FontFamily,
					FontSize = name.FontSize,
					FontWeight = name.FontWeight,
					FontStyle = name.FontStyle
				};
				measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				double loopDistance = measureBlock.DesiredSize.Width;

				// Apply the duplicate text to create the seamless visual wrap
				name.Text = originalText + gapText + originalText;

				// Standard left-align inside canvas for animation start
				Canvas.SetLeft(name, 0);

				// Animate from 0 to exactly negative one loop distance, then instantly snap back
				DoubleAnimation doubleAnimation = new()
				{
					From = 0,
					To = -loopDistance,
					RepeatBehavior = RepeatBehavior.Forever,
					Duration = new Duration(TimeSpan.FromSeconds(loopDistance / 15.0)) // 15 pixels per second
				};

				TranslateTransform tt = new();
				name.RenderTransform = tt;
				tt.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
			}
			else
			{
				// --- Handle Centered Text (Short Names) ---
				// Explicitly calculate and apply the X-coordinate needed to center the text
				double centeredX = (canvas.Width - trueWidth) / 2.0;
				Canvas.SetLeft(name, centeredX);
			}
		}
	}

	private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
	{
		if (parent == null) return null;
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T typedChild) return typedChild;
			var result = FindVisualChild<T>(child);
			if (result != null) return result;
		}
		return null;
	}
	private void Slider_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is Slider slider)
		{
			double change = slider.SmallChange;
			if (e.Delta > 0) slider.Value += change;
			else slider.Value -= change;
			e.Handled = true;
		}
	}

	private void EllipsisTimer_Tick(object sender, EventArgs e)
	{
		_ellipsisCount++;
		if (_ellipsisCount > 3) _ellipsisCount = 1;

		if (WaitingForDisplaysText is { } wfd)
		{
			wfd.Text = "Waiting for displays" + new string('.', _ellipsisCount);
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	protected override void OnSourceInitialized(System.EventArgs e)
	{
		base.OnSourceInitialized(e);
		System.IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

		int preference = NativeMethods.DWMWCP_ROUND;
		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

		int darkMode = 0;
		if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int isLight && isLight == 0)
		{
			darkMode = 1;
		}

		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
	}

	// --- CUSTOM OFFSET SLIDER CLICK-AND-DRAG LOGIC ---

	private void OffsetSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Slider slider)
		{
			slider.CaptureMouse();
			UpdateOffsetSliderFromMouse(slider, e);
			_isAnySliderDragging = true;
		}
	}

	private void OffsetSlider_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isAnySliderDragging && sender is Slider slider && slider.IsMouseCaptured)
		{
			UpdateOffsetSliderFromMouse(slider, e);
		}
	}

	private void OffsetSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isAnySliderDragging && sender is Slider slider)
		{
			slider.ReleaseMouseCapture();
			_isAnySliderDragging = false;
		}
	}

	private static void UpdateOffsetSliderFromMouse(Slider slider, MouseEventArgs e)
	{
		if (slider.Template.FindName("PART_Track", slider) is Track track && track.ActualHeight > 0)
		{
			Point p = e.GetPosition(track);
			double ratio = 1.0 - (p.Y / track.ActualHeight);
			ratio = Math.Max(0.0, Math.Min(1.0, ratio));
			slider.Value = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
		}
	}
}