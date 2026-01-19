using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace GitHubContributionWidget
{
	public partial class App : Application
	{
		public static IConfiguration? Configuration { get; private set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AddToStartup();

			try
			{
				var basePath = AppDomain.CurrentDomain.BaseDirectory;

				Configuration = new ConfigurationBuilder()
					.SetBasePath(basePath)
					.AddJsonFile("appsettings.json", optional: false)
					.Build();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"appsettings.json yüklenemedi!\n\n{ex.Message}",
					"Configuration Hatası",
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);
				Shutdown();
			}
		}

		private void AddToStartup()
		{
			try
			{
				string appName = "GitHubContributionWidget";
				string exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

				using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(
					@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
				{
					if (key != null)
					{
						var existingValue = key.GetValue(appName);
						if (existingValue == null || existingValue.ToString() != exePath)
						{
							key.SetValue(appName, exePath);
						}
					}
				}
			}
			catch
			{
			}
		}
	}
}