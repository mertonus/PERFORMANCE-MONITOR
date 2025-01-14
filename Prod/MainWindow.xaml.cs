using LiveCharts;
using LiveCharts.Definitions.Charts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Separator = LiveCharts.Wpf.Separator;

namespace Prod
{
    public partial class MainWindow : Window
    {
        private SeriesCollection _totalSeries;
        private Grid _logicalProcessorsGrid;
        private CPU cpu = new CPU();

        public MainWindow()
        {
            InitializeComponent();

            var memory = new Memory();
            //CPU
            {
                InitializeTotalChart();
                InitializeLogicalProcessors();
                SetTotalChart();

                ProcessorInfoList.ItemsSource = cpu.processorInfo;
            }
            //Memory
            {
                MemoryChart.Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Memory Usage (%)",
                        Values = memory.MemoryValues,
                        PointGeometry = null
                    }
                };
                MemoryChart.AxisX.Add(new Axis
                {
                    Title = "Time",
                    MinValue = 0,
                    MaxValue = 60,
                    Separator = new LiveCharts.Wpf.Separator { Step = 60 }
                });

                MemoryChart.AxisY.Add(new Axis
                {
                    Title = "Memory",
                    MinValue = 0,
                    MaxValue = memory.MemoryCount 
                });
                Mem.ItemsSource = memory.memoryInfo;
            }
            //Disk
            CreateDynamicTabsDisk();
            //Network
            CreateDynamicTabsNetwork();
            //GPU
            {
                var allgpu = new AllGPU();
                DataContext = allgpu;

                allgpu.UpdateGpuCharts(GpuChartGrid);

                var gpu = new GPU();
                GPUInfoList.ItemsSource = gpu.gpuInfo;
            }
            
        }

        public void CreateDynamicTabsDisk()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            var disk = new Disk();
            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    TabItem tabItem = new TabItem
                    {
                        Header = drive.Name
                    };
                    var mainGrid = new Grid();
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var chartGrid = new Grid();
                    chartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
                    chartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var pieChart = new PieChart
                    {
                        Series = new SeriesCollection
                        {
                            new PieSeries
                            {
                                Title = "Empty disk space",
                                Values = new ChartValues<double> {  Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024 * 1024), 2)},
                                DataLabels = true
                            },
                            new PieSeries
                            {
                                Title = "Used disk space",
                                Values = new ChartValues<double> {  Math.Round((drive.TotalSize - drive.AvailableFreeSpace) / (1024.0 * 1024 * 1024), 2) },
                                DataLabels = true
                            }
                        },
                        LegendLocation = LegendLocation.Bottom
                    };
                    Grid.SetColumn(pieChart, 0);
                    chartGrid.Children.Add(pieChart);

                    var lineChart = new CartesianChart
                    {
                        Series = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Title = "Disk Usage (%)",
                                Values = new ChartValues<double>(),
                                PointGeometry = null
                            }
                        },
                        AxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Time",
                                Labels = new List<string>()
                            }
                        },
                        AxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "Disk Usage (%)",
                                MinValue = 0,
                                MaxValue = 100
                            }
                        }
                    };
                    disk.diskUsageData[drive.Name] = (ChartValues<double>)lineChart.Series[0].Values;
                    Grid.SetColumn(lineChart, 1);
                    chartGrid.Children.Add(lineChart);

                    Grid.SetRow(chartGrid, 0);
                    mainGrid.Children.Add(chartGrid);

                    var diskInfo = disk.GetDiskInfo(drive);
                    var listView = new ListView
                    {
                        ItemsSource = diskInfo,
                        View = new GridView
                        {
                            Columns =
                            {
                                new GridViewColumn
                                {
                                    Header = "Property",
                                    DisplayMemberBinding = new System.Windows.Data.Binding("Key"),
                                    Width = 150
                                },
                                new GridViewColumn
                                {
                                    Header = "Value",
                                    DisplayMemberBinding = new System.Windows.Data.Binding("Value"),
                                    Width = 300
                                }
                            }
                        },
                        Margin = new Thickness(10)
                    };
                    Grid.SetRow(listView, 1);
                    mainGrid.Children.Add(listView);

                    tabItem.Content = mainGrid;

                    DynamicTabControlDisk.Items.Add(tabItem);
                }
            }
        }

        public void CreateDynamicTabsNetwork()
        {
            var network = new Network();

            foreach (var instance in network.SendData.Keys)
            {
                TabItem tabItem = new TabItem
                {
                    Header = instance
                };

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var chartGrid = new Grid();
                chartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                chartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var sendChart = new CartesianChart
                {
                    Series = new SeriesCollection
                {
                new LineSeries
                {
                    Title = "Send (KB/s)",
                    Values = network.SendData[instance],
                    PointGeometry = null
                }
            },
                    AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "Time",
                    Labels = new List<string>()
                }
            },
                    AxisY = new AxesCollection
            {
                new Axis
                {
                    Title = "Speed (KB/s)",
                    MinValue = 0
                }
            }
                };
                Grid.SetColumn(sendChart, 0);
                chartGrid.Children.Add(sendChart);

                var receiveChart = new CartesianChart
                {
                    Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Receive (KB/s)",
                    Values = network.ReceiveData[instance],
                    PointGeometry = null
                }
            },
                    AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "Time",
                    Labels = new List<string>()
                }
            },
                    AxisY = new AxesCollection
            {
                new Axis
                {
                    Title = "Speed (KB/s)",
                    MinValue = 0
                }
            }
                };
                Grid.SetColumn(receiveChart, 1);
                chartGrid.Children.Add(receiveChart);

                Grid.SetRow(chartGrid, 0);
                mainGrid.Children.Add(chartGrid);

                var interfaceInfo = network.GetNetworkInterfaceInfo(instance);
                var listView = new ListView
                {
                    ItemsSource = interfaceInfo,
                    View = new GridView
                    {
                        Columns =
                {
                    new GridViewColumn
                    {
                        Header = "Property",
                        DisplayMemberBinding = new System.Windows.Data.Binding("Key"),
                        Width = 150
                    },
                    new GridViewColumn
                    {
                        Header = "Value",
                        DisplayMemberBinding = new System.Windows.Data.Binding("Value"),
                        Width = 300
                    }
                }
                    },
                    Margin = new Thickness(10)
                };
                Grid.SetRow(listView, 1);
                mainGrid.Children.Add(listView);

                tabItem.Content = mainGrid;

                DynamicTabControlNetwork.Items.Add(tabItem);
            }
        }

        private void InitializeTotalChart()
        {
            _totalSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "CPU Usage (%)",
                    Values = cpu.CpuValues,
                    PointGeometry = null
                }
            };
        }

        private void InitializeLogicalProcessors()
        {
            var processor = new AllCPU();
            int processors = Environment.ProcessorCount;

            _logicalProcessorsGrid = new Grid
            {
                Margin = new Thickness(10)
            };

            int columns = (int)Math.Ceiling(Math.Sqrt(processors));
            int rows = (int)Math.Ceiling((double)processors / columns);

            for (int i = 0; i < rows; i++)
                _logicalProcessorsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < columns; i++)
                _logicalProcessorsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < processors; i++)
            {
                var chart = new CartesianChart
                {
                    Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = $"CPU {i} Usage (%)",
                            Values = processor.CpuUsageData[i], 
                            PointGeometry = null
                        }
                    },
                    AxisX = new AxesCollection
                    {
                        new Axis
                        {
                            Title = "Time",
                            MinValue = 0,
                            MaxValue = 60,
                            Separator = new LiveCharts.Wpf.Separator { Step = 60 }
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
                    Margin = new Thickness(5)
                };

                Grid.SetRow(chart, i / columns);
                Grid.SetColumn(chart, i % columns);
                _logicalProcessorsGrid.Children.Add(chart);
            }
        }

        private void SetTotalChart()
        {
            var totalChart = new CartesianChart
            {
                Series = _totalSeries,
                AxisX = new AxesCollection
                {
                    new Axis
                    {
                        Title = "Time",
                        MinValue = 0,
                        MaxValue = 60,
                        Separator = new Separator { Step = 60 }
                    }
                },
                AxisY = new AxesCollection
                {
                    new Axis
                    {
                        Title = "CPU Usage (%)",
                        MinValue = 0,
                        MaxValue = 100
                    }
                },
                Margin = new Thickness(10)
            };

            MainContent.Content = totalChart;
        }

        private void SetLogicalProcessorsGrid()
        {
            MainContent.Content = _logicalProcessorsGrid;
        }


        private void MenuItem_Total_Click(object sender, RoutedEventArgs e)
        {
            SetTotalChart();
        }

        private void MenuItem_LogicalProcessors_Click(object sender, RoutedEventArgs e)
        {
            SetLogicalProcessorsGrid();
        }

    }
}
