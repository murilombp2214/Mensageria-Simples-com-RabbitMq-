using Newtonsoft.Json;
using System;
using System.Data.Entity.Core.Objects;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Text;
using System.Net;
using System.Net.Sockets;
using RabbitMQ.Client.Events;

namespace ClienteRPC
{
    class Program
    {
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void InicieCliente()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };


            using (IConnection connection = factory.CreateConnection())
            {
                //CRIANDO FILA
                using IModel channel = connection.CreateModel();
                channel.QueueDeclare(queue: "hello", false, false, false, null);

                //definindo mensagem
                var body = Encoding.UTF8.GetBytes(MyMachine.Json());
                channel.BasicPublish(exchange: "", "SD", null, body);
                Console.WriteLine("Enviado...");
            }
        }

        static void Main(params string[] args)
        {

            for (int i = 0; i < 1; ++i)
            {
                string nomeFila = $"cliente{i}";
                Task.Run(() =>
                {
                    while (true)
                    {
                        EnviarDados();
                    }
                });


                //Vigia a fila
                Task.Run(() =>
                {

                    while (true)
                    {
                        Thread.Sleep(1000);

                        //string host = GetLocalIPAddress() + ":" + (4000 + i).ToString();
                        var factory = new ConnectionFactory()
                        {
                            HostName = "localhost",
                            Port = 5672
                           
                        };
                        //var endpoint = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 1026);
                        //var servidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        //servidor.Bind(endpoint);
                        //servidor.Listen(100);
                        // servidor.Accept();

                        var connection = factory.CreateConnection();
                        var channel = connection.CreateModel();
                        channel.QueueDeclare(queue: nomeFila,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);
                        var Consumer = new EventingBasicConsumer(channel);
                        Consumer.Received += (object? model, BasicDeliverEventArgs ea) =>
                        {
                            var body = ea.Body;
                            var message = Encoding.UTF8.GetString(body.ToArray());
                            if (message == "send")
                            {
                                EnviarDados();
                            }
                        };

                        channel.BasicConsume(queue: nomeFila, true, Consumer);

                        Consumer = null;
                    }

                });
            }
            Console.WriteLine("Clique para encerrar o cliente");
            Console.ReadKey();

        }

        private static void EnviarDados()
        {
            Console.WriteLine("Aguardando para novo envio");
            Thread.Sleep(10000);
            Console.WriteLine("Enviando...");
            InicieCliente();
            Console.WriteLine("Dados enviados");
            Console.Clear();
        }
    }

}
