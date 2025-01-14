using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Windows.Threading;

namespace Prod
{
    internal class Memory
    {
        private PerformanceCounter memCounter;
        private DispatcherTimer timer;

        public ChartValues<double> MemoryValues { get; private set; }

        public List<KeyValuePair<string, string>> memoryInfo { get; private set; }
        public UInt64 MemoryCount { get; private set; }

        public Memory()
        {
            LoadMemoryInfo();
            memCounter = new PerformanceCounter("Memory", "Available MBytes");

            MemoryValues = new ChartValues<double>();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateMemoryUsage;
            timer.Start();
        }

        private void UpdateMemoryUsage(object sender, EventArgs e)
        {
            double memUsage = memCounter.NextValue();
            memUsage = Math.Round(memUsage, 2);

            MemoryValues.Add(MemoryCount - memUsage);

            if (MemoryValues.Count > 60) 
            {
                MemoryValues.RemoveAt(0);
            }
        }

        private void LoadMemoryInfo()
        {
            var searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            memoryInfo = new List<KeyValuePair<string, string>>();
            
            foreach (ManagementObject obj in searcher.Get())
            {
                MemoryCount += (UInt64)obj.GetPropertyValue("Capacity");
                foreach (var property in obj.Properties)
                {
                    if (property.Value != null)
                    {
                        memoryInfo.Add(new KeyValuePair<string, string>(property.Name, property.Value.ToString()));
                    }
                }
            }
            MemoryCount /= 1048576;
        }
    }
}
