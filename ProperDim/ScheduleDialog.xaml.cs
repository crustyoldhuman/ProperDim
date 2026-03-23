using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProperDim;

public partial class ScheduleDialog : Window
{
	public DimSchedule Result { get; private set; }
	private readonly List<DimSchedule> _existingSchedules;
	private readonly string _originalButtonText = "Add Event";
	private readonly DimSchedule _editingItem = null;
	private readonly double _originalBrightness;
	private static readonly string[] _daySeparator = [", "];


	// 1. THIS IS FOR ADDING NEW SCHEDULES
	public ScheduleDialog(List<DimSchedule> existingSchedules)
	{
		InitializeComponent();

		this.Opacity = 0;
		this.Loaded += (s, e) =>
		{
			DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
			this.BeginAnimation(Window.OpacityProperty, anim);
		};

		_existingSchedules = existingSchedules ?? [];

		Is24hCheck.IsChecked = ConfigManager.Settings.UseMilitaryTime;
		_originalBrightness = ConfigManager.Settings.LastOpacity;
		DialogDimmerSlider.Background = Brushes.Transparent;

		// "Add" mode defaults
		_originalButtonText = "Add Event";
		ActionButton.Content = _originalButtonText;

		PopulateTimeBoxes();

		// Default to current time and brightness
		DateTime now = DateTime.Now;
		SetTimeUI(now.Hour, now.Minute);
		DialogDimmerSlider.Value = _originalBrightness;

		// Load the last used days from settings
		string savedDays = ConfigManager.Settings.LastScheduleDays;
		if (string.IsNullOrEmpty(savedDays)) savedDays = "Mo, Tu, We, Th, Fr, Sa, Su";
		SetDayCheckboxes(savedDays);

		ValidateTime();
	}

	// 2. THIS IS FOR EDITING EXISTING SCHEDULES
	public ScheduleDialog(DimSchedule existing, List<DimSchedule> existingSchedules)
	{
		InitializeComponent();

		this.Opacity = 0;
		this.Loaded += (s, e) =>
		{
			DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
			this.BeginAnimation(Window.OpacityProperty, anim);
		};

		_existingSchedules = existingSchedules ?? [];
		_editingItem = existing;

		Is24hCheck.IsChecked = ConfigManager.Settings.UseMilitaryTime;
		_originalBrightness = ConfigManager.Settings.LastOpacity;
		DialogDimmerSlider.Background = Brushes.Transparent;

		// "Update" mode text
		_originalButtonText = "Update Event";
		ActionButton.Content = _originalButtonText;

		PopulateTimeBoxes();

		// Load values from the existing item
		DialogDimmerSlider.Value = existing.Brightness;
		SetTimeUI(existing.Time.Hours, existing.Time.Minutes);
		SetDayCheckboxes(existing.Days);

		ValidateTime();
	}

	private void SetTimeUI(int h, int m)
	{
		if (Is24hCheck.IsChecked == true)
		{
			HourBox.SelectedItem = h.ToString("D2");
		}
		else
		{
			string amPm = (h >= 12) ? "PM" : "AM";
			int h12 = h % 12;
			if (h12 == 0) h12 = 12;

			HourBox.SelectedItem = h12.ToString("D2");
			if (PmRadio != null && AmRadio != null)
			{
				if (amPm == "PM") PmRadio.IsChecked = true;
				else AmRadio.IsChecked = true;
			}
		}
		MinuteBox.SelectedItem = m.ToString("D2");
	}

	private void SetDayCheckboxes(string daysString)
	{
		// Reset all boxes first
		DayM.IsChecked = DayT.IsChecked = DayW.IsChecked = DayTh.IsChecked = DayF.IsChecked = DayS.IsChecked = DaySu.IsChecked = false;

		var dayParts = daysString.Split(_daySeparator, StringSplitOptions.None);

		foreach (var d in dayParts)
		{
			if (d == "Mo" || d == "M" || d == "Monday") DayM.IsChecked = true;
			if (d == "Tu" || d == "T" || d == "Tuesday") DayT.IsChecked = true;
			if (d == "We" || d == "W" || d == "Wednesday") DayW.IsChecked = true;
			if (d == "Th" || d == "Thursday") DayTh.IsChecked = true;
			if (d == "Fr" || d == "F" || d == "Friday") DayF.IsChecked = true;
			if (d == "Sa" || d == "S" || d == "Saturday") DayS.IsChecked = true;
			if (d == "Su" || d == "Sunday") DaySu.IsChecked = true;
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

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = false;
		this.Close();
	}

	private void PopulateTimeBoxes()
	{
		if (HourBox == null) return;
		HourBox.Items.Clear();
		bool is24h = Is24hCheck?.IsChecked ?? false;

		if (is24h)
		{
			for (int i = 0; i < 24; i++) HourBox.Items.Add(i.ToString("D2"));
		}
		else
		{
			for (int i = 1; i <= 12; i++) HourBox.Items.Add(i.ToString("D2"));
		}

		HourBox.SelectedIndex = 0;
		if (MinuteBox?.Items.Count == 0)
		{
			for (int i = 0; i < 60; i++) MinuteBox.Items.Add(i.ToString("D2"));
			MinuteBox.SelectedIndex = 0;
		}
	}

	private void ValidateTime()
	{
		if (HourBox?.SelectedItem == null || MinuteBox?.SelectedItem == null || ActionButton == null) return;

		int hour = int.Parse(HourBox.SelectedItem.ToString());
		int minute = int.Parse(MinuteBox.SelectedItem.ToString());

		if (Is24hCheck.IsChecked == false && PmRadio != null)
		{
			string amPm = PmRadio.IsChecked == true ? "PM" : "AM";
			if (amPm == "PM" && hour < 12) hour += 12;
			if (amPm == "AM" && hour == 12) hour = 0;
		}

		TimeSpan selectedTime = new(hour, minute, 0);

		// Gather currently selected days using the new helper method
		List<string> currentDays = GetSelectedDaysFromUI();

		bool exists = _existingSchedules.Any(s =>
		{
			// Skip if it's the item we are currently editing
			if (s == _editingItem) return false;

			// If times don't match, there is no conflict
			if (s.Time != selectedTime) return false;

			// If times match, check for day overlap
			// Split the stored days string (e.g. "Mo, Tu") into parts
			var scheduleDays = s.Days.Split(_daySeparator, StringSplitOptions.None);

			// Conflict exists ONLY if the sets of days intersect
			return scheduleDays.Intersect(currentDays).Any();
		});

		if (exists)
		{
			ActionButton.Content = "Timeslot already scheduled";
			ActionButton.IsEnabled = false;
			ActionButton.Opacity = 0.4;
		}
		else
		{
			ActionButton.Content = _originalButtonText;
			ActionButton.IsEnabled = true;
			ActionButton.Opacity = 1.0;
		}
	}

	private List<string> GetSelectedDaysFromUI()
	{
		List<string> days = [];

		if (DayM.IsChecked == true) days.Add("Mo");
		if (DayT.IsChecked == true) days.Add("Tu");
		if (DayW.IsChecked == true) days.Add("We");
		if (DayTh.IsChecked == true) days.Add("Th");
		if (DayF.IsChecked == true) days.Add("Fr");
		if (DayS.IsChecked == true) days.Add("Sa");
		if (DaySu.IsChecked == true) days.Add("Su");

		return days;
	}

	private void DayCheck_Changed(object sender, RoutedEventArgs e) => ValidateTime();

	private void TimeSelection_Changed(object sender, SelectionChangedEventArgs e) => ValidateTime();

	private void AmPmRadio_Click(object sender, RoutedEventArgs e) => ValidateTime();

	private void Is24hCheck_Changed(object sender, RoutedEventArgs e)
	{
		int currentHour = 0;
		int currentMinute = 0;
		bool hasSelection = HourBox?.SelectedItem is not null && MinuteBox?.SelectedItem is not null;

		if (hasSelection)
		{
			currentHour = int.Parse(HourBox.SelectedItem.ToString());
			currentMinute = int.Parse(MinuteBox.SelectedItem.ToString());

			// If switching TO 24h, the old state was 12h. Convert to 24h format before repopulating.
			if (Is24hCheck.IsChecked == true && PmRadio is { } pmRadio)
			{
				string amPm = pmRadio.IsChecked == true ? "PM" : "AM";
				if (amPm == "PM" && currentHour < 12) currentHour += 12;
				if (amPm == "AM" && currentHour == 12) currentHour = 0;
			}
			// If switching TO 12h, currentHour is already 0-23 which SetTimeUI expects.
		}

		PopulateTimeBoxes();

		if (AmPmContainer is { } container)
		{
			container.Visibility = Is24hCheck.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
		}

		if (hasSelection)
		{
			SetTimeUI(currentHour, currentMinute);
		}

		ValidateTime();
	}

	private void DialogSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (this.IsLoaded && Owner is ControlPanel main)
		{
			if (_hasDragged)
			{
				main.ApplyPreview(e.NewValue, false);
			}
		}
	}

	private void DialogDimmerSlider_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		double step = 0.01; // 1%
		if (e.Delta > 0)
		{
			DialogDimmerSlider.Value = Math.Min(DialogDimmerSlider.Maximum, DialogDimmerSlider.Value + step);
		}
		else
		{
			DialogDimmerSlider.Value = Math.Max(DialogDimmerSlider.Minimum, DialogDimmerSlider.Value - step);
		}
		e.Handled = true;
	}

	// --- CUSTOM SLIDER LOGIC START ---

	private bool _isDragging = false;
	private void Add_Click(object sender, RoutedEventArgs e)
	{
		if (HourBox.SelectedItem == null || MinuteBox.SelectedItem == null) return;

		int hour = int.Parse(HourBox.SelectedItem.ToString());
		int minute = int.Parse(MinuteBox.SelectedItem.ToString());

		if (Is24hCheck.IsChecked == false && PmRadio != null)
		{
			string amPm = PmRadio.IsChecked == true ? "PM" : "AM";
			if (amPm == "PM" && hour < 12) hour += 12;
			if (amPm == "AM" && hour == 12) hour = 0;
		}

		// 1. Use the helper method (Lowers complexity from 17 down to 4)
		List<string> selectedDays = GetSelectedDaysFromUI();

		if (selectedDays.Count == 0)
		{
			WarningMessage.Show(this, "You forgot to select the event days!", "Missing Event Information", true, "Oops");
			return;
		}

		// 2. Create the Result object using ONLY the properties defined in DimSchedule.cs
		Result = new DimSchedule
		{
			Time = new TimeSpan(hour, minute, 0),
			Brightness = DialogDimmerSlider.Value,
			Days = string.Join(", ", selectedDays)
		};

		ConfigManager.Settings.LastScheduleDays = Result.Days;
		ConfigManager.Settings.UseMilitaryTime = Is24hCheck.IsChecked == true;
		ConfigManager.Settings.Save();

		this.DialogResult = true;
		// Note: this.Close() isn't needed cuz setting DialogResult closes it automatically.
	}

	// --- CUSTOM SCHEDULE SLIDER CLICK-AND-DRAG LOGIC ---

	private bool _hasDragged = false;

	private void DialogDimmerSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Slider slider)
		{
			slider.CaptureMouse();
			_hasDragged = false;

			// Only snap to the pointer if clicking the track, ignore if clicking the Thumb itself
			if (!(e.OriginalSource is FrameworkElement fe && (fe is Thumb || fe.TemplatedParent is Thumb)))
			{
				UpdateScheduleSliderFromMouse(slider, e);
			}

			_isDragging = true;
		}
	}

	private void DialogDimmerSlider_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isDragging && sender is Slider slider && slider.IsMouseCaptured)
		{
			_hasDragged = true;
			UpdateScheduleSliderFromMouse(slider, e);
		}
	}

	private void DialogDimmerSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging && sender is Slider slider)
		{
			slider.ReleaseMouseCapture();
			_isDragging = false;

			// Only restore original brightness if they actually dragged the slider
			if (_hasDragged && Owner is ControlPanel main)
			{
				main.EndPreview(false);
			}
			_hasDragged = false;
		}
	}

	private static void UpdateScheduleSliderFromMouse(Slider slider, MouseEventArgs e)
	{
		if (slider.Template.FindName("PART_Track", slider) is Track track && track.ActualWidth > 0)
		{
			Point p = e.GetPosition(track);
			double ratio = p.X / track.ActualWidth;
			ratio = Math.Max(0.0, Math.Min(1.0, ratio));
			slider.Value = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
		}
	}

	private void ScrollBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is ScrollBar scrollBar && scrollBar.Template.FindName("PART_Track", scrollBar) is Track track)
		{
			if (e.OriginalSource is DependencyObject source)
			{
				DependencyObject parent = source;
				while (parent != null)
				{
					if (parent is Thumb) return;
					parent = VisualTreeHelper.GetParent(parent);
				}
			}

			// Lock mouse focus to the ScrollBar so it cannot misroute the release event
			scrollBar.CaptureMouse();

			Point p = e.GetPosition(track);
			double newValue = track.ValueFromPoint(p);
			if (!double.IsNaN(newValue) && !double.IsInfinity(newValue))
			{
				if (scrollBar.TemplatedParent is ScrollViewer sv)
				{
					sv.ScrollToVerticalOffset(newValue);
				}
				else
				{
					scrollBar.Value = newValue;
				}
				e.Handled = true;
			}
		}
	}
	private static void AnimateScroll(ScrollViewer sv, double ToValue)
	{
		DoubleAnimation scrollAnimation = new()
		{
			From = sv.VerticalOffset,
			To = ToValue,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
		};

		sv.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, scrollAnimation);
	}

	private void ScrollBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is ScrollBar scrollBar)
		{
			if (e.OriginalSource is DependencyObject source)
			{
				DependencyObject parent = source;
				while (parent != null)
				{
					if (parent is Thumb) return;
					parent = VisualTreeHelper.GetParent(parent);
				}
			}

			// Release the lock and consume the event
			if (scrollBar.IsMouseCaptured)
			{
				scrollBar.ReleaseMouseCapture();
				e.Handled = true;
			}
			else if (!e.Handled)
			{
				e.Handled = true;
			}
		}
	}

	private const double ItemHeight = 24.0; // The pixel height of one ComboBoxItem 

	private void DropDownScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is ScrollViewer sv && sv.TemplatedParent is ComboBox cb)
		{
			if (cb.Name == "MinuteBox" || cb.Name == "HourBox")
			{
				e.Handled = true;

				// 1. Get the current actual position and the current attached (animated) position
				double currentActual = sv.VerticalOffset;
				double currentAttached = ScrollViewerBehavior.GetVerticalOffset(sv);

				// 2. Sync Check: If they are far apart, reset to stop the "snap back"
				if (Math.Abs(currentActual - currentAttached) > 1.0)
				{
					sv.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
					ScrollViewerBehavior.SetVerticalOffset(sv, currentActual);
					currentAttached = currentActual;
				}

				// 3. Conditional Speed: Scroll 8 lines for Minutes, 4 lines for Hours
				double linesToScroll = (cb.Name == "MinuteBox") ? 8 : 4;
				double scrollChange = (e.Delta > 0) ? -(ItemHeight * linesToScroll) : (ItemHeight * linesToScroll);

				// 4. Determine Target
				double newTarget = Math.Max(0, Math.Min(sv.ScrollableHeight, currentAttached + scrollChange));

				// 5. Apply the animation
				DoubleAnimation animation = new()
				{
					From = currentAttached,
					To = newTarget,
					Duration = TimeSpan.FromMilliseconds(300),
					EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
				};

				sv.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);

				// 6. Update the attached property immediately for the next tick
				ScrollViewerBehavior.SetVerticalOffset(sv, newTarget);
			}
		}
	}
	public static class ScrollViewerBehavior
	{
		public static readonly DependencyProperty VerticalOffsetProperty =
			DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
				new FrameworkPropertyMetadata(0.0, OnVerticalOffsetChanged));

		public static void SetVerticalOffset(FrameworkElement target, double value) => target.SetValue(VerticalOffsetProperty, value);
		public static double GetVerticalOffset(FrameworkElement target) => (double)target.GetValue(VerticalOffsetProperty);

		private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			if (target is ScrollViewer scrollViewer)
			{
				scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
			}
		}
	}



}