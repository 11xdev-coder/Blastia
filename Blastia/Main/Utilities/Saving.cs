using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Blastia.Main.Blocks;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Persistence;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Saving
{
    // TODO: refactor further
    /// <summary>
    /// Writes <c>Dictionary&lt;Vector2, BlockInstance&gt;</c> to a binary writer
    /// </summary>
    public static void WriteBlockDictionary(Dictionary<Vector2, BlockInstance> dict, BinaryWriter writer, bool debugLogs = false)
    {
        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, ushort> with {dict.Count} items");
                        
        writer.Write(dict.Count);
        foreach (var keyValuePair in dict)
        {
            Vector2 vector = keyValuePair.Key;
            BlockInstance inst = keyValuePair.Value;
                            
            if (debugLogs)
            {
                Console.WriteLine(vector == default
                    ? "Couldn't write Vector2"
                    : $"Writing Dictionary<Vector2, BlockInstance> entry: Position({vector.X}, {vector.Y}), BlockInst ID: {inst.Id}");
            }
                            
            writer.Write(vector.X);
            writer.Write(vector.Y);
            WriteObject(writer, inst);
        }
    }

    public static Dictionary<Vector2, BlockInstance> ReadBlockDictionary(BinaryReader reader, bool debugLogs = false)
    {
        var tileDictionary = new Dictionary<Vector2, BlockInstance>();
        var count = reader.ReadInt32();
        if (debugLogs) Console.WriteLine($"Reading dictionary with {count} entries from FileStream position: {reader.BaseStream.Position}");
            
        for (int i = 0; i < count; i++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            BlockInstance inst = (BlockInstance) ReadObject(reader, typeof(BlockInstance));
                
            if (float.IsInfinity(x) || float.IsNaN(x) ||
                float.IsInfinity(y) || float.IsNaN(y))
            {
                if (debugLogs) Console.WriteLine($"Skipping invalid tile entry {i}: ({x}, {y})");
                continue;
            }
                
            if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), BlockInst ID: {inst.Id}");
            tileDictionary.Add(new Vector2(x, y), inst);
        }
            
        if (debugLogs) Console.WriteLine($"Successfully read {tileDictionary.Count} entries");
        return tileDictionary;
    }
    
    public static void WriteStringDictionary(Dictionary<Vector2, string> dict, BinaryWriter writer, bool debugLogs = false) 
    {
        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, string> with {dict.Count} items");

        writer.Write(dict.Count);
        foreach (var keyValuePair in dict)
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

        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {writer.BaseStream.Position}");
    }
    
    public static Dictionary<Vector2, string> ReadStringDictionary(BinaryReader reader, bool debugLogs = false) 
    {
        var dict = new Dictionary<Vector2, string>();
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
            dict.Add(new Vector2(x, y), str);
        }
        
        if (debugLogs) Console.WriteLine($"Successfully read {dict.Count} entries");
        return dict;
    }
    
    public static void WriteObject(BinaryWriter writer, object value) 
    {
        switch (value)
        {
            case Dictionary<Vector2, BlockInstance> blockDict:
                WriteBlockDictionary(blockDict, writer);
                break;
            case Dictionary<Vector2, string> stringDict:
                WriteStringDictionary(stringDict, writer);
                break;
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
            case BlockInstance inst:
                writer.Write(inst.Id);
                if (inst.Block is LiquidBlock liquid)
                {
                    writer.Write(liquid.FlowLevel);
                }
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
            PropertyInfo[] properties = typeof(T).GetProperties()
                .OrderBy(p => p.MetadataToken)
                .ToArray();

            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttribute<NoSaveAttribute>() != null)
                    continue;
                    
                object? value = property.GetValue(state);
                if (value == null) continue;

                if (debugLogs)
                {
                    Console.WriteLine($"\nInspecting property {property.Name}");
                    Console.WriteLine($"Property type: {property.PropertyType.FullName}");
                }

                WriteObject(writer, value);   
            }
        }
    }

    /// <summary>
    /// Loads state class data (must be empty constructor) from a file and returns state with loaded parameters
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="debugLogs">If <c>true</c> prints debugging logs in the console</param>
    /// <param name="readCondition">If Func returns true, then it continues to read properties. Otherwise <strong>breaks</strong> and stops reading</param>
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Returns empty if file doesn't exist</returns>
    private static T LoadWithCondition<T>(string filePath, Func<PropertyInfo, bool> readCondition, bool debugLogs = false) where T : new()
    {
        T state = new T();
        
        using FileStream fs = File.Open(filePath, FileMode.Open);
        using (BinaryReader reader = new BinaryReader(fs))
        {
            PropertyInfo[] properties = typeof(T).GetProperties()
                .OrderBy(p => p.MetadataToken)
                .ToArray();
                
            foreach (PropertyInfo property in properties)
            {
                if (!readCondition(property)) break;
                
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
    
    /// <summary>
    /// <inheritdoc cref="LoadWithCondition"/>
    /// </summary>
    public static T Load<T>(string filePath, bool debugLogs = false) where T : new() => LoadWithCondition<T>(filePath, (p) => true, debugLogs);
    /// <summary>
    /// Loads only essential properties (marked with <c>EssentialAttribute</c>). For proper load these properties must be listed first in a class declaration
    /// </summary>
    public static T LoadLightweight<T>(string filePath, bool debugLogs = false) where T : new() => LoadWithCondition<T>(filePath, (p) => p.GetCustomAttribute<EssentialPropertyAttribute>() != null, debugLogs);
    
    public static object ReadObject(BinaryReader reader, Type type) 
    {
        if (type == typeof(Dictionary<Vector2, BlockInstance>))
        {
            return ReadBlockDictionary(reader);
        }
        
        if (type == typeof(Dictionary<Vector2, string>))
        {
            return ReadStringDictionary(reader);
        }
        
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
        
        if (type == typeof(BlockInstance)) 
        {
            ushort id = reader.ReadUInt16();
            Block? block = StuffRegistry.GetBlock(id) ?? throw new Exception($"Error reading BlockInstance. Block with ID: {id} not found.");
            
            if (block is LiquidBlock liquid) 
            {
                var liquidClone = liquid.CreateNewInstance();
                liquidClone.FlowLevel = reader.ReadInt32();
                block = liquidClone;
            }
            
            return new BlockInstance(block, 0);
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