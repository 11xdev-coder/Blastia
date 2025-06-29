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
        var clientPlayer = new Player(PlayerNWorldManager.Instance.SelectedWorld.GetSpawnPoint(), _worldFactory())
        {
            SteamId = clientId,
            LocallyControlled = false,
            Name = SteamFriends.GetFriendPersonaName(clientId)
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

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        // send _myPlayer to every connection
        var myPlayer = _myPlayerFactory();
        if (myPlayer != null)
        {
            var myPlayerBytes = myPlayer.GetNetworkData().Serialize(stream, writer);
            var myPlayerBase64 = Convert.ToBase64String(myPlayerBytes);

            foreach (var kvp in NetworkManager.Instance.Connections)
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerUpdate, myPlayerBase64);
        }

        // send every remote player to every connection
        foreach (var player in _playersFactory())
        {
            var playerBytes = player.GetNetworkData().Serialize(stream, writer);
            var playerBase64 = Convert.ToBase64String(playerBytes);

            foreach (var kvp in NetworkManager.Instance.Connections)
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerUpdate, playerBase64);
        }
    }

    /// <summary>
    /// Handles the <c>PlayerSpawned</c> message (client only)
    /// </summary>
    /// <param name="content"></param>
    public static void HandlePlayerSpawned(string content)
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.IsHost || _worldFactory == null || _addToPlayersListMethod == null) return;

        using var stream = new MemoryStream();
        using var reader = new BinaryReader(stream);
        var networkPlayer = new NetworkPlayer().Deserialize(reader);
        
        // dont create a player for ourselves
        if (networkPlayer.SteamId == NetworkManager.Instance.MySteamId) return;

        var player = new Player(Vector2.Zero, _worldFactory());
        networkPlayer.ApplyToEntity(player);
        
        _addToPlayersListMethod(player);
    }
}