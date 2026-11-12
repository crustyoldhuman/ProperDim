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
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ProperDim;

public class AppSettings
{
	public bool ShowOnStartup { get; set; } = true;
	public bool CloseToTray { get; set; } = true;
	public bool HotkeysEnabled { get; set; } = false;
	public string IncreaseHotkey { get; set; } = "";
	public string DecreaseHotkey { get; set; } = "";
	public bool ScheduleToggle { get; set; } = false;
	public double ControlPanelLeft { get; set; } = -1;
	public double ControlPanelTop { get; set; } = -1;
	public double ControlPanelWidth { get; set; } = 350;
	public double ControlPanelHeight { get; set; } = 200;
	public double ScheduleDialogWidth { get; set; } = 320;
	public double ScheduleDialogHeight { get; set; } = 280;
	public double AppInfoWindowWidth { get; set; } = 320;
	public double AppInfoWindowHeight { get; set; } = 384;
	public double WarningMessageWidth { get; set; } = 300;
	public double WarningMessageHeight { get; set; } = 150;
	public double MinBrightnessDialogWidth { get; set; } = 300;
	public double MinBrightnessDialogHeight { get; set; } = 260;
	public double LastOpacity { get; set; } = 1.0;
	public List<DimSchedule> SavedSchedules { get; set; } = [];
	public bool UseMilitaryTime { get; set; } = false;
	public string LastScheduleDays { get; set; } = "Mo,Tu,We,Th,Fr,Sa,Su";
	public bool SwapTrayIconClicks { get; set; } = false;
	public double GlobalMinimum { get; set; } = 0.30;
	public void Save()
	{
		ConfigManager.SaveInstance(this);
	}
}

public static class ConfigManager
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
	private static readonly string ConfigFilePath = GetSettingsFilePath();

	private static string GetSettingsFilePath()
	{
		string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProperDim");
		string appDataPath = Path.Combine(appDataFolder, "settings.json");

		if (NativeMethods.IsRunningAsMsix())
		{
			return appDataPath;
		}

		string exePath = Environment.ProcessPath ?? string.Empty;
		string exeFolder = Path.GetDirectoryName(exePath) ?? string.Empty;

		if (!string.IsNullOrEmpty(exeFolder) && !exeFolder.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase) && CanWriteToDirectory(exeFolder))
		{
			return Path.Combine(exeFolder, "settings.json");
		}

		return appDataPath;
	}

	private static bool CanWriteToDirectory(string directoryPath)
	{
		try
		{
			string testFile = Path.Combine(directoryPath, Path.GetRandomFileName());
			File.WriteAllText(testFile, "test");
			File.Delete(testFile);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static AppSettings Settings { get; private set; } = new AppSettings();

	public static void Load()
	{
		try
		{
			if (File.Exists(ConfigFilePath))
			{
				string json = File.ReadAllText(ConfigFilePath);
				Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
			}
		}
		catch (Exception ex1)
		{
			try
			{
				if (File.Exists(ConfigFilePath + ".bak"))
				{
					string bakJson = File.ReadAllText(ConfigFilePath + ".bak");
					Settings = JsonSerializer.Deserialize<AppSettings>(bakJson) ?? new AppSettings();
				}
				else
				{
					Settings = new AppSettings();
					throw new FileNotFoundException("No backup file exists to recover from.");
				}
			}
			catch (Exception ex2)
			{
				Settings = new AppSettings();
				try
				{
					string directory = Path.GetDirectoryName(ConfigFilePath) ?? string.Empty;
					string logPath = Path.Combine(directory, "error.log");
					File.AppendAllText(logPath, $"[{System.DateTime.Now:O}] Settings load failed. Primary: {ex1.Message} | Backup: {ex2.Message}{Environment.NewLine}");
				}
				catch { }
			}
		}
	}

	public static void Save()
	{
		SaveInstance(Settings);
	}

	internal static readonly object _saveLock = new();

	internal static void SaveInstance(AppSettings instance)
	{
		try
		{
			string directory = Path.GetDirectoryName(ConfigFilePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string json;
			lock (_saveLock)
			{
				AppSettings snapshot = new()
				{
					ShowOnStartup = instance.ShowOnStartup,
					CloseToTray = instance.CloseToTray,
					HotkeysEnabled = instance.HotkeysEnabled,
					IncreaseHotkey = instance.IncreaseHotkey,
					DecreaseHotkey = instance.DecreaseHotkey,
					ScheduleToggle = instance.ScheduleToggle,
					ControlPanelLeft = instance.ControlPanelLeft,
					ControlPanelTop = instance.ControlPanelTop,
					ControlPanelWidth = instance.ControlPanelWidth,
					ControlPanelHeight = instance.ControlPanelHeight,
					ScheduleDialogWidth = instance.ScheduleDialogWidth,
					ScheduleDialogHeight = instance.ScheduleDialogHeight,
					AppInfoWindowWidth = instance.AppInfoWindowWidth,
					AppInfoWindowHeight = instance.AppInfoWindowHeight,
					WarningMessageWidth = instance.WarningMessageWidth,
					WarningMessageHeight = instance.WarningMessageHeight,
					MinBrightnessDialogWidth = instance.MinBrightnessDialogWidth,
					MinBrightnessDialogHeight = instance.MinBrightnessDialogHeight,
					LastOpacity = instance.LastOpacity,
					SavedSchedules = [.. instance.SavedSchedules],
					UseMilitaryTime = instance.UseMilitaryTime,
					LastScheduleDays = instance.LastScheduleDays,
					SwapTrayIconClicks = instance.SwapTrayIconClicks,
					GlobalMinimum = instance.GlobalMinimum
				};
				json = JsonSerializer.Serialize(snapshot, _jsonOptions);
			}

			System.Threading.Tasks.Task.Run(() =>
			{
				lock (_saveLock)
				{
					try
					{
						string tempPath = ConfigFilePath + ".tmp";
						File.WriteAllText(tempPath, json);

						if (File.Exists(ConfigFilePath))
						{
							File.Replace(tempPath, ConfigFilePath, ConfigFilePath + ".bak", ignoreMetadataErrors: true);
						}
						else
						{
							File.Move(tempPath, ConfigFilePath);
						}
					}
					catch (Exception ex)
					{
						try
						{
							string logPath = Path.Combine(directory ?? string.Empty, "error.log");
							File.AppendAllText(logPath, $"[{System.DateTime.Now:O}] Save failure: {ex.Message}{Environment.NewLine}");
						}
						catch { }
					}
				}
			});
		}
		catch { }
	}
}