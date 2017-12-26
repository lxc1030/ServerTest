using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyController : CharacterCommon
{
    public static MyController instance;

    /// <summary>
    /// 相机
    /// </summary>
    public Camera firstCamera;



    void Awake()
    {
        instance = this;
    }
    
    public void PlayShootAnimation()
    {
        //此处需要修改
        //string tempAniName = "";
        //if (curAnimation == "soldierSprint")
        //{
        //    tempAniName = "soldierRun";
        //}
        //if (curAnimation == "soldierIdleRelaxed")
        //{
        //    tempAniName = "soldierFiring";
        //}
        //if (!string.IsNullOrEmpty(tempAniName))
        //{
        //    if (curAnimation != tempAniName)
        //    {
        //        Debug.LogError(tempAniName);
        //        curAnimation = tempAniName;
        //        myAnimation.Play(curAnimation);
        //        SendActorAnimation(curAnimation);
        //    }
        //}
        //else
        //{
        //    Debug.LogError("动画状态不允许射击");
        //}
    }

    // Wait end of frame to manage charactercontroller, because gravity is managed by virtual controller
    void LateUpdate()
    {
        //此处需要修改
        //if (myControl.isGrounded && (ETCInput.GetAxis("Vertical") != 0 || ETCInput.GetAxis("Horizontal") != 0))
        //{
        //    string tempAniName = "soldierSprint";
        //    if (curAnimation != tempAniName)
        //    {
        //        curAnimation = tempAniName;
        //        myAnimation.CrossFade(curAnimation);
        //        SendActorAnimation(curAnimation);
        //    }
        //}

        //if (myControl.isGrounded && ETCInput.GetAxis("Vertical") == 0 && ETCInput.GetAxis("Horizontal") == 0)
        //{
        //    string tempAniName = "soldierIdleRelaxed";
        //    if (curAnimation != tempAniName)
        //    {
        //        curAnimation = tempAniName;
        //        myAnimation.CrossFade(curAnimation);
        //        SendActorAnimation(curAnimation);
        //    }
        //}

        //if (!myControl.isGrounded)
        //{
        //    string tempAniName = "soldierFalling";
        //    if (curAnimation != tempAniName)
        //    {
        //        curAnimation = tempAniName;
        //        myAnimation.CrossFade(curAnimation);
        //        SendActorAnimation(curAnimation);
        //    }
        //}
    }


    private ActorMoveDirection lastMove;

    public void UIMove(Vector3 moveDirection, float moveSpeed)
    {
        if (DataController.instance.MyRoomInfo.ActorList[myIndex].CurState != RoomActorState.Gaming)
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
            userIndex = DataController.instance.MyRoomInfo.MyLocateIndex,
            direction = new NetVector3(x, y, z),
            speed = _speed
        };


        if (lastMove == null
            || SerializeHelper.BackVector(tempMove.direction) != SerializeHelper.BackVector(lastMove.direction)
            || tempMove.speed != lastMove.speed
            )
        {
            byte[] sendData = SerializeHelper.Serialize<ActorMoveDirection>(tempMove);
            lastMove = tempMove;
            SocketManager.instance.SendSave((byte)MessageConvention.moveDirection, sendData, false);
        }
    }

    private ActorRotateDirection lastRotate;
    public void UIRotation()
    {
        if (DataController.instance.MyRoomInfo.ActorList[myIndex].CurState != RoomActorState.Gaming)
        {
            return;
        }
        int lookAt = (int)myModel.eulerAngles.y;
        int rotateIndex = GameManager.uiRotateIndex;

        ActorRotateDirection tempRotate = new ActorRotateDirection()
        {
            userIndex = DataController.instance.MyRoomInfo.MyLocateIndex,
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
        if (DataController.instance.MyRoomInfo.ActorList[myIndex].CurState != RoomActorState.Gaming)
        {
            return;
        }
        int userIndex = DataController.instance.MyRoomInfo.MyLocateIndex;
        byte[] message = SerializeHelper.ConvertToByte(userIndex + "");
        SocketManager.instance.SendSave((byte)MessageConvention.shootBullet, message, false);
    }

}

