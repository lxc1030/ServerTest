using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCommon : MonoBehaviour
{
    public GameObject teamBlue;
    public GameObject teamRed;

    public Transform myModel;
    public Transform cameraParent;

    /// <summary>
    /// 该模型在房间中的站位
    /// </summary>
    public int myIndex;


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
        SetOriginal();
    }


    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Rigidbody body = hit.collider.attachedRigidbody;
        bool isSend = DataController.instance.MyLocateIndex == myIndex;
        if (!isSend)
        {
            return;
        }

        GameObject obj = hit.gameObject;
        BulletInfo bulletInfo = new BulletInfo();

        switch (obj.tag)
        {
            case nameof(Tag.Box):
                Box box = obj.GetComponent<Box>();
                if (box.myInfo.ownerIndex == myIndex)//碰到自己的领地
                {
                    return;
                }
                else
                {
                    TeamType type = DataController.instance.ActorList[myIndex].MyTeam;
                    box.ChangeTexture(type);
                }
                bulletInfo.shootTag = ShootTag.Box;
                bulletInfo.shootInfo = box.myInfo.myIndex + "";
                break;
            default:
                bulletInfo = null;
                break;
        }
        if (bulletInfo != null)
        {
            Debug.Log("踩中：" + bulletInfo.shootTag);
            bulletInfo.userIndex = DataController.instance.MyLocateIndex;
            //发送
            byte[] message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
            //SocketManager.instance.SendSave((byte)MessageConvention.bulletInfo, message, false);
            UDPManager.instance.SendSave((byte)MessageConvention.bulletInfo, message);
        }
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
    //public void ShowMyName(string name)
    //{
    //    GameObject obj = PoolManager.instance.GetPoolObjByType(PreLoadType.PeopleInfo, GameRunUI.instance.transName);
    //    obj.transform.position = Vector3.zero;
    //    PeopleInfo info = obj.GetComponent<PeopleInfo>();
    //    info.Init(this, name);
    //}

    public void ShowBullet(ShootInfo info)
    {
        GameObject obj = null;
        obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Bullet, shootMuzzle);
        obj.transform.localEulerAngles = Vector3.zero;
        obj.transform.parent = GameManager.instance.transBullet;
        //
        MagicFireball b = obj.GetComponent<MagicFireball>();
        b.Init(info, myIndex);
    }
    public void SetOriginal()
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
        netMove = new ActorMoveDirection();
        lastMove = new ActorMoveDirection();
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
        transform.rotation = Quaternion.Euler(rotate);//单纯的设置旋转方向就行了
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
    public void SetNetDirection(ActorMoveDirection dir)
    {
        float distance = Vector3.Distance(SerializeHelper.BackVector(dir.position), transform.position);
        //Debug.LogError("相差长度：" + distance);
        netMove = dir;
    }

    public void SetNetDirection(ActorRotateDirection dir)
    {
        if (myIndex == DataController.instance.MyLocateIndex)//自身在收到服务器消息之前已旋转
            return;
        SetRotate(new Vector3(0, dir.rotateY, 0));//单纯的设置旋转方向就行了
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
        DoMove();
    }

    private void DoMove()
    {
        CharacterMove();
    }

    private float moveSpeed = 80;
    private void CharacterMove()
    {
        Vector3 main = transform.forward * netMove.direction.z + transform.right * netMove.direction.x;
        myControl.SimpleMove(main * moveSpeed * netMove.speed * DataController.FrameFixedTime);
    }

    private float rotateSpeed = 100;
    private void CharacterRotate()
    {
        float length = Mathf.Abs(netMove.direction.x);
        float side = netMove.direction.x < 0 ? -1 : 1;
        Vector3 main = Vector3.up;
        myControl.transform.Rotate(main * side * length * rotateSpeed * DataController.FrameFixedTime);
    }

    #region MyControl

    private ActorMoveDirection netMove;//服务器的值，本地用来复现移动
    private ActorMoveDirection lastMove;//记录上一次发送给服务器的移动值
    public void UIMove(Vector3 moveDirection, float moveSpeed)
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            return;
        }
        NetVector3 move = DataController.BackNetLimetByType(moveDirection);
        float _speed = DataController.BackNetLimetByType(moveSpeed);
        NetVector3 pos = DataController.BackNetLimetByType(transform.position);

        ActorMoveDirection tempMove = new ActorMoveDirection()
        {
            userIndex = DataController.instance.MyLocateIndex,
            frameIndex = GameManager.instance.frameIndex,
            position = pos,
            direction = move,
            speed = _speed
        };

        if (
            lastMove == null
            || move.x != lastMove.direction.x
            || move.y != lastMove.direction.y
            || move.z != lastMove.direction.z
            || _speed != lastMove.speed
         )
        {
            lastMove = tempMove;
            byte[] sendData = SerializeHelper.Serialize<ActorMoveDirection>(tempMove);
            //SocketManager.instance.SendSave((byte)MessageConvention.moveDirection, sendData, false);
            UDPManager.instance.SendSave((byte)MessageConvention.moveDirection, sendData);
        }
    }


    private ActorRotateDirection lastRotate;
    private static int eulerLimet = 5;
    public void UIRotation()
    {
        if (DataController.instance.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            return;
        }
        int lookAt = (int)myModel.eulerAngles.y;
        int rotateIndex = eulerLimet;

        ActorRotateDirection tempRotate = new ActorRotateDirection()
        {
            userIndex = DataController.instance.MyLocateIndex,
            rotateY = lookAt
        };

        if (lastRotate == null || Mathf.Abs(lookAt - lastRotate.rotateY) >= rotateIndex)
        {
            lastRotate = tempRotate;
            byte[] sendData = SerializeHelper.Serialize<ActorRotateDirection>(tempRotate);
            //SocketManager.instance.SendSave((byte)MessageConvention.rotateDirection, sendData, false);
            UDPManager.instance.SendSave((byte)MessageConvention.rotateDirection, sendData);
        }
    }

  


    #endregion
}
