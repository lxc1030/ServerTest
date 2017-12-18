using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace NetFrame.Net
{
    class Program
    {
        //public const string IP = "192.168.0.110";
        //public const int portNo = 500;
        public const string IP = "192.168.1.110";
        public const int portNo = 500;
        static void Main(string[] args)
        {
            LogManager.DeleteFiles();//清除Log文件夹
            SqlManager.Init();
            ServerDataManager.Init();

            new AsyncIOCPServer(IPAddress.Parse(IP), portNo, 10);

            



        }


    }
}
