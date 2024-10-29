using Blastia.Main.Utilities;

namespace Blastia.Main;

public abstract class ManagerWithStateSaving<T> : Singleton<T> where T : class, new()
{
    private string _savePath = "";
    protected abstract string SaveFileName { get; }

    protected abstract TState GetState<TState>();
    protected abstract void SetState<TState>(TState state);
    
    public virtual void Initialize()
    {
        _savePath = Path.Combine(Paths.GetSaveGameDirectory(), SaveFileName);
    }

    public void SaveStateToFile<TState>()
    {
        // if no file -> create it
        if (!File.Exists(_savePath))
        {
            File.Create(_savePath).Close();
        }
        // then save
        var state = GetState<TState>();
        Saving.Save(_savePath, state);
    }

    public void LoadStateFromFile<TState>() where TState : new()
    {
        // if no file -> create and save to fill it
        if (!File.Exists(_savePath))
        {
            File.Create(_savePath).Close();
            SaveStateToFile<TState>();
        }
        else
        {
            // if file exists, load it
            var state = Saving.Load<TState>(_savePath);
            SetState(state);
        }
    }
}