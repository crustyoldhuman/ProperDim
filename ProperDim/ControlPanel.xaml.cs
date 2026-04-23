using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ProperDim;

public partial class ControlPanel : Window
{
	private readonly MainWindow _mainWindow;
	private bool _isSyncing = false;
	private bool _isInitializing = true;
	private bool _isDragging = false;
	private bool _isScrolling = false;
	private DispatcherTimer _scrollDebounceTimer;
	private DateTime _lastUserInteraction = DateTime.MinValue;
	private bool _isPreviewActive = false; // Prevents main slider from syncing during preview
	private readonly ObservableCollection<DimSchedule> _displaySchedules = [];

	public ControlPanel(MainWindow mainWindow)
	{
		NativeMethods.PrepareWindowForOS(this, "#2D2D2D");
		InitializeComponent();
		NativeMethods.ApplyWindows10Shadow(this);
		_mainWindow = mainWindow;
		_mainWindow.GlobalBrightnessChanged += OnGlobalBrightnessChanged;
		_mainWindow.ScheduleTriggered += RefreshList;
		_mainWindow.ScheduleEvaluated += RefreshList;
		ScheduleListBox.ItemsSource = _displaySchedules;

		this.IsVisibleChanged += ControlPanel_IsVisibleChanged;

		this.PreviewKeyDown += (s, e) =>
		{
			if (e.Key == Key.Escape)
			{
				HandleClosure();
				e.Handled = true;
			}
		};
	}

	private void ControlPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!this.IsVisible)
		{
			var childWindows = Application.Current.Windows.OfType<Window>()
				.Where(w => w is MinBrightnessDialog || w is ScheduleDialog || w is AppInfoWindow)
				.ToList();

			foreach (var win in childWindows)
			{
				win.Close();
			}
		}
	}

	private async System.Threading.Tasks.Task InitializeFromSettingsAsync()
	{
		if (NativeMethods.IsRunningAsMsix())
		{
			try
			{
				var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("ProperDimStartupId");
				StartupCheckBox.IsChecked = startupTask.State is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
			}
			catch { StartupCheckBox.IsChecked = false; }
		}
		else
		{
			StartupCheckBox.IsChecked = RegistryService.IsStartupEnabled();
		}
		ShowStartupCheckBox.IsChecked = ConfigManager.Settings.ShowOnStartup;
		TrayCheckBox.IsChecked = ConfigManager.Settings.CloseToTray;
		SwapTrayClicksCheckBox.IsChecked = ConfigManager.Settings.SwapTrayIconClicks;
		EnableHotkeysCheckBox.IsChecked = ConfigManager.Settings.HotkeysEnabled;
		IncreaseHotkeyText.Text = ConfigManager.Settings.IncreaseHotkey;
		DecreaseHotkeyText.Text = ConfigManager.Settings.DecreaseHotkey;

		ScheduleCheckbox.IsChecked = ConfigManager.Settings.ScheduleToggle;
		ToggleScheduleUI(ConfigManager.Settings.ScheduleToggle);
	}

	// --- POSITION SAVING LOGIC ---

	private void Window_LocationChanged(object sender, EventArgs e)
	{
		if (_isInitializing) return;
		if (this.WindowState != WindowState.Normal) return;

		ConfigManager.Settings.ControlPanelLeft = this.Left;
		ConfigManager.Settings.ControlPanelTop = this.Top;
		ConfigManager.Settings.Save();
	}

	protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonUp(e);
		ConfigManager.Settings.Save();
	}
	private void OnGlobalBrightnessChanged(double newBrightness)
	{
		// If we are previewing a schedule, do NOT move the main slider.
		if (_isPreviewActive) return;

		// This method is called from MainWindow whenever the global brightness changes.
		// It ensures THIS window's slider is updated to reflect the new value.
		SyncSliderWithOpacity(newBrightness);
	}

	// --- CUSTOM TITLE BAR LOGIC ---

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	private void Minimize_Click(object sender, RoutedEventArgs e)
	{
		this.WindowState = WindowState.Minimized;
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		HandleClosure();
	}

	private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (TrayCheckBox.IsChecked == true)
		{
			e.Cancel = true;
			this.Hide();
			this.ShowInTaskbar = false;

			MainTabs.SelectedIndex = 0;

			var scrollViewer = GetScrollViewer(ScheduleListBox);
			scrollViewer?.ScrollToTop();
		}
	}

	private void HandleClosure()
	{
		if (TrayCheckBox.IsChecked == true)
		{
			this.Hide();
			this.ShowInTaskbar = false;

			MainTabs.SelectedIndex = 0;

			var scrollViewer = GetScrollViewer(ScheduleListBox);
			scrollViewer?.ScrollToTop();
		}
		else
		{
			// 1. Unhook events
			_mainWindow.GlobalBrightnessChanged -= OnGlobalBrightnessChanged;
			_mainWindow.ScheduleTriggered -= RefreshList;
			_mainWindow.ScheduleEvaluated -= RefreshList;

			// 2. Clean up and shut down
			if (_mainWindow != null)
			{
				_mainWindow.ApplyBrightness(1.0);
				_mainWindow.SaveSchedules();
				_mainWindow.TrayIcon?.Dispose();
			}

			ConfigManager.Settings.Save();
			Application.Current.Shutdown();
		}
	}

	// --- BRIGHTNESS HELPERS ---

	public void UpdateDimmerOpacity(double brightness)
	{
		_mainWindow?.ApplyBrightnessAnimated(brightness);
	}
	public void ApplyPreview(double brightness, bool animate = false)
	{
		// Activate preview mode: Apply brightness to screen, but BLOCK slider updates
		_isPreviewActive = true;
		if (animate) _mainWindow.ApplyBrightnessAnimated(brightness, ignoreMinimum: true);
		else _mainWindow.ApplyBrightness(brightness, animate: false, linear: false, durationMs: 200, ignoreMinimum: true);
	}

	public async void EndPreview(bool animate = false)
	{
		// Dynamically pull the current setting so any hotkey adjustments are safely respected
		double restoreBrightness = ConfigManager.Settings.LastOpacity;

		// BUGFIX: Set ignoreMinimum to FALSE so the screen properly respects the floor again
		if (animate) _mainWindow.ApplyBrightnessAnimated(restoreBrightness, ignoreMinimum: false);
		else _mainWindow.ApplyBrightness(restoreBrightness, animate: false, linear: false, durationMs: 200, ignoreMinimum: false);

		// Wait for the screen to finish animating before unfreezing the slider UI
		if (animate) await System.Threading.Tasks.Task.Delay(250);

		_isPreviewActive = false;
		SyncSliderWithOpacity(restoreBrightness);
	}

	public void UpdateDimmerOpacityInstant(double brightness)
	{
		// Calls the new Authority method directly
		_mainWindow.ApplyBrightness(brightness);
		SyncSliderWithOpacity(brightness);
	}

	public void SyncSliderWithOpacity(double brightness)
	{
		if ((DateTime.Now - _lastUserInteraction).TotalMilliseconds < 500) return;

		if (DimmerSlider.IsMouseCaptured || _isScrolling) return;

		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.Invoke(() => SyncSliderWithOpacity(brightness));
			return;
		}

		_isSyncing = true;
		if (Math.Abs(DimmerSlider.Value - brightness) > 0.001)
		{
			DimmerSlider.Value = brightness;
		}
		_isSyncing = false;
	}

	private void RefreshList()
	{
		DateTime now = DateTime.Now;

		static string GetDayAbbrev(DayOfWeek dow) => dow switch
		{
			DayOfWeek.Monday => "Mo",
			DayOfWeek.Tuesday => "Tu",
			DayOfWeek.Wednesday => "We",
			DayOfWeek.Thursday => "Th",
			DayOfWeek.Friday => "Fr",
			DayOfWeek.Saturday => "Sa",
			DayOfWeek.Sunday => "Su",
			_ => ""
		};

		var sortedSchedules = _mainWindow.ActiveSchedules
			.Select(s =>
			{
				DateTime nextOccurrence = DateTime.MaxValue;

				for (int i = 0; i <= 7; i++)
				{
					DateTime checkDate = now.Date.AddDays(i);
					DateTime candidate = checkDate + s.Time;
					string currentDayAbbrev = GetDayAbbrev(checkDate.DayOfWeek);

					if (s.Days.Contains(currentDayAbbrev) && candidate > now)
					{
						nextOccurrence = candidate;
						break;
					}
				}

				return new { Schedule = s, SortDate = nextOccurrence };
			})
			.OrderBy(x => x.SortDate)
			.Select(x => x.Schedule)
			.ToList();

		_displaySchedules.Clear();
		foreach (var s in sortedSchedules)
		{
			_displaySchedules.Add(s);
		}

		NoSchedulesText.Visibility = _mainWindow.ActiveSchedules.Count == 0
			? Visibility.Visible
			: Visibility.Collapsed;
	}

	// --- STARTUP REGISTRY LOGIC (Run with Windows) ---

	private async void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;

		bool shouldRun = StartupCheckBox.IsChecked == true;
		bool success;

		if (NativeMethods.IsRunningAsMsix())
		{
			success = await HandleMsixStartupAsync(shouldRun);
		}
		else
		{
			success = RegistryService.SetStartupRegistry(shouldRun);
		}

		if (!success)
		{
			_isInitializing = true;
			StartupCheckBox.IsChecked = !shouldRun;
			_isInitializing = false;
		}
	}

	private static async System.Threading.Tasks.Task<bool> HandleMsixStartupAsync(bool enable)
	{
		try
		{
			var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("ProperDimStartupId");

			if (enable)
			{
				var state = await startupTask.RequestEnableAsync();
				return state is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
			}
			else
			{
				startupTask.Disable();
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	// --- SHOW ON STARTUP LOGIC ---

	private void ShowStartupCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		ConfigManager.Settings.ShowOnStartup = ShowStartupCheckBox.IsChecked == true;
		ConfigManager.Settings.Save();
	}

	// --- TRAY SETTING LOGIC ---

	private void TrayCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		ConfigManager.Settings.CloseToTray = TrayCheckBox.IsChecked == true;
		ConfigManager.Settings.Save();
	}

	private void SwapTrayClicksCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		ConfigManager.Settings.SwapTrayIconClicks = SwapTrayClicksCheckBox.IsChecked == true;
		ConfigManager.Settings.Save();
	}

	// --- SCHEDULE EVENT HANDLERS ---

	private void AddSchedule_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ScheduleDialog dialog = new([.. _mainWindow.ActiveSchedules])
			{
				Owner = this
			};
			if (dialog.ShowDialog() == true)
			{
				_mainWindow.ActiveSchedules.Add(dialog.Result);
				_mainWindow.SaveSchedules();
				RefreshList();
			}
		}
		finally
		{
			EndPreview(false);
		}
	}

	private void DeleteSchedule_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.DataContext is DimSchedule itemToDelete)
		{
			_mainWindow.ActiveSchedules.Remove(itemToDelete);
			_mainWindow.SaveSchedules();
			RefreshList();
		}
	}

	private void ScheduleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		EditSelectedSchedule();
	}

	private void ScheduleList_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter || e.Key == Key.Space)
		{
			// Allow the Delete button to natively handle the keystroke if it has focus
			if (e.OriginalSource is System.Windows.Controls.Button) return;

			EditSelectedSchedule();
			e.Handled = true;
		}
	}

	private void EditSelectedSchedule()
	{
		if (ScheduleListBox.SelectedItem is DimSchedule selectedSchedule)
		{
			try
			{
				ScheduleDialog dialog = new(selectedSchedule, [.. _mainWindow.ActiveSchedules])
				{
					Owner = this
				};
				if (dialog.ShowDialog() == true)
				{
					int index = _mainWindow.ActiveSchedules.IndexOf(selectedSchedule);
					if (index != -1)
					{
						_mainWindow.ActiveSchedules[index] = dialog.Result;
						_mainWindow.SaveSchedules();
					}
					RefreshList();
				}
			}
			finally
			{
				EndPreview(false);
			}
		}
	}

	// --- UI HANDLERS ---

	private double _targetScrollOffset = 0;

	private void ScheduleList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		var scrollViewer = GetScrollViewer(ScheduleListBox);
		if (scrollViewer == null) return;

		// 1. Determine the height of exactly one line/item
		double itemHeight = 25; // Fallback height
		if (ScheduleListBox.Items.Count > 0)
		{
			if (ScheduleListBox.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement container)
			{
				itemHeight = container.ActualHeight;
			}
		}

		// 2. Sync target with current scroll position 
		// This prevents the "jump" if you manually dragged the scrollbar between wheel spins
		if (Math.Abs(_targetScrollOffset - scrollViewer.VerticalOffset) > 1.0)
		{
			_targetScrollOffset = scrollViewer.VerticalOffset;
		}

		// 3. Snap current position to the nearest line to prevent cumulative "drift"
		_targetScrollOffset = Math.Round(_targetScrollOffset / itemHeight) * itemHeight;

		// 4. Calculate new target: Snap to multiple lines per notch
		double linesPerNotch = 1;
		if (e.Delta > 0)
			_targetScrollOffset -= (itemHeight * linesPerNotch);
		else
			_targetScrollOffset += (itemHeight * linesPerNotch);

		// 5. Clamp the target within the list bounds
		_targetScrollOffset = Math.Max(0, Math.Min(_targetScrollOffset, scrollViewer.ScrollableHeight));

		// 6. Smoothly animate to the exact line position
		DoubleAnimation scrollAnim = new()
		{
			To = _targetScrollOffset,
			Duration = TimeSpan.FromMilliseconds(100),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
		};

		_isScrolling = true;
		scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, scrollAnim);

		// Reset debounce timer for UI state management
		_scrollDebounceTimer?.Stop();
		_scrollDebounceTimer?.Start();

		e.Handled = true;
	}

	private static ScrollViewer GetScrollViewer(DependencyObject depObj)
	{
		if (depObj is ScrollViewer sv) return sv;

		for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
		{
			var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
			var result = GetScrollViewer(child);
			if (result != null) return result;
		}
		return null;
	}
	private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (!_isSyncing && !_isInitializing)
		{
			_lastUserInteraction = DateTime.Now;

			double newGlobal = e.NewValue;
			ConfigManager.Settings.LastOpacity = newGlobal;

			if (_isDragging)
			{
				_mainWindow.ApplyBrightness(newGlobal);
			}
			else
			{
				_mainWindow.ApplyBrightnessAnimated(newGlobal);
				_mainWindow.TriggerSave();
			}
		}
	}

	private bool _isScheduleScrollBarDragging = false;

	private void ScheduleScrollBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		// If the user clicked the Thumb itself, ignore and let standard thumb dragging handle it
		if (e.OriginalSource is FrameworkElement fe && (fe is Thumb || fe.TemplatedParent is Thumb))
		{
			return;
		}

		if (sender is ScrollBar scrollBar && scrollBar.Orientation == Orientation.Vertical)
		{
			scrollBar.CaptureMouse();
			_isScheduleScrollBarDragging = true;
			UpdateScheduleScrollFromMouse(scrollBar, e);
			e.Handled = true; // Prevents the default PageUp/PageDown jump
		}
	}

	private void ScheduleScrollBar_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isScheduleScrollBarDragging && sender is ScrollBar scrollBar && scrollBar.IsMouseCaptured)
		{
			UpdateScheduleScrollFromMouse(scrollBar, e);
		}
	}

	private void ScheduleScrollBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isScheduleScrollBarDragging && sender is ScrollBar scrollBar)
		{
			scrollBar.ReleaseMouseCapture();
			_isScheduleScrollBarDragging = false;
			e.Handled = true;
		}
	}

	private void UpdateScheduleScrollFromMouse(ScrollBar scrollBar, MouseEventArgs e)
	{
		if (scrollBar.Template.FindName("PART_Track", scrollBar) is Track track)
		{
			double value = track.ValueFromPoint(e.GetPosition(track));
			if (!double.IsNaN(value))
			{
				var scrollViewer = GetScrollViewer(ScheduleListBox);
				if (scrollViewer != null)
				{
					// Explicitly destroy any holding animations to prevent snapping
					scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);

					// Scroll the viewer directly
					scrollViewer.ScrollToVerticalOffset(value);

					// Keep the smooth scroll attached property in sync
					ScrollViewerBehavior.SetVerticalOffset(scrollViewer, value);
				}
			}
		}
	}

	private void QuickSet_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag != null)
		{
			double val = Convert.ToDouble(btn.Tag);
			DimmerSlider.Value = val;
		}
	}

	private void ClearSchedule_Click(object sender, RoutedEventArgs e)
	{
		var result = WarningMessage.Show(this, "You are about to completely wipe all of your scheduled events. Is this what you truly desire?", "Clear Schedule");
		if (result == MessageBoxResult.Yes)
		{
			_mainWindow.ActiveSchedules.Clear();
			_mainWindow.SaveSchedules();
			RefreshList();
		}
	}

	private void ResetBrightness_Click(object sender, RoutedEventArgs e)
	{
		var result = WarningMessage.Show(this, "You are about to reset your brightness settings. Your schedule will remain intact. Is this what you truly desire?", "Reset Brightness");

		if (result == MessageBoxResult.Yes)
		{
			// Trigger the hardware gamma and magnification reset
			_mainWindow.HardResetDisplays();

			// Ensure the main slider UI snaps to 100% to match the reset state
			SyncSliderWithOpacity(1.0);
		}
	}
	private void DimmerSlider_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		// Flag that we are scrolling so SyncSliderWithOpacity knows to back off
		_isScrolling = true;
		_scrollDebounceTimer?.Stop();
		_scrollDebounceTimer?.Start();

		int currentPercent = (int)Math.Round(DimmerSlider.Value * 100);
		int newPercent;

		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			if (e.Delta > 0) newPercent = currentPercent + 1;
			else newPercent = currentPercent - 1;
		}
		else
		{
			if (e.Delta > 0)
			{
				newPercent = ((currentPercent + 4) / 5) * 5;
				if (newPercent == currentPercent) newPercent += 5;
			}
			else
			{
				newPercent = ((currentPercent - 1) / 5) * 5;
			}
		}

		int minPercent = (int)Math.Round(DimmerSlider.Minimum * 100);
		int maxPercent = (int)Math.Round(DimmerSlider.Maximum * 100);

		newPercent = Math.Max(minPercent, Math.Min(maxPercent, newPercent));

		DimmerSlider.Value = newPercent / 100.0;
		e.Handled = true;
	}

	private void MainTabs_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Only intercept keys if the focus is actually resting on the Tab Header itself
        if (e.OriginalSource is TabItem)
        {
            if (e.Key == Key.Left)
            {
                int newIndex = MainTabs.SelectedIndex - 1;
                if (newIndex < 0) newIndex = MainTabs.Items.Count - 1; // Wrap around
                
                MainTabs.SelectedIndex = newIndex;

                // Force the gray highlight to follow you to the new tab header
                if (MainTabs.ItemContainerGenerator.ContainerFromIndex(newIndex) is TabItem newTab)
                {
                    newTab.Focus();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                int newIndex = MainTabs.SelectedIndex + 1;
                if (newIndex >= MainTabs.Items.Count) newIndex = 0; // Wrap around
                
                MainTabs.SelectedIndex = newIndex;

                if (MainTabs.ItemContainerGenerator.ContainerFromIndex(newIndex) is TabItem newTab)
                {
                    newTab.Focus();
                }
                e.Handled = true;
            }
        }
    }

	private void TabsArea_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (e.Handled) return;

		// Check if the mouse is hovering within the top 32 pixels (the tab header area)
		Point pos = e.GetPosition((UIElement)sender);
		if (pos.Y > 32) return;

		int currentIndex = MainTabs.SelectedIndex;

		if (e.Delta > 0) // Scroll Up -> Next Tab Right
		{
			currentIndex++;
			if (currentIndex >= MainTabs.Items.Count) currentIndex = 0; // Wrap around to first
		}
		else // Scroll Down -> Previous Tab Left
		{
			currentIndex--;
			if (currentIndex < 0) currentIndex = MainTabs.Items.Count - 1; // Wrap around to last
		}

		MainTabs.SelectedIndex = currentIndex;
		e.Handled = true;
	}

	// --- APP INFO LOGIC ---

	private void AppInfo_Click(object sender, RoutedEventArgs e)
	{
		AppInfoWindow infoWin = new()
		{
			Owner = this,
			WindowStartupLocation = WindowStartupLocation.CenterOwner
		};
		infoWin.ShowDialog();
	}

	private void MinBrightnessLink_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			MinBrightnessDialog dialog = new()
			{
				Owner = this,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			dialog.ShowDialog();
		}
		finally
		{
			// Unfreeze the main UI slider and ensure brightness is restored safely 
			// incase hotkeys were used while the dialog was open.
			EndPreview(false);
		}
	}

	private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		e.Handled = true;
		if (sender is not TextBox tb) return;

		ModifierKeys modifiers = Keyboard.Modifiers;
		List<string> keys = [];
		if (modifiers.HasFlag(ModifierKeys.Control)) keys.Add("Ctrl");
		if (modifiers.HasFlag(ModifierKeys.Alt)) keys.Add("Alt");
		if (modifiers.HasFlag(ModifierKeys.Shift)) keys.Add("Shift");
		if (modifiers.HasFlag(ModifierKeys.Windows)) keys.Add("Win");

		Key key = e.Key == Key.System ? e.SystemKey : e.Key;

		if (key != Key.LeftCtrl && key != Key.RightCtrl &&
			key != Key.LeftAlt && key != Key.RightAlt &&
			key != Key.LeftShift && key != Key.RightShift &&
			key != Key.LWin && key != Key.RWin &&
			key != Key.None)
		{
			keys.Add(key.ToString());
		}

		string hotkeyString = string.Join(" + ", keys);

		if (!string.IsNullOrEmpty(hotkeyString))
		{
			tb.Text = hotkeyString;

			if (tb.Name == "IncreaseHotkeyText")
				ConfigManager.Settings.IncreaseHotkey = hotkeyString;
			else if (tb.Name == "DecreaseHotkeyText")
				ConfigManager.Settings.DecreaseHotkey = hotkeyString;

			ConfigManager.Settings.Save();
			_mainWindow?.RegisterGlobalHotkeys();
		}
	}

	private void EnableHotkeysCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		bool isEnabled = (sender as CheckBox)?.IsChecked ?? false;
		ConfigManager.Settings.HotkeysEnabled = isEnabled;
		ConfigManager.Settings.Save();
		_mainWindow?.RegisterGlobalHotkeys();
	}

	private void HotkeysTab_MouseDown(object sender, MouseButtonEventArgs e)
	{
		// 1. Check if the click target is actually the TextBox
		if (IsClickOnTextBox(e.OriginalSource as DependencyObject))
			return;

		// 2. Force focus to the background Grid (sender).
		// Since we set Focusable="True" in XAML, this will succeed, 
		// stealing focus from the TextBox and removing the blue border.
		(sender as UIElement)?.Focus();
	}

	private static bool IsClickOnTextBox(DependencyObject clickedObj)
	{
		// Walk up the visual tree to see if the clicked element belongs to a TextBox
		while (clickedObj != null)
		{
			if (clickedObj is TextBox) return true;
			clickedObj = System.Windows.Media.VisualTreeHelper.GetParent(clickedObj);
		}
		return false;
	}

	// --- WINDOW LIFECYCLE ---

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		_scrollDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
		_scrollDebounceTimer.Tick += (s, args) =>
		{
			_isScrolling = false;
			_scrollDebounceTimer.Stop();
		};
		_isInitializing = true;
		// --- Restore window position ---
		double left = ConfigManager.Settings.ControlPanelLeft;
		double top = ConfigManager.Settings.ControlPanelTop;

		if (left >= 0 && top >= 0 &&
			left < SystemParameters.VirtualScreenWidth - 50 &&
			top < SystemParameters.VirtualScreenHeight - 50)
		{
			this.Left = left;
			this.Top = top;
		}
		else
		{
			this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		// --- Restore last dimmer brightness ---
		DimmerSlider.Value = ConfigManager.Settings.LastOpacity;
		if (_mainWindow != null)
		{
			// Use Authority method on load
			_mainWindow.ApplyBrightness(ConfigManager.Settings.LastOpacity);
			_mainWindow.LoadSchedules();
		}

		// --- Sync checkboxes and hotkeys ---
		await InitializeFromSettingsAsync();

		// SELF-REPAIR: Enforce the registry state matches the user's setting immediately.
		// This ensures the Registry always points to THIS executable (if true), 
		// or removes the key (if false), killing any "Zombie" references to old versions.
		if (!NativeMethods.IsRunningAsMsix())
		{
			RegistryService.SetStartupRegistry(StartupCheckBox.IsChecked == true);
		}

		// --- Apply default tab selection ---
		MainTabs.SelectedIndex = 0;
		_isInitializing = false;

		// --- Register hotkeys and refresh list ---
		_mainWindow?.RegisterGlobalHotkeys();
		RefreshList();
	}

	public void ShowControlPanel()
	{
		// 1. MUST be set BEFORE showing to prevent WPF from silently destroying and rebuilding the window handle!
		this.ShowInTaskbar = true;

		if (!this.IsVisible)
		{
			if (SystemParameters.ClientAreaAnimation)
			{
				this.Opacity = 0;
				this.Show();
				ApplyThemeShadow();
				DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
				this.BeginAnimation(Window.OpacityProperty, anim);
			}
			else
			{
				this.Opacity = 1.0;
				this.Show();
				ApplyThemeShadow();
			}
		}

		this.WindowState = WindowState.Normal;

		// 2. The proper sequence to steal focus from the tray without flashing:
		this.Activate();
		this.Topmost = true;
		this.Topmost = false;
		this.Focus();

		// tab resetting logic
		Dispatcher.BeginInvoke(new Action(() =>
		{
			MainTabs.SelectedIndex = 0;
		}), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
	}

	private void AppInfoLink_Click(object sender, RoutedEventArgs e)
	{
		AppInfo_Click(sender, e);
	}

	public void ApplyThemeShadow()
	{
		System.IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
		if (hwnd == IntPtr.Zero) return;

		int darkMode = 0;
		if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int isLight && isLight == 0)
		{
			darkMode = 1;
		}

		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

		// Force DWM to recalculate the frame to clear the WPF Hide/Show ghost shadow bug
		// 0x0020 = SWP_FRAMECHANGED, 0x0002 = SWP_NOMOVE, 0x0001 = SWP_NOSIZE, 0x0004 = SWP_NOZORDER
		NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0020 | 0x0002 | 0x0001 | 0x0004);
	}

	protected override void OnSourceInitialized(System.EventArgs e)
	{
		base.OnSourceInitialized(e);
		System.IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

		int preference = NativeMethods.DWMWCP_ROUND;
		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

		ApplyThemeShadow();
		System.Windows.Interop.HwndSource.FromHwnd(hwnd)?.AddHook(HwndHook);
	}

	private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		// 0x001A is the OS broadcast for WM_SETTINGCHANGE
		if (msg == 0x001A)
		{
			ApplyThemeShadow();
		}
		return IntPtr.Zero;
	}
	// Override OnActivated to auto-dim this window whenever it gains focus
	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		RefreshList();
	}
	private void ScheduleCheckbox_Checked(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		ConfigManager.Settings.ScheduleToggle = true;
		ConfigManager.Settings.Save();
		ToggleScheduleUI(true);
	}

	private void ScheduleCheckbox_Unchecked(object sender, RoutedEventArgs e)
	{
		if (_isInitializing) return;
		ConfigManager.Settings.ScheduleToggle = false;
		ConfigManager.Settings.Save();
		ToggleScheduleUI(false);
	}

	private void ToggleScheduleUI(bool isEnabled)
	{
		// Dim and disable the "+" button
		// Use IsHitTestVisible instead of IsEnabled.
		// This prevents the default "Disabled" style (white borders) from appearing.
		AddScheduleButton.IsHitTestVisible = isEnabled;
		AddScheduleButton.Opacity = isEnabled ? 1.0 : 0.4;

		// Dim and disable the list of events
		// Same here. Prevents interaction but keeps original colors.
		ScheduleListBox.IsHitTestVisible = isEnabled;
		ScheduleListBox.Opacity = isEnabled ? 1.0 : 0.4;

		// Handle the "No Schedules" text visibility
		NoSchedulesText.Opacity = isEnabled ? 1.0 : 0.4;
	}

	// --- CUSTOM CLICK-AND-DRAG SLIDER LOGIC ---

	private void DimmerSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is Slider slider)
		{
			slider.CaptureMouse();
			UpdateSliderFromMouse(slider, e);
			_isDragging = true;
		}
	}

	private void DimmerSlider_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_isDragging && sender is Slider slider && slider.IsMouseCaptured)
		{
			UpdateSliderFromMouse(slider, e);
		}
	}

	private void DimmerSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_isDragging && sender is Slider slider)
		{
			slider.ReleaseMouseCapture();
			_isDragging = false;
			_mainWindow?.TriggerSave();
		}
	}

	private static void UpdateSliderFromMouse(Slider slider, MouseEventArgs e)
	{
		if (slider.Template.FindName("PART_Track", slider) is Track track)
		{
			double newValue = track.ValueFromPoint(e.GetPosition(track));
			if (!double.IsNaN(newValue))
			{
				slider.Value = newValue;
			}
		}
	}
	public static class ScrollViewerBehavior
	{
		public static readonly DependencyProperty VerticalOffsetProperty =
			DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
				new PropertyMetadata(0.0, OnVerticalOffsetChanged));

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