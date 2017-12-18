using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public bool isMove;
    public float moveSpeed;
    public Vector3 orgPos;
    public Vector3 endPos;

    public void Init(bool isTrigger, Vector3 _orgPos, Vector3 _endPos)
    {
        orgPos = _orgPos;
        endPos = _endPos;
        //if (isTrigger)
        //{
        //    BulletInfo bullet = new BulletInfo()
        //    {
        //        userIndex = DataController.instance.myRoomInfo.MyLocateIndex,
        //        pos = orgPos
        //    };
        //    string info = bullet.GetSendInfo();
        //    byte[] message = SerializeHelper.ConvertToByte(info);
        //    SocketManager.instance.SendSave((byte)MessageConvention.shootBullet, message);
        //}
        DoAnimation();
    }
 

    [ContextMenu("射击动画")]
    private void DoAnimation()
    {
        isMove = true;
        transform.position = orgPos;
        transform.LookAt(endPos);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (isMove)
        {
            isMove = false;
            GameObject obj = other.gameObject;
            Debug.LogError(obj.name);
            PoolManager.instance.SetPoolObjByType(PreLoadType.Bullet, gameObject);
        }
    }



    private void FixedUpdate()
    {
        if (!isMove)
            return;
        transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime);
    }
}
