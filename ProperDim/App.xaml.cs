using System;
using System.Threading;
using System.Windows;

namespace ProperDim;

public partial class App : Application
{
	private static Mutex _mutex;
	private MainWindow _mainWindow;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		EventManager.RegisterClassHandler(typeof(System.Windows.Controls.CheckBox), System.Windows.UIElement.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(GlobalCheckBox_PreviewKeyDown));
		EventManager.RegisterClassHandler(typeof(System.Windows.Documents.Hyperlink), System.Windows.ContentElement.PreviewKeyDownEvent, new System.Windows.Input.KeyEventHandler(GlobalHyperlink_PreviewKeyDown));

		AppDomain.CurrentDomain.UnhandledException += (s, args) => EmergencyReset();
		this.DispatcherUnhandledException += (s, args) =>
		{
			if (args.Exception is InvalidOperationException && args.Exception.Message.Contains("UpdateToolTip"))
			{
				args.Handled = true;
				return;
			}
			EmergencyReset();
		};

		try
		{
			// 1. Singleton Check
			_mutex = new(true, "ProperDim_Unique_Mutex", out bool createdNew);
			if (!createdNew)
			{
				Environment.Exit(0);
				return;
			}

			// 2. Load Settings
			ConfigManager.Load();

			// 3. Initialize the Brain (Invisible Controller)
			_mainWindow = new MainWindow();
			_mainWindow.Show();

			// 4. Show the UI if configured
			if (ConfigManager.Settings.ShowOnStartup)
			{
				ControlPanel controlPanel = new(_mainWindow);
				controlPanel.ShowControlPanel();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Startup Error: {ex.Message}");
			Application.Current.Shutdown();
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		if (_mutex != null)
		{
			try { _mutex.ReleaseMutex(); } catch { }
			_mutex.Dispose();
		}
		base.OnExit(e);
	}

	private void EmergencyReset()
	{
		try
		{
			_mainWindow?.ShutdownApp();
		}
		catch { }
	}

	private void GlobalCheckBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Enter && sender is System.Windows.Controls.CheckBox checkBox)
		{
			checkBox.IsChecked = !checkBox.IsChecked;
			e.Handled = true;
		}
	}

	private void GlobalHyperlink_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if ((e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Space) && sender is System.Windows.Documents.Hyperlink hyperlink)
		{
			hyperlink.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Documents.Hyperlink.ClickEvent));
			e.Handled = true;
		}
	}
}