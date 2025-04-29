using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MultiThreadedChat.Client.ConsoleUI;

class Program
{
    const int port = 4040;

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting client chat");

        Console.Write("Enter your name: ");
        string? name = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(name))
            name = "Anonymous";

        var hostName = Dns.GetHostName();
        var hostEntry = Dns.GetHostEntry(hostName);
        var serverIp = hostEntry.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);


        using var client = new ChatClient(serverIp, port, name);

        Console.WriteLine($"Connected as {name}. Type messages and press Enter, or type /exit to quit.");

        string? input;
        while ((input = Console.ReadLine()) != null)
        {
            if (input.Equals("/exit"))
            {
                client.FinishChat();
                break;
            }

            client.SendMessage(input);
        }

        Console.WriteLine("Chat client shutting down.");
    }
}

