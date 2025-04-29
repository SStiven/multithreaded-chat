using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using MultiThreadedChat.Infrastructure;

namespace MultiThreadedChat.Server.ConsoleUI;

public class ChatServer : IDisposable
{
    private readonly ServerNetworkHandler _network;
    private readonly ConcurrentDictionary<string, ClientHandler> _handlers = new();
    private readonly ConcurrentQueue<string> _history = new();
    private const int MaxHistory = 50;

    public ChatServer(IPAddress ipAddress, int port)
    {
        _network = new ServerNetworkHandler(ipAddress, port);
        _network.ClientConnected += OnClientConnected;
        _network.MessageReceived += OnMessageReceived;
        _network.ClientDisconnected += OnClientDisconnected;
    }

    public void Start() => _network.Start();

    private void OnClientConnected(string clientId)
    {
        var handler = new ClientHandler(clientId, this);
        _handlers[clientId] = handler;
    }

    private void OnMessageReceived(string clientId, string message)
    {
        if (!_handlers.TryGetValue(clientId, out var handler))
        {
            return;
        }

        if (!handler.HasName)
        {
            handler.Name = message;
            Console.WriteLine($"Client {clientId} set name to '{message}'");

            foreach (var histMsg in _history)
            {
                _network.SendToClient(clientId, histMsg);
            }

            return;
        }

        handler.HandleIncoming(message);
    }

    private void OnClientDisconnected(string clientId)
    {
        _handlers.TryRemove(clientId, out _);
    }

    internal void Broadcast(string fromClientId, string message)
    {
        if (!_handlers.TryGetValue(fromClientId, out var fromHandler) || !fromHandler.HasName)
        {
            return;
        }

        string tagged = $"[{fromHandler.Name}] {message}";

        _history.Enqueue(tagged);
        while (_history.Count > MaxHistory)
        {
            _history.TryDequeue(out _);
        }

        foreach (var clientId in _network.ClientIds.Where(id => id != fromClientId))
        {
            _network.SendToClient(clientId, tagged);
        }
    }

    public void Dispose()
    {
        NotifyClientsShutingdown();

        _network.Dispose();
    }

    private void NotifyClientsShutingdown()
    {
        foreach (var clientId in _network.ClientIds)
        {
            _network.SendToClient(clientId, "[SERVER] Shutting down");
        }
    }
}