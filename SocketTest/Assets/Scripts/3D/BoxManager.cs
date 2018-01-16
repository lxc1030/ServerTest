using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    public static BoxManager instance;

    private Dictionary<int, Box> allBox = new Dictionary<int, Box>();
    private Dictionary<int, BoxInfo> allBoxInfo = new Dictionary<int, BoxInfo>();
    /// <summary>
    /// 位置排布方向
    /// </summary>
    public Vector3 Fix = new Vector3(1, 0, 0);
    /// <summary>
    /// 盒子大小
    /// </summary>
    public Vector3 BoxScale = new Vector3(1, 1, 1);
    /// <summary>
    /// 每行个数
    /// </summary>
    public int X = 10;


    public int teamBlue;
    public int teamRed;



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

    public void UpdateBulletInfo(MessageXieYi xieyi)
    {
        BulletInfo bulletInfo = SerializeHelper.Deserialize<BulletInfo>(xieyi.MessageContent);
        //
        int boxIndex = int.Parse(bulletInfo.shootInfo);
        Box hitBox = allBox[boxIndex];
        if (hitBox.myInfo.ownerIndex >= 0)
        {
            TeamType lastTeam = DataController.instance.ActorList[hitBox.myInfo.ownerIndex].MyTeam;
            if (lastTeam == TeamType.Blue)
            {
                teamBlue -= 1;
            }
            else if (lastTeam == TeamType.Red)
            {
                teamRed -= 1;
            }
        }
        //
        hitBox.ChangeOwner(bulletInfo.userIndex);
        TeamType changeTeam = DataController.instance.ActorList[bulletInfo.userIndex].MyTeam;
        if (changeTeam == TeamType.Blue)
        {
            teamBlue += 1;
        }
        else if (changeTeam == TeamType.Red)
        {
            teamRed += 1;
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
                allBoxInfo = GameTypeManager.BackBoxInfosByType(model);
                map = Common.Generate(DataController.prefabPath_Ground + (int)model, transform);//根据游戏模式生成地图
                Box[] getBoxes = map.GetComponentsInChildren<Box>();
                for (int i = 0; i < getBoxes.Length; i++)
                {
                    if (!allBox.ContainsKey(i))
                    {
                        allBox.Add(i, null);
                    }
                    allBox[i] = getBoxes[i];
                    getBoxes[i].Init(allBoxInfo[i]);
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



    [ContextMenu("设置盒子位置和大小")]
    public void FixBoxesPosition()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int yu = i % X;
            int chu = i / X;
            Transform trans = transform.GetChild(i);
            trans.position = new Vector3(yu * Fix.x * BoxScale.x, chu * Fix.y * BoxScale.y, chu * Fix.z * BoxScale.z);
            trans.localScale = BoxScale;
        }
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
