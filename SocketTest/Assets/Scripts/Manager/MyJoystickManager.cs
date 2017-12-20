using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyJoystickManager : MonoBehaviour
{
    public static MyJoystickManager instance;
    public ControlPartUI uiControl;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        instance = this;
        instance.Init();
    }
    public void Init()
    {
        Close();
    }
    public void Close()
    {
        uiControl.all.SetActive(false);
    }

    public void Open()
    {
        uiControl.all.SetActive(true);
        uiControl.etcMove.gameObject.SetActive(true);
        uiControl.etcRotate.gameObject.SetActive(true);
    }



    #region 遥杆命令

    //public void OnMove()
    //{
    //    float x = ETCInput.GetAxis("Horizontal");
    //    float z = ETCInput.GetAxis("Vertical");
    //    Vector3 moveDirection = new Vector3(x, 0, z);
    //    Debug.Log(moveDirection);
    //    float speed = moveDirection.magnitude;
    //    UIMove(x, 0, z, speed);
    //}

    public void OnMove(Vector2 move)
    {
        Vector3 moveDirection = new Vector3(move.x, 0, move.y);
        float speed = moveDirection.magnitude;
        UIMove(moveDirection.x, 0, moveDirection.z, speed);
    }

    public void OnMoveEnd()
    {
        UIMove(0, 0, 0, 0);
    }

    private void UIMove(float x, float y, float z, float speed)
    {
        Vector3 direction = new Vector3(x, y, z);
        speed = GameManager.myActorMoveSpeed * speed;
        MyController.instance.UIMove(direction, speed);
    }

    public void OnClickLookAt(Vector2 move)
    {
        Vector3 moveDir = new Vector3(move.x, 0, move.y);
        MyController.instance.SetDirectionEnable(true);
        double angleOfLine = Mathf.Atan2(-moveDir.z, moveDir.x) * 180 / Mathf.PI;
        angleOfLine += 90;
        MyController.instance.SetRotate(new Vector3(0, (float)angleOfLine, 0));
    }

    /// <summary>
    /// 射击CD时间
    /// </summary>
    private const float shootCDLimet = 0.5f;
    /// <summary>
    /// 射击时间记录
    /// </summary>
    private float shootPassTime = 0.5f;
    public void OnClickShoot()
    {
        MyController.instance.SetDirectionEnable(false);
        //
        MyController.instance.UIRotation();
        //
        if (shootPassTime >= shootCDLimet)
        {
            shootPassTime = 0;
            MyController.instance.PlayShootAnimation();
            UIShot();
        }
    }

    private void UIShot()
    {
        int userIndex = DataController.instance.MyRoomInfo.MyLocateIndex;
        byte[] message = SerializeHelper.ConvertToByte(userIndex + "");
        SocketManager.instance.SendSave((byte)MessageConvention.shootBullet, message, false);
    }

    #endregion



    public void BeShoot()
    {
        uiControl.all.SetActive(false);
    }
    public void ReLife()
    {
        uiControl.all.SetActive(true);
    }


    public void OffLine()
    {
        Close();
    }




    public void Update()
    {
        if (shootPassTime <= shootCDLimet)
        {
            shootPassTime += Time.deltaTime;
        }
    }
}

[Serializable]
public class ControlPartUI
{
    public GameObject all;
    public ETCJoystick etcMove;
    public GameObject etcRotate;
}
