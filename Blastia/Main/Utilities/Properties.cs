namespace Blastia.Main.Utilities;

public static class Properties
{
    /// <summary>
    /// When value has changed, executes action
    /// </summary>
    /// <param name="backUpField">Preferably private backup field to store data</param>
    /// <param name="newValue">New value</param>
    /// <param name="onChangedAction">Action which will be executed if newValue != backUpField</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T OnValueChangedProperty<T>(ref T backUpField, T newValue, Action? onChangedAction = null)
    {
        if (!EqualityComparer<T>.Default.Equals(backUpField, newValue))
        {
            backUpField = newValue;
            onChangedAction?.Invoke();
        }
        
        return backUpField;
    }
}