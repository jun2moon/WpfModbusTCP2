using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Sockets;
using System.Windows.Threading;

namespace WpfModbusTCP2
{
    public partial class MainWindow : Window
    {
        TcpClient tcpClient;
        NetworkStream stream;
        DispatcherTimer timer1 = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            InitializeEnableStates(true);
            timer1.Interval = new TimeSpan(0, 0, 1);
            timer1.Tick += Timer1_Tick;
            timer1.IsEnabled = false;

            try
            {
                tcpClient = new TcpClient("127.0.0.1", 502);  // local host
                stream = tcpClient.GetStream();
            }
            catch
            {
                MessageBox.Show("There is no Modbus server at local host", "Error");
            }
        }

        private void tbFunc_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                byte functionCode = Convert.ToByte(tbFunc.Text);
                switch (functionCode)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        break;
                    default:
                        MessageBox.Show("Function codes 1 to 6 can be executed", "Notice");
                        break;
                }
                tbFunc.Text = functionCode.ToString();
            }
            catch
            {
                MessageBox.Show("Input an unsigned 8-bit integer", "Error");
            }
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tcpClient = new TcpClient(tbIP.Text, int.Parse(tbPort.Text));
                stream = tcpClient.GetStream();
                InitializeEnableStates(false);
                btStop.IsEnabled = false;
            }
            catch
            {
                MessageBox.Show("IP connection error!", "Error");
            }
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            timer1.IsEnabled = false;
            tcpClient.Close();
            InitializeEnableStates(true);
            tbReceive.Text = "";
            tbSend.Text = "";
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            btStart.IsEnabled = false;
            btStop.IsEnabled = true;
            timer1.IsEnabled = true;
        }

        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            btStart.IsEnabled = true;
            btStop.IsEnabled = false;
            timer1.IsEnabled = false;
        }

        void InitializeEnableStates(bool t)
        {
            btOpen.IsEnabled = t;
            btClose.IsEnabled = !t;
            btStart.IsEnabled = !t;
            btStop.IsEnabled = !t;
            tbReceive.IsEnabled = !t;
            tbSend.IsEnabled = !t;

            tbID.IsEnabled = t;
            tbFunc.IsEnabled = t;
            tbAddressH.IsEnabled = t;
            tbAddressL.IsEnabled = t;
            tbDataH.IsEnabled = t;
            tbDataL.IsEnabled = t;
        }

        private async void Timer1_Tick(object? sender, EventArgs e)
        {
            try
            {
                byte slaveAddress = Convert.ToByte(tbID.Text, 16);  // 0x01;  // 국번 (16진수)
                byte functionCode = Convert.ToByte(tbFunc.Text, 16);  // 0x01;  // 펑션 코드 (16진수)
                byte addressH = Convert.ToByte(tbAddressH.Text, 16);  // 0x00;
                byte addressL = Convert.ToByte(tbAddressL.Text, 16);  // 0x00;  // 시작 주소 (16진수)
                byte dataH = Convert.ToByte(tbDataH.Text, 16);  // 0x00;
                byte dataL = Convert.ToByte(tbDataL.Text, 16); // 0x09;  // 데이터 개수 (16진수)

                // 데이터 송신
                byte[] bytesSent = new byte[] { 0, 0, 0, 0, 0, 6, slaveAddress, functionCode, addressH, addressL, dataH, dataL };
                stream.Write(bytesSent, 0, bytesSent.Length);
                tbSend.Text = BitConverter.ToString(bytesSent);

                await Task.Delay(50);

                // 데이터 수신
                int bytesLength = functionCode switch
                {
                    1 => 9 + dataL / 8 + ((dataL % 8 == 0) ? 0 : 1),  // bit read (coil)
                    2 => 9 + dataL / 8 + ((dataL % 8 == 0) ? 0 : 1),  // bit read (input bit)
                    3 => 9 + dataL * 2,  // word read (holding register)
                    4 => 9 + dataL * 2,  // word read (input register)
                    5 => 12,            // bit write (coil)
                    6 => 12,            // word write (holding register)
                    _ => 12
                };
                byte[] bytesReceived = new byte[bytesLength];
                stream.Read(bytesReceived, 0, bytesReceived.Length);
                tbReceive.Text = BitConverter.ToString(bytesReceived);
            }
            catch
            {
                timer1.IsEnabled = false;
                MessageBox.Show("Unsigned 8-bit integers should be input", "Error");
            }
        }
    }
}