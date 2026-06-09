/*
 * Copyright 2026 Kevin Stanislawski
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * As an exception, this software is subject to the Commons Clause License Condition v1.0.
 * You may not Sell the Software. For the full text of the Commons Clause, see the LICENSE file.
 */

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ProperDim;

public partial class WarningMessage : Window
{
	public MessageBoxResult Result { get; private set; } = MessageBoxResult.No;

	private WarningMessage(string message, string title, bool isOkOnly, string customButtonText)
	{
		NativeMethods.PrepareWindowForOS(this, "#2D2D2D");
		InitializeComponent();
		NativeMethods.ApplyWindows10Shadow(this);
		MessageText.Text = message;
		TitleText.Text = title;

		if (isOkOnly)
		{
			// Hide the red destructive button
			PrimaryButton.Visibility = Visibility.Collapsed;

			// Repurpose the gray secondary button as the main action
			SecondaryButton.Content = customButtonText;
			SecondaryButton.Visibility = Visibility.Visible;

			// Rewire the click event to return 'Yes' instead of 'No'
			SecondaryButton.Click -= No_Click;
			SecondaryButton.Click += Yes_Click;
		}

		this.Opacity = 0;
		this.Loaded += (s, e) =>
		{
			System.Windows.Media.Animation.DoubleAnimation anim = new(0.0, 1.0, TimeSpan.FromMilliseconds(100));
			this.BeginAnimation(Window.OpacityProperty, anim);
		};
	}

	public static MessageBoxResult Show(Window owner, string message, string title, bool isOkOnly = false, string customButtonText = "OK")
	{
		WarningMessage msgBox = new(message, title, isOkOnly, customButtonText)
		{
			Owner = owner
		};
		msgBox.ShowDialog();
		return msgBox.Result;
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			this.DragMove();
		}
	}

	private void Yes_Click(object sender, RoutedEventArgs e)
	{
		Result = MessageBoxResult.Yes;
		this.Close();
	}

	private void No_Click(object sender, RoutedEventArgs e)
	{
		Result = MessageBoxResult.No;
		this.Close();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		IntPtr hwnd = new WindowInteropHelper(this).Handle;

		int preference = NativeMethods.DWMWCP_ROUND;
		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

		int darkMode = 0;
		if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int isLight && isLight == 0)
		{
			darkMode = 1;
		}

		_ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
	}
}