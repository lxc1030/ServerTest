using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using UnityEngine;

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
        Init();

        byteReceive = new byte[size];
        SAEA_Receive = new SocketAsyncEventArgs();
        SAEA_Receive.SetBuffer(byteReceive, 0, size);

        byteSend = new byte[size];
        SAEA_Send = new SocketAsyncEventArgs();
        SAEA_Send.SetBuffer(byteSend, 0, size);
    }

    public void Init()
    {
        userInfo = null;

        _receiveBuffer = new List<byte>();
        _sendBuffer = new List<byte>();

        isSending = false;

        ConnectSocket = null;
    }
    
    public static int lengthLength = 4;


    public byte[] GetSendBytes()
    {
        List<byte> send = null;
        lock (SendBuffer)
        {
            send = new List<byte>();

            int length = SendBuffer.Count;
            byte[] lengthB = BitConverter.GetBytes(length);
            send.AddRange(lengthB);

            byte[] body = SendBuffer.ToArray();
            send.AddRange(body);
            //
            SendBuffer.Clear();
        }
        return send.ToArray();
    }

    //public bool SplitReceiveByte(byte[] receive)
    //{
    //    string error = "";

    //    if (receive.Length < AsyncUserToken.lengthHead + AsyncUserToken.lengthLength)
    //    {
    //        error = "接收长度不对：" + receive.Length;
    //        Console.WriteLine(error);
    //        Debug.LogError(error);
    //        return false;
    //    }

    //    while (receive.Length > 0)
    //    {
    //        byte[] head = new byte[lengthHead];
    //        for (int i = 0; i < lengthHead; i++)
    //        {
    //            head[i] = receive[i];
    //        }
    //        int index = BitConverter.ToInt32(head, 0);
    //        if (index == receiveIndex)
    //        {
    //            receiveIndex++;
    //            //
    //            byte[] length = new byte[lengthLength];
    //            for (int i = 0; i < lengthLength; i++)
    //            {
    //                length[i] = receive[lengthHead + i];
    //            }
    //            int leng = BitConverter.ToInt32(length, 0);
    //            receive = receive.Skip(lengthHead + lengthLength).ToArray();
    //            //
    //            lock (ReceiveBuffer)
    //            {
    //                byte[] buffer = receive.Take(leng).ToArray();
    //                ReceiveBuffer.AddRange(buffer);
    //            }
    //            receive = receive.Skip(leng).ToArray();
    //        }
    //        else
    //        {
    //            error = "接收序号不对:" + index + "/" + receiveIndex;
    //            Console.WriteLine(error);
    //            Debug.LogError(error);
    //            return false;
    //        }
    //    }
    //    return true;
    //}

}
