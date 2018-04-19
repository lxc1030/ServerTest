using System.Collections.Generic;
using UnityEngine;

public class GameTypeManager
{
    //public static Dictionary<int, BoxInfo> BackBoxInfosByType(GameModel type)
    //{
    //    Dictionary<int, BoxInfo> boxes = new Dictionary<int, BoxInfo>();
    //    switch (type)
    //    {
    //        case GameModel.组队模式:
    //            /// <summary>
    //            /// 位置排布方向
    //            /// </summary>
    //            Vector3 Fix = new Vector3(1, 0, 0);
    //            //生成原始点
    //            Vector3 OrgPos = new Vector3(0, 0, 0);
    //            /// <summary>
    //            /// 盒子大小
    //            /// </summary>
    //            Vector3 BoxScale = new Vector3(1, 1, 1);
    //            /// <summary>
    //            /// 每行个数
    //            /// </summary>
    //            int X = 20;
    //            //
    //            for (int i = 0; i < X * X; i++)
    //            {
    //                int yu = i % X;
    //                int chu = i / X;
    //                boxes.Add(i, new BoxInfo()
    //                {
    //                    myIndex = i,
    //                    ownerIndex = -1,
    //                });
    //            }
    //            break;
    //    }
    //    return boxes;
    //}


    public static int PosHeight = 8;
    public static Vector3 BackStandPos(GameModel type, int index)
    {
        Vector3 value = Vector3.zero;

        switch (type)
        {
            case GameModel.组队模式:
                switch (index)
                {
                    case 0:
                        value = new Vector3(-20, PosHeight, -24);
                        break;
                    case 1:
                        value = new Vector3(-20, PosHeight, 24);
                        break;
                    case 2:
                        value = new Vector3(-10, PosHeight, -24);
                        break;
                    case 3:
                        value = new Vector3(-15, PosHeight, 24);
                        break;
                    case 4:
                        value = new Vector3(0, PosHeight, -24);
                        break;
                    case 5:
                        value = new Vector3(0, PosHeight, 24);
                        break;
                    case 6:
                        value = new Vector3(10, PosHeight, -24);
                        break;
                    case 7:
                        value = new Vector3(10, PosHeight, 24);
                        break;
                    case 8:
                        value = new Vector3(20, PosHeight, -24);
                        break;
                    case 9:
                        value = new Vector3(20, PosHeight, 24);
                        break;
                    default:
                        value = new Vector3(index, PosHeight, index);
                        break;
                }
                break;
        }
        return value;
    }
    public static int BackLookAt(GameModel type, int index)
    {
        int value = 0;
        switch (type)
        {
            case GameModel.组队模式:
                value = (index % 2) * 180;
                break;
        }
        return value;
    }
}

public enum GameModel
{
    组队模式,
    Boss模式,
}