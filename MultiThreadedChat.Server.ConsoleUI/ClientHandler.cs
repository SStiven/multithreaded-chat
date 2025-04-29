namespace MultiThreadedChat.Server.ConsoleUI;

internal class ClientHandler : IDisposable
{
    private readonly string _clientId;
    private readonly ChatServer _server;

    public string? Name { get; set; }
    public bool HasName => !string.IsNullOrEmpty(Name);

    public ClientHandler(string clientId, ChatServer server)
    {
        _clientId = clientId;
        _server = server;
    }

    public void HandleIncoming(string message)
    {
        Console.WriteLine($"[{Name}] {message}");
        _server.Broadcast(_clientId, message);
    }

    public void Dispose() { }
}