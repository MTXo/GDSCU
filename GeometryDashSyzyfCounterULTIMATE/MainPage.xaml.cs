using System.Text.Json;

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
            return $"{OnlineId} : (\"{Name}\" By: {Author} Starrate: {Difficulty})";
        }
    }
    public class RootObject
    {
        public Dictionary<int, int>? Deaths { get; set; }
    }
    public partial class MainPage : ContentPage
    {
        public bool unratedLevels = false;
        public bool isTextMode = true;
        int[] attempts = new int[101];

        private async void ToggleOptions(object sender, EventArgs e)
        {
            if (OptionsMenu.IsVisible)
            {
                await OptionsMenu.FadeTo(0, 200);
                OptionsMenu.IsVisible = false;
            }
            else
            {
                OptionsMenu.IsVisible = true;
                await OptionsMenu.FadeTo(1, 200);
            }
        }
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
                    var filteredHits = data.hits;
                    if (unratedLevels == false)
                    {
                        filteredHits = data.hits
                            .Where(hit => hit.cache_stars > 0)
                            .Where(hit => string.Equals(hit.cache_level_name?.Trim(), levelName?.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    else
                    {
                        filteredHits = data.hits
                            .Where(hit => string.Equals(hit.cache_level_name?.Trim(), levelName?.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
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
        private async void OnClick(object sender, EventArgs e)
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
                LevelPicker.SelectedItem = null;
                LevelPicker.ItemsSource = levels;
                Levels.Text = $"Znaleziono {levels.Count} poziom(ów)";
            }
        }
        private void OnLevelSelect(object sender, EventArgs e)
        {
            CalculateSection.IsVisible=true;
            CalculateButton.IsVisible = true;

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..",
                "Local",
                "GeometryDash",
                "geode",
                "mods",
                "elohmrow.death_tracker",
                "levels");
            if (LevelPicker.ItemsSource != null)
            {
                if (LevelPicker.SelectedIndex == -1)
                    return;

                var selectedItem = LevelPicker.SelectedItem;
                if (selectedItem is LevelInfo level)
                {
                    string levelInfo = selectedItem.ToString() ?? " ";
                    int index = levelInfo.IndexOf(' ');
                    string levelId = index >= 0 ? levelInfo.Substring(0, index) : levelInfo;
                    string json = string.Empty;

                    string deathTrackerLevelPath = $"{path}\\{levelId}.json";
                    try
                    {
                        json = File.ReadAllText(deathTrackerLevelPath);
                    }
                    catch
                    {
                        json = string.Empty;
                    }
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    if(json == null || !File.Exists(deathTrackerLevelPath))
                    {
                        Levels.Text = "Brak danych o śmierciach dla tego poziomu.";
                        return;
                    }

                    RootObject? data = JsonSerializer.Deserialize<RootObject>(json, options);

                    Levels.Text = "";

                    if (data == null || data.Deaths == null || data.Deaths.Count == 0)
                    {
                        Levels.Text = "Brak danych o śmierciach dla tego poziomu.";
                        return;
                    }
                    
                    for (int i = 0; i < 101; i++)
                    {
                        attempts[i] = 0;
                    }
                    foreach (var pair in data.Deaths)
                    {
                        var percentage = pair.Key;
                        var attemptCount = pair.Value;

                        attempts[percentage] = pair.Value;
                    }
                }
            }
        }
        private void FillTab()
        {
            int percent;
            string[] lines = AttemptsInput.Text.Split("\r");

            for (int i = 0; i < 100; i++)
            {
                attempts[i] = 0;
            }

            foreach (string line in lines)
            {
                if (line == " " || line == "")
                { continue; }
                for (int i = 0; i < line.Length; i++)
                {
                    if (line.Contains("From 0:"))
                    {
                        continue;
                    }
                    if ((string.Compare(line[i].ToString(), "%")) == 0)
                    {
                        percent = int.Parse(line.Substring(0, i));
                        for (int j = 0; j < line.Length; j++)
                        {
                            if ((string.Compare(line[j].ToString(), "x")) == 0)
                            {
                                attempts[percent] = int.Parse(line.Substring(j + 1));
                            }
                        }
                    }
                }
            }
        }
        private void CalculateAttempts(object sender, EventArgs e)
        {
            if (isTextMode && AttemptsInput.Text is not null)
            {
                FillTab();
            }
            int sum = 0;
            bool isEndGiven = false;
            if (int.TryParse(PercentageEnd.Text, out int end))
            {
                if (end <= 100)
                {
                    isEndGiven = true;
                    end++;
                }
                else
                {
                    end = 100;
                    isEndGiven = false;
                }
            }
            else
            {
                end = 100;
                isEndGiven = false;
            }

            int start = 0;
            if (int.TryParse(PercentageStart.Text, out start))
            {
                for (int i = start; i < end; i++)
                {
                    if (attempts[i] != 0)
                    {
                        sum += attempts[i];
                    }
                }
                if (isEndGiven)
                    Levels.Text = $"Attempt count from {start}% to {end - 1}% is {sum}.";
                else
                    Levels.Text = $"Attempt count from {start}% is {sum}.";
            }
            for (int i = start; i < end; ++i)
            {
                attempts[i] = 0;
            }

        }
        private async void ShowUnratedLevels(object sender, EventArgs e)
        {
            if (unratedLevels)
            {
                await UnratedLevelsOption.FadeTo(0, 200);
                UnratedLevelsOption.Text = "Include Unrated Levels";
                await UnratedLevelsOption.FadeTo(1, 200);
                unratedLevels = false;
            }
            else if (unratedLevels == false)
            {
                await UnratedLevelsOption.FadeTo(0, 200);
                UnratedLevelsOption.Text = "Don't Include Unrated Levels";
                await UnratedLevelsOption.FadeTo(1, 200);
                unratedLevels = true;
            }
        }
        private async void LevelPickerMode(object sender, EventArgs e)
        {
            if (isTextMode)
            {
                await Task.WhenAll(
                    ChangeMode.FadeTo(0, 200),
                    TextMode.FadeTo(0, 200)
                );
                TextMode.IsVisible = false;
                ChangeMode.Text = "Text Mode";
                PickerMode.IsVisible = true;
                UnratedLevelsOption.IsVisible = true;
                await Task.WhenAll(
                    ChangeMode.FadeTo(1, 200),
                    PickerMode.FadeTo(1, 200)
                );
                isTextMode = false;
                
            }
            else if (isTextMode == false)
            {
                await Task.WhenAll(
                    ChangeMode.FadeTo(0, 200),
                    PickerMode.FadeTo(0, 200)
                );
                PickerMode.IsVisible = false;
                UnratedLevelsOption.IsVisible = false;
                ChangeMode.Text = "Picker Mode";
                TextMode.IsVisible = true;
                await Task.WhenAll(
                    ChangeMode.FadeTo(1, 200),
                    TextMode.FadeTo(1, 200)
                );
                isTextMode = true;
            }
        }
        private void ShowInfo(object sender, EventArgs e)
        {

        }
        private async void UnfocusedOptionsMenu(object sender, EventArgs e)
        {
            await OptionsMenu.FadeTo(0, 200);
            OptionsMenu.IsVisible = false;
        }
    }

}
