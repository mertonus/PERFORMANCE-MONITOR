using LiveCharts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Windows.Threading;

namespace Prod
{
    internal class GPU
    {
        public List<KeyValuePair<string, string>> gpuInfo { get; private set; }

        public GPU()
        {
            LoadGpuInfo();
        }

        private void LoadGpuInfo()
        {
            var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
            gpuInfo = new List<KeyValuePair<string, string>>();

            foreach (ManagementObject obj in searcher.Get())
            {
                foreach (var property in obj.Properties)
                {
                    if (property.Value != null)
                    {
                        gpuInfo.Add(new KeyValuePair<string, string>(property.Name, property.Value.ToString()));
                    }
                }
            }
        }
    }
}
