using System.Text.Json;

namespace Blastia.Main.Utilities;

public class WorldNameData 
{
    public string[] Prefixes { get; set; } = [];
    public string[] Adjectives { get; set; } = [];
    public string[] Nouns { get; set; } = [];
    public string[] Suffixes { get; set; } = [];
}

public class PlayerNameData 
{
    public string[] NameStarts { get; set; } = [];
    public string[] NameEnds { get; set; } = [];
    public string[] Adjectives { get; set; } = [];
    public string[] Nouns { get; set; } = [];
    public string[] Titles { get; set; } = [];
    public string[] Epithets { get; set; } = [];
}

/// <summary>
/// Returns a random world name from the list ("worldnames.json")
/// </summary>
public static class RandomNameGenerator 
{
    private static readonly Random _rng = new();
    private static WorldNameData? _worldNameData;
    private static List<Func<string>>? WorldPatterns;
    private static PlayerNameData? _playerNameData;
    private static List<Func<string>>? PlayerPatterns;
    
    public static void Load() 
    {   
        // load names from json
        string worldJson = File.ReadAllText(Paths.WorldNamesData);
        _worldNameData = JsonSerializer.Deserialize<WorldNameData>(worldJson);
        if (_worldNameData == null) 
        {
            Console.WriteLine("[ERROR] Failed to load world names");
            return;
        }
        
        WorldPatterns = new List<Func<string>>
        {
            () => $"{Pick(_worldNameData.Prefixes)} {Pick(_worldNameData.Adjectives)} {Pick(_worldNameData.Nouns)}",
            () => $"{Pick(_worldNameData.Prefixes)} {Pick(_worldNameData.Nouns)}",
            () => $"{Pick(_worldNameData.Adjectives)} {Pick(_worldNameData.Nouns)}",
            () => $"{Pick(_worldNameData.Nouns)} {Pick(_worldNameData.Suffixes)}",
            () => $"{Pick(_worldNameData.Prefixes)} {Pick(_worldNameData.Adjectives)} {Pick(_worldNameData.Nouns)} {Pick(_worldNameData.Suffixes)}",
        };
        
        string playerJson = File.ReadAllText(Paths.PlayerNamesData);
        _playerNameData = JsonSerializer.Deserialize<PlayerNameData>(playerJson);
        if (_playerNameData == null) 
        {
            Console.WriteLine("[ERROR] Failed to load player names");
            return;
        }
        
        PlayerPatterns = new List<Func<string>> 
        {
            () => $"{MakePlayerName()}",
            () => $"{MakePlayerName()} {Pick(_playerNameData.Epithets)}",
            () => $"{MakePlayerName()} {Pick(_playerNameData.Titles)}",
            () => $"{Pick(_playerNameData.Adjectives)} {Pick(_playerNameData.Nouns)}",
            () => $"{Pick(_playerNameData.Adjectives)} {Pick(_playerNameData.Nouns)} {Pick(_playerNameData.Titles)}",
            () => $"{MakePlayerName()} {Pick(_playerNameData.Adjectives)} {Pick(_playerNameData.Nouns)}",
            () => $"{Pick(_playerNameData.Epithets)}",
            () => $"{Pick(_playerNameData.Nouns)} {Pick(_playerNameData.Titles)}",
        };
    }
    
    private static string MakePlayerName() 
    {
        if (_playerNameData == null)
            return "";
            
        string start = Pick(_playerNameData.NameStarts);
        string end = Pick(_playerNameData.NameEnds);
        
        if (start[^1] == end[0])
            end = end[1..];
            
        return $"{start}{end}";
    }
    
    private static string Pick(string[] list) 
    {
        return list[_rng.Next(list.Length)];
    }
    
    public static string GenerateWorldName(int lengthLimit) => Generate(WorldPatterns, () => "Big Chunk", lengthLimit);
    public static string GeneratePlayerName(int lengthLimit) => Generate(PlayerPatterns, MakePlayerName, lengthLimit);
    
    private static string Generate(List<Func<string>>? patterns, Func<string> fallback, int lengthLimit) 
    {
        if (patterns == null)
            return "";
        
        for (int attempt = 0; attempt < 50; attempt++) 
        {
            string name = patterns[_rng.Next(patterns.Count)]();
            if (name.Length <= lengthLimit) 
            {
                Console.WriteLine($"[NameGenerator] Generated a name in {attempt} attempts");
                return name;
            }
        }
        
        Console.WriteLine($"[NameGenerator] Using a fallback");
        string safe = fallback();
        return safe.Length < lengthLimit ? safe : safe[..lengthLimit];
    }
}