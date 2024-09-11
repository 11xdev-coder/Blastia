using System.Text.Json;

namespace BlasterMaster.Main.Utilities;

public static class Saving
{
    public static void Save<T>(string filePath, T state)
    {
        var jsonString = JsonSerializer.Serialize(state);
        File.WriteAllText(filePath, jsonString);
    }

    public static T Load<T>(string filePath) where T : new()
    {
        if (File.Exists(filePath))
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(jsonString) ?? new T();
        }

        return new T();
    }
}