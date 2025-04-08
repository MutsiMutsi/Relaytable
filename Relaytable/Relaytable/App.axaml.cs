using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Relaytable.Helpers;
using Relaytable.ViewModels;
using Relaytable.Views;
using System.Threading.Tasks;

namespace Relaytable
{
	public partial class App : Application
	{
		public static ConfigurationManager Config { get; private set; } = new ConfigurationManager();
		private static RootWindow? root;

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				root = new RootWindow
				{
					DataContext = new MainWindowViewModel(),
				};

				desktop.MainWindow = root;
			}
			if (Config.GetValue("AppVersion", "0.0.0") == "0.0.0")
			{
				var setup = new SetupWindow();
				root?.AppContent.Children.Add(setup);
				setup.OnSetupCompleted += (s, e) =>
				{
					setup = null;
					Config.SetValue("AppVersion", "0.1.0");
					if (root != null)
						root.Content = new MainWindow();
				};
			}
			else
			{
				if (root != null)
					root.Content = new MainWindow();
			}

			base.OnFrameworkInitializationCompleted();
		}

		// In your App.xaml.cs or MainWindow.xaml.cs
		/*private async Task InitializeNknAsync()
		{
			var updater = new NknGitHubUpdater();

			// Show loading indicator to user
			ShowLoadingState("Checking for NKN updates...");

			bool updated = await updater.CheckAndUpdateAsync();

			if (updated)
				LogInfo("NKN binaries have been updated to the latest version");
			else
				LogInfo("NKN binaries are up-to-date");

			// Hide loading indicator
			HideLoadingState();

			// Now you can use updater.StartNknd() and updater.StartNknc()
		}*/

	}
}