using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;



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
    public static SqlManager instance;

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

    public static int freeConnectCount = 1;
    public Queue<SqlDataGroup> gFree;
    SqlConnection connect = new SqlConnection(strConnection);

    public void Connect()
    {
        gFree = new Queue<SqlDataGroup>();
        for (int i = 0; i < freeConnectCount; i++)
        {
            gFree.Enqueue(
                new SqlDataGroup()
                {
                    connect = connect,
                    command = new SqlCommand("", connect)
                }
                );
        }
        Log4Debug("数据接口连接数：" + freeConnectCount);
    }




    #region 读取数据库，保存数值

    public SqlDataGroup GetOneGroup()
    {
        SqlDataGroup group = null;
        if (gFree.Count > 0)
        {
            group = gFree.Dequeue();
        }
        else
        {
            Log4Debug("接口数量不够，动态生成新的接口");
            SqlConnection con = new SqlConnection(strConnection);
            group = new SqlDataGroup()
            {
                connect = con,
                command = new SqlCommand("", con)
            };
        }
        return group;
    }

    public SqlDataGroup ReaderFindBySql(string sql)
    {
        SqlDataGroup group = GetOneGroup();
        group.connect.Open();
        group.command.CommandText = sql;
        group.InquireReader();
        return group;
    }
    public int ReaderUpdateBySql(string sql)
    {
        SqlDataGroup group = GetOneGroup();
        group.connect.Open();
        group.command.CommandText = sql;
        int count = group.UpdateReader();
        Log4Debug("更新数据的行数量：" + count);
        Close(group);
        return count;
    }
    
    #endregion




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

    //static void print(ConnectionPool c)
    //{
    //    Console.WriteLine("实际连接(包含失效的)：" + c.RealFormPool.ToString());
    //    Console.WriteLine("实际连接(不包含失效的)：" + c.PotentRealFormPool.ToString());
    //    Console.WriteLine("目前可以提供的连接数：" + c.SpareFormPool.ToString());
    //    Console.WriteLine("空闲的实际连接：" + c.SpareRealFormPool.ToString());
    //    Console.WriteLine("已分配的实际连接：" + c.UseRealFormPool.ToString());
    //    Console.WriteLine("已分配连接数：" + c.UseFormPool.ToString());
    //    Console.WriteLine("已分配只读连接：" + c.ReadOnlyFormPool.ToString());
    //    Console.WriteLine("--------------------------");
    //}
    /// <summary>
    /// 数据库的打印Debug方法
    /// </summary>
    /// <param name="msg"></param>
    void Log4Debug(string msg)
    {
        LogManager.WriteLog(this.GetType().Name + ":" + msg);
    }

    //public SqlDataReader UserAccept(string clientID, string sql, ConnLevel level = ConnLevel.None)
    //{
    //    SqlDataReader reader = null;
    //    try
    //    {
    //        if (c.State == PoolState.Run)
    //        {
    //            try
    //            {
    //                SqlConnection conn = (SqlConnection)c.GetConnectionFormPool(clientID, level);//申请一个用户连接
    //                SqlCommand command = new SqlCommand(sql, conn);
    //                reader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
    //            }
    //            catch (Exception e)
    //            {
    //                LogMessage("Error " + e.Message);
    //            }
    //            finally
    //            {
    //                c.DisposeConnection(clientID);//将该用户的连接关掉
    //            }
    //        }
    //        else if (c.State == PoolState.Initialize)
    //            LogMessage("连接池正在初始化连接！");
    //        else if (c.State == PoolState.Stop)
    //            LogMessage("已经停止服务");
    //    }
    //    catch (Exception ee)
    //    {
    //        LogMessage(ee.Message);
    //    }
    //    return reader;
    //}

    #region 操作数据库相关命令


    public string CreateTable(string tableName, string[] col, string[] colType)
    {
        string sql = "";

        if (col.Length != colType.Length)
        {
            Log4Debug("columns.Length != colType.Length");
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
        Log4Debug(sql);
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
        Log4Debug(sql);
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
        Log4Debug(sql);
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
            Log4Debug("columns.Length != values.Length");
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
        Log4Debug(sql);
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
        sql = "UPDATE " + tableName + " SET ";
        for (int i = 0; i < coluvalues.Length; ++i)
        {
            sql += columnNames[i] + " = " + "'" + coluvalues[i] + "'" + ",";
        }
        sql = sql.Remove(sql.Length - 1);
        sql += " WHERE " + selectkey + operation + "'" + selectvalue + "'";
        Log4Debug(sql);
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
            Log4Debug("col.Length != operation.Length != values.Length");
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
        Log4Debug(sql);
        return sql;
    }

    #endregion






    //public void JJFA()
    //{
    //    List<Register> result;
    //    using (var conn = new SqlConnection(strConnection))
    //    {
    //        conn.Open();
    //        var comm = conn.CreateCommand();
    //        comm.CommandText = "select e.TheName, e.Number from Users as e where ..........";
    //        comm.CommandType = System.Data.CommandType.Text;
    //        result = (from DbDataRecord record in comm.ExecuteReader()
    //                  select new Register
    //                  {
    //                      name = (string)record["TheName"],
    //                      userID = (string)record["Number"]
    //                  };
    //                       ).ToList();
    //    }
    //}




    public void Close(SqlDataGroup group)
    {
        group.Close();
        gFree.Enqueue(group);
    }







}

public class SqlDataGroup
{
    public SqlConnection connect;
    public SqlCommand command;
    private SqlDataReader reader;
    public SqlDataReader GetReader()
    {
        return reader;
    }
    /// <summary>
    /// 更新数据库，返回受影响的行数
    /// </summary>
    /// <returns></returns>
    public int UpdateReader()
    {
        return command.ExecuteNonQuery();
    }
    /// <summary>
    /// 查询数据库，设置reader的值
    /// </summary>
    public void InquireReader()
    {
        reader = command.ExecuteReader();
    }

    public void Close()
    {
        connect.Close();
        command.Dispose();
        reader.Close();
    }
}