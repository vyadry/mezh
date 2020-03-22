using System;

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace tcp
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 58900);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipe);
            if (!socket.Connected)
            {
                Console.WriteLine("Fail - Connection failed");
                return;
            }
            Console.WriteLine("Connected!");
            while (true)
            {
                string str = Console.ReadLine();
                Console.WriteLine(str);
                socket.Send(Encoding.ASCII.GetBytes(str), Encoding.ASCII.GetBytes(str).Length, 0);
            }
        }
    }
}
