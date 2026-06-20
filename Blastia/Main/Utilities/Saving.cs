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
    public static bool EnableLogs = true;
    public static bool EnableSpamLogs = false;
    
    private static Dictionary<Type, Action<BinaryWriter, object>> _primitiveWriters = new()
    {
        { typeof(byte),   (w, v) => w.Write((byte) v) },
        { typeof(ushort), (w, v) => w.Write((ushort) v) },
        { typeof(int),    (w, v) => w.Write((int) v) },
        { typeof(float),  (w, v) => w.Write((float) v) },
        { typeof(double), (w, v) => w.Write((double) v) },
        { typeof(bool),   (w, v) => w.Write((bool) v) },
        { typeof(string), (w, v) => w.Write((string) v) },
        { typeof(ulong),  (w, v) => w.Write((ulong) v) },
    };
    
    private static Dictionary<Type, Func<BinaryReader, object>> _primitiveReaders = new() 
    {
        { typeof(byte),   r => r.ReadByte() },
        { typeof(ushort), r => r.ReadUInt16() },
        { typeof(int),    r => r.ReadInt32() },
        { typeof(float),  r => r.ReadSingle() },
        { typeof(double), r => r.ReadDouble() },
        { typeof(bool),   r => r.ReadBoolean() },
        { typeof(string), r => r.ReadString() },
        { typeof(ulong),  r => r.ReadUInt64() },
    };
    
    /// <summary>
    /// Writes Dictionary&lt;Vector2, T&gt;
    /// </summary>
    public static void WriteVector2Dictionary<T>(Dictionary<Vector2, T> dict, BinaryWriter writer) 
    {
        if (EnableLogs) 
            Console.WriteLine($"Writing Vector2 dictionary of type <Vector2, {typeof(T).Name}> with {dict.Count} items");
            
        writer.Write(dict.Count);
        foreach (var kvp in dict) 
        {
            Vector2 vector = kvp.Key;
            if (float.NaN == vector.X || float.NaN == vector.Y || float.IsInfinity(vector.X) || float.IsInfinity(vector.Y))
            {
                if (EnableLogs)
                    Console.WriteLine($"Found invalid Vector2 values, skipping.");
                    
                continue;
            }
            
            T value = kvp.Value;
            if (value == null) 
            {
                if (EnableLogs)
                    Console.WriteLine($"Value is null while writing, skipping.");
                continue;
            }
            
            if (EnableSpamLogs) 
                Console.WriteLine($"Writing Dictionary<Vector2, {typeof(T).Name}> entry at {vector}. Value: {value}");
            
            writer.Write(vector.X);
            writer.Write(vector.Y);
            WriteObject(writer, value);
        }
    }
    
    public static void WriteVector2BlockInstDictionary(Dictionary<Vector2, BlockInstance> dict, BinaryWriter writer) => WriteVector2Dictionary(dict, writer);
    public static void WriteVector2StringDictionary(Dictionary<Vector2, string> dict, BinaryWriter writer) => WriteVector2Dictionary(dict, writer);

    public static Dictionary<Vector2, T> ReadVector2Dictionary<T>(BinaryReader reader)
    {
        var resultDict = new Dictionary<Vector2, T>();
        int count = reader.ReadInt32();
        
        if (EnableLogs) 
            Console.WriteLine($"Reading dictionary of type <Vector2, {typeof(T).Name} with {count} entries from FileStream position: {reader.BaseStream.Position}");
            
        for (int i = 0; i < count; i++)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            if (float.IsInfinity(x) || float.IsNaN(x) || float.IsInfinity(y) || float.IsNaN(y))
            {
                if (EnableLogs)
                    Console.WriteLine($"Found invalid Vector2 values, skipping.");
                continue;
            }
            
            T value = (T) ReadObject(reader, typeof(T));
            if (value == null)
            {
                if (EnableLogs)
                    Console.WriteLine($"Value is null while reading, skipping.");
                continue;
            }
            
            Vector2 vector = new Vector2(x, y);
            if (EnableSpamLogs) 
                Console.WriteLine($"Successfully read entry {i} of type Dictionary<Vector2, {typeof(T).Name}>: (Vector: {vector}, Value: {value})");
                
            resultDict.Add(vector, value);
        }
            
        return resultDict;
    }
    
    public static Dictionary<Vector2, BlockInstance> ReadVector2BlockInstDictionary(BinaryReader reader) => ReadVector2Dictionary<BlockInstance>(reader);
    public static Dictionary<Vector2, string> ReadVector2StringDictionary(BinaryReader reader) => ReadVector2Dictionary<string>(reader);
    
    public static void WriteObject(BinaryWriter writer, object value) 
    {
        var type = value.GetType();
        
        if (_primitiveWriters.TryGetValue(type, out var writeAction)) 
        {
            writeAction(writer, value);
            return;
        }
        
        // handle special cases
        switch (value)
        {
            case Enum e:
                writer.Write(Convert.ToInt32(e));
                break;
            case BlockInstance inst:
                writer.Write(inst.Id);
                if (inst.Block is LiquidBlock liquid)
                    writer.Write(liquid.FlowLevel);
                break;
            case Dictionary<Vector2, BlockInstance> d:
                WriteVector2BlockInstDictionary(d, writer);
                break;
            case Dictionary<Vector2, string> d:
                WriteVector2StringDictionary(d, writer);
                break;
            // fallback call for dictionaries, slower than known types
            case IDictionary dict when type.IsGenericType // if the type is dictionary
            && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) // check if it is a Dictionary<,> with 2 type parameters
            && type.GetGenericArguments()[0] == typeof(Vector2): // [0] - key, [1] - value. check if key is Vector2
                var valueType = type.GetGenericArguments()[1]; // get the value type
                
                // find the method by name and creates a generic version like WriteVector2Dictionary<valueType>
                var method = typeof(Saving).GetMethod(nameof(WriteVector2Dictionary))!.MakeGenericMethod(valueType);
                method.Invoke(null, [dict, writer]);
                break;
        }
    }
    
    public static object ReadObject(BinaryReader reader, Type type) 
    {
        if (_primitiveReaders.TryGetValue(type, out var readerFunc)) 
        {
            return readerFunc(reader);
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
                array.SetValue(ReadObject(reader, elementType), i);
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
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) 
        && type.GetGenericArguments()[0] == typeof(Vector2)) 
        {
            Type valueType = type.GetGenericArguments()[1];
            var method = typeof(Saving).GetMethod(nameof(ReadVector2Dictionary))!.MakeGenericMethod(valueType);
            return method.Invoke(null, [reader])!;
        }

        throw new ArgumentException($"Unsupported type: {type.Name} (Full name: {type.FullName})");
    }
    
    /// <summary>
    /// Writes to a file at filePath state's class data. Supports: <c> Dictionary&lt;Vector2, ushort&gt;</c>,
    /// <c> Vector2</c>, <c> ushort[]</c>, <c> enum values</c>, <c> byte</c>, <c> ushort</c>,
    /// <c> int</c>, <c> float</c>, <c> double</c>, <c> bool</c>, <c> string</c>
    /// </summary>
    public static void Save<T>(string filePath, T state)
    {
        // the idea is to save properties with a temp buffer to measure its length
        // write its length so we can correctly skip over the property
        
        if (state == null) return;

        using FileStream fs = File.Open(filePath, FileMode.Create);
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            PropertyInfo[] properties = state.GetType().GetProperties().ToArray();
            
            foreach (PropertyInfo property in properties)
            {
                // skip with NoSave tag
                if (property.GetCustomAttribute<NoSaveAttribute>() != null)
                    continue;
                
                object? value = property.GetValue(state);
                bool present = value != null;
                
                // empty buffer
                byte[] buffer = Array.Empty<byte>();
                if (present) 
                {
                    using MemoryStream tempMS = new MemoryStream();
                    using (BinaryWriter tempWriter = new BinaryWriter(tempMS)) 
                    {
                        WriteObject(tempWriter, value!);
                        tempWriter.Flush(); // writer may keep some bytes so flush them all out
                        buffer = tempMS.ToArray();
                    }                    
                }
                
                // name, present byte, value length and value
                writer.Write(property.Name);
                writer.Write(present);
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
        }
    }

    /// <summary>
    /// Loads state class data (must be empty constructor) from a file and returns state with loaded parameters
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="readCondition">If true - read the property, otherwise skips over it</param>
    /// <typeparam name="T">Serializable state class with empty constructor</typeparam>
    /// <returns>State class with loaded parameters from the file. Throws <c>FileNotFoundException</c> if file is missing. </returns>
    private static T LoadWithCondition<T>(string filePath, Func<PropertyInfo, bool> readCondition) where T : new()
    {
        // here we can just skip over if the property doesnt satisfy read condition
        // dont care about order
        
        T state = new T();
        
        // create a dictionary: property name - property info
        // fixes multiple properties with the same name -> GroupBy gathers into groups from a list
        // e.g given [Prop1, Prop2, Prop3, Prop2] returns = {Prop1 -> [Prop1], Prop2 -> [Prop2, Prop2], Prop3 -> [Prop3]}
        // ToDictionary(Key, Value) -> as key we pass group's key (g.Key) and value = first value of the group
        // so we get = Prop1: Prop1, Prop2: Prop2, Prop3: Prop3 (fixed duplicate names!)
        Dictionary<string, PropertyInfo> map = typeof(T).GetProperties().GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
        
        using FileStream fs = File.Open(filePath, FileMode.Open);
        using (BinaryReader reader = new BinaryReader(fs))
        {
            while (fs.Position < fs.Length) 
            {
                string propertyName = reader.ReadString();
                bool present = reader.ReadBoolean();
                int length = reader.ReadInt32();
                
                long dataStart = fs.Position;
                
                PropertyInfo? info = null;
                bool shouldLoad = present &&
                                    map.TryGetValue(propertyName, out info) && // if we have a property with this name in T class
                                    info.GetCustomAttribute<NoSaveAttribute>() == null && // no 'NoSave' tag
                                    readCondition(info); // we want this property
                                    
                if (shouldLoad && info != null) 
                {
                    object value = ReadObject(reader, info.PropertyType);
                    info.SetValue(state, value);
                }
                
                // move relative to the beginning (pos before read + length = pos after read)
                fs.Seek(dataStart + length, SeekOrigin.Begin);
            }
        }

        return state;
    }
    
    /// <summary>
    /// <inheritdoc cref="LoadWithCondition"/>
    /// </summary>
    public static T Load<T>(string filePath) where T : new() => LoadWithCondition<T>(filePath, (p) => true);
    /// <summary>
    /// Loads only essential properties (marked with <c>EssentialAttribute</c>). For proper load these properties must be listed first in a class declaration
    /// </summary>
    public static T LoadLightweight<T>(string filePath) where T : new() => LoadWithCondition<T>(filePath, (p) => p.GetCustomAttribute<EssentialPropertyAttribute>() != null);
    
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