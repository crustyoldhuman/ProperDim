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

		if (!string.IsNullOrEmpty(exeFolder) && CanWriteToDirectory(exeFolder))
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
		catch
		{
			Settings = new AppSettings();
		}
	}

	public static void Save()
	{
		SaveInstance(Settings);
	}

	internal static void SaveInstance(AppSettings instance)
	{
		try
		{
			string directory = Path.GetDirectoryName(ConfigFilePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string json = JsonSerializer.Serialize(instance, _jsonOptions);
			File.WriteAllText(ConfigFilePath, json);
		}
		catch { }
	}
}