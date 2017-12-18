using System;
using System.Collections;
using System.Text;
using System.Data;
using System.Data.Common;

namespace DateBaseConnectionPool
{
    /// <summary>
    /// 连接池状态
    /// </summary>
    public enum PoolState
    {
        /// <summary>
        /// 刚刚创建的对象，表示该对象未被调用过StartSeivice方法。
        /// </summary>
        UnInitialize,
        /// <summary>
        /// 初始化中，该状态下服务正在按照参数初始化连接池。
        /// </summary>
        Initialize,
        /// <summary>
        /// 运行中
        /// </summary>
        Run,
        /// <summary>
        /// 停止状态
        /// </summary>
        Stop
    }

    /// <summary>
    /// 要申请连接的级别
    /// </summary>
    public enum ConnLevel
    {
        /// <summary>
        /// 独占方式，分配全新的连接资源，并且该连接资源在本次使用释放回连接池之前不能在分配出去。如果连接池只能分配引用记数类型连接资源则该级别将产生一个异常，标志连接池资源耗尽
        /// </summary>
        ReadOnly,
        /// <summary>
        /// 优先级-高，分配全新的连接资源，不使用引用记数技术。注：此级别不保证在分配后该连接资源后，仍然保持独立占有资源，若想独立占有资源请使用ReadOnely
        /// </summary>
        High,
        /// <summary>
        /// 优先级-中，适当应用引用记数技术分配连接
        /// </summary>
        None,
        /// <summary>
        /// 优先级-底，尽可能使用引用记数技术分配连接
        /// </summary>
        Bottom
    }

    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnTypeEnum
    {
        /// <summary>
        /// ODBC 数据源
        /// </summary>
        Odbc,
        /// <summary>
        /// OLE DB 数据源
        /// </summary>
        OleDb,
        /// <summary>
        /// SqlServer 数据库连接
        /// </summary>
        SqlClient,
        /// <summary>
        /// 默认（无分配）
        /// </summary>
        None
    }

    /// <summary>
    /// 连接池中的一个连接类型
    /// </summary>
    public class ConnStruct : IDisposable
    {
        /// <summary>
        /// 连接池中的连接
        /// </summary>
        /// <param name="dbc">数据库连接</param>
        /// <param name="cte">连接类型</param>
        public ConnStruct(DbConnection dbc, ConnTypeEnum cte)
        {
            createTime = DateTime.Now;
            connect = dbc;
            connType = cte;
        }
        /// <summary>
        /// 连接池中的连接
        /// </summary>
        /// <param name="dt">连接创建时间</param>
        /// <param name="dbc">数据库连接</param>
        /// <param name="cte">连接类型</param>
        public ConnStruct(DbConnection dbc, ConnTypeEnum cte, DateTime dt)
        {
            createTime = dt;
            connect = dbc;
            connType = cte;
        }
        //--------------------------------------------------------------------
        private bool enable = true;//是否失效
        private bool use = false;//是否正在被使用中
        private bool allot = true;//表示该连接是否可以被分配
        private DateTime createTime = DateTime.Now;//创建时间
        private int useDegree = 0;//被使用次数
        private int repeatNow = 0;//当前连接被重复引用多少
        private bool isRepeat = true;//连接是否可以被重复引用，当被分配出去的连接可能使用事务时，该属性被标识为true
        private ConnTypeEnum connType = ConnTypeEnum.None;//连接类型
        private DbConnection connect = null;//连接对象
        private object obj = null;//连接附带的信息

        #region 属性部分
        /// <summary>
        /// 表示该连接是否可以被分配
        /// </summary>
        public bool Allot
        {
            get { return allot; }
            set { allot = value; }
        }
        /// <summary>
        /// 是否失效；false表示失效，只读
        /// </summary>
        public bool Enable
        { get { return enable; } }
        /// <summary>
        /// 是否正在被使用中，只读
        /// </summary>
        public bool IsUse
        { get { return use; } }
        /// <summary>
        /// 创建时间，只读
        /// </summary>
        public DateTime CreateTime
        { get { return createTime; } }
        /// <summary>
        /// 被使用次数，只读
        /// </summary>
        public int UseDegree
        { get { return useDegree; } }
        /// <summary>
        /// 当前连接被重复引用多少，只读
        /// </summary>
        public int RepeatNow
        { get { return repeatNow; } }
        /// <summary>
        /// 得到数据库连接状态，只读
        /// </summary>
        public ConnectionState State
        { get { return connect.State; } }
        /// <summary>
        /// 得到该连接，只读
        /// </summary>
        public DbConnection Connection
        { get { return connect; } }
        /// <summary>
        /// 连接是否可以被重复引用
        /// </summary>
        public bool IsRepeat
        {
            get { return isRepeat; }
            set { isRepeat = value; }
        }
        /// <summary>
        /// 连接类型，只读
        /// </summary> 
        public ConnTypeEnum ConnType
        { get { return connType; } }
        /// <summary>
        /// 连接附带的信息
        /// </summary>
        public object Obj
        {
            get { return obj; }
            set { obj = value; }
        }
        #endregion
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        public void Open()
        { connect.Open(); }
        /// <summary>
        /// 关闭数据库连接 
        /// </summary>
        public void Close()
        { connect.Close(); }
        /// <summary>
        /// 无条件将连接设置为失效
        /// </summary>
        public void SetConnectionLost()
        { enable = false; allot = false; }
        /// <summary>
        /// 被分配出去，线程安全的
        /// </summary>
        public void Repeat()
        {
            lock (this)
            {
                if (enable == false)//连接可用
                    throw new ResLostnExecption();//连接资源已经失效
                if (allot == false)//是否可以被分配
                    throw new AllotExecption();//连接资源不可以被分配
                if (use == true && isRepeat == false)
                    throw new AllotAndRepeatExecption();//连接资源已经被分配并且不允许重复引用
                repeatNow++;//引用记数+1
                useDegree++;//被使用次数+1
                use = true;//被使用
            }
        }
        /// <summary>
        /// 被释放回来，线程安全的
        /// </summary>
        public void Remove()
        {
            lock (this)
            {
                if (enable == false)//连接可用
                    throw new ResLostnExecption();//连接资源已经失效
                if (repeatNow == 0)
                    throw new RepeatIsZeroExecption();//引用记数已经为0
                repeatNow--;//引用记数-1
                if (repeatNow == 0)
                    use = false;//未使用
                else
                    use = true;//使用中
            }
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            enable = false;
            connect.Close();
            connect = null;
        }
    }
}
