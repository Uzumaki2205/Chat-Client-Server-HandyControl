using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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

namespace Chat_Client_Server_HandyControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        public IPEndPoint IP { get; set; }
        public Socket Client { get; set; }
        public string Nickname { get; set; }
        public MainWindow()
        {
            // Client
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() => ConnectToServer());
            thread.IsBackground = true;
            thread.Start();
        }
        private void btnNickname_Click(object sender, RoutedEventArgs e)
        {
            SendNickname();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Client.Send(Serialize($"{Nickname}:exit"));
            //Closes();
            Client.Shutdown(SocketShutdown.Both);
            MessageBox.Show("You have disconnect to server");
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMess.Text != string.Empty)
            {
                Send();
                lsvMess.Items.Add(new ListViewItem() { Content = tbxMess.Text });
                tbxMess.Clear();
            }
        }

        private void tbxMess_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSendMess_Click(sender, null);
            }
        }

        private void ConnectToServer()
        {
            try
            {
                IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Connect(IP);
                MessageBox.Show("Connect Success!!");
            }
            catch (Exception)
            {
                MessageBox.Show("Not connect to server....");
            }

            Thread listen = new Thread(ReceiveMessage);
            listen.IsBackground = true;
            listen.Start();
        }

        private void SendNickname()
        {
            if (string.IsNullOrEmpty(tbxNickName.Text))
                Nickname = "Anonymous";
            else Nickname = tbxNickName.Text;

            MessageBox.Show("You apply nickname: " + Nickname);
            Client.Send(Serialize($"[Authorization]:{Nickname}"));
        }

        private void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    Client.Receive(data, 0, data.Length, SocketFlags.None);

                    string message = (string)Deserialize(data);
                    this.Dispatcher.BeginInvoke(new Action(() => { AddMessage(message); }));
                }
            }
            catch (Exception)
            {
                Closes();
            }
        }
        private void Send()
        {
            try
            {
                Client.Send(Serialize($"{Nickname}:{tbxMess.Text}"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSendMess_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMess.Text != string.Empty)
            {
                Send();
                lsvMess.Items.Add(new ListViewItem() { Content = tbxMess.Text });
                tbxMess.Clear();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Closes();
        }

        private void Closes()
        {
            Client.Close();
        }

        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        void AddMessage(string s)
        {
            lsvMess.Items.Add(new ListViewItem() { Content = s });
            tbxMess.Clear();
        }

        private void btnCommand_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Tag.Equals("Close"))
            {
                Application.Current.Shutdown();
            }
            if (btn.Tag.Equals("Maximize"))
            {
                if (this.WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else WindowState = WindowState.Maximized;
            }
            if (btn.Tag.Equals("Minimize"))
            {
                if (this.WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;
                else WindowState = WindowState.Minimized;
            }
        }
    }

    public class HelperConnect
    {
        public IPEndPoint IP { get; set; }
        public Socket Client { get; set; }

        public HelperConnect()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }
    }
}
