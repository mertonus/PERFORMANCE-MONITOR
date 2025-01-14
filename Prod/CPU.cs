using LiveCharts;
using System;
using System.Management;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Prod
{
    internal class CPU
    {
        private PerformanceCounter cpuCounter;
        private DispatcherTimer timer;

        public ChartValues<double> CpuValues { get; private set; }
        public List<KeyValuePair<string, string>> processorInfo { get; private set; }

        public CPU() 
        {
            LoadProcessorInfo();
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            CpuValues = new ChartValues<double>();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateCpuUsage;
            timer.Start();
        }

        private void UpdateCpuUsage(object sender, EventArgs e)
        {
            double cpuUsage = cpuCounter.NextValue();
            cpuUsage = Math.Round(cpuUsage, 2);

            CpuValues.Add(cpuUsage);

            if (CpuValues.Count > 60) 
            {
                CpuValues.RemoveAt(0);
            }
        }

        private void LoadProcessorInfo()
        {

            var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            processorInfo = new List<KeyValuePair<string, string>>();

            foreach (ManagementObject obj in searcher.Get())
            {
                foreach (var property in obj.Properties)
                {
                    if (property.Value != null)
                    {
                        processorInfo.Add(new KeyValuePair<string, string>(property.Name, property.Value.ToString()));
                    }
                }
            }
        }
    }
}
