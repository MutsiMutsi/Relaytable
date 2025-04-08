using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Relaytable.Helpers
{
	public class ConfigurationManager
	{
		private static readonly string _configFileName = "appsettings.json";
		private IConfiguration _configuration;
		private string _configFilePath;

		public ConfigurationManager()
		{
			_configFilePath = GetConfigurationPath();
			Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));

			if (!File.Exists(_configFilePath))
			{
				// Create an empty configuration file
				File.WriteAllText(_configFilePath, "{}");
			}

			ReloadConfiguration();
		}

		private void ReloadConfiguration()
		{
			_configuration = new ConfigurationBuilder()
				.AddJsonFile(_configFilePath, optional: false, reloadOnChange: true)
				.Build();
		}

		/// <summary>
		/// Gets a configuration value by key.
		/// </summary>
		/// <param name="key">The configuration key.</param>
		/// <returns>The configuration value or null if not found.</returns>
		public string GetValue(string key)
		{
			return _configuration[key];
		}

		/// <summary>
		/// Gets a configuration value by key with a default value if not found.
		/// </summary>
		/// <param name="key">The configuration key.</param>
		/// <param name="defaultValue">The default value to return if key is not found.</param>
		/// <returns>The configuration value or default value if not found.</returns>
		public string GetValue(string key, string defaultValue)
		{
			return _configuration[key] ?? defaultValue;
		}

		/// <summary>
		/// Sets a configuration value.
		/// </summary>
		/// <param name="key">The configuration key.</param>
		/// <param name="value">The configuration value.</param>
		public void SetValue(string key, string value)
		{
			// Read existing configuration
			Dictionary<string, string> config = ReadConfigDictionary();

			// Update value
			config[key] = value;

			// Write back to file
			WriteConfigDictionary(config);

			// Reload configuration
			ReloadConfiguration();
		}

		/// <summary>
		/// Sets multiple configuration values at once.
		/// </summary>
		/// <param name="keyValuePairs">Dictionary of configuration key-value pairs.</param>
		public void SetValues(Dictionary<string, string> keyValuePairs)
		{
			// Read existing configuration
			Dictionary<string, string> config = ReadConfigDictionary();

			// Update values
			foreach (var pair in keyValuePairs)
			{
				config[pair.Key] = pair.Value;
			}

			// Write back to file
			WriteConfigDictionary(config);

			// Reload configuration
			ReloadConfiguration();
		}

		/// <summary>
		/// Gets all configuration values.
		/// </summary>
		/// <returns>Dictionary of all configuration key-value pairs.</returns>
		public Dictionary<string, string> GetAllValues()
		{
			return ReadConfigDictionary();
		}

		/// <summary>
		/// Removes a configuration value.
		/// </summary>
		/// <param name="key">The configuration key to remove.</param>
		/// <returns>True if the key was found and removed, false otherwise.</returns>
		public bool RemoveValue(string key)
		{
			Dictionary<string, string> config = ReadConfigDictionary();

			bool removed = config.Remove(key);

			if (removed)
			{
				WriteConfigDictionary(config);
				ReloadConfiguration();
			}

			return removed;
		}

		/// <summary>
		/// Checks if a configuration key exists.
		/// </summary>
		/// <param name="key">The configuration key to check.</param>
		/// <returns>True if the key exists, false otherwise.</returns>
		public bool ContainsKey(string key)
		{
			return ReadConfigDictionary().ContainsKey(key);
		}

		/// <summary>
		/// Clears all configuration values.
		/// </summary>
		public void ClearAll()
		{
			WriteConfigDictionary(new Dictionary<string, string>());
			ReloadConfiguration();
		}

		private Dictionary<string, string> ReadConfigDictionary()
		{
			try
			{
				string json = File.ReadAllText(_configFilePath);
				Dictionary<string, string> config = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
				return config;
			}
			catch (Exception)
			{
				// If there's an error reading the file (e.g. invalid JSON), return empty dictionary
				return new Dictionary<string, string>();
			}
		}

		private void WriteConfigDictionary(Dictionary<string, string> config)
		{
			try
			{
				string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(_configFilePath, json);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to write configuration file: {ex.Message}", ex);
			}
		}

		private string GetConfigurationPath()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			if (OperatingSystem.IsWindows())
			{
				// Windows: %APPDATA%\Relaytable\
				return Path.Combine(appDataPath, "Relaytable", _configFileName);
			}
			else if (OperatingSystem.IsMacOS())
			{
				// macOS: ~/Library/Application Support/Relaytable/
				return Path.Combine(appDataPath, "Relaytable", _configFileName);
			}
			else // Linux and others
			{
				// Linux: ~/.config/Relaytable/
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".config",
					"Relaytable",
					_configFileName);
			}
		}
	}
}
