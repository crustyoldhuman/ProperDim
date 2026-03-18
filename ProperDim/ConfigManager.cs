using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ProperDim;

public class AppSettings
{
	public bool ShowOnStartup { get; set; } = false;
	public bool CloseToTray { get; set; } = false;
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

	public void Save()
	{
		ConfigManager.SaveInstance(this);
	}
}

public static class ConfigManager
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
	private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProperDim");
	private static readonly string ConfigFilePath = Path.Combine(ConfigFolder, "settings.json");

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
			Directory.CreateDirectory(ConfigFolder);
			string json = JsonSerializer.Serialize(instance, _jsonOptions);
			File.WriteAllText(ConfigFilePath, json);
		}
		catch { }
	}
}