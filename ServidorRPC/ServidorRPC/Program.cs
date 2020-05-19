using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ServidorRPC
{
    class Program //consumidor
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

        private static EventingBasicConsumer Consumer;
        static ConnectionFactory factory;
        static IConnection connection;
        static IModel channel { get => connection.CreateModel(); }
        static void Main(params string[] args)
        {
            //Relatorio
            Task.Run(() =>
            {
                while (true)
                {

                    List<Machine> machines = Select().ToList();
                    double mediaMemoria = 0;
                    double mediaCPU = 0;
                    double mediaDisk = 0;
                    foreach (Machine item in machines)
                    {
                        mediaCPU += item.CPU;
                        mediaDisk += item.Disk;
                        mediaMemoria += item.Memory;
                    }


                    Console.WriteLine("Media memoria: {0} mb", (mediaMemoria / machines.Count).ToString("N2"));
                    Console.WriteLine("Media CPU: {0} %", (mediaCPU / machines.Count).ToString("N2"));
                    Console.WriteLine("Media Disk: {0} %", (mediaDisk / machines.Count).ToString("N2"));

                    Thread.Sleep(1000 * 20);
                }
            });

            //Vigia a fila
            Task.Run(() => {
                while (true)
                {


                    factory = new ConnectionFactory()
                    {
                        HostName = "localhost"
                    };


                    connection = factory.CreateConnection();
                    channel.QueueDeclare(queue: "SD",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);
                    Consumer = new EventingBasicConsumer(channel);
                    Consumer.Received += (object? model, BasicDeliverEventArgs ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body.ToArray());
                        Console.WriteLine("[x] Mensagem recebida  {0}", message);
                        SalveDadosNoBanco(JsonConvert.DeserializeObject<Machine>(message));
                    };

                    channel.BasicConsume(queue: "SD", true, Consumer);


                    Thread.Sleep(1000 * 10);
                    Consumer = null;
                }
            });

            while(true)
            {
                string fila = string.Empty;
                Console.WriteLine("Fila:");
                fila = Console.ReadLine();

                RealizePooling(fila);
            }

        }

        private static void RealizePooling(string fila)
        {
            if (!string.IsNullOrEmpty(fila))
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost"
                };


                using (IConnection connection = factory.CreateConnection())
                {
                    //CRIANDO FILA
                    using IModel channel = connection.CreateModel();
                    channel.QueueDeclare(queue: fila, false, false, false, null);

                    var body = Encoding.UTF8.GetBytes("send"); 
                    channel.BasicPublish(exchange: "",fila, null, body);
                    Console.WriteLine("Enviado...");

                }
            }
        }

        private static string connectionString = "Data Source=N1001721;Initial Catalog=GeradorDeSoftware;Integrated Security=True";
        private static void SalveDadosNoBanco(Machine maquina)
        {
            maquina.IP = string.IsNullOrEmpty(maquina.IP) ? "127.0.0.0" : maquina.IP;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = $@"
                        INSERT INTO SD
                                   (Memory
                                   ,CPU
                                   ,DISK_MACHINE
                                   ,IP
		                           ,id)
                             VALUES
                                   ({maquina.Memory.ToString().Replace(',', '.')}
                                   ,{maquina.CPU.ToString().Replace(',', '.')}
                                   ,{maquina.Disk.ToString().Replace(',', '.')}
                                   ,'{maquina.IP.ToString().Replace(',', '.')}'
		                           ,'{Guid.NewGuid()}')";

                SqlCommand command = new SqlCommand(query, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static IEnumerable<Machine> Select()
        {
            const int paramValue = 5;
            string queryString = @"select 
                                      id,
                                      Memory,
                                      CPU,
                                      Disk_Machine,
                                      IP
                                    from SD";
            using (var connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@pricePoint", paramValue);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    yield return new Machine
                    {
                        Memory = reader.GetDouble(1),
                        CPU = reader.GetDouble(2),
                        Disk = reader.GetDouble(3),
                        IP = reader.GetString(4)
                    };
                }
                reader.Close();
            }
        }
    }
}

