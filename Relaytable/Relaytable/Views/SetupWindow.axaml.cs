﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using LoadingIndicators.Avalonia;
using Relaytable.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Relaytable;

public partial class SetupWindow : UserControl
{
	public static StackPanel stepGrid;

	public event EventHandler<EventArgs>? OnSetupCompleted;

	public SetupWindow()
	{
		InitializeComponent();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		try
		{
			Console.WriteLine("start setup");
			_ = Start();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
			throw;
		}
	}

	private StackPanel CreateStepPanel(string stepText)
	{
		StackPanel contents = new StackPanel()
		{
		};
		Border border = new Border()
		{
			Background = SolidColorBrush.Parse("#66000000"),
			Padding = Thickness.Parse("20"),
			CornerRadius = CornerRadius.Parse("8"),
			Child = contents,
		};

		TextBlock textBlock = new TextBlock()
		{
			Text = stepText,
			FontSize = 24,
			FontWeight = FontWeight.Bold,
			Foreground = SolidColorBrush.Parse("#FFFFFF"),
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
		};

		contents.Children.Add(textBlock);
		SetupContent.Content = border;

		return contents;
	}

	private async Task Start()
	{
		StackPanel contents = CreateStepPanel("Setup Mining");

		//Mode="Wave" SpeedRatio="1.0" Width="128" Margin="0" Foreground="DarkCyan"
		LoadingIndicator indicator = new LoadingIndicator()
		{
			Mode = LoadingIndicatorMode.Ring,
			Width = 256,
			Foreground = Brushes.DarkCyan,
			VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
		};

		contents.Children.Add(indicator);


		Grid grid = new Grid();
		grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");
		stepGrid = new StackPanel()
		{
			[Grid.ColumnProperty] = 1
		};
		grid.Children.Add(stepGrid);
		contents.Children.Add(grid);

		await NknClientManager.CheckAndUpdateAsync(updateAction);
		await WalletCreateStep();
		await NodeIDGenStep();
		//await NodeConfigureStep();
	}

	private void updateAction(string update, bool isDone)
	{

		_ = Dispatcher.UIThread.InvokeAsync(() =>
		{
			//Early out if we repeat the same undone state.
			if (!isDone)
			{
				if (stepGrid.Children.Any() && stepGrid.Children.Last() is StackPanel sp)
				{
					if (sp.Children.Last() is TextBlock tb)
					{
						if (tb.Text == update)
						{
							return;
						}
					}
				}
			}


			if (!isDone)
			{
				StackPanel sp = new StackPanel()
				{
					HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
					Orientation = Avalonia.Layout.Orientation.Horizontal
				};
				stepGrid.Children.Add(sp);
				sp.Children.Add(new LoadingIndicator()
				{
					Mode = LoadingIndicatorMode.ThreeDots,
					Foreground = Brushes.DarkCyan,
				});
				sp.Children.Add(new TextBlock() { Text = update, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
			}
			else
			{
				if (stepGrid.Children.Last() is StackPanel sp)
				{
					if (sp.Children.Last() is TextBlock tb)
					{
						if (tb.Text == update)
						{
							sp.Children.Replace(sp.Children.First(), new TextBlock() { Text = "✔" });
						}
					}
				}
			}
		});
	}

	private async Task WalletCreateStep()
	{
		await NknCli.CreateWallet(updateAction);
	}

	private async Task NodeIDGenStep()
	{
		updateAction("Trying to run node.", false);

		bool moneyReceived = false;
		while (!moneyReceived)
		{
			moneyReceived = await NknCli.NkndCheck(updateAction);
		}

		await NodeConfigureStep();
	}

	private Task NodeConfigureStep()
	{
		var startButton = new Button()
		{
			Margin = new Thickness(0, 64, 0, 0),
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
			VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
			HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
			VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
			Content = "Confirm",
			IsEnabled = false
		};

		bool syncSelected = false;
		bool addressCorrect = true;

		//string v = await GetNodeVersion();
		StackPanel contents = CreateStepPanel("Configure your node");

		Grid grid = new Grid();
		grid.ColumnDefinitions = new ColumnDefinitions("*,*,*");

		StackPanel gridContent = new StackPanel()
		{
			[Grid.ColumnProperty] = 1
		};
		gridContent.Children.Add(new Label()
		{
			Content = "Beneficiary Address",
		});
		var beneficiaryInput = new TextBox()
		{
			Watermark = "NKN wallet address",
		};
		gridContent.Children.Add(beneficiaryInput);
		gridContent.Children.Add(new TextBlock()
		{
			Text = "When you receive block rewards, this address will be the recipient of the NKN payouts. If you leave this empty, NKN rewards will be received on the node wallet (not advised).",
			TextWrapping = TextWrapping.Wrap,
		});

		//Leave empty to receive mining rewards in the node wallet.

		gridContent.Children.Add(new Label()
		{
			Content = "Sync Mode",
			Margin = new Thickness(0, 16, 0, 0)
		});
		ComboBox comboBox = new ComboBox()
		{
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
		};
		comboBox.Items.Add(new ComboBoxItem
		{
			Content = "Full",
			IsEnabled = false,
		});
		comboBox.Items.Add(new ComboBoxItem
		{
			Content = "Fast",
			IsEnabled = false,
		});
		comboBox.Items.Add(new ComboBoxItem
		{
			Content = "Light",
		});
		gridContent.Children.Add(comboBox);

		grid.Children.Add(gridContent);

		var syncExplanation = new TextBlock()
		{
			MaxLines = 16,
			TextWrapping = TextWrapping.Wrap,
		};
		gridContent.Children.Add(syncExplanation);

		comboBox.SelectionChanged += (s, e) =>
		{
			syncSelected = true;
			switch (comboBox.SelectionBoxItem)
			{
				case "Full":
					syncExplanation.Text = @"Node will perform a full sync by fetching and validating every block individually, this is the most safe, secure, and decentralised way to sync, but also the slowest. All blockchain information will be available to be queried from your local verified ChainDB files.";
					break;
				case "Fast":
					syncExplanation.Text = @"Node will sync the same full block history as before, but much faster by syncing the state trie directly. Think of it as a decentralized replacement of the ChainDB snapshot that many people are using.";
					break;
				case "Light":
					syncExplanation.Text = @"Node will only sync headers of old blocks without transactions. The local ledger size will be much smaller than before (ChainDB size is about 4GB at the time of the release), but node will not be able to respond to getblock and gettransaction RPC requests for old blocks/transactions. We recommend using light sync only when node disk space is not enough.";
					break;
				default:
					break;
			}
			startButton.IsEnabled = (syncSelected && addressCorrect);
		};


		beneficiaryInput.TextChanged += (s, e) =>
		{
			if (string.IsNullOrWhiteSpace(beneficiaryInput.Text))
			{
				//valid
				beneficiaryInput.BorderBrush = Brushes.Green;
				addressCorrect = true;
			}
			else
			{
				var match = new Regex("NKN[A-z0-9]{33}").Match(beneficiaryInput.Text).Value;
				if (match != beneficiaryInput.Text)
				{
					//invalid
					beneficiaryInput.BorderBrush = Brushes.Red;
					addressCorrect = false;
				}
				else
				{
					beneficiaryInput.BorderBrush = Brushes.Green;
					addressCorrect = true;
				}
			}

			startButton.IsEnabled = (syncSelected && addressCorrect);
		};

		contents.Children.Add(grid);
		contents.Children.Add(startButton);

		startButton.Click += (s, e) =>
		{
			if (comboBox.SelectionBoxItem == null) { throw new ArgumentException("No selection for ledger type."); }

			File.WriteAllText(Path.Combine(NknClientManager.BinaryDirectory, "config.json"),
@"{
	""BeneficiaryAddr"": ""{0}"",
	""SyncMode"": ""{1}"",
	""PasswordFile"": ""wallet.pswd""
}"
				.Replace("{0}", beneficiaryInput.Text)
				.Replace("{1}", comboBox.SelectionBoxItem.ToString()?.ToLowerInvariant())
			);

			OnSetupCompleted?.Invoke(this, new EventArgs());
		};

		return Task.CompletedTask;
	}

}