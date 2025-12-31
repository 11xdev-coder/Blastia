using Assimp.Configs;
using Blastia.Main.Entities;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using NAudio.MediaFoundation;
using Steamworks;

namespace Blastia.Main.Networking;

public static class NetworkEntitySync
{
    private static Func<Player?>? _myPlayerFactory;
    private static Func<List<Player>>? _playersFactory;
    private static Action<Player>? _addToPlayersListMethod;
    private static Func<List<Entity>>? _entitiesFactory;
    private static Action<Entity>? _addToEntitiesListMethod;
    private static Func<World?>? _worldFactory;
    private static Action<Entity>? _removeEntityAction;

    public static void Initialize(Func<Player?> myPlayerFactory, Func<List<Player>> playersFactory, Action<Player> addToPlayersAction, Func<List<Entity>> entitiesFactory,
        Action<Entity> addToEntitiesAction, Func<World?> getWorldFactory, Action<Entity> removeEntityAction)
    {
        _myPlayerFactory = myPlayerFactory;
        _playersFactory = playersFactory;
        _addToPlayersListMethod = addToPlayersAction;
        _entitiesFactory = entitiesFactory;
        _addToEntitiesListMethod = addToEntitiesAction;
        _worldFactory = getWorldFactory;
        _removeEntityAction = removeEntityAction;
    }

    /// <summary>
    /// Must be called when a new client has joined (host only). Spawns a new <c>Player</c> at spawn and tells him about all existing players
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientConnection"></param>
    public static void OnClientJoined(CSteamID clientId, HSteamNetConnection clientConnection)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || PlayerNWorldManager.Instance.SelectedWorld == null
        || _worldFactory == null || _addToPlayersListMethod == null || _playersFactory == null || _myPlayerFactory == null || _entitiesFactory == null) return;

        var clientName = SteamFriends.GetFriendPersonaName(clientId);

        // Create a not locally controlled player for the client
        var clientPlayer = new Player(PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint(), _worldFactory(), Entity.PlayerScale, false)
        {
            SteamId = clientId,
            LocallyControlled = false,
            Name = clientName,
            NetworkPosition = PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint()
        };

        _addToPlayersListMethod(clientPlayer);

        Console.WriteLine($"[NetworkEntitySync] [HOST] Created client player '{clientPlayer.Name}' (ID: {clientPlayer.SteamId})");

        // list all players for debugging
        var allPlayers = _playersFactory();
        Console.WriteLine($"[NetworkEntitySync] [HOST] All players after adding client:");
        foreach (var p in allPlayers)
        {
            Console.WriteLine($"  - Player '{p.Name}' (ID: {p.SteamId}, LocallyControlled: {p.LocallyControlled})");
        }

        // serialize
        var clientPlayerBytes = clientPlayer.GetNetworkData().Serialize();
        var clientPlayerBase64 = Convert.ToBase64String(clientPlayerBytes);

        // tell all players that new client joined
        foreach (var kvp in NetworkManager.Instance.Connections)
        {
            if (kvp.Value != clientConnection)
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerSpawned, clientPlayerBase64);
        }

        // send existing players to this connected client
        foreach (var player in _playersFactory().Where(p => !p.LocallyControlled))
        {
            if (player.SteamId == clientId) continue; // skip own player

            // create network data and serialize
            var playerBytes = player.GetNetworkData().Serialize();
            var playerBase64 = Convert.ToBase64String(playerBytes);

            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, playerBase64);
        }

        // send host player new client
        var hostPlayer = _myPlayerFactory?.Invoke();
        if (hostPlayer != null)
        {
            var hostPlayerBytes = hostPlayer.GetNetworkData().Serialize();
            var hostPlayerBase64 = Convert.ToBase64String(hostPlayerBytes);
            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, hostPlayerBase64);
            Console.WriteLine($"[NetworkEntitySync] [HOST] Sent host player {hostPlayer.Name} to client {SteamFriends.GetFriendPersonaName(clientId)}");
        }

        Console.WriteLine($"[NetworkEntitySync] [HOST] Client {SteamFriends.GetFriendPersonaName(clientId)} joined, sent existing players and created new player for them");

        // send every entity to new client
        foreach (var entity in _entitiesFactory())
        {
            if (entity.NetworkId == Guid.Empty)
                entity.AssignNetworkId();
                
            var entityBytes = entity.GetNetworkData().Serialize();
            var entityBase64 = Convert.ToBase64String(entityBytes);

            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.EntitySpawned, entityBase64);
        }

        Console.WriteLine($"[NetworkEntitySync] [HOST] Client {SteamFriends.GetFriendPersonaName(clientId)} joined, sent existing entities");
    }
    
    private static void ApplyToPlayer(NetworkPlayer player, CSteamID clientId) 
    {
        if (NetworkManager.Instance == null || _playersFactory == null || player.SteamId == NetworkManager.Instance.MySteamId) return;
        
        var allPlayers = _playersFactory();
        var existingPlayer = allPlayers.FirstOrDefault(p => p.SteamId == clientId);
        if (existingPlayer == null) 
        {
            var str = NetworkManager.Instance.IsHost ? "HOST" : "CLIENT";
            Console.WriteLine($"[NetworkEntitySync] [{str}] Client player with ID {clientId} (name '{player.Name}) not found");
            return;
        }

        player.ApplyToEntity(existingPlayer);
    }
    
    public static void HandlePlayerPositionUpdate(string playerBase64, CSteamID clientId, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null || _playersFactory == null) return;
        
        NetworkSync.HandleNetworkMessage<NetworkPlayer>(playerBase64, MessageType.PlayerPositionUpdate,
            (player) => ApplyToPlayer(player, clientId),
            (player, _) => ApplyToPlayer(player, clientId));
    }

    public static void HandlePlayerSpawned(string playerBase64) 
    {
        if (NetworkManager.Instance == null || _playersFactory == null || _worldFactory == null || _addToPlayersListMethod == null) return;

        NetworkSync.HandleNetworkMessage<NetworkPlayer>(playerBase64, MessageType.None,
        (player) =>
        {
            if (player.SteamId == NetworkManager.Instance.MySteamId) return;

            var allPlayers = _playersFactory();
            var existingPlayer = allPlayers.FirstOrDefault(p => p.SteamId == player.SteamId);
            if (existingPlayer != null)
            {
                Console.WriteLine($"[NetworkEntitySync] [CLIENT] Player {player.Name} already exists");
                return;
            }

            var newPlayer = new Player(Vector2.Zero, _worldFactory(), Entity.PlayerScale, false);
            player.ApplyToEntity(newPlayer);
            _addToPlayersListMethod(newPlayer);
        });
    }

    public static void SyncMyPlayerPosition() 
    {
        if (NetworkManager.Instance == null || _myPlayerFactory == null) return;

        var myPlayer = _myPlayerFactory();
        if (myPlayer == null) return;

        var data = myPlayer.GetNetworkData();
        NetworkSync.Sync(data, MessageType.PlayerPositionUpdate, SyncMode.Auto);
    }
    
    private static void CreateEntity(NetworkEntity entity) 
    {
        if (NetworkManager.Instance == null || _entitiesFactory == null || _worldFactory == null || _addToEntitiesListMethod == null) return;
        
        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == entity.NetworkId);
        if (existingEntity != null) 
        {
            var str = NetworkManager.Instance.IsHost ? "HOST" : "CLIENT";
            Console.WriteLine($"[NetworkEntitySync] [{str}] Error: entity with network ID: {entity.NetworkId} already exists!");
            return;
        }

        var world = _worldFactory();
        if (world == null) return;

        var newEntity = Entity.CreateEntity(entity.Id, entity.Position, world);
        if (newEntity == null) return;

        entity.ApplyToEntity(newEntity);
        _addToEntitiesListMethod(newEntity);
    }
    
    public static void HandleEntitySpawned(string entityBase64, HSteamNetConnection senderConnection) 
    {
        NetworkSync.HandleNetworkMessage<NetworkEntity>(entityBase64, MessageType.EntitySpawned,
        CreateEntity, (entity, _) => CreateEntity(entity), senderConnection, true);
    }
    
    public static void SyncNewEntity(Entity entity, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null) return;

        if (NetworkManager.Instance.IsHost && entity.NetworkId == Guid.Empty)
            entity.AssignNetworkId();

        var data = entity.GetNetworkData();
        NetworkSync.Sync(data, MessageType.EntitySpawned, SyncMode.Auto, senderConnection);
    }
    
    /// <summary>
    /// Sends all entities data from host to every client (host only)
    /// </summary>
    public static void SyncEntitiesToClients()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _entitiesFactory == null || _myPlayerFactory == null) return;

        if (NetworkManager.Instance.Connections.Count == 0) return; // no clients to sync to

        var entities = _entitiesFactory();
        foreach (var entity in entities)
        {
            try
            {
                var entityBytes = entity.GetNetworkData().Serialize();
                var entityBase64 = Convert.ToBase64String(entityBytes);

                foreach (var kvp in NetworkManager.Instance.Connections)
                {
                    NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.EntityPositionUpdate, entityBase64);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkEntitySync] [HOST] Error syncing entity with ID: {entity.GetId()} (network ID: {entity.NetworkId}): {ex.Message}");
            }
        }
    }
    
    public static void HandleEntityPositionUpdate(string entityBase64) 
    {
        if (_entitiesFactory == null) return;
        
        NetworkSync.HandleNetworkMessage<NetworkEntity>(entityBase64, MessageType.EntityPositionUpdate,
        (entity) => 
        {
            var allEntities = _entitiesFactory();
            var existingEntity = allEntities.FirstOrDefault(e => e.NetworkId == entity.NetworkId);
            if (existingEntity != null)
            {
                entity.ApplyToEntity(existingEntity);
            }
            else
            {
                Console.WriteLine($"[NetworkEntitySync] [CLIENT] Warning: Entity with ID: {entity.Id} (network ID: {entity.NetworkId}) not found, nothing to update!");
            }
        });
    }
    
    public static void SyncEntityRemoved(Guid entityNetworkId, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null) return;

        NetworkSync.Sync(entityNetworkId, MessageType.EntityKilled, SyncMode.Auto, senderConnection);
    }
    
    private static void RemoveEntity(Guid entityGuid) 
    {
        if (NetworkManager.Instance == null || _removeEntityAction == null || _entitiesFactory == null) return;
        
        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == entityGuid);
        if (existingEntity == null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [CLIENT] Error: entity with network ID: {entityGuid} not found, nothing to remove!");
            return;
        }
        else 
        {
            _removeEntityAction(existingEntity);
        }
    }
    
    public static void HandleEntityKilled(string guidBase64) 
    {
        NetworkSync.HandleNetworkMessage<Guid>(guidBase64, MessageType.EntityKilled,
        RemoveEntity, (guid, _) => RemoveEntity(guid));
    }
    
    public static void SyncItemPull(Guid droppedItemId, ulong pullerId, bool isPulling) 
    {
        var itemPull = new NetworkItemPullMessage
        {
            DroppedItemNetworkId = droppedItemId,
            PullerId = pullerId,
            IsPulling = isPulling
        };

        NetworkSync.Sync(itemPull, MessageType.ItemPull, SyncMode.Auto);
    }
    
    private static void ApplyItemPull(NetworkItemPullMessage itemPull) 
    {
        if (_entitiesFactory == null || _playersFactory == null) return;
        
        var droppedItems = _entitiesFactory().OfType<DroppedItem>();
        var droppedItem = droppedItems.FirstOrDefault(e => e.NetworkId == itemPull.DroppedItemNetworkId);
        if (droppedItem == null) return;

        if (itemPull.IsPulling)
        {
            var players = _playersFactory();
            var puller = players.FirstOrDefault(p => p.SteamId.m_SteamID == itemPull.PullerId);
            if (puller == null) return;

            droppedItem.StartPull(puller);
        }
        else
        {
            droppedItem.StopPull();
        }       
    }
    
    public static void HandleItemPull(string itemPullBase64) 
    {
        NetworkSync.HandleNetworkMessage<NetworkItemPullMessage>(itemPullBase64, MessageType.ItemPull,
        ApplyItemPull, (itemPull, _) => ApplyItemPull(itemPull));
    }
}