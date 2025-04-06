using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using FluentAvalonia.UI.Windowing;
using LoadingIndicators.Avalonia;
using Relaytable.Models;
using Relaytable.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Relaytable.Views
{
	public partial class MainWindow : AppWindow
	{
		private Process _nkndProcess;
		private DispatcherTimer _statusUpdateTimer;
		private readonly string _nkndPath = "c:/nkn/nknd";
		private readonly string _nkncPath = "c:/nkn/nknc";

		private TextBlock _miningStatusText;
		private TextBlock _syncStatusText;

		private TextBlock _liveRelayCount;
		private TextBlock _liveRelayPerHourCount;
		private LoadingIndicator _activityIndicator;
		private double currentRelayCount;
		private double targetRelayCount;
		private int _uptime;

		private TextBlock _walletAddressText;
		private TextBlock _walletAddressPublicKeyText;
		private TextBlock _walletBalanceText;
		private TextBlock? _blockheightText;

		private ToggleSwitch _onOffSwitch;

		/*
private TextBlock _nodeInfoText;
private TextBox _logTextBox;

private ListBox _logListBox;
private ComboBox _refreshRateComboBox;

private TextBlock _peersCountText;
private TextBlock _heightText;
;*/

		public MainWindow()
		{
			//Width = 960;
			//Height = 380;
			InitializeComponent();


			TitleBar.ExtendsContentIntoTitleBar = true;
			TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

#if DEBUG
			this.AttachDevTools();
#endif



			InitializeControls();
			SetupTimers();

			// Update wallet address
			Task.Run(async () =>
			{
				string walletInfo = await ExecuteNkncCommand("wallet -l account --password test");

				_ = Dispatcher.UIThread.InvokeAsync(() =>
				{
					ParseAndUpdateWalletInfo(walletInfo);
				});
			});
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void InitializeControls()
		{
			_miningStatusText = this.FindControl<TextBlock>("MiningStatusText");
			_syncStatusText = this.FindControl<TextBlock>("SyncStatusText");
			_liveRelayCount = this.FindControl<TextBlock>("LiveRelayCount");
			_liveRelayPerHourCount = this.FindControl<TextBlock>("LiveRelayPerHourCount");
			_activityIndicator = this.FindControl<LoadingIndicators.Avalonia.LoadingIndicator>("ActivityIndicator");
			_activityIndicator.IsActive = false;

			_walletAddressText = this.FindControl<TextBlock>("WalletAddressText");
			_walletAddressPublicKeyText = this.FindControl<TextBlock>("WalletPublicKey");
			_walletBalanceText = this.FindControl<TextBlock>("WalletBalance");
			_blockheightText = this.FindControl<TextBlock>("BlockheightText");

			_onOffSwitch = this.FindControl<ToggleSwitch>("OffOnSwitch");

			/*_nodeInfoText = this.FindControl<TextBlock>("NodeInfoText");
			_logTextBox = this.FindControl<TextBox>("LogTextBox");

			_logListBox = this.FindControl<ListBox>("LogListBox");
			_refreshRateComboBox = this.FindControl<ComboBox>("RefreshRateComboBox");
			_peersCountText = this.FindControl<TextBlock>("PeersCountText");
			_heightText = this.FindControl<TextBlock>("HeightText");
			*/

			//_refreshRateComboBox.SelectedIndex = 1;  // Default to 5 seconds

			//TODO: investigate.
			//_logListBox.Background = new SolidColorBrush(Colors.Black);

			_onOffSwitch.IsCheckedChanged += _onOffSwitch_IsCheckedChanged;

			//_refreshRateComboBox.SelectionChanged += OnRefreshRateChanged;

			UpdateUI(false);

			DispatcherTimer timer = new(TimeSpan.FromSeconds(1.0 / 60.0), DispatcherPriority.Default, (s, e) =>
			{
				if (targetRelayCount < currentRelayCount)
				{
					currentRelayCount = targetRelayCount;
				}
				if (currentRelayCount < targetRelayCount)
				{
					currentRelayCount += Math.Ceiling((targetRelayCount - currentRelayCount) / 60.0);
					_liveRelayCount.Text = $"{currentRelayCount:0}";

					/*if (_liveRelayCount.FontSize < 20)
					{
						_liveRelayCount.FontSize += 0.2;
					};*/
				}
				else
				{
					/*if (_liveRelayCount.FontSize > 15)
					{
						_liveRelayCount.FontSize -= 0.2;
					};*/
				}

				_liveRelayPerHourCount.Text = $"{targetRelayCount / (_uptime / 3600.0):0}";
			});
		}

		private void _onOffSwitch_IsCheckedChanged(object? sender, RoutedEventArgs e)
		{
			if (_onOffSwitch.IsChecked == true)
			{
				StartNode();
			}
			else
			{
				StopNode();
			}
		}

		private void SetupTimers()
		{
			_statusUpdateTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_statusUpdateTimer.Tick += async (sender, e) => await UpdateNodeStatus();
		}

		private void OnRefreshRateChanged(object sender, SelectionChangedEventArgs e)
		{
			/*if (_refreshRateComboBox.SelectedItem is ComboBoxItem selectedItem)
			{
				string value = selectedItem.Content.ToString();
				int seconds = int.Parse(value.Split(' ')[0]);
				_statusUpdateTimer.Interval = TimeSpan.FromSeconds(seconds);

				if (_nkndProcess != null && !_nkndProcess.HasExited)
				{
					_statusUpdateTimer.Stop();
					_statusUpdateTimer.Start();
				}
			}*/
		}

		private async void StartNode()
		{
			try
			{
				//_logListBox.Items.Clear();
				AddLogEntry("Starting NKN node...", LogType.Info);

				_nkndProcess = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = _nkndPath,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true,
						WorkingDirectory = _nkndPath.Replace("nknd", ""),
						Arguments = "-c --password-file wallet.pswd",
					},
					EnableRaisingEvents = true
				};

				_nkndProcess.OutputDataReceived += (s, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
					{
						_ = Dispatcher.UIThread.InvokeAsync(() =>
						{
							AddLogEntry(args.Data, LogType.Info);
						});
					}
				};

				_nkndProcess.ErrorDataReceived += (s, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
					{
						_ = Dispatcher.UIThread.InvokeAsync(() =>
						{
							AddLogEntry(args.Data, LogType.Error);
						});
					}
				};

				_nkndProcess.Exited += (s, args) =>
				{
					_ = Dispatcher.UIThread.InvokeAsync(() =>
					{
						AddLogEntry("NKN node process exited", LogType.Warning);
						UpdateUI(false);
						_statusUpdateTimer.Stop();
					});
				};

				_ = _nkndProcess.Start();
				_nkndProcess.BeginOutputReadLine();
				_nkndProcess.BeginErrorReadLine();

				UpdateUI(true);
				_statusUpdateTimer.Start();
				await UpdateNodeStatus();
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error starting NKN node: {ex.Message}", LogType.Error);
				UpdateUI(false);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			StopNode();
			base.OnClosed(e);
		}

		private void StopNode()
		{
			try
			{
				if (_nkndProcess != null && !_nkndProcess.HasExited)
				{
					AddLogEntry("Stopping NKN node...", LogType.Info);
					_nkndProcess.Kill();
					_statusUpdateTimer.Stop();
					UpdateUI(false);
				}
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error stopping NKN node: {ex.Message}", LogType.Error);
			}
		}

		private void UpdateUI(bool isRunning)
		{
			_onOffSwitch.IsChecked = isRunning;

			//_statusText.Text = isRunning ? "Running" : "Stopped";
			//_statusText.Foreground = isRunning ? new SolidColorBrush(Colors.DarkCyan) : new SolidColorBrush(Colors.Gray);

			if (!isRunning)
			{
				_syncStatusText.Text = "offline";
				_syncStatusText.Foreground = new SolidColorBrush(Colors.Gray);
				_miningStatusText.Text = "offline";
				_miningStatusText.Foreground = new SolidColorBrush(Colors.Gray);
			}

			if (DataContext != null)
			{
				_activityIndicator.IsActive = isRunning;
			}
		}

		private async Task UpdateNodeStatus()
		{
			try
			{
				if (_nkndProcess == null || _nkndProcess.HasExited)
				{
					UpdateUI(false);
					return;
				}

				// Update mining status
				string miningStatus = await ExecuteNkncCommand("info -s");
				ParseAndUpdateNodeInfo(miningStatus);

				// Retrieve balance
				string balanceResponse = await ExecuteNkncCommand("wallet --list balance --password test");
				if (!string.IsNullOrEmpty(balanceResponse))
				{
					_ = Dispatcher.UIThread.InvokeAsync(() =>
					{
						try
						{
							string balance = JsonObject.Parse(balanceResponse)["result"]["amount"].GetValue<string>();
							_walletBalanceText.Text = $"{balance} NKN";
						}
						catch
						{
						}
					});
				}

				string neighborStatus = await ExecuteNkncCommand("info --neighbor");
				if (!string.IsNullOrEmpty(neighborStatus))
				{
					var response = JsonSerializer.Deserialize<Results<NodeNeighbour>>(neighborStatus);

					var viewModel = (DataContext as MainWindowViewModel);

					// Create a set of IDs from the response for efficient lookup
					var responseIds = response.result.Select(n => n.id).ToHashSet();

					// Remove neighbors that are no longer in the response
					for (int i = viewModel.Neighbours.Count - 1; i >= 0; i--)
					{
						if (!responseIds.Contains(viewModel.Neighbours[i].id))
						{
							viewModel.Neighbours.RemoveAt(i);
						}
					}

					// Add new neighbors that aren't already in the list
					foreach (var newNeighbor in response.result)
					{
						if (!viewModel.Neighbours.Any(existing => existing.id == newNeighbor.id))
						{
							viewModel.Neighbours.Add(newNeighbor);
						}
					}

				}

				// Update peers info
				//string neighborsInfo = await ExecuteNkncCommand("neighbor");
				//ParseAndUpdateNeighborInfo(neighborsInfo);
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error updating node status: {ex.Message}", LogType.Error);
			}
		}

		private void ParseAndUpdateNodeInfo(string output)
		{
			try
			{
				// Basic parsing - in a real app you'd want to use JSON parsing
				if (string.IsNullOrWhiteSpace(output))
				{
					AddLogEntry("Received empty response from node info command", LogType.Warning);
					return;
				}


				Result<NodeStatusModel> data = JsonSerializer.Deserialize<Result<NodeStatusModel>>(output);

				// Try to get mining status
				bool isMining = data.result.syncState == "PERSIST_FINISHED";
				_miningStatusText.Text = isMining ? "Mining" : "Not Mining";
				_miningStatusText.Foreground = isMining ? new SolidColorBrush(Colors.DarkCyan) : new SolidColorBrush(Colors.Gray);

				// Try to get sync status
				string? syncLine = output.Split('\n').FirstOrDefault(l => l.Contains("syncState") || l.Contains("SyncState"));
				if (syncLine != null)
				{
					string syncText = "Unknown";
					switch (data.result.syncState)
					{
						case "PERSIST_FINISHED":
							syncText = "Finished";
							break;
						case "SYNC_FINISHED":
							syncText = "Finished";
							break;
						case "WAIT_FOR_SYNCING":
							syncText = "Waiting";
							break;
					}

					bool isSynced = data.result.syncState is "PERSIST_FINISHED" or "SYNC_FINISHED";
					_syncStatusText.Text = syncText;
					_syncStatusText.Foreground = isSynced ? new SolidColorBrush(Colors.DarkCyan) : new SolidColorBrush(Colors.Orange);
				}

				// Try to get height
				//_heightText.Text = data.result.height.ToString();


				targetRelayCount = data.result.relayMessageCount;
				_uptime = data.result.uptime;

				_blockheightText.Text = $"{data.result.height}";

				//_nodeInfoText.Text = output;
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error parsing node info: {ex.Message}", LogType.Error);
			}
		}

		private void ParseAndUpdateWalletInfo(string output)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(output))
				{
					AddLogEntry("Received empty response from wallet command", LogType.Warning);
					return;
				}

				string? addressLine = output.Split('\n')[2];
				if (addressLine != null)
				{
					string[] parts = addressLine.Split(' ');
					if (parts.Length > 1)
					{
						_walletAddressText.Text = parts[0];
						_walletAddressPublicKeyText.Text = parts[3];
					}
				}
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error parsing wallet info: {ex.Message}", LogType.Error);
			}
		}

		private void ParseAndUpdateNeighborInfo(string output)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(output))
				{
					AddLogEntry("Received empty response from neighbor command", LogType.Warning);
					return;
				}

				// Count the number of neighbor entries by counting the number of IDs or addresses
				string[] lines = output.Split('\n');
				int peerCount = lines.Count(l => l.Contains("\"id\"") || l.Contains("\"addr\""));

				//_peersCountText.Text = peerCount.ToString();
			}
			catch (Exception ex)
			{
				AddLogEntry($"Error parsing neighbor info: {ex.Message}", LogType.Error);
			}
		}

		private async Task<string> ExecuteNkncCommand(string arguments)
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
		}

		private void AddLogEntry(string message, LogType logType)
		{
			// Still maintain the collection for reference
			LogEntry logEntry = new()
			{
				Timestamp = DateTime.Now,
				Message = message,
				Type = logType
			};

			// Add a TextBlock directly to the ListBox
			TextBlock textBlock = new()
			{
				Text = $"[{logEntry.Timestamp:HH:mm:ss}] {logEntry.Message}",
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush(logType switch
				{
					LogType.Info => Colors.DarkGray,
					LogType.Warning => Colors.Orange,
					LogType.Error => Colors.Red,
					_ => Colors.Black
				})
			};

			/*_ = _logListBox.Items.Add(textBlock);

			// Auto-scroll to the bottom
			if (_logListBox.Items.Count > 0)
			{
				_logListBox.ScrollIntoView(_logListBox.Items[_logListBox.Items.Count - 1]);
			}*/
		}
	}

	public enum LogType
	{
		Info,
		Warning,
		Error
	}

	public class LogEntry : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
		public LogType Type { get; set; }

		public string DisplayText => $"[{Timestamp:HH:mm:ss}] {Message}";

		public IBrush TextColor => Type switch
		{
			LogType.Info => Brushes.Black,
			LogType.Warning => Brushes.Orange,
			LogType.Error => Brushes.Red,
			_ => Brushes.Black
		};

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
	public class LogTypeToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is LogType type
				? type switch
				{
					LogType.Info => Brushes.Black,
					LogType.Warning => Brushes.Orange,
					LogType.Error => Brushes.Red,
					_ => Brushes.Black
				}
				: (object)Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}