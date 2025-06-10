using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Blastia.Main.Blocks.Common;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Saving
{
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
                        if (debugLogs) Console.WriteLine($"Writing Dictionary<Vector2, ushort> with {tileDictionary.Count} items");
                        
                        writer.Write(tileDictionary.Count);
                        foreach (var keyValuePair in tileDictionary)
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
                        
                        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {fs.Position}");
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
                        }
                        
                        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {fs.Position}");
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
                // get property type
                Type propertyType = property.PropertyType;
                // read property value
                object value = ReadValue(reader, propertyType, debugLogs);
                // set state's property to value
                property.SetValue(state, value);
            }
        }

        return state;
    }

    private static object ReadValue(BinaryReader reader, Type type, bool debugLogs = false)
    {
        if (debugLogs) Console.WriteLine($"Reading type: {type.FullName}");

        if (type == typeof(Dictionary<Vector2, ushort>))
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
                    throw new InvalidDataException(
                        $"Invalid position values at entry {i}: ({x}, {y})");
                }
                
                if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), ID: {id}");
                tileDictionary.Add(new Vector2(x, y), id);
            }
            
            if (debugLogs) Console.WriteLine($"Successfully read {tileDictionary.Count} entries");
            return tileDictionary;
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
                    throw new InvalidDataException(
                        $"Invalid position values at entry {i}: ({x}, {y})");
                }
                
                var block = StuffRegistry.GetBlock(blockId);
                if (block == null)
                {
                    if (debugLogs) Console.WriteLine($"Block ID: {blockId} not found in registry");
                    continue;
                }
                
                if (debugLogs) Console.WriteLine($"Read entry {i}: Position({x}, {y}), block ID: {blockId}, block name: {block.Name}");
                blockDictionary.Add(new Vector2(x, y), new BlockInstance(block, 0));
            }
            
            if (debugLogs) Console.WriteLine($"Successfully read {blockDictionary.Count} entries");
            return blockDictionary;
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
        
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte: return reader.ReadByte();
            case TypeCode.UInt16: return reader.ReadUInt16();
            case TypeCode.Int32: return reader.ReadInt32();
            case TypeCode.Single: return reader.ReadSingle();
            case TypeCode.Double: return reader.ReadDouble();
            case TypeCode.Boolean: return reader.ReadBoolean();
            case TypeCode.String: return reader.ReadString();
        }
        
        throw new ArgumentException($"Unsupported type: {type.Name} (Full name: {type.FullName})");
    }
}