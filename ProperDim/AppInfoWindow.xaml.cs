using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace ProperDim;

public partial class AppInfoWindow : Window
{
	public AppInfoWindow()
	{
		NativeMethods.PrepareWindowForOS(this, "#252525");
		InitializeComponent();
		NativeMethods.ApplyWindows10Shadow(this);

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
}