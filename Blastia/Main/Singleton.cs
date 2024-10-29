namespace Blastia.Main;

// ensure parameterless constructor
public abstract class Singleton<T> where T : class, new()
{
    private static T? _instance;
    
    // lock object
    private static readonly object Lock = new();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // only one thread can create a new instance
                // if multiple threads try to access instance, multiple instances can be created
                lock (Lock)
                {
                    if (_instance == null) _instance = new T();
                }
            }

            return _instance;
        }
    }
}