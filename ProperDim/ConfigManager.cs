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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
		string tempPath = ConfigFilePath + ".tmp";
		if (File.Exists(tempPath))
		{
			try { File.Delete(tempPath); } catch { }
		}

		string bakPath = ConfigFilePath + ".bak";
		string primaryError = null;
		bool primaryExists = File.Exists(ConfigFilePath);

		if (primaryExists)
		{
			if (TryLoadAndSanitize(ConfigFilePath, out AppSettings loadedPrimary, out string primaryJson, out primaryError))
			{
				Settings = loadedPrimary;
				lock (_ioLock)
				{
					_lastWrittenJson = primaryJson;
				}
				return;
			}
		}

		if (File.Exists(bakPath))
		{
			if (TryLoadAndSanitize(bakPath, out AppSettings loadedBackup, out _, out string backupError))
			{
				Settings = loadedBackup;
				if (primaryExists)
				{
					QuarantineCorruptPrimary();
					LogError($"Primary settings corrupt ({primaryError}). Recovered from backup.");
				}
				else
				{
					LogError("Primary settings missing. Recovered from backup.");
				}

				PromoteRecoveredSettings(Settings);
				return;
			}

			if (primaryExists)
			{
				QuarantineCorruptPrimary();
				LogError($"Settings load failed. Primary: {primaryError} | Backup: {backupError}");
			}
			else
			{
				LogError($"Settings load failed. Primary missing | Backup: {backupError}");
			}
		}
		else if (primaryExists)
		{
			QuarantineCorruptPrimary();
			LogError($"Settings load failed. Primary: {primaryError} | Backup: No backup file exists.");
		}

		lock (_ioLock)
		{
			_lastWrittenJson = null;
		}
		Settings = new AppSettings();
	}

	private static bool TryLoadAndSanitize(string filePath, out AppSettings settings, out string loadedJson, out string error)
	{
		settings = null;
		loadedJson = null;
		error = null;

		for (int attempt = 0; attempt < MaxWriteAttempts; attempt++)
		{
			try
			{
				string json = File.ReadAllText(filePath);
				if (string.IsNullOrWhiteSpace(json))
				{
					error = "File is empty or whitespace.";
					return false;
				}

				AppSettings deserialized = JsonSerializer.Deserialize<AppSettings>(json);
				if (deserialized == null)
				{
					error = "Deserialized settings object was null.";
					return false;
				}

				SanitizeSettings(deserialized);
				settings = deserialized;
				loadedJson = json;
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				error = ex.Message;
				if (attempt < MaxWriteAttempts - 1)
				{
					Thread.Sleep(RetryDelaysMs[attempt]);
				}
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		return false;
	}

	private static void SanitizeSettings(AppSettings s)
	{
		s.IncreaseHotkey ??= string.Empty;
		s.DecreaseHotkey ??= string.Empty;
		s.LastScheduleDays = string.IsNullOrWhiteSpace(s.LastScheduleDays)
			? "Mo,Tu,We,Th,Fr,Sa,Su"
			: s.LastScheduleDays;

		s.ControlPanelLeft = SanitizeFinite(s.ControlPanelLeft, -1);
		s.ControlPanelTop = SanitizeFinite(s.ControlPanelTop, -1);
		s.ControlPanelWidth = SanitizePositive(s.ControlPanelWidth, 350);
		s.ControlPanelHeight = SanitizePositive(s.ControlPanelHeight, 200);
		s.ScheduleDialogWidth = SanitizePositive(s.ScheduleDialogWidth, 320);
		s.ScheduleDialogHeight = SanitizePositive(s.ScheduleDialogHeight, 280);
		s.AppInfoWindowWidth = SanitizePositive(s.AppInfoWindowWidth, 320);
		s.AppInfoWindowHeight = SanitizePositive(s.AppInfoWindowHeight, 384);
		s.WarningMessageWidth = SanitizePositive(s.WarningMessageWidth, 300);
		s.WarningMessageHeight = SanitizePositive(s.WarningMessageHeight, 150);
		s.MinBrightnessDialogWidth = SanitizePositive(s.MinBrightnessDialogWidth, 300);
		s.MinBrightnessDialogHeight = SanitizePositive(s.MinBrightnessDialogHeight, 260);

		s.LastOpacity = SanitizeRange(s.LastOpacity, 0.01, 1.0, 1.0);
		s.GlobalMinimum = SanitizeRange(s.GlobalMinimum, 0.00, 0.99, 0.30);

		if (s.SavedSchedules == null)
		{
			s.SavedSchedules = [];
		}
		else
		{
			s.SavedSchedules.RemoveAll(item => item == null);
			foreach (DimSchedule item in s.SavedSchedules)
			{
				item.Days ??= string.Empty;
				item.Brightness = SanitizeRange(item.Brightness, 0.01, 1.0, 1.0);
				if (item.Time < TimeSpan.Zero || item.Time >= TimeSpan.FromDays(1))
				{
					item.Time = TimeSpan.Zero;
				}
			}
		}
	}

	private static double SanitizeFinite(double value, double fallback)
	{
		return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
	}

	private static double SanitizePositive(double value, double fallback)
	{
		return double.IsNaN(value) || double.IsInfinity(value) || value <= 0 ? fallback : value;
	}

	private static double SanitizeRange(double value, double min, double max, double fallback)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return fallback;
		}
		return Math.Clamp(value, min, max);
	}

	private static void QuarantineCorruptPrimary()
	{
		try
		{
			if (!File.Exists(ConfigFilePath)) return;
			File.Move(ConfigFilePath, ConfigFilePath + ".corrupt", overwrite: true);
		}
		catch (Exception ex)
		{
			LogError($"Failed to quarantine corrupt settings file: {ex.Message}");
		}
	}

	private static void PromoteRecoveredSettings(AppSettings recovered)
	{
		try
		{
			string json = Serialize(recovered);
			lock (_ioLock)
			{
				WriteToDiskCore(json, updateBackup: false);
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to promote recovered backup to primary settings: {ex.Message}");
		}
	}

	public static void Save()
	{
		SaveInstance(Settings);
	}

	internal static readonly object _saveLock = new();
	private static readonly object _queueLock = new();
	private static readonly object _ioLock = new();
	private static readonly object _logLock = new();

	private static string _pendingJson = null;
	private static long _revisionCounter = 0;
	private static long _pendingRevision = 0;
	private static long _lastWrittenRevision = 0;
	private static bool _isWriterRunning = false;
	private static string _lastWrittenJson = null;

	private const int MaxWriteAttempts = 4;
	private static readonly int[] RetryDelaysMs = [30, 80, 200];
	private const long MaxErrorLogBytes = 256 * 1024;

	private static string Serialize(AppSettings instance)
	{
		lock (_saveLock)
		{
			return JsonSerializer.Serialize(instance, _jsonOptions);
		}
	}

	internal static void SaveInstance(AppSettings instance)
	{
		try
		{
			string json = Serialize(instance);

			lock (_queueLock)
			{
				_pendingRevision = ++_revisionCounter;
				_pendingJson = json;

				if (!_isWriterRunning)
				{
					_isWriterRunning = true;
					_ = Task.Run(ProcessSaveQueue);
				}
			}
		}
		catch (Exception ex)
		{
			LogError($"Save snapshot failure: {ex.Message}");
		}
	}

	public static void FlushSync()
	{
		try
		{
			string json = Serialize(Settings);
			long revision;

			lock (_queueLock)
			{
				revision = ++_revisionCounter;
				_pendingJson = null;
			}

			lock (_ioLock)
			{
				if (WriteToDiskCore(json))
				{
					_lastWrittenRevision = Math.Max(_lastWrittenRevision, revision);
				}
			}
		}
		catch (Exception ex)
		{
			LogError($"FlushSync failure: {ex.Message}");
		}
	}

	private static void ProcessSaveQueue()
	{
		try
		{
			while (true)
			{
				string jsonToWrite;
				long revisionToWrite;

				lock (_queueLock)
				{
					if (_pendingJson == null)
					{
						_isWriterRunning = false;
						return;
					}

					jsonToWrite = _pendingJson;
					revisionToWrite = _pendingRevision;
					_pendingJson = null;
				}

				lock (_ioLock)
				{
					if (revisionToWrite <= _lastWrittenRevision)
					{
						continue;
					}

					if (WriteToDiskCore(jsonToWrite))
					{
						_lastWrittenRevision = revisionToWrite;
					}
				}
			}
		}
		catch (Exception ex)
		{
			lock (_queueLock)
			{
				_isWriterRunning = false;
			}
			LogError($"Save queue worker failure: {ex.Message}");
		}
	}

	private static bool WriteToDiskCore(string json, bool updateBackup = true)
	{
		if (updateBackup && string.Equals(json, _lastWrittenJson, StringComparison.Ordinal) && File.Exists(ConfigFilePath))
		{
			return true;
		}

		string directory = Path.GetDirectoryName(ConfigFilePath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		byte[] payloadBytes = Encoding.UTF8.GetBytes(json);
		string tempPath = ConfigFilePath + ".tmp";
		Exception lastException = null;

		for (int attempt = 0; attempt < MaxWriteAttempts; attempt++)
		{
			try
			{
				using (FileStream fs = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
				{
					fs.Write(payloadBytes, 0, payloadBytes.Length);
					fs.Flush(flushToDisk: true);
				}

				if (new FileInfo(tempPath).Length != payloadBytes.Length)
				{
					throw new IOException("Temp settings file verification failed: size mismatch.");
				}

				if (File.Exists(ConfigFilePath))
				{
					string backupPath = updateBackup ? ConfigFilePath + ".bak" : null;
					File.Replace(tempPath, ConfigFilePath, backupPath, ignoreMetadataErrors: true);
				}
				else
				{
					File.Move(tempPath, ConfigFilePath);
				}

				_lastWrittenJson = json;
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				lastException = ex;
				if (attempt < MaxWriteAttempts - 1)
				{
					Thread.Sleep(RetryDelaysMs[attempt]);
				}
			}
			catch (Exception ex)
			{
				lastException = ex;
				break;
			}
			finally
			{
				if (File.Exists(tempPath))
				{
					try { File.Delete(tempPath); } catch { }
				}
			}
		}

		LogError($"Save failure after {MaxWriteAttempts} attempts: {lastException?.Message}");
		return false;
	}

	private static void LogError(string message)
	{
		lock (_logLock)
		{
			try
			{
				string directory = Path.GetDirectoryName(ConfigFilePath) ?? string.Empty;
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				string logPath = Path.Combine(directory, "error.log");
				if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxErrorLogBytes)
				{
					File.Move(logPath, logPath + ".old", overwrite: true);
				}

				File.AppendAllText(logPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
			}
			catch { }
		}
	}
}