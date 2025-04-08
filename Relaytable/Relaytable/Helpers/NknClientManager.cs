using Avalonia.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Relaytable.Helpers
{
	public static class NknClientManager
	{
		private const string GITHUB_API_RELEASES_URL = "https://api.github.com/repos/nknorg/nkn/releases/latest";

		public static string NkndPath => Path.Combine(BinaryDirectory, "nknd" + (IsWindows ? ".exe" : ""));
		public static string NkncPath => Path.Combine(BinaryDirectory, "nknc" + (IsWindows ? ".exe" : ""));

		public static string BinaryDirectory { get; private set; }

		private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		static NknClientManager()
		{
			// Create a directory in AppData or similar location
			BinaryDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Relaytable",
				"NknBinaries");

			_ = Directory.CreateDirectory(BinaryDirectory);
		}

		public static async Task<bool> CheckAndUpdateAsync()
		{
			try
			{
				ReleaseInfo latestRelease = await GetLatestReleaseInfoAsync();
				long lastNodeUpdateDate = long.Parse(App.Config.GetValue("LastNodeUpdateDate", "0"));


				// Determine which asset to download based on the current OS
				string assetName = DetermineCorrectAssetName();
				AssetInfo? asset = latestRelease.Assets.Find(a => a.Name.Contains(assetName) && a.Name.Contains("64"));
				string downloadURL = $"https://commercial.nkn.org/downloads/nkn-node/{asset?.Name}"; //linux-amd64.zip

				if (asset == null)
				{
					throw new Exception($"Could not find appropriate release asset for {assetName}");
				}

				long remoteLastModifiedDate = await CheckFileInfoBeforeDownload(downloadURL);

				// Check if we need to update
				bool needsUpdate = true;
				if (File.Exists(NkndPath) && File.Exists(NkncPath))
				{
					needsUpdate = lastNodeUpdateDate < remoteLastModifiedDate;
				}

				if (needsUpdate)
				{
					await DownloadAndExtractLatestReleaseAsync(asset.Name, downloadURL);
					App.Config.SetValue("LastNodeUpdateDate", remoteLastModifiedDate.ToString());
					return true; // Updated
				}

				return false; // No update needed
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error checking for NKN updates: {ex.Message}");
				// If update check fails but binaries exist, continue with existing binaries
				return File.Exists(NkndPath) && File.Exists(NkncPath);
			}
		}

		private static async Task<ReleaseInfo> GetLatestReleaseInfoAsync()
		{
			HttpClient httpClient = new();
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Relaytable");
			string response = await httpClient.GetStringAsync(GITHUB_API_RELEASES_URL);
			using JsonDocument jsonDoc = JsonDocument.Parse(response);
			JsonElement root = jsonDoc.RootElement;

			ReleaseInfo releaseInfo = new()
			{
				TagName = root.GetProperty("tag_name").GetString() ?? "",
				Assets = []
			};

			JsonElement assets = root.GetProperty("assets");
			foreach (JsonElement asset in assets.EnumerateArray())
			{
				string name = asset.GetProperty("name").GetString() ?? "";
				string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";

				releaseInfo.Assets.Add(new AssetInfo { Name = name, DownloadUrl = downloadUrl });
			}

			return releaseInfo;
		}

		private static async Task DownloadAndExtractLatestReleaseAsync(string assetName, string downloadURL)
		{
			// Download the asset
			try
			{

				HttpClient httpClient = new();
				httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Relaytable");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));


				string tempZipPath = Path.Combine(Path.GetTempPath(), assetName);
				using (Stream response = await httpClient.GetStreamAsync(downloadURL))
				using (FileStream fileStream = new(tempZipPath, FileMode.Create))
				{
					await response.CopyToAsync(fileStream);
				}


				// Clear the binary directory (except version.txt)
				foreach (string file in Directory.GetFiles(BinaryDirectory))
				{
					if (Path.GetFileName(file) != "version.txt")
					{
						File.Delete(file);
					}
				}

				// Extract the ZIP file
				string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
				ZipFile.ExtractToDirectory(tempZipPath, extractPath);

				// Find and copy the binaries to the binary directory
				CopyBinariesFromExtracted(extractPath);

				// Clean up
				File.Delete(tempZipPath);
				Directory.Delete(extractPath, true);
			}
			catch (Exception)
			{
				throw;
			}
		}

		private static string DetermineCorrectAssetName()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return "windows";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return "linux";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return "darwin"; // macOS is often called darwin in releases
			}
			else
			{
				throw new PlatformNotSupportedException("Unsupported operating system");
			}
		}

		private static void CopyBinariesFromExtracted(string extractPath)
		{
			// The GitHub release structure might have subdirectories
			// We need to find nknd and nknc executables recursively

			foreach (string file in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
			{
				string fileName = Path.GetFileName(file);

				if (fileName.Equals("nknd") || fileName.Equals("nknd.exe") ||
					fileName.Equals("nknc") || fileName.Equals("nknc.exe"))
				{
					string destPath = Path.Combine(BinaryDirectory, fileName);
					File.Copy(file, destPath, true);

					// Make sure the files are executable on Unix-like systems
					if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						Process? chmodProcess = Process.Start(new ProcessStartInfo
						{
							FileName = "chmod",
							Arguments = $"+x \"{destPath}\"",
							UseShellExecute = false,
							CreateNoWindow = true
						});
						chmodProcess?.WaitForExit();
					}
				}
			}
		}

		public static async Task<long> CheckFileInfoBeforeDownload(string url)
		{
			using HttpClient client = new();
			// Send a HEAD request
			HttpRequestMessage request = new(HttpMethod.Head, url);
			HttpResponseMessage response = await client.SendAsync(request);

			// Check if the request was successful
			_ = response.EnsureSuccessStatusCode();

			// Get the Last-Modified header
			if (response.Content.Headers.LastModified.HasValue)
			{
				DateTime lastModified = response.Content.Headers.LastModified.Value.LocalDateTime;
				Console.WriteLine($"Last modified: {lastModified}");

				long unixTime = ((DateTimeOffset)lastModified).ToUnixTimeSeconds();
				return unixTime;
			}

			// Get the content length (file size)
			if (response.Content.Headers.ContentLength.HasValue)
			{
				long fileSize = response.Content.Headers.ContentLength.Value;
				Console.WriteLine($"File size: {fileSize} bytes");
			}

			// Get ETag (can be used for change detection)
			if (response.Headers.ETag != null)
			{
				string etag = response.Headers.ETag.Tag;
				Console.WriteLine($"ETag: {etag}");
			}
			return 0;
		}

		private class ReleaseInfo
		{
			public required string TagName { get; set; }
			public required System.Collections.Generic.List<AssetInfo> Assets { get; set; }
		}

		private class AssetInfo
		{
			public required string Name { get; set; }
			public required string DownloadUrl { get; set; }
		}
	}
}
