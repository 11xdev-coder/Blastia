namespace BlasterMaster.Main.Utilities.ListHandlers;

/// <summary>
/// Base class for handling Lists and Enums. Provides functions like Next() and Previous()
/// that add/subtract current index and wrap around.
/// </summary>
public abstract class ListHandler<T>
{
    protected List<T> _list;
    protected int _currentIndex;
    
    public T CurrentItem => _list[_currentIndex];

    protected ListHandler(List<T> list)
    {
        _list = list;
        _currentIndex = 0;
    }
    
    public virtual void Next()
    {
        _currentIndex = (_currentIndex + 1) % _list.Count;
    }

    public virtual void Previous()
    {
        _currentIndex = (_currentIndex - 1 + _list.Count) 
                        % _list.Count;
    }

    public virtual string GetString()
    {
        return CurrentItem != null ? CurrentItem.ToString() : string.Empty;
    }
}