namespace BlasterMaster.Main.Utilities.Handlers;

/// <summary>
/// Base class for handling Lists and Enums. Provides functions like Next() and Previous()
/// that add/subtract current index and wrap around.
/// </summary>
public abstract class Handler
{
    public virtual void Next()
    {
        
    }

    public virtual void Previous()
    {
        
    }

    public virtual string GetString()
    {
        return string.Empty;
    }
}