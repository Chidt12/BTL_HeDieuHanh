using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;

using System.Diagnostics;
using System.Management;
using System.Dynamic;
using System.Windows.Interop;
using System.Threading;
using System.Collections.ObjectModel;

namespace TaskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            listProcessView.ItemsSource = ListItems;
            canReload = false;
            btnReload.Content = "Loading ...";
            GetProcess();

        }

        List<Process> processes;
        //List<ProcessItem> listItems;

        private ObservableCollection<ProcessItem> listItems;
        public ObservableCollection<ProcessItem> ListItems
        {
            get
            {
                if (listItems == null)
                {
                    listItems = new ObservableCollection<ProcessItem>();
                }
                return listItems;
            }
            set
            {
                listItems = value;
            }
        }

        async void GetProcess()
        {
            ListItems.Clear();

            processes = Process.GetProcesses().ToList();

            var counters = new List<PerformanceCounter>();

            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();

            long totalPhysicalMemory = 0;

            foreach (ManagementObject result in results)
            {
                totalPhysicalMemory = Int64.Parse(result["TotalVisibleMemorySize"].ToString());
            }

            foreach (var item in processes)
            {
                var counter = new PerformanceCounter("Process", "% Processor Time", item.ProcessName);
                try { counter.NextValue(); } catch { counter.NextValue(); };
                counters.Add(counter);
            }

            int i = 0;

            await Task.Delay(1000);

            foreach (var item in processes)
            {
                float counterValue = 0;
                try
                {
                    counterValue = counters[i].NextValue();
                }
                catch
                {
                    
                }
                double CPUPercent = Math.Round(counterValue, 1);

                i++;

                var processName = ProcessExecutablePath(item);
                if (!string.IsNullOrEmpty(processName))
                {
                    Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(processName);

                    ListItems.Add(new ProcessItem()
                    {
                        Id = item.Id,
                        Name = System.IO.Path.GetFileName(processName),
                        Icon = ToImageSource(icon),
                        CPUPercent = CPUPercent,
                        MemoryPercent = (item.WorkingSet64 * 100 / (totalPhysicalMemory * 1000)).ToString()
                    }); ;
                }
            }

            btnReload.Content = "Reload";
            canReload = true;
        }

        public class ProcessItem 
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double CPUPercent { get; set; }
            public string MemoryPercent { get; set; }
            public int DiskPercent { get; set; }
            public int NetworkPercent { get; set; }
            public ImageSource Icon { get; set; }
        }


        static private string ProcessExecutablePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + process.Id;
                using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject mo in results)
                        {
                            return "";
                        }
                    }
                }
                return null;
            }

            return "";
        }

        static private ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        public bool canReload { get; set; }
        public string StatusReload { get; set; }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (canReload)
            {
                btnReload.Content = "Loading ...";
                canReload = false;
                GetProcess();
            }
        }

        private void CutCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
           
        }

        private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItem = listProcessView.SelectedItem as ProcessItem;
            var send = sender as MenuItem;
            try
            {
               var process = processes.Where(x => x.Id == selectedItem.Id).FirstOrDefault();
               if(process != null)
                {
                    process.Kill();
                }

                processes.Remove(process);
                ListItems.Remove(ListItems.Where(x => x.Id == selectedItem.Id).FirstOrDefault());
            }
            catch
            {
                MessageBox.Show("Can not end this task");
            }
            //e.Parameter;
        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItem = listProcessView.SelectedItem as ProcessItem;
            var process = Process.GetProcesses().Where(x => x.Id == selectedItem.Id).FirstOrDefault();
            if(process == null)
            {
                MessageBox.Show("Can not see the detail");
                processes.Remove(processes.Where(x => x.Id == selectedItem.Id).FirstOrDefault());
                ListItems.Remove(ListItems.Where(x => x.Id == selectedItem.Id).FirstOrDefault());
            }
            else
            {
                MessageBox.Show($"ID: {process.Id} \n ProcessName: {process.ProcessName} \n CPU: {process.WorkingSet64} bytes \n Process Time: {process.TotalProcessorTime.TotalSeconds} seconds");
            }
        }
    }
}
