using System;
using System.Collections.Generic;
using System.Text;

namespace TioCSharpClient
{
    public class TioConfig
    {
        private string ip;

        public string Ip { get => ip; set => ip = value; }
      

        private int port;

        public int Port { get => port; set => port = value; }
       
        private int timeout;
        public int Timeout { get => timeout; set => timeout = value; }


    }
}
