using System.Net.Sockets;

namespace ControlDoorsApi.Common
{
    public class ShutterClass
    {
        private TcpClient tclient;
        private NetworkStream ns1;
        private NetworkStream ns2;
        private int sAddr;
        private int sPort;
        private string sIP; 
        private static ShutterClass instance;

        private static readonly object lockobj = new object();//线程锁

        public static ShutterClass Instance => GetInstance();

        public static ShutterClass GetInstance()
        {
            if (instance == null)
            {
                lock (lockobj)
                {
                    if (instance == null)
                    {
                        instance = new ShutterClass();
                    }
                }
            }
            return instance;
        }
        public ShutterClass()
        {

        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool JYConnet(string IP, int port)
        {
            
            try
            {
                if (tclient.Connected == false)
                {
                    tclient.Connect(IP, port);
                    ns1 = tclient.GetStream();
                    ns2 = tclient.GetStream();
                    sIP = IP;
                    sPort = port;

                }
                return true;
            }
            catch (Exception ex)
            {
                tclient.Close();
                tclient = new TcpClient();
                return false;
            }

        }
        public bool Connet()
        {
            tclient?.Close();
            tclient = new TcpClient();
            tclient.Connect("192.168.10.23", 10000);
            ns1 = tclient.GetStream();
            ns2 = tclient.GetStream();
            return true; 
        }

        public void JYDisconnect()
        {
            try
            {
                if (tclient.Connected == true)
                {
                    tclient.Close();
                    tclient.Dispose();
                }
            }
            catch(Exception ex)
            {

            }
        }

        DateTime conntime = DateTime.Now;

        private void connectClient()
        {
            try
            {
                if (tclient == null)
                {
                    conntime = DateTime.Now;
                    tclient = new TcpClient();
                    tclient.BeginConnect(sIP, sPort, new AsyncCallback(ConnectCallback), tclient);
                    //因为要访问ui资源，所以需要使用invoke方式同步ui。

                }
                else if (tclient.Connected == false)
                {
                    tclient.Connect(sIP, sPort);
                    ns1 = tclient.GetStream();
                    ns2 = tclient.GetStream();
                }
            }
            catch (Exception ex)
            {
                tclient = null;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                //tclient = (TcpClient)ar.AsyncState;
                if (tclient.Connected == true)
                {
                    ns1 = tclient.GetStream();
                    ns2 = tclient.GetStream();

                }
                else
                    connectClient();
            }
            catch (Exception)
            {
                connectClient();
            }
        }

        private byte[] sendinfo(byte[] info)
        {
            if (tclient == null)
            {
                connectClient();
                return null;
            }

            if (tclient.Connected == false)
            {
                if (conntime.AddMilliseconds(5 * Convert.ToInt32(100)) < DateTime.Now)
                {
                    tclient.Close();
                    tclient = null;
                }
                return null;
            }
            tclient.SendTimeout = 300;
            tclient.ReceiveTimeout = 300;
            try
            {

                try
                {
                    if (ns1 == null)
                    {
                        ns1 = tclient.GetStream();

                    }
                    ns1.WriteTimeout = 3 + info.Length;
                    ns1.Write(info, 0, info.Length);
                }
                catch (Exception)
                {
                    tclient = null;
                    return null;
                }


                byte[] data = RcvData();
                if (data == null) return null;
                return analysisRcv(data, data.Length);
            }
            catch (Exception)
            {

            }
            return null;
        }

        private byte[] RcvData()
        {
            byte[] info = new byte[2048 + 10];
            int len = 0;
            int retrycnt = 0;
            int timeout = 1000;

            Thread.Sleep(10);
            while (timeout > 0)
            {
                timeout -= 20;
                ns2.ReadTimeout = 5;
                try
                {
                    byte[] rcv = new byte[2048 + 10];
                    if (ns2 == null)
                    {
                        ns2 = tclient.GetStream();
                    }
                    int rdlen = ns2.Read(rcv, 0, 2048);
                    for (int i = 0; i < rdlen; i++)
                    {
                        if (len < 2048) info[len++] = rcv[i];
                    }

                    if (rdlen > 0) retrycnt = 0;
                }
                catch (Exception)
                {

                }
                if (len > 0)
                {
                    retrycnt++;
                    if (retrycnt > 3) timeout = 0;
                }
            }
            if (len < 5) return null;
            byte[] rst = new byte[len];
            for (int i = 0; i < len; i++)
                rst[i] = info[i];
            return rst;
        }
        private byte[] analysisRcv(byte[] src, int len)
        {
            if (len < 6) return null;


            switch (src[1])
            {
                case 0x01:
                    if (CMBRTU.CalculateCrc(src, src[2] + 5) == 0x00)
                    {
                        byte[] dst = new byte[src[2]];
                        for (int i = 0; i < src[2]; i++)
                            dst[i] = src[3 + i];
                        return dst;
                    }
                    break;
                case 0x02:
                    if (CMBRTU.CalculateCrc(src, src[2] + 5) == 0x00)
                    {
                        byte[] dst = new byte[src[2]];
                        for (int i = 0; i < src[2]; i++)
                            dst[i] = src[3 + i];
                        return dst;
                    }
                    break;
                case 0x04:
                    if (CMBRTU.CalculateCrc(src, src[2] + 5) == 0x00)
                    {
                        byte[] dst = new byte[src[2]];
                        for (int i = 0; i < src[2]; i++)
                            dst[i] = src[3 + i];
                        return dst;
                    }
                    break;
                case 0x05:
                    if (CMBRTU.CalculateCrc(src, 8) == 0x00)
                    {
                        byte[] dst = new byte[1];
                        dst[0] = src[4];
                        return dst;
                    }
                    break;
                case 0x0f:
                    if (CMBRTU.CalculateCrc(src, 8) == 0x00)
                    {
                        byte[] dst = new byte[1];
                        dst[0] = 1;
                        return dst;
                    }
                    break;
                case 0x06:
                    if (CMBRTU.CalculateCrc(src, 8) == 0x00)
                    {
                        byte[] dst = new byte[4];
                        dst[0] = src[2];
                        dst[1] = src[3];
                        dst[2] = src[4];
                        dst[3] = src[5];
                        return dst;
                    }
                    break;
                case 0x10:
                    if (CMBRTU.CalculateCrc(src, 8) == 0x00)
                    {
                        byte[] dst = new byte[4];
                        dst[0] = src[2];
                        dst[1] = src[3];
                        dst[2] = src[4];
                        dst[3] = src[5];
                        return dst;
                    }
                    break;
            }
            return null;
        }

        public void OpenDO(int addr, int io)
        {
            byte[] info = CModbusDll.WriteDO(addr, io, true);
            sendinfo(info);
        }

        public void CloseDO(int addr, int io)
        {
            byte[] info = CModbusDll.WriteDO(addr, io, false);
            sendinfo(info);
        }

        public byte[] ReadDI(int addr, int io)
        {
            byte[] info = CModbusDll.ReadDI(addr, io); 
            byte[] rst = sendinfo(info);
            return rst;
        }
    }
}