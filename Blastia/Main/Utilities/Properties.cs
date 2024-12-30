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
    
    /// <summary>
    /// Works exactly like <see cref="OnValueChangedProperty{T}(ref T,T,System.Action?)"/>, but passes <c>newValue</c>
    /// to <c>onChangedAction</c>
    /// </summary>
    /// <param name="backUpField"></param>
    /// <param name="newValue"></param>
    /// <param name="onChangedAction"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T OnValueChangedProperty<T>(ref T backUpField, T newValue, Action<T>? onChangedAction = null)
    {
        if (!EqualityComparer<T>.Default.Equals(backUpField, newValue))
        {
            backUpField = newValue;
            onChangedAction?.Invoke(newValue);
        }
        
        return backUpField;
    }
}