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
namespace Battery_Capacity_Tester
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.IO.Ports.SerialPort _serialPort;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnectToReLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CBPortsSelection.SelectedIndex >= 0)
            {
                string portName = (string)CBPortsSelection.Items[CBPortsSelection.SelectedIndex];
                _serialPort = new SerialPort(portName, 115200);
                _serialPort.DataReceived += _serialPort_DataReceived;
            }
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //we have recieved data

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CBPortsSelection.Items.Clear();
            foreach (var port in System.IO.Ports.SerialPort.GetPortNames())
                CBPortsSelection.Items.Add(port);
            if (CBPortsSelection.Items.Count > 0)
                CBPortsSelection.SelectedIndex = 1;

        }
    }
}
