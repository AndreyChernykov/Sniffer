using System;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap;

class Program
{
    static void Main()
    {
        var devices = CaptureDeviceList.Instance;
        if (devices.Count < 1)
        {
            Console.WriteLine("Нет доступных интерфейсов");
            return;
        }

        // Выводим список интерфейсов
        for (int i = 0; i < devices.Count; i++)
            Console.WriteLine($"{i}: {devices[i].Description}");

        Console.Write("Выбери интерфейс: ");
        int index = int.Parse(Console.ReadLine());
        var device = devices[index];

        device.OnPacketArrival += (sender, e) =>
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            Console.WriteLine(packet.ToString());
        };

        device.OnPacketArrival += (sender, e) =>
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            var ipPacket = packet.Extract<IPPacket>();
            if (ipPacket != null)
            {
                var tcpPacket = ipPacket.Extract<TcpPacket>();
                if (tcpPacket != null)
                {
                    // Данные, которые реально передаются
                    byte[] payload = tcpPacket.PayloadData;

                    if (payload != null && payload.Length > 0)
                    {
                        //string text = System.Text.Encoding.ASCII.GetString(payload);
                        string text = BitConverter.ToString(payload);
                        Console.WriteLine($"Payload: {text}   {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}");
                    }
                }
            }
        };


        device.Open(DeviceModes.Promiscuous, 1000);
        device.Filter = "host 192.168.100.7";
        device.StartCapture();

        Console.WriteLine("Нажми Enter для выхода...");
        Console.ReadLine();

        device.StopCapture();
        device.Close();
    }
}


