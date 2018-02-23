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
        //ModelCollision.Init();
        new AsyncIOCPServer(IP, portNo, 30000);
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
        }
    }
}

