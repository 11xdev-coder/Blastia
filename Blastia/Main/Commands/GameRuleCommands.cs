using Blastia.Main.Utilities;

namespace Blastia.Main.Commands;

public delegate bool GameRuleGetter();

public class GameRuleCommands(bool log = true) : ICommands
{
    /// <summary>
    /// Whether to log GameRule state changes
    /// </summary>
    public bool Log { get; set; } = log;
    private readonly Dictionary<string, Action<bool>> _gameRules = [];
    private readonly Dictionary<string, GameRuleGetter> _gameRuleGetters = [];

    public bool HasGameRule(string gameRule) => _gameRules.ContainsKey(gameRule);
    
    /// <summary>
    /// Adds new <c>GameRule</c> command
    /// </summary>
    /// <param name="gameRule">Name</param>
    /// <param name="onValueChanged">Function executed whenever <c>GameRule</c> state is trying to change (<see cref="SetGameRule(string, int)"/>,
    /// <see cref="SetGameRule(string, bool)"/>)</param>
    /// <param name="getter">Function that returns <c>bool</c> representing <c>GameRule</c> state</param>
    public void AddGameRule(string gameRule, Action<bool> onValueChanged, GameRuleGetter getter)
    {
        _gameRules[gameRule] = onValueChanged;
        _gameRuleGetters[gameRule] = getter;
    }

    public void RemoveGameRule(string gameRule)
    {
        _gameRules.Remove(gameRule);
        _gameRuleGetters.Remove(gameRule);
    }
    
    /// <summary>
    /// Prints <c>gameRule</c> current state
    /// </summary>
    /// <param name="gameRule"></param>
    public void SetGameRule(string gameRule)
    {
        var state = _gameRuleGetters[gameRule].Invoke();
        Console.WriteLine($"{gameRule}: {state}");
    }

    /// <summary>
    /// Casts <c>value</c> to boolean and sets <c>gameRule</c> state
    /// </summary>
    /// <param name="gameRule"></param>
    /// <param name="value"></param>
    public void SetGameRule(string gameRule, int value)
    {
        var boolValue = MathUtilities.CastToBool(value);
        _gameRules[gameRule].Invoke(boolValue);
        if (Log) Console.WriteLine($"{gameRule} is now set to: {boolValue}");
    }
    
    /// <summary>
    /// Sets <c>gameRule</c> state to <c>value</c>
    /// </summary>
    /// <param name="gameRule"></param>
    /// <param name="value"></param>
    public void SetGameRule(string gameRule, bool value)
    {
        _gameRules[gameRule].Invoke(value);
        if (Log) Console.WriteLine($"{gameRule} is now set to: {value}");
    }

    public void Clear()
    {
        _gameRules.Clear();
        _gameRuleGetters.Clear();
    }
}