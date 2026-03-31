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
}