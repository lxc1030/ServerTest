using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicFireball : MonoBehaviour
{
    ShootInfo info;
    private int ownIndex;
    private bool isDestroy = true;

    private void Awake()
    {

    }

    public void Init(ShootInfo _info, int index)
    {
        info = _info;
        ownIndex = index;
        DoAnimation();
        //
        isDestroy = false;
    }

    [ContextMenu("射击动画")]
    private void DoAnimation()
    {
        transform.position = SerializeHelper.BackVector(info.position);
        transform.rotation = Quaternion.Euler(SerializeHelper.BackVector(info.direction));
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterCommon checkSelf = other.GetComponent<CharacterCommon>();
        if (checkSelf != null)
        {
            if (checkSelf.myIndex == DataController.instance.MyLocateIndex)//子弹碰撞的是自己，不处理，不停止
            {
                return;
            }
        }
        //自身被射检测以后再判断其他
        if (!isDestroy)
        {
            isDestroy = true;
            bool isSend = (ownIndex == DataController.instance.MyLocateIndex);
            GameObject obj = other.gameObject;
            BulletInfo bulletInfo = new BulletInfo() { userIndex = DataController.instance.MyLocateIndex };
            switch (obj.tag)
            {
                case nameof(Tag.Box):
                    Box box = obj.GetComponent<Box>();
                    bulletInfo.shootTag = ShootTag.Box;
                    bulletInfo.shootInfo = box.myInfo.myIndex + "";
                    if (isSend)//本人射击
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
                        isSend = false;
                    }
                    break;
                default://墙体及障碍物
                    isSend = false;
                    break;
            }
            if (isSend)
            {
                Debug.Log("射中：" + bulletInfo.shootTag);
                GameManager.instance.SendNetInfo(bulletInfo);
            }
            //
            PoolDestory();
        }


    }
    private void PoolDestory()
    {
        PoolManager.instance.SetPoolObjByType(PreLoadType.Bullet, gameObject);
    }


    //public Vector3 pos;
    //public Vector3 dir;
    //public int bulletType;
    //[ContextMenu("射击动画")]
    //void Test()
    //{
    //    ShootInfo shoot = new ShootInfo();
    //    shoot.pos = new NetVector3(pos.x, pos.y, pos.z);
    //    shoot.direction = new NetVector3(dir.x, dir.y, dir.z);
    //    shoot.bulletType = bulletType;
    //    Init(shoot);
    //}




    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isDestroy)
        {
            if (Vector3.Distance(transform.position, Vector3.zero) > 100)
            {
                isDestroy = true;
                PoolDestory();
            }
            else
            {
                transform.Translate(transform.forward * DataController.BackBulletSpeed(info.bulletType) * Time.fixedDeltaTime, Space.World);
            }
        }
    }



}