using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace tcp
{

    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    static class CalcOperations
    {
        public static double Factorial(double num)
        {
            double res = 1;
            for (double i = num; i > 1; i--)
                res *= i;
            return res;
        }
        public static double SumRow(double num)
        {
            double res = 1;
            for (double i = num; i > 1; i--)
                res += i;
            return res;
        }
        public static double Sqr(double num)
        {
            return Math.Pow(num, 2);
        }
        public static double To2(double num)
        {
            string s = "";
            int x =(int)(num);
            while (x > 0)
            {
                s = ((x % 2 == 0) ? "0" : "1") + s;
                x /= 2;
            }
            int number;

            int.TryParse(s, out number);
            return number;
        }
        public static double Log(double num)
        {
            return Math.Log(num);
        }
    }


    class MyTask
    {
        public string id;
        public string type;
        public double value;
        public MyTask(string id, string type, double value)
        {
            this.id = id;
            this.type = type;
            this.value = value;
        }
    }
    class Result
    {
        public string id;
        public double value;
        public string type;
        public double answer;
        public Result(string id, double value, string type, double answer)
        {
            this.id = id;
            this.value = value;
            this.type = type;
            this.answer = answer;
        }
    }

    class Server
    {
        private readonly object balanceLock = new object();
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        ConcurrentQueue<MyTask> tasks = new ConcurrentQueue<MyTask>();
        List<Result> results = new List<Result>();
        public void CalculateQueue()
        {
            while (true)
            {
                MyTask task;
                while (tasks.TryDequeue(out task))
                {
                    if (task != null)
                    {
                        lock (balanceLock)
                        {
                            results.Add(Calculate(task));
                        }
                    }
                }
                Thread.Sleep(0);
            }
        }
        public Result Calculate(MyTask task)
        {
            switch (task.type)
            {
                case "Factorial":
                    return new Result(task.id, task.value, task.type, CalcOperations.Factorial(task.value));
                case "SumOfRow":
                    return new Result(task.id, task.value, task.type, CalcOperations.SumRow(task.value));
                case "Sqr":
                    return new Result(task.id, task.value, task.type, CalcOperations.Sqr(task.value));
                case "To2":
                    return new Result(task.id, task.value, task.type, CalcOperations.To2(task.value));
                case "Log":
                    return new Result(task.id, task.value, task.type, CalcOperations.Log(task.value));
            }
            throw new Exception("not found");
        }

        public void StartListening()
        {
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8005);
            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipe.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(ipe);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            byte[] data = new byte[256]; // буфер для получаемых данных

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.Unicode.GetString(
                    state.buffer, 0, bytesRead));

                Console.WriteLine(state.sb.ToString());

                MyTask t = JsonConvert.DeserializeObject<MyTask>(state.sb.ToString());
                if (t.type == "Factorial" || t.type == "SumOfRow" || t.type == "Sqr" || t.type == "To2" || t.type == "Log")
                {
                    tasks.Enqueue(t);
                    content = "Operation add in queue";
                }
                else if (t.type == "GetResult")
                {
                    lock (balanceLock)
                    {
                        var founded = results.FindAll(el => el.id == t.id);
                        results.RemoveAll(el => el.id == t.id);
                        content = JsonConvert.SerializeObject(founded);
                    }
                }
                else
                {
                    content = "Error";
                }

                Send(handler, content);
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.Unicode.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            Thread myThread = new Thread(server.CalculateQueue);
            myThread.Start();
            server.StartListening();

        }
    }
}