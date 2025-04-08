using FluentAvalonia.UI.Windowing;
using Relaytable.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Relaytable.Helpers
{
	public static class NknCli
	{
		private static string walletPassword = "";

		public static Task<string> GetNodeVersion()
		{
			return NkncQuery("-v", false);
		}

		public static async Task CreateWallet()
		{
			if (!File.Exists(Path.Combine(NknClientManager.BinaryDirectory, "wallet.json")))
			{
				walletPassword = Guid.NewGuid().ToString().Replace("-", "");
				File.WriteAllText(Path.Combine(NknClientManager.BinaryDirectory, "wallet.pswd"), $"{walletPassword}");
				await NkncQuery($"wallet -c -p {walletPassword}", false);
			}
		}

		public static async Task<string> NkncQuery(string argument, bool auth = true)
		{
			try
			{
				if (string.IsNullOrEmpty(walletPassword))
				{
					walletPassword = await File.ReadAllTextAsync(Path.Combine(NknClientManager.BinaryDirectory, "wallet.pswd"));
				}

				using Process process = new();
				process.StartInfo = new ProcessStartInfo
				{
					FileName = NknClientManager.NkncPath,
					Arguments = argument + (auth ? $" -p {walletPassword}" : ""),
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					WorkingDirectory = NknClientManager.BinaryDirectory
				};

				process.Start();
				string output = await process.StandardOutput.ReadToEndAsync();
				await process.WaitForExitAsync();

				return output.Trim();
			}
			catch
			{
				// Let the exception bubble up to the caller
				throw;
			}
		}

		/*private async Task<string> ExecuteNkncCommand(string arguments)
		{
			using Process process = new();
			process.StartInfo = new ProcessStartInfo
			{
				FileName = _nkncPath,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = _nkncPath.Replace("nknc", ""),
			};

			StringBuilder outputBuilder = new();
			StringBuilder errorBuilder = new();

			process.OutputDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					_ = outputBuilder.AppendLine(e.Data);
				}
			};

			process.ErrorDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					_ = errorBuilder.AppendLine(e.Data);
				}
			};

			_ = process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();

			if (process.ExitCode != 0 && errorBuilder.Length > 0)
			{
				AddLogEntry($"Error executing nknc command: {errorBuilder}", LogType.Error);
				return "";
			}

			return outputBuilder.ToString();
		}*/
	}
}
