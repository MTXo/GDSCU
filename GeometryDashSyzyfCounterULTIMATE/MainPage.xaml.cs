using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace GeometryDashSyzyfCounterULTIMATE
{
    public class Hit
    {
        public long online_id { get; set; }
        public string? cache_level_name { get; set; }
        public string? cache_username { get; set; }
        public int cache_stars { get; set; }
    }

    public class ApiResponse
    {
        public List<Hit>? hits { get; set; }
    }

    public class LevelInfo
    {
        public long OnlineId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public string Author { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{OnlineId} : (\"{Name}\" By: {Author})";
        }
    }

    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public async Task<List<LevelInfo>> GetLevelInfo(string levelName)
        {
            List<LevelInfo> levels = new List<LevelInfo>();
            var url = $"https://history.geometrydash.eu/api/v1/search/level/advanced/?query=%{Uri.EscapeDataString(levelName ?? "")}%";
            using HttpClient client = new HttpClient();

            try
            {
                var response = await client.GetStringAsync(url);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = JsonSerializer.Deserialize<ApiResponse>(response, options);

                if (data?.hits != null)
                {
                    var filteredHits = data.hits
                        .Where(hit => hit.cache_stars > 0)
                        .Where(hit => string.Equals(hit.cache_level_name?.Trim(), levelName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var hit in filteredHits)
                    {
                        levels.Add(new LevelInfo
                        {
                            OnlineId = hit.online_id,
                            Name = hit.cache_level_name ?? "Unknown",
                            Difficulty = hit.cache_stars,
                            Author = hit.cache_username ?? "Unknown"
                        });
                    }
                }
            }
            catch
            {
            }

            return levels;
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            LevelPicker.SelectedIndex = 0;
            if (string.IsNullOrWhiteSpace(LevelInput.Text))
            {
                Levels.Text = "Wprowadź nazwę poziomu.";
                return;
            }

            var levels = await GetLevelInfo(LevelInput.Text);

            if (levels.Count == 0)
            {
                Levels.Text = "Brak rekordów.";
                LevelPicker.ItemsSource = null;
                LevelPicker.Title = "Brak poziomów";
            }
            else
            {
                LevelPicker.SelectedItem = levels[0];
                LevelPicker.ItemsSource = levels;
                Levels.Text = $"Znaleziono {levels.Count} poziom(ów)";
            }
        }
    }

}
