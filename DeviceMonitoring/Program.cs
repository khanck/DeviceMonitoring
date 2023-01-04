using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using Newtonsoft.Json;
using Serilog;

namespace DeviceMonitoring
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                // Creates a new client
                MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                                        .WithClientId("My.Mqtt")
                                                        .WithTcpServer("localhost", 707)
                                                        .WithCredentials("mqttclient", "Aa123456"); ;

                // Create client options objects
                ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                        .WithClientOptions(builder.Build())
                                        .Build();

                // Creates the client object
                IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();

                // Set up handlers
                _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
                _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
                _mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectingFailed);

                // Starts a connection with the Broker
                _mqttClient.StartAsync(options).GetAwaiter().GetResult();



                EventLog[] ev;

                ev = EventLog.GetEventLogs();

                Console.WriteLine("Number of logs on computer: " + ev.Length);

                foreach (EventLog log in ev)
                {

                    if (log.Log == "Application")
                    {
                        foreach (EventLogEntry entry in log.Entries)
                        {

                            // Send a new message to the broker every second

                            if ((entry.EntryType == EventLogEntryType.Error) || (entry.EntryType == EventLogEntryType.Warning))
                            {
                                string json = JsonConvert.SerializeObject(new
                                {
                                    ID = entry.Index,
                                    EntryType = entry.EntryType.ToString(),
                                    Entry = entry.Message,
                                    Source = entry.Source,
                                    Category = entry.Category,
                                    EventID = entry.EventID,
                                    sent = DateTimeOffset.UtcNow
                                });
                                _mqttClient.PublishAsync("new.mqtt/topic/json", json);

                                Task.Delay(50).GetAwaiter().GetResult();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }



        }
        public static void OnConnected(MqttClientConnectedEventArgs obj)
        {
            Log.Logger.Information("Successfully connected.");
        }

        public static void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
        {
            Log.Logger.Warning("Couldn't connect to broker.");
        }

        public static void OnDisconnected(MqttClientDisconnectedEventArgs obj)
        {
            Log.Logger.Information("Successfully disconnected.");
        }
    }
}
