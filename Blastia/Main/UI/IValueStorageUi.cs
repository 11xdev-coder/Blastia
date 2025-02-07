namespace Blastia.Main.UI;

/// <summary>
/// Used for UI which stores a variable (sets a variable, gets the variable, updates when variable has changed)
/// </summary>
public interface IValueStorageUi<T>
{
    /// <summary>
    /// Must be set in the constructor. Lambda for returning the original variable value
    /// </summary>
    protected Func<T> GetValue { get; set; }
    /// <summary>
    /// Must be set in the constructor. Lambda for setting the original variable value
    /// </summary>
    protected Action<T> SetValue { get; set; }
    
    /// <summary>
    /// Must be called when original variable has changed (subscribe to original event)
    /// </summary>
    protected abstract void UpdateLabel();
}