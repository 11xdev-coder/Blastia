using Steamworks;

namespace Blastia.Main.Networking;

public struct QueuedMessage(HSteamNetConnection connection, MessageType type, string? content)
{
    public HSteamNetConnection Connection = connection;
    public MessageType Type = type;
    public string? Content = content;
}

/// <summary>
/// Message queue to avoid sending too many messages at small periods of time
/// </summary>
public static class NetworkMessageQueue
{
    private const int MaxMessagesPerCall = 32;
    private static Queue<QueuedMessage> _messageQueue = [];
    private static DateTime _lastSentTime = DateTime.UtcNow;
    private static readonly TimeSpan MinWaitTime = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMilliseconds(60);
    private static TimeSpan _currentWaitTime = MinWaitTime;
    
    /// <summary>
    /// Queues a message to be sent
    /// </summary>
    /// <param name="connection">Connection where to send this message</param>
    /// <param name="type"></param>
    /// <param name="content"></param>
    public static void QueueMessage(HSteamNetConnection connection, MessageType type, string? content)
    {
        var message = new QueuedMessage(connection, type, content);
        _messageQueue.Enqueue(message);
    }

    /// <summary>
    /// Call in <c>Update()</c> methods, processes all queued messages with delays
    /// </summary>
    public static void ProcessQueue()
    {
        if (_messageQueue.Count <= 0 || NetworkManager.Instance == null) return;

        var messagesSent = 0;
        while (_messageQueue.Count > 0 && messagesSent < MaxMessagesPerCall)
        {
            var now = DateTime.UtcNow;
            if (now - _lastSentTime < _currentWaitTime)
            {
                // wait for the next call
                return;
            }
            
            var message = _messageQueue.Dequeue();
            var result = NetworkManager.Instance.TrySendMessage(message.Connection, message.Type, message.Content);
            
            if (result == EResult.k_EResultOK)
            {
                messagesSent++;
                _lastSentTime = now;
                
                // reduce wait time on success
                _currentWaitTime = TimeSpan.FromMilliseconds(Math.Max(MinWaitTime.TotalMilliseconds, _currentWaitTime.TotalMilliseconds * 0.5));
            }
            else if (result == EResult.k_EResultLimitExceeded)
            {
                Console.WriteLine($"[NetworkManager] Limit exceeded while sending message of type {message.Type}, retrying");
                
                // create a new queue with failed message first
                var tempQueue = new Queue<QueuedMessage>();
                tempQueue.Enqueue(message);
                while (_messageQueue.Count > 0)
                {
                    tempQueue.Enqueue(_messageQueue.Dequeue());
                }
                _messageQueue = tempQueue;
                _lastSentTime = now;

                _currentWaitTime = TimeSpan.FromMilliseconds(Math.Min(MaxWaitTime.TotalMilliseconds, _currentWaitTime.Milliseconds * 1.3f));
                
                break; // stop this call
            }
            else
            {
                Console.WriteLine($"[NetworkManager] Failed to send message: {result}");
                messagesSent++; // count failed messages to avoid infinite loop
            }
        }
    }
}