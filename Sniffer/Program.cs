using System;
using PacketDotNet;
using SharpPcap;
using System.IO.Ports;

class Program
{
    static void Main()
    {
        Console.WriteLine("Введите:\n1 если слушаем Tcp\n2 если слушаем com port");
        int listener = int.Parse(Console.ReadLine());

        switch (listener)
        {
            case 1:
                TcpListen();
                break;
            case 2:
                ComListen();
                break;
        }

    }

    private static void TcpListen()
    {
        Console.WriteLine("Введите IP ");
        string ip = Console.ReadLine();

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
        device.Filter = "host " + ip;//host 192.168.100.7";
        device.StartCapture();

        Console.WriteLine("Нажми Enter для выхода...");
        Console.ReadLine();

        device.StopCapture();
        device.Close();
    }

    private static void ComListen()
    {
        string[] ports = SerialPort.GetPortNames();
        Console.WriteLine("Доступные порты:");
        foreach (var p in ports)
            Console.WriteLine(p);

        Console.Write("Введите номер COM-порта (например: COM3): ");
        string portNum = Console.ReadLine();

        Console.Write("Введите скорость (baud rate), например 9600: ");
        int baudRate = int.Parse(Console.ReadLine());
        string portName = "COM" + portNum;
        SerialPort port = new SerialPort(portName, baudRate)
        {
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            Encoding = System.Text.Encoding.ASCII
        };

        port.DataReceived += (sender, e) =>
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string data = sp.ReadExisting();

                Console.WriteLine(
                    $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff}] COM DATA: {data}"
                );
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения: {ex.Message}");
                Console.ReadLine();
            }
        };

        try
        {
            port.Open();
            Console.WriteLine($"Прослушивание {portName} начато. Нажмите Enter для выхода...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка открытия порта: {ex.Message}");
            Console.ReadLine();
        }
        finally
        {
            if (port.IsOpen)
                port.Close();
        }
    }
}

