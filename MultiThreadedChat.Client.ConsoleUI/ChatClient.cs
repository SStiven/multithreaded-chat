using System;
using System.Net;

namespace MultiThreadedChat.Client.ConsoleUI;

internal class ChatClient : IDisposable
{
    private readonly ClientNetworkHandler _network;
    private const string EndOfChatMarker = "<|EOC|>";

    public ChatClient(IPAddress serverIp, int port, string name)
    {
        _network = new ClientNetworkHandler(serverIp, port);
        _network.MessageReceived += OnMessageReceived;
        _network.Disconnected += OnDisconnected;

        _network.Send(name);
    }

    private void OnMessageReceived(string message)
    {
        Console.WriteLine(message);
    }

    private void OnDisconnected()
    {
        Console.WriteLine("Disconnected from server.");
    }

    public void SendMessage(string message)
    {
        _network.Send(message);
    }

    public void FinishChat()
    {
        _network.Send(EndOfChatMarker);
    }

    public void Dispose()
    {
        _network.Dispose();
    }
}