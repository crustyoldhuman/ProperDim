using System;
using System.Windows;
using Microsoft.Win32;

namespace ProperDim;

public static class RegistryService
{
	private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "ProperDim";

	public static bool IsStartupEnabled()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
			return key?.GetValue(AppName) != null;
		}
		catch
		{
			return false;
		}
	}

	public static bool SetStartupRegistry(bool enable)
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
			if (enable)
			{
				string path = Environment.ProcessPath;
				key.SetValue(AppName, $"\"{path}\"");
			}
			else
			{
				key.DeleteValue(AppName, false);
			}

			return true;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Failed to update startup settings: " + ex.Message);
			return false;
		}
	}
}