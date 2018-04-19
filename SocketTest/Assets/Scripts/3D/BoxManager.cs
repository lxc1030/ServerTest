using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    public static BoxManager instance;

    private Dictionary<int, Box> allBox = new Dictionary<int, Box>();
    public int teamBlue;
    public int teamRed;

    /// <summary>
    /// 盒子大小
    /// </summary>
    public Vector3 BoxScale = new Vector3(1, 1, 1);
    /// <summary>
    /// 位置排布方向
    /// </summary>
    public Vector3 Fix = new Vector3(1, 0, 0);
    /// <summary>
    /// 每行个数
    /// </summary>
    public int FixLength = 10;
    



    #region 注册Socket处理

    /// <summary>
    /// 待处理的Socket信息
    /// </summary>
    private Queue<MessageXieYi> serverEvent = new Queue<MessageXieYi>();
    /// <summary>
    /// 该脚本要处理的socket数据类型----每个脚本自己定义自己要侦听的消息类型，只会接收已定义的协议数据
    /// </summary>
    private List<MessageConvention> messageHandle = new List<MessageConvention>()
    {

    };
    private void Awake()
    {
        instance = this;
        SocketManager.ListenDelegate(true, messageHandle, OperationListenInfo);
    }
    private void OnDisable()
    {
        SocketManager.ListenDelegate(false, messageHandle, OperationListenInfo);
    }
    /// <summary>
    /// 添加Socket管理类分发来的需要处理的数据
    /// </summary>
    /// <param name="xieyi"></param>
    public void OperationListenInfo(MessageXieYi xieyi)
    {
        serverEvent.Enqueue(xieyi);
    }
    #endregion

    #region 处理Socket信息

    public void SetBulletInfo(BulletInfo info)
    {
        //
        int boxIndex = int.Parse(info.shootInfo);
        Box box = allBox[boxIndex];
        //
        box.BeShot();
    }

    public void SetBuffData(BuffInfo info)
    {
        TeamType changeTeam = DataController.instance.ActorList[info.ownerIndex].MyTeam;
        switch (info.type)
        {
            case BuffType.Score:
                if (changeTeam == TeamType.Blue)
                {
                    teamBlue += 1;
                }
                else if (changeTeam == TeamType.Red)
                {
                    teamRed += 1;
                }
                break;
            case BuffType.CanKill://设置人物可以杀人

                break;
        }
    }

    #endregion


    #region 逻辑

    public void Init()
    {
        Common.Clear(transform);
        GameModel model = DataController.instance.MyRoomInfo.RoomType;
        GameObject map = null;
        InitScore();

        switch (model)
        {
            case GameModel.组队模式:
                map = Common.Generate(DataController.prefabPath_Ground + (int)model, transform);//根据游戏模式生成地图
                map.transform.localPosition = new Vector3(-25, 28, -25);
                Box[] getBoxes = map.GetComponentsInChildren<Box>();
                for (int i = 0; i < getBoxes.Length; i++)
                {
                    if (!allBox.ContainsKey(i))
                    {
                        allBox.Add(i, null);
                    }
                    getBoxes[i].Init(new BoxInfo() { myIndex = i, ownerIndex = -1 });
                    allBox[i] = getBoxes[i];
                    //getBoxes[i].Init(allBoxInfo[i]);
                }
                break;
            case GameModel.Boss模式:

                break;
        }
    }

    public void InitScore()
    {
        teamBlue = 0;
        teamRed = 0;
    }
    public float BackScorePercent()
    {
        float percent = 0.5f;
        if (teamBlue != 0 || teamRed != 0)
        {
            percent = (float)teamBlue / (teamBlue + teamRed);
        }
        return percent;
    }



    public Vector3 GenerateLength;



    [ContextMenu("GenerateLength")]
    public void GenerateLength1()
    {
        Transform parent = transform.GetChild(0);

        GameObject prefab = parent.gameObject;
        prefab.transform.localPosition = new Vector3(GenerateLength.x * BoxScale.x, GenerateLength.y * BoxScale.y, GenerateLength.z * BoxScale.z) + new Vector3(2, 2, 2);
        
        for (int i = parent.childCount - 1; i > 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }

        for (int i = 0; i < GenerateLength.x; i++)
        {
            for (int j = 0; j < GenerateLength.y; j++)
            {
                for (int k = 0; k < GenerateLength.z; k++)
                {
                    GameObject gen = Common.Generate(prefab, parent);
                    gen.transform.localPosition = new Vector3(i * BoxScale.x, j * BoxScale.y, k * BoxScale.z);
                    gen.transform.localScale = BoxScale;
                }
            }
        }
    }

    [ContextMenu("FixLength按子节点个数")]
    private void FixLength1()
    {
        Transform parent = transform.GetChild(0);
        for (int i = 0; i < parent.childCount; i++)
        {
            int yu = i % FixLength;
            int chu = i / FixLength;
            Transform trans = parent.GetChild(i);
            trans.position = new Vector3(yu * Fix.x * BoxScale.x, chu * Fix.y * BoxScale.y, chu * Fix.z * BoxScale.z);
            trans.localScale = BoxScale;
        }
    }



    public Box GetBoxInfoByIndex(int index)
    {
        return allBox[index];
    }

    #endregion


    public void Update()
    {
        if (serverEvent.Count > 0)
        {
            MessageXieYi xieyi = serverEvent.Dequeue();
        }
    }





}
