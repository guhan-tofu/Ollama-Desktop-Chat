using System.Text.Json;
using System.IO;

namespace OllamaDesktopChat.Models;

/// <summary>
/// Application settings management for storing user configuration
/// </summary>
public class AppSettings
{
	public string OllamaEndpoint { get; set; } = "http://localhost:11434";
	public string DefaultModel { get; set; } = string.Empty;
	public bool DarkMode { get; set; } = false;

	private static readonly string ConfigDirectory = 
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OllamaDesktopChat");
	
	private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "settings.json");

	/// <summary>
	/// Loads settings from disk or returns default if not found
	/// </summary>
	public static AppSettings Load()
	{
		try
		{
			if (File.Exists(ConfigPath))
			{
				var json = File.ReadAllText(ConfigPath);
				return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
		}

		return new AppSettings();
	}

	/// <summary>
	/// Saves settings to disk
	/// </summary>
	public void Save()
	{
		try
		{
			Directory.CreateDirectory(ConfigDirectory);
			var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(ConfigPath, json);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
		}
	}

	/// <summary>
	/// Checks if settings file exists (first run check)
	/// </summary>
	public static bool Exists => File.Exists(ConfigPath);
}
