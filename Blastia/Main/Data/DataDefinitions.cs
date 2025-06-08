using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

namespace Blastia.Main.Data;

/// <summary>
/// Data structures used for JSON serialization
/// </summary>
public class DataDefinitions
{
    public class ItemDefinition
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tooltip { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public int MaxStack { get; set; } = 1;
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional item properties for specific types
        /// </summary>
        public JObject? Properties { get; set; }
    }

    public class BlockDefinition
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TexturePath { get; set; } = string.Empty;
        public float DragCoefficient { get; set; } = 50f;
        public float Hardness { get; set; } = 1f;
        public bool IsCollidable { get; set; } = true;
        public bool IsTransparent { get; set; }
        public ushort ItemIdDrop { get; set; }
        public int ItemDropAmount { get; set; } = 1;
        public int LightLevel { get; set; }
        
        /// <summary>
        /// Additional block properties
        /// </summary>
        public JObject? Properties { get; set; }
    }
}