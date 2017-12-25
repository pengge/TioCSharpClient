using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TioCSharpClient
{
    public class TioClient
    {
        /// <summary>
        /// 配置
        /// </summary>
        TioConfig tioConfig;
        /// <summary>
        /// clientSocket变量
        /// </summary>
        Socket clientSocket;
        /// <summary>
        /// 是否暂停全部进程
        /// </summary>
        bool isStop = false;

        /// <summary>
        /// 是否重连
        /// </summary>
        bool isReconn = true;

        /// <summary>
        /// 重连的间隔时间，单位毫秒
        /// </summary>
        private int interval = 5000;

        /// <summary>
        /// 连续重连次数，当连续重连这么多次都失败时，不再重连。0和负数则一直重连
        /// </summary>
        private int retryCount = 0;

        /// <summary>
        /// 已经重新连接的次数
        /// </summary>
        private int alreadyRetryCount = 0;
        /// <summary>
        /// 是否存在错误需要重新连接
        /// </summary>
        private bool isError = false;

        public delegate void ReceiveHandler(string content);
        // 基于上面的委托定义事件
        public event ReceiveHandler receiveHandler;
        public TioClient()
        {

        }
        /// <summary>
        /// 设置是否重连 默认为自动重连  true:自动重新连接 false:不自动连接
        /// </summary>
        /// <param name="isReconn"></param>
        public void SetReConn(bool isReconn)
        {
            this.isReconn = isReconn;
        }
        /// <summary>
        /// 默认0  连续重连次数，当连续重连这么多次都失败时，不再重连。0和负数则一直重连【注意如果isReconn 是false的话，这里设置什么也米有什么用】
        /// </summary>
        /// <param name="retryCount"></param>
        public void SetRetryCount(int retryCount)
        {
            this.retryCount = retryCount;
        }
        public bool isAlive()
        {
            return !this.isStop;
        }
        /*
        public TioClient(string ip,int port)
        { 
            this.tioConfig  = new TioConfig();
            this.tioConfig.Ip = ip;
            this.tioConfig.Port = port;
            this.tioConfig.Timeout = 999999;//无限等待
        }*/
        public TioClient(string ip, int port, int timeout)
        {
            this.tioConfig = new TioConfig();
            this.tioConfig.Ip = ip;
            this.tioConfig.Port = port;
            this.tioConfig.Timeout = timeout;
        }
        public TioClient(TioConfig tioConfig)
        {
            this.tioConfig = tioConfig;
        }

        public void Start()
        {

            IPAddress ip = IPAddress.Parse(tioConfig.Ip);
            IPEndPoint ipe = new IPEndPoint(ip, tioConfig.Port);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(ipe);

            //准备接收线程
            Thread reciveThread = new Thread(Recive);
            reciveThread.Start();

            //发送心跳线程
            Thread heartThread = new Thread(HeartBeta);
            heartThread.Start();

            if (isReconn == true)
            {
                //准备重连线程
                Thread retryThread = new Thread(ReConnectThread);
                retryThread.Start();
            }

        }
        private void ReConnectSocket()
        {
            if (clientSocket.Connected)
            {
                clientSocket.Close();
            }
            if (clientSocket != null)
            {
                clientSocket = null;
            }
            IPAddress ip = IPAddress.Parse(tioConfig.Ip);
            IPEndPoint ipe = new IPEndPoint(ip, tioConfig.Port);

            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(ipe);
                if (clientSocket.Connected)
                {
                    isError = false;
                    //重新连接成功
#if _DEGUG_
                    System.Console.WriteLine("重新连接成功");
#endif 
                }
            }
            catch (SocketException se)
            {
#if _DEGUG_
                System.Console.WriteLine("重新连接失败");
#endif     
            }


        }
        public void Send(byte[] sendBytes)
        {
            if (isStop == true)
            {
                Console.WriteLine("已经关闭了");
            }
            else
            {
                if (clientSocket != null && clientSocket.Connected)
                {
                    try
                    {

                        clientSocket.Send(TioCSharpClient.UPacket.Pack(sendBytes));
                    }
                    catch (SocketException se)
                    {
                        isError = true;
                    }
                }
                else
                {
                    isError = true;
                }
            }

        }

        public void ReConnectThread()
        {
            while (!isStop)
            {
                if (isError == true)
                {
                    if (retryCount <= 0)
                    {
                        //重新连接
                        alreadyRetryCount++;
                        ReConnectSocket();
#if _DEGUG_
                        System.Console.WriteLine("重新连接");
#endif 
                    }
                    else
                    {
                        if (alreadyRetryCount < retryCount)
                        {
                            //重新连接
                            alreadyRetryCount++;
                            ReConnectSocket();
#if _DEGUG_
                            System.Console.WriteLine("重新连接");
#endif
                        }
                        else
                        {
#if _DEGUG_
                            System.Console.WriteLine("不重新连接");
#endif 
                        }

                    }


                }

                Thread.Sleep(interval);
            }
        }
        public void Recive()
        {
            while (!isStop)
            {

                byte[] buffer = new byte[1024 * 3];
                SocketError socketError = new SocketError();
                try
                {
                    int r = clientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None, out socketError);
                    if (socketError == SocketError.Success)
                    {

                        //实际接收到的有效字节数
                        if (r == 0)
                        {
                            Thread.Sleep(5);
                            continue;
                        }
                        string recStr = Encoding.UTF8.GetString(TioCSharpClient.UPacket.UnPack(buffer));
                        receiveHandler.Invoke(recStr);
                    }
                    else if (socketError == SocketError.SocketError)
                    {
                        isError = true; //TODO 其实有待考虑一下 有时候 不是接收错误就要关闭把？
                        continue;
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.Message);
                    isError = true;
                }


                Thread.Sleep(5);
            }
        }
        void HeartBeta()
        {
            while (!isStop)
            {
                string sendStr = "";
                byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
                Send(TioCSharpClient.UPacket.Pack(sendBytes));

#if _DEGUG_
                System.Console.WriteLine("发送心跳");
#endif
                Thread.Sleep(tioConfig.Timeout);
            }
            //send message

        }
        public void Stop()
        {
            this.isStop = true;
            if (this.clientSocket != null)
            {
                this.clientSocket.Close();
                this.clientSocket = null;
            }
        }
    }
}