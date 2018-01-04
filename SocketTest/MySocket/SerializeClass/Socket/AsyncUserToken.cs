using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;


/// <summary>
/// SAEA用户标记类
/// </summary>
public class AsyncUserToken
{
    public SocketAsyncEventArgs SAEA_Receive;
    public SocketAsyncEventArgs SAEA_Send;

    private byte[] byteReceive { get; set; }
    private byte[] byteSend { get; set; }


    /// <summary>
    /// 接收数据的缓冲区
    /// </summary>
    private Queue<byte> _receiveBuffer;
    public Queue<byte> ReceiveBuffer
    {
        get { return _receiveBuffer; }
        set { _receiveBuffer = value; }
    }
    /// <summary>
    /// 发送数据的缓冲区
    /// </summary>
    private Queue<byte> _sendBuffer;
    public Queue<byte> SendBuffer
    {
        get { return _sendBuffer; }
        set { _sendBuffer = value; }
    }

    public byte[] halfReceiveMessage { get; set; }

    /// <summary>
    /// 连接套接字
    /// </summary>
    private Socket _connectSocket;
    public Socket ConnectSocket
    {
        get { return _connectSocket; }
        set { _connectSocket = value; }
    }

    /// <summary>
    /// 记录发送套接字是否发送结束
    /// </summary>
    public bool isSending;

    public bool isDealReceive;


    /// <summary>
    /// 用户数据
    /// </summary>
    public RoomActor userInfo;
    /// <summary>
    /// 最新一次心跳时间
    /// </summary>
    public DateTime HeartbeatTime;


    public AsyncUserToken(int size)
    {
        byteReceive = new byte[size];
        byteSend = new byte[size];

        SAEA_Receive = new SocketAsyncEventArgs();
        SAEA_Receive.SetBuffer(byteReceive, 0, size);

        SAEA_Send = new SocketAsyncEventArgs();
        SAEA_Send.SetBuffer(byteSend, 0, size);

        _receiveBuffer = new Queue<byte>();
        _sendBuffer = new Queue<byte>();

        halfReceiveMessage = new byte[] { };

        isSending = false;
        isDealReceive = false;

        ConnectSocket = null;

    }

    public void Clear(Queue<byte> buffer)
    {
        buffer.Clear();
    }
    public void Clear(byte[] buffer)
    {
        buffer = new byte[] { };
    }
    public void SendSave(byte[] data)
    {
        lock (SendBuffer)
        {
            //存值
            for (int i = 0; i < data.Length; i++)
            {
                //将buffer保存到队列
                SendBuffer.Enqueue(data[i]);
            }
        }
    }


}

