/*
- Copyright (C) [2026] [Kevin Stanislawski]]
 *
- This program is free software: you can redistribute it and/or modify
- it under the terms of the GNU General Public License as published by
- the Free Software Foundation, either version 3 of the License, or
- (at your option) any later version.
 *
- This program is distributed in the hope that it will be useful,
- but WITHOUT ANY WARRANTY; without even the implied warranty of
- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
- GNU General Public License for more details.
 *
- You should have received a copy of the GNU General Public License
- along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ProperDim;

public static partial class UpdateChecker
{
	private const string ReleasesLatestUrl = "https://github.com/crustyoldhuman/ProperDim/releases/latest";
	private const string TagPathMarker = "/releases/tag/";

	public static async Task CheckForUpdatesAsync()
	{
		if (NativeMethods.IsRunningAsMsix()) return;

		try
		{
			Version currentVersion = GetCurrentVersion();
			if (currentVersion == null) return;

			using HttpClientHandler handler = new() { AllowAutoRedirect = false };
			using HttpClient client = new(handler) { Timeout = TimeSpan.FromSeconds(10) };

			using HttpRequestMessage request = new(HttpMethod.Get, ReleasesLatestUrl);
			request.Headers.UserAgent.ParseAdd($"ProperDim/{currentVersion}");

			using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

			int statusCode = (int)response.StatusCode;
			if (statusCode < 300 || statusCode >= 400 || response.Headers.Location == null)
			{
				return;
			}

			Uri locationUri = response.Headers.Location.IsAbsoluteUri
				? response.Headers.Location
				: new Uri(new Uri("https://github.com"), response.Headers.Location);

			if (!string.Equals(locationUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(locationUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			int tagIndex = locationUri.AbsolutePath.IndexOf(TagPathMarker, StringComparison.OrdinalIgnoreCase);
			if (tagIndex < 0) return;

			string rawTag = Uri.UnescapeDataString(locationUri.AbsolutePath[(tagIndex + TagPathMarker.Length)..]).Trim('/');
			if (string.IsNullOrWhiteSpace(rawTag)) return;

			Match match = VersionRegex().Match(rawTag);
			if (!match.Success || !Version.TryParse(match.Value, out Version parsedLatest)) return;

			Version latestVersion = NormalizeVersion(parsedLatest);
			if (latestVersion <= currentVersion) return;

			string latestVersionKey = latestVersion.ToString();
			if (string.Equals(ConfigManager.Settings.LastNotifiedUpdateVersion, latestVersionKey, StringComparison.Ordinal))
			{
				return;
			}

			Application app = Application.Current;
			if (app == null) return;

			await app.Dispatcher.InvokeAsync(() =>
			{
				if (NativeMethods.IsRunningAsMsix()) return;
				if (string.Equals(ConfigManager.Settings.LastNotifiedUpdateVersion, latestVersionKey, StringComparison.Ordinal))
				{
					return;
				}

				ConfigManager.Settings.LastNotifiedUpdateVersion = latestVersionKey;
				ConfigManager.Settings.Save();

				Window owner = app.Windows.OfType<ControlPanel>().FirstOrDefault(w => w.IsVisible);

				MessageBoxResult result = WarningMessage.Show(
					owner,
					"Hi. An updated version of ProperDim is available. It's been improved! Do you want to go to the update page now? This is a one time message.",
					"ProperDim update detected!",
					primaryButtonText: "Take me there",
					secondaryButtonText: "I'll skip it");

				if (result == MessageBoxResult.Yes)
				{
					try
					{
						Process.Start(new ProcessStartInfo(locationUri.AbsoluteUri)
						{
							UseShellExecute = true
						});
					}
					catch { }
				}
			}, DispatcherPriority.ApplicationIdle);
		}
		catch
		{
			// Fail silently when offline, blocked, or shutting down
		}
	}

	private static Version GetCurrentVersion()
	{
		Version raw = Assembly.GetExecutingAssembly().GetName().Version;
		return raw == null ? null : NormalizeVersion(raw);
	}

	private static Version NormalizeVersion(Version v)
	{
		return new Version(
			v.Major,
			Math.Max(0, v.Minor),
			Math.Max(0, v.Build),
			Math.Max(0, v.Revision));
	}

	[GeneratedRegex(@"\d+(?:\.\d+){1,3}")]
	private static partial Regex VersionRegex();
}