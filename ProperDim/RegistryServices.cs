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
using System.Windows;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace ProperDim;

public static class RegistryService
{
	private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "ProperDim";

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static async Task<bool> IsStartupEnabledMsixAsync()
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

	public static async Task<bool> IsStartupEnabledAsync()
	{
		if (NativeMethods.IsRunningAsMsix())
		{
			return await IsStartupEnabledMsixAsync();
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

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static async Task<bool> SetStartupMsixAsync(bool enable)
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
		catch
		{
			return false;
		}
	}

	public static async Task<bool> SetStartupAsync(bool enable)
	{
		if (NativeMethods.IsRunningAsMsix())
		{
			return await SetStartupMsixAsync(enable);
		}

		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
			if (key != null)
			{
				if (enable)
				{
					string path = Environment.ProcessPath;
					key.SetValue(AppName, $"\"{path}\"");
				}
				else
				{
					key.DeleteValue(AppName, false);
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}