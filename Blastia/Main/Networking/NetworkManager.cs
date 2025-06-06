using System.Runtime.InteropServices;
using System.Text;
using Steamworks;

namespace Blastia.Main.Networking;

public enum MessageType : byte
{
    // basic connection messages
    ClientHello, // client -> host
    HostWelcome, // host -> client welcome message
    PlayerJoinedGame, // host -> all clients "player joined"
    PlayerLeftGame, // host -> all clients "player left"
    
    // game state messages
    PlayerPositionUpdate,
    BlockChanged,
    EntitySpawned,
    EntityKilled,
    ChatMessage
}

/// <summary>
/// Main place for handling networking (multiplayer) logic
/// </summary>
public class NetworkManager
{
    public static NetworkManager Instance { get; private set; }
    
    public bool IsHost { get; private set; }
    /// <summary>
    /// True if client is connected to the host, or if this is host
    /// </summary>
    public bool IsConnected { get; private set; }
    public CSteamID MySteamId { get; private set; }

    private CSteamID _currentLobbyId;
    // key: SteamId of the player, value: connection
    private Dictionary<CSteamID, HSteamNetConnection> _connections = [];
    private HSteamListenSocket _listenSocket;
    /// <summary>
    /// Groups all connections together
    /// </summary>
    private HSteamNetPollGroup _pollGroup;

    private const uint AppId = 480;
    
    // callbacks
    private Callback<LobbyCreated_t> _lobbyCreatedCallback;
    private Callback<LobbyEnter_t> _lobbyEnterCallback;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;
    private Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;

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
            if (!SteamAPI.Init())
            {
                Console.WriteLine("[NetworkManager] SteamAPI.Init() failed!");
                return false;
            }
            
            MySteamId = SteamUser.GetSteamID();
            Console.WriteLine($"[NetworkManager] Steam initialized for: {SteamFriends.GetPersonaName()} (ID: {MySteamId})");
            
            // initialize callbacks
            _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
            
            // initialize networking
            SteamNetworkingUtils.InitRelayNetworkAccess();
            _pollGroup = SteamNetworkingSockets.CreatePollGroup();
            
            IsConnected = true;
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[NetworkManager] Steam initialization failed: {e}");
            IsConnected = false;
            return false;
        }
    }

    public void Update()
    {
        if (!IsConnected) return;
        
        SteamAPI.RunCallbacks();
        ReceiveNetworkMessages();
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
        
        // create lobby
        var handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 128);
        // callback will handle the rest
    }

    public void JoinLobby(CSteamID lobbyId)
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
        
        Console.WriteLine($"[NetworkManager] Joining lobby: {lobbyId}");
        IsHost = false;
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Console.WriteLine($"[NetworkManager] Lobby creation failed: {callback.m_eResult}");
            IsHost = false;
            return;
        }
        
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Console.WriteLine($"[NetworkManager] Lobby creation successful: {_currentLobbyId}");
        
        // set lobby data
        SteamMatchmaking.SetLobbyData(_currentLobbyId, "HostName", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(_currentLobbyId, "GameVersion", "0.1.0");
        
        // create listen socket for incoming connections
        var options = Array.Empty<SteamNetworkingConfigValue_t>();
        // any port
        _listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, options);
        
        IsConnected = true;
    }
    
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != (uint) EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Console.WriteLine($"[NetworkManager] Failed to enter lobby: {callback.m_EChatRoomEnterResponse}");
            return;
        }
        
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Console.WriteLine($"[NetworkManager] Lobby entered: {_currentLobbyId}");
        
        // get lobby owner
        var lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        Console.WriteLine($"[NetworkManager] Lobby owner: {SteamFriends.GetFriendPersonaName(lobbyOwner)}");
        
        // if were not the host, connect to host
        if (!IsHost && lobbyOwner != MySteamId)
        {
            ConnectToPlayer(lobbyOwner);
        }
        
        // connect to all existing players
        var memberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        for (int i = 0; i < memberCount; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyId, i);
            if (member != MySteamId && !_connections.ContainsKey(member))
            {
                ConnectToPlayer(member);
            }
        }

        IsConnected = true;
    }
    
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        var lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        var userChanged = new CSteamID(callback.m_ulSteamIDUserChanged);
        var userMakingChange = new CSteamID(callback.m_ulSteamIDMakingChange);

        if (callback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeEntered)
        {
            Console.WriteLine($"[NetworkManager] User joined: {SteamFriends.GetFriendPersonaName(userChanged)}");
            if (userChanged != MySteamId && !_connections.ContainsKey(userChanged))
            {
                ConnectToPlayer(userChanged);
            }
        }
        else if (callback.m_rgfChatMemberStateChange == (uint) EChatMemberStateChange.k_EChatMemberStateChangeLeft)
        {
            Console.WriteLine($"[NetworkManager] User left: {SteamFriends.GetFriendPersonaName(userChanged)}");
            if (_connections.TryGetValue(userChanged, out var connection))
            {
                SteamNetworkingSockets.CloseConnection(connection, 0, "User left lobby", false);
                _connections.Remove(userChanged);
            }
        }
    }
    
    private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
    {
        Console.WriteLine($"[NetworkManager] Connection status changed: {callback.m_info.m_eState}");

        if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            Console.WriteLine("[NetworkManager] Connection established");
            
            // accept connection if host
            if (IsHost)
            {
                SteamNetworkingSockets.AcceptConnection(callback.m_hConn);
                SteamNetworkingSockets.SetConnectionPollGroup(callback.m_hConn, _pollGroup);
                
                // find which steam ID this connection belongs to
                var remoteSteamId = callback.m_info.m_identityRemote.GetSteamID();
                _connections[remoteSteamId] = callback.m_hConn;
            }
            
            // send hello message
            if (!IsHost)
            {
                SendMessage(callback.m_hConn, MessageType.ClientHello, $"Hello from {SteamFriends.GetPersonaName()}");
            }
        }
        else if (callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer ||
                 callback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
        {
            Console.WriteLine($"[NetworkManager] Connection closed: {callback.m_info.m_szEndDebug}");
            
            // remove from connections
            var remoteSteamId = callback.m_info.m_identityRemote.GetSteamID();
            _connections.Remove(remoteSteamId);
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
            _connections[playerSteamId] = connection;
            SteamNetworkingSockets.SetConnectionPollGroup(connection, _pollGroup);
        }
    }
    
    private void SendMessage(HSteamNetConnection connection, MessageType type, string content)
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

            if (result != EResult.k_EResultOK)
            {
                Console.WriteLine($"[NetworkManager] Failed to send message: {result}");
            }
        }
        finally
        {
            // free memory
            Marshal.FreeHGlobal(messageDataPtr);
        }
    }

    public void SendChatMessageToAll(string chatMessage)
    {
        if (!IsConnected || string.IsNullOrEmpty(chatMessage)) return;
        
        Console.WriteLine($"[NetworkManager] Sending chat: {chatMessage}");
        var fullMessage = $"{SteamFriends.GetPersonaName()}: {chatMessage}";

        foreach (var connection in _connections.Values)
        {
            SendMessage(connection, MessageType.ChatMessage, fullMessage);
        }
        
        // show locally
        ProcessChatMessageLocally(fullMessage);
    }

    private void ProcessChatMessageLocally(string message)
    {
        Console.WriteLine($"[CHAT] {message}");
    }
    
    private void ReceiveNetworkMessages()
    {
        if (_pollGroup == HSteamNetPollGroup.Invalid) return;

        var messages = new IntPtr[32];
        var messageCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, messages, 32);

        for (int i = 0; i < messageCount; i++)
        {
            var message = SteamNetworkingMessage_t.FromIntPtr(messages[i]);

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

                Console.WriteLine($"[NetworkManager] Received {type}: {content} from {message.m_conn}");

                switch (type)
                {
                    case MessageType.ClientHello:
                        Console.WriteLine($"[NetworkManager] Host received ClientHello: {content}");
                        // send welcome back
                        SendMessage(message.m_conn, MessageType.HostWelcome, "Welcome to the game!");
                        break;
                    case MessageType.HostWelcome:
                        Console.WriteLine($"[NetworkManager] Client received HostWelcome: {content}");
                        break;
                    case MessageType.ChatMessage:
                        ProcessChatMessageLocally(content);
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
                SteamNetworkingMessage_t.Release(messages[i]);
            }
        }
    }
}