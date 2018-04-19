using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBuff : MonoBehaviour
{
    public BuffInfo myInfo;
    Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(BuffInfo _info)
    {
        myInfo = _info;
        rb.useGravity = true;
        ChangeShow();
        rb.isKinematic = false;
    }


    private void ChangeShow()
    {
        switch (myInfo.type)
        {
            case BuffType.Score:

                break;
            case BuffType.CanKill:

                break;
        }
    }

    void OnCollisionEnter(Collision hit)
    {
        rb.useGravity = false;
        GameObject obj = hit.gameObject;
        Debug.LogError("Buff:" + myInfo.myIndex + "->" + obj.name);

        switch (obj.tag)
        {
            case nameof(Tag.Member):
                CharacterCommon cha = obj.GetComponent<CharacterCommon>();
                BePickUp(cha.myIndex);
                break;
        }
    }

    public void BePickUp(int ownerIndex)
    {
        //
        if (ownerIndex == DataController.instance.MyLocateIndex)
        {
            BulletInfo bulletInfo = new BulletInfo() { userIndex = ownerIndex };
            bulletInfo.shootTag = ShootTag.Buff;
            bulletInfo.shootInfo = myInfo.myIndex + "";
            GameManager.instance.SendNetInfo(bulletInfo);
        }
        //
        PoolDestory();
    }

    private void PoolDestory()
    {
        PoolManager.instance.SetPoolObjByType(PreLoadType.Buff, gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if (isMove)
        //{
        //    transform.Translate(-transform.up * Time.fixedDeltaTime * 5, Space.World);
        //}
        //if (!isGround)
        //{
        //    return;
        //    if (Vector3.Distance(transform.position, Vector3.zero) > 100)
        //    {
        //        isGround = true;
        //        PoolDestory();
        //    }
        //    else
        //    {
        //        transform.Translate(-transform.up * Time.fixedDeltaTime, Space.World);
        //    }
        //}
    }



}