using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Threading;


namespace Prod
{
    internal class Network
    {
        private Dictionary<string, PerformanceCounter> sendCounters;
        private Dictionary<string, PerformanceCounter> receiveCounters;
        private DispatcherTimer timer;

        public Dictionary<string, ChartValues<double>> SendData { get; private set; }
        public Dictionary<string, ChartValues<double>> ReceiveData { get; private set; }

        public Network() 
        {
            sendCounters = new Dictionary<string, PerformanceCounter>();
            receiveCounters = new Dictionary<string, PerformanceCounter>();
            SendData = new Dictionary<string, ChartValues<double>>();
            ReceiveData = new Dictionary<string, ChartValues<double>>();

            InitializeNetworkPerformanceCounters();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateNetworkUsage;
            timer.Start();
        }

        private void InitializeNetworkPerformanceCounters()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                    networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                try
                {
                    var sendCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkInterface.Description);
                    var receiveCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkInterface.Description);

                    sendCounters[networkInterface.Name] = sendCounter;
                    receiveCounters[networkInterface.Name] = receiveCounter;

                    SendData[networkInterface.Name] = new ChartValues<double>();
                    ReceiveData[networkInterface.Name] = new ChartValues<double>();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Error initializing counters for {networkInterface.Name}: {ex.Message}");
                }
            }
        }

        private void UpdateNetworkUsage(object sender, EventArgs e)
        {
            foreach (var instance in sendCounters.Keys)
            {
                try
                {
                    float sent = sendCounters[instance].NextValue() / 1024; // KB/s
                    float received = receiveCounters[instance].NextValue() / 1024; // KB/s

                    if (SendData.ContainsKey(instance))
                    {
                        SendData[instance].Add(sent);
                        if (SendData[instance].Count > 60)  SendData[instance].RemoveAt(0);
                    }

                    if (ReceiveData.ContainsKey(instance))
                    {
                        ReceiveData[instance].Add(received);
                        if (ReceiveData[instance].Count > 60) ReceiveData[instance].RemoveAt(0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating network data for {instance}: {ex.Message}");
                }
            }
        }

        public List<KeyValuePair<string, string>> GetNetworkInterfaceInfo(string interfaceName)
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.Name == interfaceName || ni.Description == interfaceName);

            if (networkInterface == null) return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Error", "Interface not found") };

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Interface Name", networkInterface.Name),
                new KeyValuePair<string, string>("Description", networkInterface.Description),
                new KeyValuePair<string, string>("Status", networkInterface.OperationalStatus.ToString()),
                new KeyValuePair<string, string>("Speed", $"{networkInterface.Speed / 1_000_000} Mbps"),
                new KeyValuePair<string, string>("Type", networkInterface.NetworkInterfaceType.ToString()),
                new KeyValuePair<string, string>("Supports Multicast", networkInterface.SupportsMulticast.ToString())
            };
            
        }
    }
}
