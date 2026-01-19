using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GitHubContributionWidget
{
	public partial class MainWindow : Window
	{
		private readonly HttpClient _httpClient;
		private readonly DispatcherTimer _timer;
		private readonly string _username;
		private readonly string? _token;
		private const int CELL_SIZE = 12;
		private const int CELL_MARGIN = 3;

		public MainWindow()
		{
			InitializeComponent();

			if (App.Configuration == null)
			{
				MessageBox.Show("App.Configuration NULL!", "Debug");
				Application.Current.Shutdown();
				return;
			}

			var token = App.Configuration["GitHub:Token"];
			var username = App.Configuration["GitHub:Username"];

			_username = username ?? "bariscanaslan";
			_token = token;

			if (string.IsNullOrEmpty(_token))
			{
				MessageBox.Show("Token boş!", "Hata");
				Application.Current.Shutdown();
				return;
			}

			_httpClient = new HttpClient();
			_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubWidget");

			UsernameText.Text = $"@{_username}";

			_timer = new DispatcherTimer();
			_timer.Interval = TimeSpan.FromHours(1);
			_timer.Tick += async (s, e) => await UpdateContributions();
			_timer.Start();

			Loaded += async (s, e) =>
			{
				SetDesktopPosition(); 
				await UpdateContributions();
			};
		}

		private void SetDesktopPosition()
		{
			this.UpdateLayout();

			var screenWidth = SystemParameters.PrimaryScreenWidth;
			var screenHeight = SystemParameters.PrimaryScreenHeight;

			this.Left = screenWidth - this.ActualWidth - 20;
			this.Top = screenHeight - this.ActualHeight - 80;
		}

		private async Task UpdateContributions()
		{
			try
			{
				ContributionCount.Text = "Loading...";
				var data = await GetContributionData(_username);

				var currentYear = DateTime.Now.Year;
				var currentYearContributions = data.Days
					.Where(d => d.Date.Year == currentYear)
					.Sum(d => d.Count);

				ContributionCount.Text = currentYearContributions.ToString("N0");

				DrawContributionGraph(data.Days);
			}
			catch (Exception ex)
			{
				ContributionCount.Text = "Error";
				MessageBox.Show($"Hata: {ex.Message}", "GitHub API Hatası");
			}
		}

		private void DrawContributionGraph(List<ContributionDay> days)
		{
			ContributionCanvas.Children.Clear();

			if (days.Count == 0) return;

			var currentYear = DateTime.Now.Year;
			var currentYearDays = days.Where(d => d.Date.Year == currentYear).ToList();

			if (currentYearDays.Count == 0)
			{
				var textBlock = new TextBlock
				{
					Text = $"No contributions in {currentYear} yet",
					Foreground = new SolidColorBrush(Colors.Gray),
					FontSize = 14
				};
				Canvas.SetLeft(textBlock, 10);
				Canvas.SetTop(textBlock, 50);
				ContributionCanvas.Children.Add(textBlock);
				return;
			}

			var weeks = currentYearDays
				.GroupBy(d => GetWeekNumber(d.Date))
				.OrderBy(g => g.Key)
				.ToList();

			int weekIndex = 0;

			foreach (var week in weeks)
			{
				var daysInWeek = week.OrderBy(d => d.Date.DayOfWeek).ToList();

				foreach (var day in daysInWeek)
				{
					int dayOfWeek = ((int)day.Date.DayOfWeek + 6) % 7;

					var rect = new Rectangle
					{
						Width = CELL_SIZE,
						Height = CELL_SIZE,
						Fill = new SolidColorBrush(GetColorFromHex(day.Color)),
						Stroke = new SolidColorBrush(Color.FromRgb(27, 31, 35)),
						StrokeThickness = 1,
						RadiusX = 2,
						RadiusY = 2
					};

					rect.ToolTip = $"{day.Date:MMM dd, yyyy}\n{day.Count} contributions";

					Canvas.SetLeft(rect, weekIndex * (CELL_SIZE + CELL_MARGIN));
					Canvas.SetTop(rect, dayOfWeek * (CELL_SIZE + CELL_MARGIN));

					ContributionCanvas.Children.Add(rect);
				}

				weekIndex++;
			}

			var canvasWidth = weekIndex * (CELL_SIZE + CELL_MARGIN) + 20;
			ContributionCanvas.Width = canvasWidth;

			var windowWidth = Math.Max(400, Math.Min(1200, canvasWidth + 60));
			this.Width = windowWidth;
		}

		private int GetWeekNumber(DateTime date)
		{
			var jan1 = new DateTime(date.Year, 1, 1);
			var daysOffset = (int)jan1.DayOfWeek;
			var firstMonday = jan1.AddDays(-daysOffset + 1);
			var weekNum = ((date - firstMonday).Days / 7) + 1;
			return weekNum;
		}

		private Color GetColorFromHex(string hex)
		{
			if (string.IsNullOrEmpty(hex) || hex == "#ebedf0")
				return Color.FromRgb(45, 45, 45);

			hex = hex.TrimStart('#');

			return Color.FromRgb(
				Convert.ToByte(hex.Substring(0, 2), 16),
				Convert.ToByte(hex.Substring(2, 2), 16),
				Convert.ToByte(hex.Substring(4, 2), 16)
			);
		}

		private async Task<ContributionData> GetContributionData(string username)
		{
			var query = @"
            query($username: String!) {
              user(login: $username) {
                contributionsCollection {
                  contributionCalendar {
                    totalContributions
                    weeks {
                      contributionDays {
                        contributionCount
                        date
                        color
                      }
                    }
                  }
                }
              }
            }";

			var request = new
			{
				query = query,
				variables = new { username = username }
			};

			var content = new StringContent(
				JsonSerializer.Serialize(request),
				Encoding.UTF8,
				"application/json"
			);

			var response = await _httpClient.PostAsync(
				"https://api.github.com/graphql",
				content
			);

			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"API hatası: {response.StatusCode}\n{responseText}");
			}

			var result = JsonSerializer.Deserialize<JsonElement>(responseText);

			var calendar = result
				.GetProperty("data")
				.GetProperty("user")
				.GetProperty("contributionsCollection")
				.GetProperty("contributionCalendar");

			var totalContributions = calendar.GetProperty("totalContributions").GetInt32();
			var weeks = calendar.GetProperty("weeks");

			var days = new List<ContributionDay>();

			foreach (var week in weeks.EnumerateArray())
			{
				foreach (var day in week.GetProperty("contributionDays").EnumerateArray())
				{
					days.Add(new ContributionDay
					{
						Date = DateTime.Parse(day.GetProperty("date").GetString()!),
						Count = day.GetProperty("contributionCount").GetInt32(),
						Color = day.GetProperty("color").GetString()!
					});
				}
			}

			return new ContributionData
			{
				TotalContributions = totalContributions,
				Days = days
			};
		}

		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			TrayIcon?.Dispose();
			Application.Current.Shutdown();
		}

		private void ToggleWindow_Click(object sender, RoutedEventArgs e)
		{
			this.Visibility = this.Visibility == Visibility.Visible
				? Visibility.Hidden
				: Visibility.Visible;
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await UpdateContributions();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			TrayIcon?.Dispose();
			Application.Current.Shutdown();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			TrayIcon?.Dispose();
			base.OnClosing(e);
		}
	}

	public class ContributionData
	{
		public int TotalContributions { get; set; }
		public List<ContributionDay> Days { get; set; } = new();
	}

	public class ContributionDay
	{
		public DateTime Date { get; set; }
		public int Count { get; set; }
		public string Color { get; set; } = "#ebedf0";
	}
}