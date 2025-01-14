using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using System.Windows.Threading;

namespace Prod
{
    internal class Disk
    {
        private Dictionary<string, PerformanceCounter> diskCounter;

        private DispatcherTimer timer;
        public Dictionary<string, ChartValues<double>> diskUsageData;

        public Disk() {
            diskCounter = new Dictionary<string, PerformanceCounter>();
            diskUsageData = new Dictionary<string, ChartValues<double>>();
            InitializeDiskPerformanceCounters();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateDiskUsage;
            timer.Start();
        }

        public List<KeyValuePair<string, string>> GetDiskInfo(DriveInfo drive)
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Disk", drive.Name),
                new KeyValuePair<string, string>("Type", drive.DriveType.ToString()),
                new KeyValuePair<string, string>("File system", drive.DriveFormat),
                new KeyValuePair<string, string>("Overall size", $"{drive.TotalSize / (1024 * 1024 * 1024)} GB"),
                new KeyValuePair<string, string>("Free size", $"{drive.AvailableFreeSpace / (1024 * 1024 * 1024)} GB"),
                new KeyValuePair<string, string>("Used size", $"{(drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024)} GB")
            };
        }

        private void InitializeDiskPerformanceCounters()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    var counter = new PerformanceCounter("LogicalDisk", "% Idle Time", drive.Name.TrimEnd('\\'));
                    diskCounter[drive.Name] = counter;

                    diskUsageData[drive.Name] = new ChartValues<double>();
                }
            }
        }

        private void UpdateDiskUsage(object sender, EventArgs e)
        {

            foreach (var kvp in diskCounter)
            {
                string driveName = kvp.Key;
                PerformanceCounter counter = kvp.Value;

                try
                {
                    float idleTime = counter.NextValue();
                    float usage = 100 - idleTime;

                    if (diskUsageData.ContainsKey(driveName))
                    {
                        var values = diskUsageData[driveName];
                        values.Add(usage); 

                        if (values.Count > 60)
                        {
                            values.RemoveAt(0);
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Exception {driveName}: {ex.Message}");
                }
            }
        }

    }
}
