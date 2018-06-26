using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;

class Program
{
    //public const string IP = "192.168.0.110";
    //public const int portNo = 500;
    //public const string IP = "192.168.1.110";
    public const int portTCP = 500;

    public const string IP = "192.168.1.110";
    public const int portUDP = 12000;
    static void Main(string[] args)
    {
        LogManager.Init();//清除Log文件夹
        //
        SqlManager.Init();
        ServerDataManager.Init();
        new AsyncIOCPServer(IP, portTCP, 30000);
        new UdpServer(IP, portUDP);


        while (true)
        {
            string msg = Console.ReadLine();
            if (msg.Contains("quit"))//quit-1-1
            {
                string[] sp = msg.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                int roomID = int.Parse(sp[1]);
                int unique = int.Parse(sp[2]);

                bool isQuit = ServerDataManager.instance.allRoom.RoomList[roomID].Quit(unique);
                Console.WriteLine("踢房间->" + roomID + " 站位->" + unique + " 结果->" + isQuit);
            }
            if (msg == "test")
            {

            }
        }
    }

    //private static void Test()
    //{
    //    string sql = SqlManager.SelectWhere(TableName.register);
    //    List<Register> re = SqlManager.instance.DataRead(sql, new Func<SqlDataReader, List<Register>>(Register.BackDatas));
    //}



    public static void Log4Debug(string msg)
    {
        LogManager.instance.WriteLog(":" + msg);
    }

}

