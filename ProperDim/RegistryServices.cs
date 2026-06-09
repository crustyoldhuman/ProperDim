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