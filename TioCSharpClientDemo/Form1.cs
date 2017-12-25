using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TioCSharpClientDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        TioCSharpClient.TioClient tioClient;
        private void button1_Click(object sender, EventArgs e)
        {
            tioClient = new TioCSharpClient.TioClient("127.0.0.1", 6789,4000);
            tioClient.Start();
            tioClient.receiveHandler += TioClient_receiveHandler; 
            Thread thread = new Thread(HeartBeta);
            thread.Start();
        }

        private void TioClient_receiveHandler(string content)
        {
            Console.WriteLine("来自服务器:"+content);
        }

        void HeartBeta()
        {
            while (true)
            {
                if (tioClient.isAlive())
                {
                    tioClient.Send(Encoding.UTF8.GetBytes("信息" + DateTime.Now.ToString()));
                } 
                Thread.Sleep(4000);
            } 
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            tioClient.Stop();
        }
    }
}
