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

    public void ShowBullet(bool isTrigger)
    {
        GameObject obj = null;
        obj = PoolManager.instance.GetPoolObjByType(PreLoadType.Bullet, shootMuzzle);
        obj.transform.localEulerAngles = Vector3.zero;
        obj.transform.parent = GameManager.instance.transBullet;
        //
        BulletGrity b = obj.GetComponent<BulletGrity>();
        b.Init(isTrigger, shootMuzzle.position);
    }
    public void BeShoot()
    {
        Debug.LogError("播放死亡动画，设置到初始位置.");
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
        Debug.LogError(name);
        //myAnimation.CrossFade("soldierIdleRelaxed");
    }

    /// <summary>
    /// 外部修改移动方向
    /// </summary>
    /// <param name="dir"></param>
    public void SetNetDirection(ActorMoveDirection dir)
    {
        lastMoveDirection = dir;
    }

    public void SetNetDirection(ActorRotateDirection dir)
    {
        lastRotateDirection = dir;
        SetRotate(new Vector3(0, lastRotateDirection.rotateY, 0));//单纯的设置旋转方向就行了
    }


    /// <summary>
    /// 这个是每隔0.015s运行一次
    /// </summary>
    public void DoFrameLogic()
    {
        if (DataController.instance.MyRoomInfo.ActorList[myIndex].CurState == RoomActorState.Dead)
        {
            BeStop();
            return;
        }
        Vector3 fixG = SerializeHelper.BackVector(lastMoveDirection.direction) * lastMoveDirection.speed * Time.fixedDeltaTime;
        fixG = new Vector3(fixG.x, 0, fixG.z);
        if (DataController.instance.MyRoomInfo.ActorList[myIndex].CurState != RoomActorState.Dead)
        {
            fixG.y = -GameManager.gravity;
        }
        myControl.Move(fixG);
    }


}
