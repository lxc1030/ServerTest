using System;

class Program
{
    //public const string IP = "192.168.0.110";
    //public const int portNo = 500;
    public const string IP = "192.168.1.110";
    public const int portNo = 500;
    static void Main(string[] args)
    {
        LogManager.Init();//清除Log文件夹
        SqlManager.Init();
        ServerDataManager.Init();
        new AsyncIOCPServer(IP, portNo, 30000);
        Console.ReadLine();
    }
}

