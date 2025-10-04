using System.Runtime.InteropServices;
using System.Text;
using Blastia.Main.Blocks.Common;
using Blastia.Main.Utilities.ListHandlers;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Steamworks;

namespace Blastia.Main.Networking;

public enum MessageType : byte
{
    None,
    // basic connection messages
    ClientHello, // client -> host
    HostWelcome, // host -> client welcome message
    PlayerJoinedGame, // host -> all clients "player joined"
    PlayerLeftGame, // host -> all clients "player left"
    
    // game state messages
    RequestUpdateWorldForClient, // client -> host, requests world update
    PlayerSpawned, // host -> all clients, sends new player data
    PlayerPositionUpdate, // new player state -> host received: applies & broadcasts to all clients; client received: applies
    BlockChanged, // client -> host (block change request), host -> all clients (broadcasts)
    SignEditedAt, // new sign text (from client or host) at position
    BlockUpdate, // host -> all clients (single block updated)
    EntitySpawned, // host -> clients (when new entity spawned)
    EntityPositionUpdate, // new NetworkEntity -> client received: applies
    EntityKilled, // GUID of entity removed
    ItemPull,
    ChatMessage,
    
    // world transfer
    WorldTransferStart, // host -> client, start of world transfer
    WorldChunk, // host -> client, contains a chunk of world data
    WorldTransferComplete // client -> host, received final chunk (create a player)
}

/// <summary>
/// Main place for handling networking (multiplayer) logic
/// </summary>
public class NetworkManager
{
    // message pool
    private readonly IntPtr[] _messageBuffer = new IntPtr[32];
    
    public static NetworkManager? Instance { get; set; }

    private bool _isConnectedToHost;
    public bool IsHost { get; private set; }
    public bool IsSteamInitialized { get; private set; }
    public bool IsInMultiplayerSession { get; private set; }
    public bool IsConnected => IsSteamInitialized && IsInMultiplayerSession && (IsHost || _isConnectedToHost);
    public CSteamID MySteamId { get; private set; }

    /// <summary>
    /// The current lobby code (only set when hosting)
    /// </summary>
    public string? CurrentLobbyCode { get; private set; }
    private CSteamID _currentLobbyId;
    
    // key: SteamId of the player, value: connection
    public readonly Dictionary<CSteamID, HSteamNetConnection> Connections = [];
    private HSteamListenSocket _listenSocket;
    /// <summary>
    /// Groups all connections together
    /// </summary>
    private HSteamNetPollGroup _pollGroup;

    private const uint AppId = 480;
    
    // callbacks
    private Callback<LobbyCreated_t>? _lobbyCreatedCallback;
    private Callback<LobbyEnter_t>? _lobbyEnterCallback;
    private Callback<LobbyChatUpdate_t>? _lobbyChatUpdateCallback;
    private Callback<SteamNetConnectionStatusChangedCallback_t>? _connectionStatusChangedCallback;
    private Callback<LobbyMatchList_t>? _lobbyMatchListCallback;
    
    // entity syncing
    private float _lastPlayerSync;
    private const float PlayerSyncRate = 1f / 10f; // 10 times in 1 second
    private float _lastEntitySync;
    private const float EntitySyncRate = 1f / 5f; // 5 times in 1 second

    public NetworkManager()
    {
        if (Instance != null)
        {
            Console.WriteLine("[NetworkManager] Instance already exists!");
            return;
        }
        
        Instance = this;
    }

    public bool InitializeSteam()
    {
        try
        {
            Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());

            if (SteamAPI.InitEx(out var msg) != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Console.WriteLine($"[NetworkManager] SteamAPI.InitEx() failed! {msg}");
                return false;
            }
            
            MySteamId = SteamUser.GetSteamID();
            Console.WriteLine($"[NetworkManager] Steam initialized for: {SteamFriends.GetPersonaName()} (ID: {MySteamId})");
            
            // initialize callbacks
            _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
            _lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
            
            // initialize networking
            SteamNetworkingUtils.InitRelayNetworkAccess();
            _pollGroup = SteamNetworkingSockets.CreatePollGroup();
            
            SetupGracefulShutdown();
            
            IsSteamInitialized = true;
            IsInMultiplayerSession = false;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[NetworkManager] Steam initialization failed: {e}");
            IsSteamInitialized = false;
            IsInMultiplayerSession = false;
            return false;
        }
    }

    private void SetupGracefulShutdown()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("[NetworkManager] [SHUTDOWN] Ctrl+C detected, shutting down...");
            e.Cancel = true; // prevent immediate exit
            GracefulShutdown();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Console.WriteLine("[NetworkManager] [SHUTDOWN] Process exit detected, shutting down...");
            GracefulShutdown();
        };
    }

    private void GracefulShutdown()
    {
        if (!IsSteamInitialized) return;
        
        Console.WriteLine("[NetworkManager] [SHUTDOWN] Performing graceful shutdown");
        DisconnectFromLobby();
        SteamAPI.Shutdown();
        IsSteamInitialized = false;
    }

    public void Update()
    {
        if (!IsSteamInitialized) return;

        _lastPlayerSync += (float) BlastiaGame.GameTimeElapsedSeconds;
        if (_lastPlayerSync >= PlayerSyncRate)
        {
            NetworkEntitySync.SyncMyPlayerPosition();
            _lastPlayerSync = 0f;
        }
        
        _lastEntitySync += (float) BlastiaGame.GameTimeElapsedSeconds;
        if (_lastEntitySync >= EntitySyncRate)
        {
            if (IsHost) 
            {
                NetworkEntitySync.SyncEntitiesToClients();
            }
            _lastEntitySync = 0f;
        }
        
        NetworkMessageQueue.ProcessQueue();
        
        SteamAPI.RunCallbacks();
        ReceiveNetworkMessages();
    }

    /// <summary>
    /// Generates random 6-character lobby code
    /// </summary>
    private string GenerateLobbyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[6];

        for (var i = 0; i < 6; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(code);
    }

    public void HostGame()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Console.WriteLine("[NetworkManager] Cannot host: Steam is not running");
            return;
        }

        if (IsHost || _currentLobbyId != CSteamID.Nil)
        {
            Console.WriteLine("[NetworkManager] Already hosting or in a lobby");
            return;
        }
        
        Console.WriteLine("[NetworkManager] Creating lobby");
        IsHost = true;
        CurrentLobbyCode = GenerateLobbyCode();
        
        // create lobby
        var handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 128);
        // callback will handle the rest (OnLobbyCreated)
    }

    public void JoinLobbyWithCode(string code)
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Console.WriteLine("[NetworkManager] Cannot join: Steam is not running");
            return;
        }
        
        if (_currentLobbyId != CSteamID.Nil)
        {
            Console.WriteLine("[NetworkManager] Already in a lobby");
            return;
        }

        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            BlastiaGame.JoinGameMenu?.UpdateStatusText("Invalid lobby code format");
            Console.WriteLine("[NetworkManager] Invalid lobby code format (must be 6 characters)");
            return;
        }
        
        Console.WriteLine($"[NetworkManager] Searching for a lobby with code: {code.ToUpper()}");
        IsHost = false;
        
        // search for lobbies with this code
        SteamMatchmaking.AddRequestLobbyListStringFilter("LobbyCode", code.ToUpper(), ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter("GameVersion", "0.1.0", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
        // callback will handle joining (OnLobbyMatchList)
    }
    
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        Console.WriteLine($"[NetworkManager] Found {callback.m_nLobbiesMatching} lobbies with matching code");

        if (callback.m_nLobbiesMatching > 0)
        {
            // join first found
            var lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
            var lobbyCode = SteamMatchmaking.GetLobbyData(lobbyId, "LobbyCode");
            Console.WriteLine($"[NetworkManager] Found lobby with code: {lobbyCode}");
            JoinLobby(lobbyId);
        }
        else
        {
            BlastiaGame.JoinGameMenu?.UpdateStatusText("No lobbies found");
            Console.WriteLine("[NetworkManager] No lobbies was found (is the code correct?)");
        }
    }

    private void JoinLobby(CSteamID lobbyId)
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Console.WriteLine("[NetworkManager] Cannot join: Steam is not running");
            BlastiaGame.JoinGameMenu?.UpdateStatusText("Steam is not running");
            return;
        }

        if (_currentLobbyId != CSteamID.Nil)
        {
            Console.WriteLine("[NetworkManager] Already in a lobby");
            return;
        }
        
        Console.WriteLine($"[NetworkManager] Joining lobby: {lobbyId}");
        IsHost = false;
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            // reset all flags on failure
            Console.WriteLine($"[NetworkManager] Lobby creation failed: {callback.m_eResult}");
            IsHost = false;
            IsInMultiplayerSession = false;
            CurrentLobbyCode = null;
            _isConnectedToHost = false;
            return;
        }
        
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Console.WriteLine($"[NetworkManager] Lobby creation successful: {_currentLobbyId}");
        Console.WriteLine($"[NetworkManager] ------------------ LOBBY CODE: {CurrentLobbyCode} ------------------");
        Console.WriteLine($"[NetworkManager] Share this code with friends");
        
        // set lobby data
        SteamMatchmaking.SetLobbyData(_currentLobbyId, "LobbyCode", CurrentLobbyCode);
        SteamMatchmaking.SetLobbyData(_currentLobbyId, "HostName", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(_currentLobbyId, "GameVersion", "0.1.0");
        
        // create listen socket for incoming connections
        var options = Array.Empty<SteamNetworkingConfigValue_t>();
        // any port
        _listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, options);
        
        if (_listenSocket == HSteamListenSocket.Invalid)
        {
            // reset all flags
            Console.WriteLine("[NetworkManager] ERROR: Failed to create listen socket!");
            IsHost = false;
            IsInMultiplayerSession = false;
            CurrentLobbyCode = null;
            return;
        }
    
        Console.WriteLine($"[NetworkManager] Listen socket created: {_listenSocket}");
        Console.WriteLine("[NetworkManager] Host is ready to accept connections");
        
        // everything is up
        IsInMultiplayerSession = true;
    }
    
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != (uint) EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Console.WriteLine($"[NetworkManager] Failed to enter lobby: {callback.m_EChatRoomEnterResponse}");
            IsHost = false;
            IsInMultiplayerSession = false;
            _isConnectedToHost = false;
            return;
        }
        
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Console.WriteLine($"[NetworkManager] Lobby entered: {_currentLobbyId}");
        
        // lobby info
        var lobbyCode = SteamMatchmaking.GetLobbyData(_currentLobbyId, "LobbyCode");
        var hostName = SteamMatchmaking.GetLobbyData(_currentLobbyId, "HostName");
        Console.WriteLine($"[NetworkManager] Joined lobby with code: {lobbyCode}, hosted by: {hostName}");
        
        // get lobby owner
        var lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        Console.WriteLine($"[NetworkManager] Lobby owner: {SteamFriends.GetFriendPersonaName(lobbyOwner)}");
        
        // is in multiplayer session but not connected to host yet
        IsInMultiplayerSession = true;
        
        // connect only to host (client-server)
        if (!IsHost && lobbyOwner != MySteamId)
        {
            ConnectToPlayer(lobbyOwner);
        }
    }
    
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        var lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        var userChanged = new CSteamID(callback.m_ulSteamIDUserChanged);
        var userMakingChange = new CSteamID(callback.m_ulSteamIDMakingChange);

        if (callback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeEntered)
        {
            // clients only connect to host
            // if we're client, dont connect to other clients
            // if we're host, wait for incoming connections
            Console.WriteLine($"[NetworkManager] User joined: {SteamFriends.GetFriendPersonaName(userChanged)}");
            if (!IsHost && userChanged == MySteamId)
            {
                // we joined
                // logic handling in OnLobbyEntered
                return;
            }
            // host waits for incoming connections
        }
        else if (callback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeLeft)
        {
            //Console.WriteLine($"[NetworkManager] User left: {SteamFriends.GetFriendPersonaName(userChanged)}");

            if (IsHost && userChanged != MySteamId)
            {
                NotifyPlayerLeft(userChanged, "disconnected");
            }
            
            if (Connections.TryGetValue(userChanged, out var connection))
            {
                SteamNetworkingSockets.CloseConnection(connection, 0, "User left lobby", false);
                Connections.Remove(userChanged);
            }
        }
    }
    
    private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
    {
        Console.WriteLine($"[NetworkManager] Connection status changed: {callback.m_info.m_eState}");

        if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
        {
            // accept connection if host
            if (IsHost)
            {
                Console.WriteLine("[NetworkManager] Host accepting incoming connection");
                var result = SteamNetworkingSockets.AcceptConnection(callback.m_hConn);
                if (result != EResult.k_EResultOK)
                {
                    Console.WriteLine($"[NetworkManager] Failed to accept connection: {result}");
                    return;
                }
                
                SteamNetworkingSockets.SetConnectionPollGroup(callback.m_hConn, _pollGroup);
                
                // find which steam ID this connection belongs to
                var remoteSteamId = callback.m_info.m_identityRemote.GetSteamID();
                Connections[remoteSteamId] = callback.m_hConn;
            }
        }
        else if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            Console.WriteLine("[NetworkManager] Connection established");
            
            // send hello message
            if (!IsHost)
            {
                _isConnectedToHost = true;
                NetworkMessageQueue.QueueMessage(callback.m_hConn, MessageType.ClientHello, $"Hello from {SteamFriends.GetPersonaName()}");
                NetworkMessageQueue.QueueMessage(callback.m_hConn, MessageType.RequestUpdateWorldForClient, "host send me the world!!!");
            }
        }
        else if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer ||
                 callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
        {
            Console.WriteLine($"[NetworkManager] Connection closed: {callback.m_info.m_szEndDebug}");
            
            // remove from connections
            var remoteSteamId = callback.m_info.m_identityRemote.GetSteamID();
            if (IsHost && remoteSteamId != MySteamId && Connections.ContainsKey(remoteSteamId))
            {
                NotifyPlayerLeft(remoteSteamId, "lost connection");
            }
            Connections.Remove(remoteSteamId);
            
            // more reliable to check it here since connection is lost
            if (!IsHost && Connections.Count == 0) // ensure no connections (connection lost)
            {
                Console.WriteLine("[NetworkManager] Lost connection to host, leaving lobby");
                _isConnectedToHost = false;
                DisconnectFromLobby();
            }
        }
    }
    
    private void ConnectToPlayer(CSteamID playerSteamId)
    {
        Console.WriteLine($"[NetworkManager] Connecting to player: {SteamFriends.GetFriendPersonaName(playerSteamId)}");

        var identity = new SteamNetworkingIdentity();
        identity.SetSteamID(playerSteamId);

        var options = Array.Empty<SteamNetworkingConfigValue_t>();
        var connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, options);

        if (connection != HSteamNetConnection.Invalid)
        {
            Connections[playerSteamId] = connection;
            SteamNetworkingSockets.SetConnectionPollGroup(connection, _pollGroup);
        }
    }
    
    /// <summary>
    /// Tries to send a message to connection
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="type"></param>
    /// <param name="content"></param>
    /// <returns><c>EResult</c> of the message. If the message was sent returns <c>EResult.k_EResultOK</c></returns>
    public EResult TrySendMessage(HSteamNetConnection connection, MessageType type, string? content)
    {
        var messageTypeByte = (byte) type;
        var contentBytes = Encoding.UTF8.GetBytes(content ?? "");
        var messageData = new byte[1 + contentBytes.Length];
        
        messageData[0] = messageTypeByte;
        Buffer.BlockCopy(contentBytes, 0, messageData, 1, contentBytes.Length);
        
        // allocate memory for message data
        var messageDataPtr = Marshal.AllocHGlobal(messageData.Length);

        try
        {
            Marshal.Copy(messageData, 0, messageDataPtr, messageData.Length);

            var result = SteamNetworkingSockets.SendMessageToConnection(connection, messageDataPtr,
                (uint) messageData.Length,
                Constants.k_nSteamNetworkingSend_Reliable, out var messageNumber);

            return result;
        }
        finally
        {
            // free memory
            Marshal.FreeHGlobal(messageDataPtr);
        }
    }

    /// <summary>
    /// Syncs a new chat message for all clients
    /// </summary>
    public void SyncChatMessage(string content, string? senderName) 
    {
        var chatMessage = new NetworkChatMessage
        {
            Text = content,
            SenderName = senderName
        };
        NetworkSync.Sync(chatMessage, MessageType.ChatMessage, SyncMode.Auto);
    }

    /// <summary>
    /// Handles <c>ChatMessage</c> network message
    /// </summary>
    public void HandleChatMessage(string chatMessageBase64, HSteamNetConnection senderConnection) 
    {
        NetworkSync.HandleNetworkMessage<NetworkChatMessage>(chatMessageBase64, MessageType.ChatMessage,
        ApplyChatMessageLocally, (chatMessage, sender) => ApplyChatMessageLocally(chatMessage), senderConnection);
    }
    
    /// <summary>
    /// Applies a chat message from network
    /// </summary>
    private void ApplyChatMessageLocally(NetworkChatMessage chatMessage) 
    {
        BlastiaGame.ChatMessagesMenu?.AddMessage(chatMessage.SenderName, chatMessage.Text, false);
    }

    private void ReceiveNetworkMessages()
    {
        if (_pollGroup == HSteamNetPollGroup.Invalid) return;

        // reuse same array
        Array.Clear(_messageBuffer, 0, _messageBuffer.Length);
        var messageCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, _messageBuffer, 32);

        for (var i = 0; i < messageCount; i++)
        {
            var message = SteamNetworkingMessage_t.FromIntPtr(_messageBuffer[i]);

            try
            {
                if (message.m_cbSize < 1) continue;

                var data = new byte[message.m_cbSize];
                Marshal.Copy(message.m_pData, data, 0, message.m_cbSize);

                var type = (MessageType) data[0];
                var content = "";
                if (data.Length > 1)
                {
                    content = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                }

                // log received message
                switch (type)
                {
                    case MessageType.WorldTransferStart:
                    case MessageType.WorldChunk:
                        Console.WriteLine($"[NetworkManager] Received {type} from {message.m_conn} (not logging content)");
                        break;
                    case MessageType.PlayerPositionUpdate:
                    case MessageType.EntityPositionUpdate:
                        break;
                    default:
                        Console.WriteLine($"[NetworkManager] Received {type}: {content} from {message.m_conn}");
                        break;
                }

                // handle it
                var senderConnection = message.m_conn;
                switch (type)
                {
                    case MessageType.ClientHello:
                        Console.WriteLine($"[NetworkManager] Host received ClientHello: {content}");
                        // send welcome back
                        NetworkMessageQueue.QueueMessage(message.m_conn, MessageType.HostWelcome, "Welcome to the game!");
                        break;
                    case MessageType.HostWelcome:
                        Console.WriteLine($"[NetworkManager] Client received HostWelcome: {content}");
                        break;
                    case MessageType.ChatMessage:
                        HandleChatMessage(content, senderConnection);
                        break;
                    case MessageType.PlayerLeftGame:
                        // just a notification
                        // OnConnectionStatusChanged handles host disconnect
                        ProcessPlayerLeftGameLocally(content);
                        break;
                    case MessageType.PlayerSpawned:
                        NetworkEntitySync.HandlePlayerSpawned(content);
                        break;
                    case MessageType.PlayerPositionUpdate:
                        var senderId = Connections.FirstOrDefault(k => k.Value == senderConnection).Key;
                        NetworkEntitySync.HandlePlayerPositionUpdate(content, senderId, senderConnection);
                        break;
                    case MessageType.BlockChanged:
                        NetworkBlockSync.HandleBlockChanged(content, senderConnection);
                        break;
                    case MessageType.BlockUpdate:
                        NetworkBlockSync.HandleBlockUpdated(content, senderConnection);
                        break;
                    case MessageType.SignEditedAt:
                        NetworkBlockSync.HandleSignEdited(content, senderConnection);
                        break;
                    case MessageType.EntitySpawned:
                        NetworkEntitySync.HandleEntitySpawned(content, senderConnection);
                        break;
                    case MessageType.EntityPositionUpdate:
                        NetworkEntitySync.HandleEntityPositionUpdate(content);
                        break;
                    case MessageType.EntityKilled:
                        NetworkEntitySync.HandleEntityKilled(content);
                        break;
                    case MessageType.ItemPull:
                        NetworkEntitySync.HandleItemPull(content);
                        break;
                    case MessageType.RequestUpdateWorldForClient:
                        // if this is the host, send the world to client
                        if (IsHost)
                            NetworkWorldTransfer.SerializeWorldForConnection(message.m_conn, IsHost);
                        break;
                    case MessageType.WorldTransferStart:
                        NetworkWorldTransfer.HandleWorldTransferStart(content, IsHost);
                        break;
                    case MessageType.WorldChunk:
                        NetworkWorldTransfer.HandleWorldChunk(content, IsHost);
                        break;
                    case MessageType.WorldTransferComplete:
                        NetworkWorldTransfer.HandleWorldTransferComplete(message.m_identityPeer.GetSteamID(), senderConnection);
                        break;
                    default:
                        Console.WriteLine($"[NetworkManager] Unknown message type: {type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetworkManager] Error processing message: {e.Message}");
            }
            finally
            {
                SteamNetworkingMessage_t.Release(_messageBuffer[i]);
            }
        }
    }

    public void DisconnectFromLobby()
    {
        Console.WriteLine("[NetworkManager] Disconnecting from lobby...");
        
        // send msg to clients before leaving
        if (IsHost && Connections.Count > 0)
        {
            foreach (var connection in Connections.Values)
            {
                NetworkMessageQueue.QueueMessage(connection, MessageType.PlayerLeftGame, "Host is leaving");
                // flush messages to ensure it gets sent
                SteamNetworkingSockets.FlushMessagesOnConnection(connection);
            }
            
            Thread.Sleep(200);
        }
        
        // close every connection
        foreach (var connection in Connections.Values)
        {
            var reason = IsHost ? "Host left the game" : "Client disconnected";
            SteamNetworkingSockets.CloseConnection(connection, 0, reason, false);
        }
        Connections.Clear();
        
        // close listen socket
        if (_listenSocket != HSteamListenSocket.Invalid)
        {
            SteamNetworkingSockets.CloseListenSocket(_listenSocket);
            _listenSocket = HSteamListenSocket.Invalid;
        }

        // destroy poll group
        if (_pollGroup != HSteamNetPollGroup.Invalid)
        {
            SteamNetworkingSockets.DestroyPollGroup(_pollGroup);
            _pollGroup = SteamNetworkingSockets.CreatePollGroup();
        }
        
        // leave lobby
        if (_currentLobbyId != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;
        }

        CurrentLobbyCode = null;
        IsHost = false;
        IsInMultiplayerSession = false;
        _isConnectedToHost = false;
    }

    /// <summary>
    /// Notifies all clients that <c>leavingPlayer</c> has left
    /// </summary>
    /// <param name="leavingPlayer"></param>
    /// <param name="reason"></param>
    private void NotifyPlayerLeft(CSteamID leavingPlayer, string reason = "")
    {
        if (!IsHost || Connections.Count == 0) return;
        
        var playerName = SteamFriends.GetFriendPersonaName(leavingPlayer);
        var message = string.IsNullOrEmpty(reason)
            ? $"{playerName} left the game"
            : $"{playerName} left the game ({reason})";
        
        Console.WriteLine($"[NetworkManager] [PLAYER LEFT] Notifying all clients: {message}");

        // process on host locally too
        ProcessPlayerLeftGameLocally(reason);
        // send to all clients
        foreach (var kvp in Connections.ToList())
        {
            if (kvp.Key != leavingPlayer)
            {
                NetworkMessageQueue.QueueMessage(kvp.Value, MessageType.PlayerLeftGame, message);
                SteamNetworkingSockets.FlushMessagesOnConnection(kvp.Value);
            }
        }
    }

    private void ProcessPlayerLeftGameLocally(string content)
    {
        Console.WriteLine($"[NetworkManager] [PLAYER LEFT] Player left the game: {content}");
    }

    public void Shutdown()
    {
        Console.WriteLine("[NetworkManager] [SHUTDOWN] Shutting down...");
        GracefulShutdown();
    }
}