using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Management;
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using Separator = LiveCharts.Wpf.Separator;


namespace Prod
{
    internal class AllGPU
    {
        private List<ChartModel> charts;
        public List<ChartModel> Charts
        {
            get => charts;
            set
            {
                charts = value;
                OnPropertyChanged(nameof(Charts));
            }
        }
        private readonly List<string> gpuEngineTypes;

        private readonly Dictionary<string, ChartValues<double>> gpuData;
        private readonly DispatcherTimer timer;

        public event PropertyChangedEventHandler PropertyChanged;

        public AllGPU()
        {

            Charts = new List<ChartModel>();


            gpuEngineTypes = new List<string>
            {
            "engtype_3D",
            "engtype_VideoDecode",
            "engtype_VideoEncode",
            "engtype_Compute",
            "engtype_Copy"
            };

            gpuData = new Dictionary<string, ChartValues<double>>();
            Charts = new List<ChartModel>();

            Charts = gpuEngineTypes.Select(engineType => new ChartModel
            {
                Title = engineType,
                SeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = engineType,
                        Values = new ChartValues<double>(),
                        PointGeometry = null
                    }
                },
                TimeLabels = Array.Empty<string>(),
                Formatter = value => value.ToString("N")
            }).ToList();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += UpdateGpuUsage;
            timer.Start();
        }

        private Dictionary<string, double> GetGpuUsage()
        {
            var gpuUsage = new Dictionary<string, double>();

            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");

                foreach (var obj in searcher.Get())
                {
                    if (obj["Name"] != null)
                    {
                        string engineName = obj["Name"].ToString();
                        double utilization = Convert.ToDouble(obj["UtilizationPercentage"]);

                        foreach (var engineType in gpuEngineTypes)
                        {
                            if (engineName.Contains(engineType))
                            {
                                if (gpuUsage.ContainsKey(engineType))
                                {
                                    gpuUsage[engineType] += utilization;
                                }
                                else
                                {
                                    gpuUsage[engineType] = utilization;
                                }
                            }
                        }
                    }
                }            
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving GPU usage: " + ex.Message);
            }

            return gpuUsage;
        }

        private void UpdateGpuUsage(object sender, EventArgs e)
        {
            var gpuUsage = GetGpuUsage();

            foreach (var engineType in gpuEngineTypes)
            {
                double utilization = gpuUsage.ContainsKey(engineType) ? gpuUsage[engineType] : 0.0;

                var chart = Charts.FirstOrDefault(c => c.Title == engineType);
                if (chart != null)
                {
                    var values = chart.SeriesCollection[0].Values as ChartValues<double>;

                    values.Add(utilization);
                    if (values.Count > 30) 
                        values.RemoveAt(0);
                }
            }

            OnPropertyChanged(nameof(Charts));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateGpuCharts(Grid grid, int columns = 2)
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            int rowCount = (int)Math.Ceiling((double)Charts.Count / columns);

            for (int i = 0; i < rowCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < columns; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < Charts.Count; i++)
            {
                var chartModel = Charts[i];

                var textBlock = new TextBlock
                {
                    Text = chartModel.Title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 2)
                };

                var chart = new CartesianChart
                {
                    Series = chartModel.SeriesCollection,
                    AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "Time (s)",
                    Labels = new[] { "0", "30" },
                    Separator = new Separator { Step = 30 }
                }
            },
                    AxisY = new AxesCollection
            {
                new Axis
                {
                    Title = "Usage (%)",
                    MinValue = 0,
                    MaxValue = 100
                }
            },
                    Margin = new Thickness(0, 5, 0, 2)
                };

                int row = (i / columns) * 2;
                Grid.SetRow(textBlock, row);
                Grid.SetColumn(textBlock, i % columns);

                Grid.SetRow(chart, row + 1); 
                Grid.SetColumn(chart, i % columns);

                grid.Children.Add(textBlock);
                grid.Children.Add(chart);
            }

        }



        public class ChartModel
        {
            public string Title { get; set; }
            public SeriesCollection SeriesCollection { get; set; }
            public string[] TimeLabels { get; set; }
            public Func<double, string> Formatter { get; set; }
        }
    }

}
