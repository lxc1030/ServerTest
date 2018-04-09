using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class BulletGrity : MonoBehaviour
{
    public Vector3 pos;

    private bool isSendTrigger;
    public bool isMove = false;

    public float Power = 10;//这个代表发射时的速度/力度等，可以通过此来模拟不同的力大小
    public float Angle = 0;//发射的角度，这个就不用解释了吧
    public float Gravity = -10;//这个代表重力加速度

    private Vector3 MoveSpeed;//初速度向量
    private Vector3 GritySpeed = Vector3.zero;//重力的速度向量，t时为0
    private float dTime;//已经过去的时间


    public void Init(bool isTrigger, Vector3 orgPos)
    {
        isSendTrigger = isTrigger;
        pos = orgPos;
        DoAnimation();
    }



    [ContextMenu("射击")]
    void DoAnimation()
    {
        transform.position = pos;
        isMove = true;
        //通过一个公式计算出初速度向量
        //角度*力度
        MoveSpeed = Quaternion.Euler(new Vector3(-Angle, 0, 0)) * Vector3.forward * Power;

        GritySpeed = Vector3.zero;
        dTime = 0;
    }

    private void FixedUpdate()
    {
        if (!isMove)
            return;
        //计算物体的重力速度
        //v = at ;
        GritySpeed.y = Gravity * (dTime += Time.fixedDeltaTime);
        //位移模拟轨迹
        transform.Translate(MoveSpeed * Time.fixedDeltaTime);
        transform.Translate(GritySpeed * Time.fixedDeltaTime);//
        if (transform.position.y > DataController.bulletAutoDestory)
        {
            isMove = false;
            PoolDestory();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (isMove)
        {
            isMove = false;
            if (isSendTrigger)
            {
                GameObject obj = other.gameObject;
                BulletInfo bulletInfo = new BulletInfo();
                switch (obj.tag)
                {
                    case nameof(Tag.Box):
                        Box box = obj.GetComponent<Box>();
                        bulletInfo.shootTag = ShootTag.Box;
                        bulletInfo.shootInfo = box.myInfo.myIndex + "";
                        if (isSendTrigger)//本人射击
                        {
                            TeamType type = DataController.instance.ActorList[DataController.instance.MyLocateIndex].MyTeam;
                            box.ChangeTexture(type);
                        }
                        break;
                    case nameof(Tag.Member):
                        CharacterCommon member = obj.GetComponent<CharacterCommon>();
                        bulletInfo.shootTag = ShootTag.Character;
                        bulletInfo.shootInfo = member.myIndex + "";
                        RoomActorState state = DataController.instance.ActorList[member.myIndex].CurState;
                        string tip = "";
                        switch (state)
                        {
                            case RoomActorState.Dead:
                                tip = "该玩家已死亡";
                                break;
                            case RoomActorState.Invincible:
                                tip = "该玩家当前无敌";
                                break;
                        }
                        if (!string.IsNullOrEmpty(tip))
                        {
                            UIManager.instance.ShowAlertTip(tip);
                            isSendTrigger = false;
                        }
                        break;
                    default://墙体及障碍物
                        isSendTrigger = false;
                        break;
                }
                if (GameManager.instance.CurrentPlayType == FramePlayType.游戏未开始)
                    return;
                if (isSendTrigger)
                {
                    Debug.Log("射中：" + bulletInfo.shootTag);
                    bulletInfo.userIndex = DataController.instance.MyLocateIndex;
                    //发送
                    byte[] message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
                    //SocketManager.instance.SendSave((byte)MessageConvention.bulletInfo, message, false);
                    UDPManager.instance.SendSave((byte)MessageConvention.bulletInfo, message);
                }
            }
            PoolDestory();
        }
    }
    private void PoolDestory()
    {
        PoolManager.instance.SetPoolObjByType(PreLoadType.Bullet, gameObject);
    }
}
