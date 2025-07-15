using Assimp.Configs;
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

    public static void Initialize(Func<Player?> myPlayerFactory, Func<List<Player>> playersFactory, Action<Player> addToPlayersAction, Func<List<Entity>> entitiesFactory,
        Action<Entity> addToEntitiesAction, Func<World?> getWorldFactory)
    {
        _myPlayerFactory = myPlayerFactory;
        _playersFactory = playersFactory;
        _addToPlayersListMethod = addToPlayersAction;
        _entitiesFactory = entitiesFactory;
        _addToEntitiesListMethod = addToEntitiesAction;
        _worldFactory = getWorldFactory;
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
        var clientPlayer = new Player(PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint(), _worldFactory(), BlastiaGame.PlayerScale, false)
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

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // serialize
        var clientPlayerBytes = clientPlayer.GetNetworkData().Serialize(stream, writer);
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
            var playerBytes = player.GetNetworkData().Serialize(stream, writer);
            var playerBase64 = Convert.ToBase64String(playerBytes);

            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, playerBase64);
        }

        // send host player new client
        var hostPlayer = _myPlayerFactory?.Invoke();
        if (hostPlayer != null)
        {
            var hostPlayerBytes = hostPlayer.GetNetworkData().Serialize(stream, writer);
            var hostPlayerBase64 = Convert.ToBase64String(hostPlayerBytes);
            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, hostPlayerBase64);
            Console.WriteLine($"[NetworkEntitySync] [HOST] Sent host player {hostPlayer.Name} to client {SteamFriends.GetFriendPersonaName(clientId)}");
        }

        Console.WriteLine($"[NetworkEntitySync] [HOST] Client {SteamFriends.GetFriendPersonaName(clientId)} joined, sent existing players and created new player for them");

        // send every entity to new client
        foreach (var entity in _entitiesFactory())
        {
            var entityBytes = entity.GetNetworkData().Serialize();
            var entityBase64 = Convert.ToBase64String(entityBytes);

            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.EntitySpawned, entityBase64);
        }

        Console.WriteLine($"[NetworkEntitySync] [HOST] Client {SteamFriends.GetFriendPersonaName(clientId)} joined, sent existing entities");
    }

    /// <summary>
    /// Broadcasts host's player state to all players (host only)
    /// </summary>
    public static void SyncHostPlayer()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _playersFactory == null || _myPlayerFactory == null) return;

        if (NetworkManager.Instance.Connections.Count == 0) return; // no clients to sync to

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var hostPlayer = _myPlayerFactory?.Invoke();
        if (hostPlayer != null)
        {
            try
            {
                var playerBytes = hostPlayer.GetNetworkData().Serialize(stream, writer);
                var playerBase64 = Convert.ToBase64String(playerBytes);

                foreach (var kvp in NetworkManager.Instance.Connections)
                {
                    NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerPositionUpdate, playerBase64);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkEntitySync] [ERROR] [HOST] Error syncing host player: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Host receives position update from client and broadcasts it to all clients
    /// </summary>
    public static void HandleClientPositionUpdate(string playerBase64, CSteamID clientId)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _playersFactory == null) return;

        try
        {
            var playerBytes = Convert.FromBase64String(playerBase64);
            using var stream = new MemoryStream(playerBytes);
            using var reader = new BinaryReader(stream);
            var networkPlayer = NetworkPlayer.Deserialize(reader);

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
            var oldPosition = clientPlayer.Position;
            networkPlayer.ApplyToEntity(clientPlayer);

            // broadcast to all clients
            using var outStream = new MemoryStream();
            using var outWriter = new BinaryWriter(outStream);
            var updatedPlayerBytes = clientPlayer.GetNetworkData().Serialize(outStream, outWriter);
            var updatedPlayerBase64 = Convert.ToBase64String(updatedPlayerBytes);

            foreach (var kvp in NetworkManager.Instance.Connections)
            {
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerPositionUpdate, updatedPlayerBase64);
            }
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
        using var stream = new MemoryStream(playerBase64);
        using var reader = new BinaryReader(stream);

        var networkPlayer = NetworkPlayer.Deserialize(reader);

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

        var player = new Player(Vector2.Zero, _worldFactory(), BlastiaGame.PlayerScale, false);
        networkPlayer.ApplyToEntity(player);

        _addToPlayersListMethod(player);
    }

    /// <summary>
    /// Sends client network data to host to process and broadcast to all clients (client only)
    /// </summary>
    public static void SendClientPositionToHost()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _myPlayerFactory == null) return;

        var myPlayer = _myPlayerFactory();
        if (myPlayer == null) return;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var networkPlayer = myPlayer.GetNetworkData();
        var playerBytes = networkPlayer.Serialize(stream, writer);
        var playerBase64 = Convert.ToBase64String(playerBytes);

        // send to host (first connection)
        var hostConnection = NetworkManager.Instance.Connections.Values.FirstOrDefault();
        if (hostConnection != HSteamNetConnection.Invalid)
            NetworkMessageQueue.QueueMessage(hostConnection, MessageType.PlayerPositionUpdate, playerBase64);
    }

    /// <summary>
    /// Client receives position update from host
    /// </summary>
    /// <param name="networkPlayerBase64"></param>
    /// <param name="clientId"></param>
    public static void HandlePositionUpdateFromHost(string networkPlayerBase64)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _playersFactory == null || _worldFactory == null || _addToPlayersListMethod == null) return;
        try
        {
            var playerBytes = Convert.FromBase64String(networkPlayerBase64);
            using var stream = new MemoryStream(playerBytes);
            using var reader = new BinaryReader(stream);
            var networkPlayer = NetworkPlayer.Deserialize(reader);

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
                var newPlayer = new Player(Vector2.Zero, _worldFactory(), BlastiaGame.PlayerScale)
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
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _entitiesFactory == null || _addToEntitiesListMethod == null) return;

        var entityBytes = Convert.FromBase64String(entityBase64);
        using var stream = new MemoryStream(entityBytes);
        using var reader = new BinaryReader(stream);
        var networkEntity = NetworkEntity.Deserialize(reader);

        var existingEntity = _entitiesFactory().FirstOrDefault(e => e.NetworkId == networkEntity.NetworkId);
        if (existingEntity != null) 
        {
            Console.WriteLine($"[NetworkEntitySync] [CLIENT] Error: entity with network ID: {networkEntity.NetworkId} already exists!");
            return;
        }

        var entity = new BasicEntity(networkEntity.Position, 1f);
        networkEntity.ApplyToEntity(entity);
        _addToEntitiesListMethod(entity);
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
}