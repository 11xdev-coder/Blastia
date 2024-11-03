namespace Blastia.Main.Entities.Common;

/// <summary>
/// This attribute is used to set the ID and correctly load + register entity textures.
/// We register textures using this ID, and then create an instance of Entity and register it
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    public ushort Id { get; set; }
}