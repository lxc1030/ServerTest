﻿using System;
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
    /// 连接套接字
    /// </summary>
    private Socket _connectSocket;
    public Socket ConnectSocket
    {
        get { return _connectSocket; }
        set { _connectSocket = value; }
    }
    public readonly object LockHanding = new object();
    
    /// <summary>
    /// 用户数据
    /// </summary>
    public RoomActor userInfo { get; set; }

    public AsyncUserToken(int size)
    {
        Init();
        SAEA_Receive = new SocketAsyncEventArgs();
    }

    public void Init()
    {
        userInfo = null;
        _receiveBuffer = new List<byte>();
        ConnectSocket = null;
    }

    public static byte[] GetSendBytes(byte[] buffer)
    {
        int length = buffer.Length;
        byte[] send = new byte[buffer.Length + sizeof(int)];

        byte[] temp = BitConverter.GetBytes(length);

        Array.Copy(temp, 0, send, 0, sizeof(int));
        Array.Copy(buffer, 0, send, sizeof(int), length);

        return send.ToArray();
    }

}
public class MySocketEventArgs : SocketAsyncEventArgs
{
    /// <summary>  
    /// 标识，只是一个编号而已  
    /// </summary>  
    public int ArgsTag { get; set; }
    /// <summary>  
    /// 设置/获取使用状态  
    /// </summary>  
    public bool IsUsing { get; set; }
}