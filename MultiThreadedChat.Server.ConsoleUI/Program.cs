using System.Net;

namespace MultiThreadedChat.Server.ConsoleUI;

internal class Program
{
    static void Main(string[] args)
    {
        var ip = IPAddress.Any;
        int port = 4040;

        using var server = new ChatServer(ip, port);
        server.Start();

        Console.WriteLine("Chat server running. Press Enter to exit...");
        Console.ReadLine();
    }
}

