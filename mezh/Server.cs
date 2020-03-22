using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace tcp
{
    static class CalcOperations
    {
        public static long Factorial(long num)
        {
            long res = 1;
            for (long i = num; i > 1; i--)
                res *= i;
            return res;
        }
        public static long SumRow(long num)
        {
            long res = 1;
            for (long i = num; i > 1; i--)
                res += i;
            return res;
        }
    }


    class MyTask
    {
        public string id;
        public string type;
        public long value;
        public MyTask(string id, string type, long value)
        {
            this.id = id;
            this.type = type;
            this.value = value;
        }
    }
    class Result
    {
        public string id;
        public long value;
        public string type;
        public long answer;
        public Result(string id, long value, string type, long answer)
        {
            this.id = id;
            this.value = value;
            this.type = type;
            this.answer = answer;
        }
    }

        class Server
        {
            ConcurrentQueue<MyTask> tasks = new ConcurrentQueue<MyTask>();
            List<Result> results = new List<Result>();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8005);
            private readonly object listLock = new object();

            public void CalculateQueue()
            {
                while (true)
                {
                    MyTask task;
                    while (tasks.TryDequeue(out task))
                    {
                        if (task != null)
                        {
                            lock(listLock){
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
                }
                throw new Exception("not found");
            }
            public void Start()
            {
                Thread myThread = new Thread(CalculateQueue);
                myThread.Start();
                try
                {
                    socket.Bind(ipe);

                    socket.Listen(10);

                    Console.WriteLine("Done - Server started");
                    Console.WriteLine(ipe);


                    while (true)
                    {
                        Socket handler = socket.Accept();

                        // получаем сообщение
                        StringBuilder builder = new StringBuilder();
                        int bytes = 0; // количество полученных байтов
                        byte[] data = new byte[256]; // буфер для получаемых данных

                        do
                        {
                            bytes = handler.Receive(data);
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (handler.Available > 0);

                        Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());

                        MyTask t = JsonConvert.DeserializeObject<MyTask>(builder.ToString());
                        if (t.type == "Factorial" || t.type == "SumOfRow")
                        {
                            tasks.Enqueue(t);
                            // отправляем ответ
                            string message = "Операция добавлена в очередь";
                            data = Encoding.Unicode.GetBytes(message);
                            handler.Send(data);
                        }
                        else if (t.type == "GetResult")
                        {
                            
                            var founded = results.FindAll(el => el.id == t.id);
                            results.RemoveAll(el => el.id == t.id);
                            // отправляем ответ
                            string json = JsonConvert.SerializeObject(founded);
                            data = Encoding.Unicode.GetBytes(json);
                            handler.Send(data);
                        }
                        else
                        {
                            string message = "Ошибка";
                            data = Encoding.Unicode.GetBytes(message);
                        }

                        // закрываем сокет

                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();
            }

        }
        class Program
        {
            static void Main(string[] args)
            {
                Server server = new Server();
                server.Start();

            }
        }
    }