using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

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

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Name of the level: ");
        string? levelName = Console.ReadLine();
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
                    .Where(hit => hit.cache_level_name?.ToUpper() == levelName?.ToUpper())
                    .ToList();

                if (filteredHits.Count == 0)
                {
                    Console.WriteLine("Brak rekordów.");
                }
                else
                {
                    foreach (var hit in filteredHits)
                    {
                        Console.WriteLine($"ID: {hit.online_id}, Name: {hit.cache_level_name}, Difficulty: {hit.cache_stars}, Author: {hit.cache_username}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Brak danych w odpowiedzi.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }
}