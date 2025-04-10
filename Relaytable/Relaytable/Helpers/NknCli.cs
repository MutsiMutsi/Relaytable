using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Windowing;
using Relaytable.Models;
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
		private static string walletAddress = "";

		public static Task<string> GetNodeVersion()
		{
			return NkncQuery("-v", false);
		}

		public static async Task CreateWallet(Action<string, bool> updateAction)
		{
			if (!File.Exists(Path.Combine(NknClientManager.BinaryDirectory, "wallet.json")) || !File.Exists(Path.Combine(NknClientManager.BinaryDirectory, "wallet.pswd")))
			{
				updateAction("Creating wallet", false);
				walletPassword = Guid.NewGuid().ToString().Replace("-", "");
				File.WriteAllText(Path.Combine(NknClientManager.BinaryDirectory, "wallet.pswd"), $"{walletPassword}");
				await NkncQuery($"wallet -c -p {walletPassword}", false);
				updateAction("Creating wallet", true);
			}
			else
			{
				updateAction("Wallet already created", false);
				updateAction("Wallet already created", true);
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
			catch (Exception ex)
			{
				// Let the exception bubble up to the caller
				throw;
			}
		}

		public async static Task<bool> NkndCheck(Action<string, bool> updateAction)
		{
			var promise = new TaskCompletionSource<bool>();

			bool nodeGenerationComplete = false;

			var _nkndProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = NknClientManager.NkndPath,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					WorkingDirectory = NknClientManager.BinaryDirectory,
					Arguments = "--password-file wallet.pswd --no-nat",
				},
				EnableRaisingEvents = true
			};

			_nkndProcess.OutputDataReceived += (s, args) =>
			{
				Console.WriteLine($"OUT: {args.Data}");

				if (!string.IsNullOrWhiteSpace(args.Data))
				{
					if (args.Data.Contains("Failed to create ID"))
					{
						updateAction("Awaiting ID Generation transaction", false);
						_nkndProcess.Kill();
					}

					else if (args.Data.Contains("Generate ID transaction is on chain"))
					{
						updateAction("Awaiting ID Generation transaction", true);
						updateAction("ID Generation transaction found, waiting for ID.", false);
						_nkndProcess.Kill();
					}

					else if (args.Data.Contains("CreateID got resp") && !args.Data.Contains("error"))
					{
						updateAction("ID Generation transaction found, waiting for ID.", true);
						updateAction("Node ID Generation complete.", false);
						updateAction("Node ID Generation complete.", true);
						nodeGenerationComplete = true;
						_nkndProcess.Kill();
					}

					else if (args.Data.Contains("GetID got resp") && !args.Data.Contains("error"))
					{
						updateAction("ID Generation transaction found, waiting for ID.", true);
						updateAction("Node ID Generation complete.", false);
						updateAction("Node ID Generation complete.", true);
						nodeGenerationComplete = true;
						_nkndProcess.Kill();
					}

					else if (args.Data.Contains("current chord ID:"))
					{
						nodeGenerationComplete = true;
						_nkndProcess.Kill();
					}

					else if (args.Data.Contains("The process cannot access the file because it is being used by another process"))
					{
						updateAction("⚠ nknd process already in use, please terminate.", false);
						nodeGenerationComplete = false;
						_nkndProcess.Kill();
					}
				}
			};

			_nkndProcess.ErrorDataReceived += (s, args) =>
			{
				Console.WriteLine($"ERR: {args.Data}");
			};

			_nkndProcess.Exited += (s, args) =>
			{
				Console.WriteLine($"exited");
				_nkndProcess?.Kill();
				promise.SetResult(nodeGenerationComplete);
			};

			bool isStarted = _nkndProcess.Start();
			_nkndProcess.BeginOutputReadLine();
			_nkndProcess.BeginErrorReadLine();

			if (walletAddress == "")
			{
				string walletInfo = await NknCli.NkncQuery("wallet -l account", true);
				try
				{
					if (!string.IsNullOrWhiteSpace(walletInfo))
					{
						string? addressLine = walletInfo.Split('\n')[2];
						if (addressLine != null)
						{
							string[] parts = addressLine.Split(' ');
							if (parts.Length > 1)
							{
								walletAddress = parts[0];

								var button = new Button()
								{
									Content = new TextBlock()
									{
										Text = walletAddress,
									}
								};
								button.Click += async (s, e) =>
								{
									var clipboard = TopLevel.GetTopLevel(button)?.Clipboard;
									if (clipboard != null)
									{
										await clipboard.SetTextAsync(walletAddress);
									}
								};

								SetupWindow.stepGrid.Children.Insert(SetupWindow.stepGrid.Children.Count - 1,
									button
								);
							}
						}
					}
				}
				catch
				{
				}
			}

			if (isStarted)
			{
				updateAction("Trying to run node.", true);
			}


			return await promise.Task;
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
