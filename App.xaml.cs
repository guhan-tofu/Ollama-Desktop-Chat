using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using OllamaDesktopChat.Models;
using OllamaDesktopChat.Views;
using OllamaDesktopChat.src.Views;

namespace OllamaDesktopChat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// Global application settings
	/// </summary>
	public static AppSettings Settings { get; private set; } = new();

	protected override void OnStartup(StartupEventArgs e)
	{
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

		// Check if this is first run
		if (!AppSettings.Exists)
		{
			var setupWindow = new SetupWindow();
			if (setupWindow.ShowDialog() != true)
			{
				// User cancelled setup
				Shutdown();
				return;
			}
		}

		// Load settings
		Settings = AppSettings.Load();

		// Show main window
		MainWindow = new MainWindow();
		MainWindow.Show();

		base.OnStartup(e);
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show(
			e.Exception.Message,
			"Application Error",
			MessageBoxButton.OK,
			MessageBoxImage.Error);
		e.Handled = true;
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
		{
			MessageBox.Show(
				ex.Message,
				"Fatal Error",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		MessageBox.Show(
			e.Exception.GetBaseException().Message,
			"Background Task Error",
			MessageBoxButton.OK,
			MessageBoxImage.Error);
		e.SetObserved();
	}
}

