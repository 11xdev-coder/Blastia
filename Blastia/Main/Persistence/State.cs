namespace Blastia.Main.Persistence;

/// <summary>
/// Loaded in lightweight mode, not loaded in basic load
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EssentialPropertyAttribute : Attribute { }

/// <summary>
/// Doesn't save the property
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NoSaveAttribute : Attribute { }

/// <summary>
/// Basic class for <c>WorldState</c>, <c>PlayerState</c> etc for shared properties
/// </summary>
public class State 
{
    /// <summary>
    /// Path to this state. Set during lighweight load to help load fully afterwards
    /// </summary>
    [NoSave] public string FilePath { get; set; } = "";
    [EssentialProperty] public string Name { get; set; } = "";
    [EssentialProperty] public long CreatedAt { get; set; }
    [EssentialProperty] public int PlayedFor { get; set; }
    
    public override string ToString() => Name;
    
    [NoSave] public DateTimeOffset CreatedAtDate => DateTimeOffset.FromUnixTimeSeconds(CreatedAt);
    [NoSave] public string CreatedAtDisplay => CreatedAtDate.LocalDateTime.ToString("dd.MM.yyyy");
}