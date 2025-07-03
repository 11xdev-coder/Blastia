using Assimp.Configs;
using Blastia.Main.Entities.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
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
            || _worldFactory == null || _addToPlayersListMethod == null || _playersFactory == null || _myPlayerFactory == null) return;
        
        // add a not locally controlled player
        var clientPlayer = new Player(PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint(), _worldFactory(), BlastiaGame.PlayerScale)
        {
            SteamId = clientId,
            LocallyControlled = false,
            Name = SteamFriends.GetFriendPersonaName(clientId),
            NetworkPosition = PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint()
        };
        _addToPlayersListMethod(clientPlayer);
        
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
        foreach (var player in _playersFactory())
        {
            // create network data and serialize
            var playerBytes = player.GetNetworkData().Serialize(stream, writer);
            var playerBase64 = Convert.ToBase64String(playerBytes);
            
            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, playerBase64);
        }
        
        // send host's _myPlayer to this connected client
        var myPlayer = _myPlayerFactory();
        if (myPlayer != null)
        {
            var myPlayerBytes = myPlayer.GetNetworkData().Serialize(stream, writer);
            var myPlayerBase64 = Convert.ToBase64String(myPlayerBytes);
            
            NetworkMessageQueue.QueueMessage(clientConnection, MessageType.PlayerSpawned, myPlayerBase64);
        }
    }

    /// <summary>
    /// Syncs all player states (host only)
    /// </summary>
    public static void SyncPlayers()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _playersFactory == null || _myPlayerFactory == null) return;
        
        if (NetworkManager.Instance.Connections.Count == 0) return; // No clients to sync to
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        var allPlayers = new List<Player>();
        var myPlayer = _myPlayerFactory();
        if (myPlayer != null)
            allPlayers.Add(myPlayer);
        allPlayers.AddRange(_playersFactory());
        
        // Send each player's data to all clients
        foreach (var player in allPlayers)
        {
            try
            {
                var playerBytes = player.GetNetworkData().Serialize(stream, writer);
                var playerBase64 = Convert.ToBase64String(playerBytes);
                
                foreach (var kvp in NetworkManager.Instance.Connections)
                {
                    NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerUpdate, playerBase64);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkEntitySync] [ERROR] Error syncing player {player.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles <c>PlayerUpdate</c> message, updates player position from host (client only)
    /// </summary>
    public static void HandlePlayerUpdate(string playerBase64)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _playersFactory == null 
            || _worldFactory == null || _addToPlayersListMethod == null) return;
        
        try
        {
            var playerBytes = Convert.FromBase64String(playerBase64);
            using var stream = new MemoryStream(playerBytes);
            using var reader = new BinaryReader(stream);

            var networkPlayer = NetworkPlayer.Deserialize(reader);
            
            // dont update our own player
            if (networkPlayer.SteamId == NetworkManager.Instance.MySteamId) return;
            
            // find first player with that steam ID
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
            Console.WriteLine($"[NetworkEntitySync] [ERROR] Error updating player: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the <c>PlayerSpawned</c> message (client only)
    /// </summary>
    /// <param name="content"></param>
    public static void HandlePlayerSpawned(string content)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _worldFactory == null || _addToPlayersListMethod == null) return;

        var playerBase64 = Convert.FromBase64String(content);
        using var stream = new MemoryStream(playerBase64);
        using var reader = new BinaryReader(stream);
        
        var networkPlayer = NetworkPlayer.Deserialize(reader);
        
        // dont create a player for ourselves
        if (networkPlayer.SteamId == NetworkManager.Instance.MySteamId) return;

        var player = new Player(Vector2.Zero, _worldFactory(), BlastiaGame.PlayerScale);
        networkPlayer.ApplyToEntity(player);
        
        _addToPlayersListMethod(player);
    }

    /// <summary>
    /// Sends client network data to host to process and broadcast to all clients (client only)
    /// </summary>
    public static void SendClientStateToHost()
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
            NetworkMessageQueue.QueueMessage(hostConnection, MessageType.NetworkPlayerUpdateFromClient, playerBase64);
    }

    /// <summary>
    /// Handles player network data from client and broadcasts to all clients (host only)
    /// </summary>
    /// <param name="networkPlayerBase64"></param>
    /// <param name="clientId"></param>
    public static void HandleNetworkPlayerUpdate(string networkPlayerBase64, CSteamID clientId)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsHost || _playersFactory == null) return;
        try
        {
            var playerBytes = Convert.FromBase64String(networkPlayerBase64);
            using var stream = new MemoryStream(playerBytes);
            using var reader = new BinaryReader(stream);
            var networkPlayer = NetworkPlayer.Deserialize(reader);
            
            // find player that sent this message
            var clientPlayer = _playersFactory().FirstOrDefault(p => p.SteamId == clientId);
            if (clientPlayer == null) return;
            
            networkPlayer.ApplyToEntity(clientPlayer);
            
            // Immediately broadcast this update to all clients (including sender for confirmation)
            using var outStream = new MemoryStream();
            using var outWriter = new BinaryWriter(outStream);
            var updatedPlayerBytes = clientPlayer.GetNetworkData().Serialize(outStream, outWriter);
            var updatedPlayerBase64 = Convert.ToBase64String(updatedPlayerBytes);
            
            foreach (var kvp in NetworkManager.Instance.Connections)
            {
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerUpdate, updatedPlayerBase64);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkEntitySync] [ERROR] Error while host was handling client input: {ex.Message}");
        }
    }
}