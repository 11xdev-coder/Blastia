﻿using Assimp.Configs;
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
    
    /// <summary>
    /// Host receives position update from client and broadcasts it to all clients
    /// </summary>
    public static void HandleClientPositionUpdate(string playerBase64, CSteamID clientId, HSteamNetConnection senderConnection)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _playersFactory == null) return;

        try
        {
            var playerBytes = Convert.FromBase64String(playerBase64);
            var networkPlayer = NetworkPlayer.Deserialize(playerBytes);

            var allPlayers = _playersFactory();

            // find the client player that sent this message
            var clientPlayer = allPlayers.FirstOrDefault(p => p.SteamId == clientId);
            if (clientPlayer == null)
            {
                Console.WriteLine($"[NetworkEntitySync] [ERROR] [HOST] Client player with ID {clientId} not found!");
                Console.WriteLine($"[NetworkEntitySync] [HOST] Available players: {string.Join(", ", allPlayers.Select(p => $"{p.Name}({p.SteamId})"))}");
                return;
            }

            // update client
            networkPlayer.ApplyToEntity(clientPlayer);

            foreach (var kvp in NetworkManager.Instance.Connections)
                if (kvp.Value != senderConnection)
                    NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerPositionUpdate, playerBase64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkEntitySync] [ERROR] [HOST] Error handling client position update: {ex.Message}");
            Console.WriteLine($"[NetworkEntitySync] [ERROR] [HOST] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handles the <c>PlayerSpawned</c> message (client only)
    /// </summary>
    /// <param name="content"></param>
    public static void HandlePlayerSpawnedFromHost(string content)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _worldFactory == null || _addToPlayersListMethod == null) return;

        var playerBase64 = Convert.FromBase64String(content);
        var networkPlayer = NetworkPlayer.Deserialize(playerBase64);

        // dont create a player for ourselves
        if (networkPlayer.SteamId == NetworkManager.Instance.MySteamId)
        {
            Console.WriteLine("[NetworkEntitySync] [WARNING] [CLIENT] Skipping creation of own player");
            return;
        }

        // check if already exists
        var existingPlayer = _playersFactory?.Invoke().FirstOrDefault(p => p.SteamId == networkPlayer.SteamId);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[NetworkEntitySync] [WARNING] [CLIENT] Player {networkPlayer.Name} already exists");
            return;
        }

        var player = new Player(Vector2.Zero, _worldFactory(), Entity.PlayerScale, false);
        networkPlayer.ApplyToEntity(player);

        _addToPlayersListMethod(player);
    }

    public static void SyncMyPlayerPosition() 
    {
        if (NetworkManager.Instance == null || _myPlayerFactory == null) return;

        var myPlayer = _myPlayerFactory();
        if (myPlayer == null) return;

        var data = myPlayer.GetNetworkData();
        NetworkSync.Sync(data, MessageType.PlayerPositionUpdate, SyncMode.Auto);
    }

    /// <summary>
    /// Client receives position update from host
    /// </summary>
    /// <param name="networkPlayerBase64"></param>
    /// <param name="clientId"></param>
    public static void HandlePlayerUpdateFromHost(string networkPlayerBase64)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _playersFactory == null || _worldFactory == null || _addToPlayersListMethod == null) return;
        try
        {
            var playerBytes = Convert.FromBase64String(networkPlayerBase64);
            var networkPlayer = NetworkPlayer.Deserialize(playerBytes);

            // dont update our own player
            if (networkPlayer.SteamId == NetworkManager.Instance.MySteamId) return;

            // find player with that steam ID
            var allPlayers = _playersFactory();
            var player = allPlayers.FirstOrDefault(p => p.SteamId == networkPlayer.SteamId);
            if (player != null)
            {
                networkPlayer.ApplyToEntity(player);
            }
            else
            {
                Console.WriteLine($"[NetworkEntitySync] [WARNING] Player with Steam ID: {networkPlayer.SteamId} not found, creating new player");
                var newPlayer = new Player(Vector2.Zero, _worldFactory(), Entity.PlayerScale)
                {
                    LocallyControlled = false
                };
                networkPlayer.ApplyToEntity(newPlayer);
                _addToPlayersListMethod(newPlayer);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkEntitySync] [ERROR] Error while host was handling client input: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles <c>EntitySpawned</c> message (client only)
    /// </summary>
    /// <param name="entityBase64"></param>
    public static void HandleEntitySpawnedFromHost(string entityBase64)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _entitiesFactory == null || _addToEntitiesListMethod == null || _worldFactory == null) return;

        var entityBytes = Convert.FromBase64String(entityBase64);
        var networkEntity = NetworkEntity.Deserialize(entityBytes);

        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == networkEntity.NetworkId);
        if (existingEntity != null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [CLIENT] Error: entity with network ID: {networkEntity.NetworkId} already exists!");
            return;
        }

        var world = _worldFactory();
        if (world == null) return;
        
        var entity = Entity.CreateEntity(networkEntity.Id, networkEntity.Position, world);
        if (entity == null) return;
        
        networkEntity.ApplyToEntity(entity);
        _addToEntitiesListMethod(entity);
    }
    
    /// <summary>
    /// Handles <c>EntitySpawned</c> message (host only)
    /// </summary>
    public static void HandleClientEntitySpawned(string entityBase64, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _entitiesFactory == null || _addToEntitiesListMethod == null || _worldFactory == null) return;

        var entityBytes = Convert.FromBase64String(entityBase64);
        var networkEntity = NetworkEntity.Deserialize(entityBytes);

        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == networkEntity.NetworkId);
        if (existingEntity != null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [CLIENT] Error: entity with network ID: {networkEntity.NetworkId} already exists!");
            return;
        }

        var world = _worldFactory();
        if (world == null) return;
        
        var entity = Entity.CreateEntity(networkEntity.Id, networkEntity.Position, world);
        if (entity == null) return;
        
        networkEntity.ApplyToEntity(entity);
        _addToEntitiesListMethod(entity);

        SyncNewEntity(entity, senderConnection);
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
    
    /// <summary>
    /// Handles <c>EntityPositionUpdate</c> message (client only)
    /// </summary>
    public static void HandleEntityUpdateFromHost(string entityBase64) 
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _entitiesFactory == null || _addToEntitiesListMethod == null || _worldFactory == null) return;
        try
        {
            var entityBytes = Convert.FromBase64String(entityBase64);
            var networkEntity = NetworkEntity.Deserialize(entityBytes);

            // find entity with same network GUID
            var allEntities = _entitiesFactory();
            var entity = allEntities.FirstOrDefault(e => e.NetworkId == networkEntity.NetworkId);
            if (entity != null)
            {
                networkEntity.ApplyToEntity(entity);
            }
            else
            {
                Console.WriteLine($"[NetworkEntitySync] [WARNING] Entity with ID: {networkEntity.Id} (network ID: {networkEntity.NetworkId}) not found, nothing to update!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkEntitySync] [HOST] Error while handling EntityPositionUpdate message: {ex.Message}");
        }
    }
    
    public static void SyncEntityRemoved(Guid entityNetworkId, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null) return;

        NetworkSync.Sync(entityNetworkId, MessageType.EntityKilled, SyncMode.Auto, senderConnection);
    }
    
    /// <summary>
    /// Handles <c>EntityKilled</c> message (client only)
    /// </summary>
    public static void HandleEntityRemovedFromHost(string networkIdBase64)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _removeEntityAction == null || _entitiesFactory == null) return;

        var networkIdBytes = Convert.FromBase64String(networkIdBase64);
        var networkId = new Guid(networkIdBytes);

        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == networkId);
        if (existingEntity == null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [CLIENT] Error: entity with network ID: {networkId} not found, nothing to remove!");
            return;
        }
        else 
        {
            _removeEntityAction(existingEntity);
        }
    }
    
    /// <summary>
    /// Handles <c>EntityKilled</c> message (host only)
    /// </summary>
    public static void HandleClientEntityRemoved(string networkIdBase64, HSteamNetConnection senderConnection) 
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _entitiesFactory == null || _removeEntityAction == null) return;

        var networkIdBytes = Convert.FromBase64String(networkIdBase64);
        var networkId = new Guid(networkIdBytes);

        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == networkId);
        if (existingEntity == null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [HOST] Error: entity with network ID: {networkId} not found, nothing to remove!");
            return;
        }
        else 
        {
            _removeEntityAction(existingEntity);
            SyncEntityRemoved(networkId, senderConnection);
        }        
    }
}