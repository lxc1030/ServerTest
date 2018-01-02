using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace NetFrame.Net
{
    /// <summary>
    /// 用户对象
    /// </summary>
    public class AsyncUserToken
    {
        #region 字段

        /// <summary>
        /// 异步接收数据的缓冲区
        /// </summary>
        private byte[] _asyncReceiveBuffer;
        /// <summary>
        /// 同步发送数据的缓冲区
        /// </summary>
        private byte[] _syncSendBuffer;

        /// <summary>
        /// 接收和写入缓存的扩充倍数
        /// </summary>
        private static int sizeExtend = 4;

        private Queue<byte> _receiveBuffer;


        public Queue<byte> ReceiveBuffer
        {
            get { return _receiveBuffer; }
            set { _receiveBuffer = value; }
        }

        private Queue<byte> _sendBuffer;
        public Queue<byte> SendBuffer
        {
            get { return _sendBuffer; }
            set { _sendBuffer = value; }
        }

        public bool isDealSend { get; set; }

        /// <summary>
        /// 数据是否在循环处理中
        /// </summary>
        public bool isDealReceive { get; set; }
        public byte[] halfReceiveMessage { get; set; }
        public int sendIndex { get; set; }
        public int receiveIndex { get; set; }

        public Dictionary<int, byte[]> outOrders { get; set; }


        ///// <summary>
        ///// 动态的接收缓冲区
        ///// </summary>
        //private DynamicBufferManager _receiveBuffer;
        ///// <summary>
        ///// 动态的发送缓冲区
        ///// </summary>
        //private DynamicBufferManager _sendBuffer;

        /// <summary>
        /// 连接的Socket对象
        /// </summary>
        private Socket _connectSocket;


        /// <summary>
        /// 用户数据
        /// </summary>
        public RoomActor userInfo;


        #endregion

        #region 属性

        public Socket ConnectSocket
        {
            get { return _connectSocket; }
            set { _connectSocket = value; }
        }

        public byte[] AsyncReceiveBuffer
        {
            get { return _asyncReceiveBuffer; }
            set { _asyncReceiveBuffer = value; }
        }

        ///// <summary>
        ///// 异步接收数据的临时存储值
        ///// </summary>
        //public DynamicBufferManager ReceiveBuffer
        //{
        //    get { return _receiveBuffer; }
        //    set { _receiveBuffer = value; }
        //}

        //public DynamicBufferManager SendBuffer
        //{
        //    get { return _sendBuffer; }
        //    set { _sendBuffer = value; }
        //}

        #endregion

        public AsyncUserToken(int receiveBufferSize)
        {
            _connectSocket = null;

            //EventArgs = new SocketAsyncEventArgs();
            //EventArgs.UserToken = this;
            //EventArgs.SetBuffer(new Byte[receiveBufferSize], 0, receiveBufferSize);

            _asyncReceiveBuffer = new byte[receiveBufferSize];
            _syncSendBuffer = new byte[receiveBufferSize];

            Init();

            //_receiveBuffer = new DynamicBufferManager(receiveBufferSize * sizeExtend);
            //_sendBuffer = new DynamicBufferManager(receiveBufferSize * sizeExtend);

        }

        public void Init()
        {
            ClearReceive();
            ClearSend();

            halfReceiveMessage = new byte[] { };
            isDealReceive = false;
            isDealSend = false;

            sendIndex = 0;
            receiveIndex = 0;
            outOrders = new Dictionary<int, byte[]>();
        }

        public void ClearReceive()
        {
            _receiveBuffer = new Queue<byte>();
        }
        public void ClearSend()
        {
            _sendBuffer = new Queue<byte>();
        }
    }
}