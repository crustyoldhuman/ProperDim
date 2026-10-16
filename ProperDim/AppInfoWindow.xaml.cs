/*
 * Copyright (C) [2026] [Kevin Stanislawski]]
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
namespace ProperDim;

public partial class AppInfoWindow : Window
{
	private bool _isInitializing = true;

	public AppInfoWindow()
	{
		NativeMethods.PrepareWindowForOS(this, "#252525");
		InitializeComponent();
		NativeMethods.ApplyWindows10Shadow(this);

		this.Loaded += (s, e) => ApplySavedSize();

		if (SystemParameters.ClientAreaAnimation)
		{
			this.Opacity = 0;
			this.Loaded += (s, e) =>
			{
				System.Windows.Media.Animation.DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
				this.BeginAnimation(Window.OpacityProperty, anim);
			};
		}

		this.PreviewKeyDown += (s, e) =>
		{
			if (e.Key == Key.Escape)
			{
				this.Close();
				e.Handled = true;
			}
		};
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
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
			e.Handled = true;
		}
		catch (System.Exception ex)
		{
			WarningMessage.Show(this, "Unable to open link: " + ex.Message, "Error", true, "OK");
		}
	}

	private void HelpLink_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ProperDim_README.txt");
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch (System.Exception ex)
		{
			WarningMessage.Show(this, "Unable to open help file: " + ex.Message, "Error", true, "OK");
		}
	}

	private void ApplySavedSize()
	{
		if (ConfigManager.Settings.AppInfoWindowWidth >= this.MinWidth && ConfigManager.Settings.AppInfoWindowHeight >= this.MinHeight)
		{
			double marginOffset = this.Content is FrameworkElement root ? root.Margin.Left + root.Margin.Right : 0;
			double contentWidth = Math.Min(ConfigManager.Settings.AppInfoWindowWidth - marginOffset, 480);
			this.Width = contentWidth + marginOffset;
			this.Height = (contentWidth / (320.0 / 394.0)) + marginOffset;
		}
		_isInitializing = false;
	}

	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_isInitializing) return;

		ConfigManager.Settings.AppInfoWindowWidth = this.ActualWidth;
		ConfigManager.Settings.AppInfoWindowHeight = this.ActualHeight;
	}

	protected override void OnClosed(EventArgs e)
	{
		ConfigManager.Settings.Save();
		base.OnClosed(e);
	}

	private POINTStruct _resizeStartPos;
	private double _resizeStartWidth;
	private double _dpiScaleX = 1.0;
	private double _dpiScaleY = 1.0;

	private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
	{
		NativeMethods.GetCursorPos(out _resizeStartPos);
		_resizeStartWidth = this.ActualWidth;

		this.Width = double.NaN;
		this.Height = double.NaN;

		PresentationSource source = PresentationSource.FromVisual(this);
		if (source != null && source.CompositionTarget != null)
		{
			_dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
			_dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
		}
	}

	private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
	{
		double targetRatio = 320.0 / 394.0;
		double minWidth = 320;
		double maxWidth = 480; // 150% limit
		double marginOffset = this.Content is FrameworkElement root ? root.Margin.Left + root.Margin.Right : 0;

		NativeMethods.GetCursorPos(out POINTStruct currentPos);
		double logicalDeltaX = (currentPos.X - _resizeStartPos.X) / _dpiScaleX;

		double requestedContentWidth = (_resizeStartWidth - marginOffset) + logicalDeltaX;
		double finalContentWidth = Math.Max(minWidth, Math.Min(maxWidth, requestedContentWidth));
		double finalContentHeight = finalContentWidth / targetRatio;

		double finalLogicalWidth = finalContentWidth + marginOffset;
		double finalLogicalHeight = finalContentHeight + marginOffset;

		IntPtr hwnd = new WindowInteropHelper(this).Handle;
		int physWidth = (int)Math.Round(finalLogicalWidth * _dpiScaleX);
		int physHeight = (int)Math.Round(finalLogicalHeight * _dpiScaleY);

		NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, physWidth, physHeight, 0x0016);
	}
}