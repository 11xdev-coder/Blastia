using System.Text.Json;

namespace Blastia.Main.Utilities;

public class WorldNameData 
{
    public string[] Prefixes { get; set; } = [];
    public string[] Adjectives { get; set; } = [];
    public string[] Nouns { get; set; } = [];
    public string[] Suffixes { get; set; } = [];
}

/// <summary>
/// Returns a random world name from the list ("worldnames.json")
/// </summary>
public static class WorldNameGenerator 
{
    private static readonly Random _rng = new();
    private static WorldNameData? _data;
    private static List<Func<string>>? Patterns;
    
    public static void Load() 
    {   
        // load names from json
        string json = File.ReadAllText(Paths.WorldNamesData);
        _data = JsonSerializer.Deserialize<WorldNameData>(json);
        if (_data == null) 
        {
            Console.WriteLine("[ERROR] Failed to load world names");
            return;
        }
        
        Patterns = new List<Func<string>>
        {
            () => $"{Pick(_data.Prefixes)} {Pick(_data.Adjectives)} {Pick(_data.Nouns)}",
            () => $"{Pick(_data.Prefixes)} {Pick(_data.Nouns)}",
            () => $"{Pick(_data.Adjectives)} {Pick(_data.Nouns)}",
            () => $"{Pick(_data.Nouns)} {Pick(_data.Suffixes)}",
            () => $"{Pick(_data.Prefixes)} {Pick(_data.Adjectives)} {Pick(_data.Nouns)} {Pick(_data.Suffixes)}",
        };
    }
    
    private static string Pick(string[] list) 
    {
        return list[_rng.Next(list.Length)];
    }
    
    public static string Generate(int lengthLimit = 999) 
    {
        if (Patterns == null) 
        {
            Console.WriteLine("[ERROR] Couldn't generate a world name. (called before initialization?)");
            return "";
        }
        
        string name;
        int attempts = 0;
        
        do 
        {
            Func<string> pattern = Patterns[_rng.Next(Patterns.Count)];
            name = pattern();
            attempts++;
        } while (name.Length > lengthLimit && attempts < 100);
        
        return name;
    }
}