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
                Type propertyType = property.PropertyType;
                object value = ReadValue(reader, propertyType);
                property.SetValue(state, value);
            }
        }

        return state;
    }

    private static object ReadValue(BinaryReader reader, Type type)
    {
        if (type.IsArray)
        {
            int length = reader.ReadInt32();
            Array array = Array.CreateInstance(type.GetElementType(), length);
                        
            for (int i = 0; i < length; i++)
            {
                array.SetValue(ReadValue(reader, type.GetElementType()), i);
            }

            return array;
        }

        if (type.IsEnum)
        {
            int enumValue = reader.ReadInt32();
            return Enum.ToObject(type, enumValue);
        }
        
        if (type == typeof(byte)) return reader.ReadByte();
        if (type == typeof(ushort)) return reader.ReadUInt16();
        if (type == typeof(int)) return reader.ReadInt32();
        if (type == typeof(float)) return reader.ReadSingle();
        if (type == typeof(double)) return reader.ReadDouble();
        if (type == typeof(bool)) return reader.ReadBoolean();
        if (type == typeof(string)) return reader.ReadString();
        
        throw new ArgumentException("Unsupported type");
    }
}