namespace Blastia.Main.Utilities.ListHandlers;

/// <summary>
/// Base class for handling Lists and Enums. Provides functions like Next() and Previous()
/// that add/subtract current index and wrap around.
/// </summary>
public abstract class ListHandler<T>(List<T> list)
{
    private int _currentIndex;

    public T CurrentItem => list[_currentIndex];

    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (value < 0 || value >= list.Count)
            {
                throw new IndexOutOfRangeException("ListHandler index out of range.");
            }
            
            if (_currentIndex != value)
            {
                _currentIndex = value;
                IndexChanged?.Invoke();
            }
        }
    }
    public Action? IndexChanged;

    public virtual void Next()
    {
        CurrentIndex = (_currentIndex + 1) % list.Count;
    }

    public virtual void Previous()
    {
        CurrentIndex = (_currentIndex - 1 + list.Count) 
                       % list.Count;
    }

    public virtual string GetString()
    {
        return (CurrentItem != null ? CurrentItem.ToString() : string.Empty) ?? string.Empty;
    }
}