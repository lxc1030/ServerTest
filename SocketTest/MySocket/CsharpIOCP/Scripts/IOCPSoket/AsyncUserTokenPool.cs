using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetFrame.Net
{
    /// <summary>
    /// AsyncUserToken对象池（固定缓存设计）
    /// </summary>
    public class AsyncUserTokenPool
    {
        Stack<AsyncUserToken> m_pool;//初始化时生成的，可用的所有的异步发接收对象池

        List<AsyncUserToken> h_pool;//当前被申请使用的发接收对象池

        // Initializes the object pool to the specified size
        //
        // The "capacity" parameter is the maximum number of 
        // AsyncUserToken objects the pool can hold
        public AsyncUserTokenPool(int capacity)
        {
            m_pool = new Stack<AsyncUserToken>(capacity);

            h_pool = new List<AsyncUserToken>();
        }

        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the AsyncUserToken instance 
        // to add to the pool
        public void Push(AsyncUserToken item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
            lock (h_pool)
            {
                h_pool.Remove(item);
            }
        }

        // Removes a AsyncUserToken instance from the pool
        // and returns the object removed from the pool
        public AsyncUserToken Pop()
        {
            lock (m_pool)
            {
                AsyncUserToken temp = m_pool.Pop();
                h_pool.Add(temp);
                return temp;
            }
        }

        // The number of AsyncUserToken instances in the pool
        public int Count
        {
            get { return m_pool.Count; }
        }


        public void CheckIsConnected(int iCheckInterval, Action<AsyncUserToken> closeAction)
        {
            lock (h_pool)
            {
                Socket s = null;

                for (int i = h_pool.Count - 1; i >= 0; i--)
                {
                    if (h_pool[i].userInfo.heartbeatTime.AddMilliseconds(iCheckInterval).CompareTo(DateTime.Now) < 0)
                    {
                        s = h_pool[i].ConnectSocket;
                        if (h_pool[i].ConnectSocket != null)
                        {
                            string sClientIP = ((IPEndPoint)h_pool[i].ConnectSocket.RemoteEndPoint).ToString();
                            Console.WriteLine(sClientIP + " the heartbeat timeout ！");
                            //
                            closeAction(h_pool[i]);
                        }
                        else
                        {
                            h_pool.Remove(h_pool[i]);
                        }
                    }
                }
            }
        }

        public AsyncUserToken GetTokenByMemberID(string memberID)
        {
            lock (h_pool)
            {
                AsyncUserToken userToken = null;
                for (int i = 0; i < h_pool.Count; i++)
                {
                    if (h_pool[i].userInfo.MemberID == memberID)
                    {
                        userToken = h_pool[i];
                        break;
                    }
                }
                return userToken;
            }
        }

    }
}
