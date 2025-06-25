using Blastia.Main.Blocks.Common;
using Blastia.Main.Utilities;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Steamworks;

namespace Blastia.Main.Networking;


/// <summary>
/// Data structure for a single world chunk
/// </summary>
[Serializable]
public class WorldChunk
{
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public TileLayer Layer { get; set; }
    public Dictionary<Vector2, ushort> Tiles { get; set; } = [];
    public Dictionary<Vector2, NetworkBlockInstance> Instances { get; set; } = [];
}

/// <summary>
/// Data sent at the start of world transfer
/// </summary>
[Serializable]
public class WorldTransferData
{
    public string WorldName { get; set; } = "";
    public WorldDifficulty Difficulty { get; set; }
    public int WorldWidth { get; set; }
    public int WorldHeight { get; set; }
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public Dictionary<Vector2, string> SignTexts { get; set; } = [];
    public int TotalChunksToSend { get; set; }
}

public static class NetworkWorldTransfer
{
    private const int MaxTilesAtOnce = 25;
    private static WorldState? _clientWorldStateBuffer;
    private static int _expectedChunks;
    private static int _receivedChunks;
    private static Dictionary<int, WorldChunk> _receivedWorldChunks = [];

    private static byte[] SerializeChunk(WorldChunk chunk)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(chunk.ChunkIndex);
            writer.Write(chunk.TotalChunks);
            writer.Write((byte) chunk.Layer);
            Saving.WriteTileDictionary(chunk.Tiles, writer);
            
            writer.Write(chunk.Instances.Count);
            foreach (var kvp in chunk.Instances)
            {
                writer.Write(kvp.Key.X);
                writer.Write(kvp.Key.Y);
                kvp.Value.Serialize(stream, writer);
            }
        }

        return stream.ToArray();
    }

    private static WorldChunk DeserializeChunk(byte[] chunkBytes)
    {
        var chunk = new WorldChunk();
            
        using var stream = new MemoryStream(chunkBytes);
        using (var reader = new BinaryReader(stream))
        {
            chunk.ChunkIndex = reader.ReadInt32();
            chunk.TotalChunks = reader.ReadInt32();
            chunk.Layer = (TileLayer) reader.ReadByte();
            chunk.Tiles = Saving.ReadTileDictionary(reader);
            
            var instDict = new Dictionary<Vector2, NetworkBlockInstance>(reader.ReadInt32());
            for (var i = 0; i < instDict.Count; i++)
            {
                var vector = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                var inst = new NetworkBlockInstance().Deserialize(reader);
                
                instDict.Add(vector, inst);
            }

            chunk.Instances = instDict;
        }

        return chunk;
    }
    
    /// <summary>
    /// Called whenever <c>RequestUpdateWorldForClient</c> message is received and this is the host
    /// </summary>
    /// <param name="connection">Client that requested the world</param>
    /// <param name="isHost"></param>
    /// <param name="sendMessage">Event to send the message to client containing world chunks</param>
    public static void SerializeWorldForConnection(HSteamNetConnection connection, bool isHost, Action<HSteamNetConnection, MessageType, string?> sendMessage)
    {
        if (!isHost) return;

        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null)
        {
            Console.WriteLine("[NetworkWorldTransfer] No world selected, cannot send data");
            return;
        }
        
        Console.WriteLine("[NetworkWorldTransfer] Starting world transfer");

        // create basic data first
        var data = new WorldTransferData
        {
            WorldName = worldState.Name,
            Difficulty = worldState.Difficulty,
            WorldWidth = worldState.WorldWidth,
            WorldHeight = worldState.WorldHeight,
            SpawnX = worldState.SpawnX,
            SpawnY = worldState.SpawnY,
            SignTexts = worldState.SignTexts
        };
        
        var allChunks = new List<WorldChunk>();
        
        // create all chunks for all layers
        var groundChunks = CreateChunksForLayer(worldState.GroundTiles, worldState.GroundInstances, TileLayer.Ground);
        allChunks.AddRange(groundChunks);
        var liquidChunks = CreateChunksForLayer(worldState.LiquidTiles, worldState.LiquidInstances, TileLayer.Liquid);
        allChunks.AddRange(liquidChunks);
        var furnitureChunks = CreateChunksForLayer(worldState.FurnitureTiles, worldState.FurnitureInstances, TileLayer.Furniture);
        allChunks.AddRange(furnitureChunks);
        
        // update data total chunks
        data.TotalChunksToSend = allChunks.Count;
        // send basic data
        var dataJson = JsonConvert.SerializeObject(data);
        sendMessage(connection, MessageType.WorldTransferStart, dataJson);
        
        Console.WriteLine($"[NetworkWorldTransfer] Sending {allChunks.Count} chunks total:");
        Console.WriteLine($"  - Ground: {groundChunks.Count} chunks");
        Console.WriteLine($"  - Liquid: {liquidChunks.Count} chunks");
        Console.WriteLine($"  - Furniture: {furnitureChunks.Count} chunks");
        
        for (var i = 0; i < allChunks.Count; i++)
        {
            var chunk = allChunks[i];
            chunk.ChunkIndex = i;
            chunk.TotalChunks = allChunks.Count;
            
            var chunkBytes = SerializeChunk(chunk);
            var chunkBase64 = Convert.ToBase64String(chunkBytes);
            sendMessage(connection, MessageType.WorldChunk, chunkBase64);
            
            // add a small delay to prevent overwhelming the connection
            if (i % 10 == 0) // 10ms delay every 10 chunks
            {
                SteamNetworkingSockets.FlushMessagesOnConnection(connection);
                Thread.Sleep(10);
            }
        }
        
        // send completion message
        sendMessage(connection, MessageType.WorldTransferComplete, $"All {allChunks.Count} chunks sent");
        SteamNetworkingSockets.FlushMessagesOnConnection(connection);
        
        Console.WriteLine("[NetworkWorldTransfer] World transfer completed for client");
    }

    /// <summary>
    /// Transfers all tiles of <c>layer</c> to list of world chunks (max length = 100 tiles per chunk)
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="instances"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    private static List<WorldChunk> CreateChunksForLayer(Dictionary<Vector2, ushort> tiles, Dictionary<Vector2, BlockInstance> instances,
        TileLayer layer)
    {
        var chunks = new List<WorldChunk>();

        if (tiles.Count == 0)
        {
            // even if empty send one empty chunk
            chunks.Add(new WorldChunk
            {
                Layer = layer,
                Tiles = [],
                Instances = []
            });
            return chunks;
        }
        
        var currentChunk = new WorldChunk
        {
            Layer = layer,
            Tiles = [],
            Instances = []
        };
        var tilesInCurrentChunk = 0;

        foreach (var kvp in tiles)
        {
            var position = kvp.Key;
            var tileId = kvp.Value;

            currentChunk.Tiles[position] = tileId;
            if (instances.TryGetValue(position, out var inst))
            {
                var networkInst = new NetworkBlockInstance();
                networkInst.FromBlockInstance(inst);
                currentChunk.Instances[position] = networkInst;
            }

            tilesInCurrentChunk += 1;
            
            // if exceeded max tiles, create a new chunk
            if (tilesInCurrentChunk >= MaxTilesAtOnce)
            {
                chunks.Add(currentChunk);
                
                currentChunk = new WorldChunk
                {
                    Layer = layer,
                    Tiles = [],
                    Instances = []
                };
                tilesInCurrentChunk = 0;
            }
        }

        // left some tiles remaining -> add the last chunk
        if (tilesInCurrentChunk > 0)
        {
            chunks.Add(currentChunk);
        }

        return chunks;
    }

    /// <summary>
    /// Called whenever <c>WorldTransferStart</c> message is received (client-side)
    /// </summary>
    public static void HandleWorldTransferStart(string worldDataJson, bool isHost)
    {
        // client-side
        if (isHost) return;
        
        var worldData = JsonConvert.DeserializeObject<WorldTransferData>(worldDataJson);
        if (worldData == null) return;
        
        Console.WriteLine($"[NetworkWorldTransfer] Starting world transfer for '{worldData.WorldName}' with {worldData.TotalChunksToSend} chunks");
        
        // initialize buffer
        _clientWorldStateBuffer = new WorldState
        {
            Name = worldData.WorldName,
            Difficulty = worldData.Difficulty,
            WorldWidth = worldData.WorldWidth,
            WorldHeight = worldData.WorldHeight,
            SpawnX = worldData.SpawnX,
            SpawnY = worldData.SpawnY,
            SignTexts = worldData.SignTexts
        };
        
        _expectedChunks = worldData.TotalChunksToSend;
        _receivedChunks = 0;
        _receivedWorldChunks.Clear();
    }

    /// <summary>
    /// Called whenever <c>WorldChunk</c> message is received (client-side)
    /// </summary>
    public static void HandleWorldChunk(string chunkBase64, bool isHost)
    {
        // client-side
        if (isHost || _clientWorldStateBuffer == null) return;
        
        var chunkBytes = Convert.FromBase64String(chunkBase64);
        var chunk = DeserializeChunk(chunkBytes);

        // add chunk to buffer
        _receivedWorldChunks[chunk.ChunkIndex] = chunk;
        _receivedChunks += 1;

        Console.WriteLine($"[NetworkWorldTransfer] Received chunk {chunk.ChunkIndex + 1}/{_expectedChunks} for layer {chunk.Layer}");
        
        // all chunks received
        if (_receivedChunks >= _expectedChunks)
        {
            ReconstructWorld();
        }
    }

    private static void ReconstructWorld()
    {
        if (_clientWorldStateBuffer == null) return;
        
        Console.WriteLine("[NetworkWorldTransfer] All chunks received, reconstructing world");

        for (var i = 0; i < _expectedChunks; i++)
        {
            if (_receivedWorldChunks.TryGetValue(i, out var chunk))
            {
                // apply chunk data to appropriate layer
                switch (chunk.Layer)
                {
                    case TileLayer.Ground:
                        foreach (var kvp in chunk.Tiles)
                            _clientWorldStateBuffer.GroundTiles[kvp.Key] = kvp.Value;
                        
                        foreach (var kvp in chunk.Instances)
                            _clientWorldStateBuffer.GroundInstances[kvp.Key] = kvp.Value.ToBlockInstance() ?? throw new NullReferenceException("[NetworkWorldTransfer] Block instance cannot be null");
                        break;
                    case TileLayer.Liquid:
                        foreach (var kvp in chunk.Tiles)
                            _clientWorldStateBuffer.LiquidTiles[kvp.Key] = kvp.Value;
                        
                        foreach (var kvp in chunk.Instances)
                            _clientWorldStateBuffer.LiquidInstances[kvp.Key] = kvp.Value.ToBlockInstance() ?? throw new NullReferenceException("[NetworkWorldTransfer] Block instance cannot be null");
                        break;
                    case TileLayer.Furniture:
                        foreach (var kvp in chunk.Tiles)
                            _clientWorldStateBuffer.FurnitureTiles[kvp.Key] = kvp.Value;
                        
                        foreach (var kvp in chunk.Instances)
                            _clientWorldStateBuffer.FurnitureInstances[kvp.Key] = kvp.Value.ToBlockInstance() ?? throw new NullReferenceException("[NetworkWorldTransfer] Block instance cannot be null");
                        break;
                }
            }
        }
        
        Console.WriteLine("[NetworkWorldTransfer] World reconstruction completed");
        Console.WriteLine($"  - Ground: {_clientWorldStateBuffer.GroundTiles.Count} tiles");
        Console.WriteLine($"  - Liquid: {_clientWorldStateBuffer.LiquidTiles.Count} tiles");
        Console.WriteLine($"  - Furniture: {_clientWorldStateBuffer.FurnitureTiles.Count} tiles");
        
        // apply
        PlayerNWorldManager.Instance.SelectWorld(_clientWorldStateBuffer, false);
        
        // cleanup
        _clientWorldStateBuffer = null;
        _receivedWorldChunks.Clear();
        _expectedChunks = 0;
        _receivedChunks = 0;
    }
}