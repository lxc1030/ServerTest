using System;
using System.Collections.Generic;

public class AsyncUserTokenPool
{
    Stack<AsyncUserToken> pool;//初始化时生成的，可用的所有的异步发接收对象池
    List<AsyncUserToken> used;

    // Initializes the object pool to the specified size
    //
    // The "capacity" parameter is the maximum number of 
    // AsyncUserToken objects the pool can hold
    public AsyncUserTokenPool(int capacity)
    {
        pool = new Stack<AsyncUserToken>(capacity);
        used = new List<AsyncUserToken>();
    }

    // Add a SocketAsyncEventArg instance to the pool
    //
    //The "item" parameter is the AsyncUserToken instance 
    // to add to the pool
    public bool Push(AsyncUserToken item)
    {
        if (item == null)
        {
            //throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            return false;
        }
        lock (pool)
        {
            pool.Push(item);
        }
        return true;
    }
    // Removes a AsyncUserToken instance from the pool
    // and returns the object removed from the pool
    public AsyncUserToken Pop()
    {
        AsyncUserToken temp = null;
        lock (pool)
        {
            if (pool.Count > 0)
            {
                temp = pool.Pop();
            }
        }
        return temp;
    }
    public void RemoveUsed(AsyncUserToken item)
    {
        lock (used)
        {
            used.Remove(item);
        }
    }
    public void AddUsed(AsyncUserToken userToken)
    {
        lock (used)
        {
            used.Add(userToken);
        }
    }


    public int Count()
    {
        return pool.Count;
    }

    public void CheckIsConnected(int iCheckInterval, Action<AsyncUserToken> closeAction)
    {
        AsyncUserToken[] temp = null;
        lock (used)
        {
            temp = used.ToArray();
        }
        for (int i = 0; i < temp.Length; i++)
        {
            if (temp[i].userInfo.heartbeatTime.AddMilliseconds(iCheckInterval).CompareTo(DateTime.Now) < 0)
            {
                closeAction(temp[i]);
            }
        }
    }

    public AsyncUserToken GetTokenByMemberID(string memberID)
    {
        AsyncUserToken[] temp = null;
        lock (used)
        {
            temp = used.ToArray();
        }
        AsyncUserToken userToken = null;
        for (int i = 0; i < temp.Length; i++)
        {
            if (temp[i].userInfo.Register == null)
            {
                continue;
            }
            if (temp[i].userInfo.Register.userID == memberID)
            {
                userToken = temp[i];
                break;
            }
        }
        return userToken;
    }
    public void Log4Debug(string msg)
    {
        LogManager.instance.WriteLog(this.GetType().Name + ":" + msg);
    }


}
