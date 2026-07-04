using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
        { typeof(byte),    (w, v) => w.Write((byte) v) },
        { typeof(ushort),  (w, v) => w.Write((ushort) v) },
        { typeof(int),     (w, v) => w.Write((int) v) },
        { typeof(float),   (w, v) => w.Write((float) v) },
        { typeof(double),  (w, v) => w.Write((double) v) },
        { typeof(bool),    (w, v) => w.Write((bool) v) },
        { typeof(string),  (w, v) => w.Write((string) v) },
        { typeof(long),    (w, v) => w.Write((long) v) },
        { typeof(ulong),   (w, v) => w.Write((ulong) v) },
        { typeof(Vector2), (w, v) => { Vector2 vec = (Vector2) v; w.Write(vec.X); w.Write(vec.Y); }}
    };
    
    private static Dictionary<Type, Func<BinaryReader, object>> _primitiveReaders = new() 
    {
        { typeof(byte),    r => r.ReadByte() },
        { typeof(ushort),  r => r.ReadUInt16() },
        { typeof(int),     r => r.ReadInt32() },
        { typeof(float),   r => r.ReadSingle() },
        { typeof(double),  r => r.ReadDouble() },
        { typeof(bool),    r => r.ReadBoolean() },
        { typeof(string),  r => r.ReadString() },
        { typeof(long),    r => r.ReadInt64() },
        { typeof(ulong),   r => r.ReadUInt64() },
        { typeof(Vector2), r => new Vector2(r.ReadSingle(), r.ReadSingle()) }
    };
    
    private static Func<Vector2, bool> _vector2ValidChecker = v => { return !float.IsNaN(v.X) && !float.IsNaN(v.Y) && !float.IsInfinity(v.X) && !float.IsInfinity(v.Y); };
    private static Func<string, bool> _stringValidChecker = s => !string.IsNullOrEmpty(s);
    
    /// <summary>
    /// Writes some &lt;TKey, TValue&gt; dictionary to <c>BinaryWriter</c>
    /// </summary>
    /// <param name="keyValidationChecker">A function that must return true if we should write the key and it's valid</param>
    /// <param name="valueValidationChecker">A function that must return true if we should write the value and it's valid (<c>value != null</c> check is already included in the method itself)</param>
    public static void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, BinaryWriter writer, 
        Func<TKey, bool>? keyValidationChecker = null, Func<TValue, bool>? valueValidationChecker = null) where TKey : notnull
    {
        Dictionary<TKey, TValue> valid = [];
        foreach (var kvp in dict) 
        {
            if (keyValidationChecker != null && !keyValidationChecker(kvp.Key)) 
            {
                if (EnableLogs) 
                    Console.WriteLine($"Writing dictionary of type: <{typeof(TKey).Name}, {typeof(TValue).Name}> - key {kvp.Key} wasn't valid by provided validation checker.");
                
                // skip invalid elements
                continue;
            }
            
            // not optional, mandatory
            if (kvp.Value == null) 
            {
                if (EnableLogs) 
                    Console.WriteLine($"Writing dictionary of type: <{typeof(TKey).Name}, {typeof(TValue).Name}> - value is null while writing, skipping.");
                continue;
            }
            
            if (valueValidationChecker != null && !valueValidationChecker(kvp.Value)) 
            {
                if (EnableLogs) 
                    Console.WriteLine($"Writing dictionary of type: <{typeof(TKey).Name}, {typeof(TValue).Name}> - value {kvp.Value} wasn't valid by provided validation checker.");
                continue;
            }
            
            // add valid elements
            valid.Add(kvp.Key, kvp.Value);
        }
        
        // either way write amount of valid elements, since read method always reads this
        writer.Write(valid.Count);
        
        if (valid.Count == 0) 
        {
            if (EnableLogs)
                Console.WriteLine($"Writing dictionary of type: <{typeof(TKey).Name}, {typeof(TValue).Name}> - 0 valid values, aborting.");
            return;
        }
        
        if (EnableLogs)
            Console.WriteLine($"Writing dictionary of type: <{typeof(TKey).Name}, {typeof(TValue).Name}> - writing {valid.Count} items.");
            
        foreach (var kvp in valid) 
        {
            if (EnableSpamLogs)
                Console.WriteLine($"Writing dictionary of type <{typeof(TKey).Name}, {typeof(TValue).Name}>: (Key: {kvp.Key}, Value: {kvp.Value})");
                
            WriteObject(writer, kvp.Key);
            WriteObject(writer, kvp.Value!);
        }
    }
    
    public static void WriteVector2BlockInstDictionary(Dictionary<Vector2, BlockInstance> dict, BinaryWriter writer) => WriteDictionary(dict, writer, _vector2ValidChecker);
    public static void WriteVector2StringDictionary(Dictionary<Vector2, string> dict, BinaryWriter writer) => WriteDictionary(dict, writer, _vector2ValidChecker, _stringValidChecker);

    public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(BinaryReader reader) where TKey : notnull
    {
        // writer doesnt allow invalid elements
        // reader should be dumb - read everything without checking
        
        var resultDict = new Dictionary<TKey, TValue>();
        int count = reader.ReadInt32();
        
        if (EnableLogs)
            Console.WriteLine($"Reading dictionary of type <{typeof(TKey).Name}, {typeof(TValue).Name}> with {count} items from FileStream position: {reader.BaseStream.Position}");
            
        for (int i = 0; i < count; i++) 
        {
            TKey key = (TKey) ReadObject(reader, typeof(TKey));
            TValue value = (TValue) ReadObject(reader, typeof(TValue));
            
            if (EnableSpamLogs) 
                Console.WriteLine($"Successfully read entry {i} from dictionary of type <{typeof(TKey).Name}, {typeof(TValue).Name}>: (Key: {key}, Value: {value})");
                
            resultDict.Add(key, value);
        }
        
        return resultDict;
    }
    
    public static Dictionary<Vector2, BlockInstance> ReadVector2BlockInstDictionary(BinaryReader reader) => ReadDictionary<Vector2, BlockInstance>(reader);
    public static Dictionary<Vector2, string> ReadVector2StringDictionary(BinaryReader reader) => ReadDictionary<Vector2, string>(reader);
    
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
                inst.Write(writer);
                break;
            case TileLayerData tld:
                tld.Write(writer);
                break;
            case Dictionary<Vector2, BlockInstance> d:
                WriteVector2BlockInstDictionary(d, writer);
                break;
            case Dictionary<Vector2, string> d:
                WriteVector2StringDictionary(d, writer);
                break;
            // fallback call for dictionaries, slower than known types
            case IDictionary dict when type.IsGenericType // if the type is dictionary
            && type.GetGenericTypeDefinition() == typeof(Dictionary<,>): // check if it is a Dictionary<,> with 2 type parameters
                Type[] args = type.GetGenericArguments();
                Type keyType = args[0]; // get the key type
                Type valueType = args[1]; // get the value type
                
                // find the method by name and creates a generic version like WriteDictionary<keyType, valueType>
                var method = typeof(Saving).GetMethod(nameof(WriteDictionary))!.MakeGenericMethod(keyType, valueType);
                method.Invoke(null, [dict, writer, null, null]);
                break;
            default:
                Console.WriteLine($"Tried to save unsupported type: {value.GetType().FullName}");
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
            return BlockInstance.Read(reader);
        }
        
        if (type == typeof(TileLayerData)) 
        {
            return TileLayerData.Read(reader);
        }
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) 
        {
            Type[] args = type.GetGenericArguments();
            Type keyType = args[0];
            Type valueType = args[1];
            var method = typeof(Saving).GetMethod(nameof(ReadDictionary))!.MakeGenericMethod(keyType, valueType);
            
            try 
            {
                return method.Invoke(null, [reader])!;
            }
            catch (Exception e) 
            {
                if (e.InnerException != null)
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
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