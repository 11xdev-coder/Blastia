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
        public string Type { get; set; } = "Generic";
        
        /// <summary>
        /// Additional item properties for specific types
        /// </summary>
        public JObject? Properties { get; set; }
    }
}