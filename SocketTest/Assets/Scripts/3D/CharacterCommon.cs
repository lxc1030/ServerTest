using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCommon : MonoBehaviour
{
    public GameObject teamBlue;
    public GameObject teamRed;

    public Transform myModel;
    public Transform modelHead;

    /// <summary>
    /// 该模型在房间中的站位
    /// </summary>
    public int myIndex;

    public ActorMoveDirection lastMoveDirection = new ActorMoveDirection();
    public ActorRotateDirection lastRotateDirection = new ActorRotateDirection();


    private CharacterController cc;
    public CharacterController myControl
    {
        get { return cc; }
    }

    public Animation myAnimation;


    public Transform transKillGroup;
    public MeshRenderer[] meshes = null;

    /// <summary>
    /// 枪口射击点
    /// </summary>
    public Transform shootMuzzle;
    /// <summary>
    /// 射击方向指示器
    /// </summary>
    public GameObject shootDirection;




    void Start()
    {
        cc = GetComponentInChildren<CharacterController>();
    }

    public void Init(int index)
    {
        BeStop();
        myIndex = index;
        SetDirectionEnable(false);
        ShowKill(0);
        ShowCharacterControl();

        //FrameManager.ListenDelegate(true, DoMove);
    }
    public void SetDirectionEnable(bool isShow)
    {
        shootDirection.SetActive(isShow);
    }

    public void ShowTeam(TeamType type)
    {
        teamBlue.SetActive(type == TeamType.Blue);
        teamRed.SetActive(type == TeamType.Red);
    }
    public void ShowKill(int num)
    {
        float angle = 0;
        if (num <= 0)
        {

        }
        else if (num <= transKillGroup.childCount)
        {
            angle = 360 / num;
        }
        else
        {
            angle = 360 / transKillGroup.childCount;
        }
        if (meshes.Length != transKillGroup.childCount)
        {
            meshes = transKillGroup.GetComponentsInChildren<MeshRenderer>(true);
        }
        for (int i = 0; i < transKillGroup.childCount; i++)
        {
            if (num > i)
            {
                transKillGroup.GetChild(i).gameObject.SetActive(true);
                transKillGroup.GetChild(i).transform.rotation = Quaternion.Euler(new Vector3(0, i * angle, 0));
                if (num <= transKillGroup.childCount)
                {
                    Material m = DataController.instance.GetMaterialOfKillCount(num);
                    meshes[i].material = m;
                }
                else
                {
                    Material m = DataController.instance.GetMaterialOfKillCount(transKillGroup.childCount);
                    meshes[i].material = m;
                }
            }
            else
            {
                transKillGroup.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
    public void ShowMyName(string name)
    {
        GameObject obj = PoolManager.instance.GetPoolObjByType(PreLoadType.PeopleInfo, GameRunUI.instance.transName);
        obj.transform.position = Vector3.zero;
        PeopleInfo info = obj.GetComponent<PeopleInfo>();
        info.Init(this, name);
    }

    public void ShowBullet()
    {
        bool isTrigger = false;
        if (myIndex == DataController.instance.MyLocateIndex)
        {
            isTrigger = true;
        }
        GameObject obj = null;
        obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Bullet, shootMuzzle);
        obj.transform.localEulerAngles = Vector3.zero;
        obj.transform.parent = GameManager.instance.transBullet;
        //
        BulletGrity b = obj.GetComponent<BulletGrity>();
        b.Init(isTrigger, shootMuzzle.position);
    }
    public void ShowCharacterControl()
    {
        return;
        if (myIndex == DataController.instance.MyLocateIndex)
        {
            myControl.enabled = true;
        }
        else
        {
            myControl.enabled = false;
        }
    }


    public void BeShoot()
    {
        Debug.Log("播放死亡动画，设置到初始位置.");
        GameModelData info = new GameModelData()
        {
            userIndex = myIndex,
            pos = (NetVector3)GameTypeManager.BackStandPos(DataController.instance.MyRoomInfo.RoomType, myIndex),
            rotate = new NetVector3(0, GameTypeManager.BackLookAt(DataController.instance.MyRoomInfo.RoomType, myIndex), 0),
            animation = 0
        };
        SetPosition(SerializeHelper.BackVector(info.pos));
        SetRotate(SerializeHelper.BackVector(info.rotate));
        SetAnimation(info.animation);
    }
    public void BeStop()
    {
        lastMoveDirection = new ActorMoveDirection();
        lastRotateDirection = new ActorRotateDirection();
    }

    public bool IsShowHierarchy()
    {
        return gameObject.activeInHierarchy;
    }


    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }
    public void SetRotate(Vector3 rotate)
    {
        myModel.rotation = Quaternion.Euler(rotate);//单纯的设置旋转方向就行了
    }
    public void SetAnimation(int animation)
    {
        string name = GameManager.instance.animationGroup[animation].name;
        Debug.Log(name);
        //myAnimation.CrossFade("soldierIdleRelaxed");
    }

    /// <summary>
    /// 外部修改移动方向
    /// </summary>
    /// <param name="dir"></param>
    public void SetMoveDirection(ActorMoveDirection dir)
    {
        lastMoveDirection = dir;
    }

    public void SetRotateDirection(ActorRotateDirection dir)
    {
        lastRotateDirection = dir;
        SetRotate(new Vector3(0, lastRotateDirection.rotateY, 0));//单纯的设置旋转方向就行了
    }


    /// <summary>
    /// 这个是每隔0.015s运行一次
    /// </summary>
    public void DoFrameLogic()
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            BeStop();
            return;
        }
        if (myIndex != DataController.instance.MyLocateIndex)
        {
            DoMove();
        }
    }

    private void DoMove()
    {
        CharacterMove(lastMoveDirection.direction, lastMoveDirection.speed, Time.deltaTime);
    }
    private void CharacterMove(NetVector3 direction, float speed, float time)
    {
        Vector3 fixG = SerializeHelper.BackVector(direction) * speed * time;
        fixG = new Vector3(fixG.x, 0, fixG.z);
        fixG.y = -GameManager.gravity;
        //if (myIndex == DataController.instance.MyLocateIndex)
        {
            myControl.Move(fixG);
        }
    }

    #region MyControl


    private ActorMoveDirection lastMove;

    public void UIMove(Vector3 moveDirection, float moveSpeed)
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            return;
        }
        int moveIndex = GameManager.uiMoveIndex;

        float x = (float)Math.Round(moveDirection.x, moveIndex);
        float y = (float)Math.Round(moveDirection.y, moveIndex);
        float z = (float)Math.Round(moveDirection.z, moveIndex);
        float _speed = (float)Math.Round(moveSpeed, GameManager.uiSpeedIndex);

        ActorMoveDirection tempMove = new ActorMoveDirection()
        {
            userIndex = DataController.instance.MyLocateIndex,
            position = new NetVector3(transform.position.x, transform.position.y, transform.position.z),
            direction = new NetVector3(x, y, z),
            speed = _speed
        };

        if (lastMove == null
         || SerializeHelper.BackVector(tempMove.direction) != SerializeHelper.BackVector(lastMove.direction)
         || tempMove.speed != lastMove.speed
         )
        {
            tempMove.runningTime = ServerTimeManager.instance.ServerTime;
            lastMove = tempMove;

            //发送信息
            SendMoveData(lastMove);
        }
        //移动
        CharacterMove(lastMove.direction, lastMove.speed, Time.deltaTime);
    }

    private void SendMoveData(ActorMoveDirection tempMove)
    {
        byte[] sendData = SerializeHelper.Serialize<ActorMoveDirection>(tempMove);
        SocketManager.instance.SendSave((byte)MessageConvention.moveDirection, sendData, false);
    }




    private ActorRotateDirection lastRotate;
    public void UIRotation()
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            return;
        }
        int lookAt = (int)myModel.eulerAngles.y;
        int rotateIndex = GameManager.uiRotateIndex;

        ActorRotateDirection tempRotate = new ActorRotateDirection()
        {
            userIndex = DataController.instance.MyLocateIndex,
            rotateY = lookAt
        };

        byte[] sendData = SerializeHelper.Serialize<ActorRotateDirection>(tempRotate);
        if (lastRotate == null || Mathf.Abs(lookAt - lastRotate.rotateY) >= rotateIndex)
        {
            lastRotate = tempRotate;
            SocketManager.instance.SendSave((byte)MessageConvention.rotateDirection, sendData, false);
        }

    }

    public void UIShot()
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            return;
        }
        Vector3 pos = shootMuzzle.position;

        int moveIndex = GameManager.uiMoveIndex;
        float x = (float)Math.Round(pos.x, moveIndex);
        float y = (float)Math.Round(pos.y, moveIndex);
        float z = (float)Math.Round(pos.z, moveIndex);

        int lookAt = (int)myModel.eulerAngles.y;
        int rotateIndex = GameManager.uiRotateIndex;

        ActorMoveDirection shootDirection = new ActorMoveDirection()
        {
            userIndex = DataController.instance.MyLocateIndex,
            position = new NetVector3(x, y, z),
             direction = new NetVector3 ()
        }
        
        byte[] message = SerializeHelper.ConvertToByte(userIndex + "");
        SocketManager.instance.SendSave((byte)MessageConvention.shootBullet, message, false);
    }


    #endregion
}
