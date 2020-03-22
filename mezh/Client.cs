using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace client
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint localIP = new IPEndPoint(IPAddress.Broadcast, 80);



            var message = "THIS is US";
            byte[] data = Encoding.Unicode.GetBytes(message);
            socket.SendTo(data, localIP);
            Console.WriteLine("done");


            byte[] buf = new byte[256];

            // byte[] data = Encoding.Unicode.GetBytes(message);
            //socket.SendTo(data, remotePoint);



            var result = socket.ReceiveFrom(buf, SocketFlags.None, ref localIP);

            var str = Encoding.Unicode.GetString(buf, 0, result);
            Console.WriteLine($"{str}, {IPAddress.Broadcast}");









            socket.Close();
        }
    }
}