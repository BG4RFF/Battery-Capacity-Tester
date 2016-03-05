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
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Windows.Controls.DataVisualization.Charting;
namespace Battery_Capacity_Tester
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.IO.Ports.SerialPort _serialPort;
        string currentFile = "";
        Boolean fileStarted = false;
        Boolean filePaused = false;
        Boolean portOpened = false;
        DateTime lastReading, startTime;

        ObservableCollection<MeasurementObject> pastReadings = new ObservableCollection<MeasurementObject> { };
        MeasurementObject latestRecording;
        public MainWindow()
        {
            InitializeComponent();
           
        }

        private void btnConnectToReLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CBPortsSelection.SelectedIndex >= 0)
            {
                if (((string)btnConnectToReLoad.Content) == "Connect")
                {
                    string portName = (string)CBPortsSelection.Items[CBPortsSelection.SelectedIndex];
                    _serialPort = new SerialPort(portName, 115200);
                    _serialPort.DataReceived += _serialPort_DataReceived;
                    _serialPort.Open();
                    DateTime start = DateTime.Now;
                    while (lastReading == null || (DateTime.Now - lastReading).TotalSeconds > 3)
                    {
                        if ((DateTime.Now - start).TotalSeconds > 3)
                            _serialPort.WriteLine("monitor 500");//send the monitor command
                        if ((DateTime.Now - start).TotalSeconds > 10)
                        {
                            _serialPort.Close();
                            return;
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                    vStopUpDown.IsEnabled = true;
                    iSetUpDown.IsEnabled = true;
                    btnStartStopLogging.IsEnabled = true;
                    btnpause.IsEnabled = true;
                    btnConnectToReLoad.Content = "Disconnect";
                    portOpened = true;
                }
                else
                {
                    _serialPort.DataReceived -= _serialPort_DataReceived;
                    lock (_serialPort)
                    {
                        _serialPort.Close();
                        _serialPort = null;
                    } 
                    portOpened = false;
                    btnConnectToReLoad.Content = "Connect";
                }
            }
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //we have recieved data
            while (_serialPort.BytesToRead > 0)
            {
                string rec = _serialPort.ReadLine().Replace("\r", "").Replace("\n", "");
                string[] words = rec.Split(' ');
                switch (words[0])
                {
                    case "read":
                        {
                            lastReading = DateTime.Now;
                            //file isnt paused, log results
                                addreading(words);
                            
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        private void addreading(string[] data)
        {//process the incoming data
            double voltage, current, mah, mwh;
            voltage = current = mah = mwh = 0;
            current = double.Parse(data[1]);
            voltage = double.Parse(data[2]) / 1000;
            mah = double.Parse(data[3]) / 1000;
            mwh = double.Parse(data[4]) / 1000;

            MeasurementObject m = new MeasurementObject(DateTime.Now,startTime, voltage, current, mah, mwh);
            //add the entry to the list
            latestRecording = m;
            if (m.Current != 0 && fileStarted && !filePaused)
            {

                lock (pastReadings)
                {
                    try
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => pastReadings.Add(m)));
                    }
                    catch { }
                    //   pastReadings.Add(m);
                    m.Append(currentFile, startTime);
                }
                
                //write it out to the save file
                
            }
            updateDisplay();
        }
        private void updateDisplay()
        {
            if (portOpened)
            {
                lblCurrentVoltage.Dispatcher.Invoke(new Action(() => lblCurrentVoltage.Content = string.Format("{0} V", latestRecording.Voltage)));
                lblCurrentCurrent.Dispatcher.Invoke(new Action(() => lblCurrentCurrent.Content = string.Format("{0} A", latestRecording.Current / 1000)));
                lblCurrentCapacity.Dispatcher.Invoke(new Action(() => lblCurrentCapacity.Content = string.Format("{0} mAh", latestRecording.mAh)));
                lblCurrentWattHours.Dispatcher.Invoke(new Action(() => lblCurrentWattHours.Content = string.Format("{0} mWh", latestRecording.mWh)));
                lblCurrentRuntime.Dispatcher.Invoke(new Action(() => lblCurrentRuntime.Content = string.Format("{0}", ((lastReading - startTime).TotalSeconds / 60))));
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CBPortsSelection.Items.Clear();
            foreach (var port in System.IO.Ports.SerialPort.GetPortNames())
                CBPortsSelection.Items.Add(port);
            if (CBPortsSelection.Items.Count > 0)
                CBPortsSelection.SelectedIndex = 0;

        }

        private void btnStartStopLogging_Click(object sender, RoutedEventArgs e)
        {
            if (((string)btnStartStopLogging.Content) == "Start")
            {//starting logging
                if (txtFileName.Text.Length > 0)
                {
                    if (!System.IO.Directory.Exists("Results"))
                        System.IO.Directory.CreateDirectory("Results");
                    if (!System.IO.Directory.Exists("Results\\" + txtFileName.Text + "\\"))
                        System.IO.Directory.CreateDirectory("Results\\" + txtFileName.Text + "\\");

                    string fName = "Results\\" + txtFileName.Text + "\\" + iSetUpDown.Value.ToString() + ".csv";
                    if (System.IO.File.Exists(fName))
                    {
                        MessageBoxResult result = MessageBox.Show("Overwrite Existing File?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    //if we get here then the user is okay to overwrite the file
                    currentFile = fName;
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(currentFile, false))//no append here
                    {
                        sw.WriteLine(String.Format("Recording,{0}", txtFileName.Text));
                        sw.WriteLine(String.Format("Current Setpoint,{0}", iSetUpDown.Value));
                        sw.WriteLine(String.Format("Low Voltage Stop,{0}", vStopUpDown.Value));
                        sw.WriteLine("Seconds, Voltage, Current, mAh, mWh");
                    }
                    startTime = DateTime.Now;

                    _serialPort.WriteLine("set " + iSetUpDown.Value.ToString());
                    _serialPort.WriteLine("uvlo " + vStopUpDown.Value.ToString());
                    iSetUpDown.IsEnabled = false;
                    vStopUpDown.IsEnabled = false;
                    pastReadings.Clear();
                    chart.DataContext = pastReadings;
                    btnStartStopLogging.Content = "Stop";
                    fileStarted = true; filePaused = false;
                }
            }
            else
            {//stopping logging
                fileStarted = false; filePaused = false;

                iSetUpDown.IsEnabled = true;
                vStopUpDown.IsEnabled = true;
                btnStartStopLogging.Content = "Start";
            }
        }

        private void btnpause_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// used to turn axis on and off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (sender as CheckBox);
            LineSeries ls = chart.Series.Cast<LineSeries>().Where(s => s.Title.ToString() == chk.Tag.ToString()).ElementAtOrDefault(0);
            if (chk.IsChecked.Value)
                chk.Opacity = ls.Opacity = 1;
            else
            {
                chk.Opacity = 0.5;
                ls.Opacity = 0;
            }
            
        }

        
    }
    class MeasurementObject
    {
        public DateTime startTime;
        public double TimeStamp { get { return (CaptureTime - startTime).TotalSeconds; } }
        public DateTime CaptureTime { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double mAh { get; set; }
        public double mWh { get; set; }
        public MeasurementObject(DateTime time,DateTime start, double voltage, double current, double mah, double mwh)
        {

            startTime = start;
            Voltage = voltage;
            CaptureTime = time;
            Current = current;
            mAh = mah;
            mWh = mwh;
        }
        public string toString(DateTime startTime)
        {
            return String.Format("{0},{1},{2},{3},{4}", (CaptureTime - startTime).TotalSeconds, Voltage, Current, mAh, mWh);
        }
        public void Append(string fileName, DateTime startTime)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileName, true))
            {
                sw.WriteLine(toString(startTime));

            }
        }
    }
}
