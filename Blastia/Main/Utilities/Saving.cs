using System.Text.Json;

namespace Blastia.Main.Utilities;

public static class Saving
{
    /// <summary>
    /// Writes to a file at filePath state's class data (must be serializable)
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="state">Serializable state class</param>
    /// <typeparam name="T"></typeparam>
    public static void Save<T>(string filePath, T state)
    {
        var jsonString = JsonSerializer.Serialize(state);
        File.WriteAllText(filePath, jsonString);
    }

    /// <summary>
    /// Loads state class data (must be empty constructor + serializable) from a file and returns state
    /// with loaded parameters
    /// </summary>
    /// <param name="filePath"></param>
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Returns empty if file doesnt exist</returns>
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