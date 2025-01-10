using Blastia.Main.Utilities;

namespace Blastia.Main.Commands;

public delegate bool GameRuleGetter();

public class GameRuleCommands(bool log = true)
{
    /// <summary>
    /// Whether to log GameRule state changes
    /// </summary>
    public bool Log { get; set; } = log;
    private readonly Dictionary<string, Action<bool>> _gameRules = [];
    private Dictionary<string, GameRuleGetter> _gameRuleGetters = [];

    public void AddGameRule(string gameRule, Action<bool> onValueChanged) => _gameRules[gameRule] = onValueChanged;
    public void RemoveGameRule(string gameRule) => _gameRules.Remove(gameRule);
    
    /// <summary>
    /// Prints <c>gameRule</c> current state
    /// </summary>
    /// <param name="gameRule"></param>
    public void SetGameRule(string gameRule)
    {
        Console.WriteLine($"{gameRule}: {_gameRules[gameRule]}");
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

    public bool HasGameRule(string gameRule) => _gameRules.ContainsKey(gameRule);
}