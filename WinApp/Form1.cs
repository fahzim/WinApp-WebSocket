using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace WinApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private class jsonResponse
        {
            public string text { get; set; }
        }

        public ClientWebSocket ws { get; set; }
        
        //Simulating machine name or username...
        public string RandomMachineName { get; set; }

        /// <summary>
        /// Responsible for connect to server and set async monitors to receive data.
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToServer()
        {
            //URL for server
            var wsServer = new Uri("ws://localhost:5297/ws?appOrigin=WindowsAplication");

            //Initialize new ws client
            ws = new ClientWebSocket();
            
            try
            {
                //try connect
                label4.Text = "Connecting...";
                await ws.ConnectAsync(wsServer, CancellationToken.None);
                //if pass will connect.
                label4.Text = "Connected";
                button2.Text = "Disconnect";
                groupBox1.Enabled = true;

                //run while conn is open to check if has new messages.
                await Task.WhenAll(Receive(ws));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on Connecting: " + ex.Message);
                label4.Text = "Failed to Connect";
                button2.Text = "Connect";
            }
            finally
            {
                if (ws != null) ws.Dispose();
            }
        }

        private void Send(ClientWebSocket webSocket)
        {
            var bytesMessage = Encoding.UTF8.GetBytes($"({RandomMachineName}): {textBox1.Text}");

            if (webSocket.State == WebSocketState.Open)
            {
                webSocket.SendAsync(new ArraySegment<byte>(bytesMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                textBox1.Clear();
                textBox1.Focus();
            } else
            {
                label4.Text = "No Connection";
                button2.Text = "Connect";
                groupBox1.Enabled = false;
            }
        }

        private async Task Receive(ClientWebSocket webSocket)
        {
            byte[] buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                //Check if conn is not closed.
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    label4.Text = "Disconnected By Server";
                    button2.Text = "Connect";
                    groupBox1.Enabled = false;
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                { 
                    var decodeMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var deserializeMsg = JsonSerializer.Deserialize<jsonResponse>(decodeMsg);
                    listBox1.Items.Add(deserializeMsg.text);
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                }
            }
        }

        private async Task CloseConn(ClientWebSocket webSocket)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(button2.Text == "Connect")
            {
                listBox1.Items.Clear();
                button2.Text = "Connecting";
                ConnectToServer();
            } else
            {
                CloseConn(ws);
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Send(ws);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Random rnd = new Random();
            RandomMachineName = $"WinApp-{rnd.Next(10000,100000)}";
            this.Text = $"Fake Machine Name: {RandomMachineName}";
        }

    }
}
