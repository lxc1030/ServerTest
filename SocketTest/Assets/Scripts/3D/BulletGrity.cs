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
        if (isTrigger)
        {
        }
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
    }

    public void OnTriggerEnter(Collider other)
    {
        if (isMove)
        {
            isMove = false;
            GameObject obj = other.gameObject;

            BulletInfo bulletInfo = new BulletInfo();

            if (obj.tag == nameof(Tag.Box))
            {
                Box box = obj.GetComponent<Box>();
                bulletInfo.shootTag = ShootTag.Box;
                bulletInfo.shootInfo = box.myInfo.myIndex + "";
            }
            else if (obj.tag == nameof(Tag.Member))
            {
                CharacterCommon member = obj.GetComponent<CharacterCommon>();
                bulletInfo.shootTag = ShootTag.Character;
                bulletInfo.shootInfo = member.myIndex + "";
            }
            else
            {
                bulletInfo.shootTag = ShootTag.Wall;
                bulletInfo.shootInfo = obj.name + "";
            }
            if (isSendTrigger)
            {
                Debug.Log("射中：" + bulletInfo.shootTag);
                bulletInfo.userIndex = DataController.instance.MyRoomInfo.MyLocateIndex;
                //发送
                byte[] message = SerializeHelper.Serialize<BulletInfo>(bulletInfo);
                SocketManager.instance.SendSave((byte)MessageConvention.bulletInfo, message, false);
            }
            PoolManager.instance.SetPoolObjByType(PreLoadType.Bullet, gameObject);
        }
    }

}
