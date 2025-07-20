using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Blastia.Main.Blocks;
using Blastia.Main.Blocks.Common;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Saving
{
    /// <summary>
    /// Writes <c>Dictionary&lt;Vector2, ushort&gt;</c> to a binary writer
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="writer"></param>
    /// <param name="debugLogs"></param>
    public static void WriteTileDictionary(Dictionary<Vector2, ushort> dict, BinaryWriter writer, bool debugLogs = false)
    {
        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, ushort> with {dict.Count} items");
                        
        writer.Write(dict.Count);
        foreach (var keyValuePair in dict)
        {
            Vector2 vector = keyValuePair.Key;
            ushort id = keyValuePair.Value;
                            
            if (debugLogs)
            {
                Console.WriteLine(vector == default
                    ? "Couldn't write Vector2"
                    : $"Writing Dictionary<Vector2, ushort> entry: Position({vector.X}, {vector.Y}), ID: {id}");
            }
                            
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(id);
        }
    }

    public static Dictionary<Vector2, ushort> ReadTileDictionary(BinaryReader reader, bool debugLogs = false)
    {
        var tileDictionary = new Dictionary<Vector2, ushort>();
        var count = reader.ReadInt32();
        if (debugLogs) Console.WriteLine($"Reading dictionary with {count} entries from FileStream position: {reader.BaseStream.Position}");
            
        for (int i = 0; i < count; i++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var id = reader.ReadUInt16();
                
            if (float.IsInfinity(x) || float.IsNaN(x) ||
                float.IsInfinity(y) || float.IsNaN(y))
            {
                if (debugLogs) Console.WriteLine($"Skipping invalid tile entry {i}: ({x}, {y})");
                continue;
            }
                
            if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), ID: {id}");
            tileDictionary.Add(new Vector2(x, y), id);
        }
            
        if (debugLogs) Console.WriteLine($"Successfully read {tileDictionary.Count} entries");
        return tileDictionary;
    }
    
    public static void WriteObject(BinaryWriter writer, object value) 
    {
        switch (value)
        {
            case Vector2 vectorValue:
                // write X and Y
                writer.Write(vectorValue.X);
                writer.Write(vectorValue.Y);
                break;
            case ushort[] ushortArrayValue:
                writer.Write(ushortArrayValue.Length);
                foreach (var item in ushortArrayValue)
                {
                    writer.Write(item);
                }
                break;
            case Enum enumValue:
                writer.Write(Convert.ToInt32(enumValue));
                break;
            case byte byteValue:
                writer.Write(byteValue);
                break;
            case ushort ushortValue:
                writer.Write(ushortValue);
                break;
            case int intValue:
                writer.Write(intValue);
                break;
            case float floatValue:
                writer.Write(floatValue);
                break;
            case double doubleValue:
                writer.Write(doubleValue);
                break;
            case bool boolValue:
                writer.Write(boolValue);
                break;
            case string stringValue:
                writer.Write(stringValue);
                break;
            case ulong ulongValue:
                writer.Write(ulongValue);
                break;
        }
    }
    
    /// <summary>
    /// Writes to a file at filePath state's class data. Supports: <c> Dictionary&lt;Vector2, ushort&gt;</c>,
    /// <c> Vector2</c>, <c> ushort[]</c>, <c> enum values</c>, <c> byte</c>, <c> ushort</c>,
    /// <c> int</c>, <c> float</c>, <c> double</c>, <c> bool</c>, <c> string</c>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="state">Serializable state class</param>
    /// <param name="debugLogs">If <c>true</c> prints debugging logs in the console</param>
    /// <typeparam name="T"></typeparam>
    public static void Save<T>(string filePath, T state, bool debugLogs = false)
    {
        if (state == null) return;

        using FileStream fs = File.Open(filePath, FileMode.Create);
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            PropertyInfo[] properties = state.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object? value = property.GetValue(state);
                if (value == null) continue;

                if (debugLogs)
                {
                    Console.WriteLine($"\nInspecting property {property.Name}");
                    Console.WriteLine($"Property type: {property.PropertyType.FullName}");
                }

                switch (value)
                {
                    case Dictionary<Vector2, ushort> tileDictionary:
                        WriteTileDictionary(tileDictionary, writer, debugLogs);
                        break;
                    case Dictionary<Vector2, BlockInstance> blockInstanceDictionary:
                        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, BlockInstance> with {blockInstanceDictionary.Count} items");

                        writer.Write(blockInstanceDictionary.Count);
                        foreach (var keyValuePair in blockInstanceDictionary)
                        {
                            var vector = keyValuePair.Key;
                            var block = keyValuePair.Value;

                            if (debugLogs)
                            {
                                Console.WriteLine(vector == default
                                    ? "Couldn't write Vector2"
                                    : $"Writing Dictionary<Vector2, BlockInstance> entry: Position({vector.X}, {vector.Y}), Block ID: {block.Id}");
                            }

                            writer.Write(vector.X);
                            writer.Write(vector.Y);
                            // write blocks ID, reconstruct later from StuffRegistry
                            writer.Write(block.Id);
                            if (keyValuePair.Value.Block is LiquidBlock liquid)
                            {
                                writer.Write(liquid.FlowLevel);
                            }
                        }

                        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {fs.Position}");
                        break;
                    case Dictionary<Vector2, string> stringDictionary:
                        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, string> with {stringDictionary.Count} items");

                        writer.Write(stringDictionary.Count);
                        foreach (var keyValuePair in stringDictionary)
                        {
                            var vector = keyValuePair.Key;
                            var str = keyValuePair.Value;

                            if (debugLogs)
                            {
                                Console.WriteLine(vector == default
                                    ? "Couldn't write Vector2"
                                    : $"Writing Dictionary<Vector2, string> entry: Position({vector.X}, {vector.Y}), String: {str}");
                            }

                            writer.Write(vector.X);
                            writer.Write(vector.Y);
                            writer.Write(str);
                        }

                        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {fs.Position}");
                        break;
                    default:
                        WriteObject(writer, value);
                        break;
                }    
            }
        }
    }

    /// <summary>
    /// Loads state class data (must be empty constructor) from a file and returns state
    /// with loaded parameters
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="debugLogs">If <c>true</c> prints debugging logs in the console</param>
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Returns empty if file doesn't exist</returns>
    public static T Load<T>(string filePath, bool debugLogs = false) where T : new()
    {
        T state = new T();
        
        using FileStream fs = File.Open(filePath, FileMode.Open);
        using (BinaryReader reader = new BinaryReader(fs))
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    Type propertyType = property.PropertyType;
                    object value = ReadValue(reader, propertyType, debugLogs);
                    property.SetValue(state, value);
                }
                catch (EndOfStreamException)
                {
                    // legacy save without this property -> stop reading further properties
                    break;
                }
            }
        }

        return state;
    }
    
    public static object ReadObject(BinaryReader reader, Type type) 
    {
        if (type == typeof(Vector2))
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            return new Vector2(x, y);
        }
        
        if (type.IsArray)
        {
            var elementType = type.GetElementType() ?? 
                              throw new NullReferenceException("Array element type cannot be null");
            
            int length = reader.ReadInt32();
            Array array = Array.CreateInstance(elementType, length);
                        
            for (int i = 0; i < length; i++)
            {
                array.SetValue(ReadValue(reader, elementType), i);
            }

            return array;
        }

        if (type.IsEnum)
        {
            int enumValue = reader.ReadInt32();
            return Enum.ToObject(type, enumValue);
        }
        
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte: return reader.ReadByte();
            case TypeCode.UInt16: return reader.ReadUInt16();
            case TypeCode.Int32: return reader.ReadInt32();
            case TypeCode.Single: return reader.ReadSingle();
            case TypeCode.Double: return reader.ReadDouble();
            case TypeCode.Boolean: return reader.ReadBoolean();
            case TypeCode.String: return reader.ReadString();
            case TypeCode.UInt64: return reader.ReadUInt64();
        }

        throw new ArgumentException($"Unsupported type: {type.Name} (Full name: {type.FullName})");
    }

    private static object ReadValue(BinaryReader reader, Type type, bool debugLogs = false)
    {
        if (debugLogs) Console.WriteLine($"Reading type: {type.FullName}");

        if (type == typeof(Dictionary<Vector2, ushort>))
        {
            return ReadTileDictionary(reader, debugLogs);
        }
        
        if (type == typeof(Dictionary<Vector2, BlockInstance>))
        {
            var blockDictionary = new Dictionary<Vector2, BlockInstance>();
            var count = reader.ReadInt32();
            if (debugLogs) Console.WriteLine($"Reading block dictionary with {count} entries from FileStream position: {reader.BaseStream.Position}");

            for (int i = 0; i < count; i++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var blockId = reader.ReadUInt16();

                if (float.IsInfinity(x) || float.IsNaN(x) ||
                    float.IsInfinity(y) || float.IsNaN(y))
                {
                    if (debugLogs) Console.WriteLine($"Skipping invalid block entry {i}: Position({x}, {y})");
                    continue;
                }
                
                var blockTemplate = StuffRegistry.GetBlock(blockId);
                if (blockTemplate == null)
                {
                    if (debugLogs) Console.WriteLine($"Block ID: {blockId} not found in registry");
                    continue;
                }

                Block instance;
                if (blockTemplate is LiquidBlock liquid)
                {
                    var clone = liquid.CreateNewInstance();
                    clone.FlowLevel = reader.ReadInt32();
                    instance = clone;
                }
                else
                    instance = blockTemplate;
                
                if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), block ID: {blockId}, block name: {instance.Name}");
                blockDictionary.Add(new Vector2(x, y), new BlockInstance(instance, 0));
            }
            
            if (debugLogs) Console.WriteLine($"Successfully read {blockDictionary.Count} entries");
            return blockDictionary;
        }
        
        if (type == typeof(Dictionary<Vector2, string>))
        {
            var stringDictionary = new Dictionary<Vector2, string>();
            var count = reader.ReadInt32();
            if (debugLogs) Console.WriteLine($"Reading block dictionary with {count} entries from FileStream position: {reader.BaseStream.Position}");
            
            for (int i = 0; i < count; i++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var str = reader.ReadString();
                
                if (float.IsInfinity(x) || float.IsNaN(x) ||
                    float.IsInfinity(y) || float.IsNaN(y))
                {
                    if (debugLogs) Console.WriteLine($"Skipping invalid string entry {i}: ({x}, {y})");
                    continue;
                }
                
                if (string.IsNullOrEmpty(str))
                {
                    if (debugLogs) Console.WriteLine("String is null or empty, skipping");
                    continue;
                }
                
                if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), string: {str}");
                stringDictionary.Add(new Vector2(x, y), str);
            }
            
            if (debugLogs) Console.WriteLine($"Successfully read {stringDictionary.Count} entries");
            return stringDictionary;
        }

        return ReadObject(reader, type);
    }
    
    /// <summary>
    /// Gets a type code for custom serialization
    /// </summary>
    public static byte GetTypeCode(object value)
    {
        return value switch
        {
            Vector2 => 101,
            ushort[] => 102,
            byte[] => 103,
            Enum => 104,
            byte => 1,
            ushort => 2,
            int => 3,
            float => 4,
            double => 5,
            bool => 6,
            string => 7,
            ulong => 8,
            _ => 0 // unknown
        };
    }

    /// <summary>
    /// Gets the Type from a type code
    /// </summary>
    public static Type? GetTypeFromCode(byte typeCode)
    {
        return typeCode switch
        {
            101 => typeof(Vector2),
            102 => typeof(ushort[]),
            103 => typeof(byte[]),
            104 => typeof(Enum),
            1 => typeof(byte),
            2 => typeof(ushort),
            3 => typeof(int),
            4 => typeof(float),
            5 => typeof(double),
            6 => typeof(bool),
            7 => typeof(string),
            8 => typeof(ulong),
            _ => null
        };
    }
}