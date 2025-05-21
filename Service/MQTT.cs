using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MQTT
{
    public class MQTTDeviceClient
    {
        private TcpClient client;
        private NetworkStream stream;
        private string clientId;
        private string topic;
        private string broker;
        private int port;

        public event Action<string, double, double, int> OnSensorDataReceived; // id, temp, hum, water level

        public MQTTDeviceClient(string brokerAddress, int brokerPort, string topic, string clientId)
        {
            this.broker = brokerAddress;
            this.port = brokerPort;
            this.topic = topic;
            this.clientId = clientId;
        }

        public async Task ConnectAsync(string username = null, string password = null)
        {
            client = new TcpClient();
            await client.ConnectAsync(broker, port);
            stream = client.GetStream();

            var connectPacket = Packet.Connect(clientId, username, password, 60);
            await SendPacketAsync(connectPacket);

            await ReadResponseAsync(); // Connack

            var subscribePacket = Packet.Subscribe(topic, 0);
            await SendPacketAsync(subscribePacket);

            await ReadResponseAsync(); // Suback

            _ = Task.Run(ListenForMessagesAsync);
        }

        public async Task DisconnectAsync()
        {
            var disconnectPacket = Packet.Disconnect();
            await SendPacketAsync(disconnectPacket);
            stream.Close();
            client.Close();
        }

        private async Task SendPacketAsync(Packet packet)
        {
            var data = packet.ToBytes();
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async Task ReadResponseAsync()
        {
            byte[] header = new byte[2];
            await stream.ReadAsync(header, 0, 2);
            int length = header[1];
            byte[] payload = new byte[length];
            await stream.ReadAsync(payload, 0, length);
        }

        private async Task ListenForMessagesAsync()
        {
            byte[] buffer = new byte[4096];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                try
                {
                    string payload = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (payload.Contains("{"))
                    {
                        var json = JsonDocument.Parse(payload);
                        string id = json.RootElement.GetProperty("id").GetString();
                        double temp = json.RootElement.GetProperty("Temp").GetDouble();
                        double hum = json.RootElement.GetProperty("humidity").GetDouble();
                        int water = json.RootElement.GetProperty("water").GetInt32();

                        OnSensorDataReceived?.Invoke(id, temp, hum, water);
                    }
                }
                catch { }
            }
        }

        public async Task SendCommandAsync(string device, bool state)
        {
            var command = new
            {
                cmd = device,
                value = state ? 1 : 0
            };
            string json = JsonSerializer.Serialize(command);
            var publishPacket = Packet.Publish(topic, Encoding.UTF8.GetBytes(json), 0, false);
            await SendPacketAsync(publishPacket);
        }
    }
}
