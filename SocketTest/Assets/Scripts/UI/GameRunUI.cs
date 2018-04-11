using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRunUI : MonoBehaviour
{
    public static GameRunUI instance;
    public static string Name = "GameRunUI";


    public ControlPartUI uiControl;

    /// <summary>
    /// 比赛时的进度条
    /// </summary>
    public Slider showScore;
    public ShowNumImage showBlue;
    public ShowNumImage showRed;
    public ShowNumImage showTime;

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
        //
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

    public static void Show()
    {
        UIManager.instance.ShowPanel(Name);
        instance.Init();
    }
    private void Init()
    {
        uiControl.Show(true);
        //
        //SetNameUIPool();
    }
    public void OnClose()
    {
        Close();
    }
    public static void Close()
    {
        UIManager.instance.HidePanel(Name);
    }


    /// <summary>
    /// 游戏中显示游戏时间
    /// </summary>
    private void SetShowTime(float time)
    {
        showTime.Show(SecondToString(time));
    }

    private string SecondToString(float time)
    {
        DateTime utcdt = DateTime.Parse(DateTime.UtcNow.ToString("1970-01-01 00:00:00")).AddSeconds(time);
        //转成本地时间  
        DateTime localdt = utcdt.ToLocalTime();
        string timeformat = localdt.ToString("mm:ss");
        return timeformat;
    }

    private void SetScorePercent()
    {
        showScore.value = BoxManager.instance.BackScorePercent();
        showBlue.Show(BoxManager.instance.teamBlue + "");
        showRed.Show(BoxManager.instance.teamRed + "");
    }
    //private void SetNameUIPool()
    //{
    //    for (int i = transName.childCount - 1; i >= 0; i--)
    //    {
    //        PoolManager.instance.SetPoolObjByType(PreLoadType.PeopleInfo, transName.GetChild(i).gameObject);
    //    }
    //}

    #region 遥杆
    public void Open()
    {
        uiControl.Show(true);
    }
    public void BeShoot()
    {
        uiControl.Show(false);
    }

    #endregion



    #region Move
    public void OnMove(Vector2 move)
    {
        Vector3 moveDirection = new Vector3(move.x, 0, move.y);
        Vector3 thumbPos = uiControl.etcMove.thumb.localPosition;
        float speed = thumbPos.magnitude / uiControl.etcMove.GetRadius();//vector2 move计算出的速度不准确，斜角不为1
        UIMove(moveDirection.x, 0, moveDirection.z, speed);
    }

    public void OnMoveEnd()
    {
        UIMove(0, 0, 0, 0);
    }

    private void UIMove(float x, float y, float z, float speed)
    {
        //
        Vector3 direction = new Vector3(x, y, z);
        speed = GameManager.myActorMoveSpeed * speed;
        GameManager.instance.GetMyControl().UIMove(direction, speed);
    }

    #endregion



    #region Touch

    private float xSpeed = 0.5f;
    private float ySpeed = 0.2f;
    public void OnTouchRotate(Vector2 move)
    {
        NetVector3 memberRotate = DataController.BackNetLimetByType(new Vector3(0, move.x * xSpeed, 0), NetLimetType.保留0位);
        Vector3 memberEnd = SerializeHelper.BackVector(memberRotate) + GameManager.instance.GetMyControl().transform.eulerAngles;
        GameManager.instance.GetMyControl().SetRotate(memberEnd);
        //
        UIRotate();
        //
        NetVector3 cameraRotate = DataController.BackNetLimetByType(new Vector3(-move.y * ySpeed, 0, 0), NetLimetType.保留1位);
        Vector3 angle = new Vector3(GameManager.instance.GetMyControl().cameraParent.localEulerAngles.x, 0, 0);
        if (angle.x > 90)//转换成-30
        {
            angle.x -= 360;
        }
        Vector3 cameraEnd = SerializeHelper.BackVector(cameraRotate) + angle;
        if (Mathf.Abs(cameraEnd.x) <= 20)
        {
            GameManager.instance.GetMyControl().cameraParent.localEulerAngles = cameraEnd;
        }
    }

    private void UIRotate()
    {
        //发送人物旋转
        GameManager.instance.GetMyControl().UIRotation();
    }



    public void OnFightDown()
    {
        InvokeRepeating("ShootSpan", 0, DataController.BackShootSpan(0));
    }
    public void OnFightUp()
    {
        CancelInvoke("ShootSpan");
        //GameManager.instance.UIShot();
    }


    private void ShootSpan()
    {
        GameManager.instance.UIShot();
    }


    #endregion








    public void Update()
    {
        SetShowTime(GameManager.instance.GetGameTime());
        SetScorePercent();
    }

}

[Serializable]
public class ControlPartUI
{
    public ETCJoystick etcMove;
    public ETCTouchPad etcRotate;
    public ETCTouchPad etcShoot;

    public void Show(bool isEnable)
    {
        etcMove.gameObject.SetActive(isEnable);
        etcRotate.gameObject.SetActive(isEnable);
        etcShoot.gameObject.SetActive(isEnable);
    }
}
