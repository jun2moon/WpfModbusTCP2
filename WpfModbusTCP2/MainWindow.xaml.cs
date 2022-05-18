using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Sockets;  // NuGet으로 설치해야 함
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

            tcpClient = new TcpClient("127.0.0.1", 502);
            stream = tcpClient.GetStream();

            InitializeEnableStates(true);
            timer1.Interval = new TimeSpan(0, 0, 1);
            timer1.Tick += Timer1_Tick;
            timer1.IsEnabled = false;
        }

        private void comIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comIP.Text = comIP.SelectedItem.ToString();
        }

        private void btOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //tcpClient = new TcpClient(comIP.Text, 502); // ip + 포트 (local host: 127.0.0.1)(보통 예: 192.168.10.10)
                //stream = tcpClient.GetStream();

                if (!tcpClient.Connected)
                {
                    tcpClient.ConnectAsync(comIP.Text, 502);
                    stream = tcpClient.GetStream();
                }
                InitializeEnableStates(false);
                btStop.IsEnabled = false;
            }
            catch
            {
                MessageBox.Show("No IP selected!", "Warning");
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
                byte slaveAddress = Convert.ToByte(tbID.Text);  // 0x01;  // 국번
                byte functionCode = Convert.ToByte(tbFunc.Text);  // 0x01;  // 펑션 코드
                byte addressH = Convert.ToByte(tbAddressH.Text);  // 0x00;
                byte addressL = Convert.ToByte(tbAddressL.Text);  // 0x00;  // 시작 주소
                byte dataH = Convert.ToByte(tbDataH.Text);  // 0x00;
                byte dataL = Convert.ToByte(tbDataL.Text); // 0x09;  // 데이터 개수

                // 데이터 송신
                byte[] bytesSent = new byte[] {0,0,0,0,0,6, slaveAddress, functionCode, addressH, addressL, dataH, dataL };
                stream.Write(bytesSent, 0, bytesSent.Length);
                tbSend.Text = BitConverter.ToString(bytesSent);

                await Task.Delay(50);

                // 데이터 수신
                int bytesLength = functionCode switch {
                    1 => 10 + dataL/8,
                    2 => 10 + dataL/8,
                    3 => 9 + dataL*2,
                    4 => 9 + dataL*2,
                    5 => 10,
                    6 => 10,
                    _ => 10
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
    }
}
