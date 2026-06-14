using Blastia.Main.Persistence;
using Blastia.Main.Utilities;


/// <summary>
/// Contains utility/base methods for <c>WorldManager</c> and <c>PlayerManager</c>
/// </summary>
public static class ManagerFileHelper 
{
    public static void New(string pathToSaveFolder, string name, string extension, object? data = null)
	{
		if (string.IsNullOrEmpty(pathToSaveFolder))
		    throw new Exception($"Failed creating a save: Provided folder path is null or empty.");
		    
        string fullPath = GetFullPath(pathToSaveFolder, name, extension);

        if (File.Exists(fullPath))
            throw new Exception($"Failed creating a save: Save already exists. Full path: {fullPath}");
            
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
		where T : new()
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
                if (loadedState is State state)
                    state.FilePath = file;
                items.Add(loadedState);
            }
        }

        return items;
	}

    /// <summary>
    /// Returns full path including extension of type 'folder/name.extension'
    /// </summary>
	public static string GetFullPath(string folder, string name, string extension)
	{
		// Players/Name.bmplr or Worlds/Name.bmwld
		return Path.Combine(folder, name + extension);
	}
}