using System.Collections;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
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

                // first check dictionary<vector2, ushort> 
                if (value.GetType().IsDictionary(typeof(Vector2), typeof(ushort)))
                {
                    if (value is IDictionary dictionary)
                    {
                        Console.WriteLine($"Writing dictionary with {dictionary.Count} items");
                        writer.Write(dictionary.Count);

                        foreach (DictionaryEntry keyValuePair in dictionary)
                        {
                            Vector2 vector = (Vector2)keyValuePair.Key;
                            ushort id = (ushort)keyValuePair.Value;

                            if (debugLogs)
                            {
                                Console.WriteLine(vector == default
                                    ? "Couldn't write Vector2"
                                    : $"Writing entry: Position({vector.X}, {vector.Y}), ID: {id}");
                            }
                            
                            writer.Write(vector.X);
                            writer.Write(vector.Y);
                            writer.Write(id);
                        }
                        
                        if (debugLogs) Console.WriteLine($"Finished writing Dictionary at FileStream position: {fs.Position}");
                    }
                }
                else
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
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loads state class data (must be empty constructor) from a file and returns state
    /// with loaded parameters
    /// </summary>
    /// <param name="filePath"></param>
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Returns empty if file doesn't exist</returns>
    public static T Load<T>(string filePath) where T : new()
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
                object value = ReadValue(reader, propertyType);
                // set state's property to value
                property.SetValue(state, value);
            }
        }

        return state;
    }

    private static object ReadValue(BinaryReader reader, Type type)
    {
        Console.WriteLine($"Reading type: {type.FullName}");

        // Check for Dictionary with Vector2 key and ushort value
        if (type.IsGenericType && 
            (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
             type == typeof(Dictionary<Vector2, ushort>)))
        {
            var args = type.GetGenericArguments();
            if (args.Length == 2 && 
                (args[0] == typeof(Vector2) || args[0].FullName.Contains("Vector2")) && 
                (args[1] == typeof(ushort) || args[1].FullName.Contains("UInt16")))
            {
                try
                {
                    var tileDictionary = new Dictionary<Vector2, ushort>();
                    var count = reader.ReadInt32();
                    Console.WriteLine($"Reading dictionary with {count} entries from position: {reader.BaseStream.Position}");
                    
                    if (count < 0 || count > 1000000) // Sanity check
                    {
                        throw new InvalidDataException($"Invalid dictionary count: {count}");
                    }

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
                        
                        Console.WriteLine($"Read entry {i}: Position({x}, {y}), ID: {id}");
                        tileDictionary.Add(new Vector2(x, y), id);
                    }
                    
                    Console.WriteLine($"Successfully read {tileDictionary.Count} entries");
                    return tileDictionary;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading dictionary at position {reader.BaseStream.Position}");
                    Console.WriteLine($"Exception: {ex.Message}");
                    throw;
                }
            }
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

    /// <summary>
    /// Checks whether dictionary has same key and value type as <c>keyType</c> and <c>valueType</c>
    /// </summary>
    /// <param name="dictionaryType"></param>
    /// <param name="keyType"></param>
    /// <param name="valueType"></param>
    /// <param name="keyFullName">If not <c>null</c>, dictionary key can be checked by checking does its <c>key</c>
    /// contains <c>keyFullName</c></param>
    /// <returns>Returns <c>true</c> if <c>dictionaryType</c> is dictionary with specified <c>keyType</c> and <c>valueType</c>.
    /// Otherwise -> <c>false</c></returns>
    private static bool IsDictionary(this Type dictionaryType, Type keyType, Type valueType, string? keyFullName = null)
    {
        if (!dictionaryType.IsGenericType || 
            dictionaryType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return false;
    
        var args = dictionaryType.GetGenericArguments();
        return args[0] == keyType && args[1] == valueType || 
               (keyFullName != null && args[0].FullName?.Contains(keyFullName) == true);
    }
}