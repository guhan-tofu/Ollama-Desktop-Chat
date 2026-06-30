using System.Windows;
using OllamaDesktopChat.Models;

namespace OllamaDesktopChat.Views;

/// <summary>
/// Settings dialog for configuring application options
/// </summary>
public partial class SettingsWindow : Window
{
	public SettingsWindow()
	{
		InitializeComponent();
		LoadSettings();
	}

	private void LoadSettings()
	{
		var settings = AppSettings.Load();
		EndpointTextBox.Text = settings.OllamaEndpoint;
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		// Validate endpoint
		if (string.IsNullOrWhiteSpace(EndpointTextBox.Text))
		{
			MessageBox.Show("Please enter an Ollama server endpoint.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		// Save settings
		var settings = new AppSettings
		{
			OllamaEndpoint = EndpointTextBox.Text.Trim()
		};
		settings.Save();

		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
		Close();
	}
}
