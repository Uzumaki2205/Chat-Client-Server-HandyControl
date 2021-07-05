using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
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

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        public int DefaultPort { get; set; }
        public IPEndPoint IP { get; set; }
        public Socket Server { get; set; }
        public List<Users> ClientList { get; set; }
        private static BackgroundWorker backgroundWorker;
        public MainWindow()
        {
            // Server
            DefaultPort = 8888;
            InitializeComponent();
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            if (tbxPort.Text != string.Empty)
            {
                try
                {
                    DefaultPort = int.Parse(tbxPort.Text);
                    MessageBox.Show("Server is Started...");
                    backgroundWork();
                }
                catch (Exception)
                {
                    MessageBox.Show("Port is invalid");
                }
            }
            else
            {
                MessageBox.Show("Server is Started...");
                backgroundWork();
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (tbxMess.Text != string.Empty)
            {
                try
                {
                    foreach (var item in ClientList)
                    {
                        Send(item.IPUser);
                    }

                    AddMessage(tbxMess.Text);
                    tbxMess.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        void ConnectAsync()
        {
            try
            {
                ClientList = new List<Users>();

                IP = new IPEndPoint(IPAddress.Any, DefaultPort);
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                Server.Bind(IP);

                while (true)
                {
                    Server.Listen(100);
                    Socket client = Server.Accept();
                    //ClientList.Add(client);

                    System.Threading.Thread receive = new System.Threading.Thread(Receive);
                    receive.IsBackground = true;
                    receive.Start(client);
                }
            }
            catch (Exception)
            {
                IP = new IPEndPoint(IPAddress.Any, 9999);
                Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            }
        }


        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Closes();
            MessageBox.Show("ShutDown Server....");
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectAsync();
        }

        void backgroundWork()
        {
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync(10000);

            if (backgroundWorker.IsBusy)
                backgroundWorker.CancelAsync();
        }

        /// <summary>
        /// Receive message from client
        /// </summary>
        /// <param name="obj"></param>
        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);

                    if (message.Contains("exit"))
                    {
                        Users u = new Users();
                        string nickname = message.Replace(":exit", "");
                        if (ClientList.Count != 0)
                        {
                            foreach (var item in ClientList)
                            {
                                if (item.NickName == nickname)
                                {
                                    u = item;
                                }
                            }
                            
                            ClientList.Remove(u);
                        }
                    }
                    else if (!message.Contains("[Authorization]"))
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddMessage($"{message}");
                        }));
                    }
                    else
                    {
                        string nickname = message.Replace("[Authorization]:", "");
                        Users user = new Users() { IPUser = client, NickName = nickname };

                        int i = 0;
                        if (ClientList.Count != 0)
                        {
                            foreach (var item in ClientList)
                            {
                                if (item.NickName.Equals(nickname))
                                    i++;
                            }
                            if (i == 0)
                            {
                                ClientList.Add(user);
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    cbxUser.Items.Add(new ComboBoxItem() { Content = nickname });
                                }));
                            }
                            else client.Send(Serialize("User is exist"));
                        }
                        else
                        {
                            ClientList.Add(user);
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                cbxUser.Items.Add(new ComboBoxItem() { Content = nickname });
                            }));
                        }
                    }
                    //lsvMess.ItemsSource = ClientList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Your Error: " + ex.Message);
                foreach (var item in ClientList)
                {
                    if (item.IPUser.Equals(client))
                    {
                        ClientList.Remove(item);
                    }   
                }
                client.Close();
            }
        }

        /// <summary>
        /// Add Message To List View
        /// </summary>
        /// <param name="s"></param>
        void AddMessage(string s)
        {
            lsvMess.Items.Add(new ListViewItem() { Content = s });
            tbxMess.Clear();
        }

        /// <summary>
        /// Send Message as Socket
        /// </summary>
        /// <param name="client"></param>
        void Send(Socket client)
        {
            client.Send(Serialize(tbxMess.Text));
        }

        void Closes()
        {
            Server.Close();
        }

        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(Server != null)
                Closes();
        }

        private void tbxMess_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSend_Click(sender, null);
            }
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
    public class Users
    {
        public Socket IPUser { get; set; }
        public string NickName { get; set; }
    }
}
