using System.Text.RegularExpressions;
using Blastia.Main.Persistence;
using Blastia.Main.Utilities;

public enum SaveValidationResult
{
    InvalidName,
    InvalidPath,
    AlreadyExists,
    Success
}

/// <summary>
/// Contains utility/base methods for <c>WorldManager</c> and <c>PlayerManager</c>
/// </summary>
public static class ManagerFileHelper 
{
    private static string GetFullPath(string folder, string name, string extension) => Path.Combine(folder, name + extension);
    
    public static SaveValidationResult CanCreate(string pathToSaveFolder, string name, string extension) 
    {
        if (string.IsNullOrEmpty(pathToSaveFolder))
            return SaveValidationResult.InvalidPath;
           
        if (!IsValidName(name))
            return SaveValidationResult.InvalidName;
            
        if (File.Exists(GetFullPath(pathToSaveFolder, name, extension))) 
            return SaveValidationResult.AlreadyExists;
            
        return SaveValidationResult.Success;
    }
    
    public static void New(string pathToSaveFolder, string name, string extension, object? data = null)
	{
        if (CanCreate(pathToSaveFolder, name, extension) != SaveValidationResult.Success)
            return;
        
        var fullPath = GetFullPath(pathToSaveFolder, name, extension);
        // save data if provided
        if (data != null) 
            Saving.Save(fullPath, data);
        else   
            File.Create(fullPath).Close();
	}

	public static bool Exists(string pathToSaveFolder, string name, string extension)
	{	
        if (string.IsNullOrEmpty(pathToSaveFolder))
		    throw new Exception($"Failed to check a save for existing: Provided folder path is null or empty.");

        string fullPath = GetFullPath(pathToSaveFolder, name, extension);
        return File.Exists(fullPath);
	}

	public static List<T> LoadAll<T>(string pathToSaveFolder, string extension)
		where T : State, new()
	{
		if (string.IsNullOrEmpty(pathToSaveFolder))
		    throw new Exception($"Failed loading all saves: Provided folder path is null or empty.");
		 
        List<T> items = [];

        // go through each file
        foreach (string file in Directory.GetFiles(pathToSaveFolder))
        {
            // if correct extension
            if (file.EndsWith(extension))
            {
                // load new instance
                var loadedState = Saving.LoadLightweight<T>(file);
                loadedState.FilePath = file;
                items.Add(loadedState);
            }
        }

        return items;
	}
	
	public static bool IsValidName(string name) => Regex.IsMatch(name, @"^[a-zA-Z0-9\s_]+$");
}