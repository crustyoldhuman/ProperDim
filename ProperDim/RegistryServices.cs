/*
 * properdim[cite: 1] - <lightweight non-DDC screen brightness management app>
 * Copyright (C) <2026> <crustyoldhuman/Kevin Stanislawski>
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
using System.Windows;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace ProperDim;

public static class RegistryService
{
	private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "ProperDim";

	public static async Task<bool> IsStartupEnabledAsync()
	{
		if (NativeMethods.IsRunningAsMsix())
		{
			try
			{
				var task = await Windows.ApplicationModel.StartupTask.GetAsync("ProperDimStartupTask");
				return task.State is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
			}
			catch
			{
				return false;
			}
		}

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

	public static async Task<bool> SetStartupAsync(bool enable)
	{
		if (NativeMethods.IsRunningAsMsix())
		{
			try
			{
				var task = await Windows.ApplicationModel.StartupTask.GetAsync("ProperDimStartupTask");
				if (enable)
				{
					var state = await task.RequestEnableAsync();
					return state is Windows.ApplicationModel.StartupTaskState.Enabled or Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;
				}
				else
				{
					task.Disable();
					return true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to update packaged startup settings: " + ex.Message);
				return false;
			}
		}

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