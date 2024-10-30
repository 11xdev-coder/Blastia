using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace Blastia.Main.Utilities;

public static class Saving
{
    /// <summary>
    /// Writes to a file at filePath state's class data. Supports: ushort[], enum values, byte, ushort,
    /// int, float, double, bool, string
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="state">Serializable state class</param>
    /// <typeparam name="T"></typeparam>
    public static void Save<T>(string filePath, T state)
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

                switch (value)
                {
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
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Returns empty if file doesnt exist</returns>
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
        
        throw new ArgumentException("Unsupported type");
    }
}