using Steamworks;

namespace Blastia.Main.Networking;

public enum SyncMode 
{
    Auto, // automatically determines where to send
    SendToHost, // force sends to host (client only)
    BroadcastToClients // force broadcasts to all clients (host only)
}

/// <summary>
/// Generic functions for network message syncing
/// </summary>
public class NetworkSync 
{
    /// <summary>
    /// Sends some data to other clients (<c>host</c> -> broadcasts to all clients; <c>client</c> -> sends to host)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">Data to send</param>
    /// <param name="message">MessageType</param>
    /// <param name="mode"></param>
    /// <param name="senderConnection">Connection that sent this data</param>
    public static void Sync<T>(T data, MessageType message, SyncMode mode = SyncMode.Auto, HSteamNetConnection? senderConnection = null) 
    {
        if (NetworkManager.Instance == null) return;

        var content = SerializeData(data);
        
        switch (mode) 
        {
            case SyncMode.Auto:
                if (NetworkManager.Instance.IsHost)
                    BroadcastToClients(content, message, senderConnection);
                else
                    SendToHost(content, message);
                break;
            
            case SyncMode.SendToHost:
                if (!NetworkManager.Instance.IsHost)
                    SendToHost(content, message);
                break;
                
            case SyncMode.BroadcastToClients:
                if (NetworkManager.Instance.IsHost)
                    BroadcastToClients(content, message, senderConnection);
                break;
        }
    }
    
    private static void BroadcastToClients(string content, MessageType message, HSteamNetConnection? senderConnection) 
    {
        if (NetworkManager.Instance == null) return;
        
        // send to every client except who sent it
        foreach (var connection in NetworkManager.Instance.Connections.Values) 
        {
            if (senderConnection != connection)
                NetworkMessageQueue.QueueMessage(connection, message, content);
        }
    }

    private static void SendToHost(string content, MessageType message) 
    {
        if (NetworkManager.Instance == null) return;

        // send data to host (first connection)
        var hostConnection = NetworkManager.Instance.Connections.Values.FirstOrDefault();
        if (hostConnection != HSteamNetConnection.Invalid)
            NetworkMessageQueue.QueueMessage(hostConnection, message, content);
    }
    
    private static string SerializeData<T>(T data) 
    {
        var bytes = data switch
        {
            NetworkPlayer player => player.Serialize(),
            NetworkEntity entity => entity.Serialize(),
            NetworkBlockChangeMessage blockChange => blockChange.Serialize(),
            NetworkBlockUpdateMessage blockUpdate => blockUpdate.Serialize(),
            NetworkSignEditedMessage signEdited => signEdited.Serialize(),
            Guid networkId => networkId.ToByteArray(),
            _ => System.Text.Encoding.UTF8.GetBytes(data?.ToString() ?? "")
        };

        return Convert.ToBase64String(bytes);
    }
}