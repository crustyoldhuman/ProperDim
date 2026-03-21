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
		InitializeComponent();
		MessageText.Text = message;
		TitleText.Text = title;

		if (isOkOnly)
		{
			PrimaryButton.Content = customButtonText;
			PrimaryButton.Margin = new Thickness(0);
			SecondaryButton.Visibility = Visibility.Collapsed;
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