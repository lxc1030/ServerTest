using DateBaseConnectionPool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;



//新建一张表
//Program.mySql.CreateTable("Register", new string[] { "deviceID", "userID", "password" }, new string[] { "varchar(20)", "varchar(20)", "varchar(20)" });
//增加一列属性
//Program.mySql.AddColumn("Register", "userID", "varchar(20)", false);
//插入一行新值
//Program.mySql.InsertRow("Register", new string[] { "4", "2", "'na'" });
//插入一行新值
//Program.mySql.InsertRow("Register", new string[] { "userID", "password", "name" }, new string[] { "5", "2", "'na'" });
//更新值
//Program.mySql.UpdateInto("Register", new string[] { "userID", "password" }, new string[] { "2", "33" }, "userID", "2");
//查询
//SqlDataReader sqReader = Program.mySql.SelectWhere("Register", new string[] { "name", "password" }, new string[] { "userID" }, new string[] { "=" }, new string[] { "3" });
//sMsg = SqlManager.ToJson(sqReader);
//查询
//SqlDataReader sqReader = Program.mySql.SelectWhere("Register", new string[] { "userID" }, new string[] { "=" }, new string[] { "3" });
//sMsg = SqlManager.ToJson(sqReader);

class SqlManager
{
    public static SqlManager instance;//链接数据库--

    public static Dictionary<string, DataTable> AllTable = new Dictionary<string, DataTable>()
    {
        {TableName.register ,new DataTable()}
    };
    
    /// <summary>
    /// 可供连接的对象池
    /// </summary>
    static ConnectionPool c = null;

    public static void Init()
    {
        if (instance == null)
        {
            instance = new SqlManager();
        }
    }
    public SqlManager()
    {
        Connect();
    }
    private static string strConnection
    {
        get
        {
            return
              "Server=localhost;" +
              "initial catalog=Test;" +
              "user id=sa;" +
              "password=lrk1030;";
        }
    }

    public void Connect()
    {
        foreach (var item in AllTable)
        {

        }
    }



    //public void Connect()
    //{
    //    c = new ConnectionPool(strConnection, ConnTypeEnum.SqlClient);
    //    c.MinConnection = 5;
    //    c.MaxConnection = 1000;
    //    c.MaxRepeatDegree = 1;
    //    c.KeepRealConnection = 5;
    //    c.SeepConnection = 5;
    //    //开启数据服务
    //    c.StartServices();
    //    print(c);

    //    //c的方法
    //    //aaa = Console.ReadLine();
    //    //if (aaa == "start")
    //    //    c.StartServices();
    //    //else if (aaa == "stop")
    //    //    c.StopServices(true);
    //    //else if (aaa == "getr")
    //    //    conn = (SqlConnection)c.GetConnectionFormPool(new object(), ConnLevel.ReadOnly);
    //    //else if (aaa == "geth")
    //    //    conn = (SqlConnection)c.GetConnectionFormPool(new object(), ConnLevel.High);
    //    //else if (aaa == "getb")
    //    //    conn = (SqlConnection)c.GetConnectionFormPool(new object(), ConnLevel.Bottom);
    //    //else if (aaa == "getn")
    //    //    conn = (SqlConnection)c.GetConnectionFormPool(new object(), ConnLevel.None);

    //}

    static void print(ConnectionPool c)
    {
        Console.WriteLine("实际连接(包含失效的)：" + c.RealFormPool.ToString());
        Console.WriteLine("实际连接(不包含失效的)：" + c.PotentRealFormPool.ToString());
        Console.WriteLine("目前可以提供的连接数：" + c.SpareFormPool.ToString());
        Console.WriteLine("空闲的实际连接：" + c.SpareRealFormPool.ToString());
        Console.WriteLine("已分配的实际连接：" + c.UseRealFormPool.ToString());
        Console.WriteLine("已分配连接数：" + c.UseFormPool.ToString());
        Console.WriteLine("已分配只读连接：" + c.ReadOnlyFormPool.ToString());
        Console.WriteLine("--------------------------");
    }
    /// <summary>
    /// 数据库的打印Debug方法
    /// </summary>
    /// <param name="msg"></param>
    static void LogMessage(string msg)
    {
        LogManager.WriteLog("SqlManager:" + msg);
    }

    public SqlDataReader UserAccept(string clientID, string sql, ConnLevel level = ConnLevel.None)
    {
        SqlDataReader reader = null;
        try
        {
            if (c.State == PoolState.Run)
            {
                try
                {
                    SqlConnection conn = (SqlConnection)c.GetConnectionFormPool(clientID, level);//申请一个用户连接
                    SqlCommand command = new SqlCommand(sql, conn);
                    reader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
                }
                catch (Exception e)
                {
                    LogMessage("Error " + e.Message);
                }
                finally
                {
                    c.DisposeConnection(clientID);//将该用户的连接关掉
                }
            }
            else if (c.State == PoolState.Initialize)
                LogMessage("连接池正在初始化连接！");
            else if (c.State == PoolState.Stop)
                LogMessage("已经停止服务");
        }
        catch (Exception ee)
        {
            LogMessage(ee.Message);
        }
        return reader;
    }




    public string CreateTable(string tableName, string[] col, string[] colType)
    {
        string sql = "";

        if (col.Length != colType.Length)
        {
            LogMessage("columns.Length != colType.Length");
        }
        if (col != null && colType != null)
        {
            sql = "CREATE TABLE " + tableName + " (" + col[0] + " " + colType[0];
            for (int i = 1; i < col.Length; ++i)
            {
                sql += ", " + col[i] + " " + colType[i];
            }
            sql += ")";
        }
        LogMessage(sql);
        return sql;
    }
    /// <summary>
    /// alter   table   表名   add   列名   数据类型
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnName"></param>
    /// <param name="type"></param>
    /// <param name="isNull"></param>
    public string AddColumn(string tableName, string columnName, string type, bool isNull)//插入一些数据
    {
        string sql = "";
        sql = "ALTER TABLE " + tableName + " ADD " + columnName + " " + type;
        if (!isNull)
        {
            sql += " NOT NULL";
        }
        LogMessage(sql);
        return sql;
    }
    /// <summary>
    /// 插入一整行数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="colsvalues"></param>
    public string InsertRow(string tableName, string[] colsvalues)
    {
        string sql = "INSERT INTO " + tableName + " VALUES (" + colsvalues[0];
        for (int i = 1; i < colsvalues.Length; ++i)
        {
            sql += ", " + colsvalues[i];
        }
        sql += ")";
        LogMessage(sql);
        return sql;
    }

    /// <summary>
    /// 插入一行
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="values"></param>
    public string InsertRow(string tableName, string[] columnNames, string[] colsvalues)
    {
        string sql = "";
        if (columnNames.Length != colsvalues.Length)
        {
            LogMessage("columns.Length != values.Length");
        }
        sql = "INSERT INTO " + tableName + "(" + columnNames[0];
        for (int i = 1; i < columnNames.Length; ++i)
        {
            sql += ", " + columnNames[i];
        }
        sql += ") VALUES (" + colsvalues[0];
        for (int i = 1; i < colsvalues.Length; ++i)
        {
            sql += ", " + colsvalues[i];
        }
        sql += ")";
        LogMessage(sql);
        return sql;
    }

    /// <summary>
    /// 更新表中的数据
    /// </summary>
    /// <param name="tableName">表名称</param>
    /// <param name="columnNames">要更新的列名称</param>
    /// <param name="coluvalues">要更新的列值</param>
    /// <param name="selectkey">查找要更新的对象列名</param>
    /// <param name="selectvalue">查找要更新的对象值</param>
    public string UpdateInto(string tableName, string columnNames, string coluvalues, string selectkey, string selectvalue, string operation = "=")
    {
        return UpdateInto(tableName, new string[] { columnNames }, new string[] { coluvalues }, selectkey, selectvalue, operation);
    }
    /// <summary>
    /// 更新表中的数据
    /// </summary>
    /// <param name="tableName">表名称</param>
    /// <param name="columnNames">要更新的列名称集合</param>
    /// <param name="coluvalues">要更新的列值集合</param>
    /// <param name="selectkey">查找要更新的对象列名</param>
    /// <param name="selectvalue">查找要更新的对象值</param>
    public string UpdateInto(string tableName, string[] columnNames, string[] coluvalues, string selectkey, string selectvalue, string operation = "=")
    {
        string sql = "";
        sql = "UPDATE " + tableName + " SET " + columnNames[0] + " = " + coluvalues[0];
        for (int i = 1; i < coluvalues.Length; ++i)
        {
            sql += ", " + columnNames[i] + " = " + coluvalues[i] + "";
        }
        sql += " WHERE " + selectkey + operation + "'" + selectvalue + "'";
        LogMessage(sql);
        return sql;
    }

    public string SelectWhere(string tableName)
    {
        string sql = "";
        sql = "SELECT * FROM" + tableName;
        return sql;
    }


    /// <summary>
    /// 查询所有值
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="selectName">需要对应的数据值名称</param>
    /// <param name="operation">大于或者等于或者小于</param>
    /// <param name="selectValues">对应数据的值</param>
    /// <returns></returns>
    public string SelectWhere(string tableName, string[] selectName, string[] operation, string[] selectValues)
    {
        return SelectWhere(tableName, new string[] { "*" }, selectName, operation, selectValues);
    }

    /// <summary>
    /// 查询某些值
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="items">要得到的所有值的列名称</param>
    /// <param name="selectName">查找的对象列名</param>
    /// <param name="operation">大于或者等于或者小于</param>
    /// <param name="selectValues">查找的对象值</param>
    /// <returns></returns>
    public string SelectWhere(string tableName, string[] items, string[] selectName, string[] operation, string[] selectValues)
    {
        if (selectName.Length != operation.Length || operation.Length != selectValues.Length)
        {
            LogMessage("col.Length != operation.Length != values.Length");
        }
        string sql = "SELECT " + items[0];
        for (int i = 1; i < items.Length; ++i)
        {
            sql += ", " + items[i];
        }
        sql += " FROM " + tableName + " WHERE " + selectName[0] + operation[0] + "'" + selectValues[0] + "' ";
        for (int i = 1; i < selectName.Length; ++i)
        {
            sql += " AND " + selectName[i] + operation[i] + "'" + selectValues[0] + "' ";
        }
        LogMessage(sql);
        return sql;
    }
}

