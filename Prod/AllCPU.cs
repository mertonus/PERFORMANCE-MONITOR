using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace Prod
{
    internal class AllCPU
    {
        private List<PerformanceCounter> cpuCounters;
        private DispatcherTimer timer;
        public Dictionary<int, ChartValues<double>> CpuUsageData;

        public AllCPU() 
        {
            int processorCount = Environment.ProcessorCount;
            cpuCounters = new List<PerformanceCounter>();
            CpuUsageData = new Dictionary<int, ChartValues<double>>();

            for (int i = 0; i < processorCount; i++)
            {
                try
                {
                    using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", i.ToString()))
                    {
                        cpuCounters.Add(cpuCounter);
                        CpuUsageData[i] = new ChartValues<double>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize PerformanceCounter for CPU {i}: {ex.Message}");
                }
            }
            
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateAllCpuUsage;
            timer.Start();
        }

        private void UpdateAllCpuUsage(object sender, EventArgs e)
        {
            for (int i = 0; i < cpuCounters.Count; i++)
            {
                try
                {
                    float usage = cpuCounters[i].NextValue();
                    if (CpuUsageData[i].Count >= 60)
                    {
                        CpuUsageData[i].RemoveAt(0);
                    }
                    CpuUsageData[i].Add(usage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating CPU {i} usage: {ex.Message}");
                }
            }
        }
    }
}
