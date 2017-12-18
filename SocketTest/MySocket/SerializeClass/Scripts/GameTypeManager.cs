using System.Collections.Generic;
using UnityEngine;

public class GameTypeManager
{

    public static int BackRoomLimet(GameModel type)
    {
        int limet = 1;
        switch (type)
        {
            case GameModel.组队模式:
                limet = 2;
                break;

        }
        return limet;
    }
    public static Dictionary<int, BoxInfo> BackBoxInfosByType(GameModel type)
    {
        Dictionary<int, BoxInfo> boxes = new Dictionary<int, BoxInfo>();
        switch (type)
        {
            case GameModel.组队模式:
                /// <summary>
                /// 位置排布方向
                /// </summary>
                Vector3 Fix = new Vector3(1, 0, 0);
                //生成原始点
                Vector3 OrgPos = new Vector3(0, 0, 0);
                /// <summary>
                /// 盒子大小
                /// </summary>
                Vector3 BoxScale = new Vector3(1, 1, 1);
                /// <summary>
                /// 每行个数
                /// </summary>
                int X = 20;
                //
                for (int i = 0; i < X * X; i++)
                {
                    int yu = i % X;
                    int chu = i / X;
                    boxes.Add(i, new BoxInfo()
                    {
                        myIndex = i,
                        ownerIndex = -1,
                        position = new Vector3(yu * Fix.x * BoxScale.x, chu * Fix.y * BoxScale.y, chu * Fix.z * BoxScale.z + chu * BoxScale.z) + OrgPos,
                        scale = BoxScale
                    });
                }
                break;
        }
        return boxes;
    }


    public static int PosHeight = 2;
    public static Vector3 BackStandPos(GameModel type, int index)
    {
        Vector3 value = Vector3.zero;

        switch (type)
        {
            case GameModel.组队模式:
                switch (index)
                {
                    case 0:
                        value = new Vector3(2, PosHeight, 2);
                        break;
                    case 1:
                        value = new Vector3(18, PosHeight, 2);
                        break;
                    case 2:
                        value = new Vector3(18, PosHeight, 18);
                        break;
                    case 3:
                        value = new Vector3(2, PosHeight, 18);
                        break;
                    default:
                        value = new Vector3(5, PosHeight, 5);
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
                switch (index)
                {
                    case 0:
                        value = 45;
                        break;
                    case 1:
                        value = -45;
                        break;
                    case 2:
                        value = 135;
                        break;
                    case 3:
                        value = 225;
                        break;
                    default:
                        value = 45;
                        break;
                }
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