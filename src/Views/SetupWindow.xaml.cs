using System.Windows;
using OllamaDesktopChat.Models;

namespace OllamaDesktopChat.Views;

/// <summary>
/// First-run setup window for configuring Ollama connection
/// </summary>
public partial class SetupWindow : Window
{
	public SetupWindow()
	{
		InitializeComponent();
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
