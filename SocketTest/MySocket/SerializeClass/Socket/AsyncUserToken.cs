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
    private List<byte> _receiveBuffer;
    public List<byte> ReceiveBuffer
    {
        get { return _receiveBuffer; }
        set { _receiveBuffer = value; }
    }
    /// <summary>
    /// 发送数据的缓冲区
    /// </summary>
    private List<byte> _sendBuffer;
    public List<byte> SendBuffer
    {
        get { return _sendBuffer; }
        set { _sendBuffer = value; }
    }

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
    public bool isSending { get; set; }


    /// <summary>
    /// 用户数据
    /// </summary>
    public RoomActor userInfo { get; set; }


    public AsyncUserToken(int size)
    {
        byteReceive = new byte[size];
        byteSend = new byte[size];

        SAEA_Receive = new SocketAsyncEventArgs();
        SAEA_Receive.SetBuffer(byteReceive, 0, size);

        SAEA_Send = new SocketAsyncEventArgs();
        SAEA_Send.SetBuffer(byteSend, 0, size);

        _receiveBuffer = new List<byte>();
        _sendBuffer = new List<byte>();

        isSending = false;

        ConnectSocket = null;
    }


}
