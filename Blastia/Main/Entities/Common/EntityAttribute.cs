namespace Blastia.Main.Entities.Common;

[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    public ushort Id { get; set; }
}