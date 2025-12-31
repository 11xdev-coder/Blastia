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
    
    public static void HandleBlockChanged(string updateBase64, HSteamNetConnection senderConnection) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null || NetworkManager.Instance == null) return;

        NetworkSync.HandleNetworkMessage<NetworkBlockChangeMessage>(updateBase64, MessageType.BlockChanged,
        (update) => worldState.SetTile((int)update.Position.X, (int)update.Position.Y, update.BlockId, update.Layer, null),
        (update, sender) =>
        {
            // find client player
            // first CSteamId with value matching connection
            var clientId = NetworkManager.Instance.Connections.FirstOrDefault(kvp => kvp.Value == sender).Key;
            var clientPlayer = _playersFactory?.Invoke().FirstOrDefault(p => p.SteamId == clientId);
            worldState.SetTile((int)update.Position.X, (int)update.Position.Y, update.BlockId, update.Layer, clientPlayer);
        }, senderConnection, true); // force broadcast to sender
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

            // dont send updates if block should break
            if (inst == null || inst.HasRequestedBreak) continue;
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
    
    public static void HandleBlockUpdated(string updateBase64, HSteamNetConnection senderConnection) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        var world = _worldFactory?.Invoke();
        if (worldState == null || NetworkManager.Instance == null || world == null) return;

        NetworkSync.HandleNetworkMessage<NetworkBlockUpdateMessage>(updateBase64, MessageType.BlockUpdate,
        (update) => ApplyBlockUpdateLocally(update, worldState, world),
        (update, sender) => ApplyBlockUpdateLocally(update, worldState, world), senderConnection);
    }
    
    private static void ApplyBlockUpdateLocally(NetworkBlockUpdateMessage update, WorldState worldState, World world) 
    {
        var blockInstance = worldState.GetBlockInstance((int)update.OriginalPosition.X, (int)update.OriginalPosition.Y, update.Layer);
        if (blockInstance != null)
        {
            blockInstance.Block.ForceUpdate = true;
            blockInstance.Damage = update.Damage;
            
            blockInstance.Update(world, update.OriginalPosition);
        }
    }
    
    public static void HandleSignEdited(string editBase64, HSteamNetConnection senderConnection) 
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null || NetworkManager.Instance == null) return;

        NetworkSync.HandleNetworkMessage<NetworkSignEditedMessage>(editBase64, MessageType.SignEditedAt,
        (edit) => worldState.SignTexts[edit.Position] = edit.NewText,
        (edit, sender) => worldState.SignTexts[edit.Position] = edit.NewText, senderConnection);
    }
}