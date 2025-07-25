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
    
    public static void SyncBlockChange(Vector2 position, ushort newId, TileLayer layer, Player? player) 
    {
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
        
        var blockChange = new NetworkBlockChangeMessage
        {
            Position = position,
            BlockId = newId,
            Layer = layer,
            PlayerName = playerName,
            PlayerSteamId = playerId
        };
        NetworkSync.Sync(blockChange, MessageType.BlockChanged, SyncMode.Auto);
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
            var blockChange = NetworkBlockChangeMessage.Deserialize(blockChangeBytes);

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
            var blockChange = NetworkBlockChangeMessage.Deserialize(blockChangeBytes);

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
    /// <c>Host</c> -> broadcast blocks update on a new position, <c>client</c> sends block update from an old position
    /// </summary>
    public static void SyncBlockUpdate(WorldState worldState, Vector2 newPos, Vector2 originalPos) 
    {
        if (NetworkManager.Instance == null) return;

        // each tile layer
        foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer)))
        {
            BlockInstance? inst;
            if (NetworkManager.Instance.IsHost) // host -> new block position
                inst = worldState.GetBlockInstance((int)newPos.X, (int)newPos.Y, layer);
            else // client -> old block position
                inst = worldState.GetBlockInstance((int)originalPos.X, (int)originalPos.Y, layer);

            if (inst == null) continue;
            var update = new NetworkBlockUpdateMessage
            {
                OriginalPosition = originalPos,
                NewPosition = newPos,
                Damage = inst.Damage,
                Layer = layer
            };
            NetworkSync.Sync(update, MessageType.BlockUpdate, SyncMode.Auto);
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
            var update = NetworkBlockUpdateMessage.Deserialize(updateBytes);

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
            var update = NetworkBlockUpdateMessage.Deserialize(updateBytes);

            ApplyBlockUpdateLocally(update, true);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] [CLIENT] Error handling block update from host: {ex.Message}");
        }
    }
    
    private static void ApplyBlockUpdateLocally(NetworkBlockUpdateMessage update, bool isClient) 
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
    
    /// <summary>
    /// Handles <c>SignEdtedAt</c> message (host only)
    /// </summary>
    public static void HandleClientSignEdited(string editBase64, HSteamNetConnection senderConnection) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || worldState == null) return;
        
        try 
        {
            var editBytes = Convert.FromBase64String(editBase64);
            var edit = NetworkSignEditedMessage.Deserialize(editBytes);

            worldState.SignTexts[edit.Position] = edit.NewText;

            var signEdited = new NetworkSignEditedMessage
            {
                Position = edit.Position,
                NewText = edit.NewText
            };
            NetworkSync.Sync(signEdited, MessageType.SignEditedAt, SyncMode.Auto, senderConnection);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] Error handling sign edit message: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles <c>SignEdtedAt</c> message (client only)
    /// </summary>
    public static void HandleSignEditedFromHost(string editBase64) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || worldState == null) return;
        
        try 
        {
            var editBytes = Convert.FromBase64String(editBase64);
            var edit = NetworkSignEditedMessage.Deserialize(editBytes);

            worldState.SignTexts[edit.Position] = edit.NewText;
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[NetworkBlockSync] Error handling sign edit message: {ex.Message}");
        }
    }
}