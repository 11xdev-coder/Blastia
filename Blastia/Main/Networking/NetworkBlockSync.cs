using Blastia.Main.Blocks;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Steamworks;

namespace Blastia.Main.Networking;

public static class NetworkBlockSync
{
    private static Func<List<Player>>? _playersFactory;
    private static Func<World?>? _worldFactory;
    private static Func<Player?>? _myPlayerFactory;
    
    public static void Initialize(Func<List<Player>> playersFactory, Func<World?> worldFactory, Func<Player?> myPlayerFactory)
    {
        _playersFactory = playersFactory;
        _worldFactory = worldFactory;
        _myPlayerFactory = myPlayerFactory;
    }
    
    /// <summary>
    /// Sends <c>BlockChanged</c> message to host (client only) - call whenever block is changed on client
    /// </summary>
    /// <param name="position"></param>
    /// <param name="newId">0 when block is destroyed</param>
    /// <param name="layer"></param>
    /// <param name="player"></param>
    public static void SendBlockChangedToHost(Vector2 position, ushort newId, TileLayer layer, Player? player) 
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost) return;

        string playerName;
        ulong playerId;
        if (player == null)
        {
            playerName = "NULL";
            playerId = 0;
        }
        else 
        {
            playerName = player.Name;
            playerId = player.SteamId.m_SteamID;
        }

        var blockChange = new NetworkBlockChange
        {
            Position = position,
            BlockId = newId,
            Layer = layer,
            PlayerName = playerName,
            PlayerSteamId = playerId
        };

        var blockChangeBytes = blockChange.Serialize();
        var blockChangeBase64 = Convert.ToBase64String(blockChangeBytes);

        // host connection is the first
        var hostConnection = NetworkManager.Instance.Connections.Values.FirstOrDefault();
        if (hostConnection != HSteamNetConnection.Invalid) 
        {
            NetworkMessageQueue.QueueMessage(hostConnection, MessageType.BlockChanged, blockChangeBase64);
            Console.WriteLine($"[NetworkBlockSync] [CLIENT] Sent block placement at {position}: {newId} in {layer} by '{playerName}'");
        }
    }
    
    /// <summary>
    /// Sends <c>BlockChanged</c> message to all clients (host only) - call whenever block is changed on host
    /// </summary>
    public static void BroadcastBlockChangedToClients(Vector2 position, ushort newId, TileLayer layer, Player? player) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost) return;

        string playerName;
        ulong playerId;
        if (player == null)
        {
            playerName = "NULL";
            playerId = 0;
        }
        else 
        {
            playerName = player.Name;
            playerId = player.SteamId.m_SteamID;
        }
        
        var blockChange = new NetworkBlockChange
        {
            Position = position,
            BlockId = newId,
            Layer = layer,
            PlayerName = playerName,
            PlayerSteamId = playerId
        };

        var blockChangeBytes = blockChange.Serialize();
        var blockChangeBase64 = Convert.ToBase64String(blockChangeBytes);
        
        foreach (var connection in NetworkManager.Instance.Connections.Values) 
        {
            NetworkMessageQueue.QueueMessage(connection, MessageType.BlockChanged, blockChangeBase64);
            Console.WriteLine($"[NetworkBlockSync] [HOST] Broadcasted all clients new block at {position}: {newId} in {layer}");
        }
    }
    
    /// <summary>
    /// Called when <c>BlockChanged</c> message is received and broadcasts to all clients (host only)
    /// </summary>
    public static void HandleClientBlockChanged(string blockChange64, CSteamID clientId) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost) return;
        
        try 
        {
            var blockChangeBytes = Convert.FromBase64String(blockChange64);
            var blockChange = NetworkBlockChange.Deserialize(blockChangeBytes);

            // find client 
            var clientPlayer = _playersFactory?.Invoke().FirstOrDefault(p => p.SteamId == clientId);
            if (clientPlayer == null) return;

            // apply locally
            var worldState = PlayerNWorldManager.Instance.SelectedWorld;
            if (worldState != null) 
            {
                worldState.SetTile((int)blockChange.Position.X, (int)blockChange.Position.Y, blockChange.BlockId, blockChange.Layer, clientPlayer);
                Console.WriteLine($"[NetworkBlockSync] [HOST] Applied changes from client {clientPlayer.Name} at {blockChange.Position}");
                
                // broadcast
                foreach (var connection in NetworkManager.Instance.Connections.Values) 
                {
                    NetworkMessageQueue.QueueMessage(connection, MessageType.BlockChanged, blockChange64);
                }
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] [HOST] Error: handling block changed {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called whenever <c>BlockChanged</c> message is received (client only)
    /// </summary>
    public static void HandleBlockChangedFromHost(string blockChange64) 
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost) return;
        
        try 
        {
            var blockChangeBytes = Convert.FromBase64String(blockChange64);
            var blockChange = NetworkBlockChange.Deserialize(blockChangeBytes);

            // apply locally
            var worldState = PlayerNWorldManager.Instance.SelectedWorld;
            if (worldState != null) 
            {
                worldState.SetTile((int)blockChange.Position.X, (int)blockChange.Position.Y, blockChange.BlockId, blockChange.Layer, null);
                Console.WriteLine($"[NetworkBlockSync] [CLIENT] Received changes from host at {blockChange.Position}");
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] [CLIENT] Error: handling block changed {ex.Message}");
        }
    }
    
    /// <summary>
    /// Sends blocks' positions that were updated this frame(host -> broadcasts to clients)
    /// </summary>
    /// <param name="updatedBlocksPositions"></param>
    public static void BroadcastUpdatedBlocksToClients(HashSet<(Vector2 original, Vector2 newPos)> updatedBlocksPositions) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;

        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;   
           
        try 
        {
            if (NetworkManager.Instance.IsHost) 
            {
                foreach (var t in updatedBlocksPositions)
                    BroadcastBlockUpdate(t.original, t.newPos, HSteamNetConnection.Invalid);
                Console.WriteLine("[NetworkBlockSync] [HOST] Broadcasted block updates to all clients");
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] Error sending block updates: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Broadcasts a single block update to every connection except <c>senderConnection</c> if not nil (host only)
    /// </summary>
    private static void BroadcastBlockUpdate(Vector2 originalPosition, Vector2 newPosition, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost) return;

        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;
        
        foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer))) 
        {
            var newBlock = worldState.GetBlockInstance((int)newPosition.X, (int)newPosition.Y, layer);
        
            NetworkBlockUpdate update;
            if (newBlock == null) 
            {
                // tile not found
                continue;
            }
            else 
            {
                update = new NetworkBlockUpdate
                {
                    OriginalPosition = originalPosition,
                    NewPosition = newPosition,
                    Damage = newBlock.Damage,
                    Layer = layer
                };
            }
            var updateBytes = update.Serialize();
            var updateBase64 = Convert.ToBase64String(updateBytes);

            foreach (var connection in NetworkManager.Instance.Connections.Values)
                if (connection != senderConnection)
                    NetworkMessageQueue.QueueMessage(connection, MessageType.BlockUpdate, updateBase64);
        }
    }
    
    /// <summary>
    /// Sends a block update to host (client only)
    /// </summary>
    public static void SendBlockUpdateToHost(Vector2 originalPosition, Vector2 newPosition) 
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost) return;

        var hostConnection = NetworkManager.Instance.Connections.Values.FirstOrDefault();
        if (hostConnection == HSteamNetConnection.Invalid) return;
        
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;
        
        foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer))) 
        {
            var blockInstance = worldState.GetBlockInstance((int)originalPosition.X, (int)originalPosition.Y, layer);
            if (blockInstance == null) continue; 
        
            var update = new NetworkBlockUpdate
            {
                OriginalPosition = originalPosition,
                NewPosition = newPosition,
                Damage = blockInstance.Damage,
                Layer = layer
            };
            var updateBytes = update.Serialize();
            var updateBase64 = Convert.ToBase64String(updateBytes);
            NetworkMessageQueue.QueueMessage(hostConnection, MessageType.BlockUpdate, updateBase64);
        }
    }
    
    /// <summary>
    /// Called when <c>BlockUpdated</c> message is received (host only)
    /// </summary>
    public static void HandleClientBlockUpdate(string updateBase64, HSteamNetConnection senderConnection) 
    {
        // dont need this now
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost) return;
        
        try 
        {
            var updateBytes = Convert.FromBase64String(updateBase64);
            var update = NetworkBlockUpdate.Deserialize(updateBytes);

            // apply locally
            ApplyBlockUpdateLocally(update, false);
            
            // broadcast to all other clients (excluding the sender)
            foreach (var connection in NetworkManager.Instance.Connections.Values) 
            {
                if (connection != senderConnection)
                    NetworkMessageQueue.QueueMessage(connection, MessageType.BlockUpdate, updateBase64);
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] [HOST] Error: failed to handle block update: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called when <c>BlockUpdated</c> message is received (client only)
    /// </summary>
    public static void HandleBlockUpdateFromHost(string updateBase64) 
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost) return;
    
        try 
        {
            var updateBytes = Convert.FromBase64String(updateBase64);
            var update = NetworkBlockUpdate.Deserialize(updateBytes);

            ApplyBlockUpdateLocally(update, true);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] [CLIENT] Error handling block update from host: {ex.Message}");
        }
    }
    
    private static void ApplyBlockUpdateLocally(NetworkBlockUpdate update, bool isClient) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        var world = _worldFactory?.Invoke();
        if (worldState == null || world == null) return;
        
        // update the block
        var blockInstance = worldState.GetBlockInstance((int)update.OriginalPosition.X, (int)update.OriginalPosition.Y, update.Layer);
        if (blockInstance != null)
        {
            blockInstance.Block.ForceUpdate = true;
            blockInstance.Damage = update.Damage;
            
            blockInstance.Update(world, update.OriginalPosition);
            
            var output = isClient ? "[CLIENT]" : "[HOST]";
            Console.WriteLine($"[NetworkBlockSync] {output} Updated block at {update.OriginalPosition} on layer {update.Layer}");
        }
    }
}