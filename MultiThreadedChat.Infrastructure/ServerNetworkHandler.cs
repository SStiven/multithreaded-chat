using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace MultiThreadedChat.Infrastructure;

public class ServerNetworkHandler : IDisposable
{
    private readonly Socket _listener;
    private readonly ConcurrentDictionary<string, Socket> _clientSockets = new();
    private readonly int _backlog;
    private volatile bool _isRunning;

    public event Action<string>? ClientConnected;
    public event Action<string, string>? MessageReceived;
    public event Action<string>? ClientDisconnected;

    private const string EndOfMessageMarker = "<|EOM|>";
    private const string EndOfChatMarker = "<|EOC|>";

    public ServerNetworkHandler(IPAddress ipAddress, int port, int backlog = 10)
    {
        _backlog = backlog;
        _listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(ipAddress, port));
    }

    public void Start()
    {
        _listener.Listen(_backlog);
        _isRunning = true;
        Console.WriteLine($"[ServerNetwork] Listening on {_listener.LocalEndPoint}");
        ThreadPool.QueueUserWorkItem(_ => AcceptLoop());
    }

    private void AcceptLoop()
    {
        while (_isRunning)
        {
            try
            {
                var client = _listener.Accept();
                string clientId = Guid.NewGuid().ToString("N").Substring(0, 8);
                _clientSockets[clientId] = client;
                Console.WriteLine($"[ServerNetwork] Client {clientId} connected from {client.RemoteEndPoint}");
                ClientConnected?.Invoke(clientId);

                ThreadPool.QueueUserWorkItem(_ => ReceiveLoop(clientId, client));
            }
            catch (SocketException) when (!_isRunning)
            {
                // Listener was stopped
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerNetwork] AcceptLoop error: {ex.Message}");
            }
        }
    }

    private void ReceiveLoop(string clientId, Socket socket)
    {
        var buffer = new byte[4096];
        var builder = new StringBuilder();
        try
        {
            while (true)
            {
                int bytesRead = socket.Receive(buffer);
                if (bytesRead == 0) break;

                builder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                string data = builder.ToString();

                int eomIdx;
                while ((eomIdx = data.IndexOf(EndOfMessageMarker, StringComparison.Ordinal)) >= 0)
                {
                    string message = data.Substring(0, eomIdx);
                    builder.Remove(0, eomIdx + EndOfMessageMarker.Length);
                    data = builder.ToString();

                    if (message == EndOfChatMarker)
                    {
                        RemoveClient(clientId);
                        return;
                    }

                    MessageReceived?.Invoke(clientId, message);
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ServerNetwork] ReceiveLoop socket error for {clientId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServerNetwork] ReceiveLoop error for {clientId}: {ex.Message}");
        }
        finally
        {
            RemoveClient(clientId);
        }
    }

    public void SendToClient(string clientId, string message)
    {
        if (!_clientSockets.TryGetValue(clientId, out var sock))
            return;

        try
        {
            string framed = message + EndOfMessageMarker;
            byte[] data = Encoding.UTF8.GetBytes(framed);
            sock.Send(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServerNetwork] SendToClient error for {clientId}: {ex.Message}");
            RemoveClient(clientId);
        }
    }

    private void RemoveClient(string clientId)
    {
        if (_clientSockets.TryRemove(clientId, out var sock))
        {
            try { sock.Shutdown(SocketShutdown.Both); } catch { }
            sock.Close();
            Console.WriteLine($"[ServerNetwork] Client {clientId} disconnected");
            ClientDisconnected?.Invoke(clientId);
        }
    }

    public IEnumerable<string> ClientIds => _clientSockets.Keys;

    public void Dispose()
    {
        _isRunning = false;
        try { _listener.Close(); } catch { }
        foreach (var clientId in _clientSockets.Keys)
            RemoveClient(clientId);
    }
}
