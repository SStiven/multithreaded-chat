using System.Net.Sockets;
using System.Net;
using System.Text;

public class ClientNetworkHandler : IDisposable
{
    private readonly Socket _socket;
    private readonly byte[] _buffer = new byte[1024];
    private readonly StringBuilder _builder = new StringBuilder();

    public event Action<string>? MessageReceived;
    public event Action? Disconnected;

    private const string EndOfMessageMarker = "<|EOM|>";
    private const string EndOfChatMarker = "<|EOC|>";

    public ClientNetworkHandler(IPAddress serverIp, int port)
    {
        _socket = new Socket(serverIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(serverIp, port);
        ThreadPool.QueueUserWorkItem(_ => ReceiveLoop());
    }

    private void ReceiveLoop()
    {
        try
        {
            while (true)
            {
                int bytes = _socket.Receive(_buffer);
                if (bytes == 0) break;

                _builder.Append(Encoding.UTF8.GetString(_buffer, 0, bytes));
                string data = _builder.ToString();

                int endOfMessageIndex;
                while ((endOfMessageIndex = data.IndexOf(EndOfMessageMarker, StringComparison.Ordinal)) >= 0)
                {
                    var message = data.Substring(0, endOfMessageIndex);
                    _builder.Remove(0, endOfMessageIndex + EndOfMessageMarker.Length);
                    MessageReceived?.Invoke(message);
                    data = _builder.ToString();
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnected?.Invoke();
            Dispose();
        }
    }

    public void Send(string message)
    {
        try
        {
            string framed = message + EndOfMessageMarker;
            byte[] data = Encoding.UTF8.GetBytes(framed);
            _socket.Send(data);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[ClientNetworkHandler] Send error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        _socket.Close();
    }
}