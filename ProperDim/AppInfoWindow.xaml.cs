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

		this.Opacity = 0;
		this.Loaded += (s, e) =>
		{
			ApplySavedSize();
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
			MessageBox.Show("Unable to open link: " + ex.Message);
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
			MessageBox.Show("Unable to open help file: " + ex.Message);
		}
	}

	private void ApplySavedSize()
	{
		if (ConfigManager.Settings.AppInfoWindowWidth >= this.MinWidth && ConfigManager.Settings.AppInfoWindowHeight >= this.MinHeight)
		{
			this.Width = Math.Min(ConfigManager.Settings.AppInfoWindowWidth, 480);
			this.Height = Math.Min(ConfigManager.Settings.AppInfoWindowHeight, 576);
		}
		_isInitializing = false;
	}

	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_isInitializing) return;

		ConfigManager.Settings.AppInfoWindowWidth = this.Width;
		ConfigManager.Settings.AppInfoWindowHeight = this.Height;
		ConfigManager.Settings.Save();
	}

	private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
	{
		double targetRatio = 320.0 / 384.0;
		double minWidth = 320;
		double maxWidth = 480; // 150% limit

		double newWidth = this.Width + e.HorizontalChange;
		double finalWidth = Math.Max(minWidth, Math.Min(maxWidth, newWidth));

		this.Width = finalWidth;
		this.Height = finalWidth / targetRatio;
	}
}