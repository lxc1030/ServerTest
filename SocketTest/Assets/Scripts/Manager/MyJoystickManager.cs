using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        uiControl.all = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            uiControl.all[i] = transform.GetChild(i).gameObject;
        }
        Close();
    }
    public void Close()
    {
        SetAllEnable(false);
    }

    public void Open()
    {
        SetAllEnable(true);
        uiControl.etcMove.gameObject.SetActive(true);
        uiControl.etcRotate.gameObject.SetActive(true);
    }

    public void SetAllEnable(bool enable)
    {
        for (int i = 0; i < uiControl.all.Length; i++)
        {
            uiControl.all[i].gameObject.SetActive(enable);

        }
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
        Vector3 thumbPos = uiControl.etcMove.thumb.localPosition;
        float speed = thumbPos.magnitude / uiControl.etcMove.GetRadius();//vector2 move计算出的速度不准确，斜角不为1
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
        GameManager.instance.GetMyControl().UIMove(direction, speed);
    }

    public void OnClickLookAt(Vector2 move)
    {
        Vector3 moveDir = new Vector3(move.x, 0, move.y);
        GameManager.instance.GetMyControl().SetDirectionEnable(true);
        double angleOfLine = Mathf.Atan2(-moveDir.z, moveDir.x) * 180 / Mathf.PI;
        angleOfLine += 90;
        GameManager.instance.GetMyControl().SetRotate(new Vector3(0, (float)angleOfLine, 0));
    }

    /// <summary>
    /// 射击CD时间
    /// </summary>
    private const float shootCDLimet = 0.5f;

    public void OnClickShoot()
    {
        if (DataController.instance.ActorList[DataController.instance.MyLocateIndex].CurState == RoomActorState.Dead)
            return;
        GameManager.instance.GetMyControl().SetDirectionEnable(false);
        //
        GameManager.instance.GetMyControl().UIRotation();
        //
        if (!IsInvoking("RemoveShootCD"))
        {
            Invoke("RemoveShootCD", shootCDLimet);
            ShowShootCDAnimation();
            GameManager.instance.GetMyControl().UIShot();
            GameManager.instance.GetMyControl().ShowBullet();
        }
        else
        {
            uiControl.cdTip.gameObject.SetActive(true);
            Color color = Color.red;
            uiControl.cdTip.color = color;
            uiControl.cdTip.DOColor(new Color(color.r, color.g, color.b, 0), 0.2f);
        }
    }

    private void RemoveShootCD()
    {
        //Debug.LogError("");
    }
    private void ShowShootCDAnimation()
    {
        for (int i = 0; i < uiControl.cdShoot.Length; i++)
        {
            uiControl.cdShoot[i].gameObject.SetActive(true);
            uiControl.cdShoot[i].fillAmount = 1;
            uiControl.cdShoot[i].DOFillAmount(0, shootCDLimet);
        }
    }

    #endregion



    public void BeShoot()
    {
        SetAllEnable(false);
    }


    public void OffLine()
    {
        Close();
    }




    public void Update()
    {

    }
}

[Serializable]
public class ControlPartUI
{
    public GameObject[] all;
    public ETCJoystick etcMove;
    public ETCJoystick etcRotate;
    public Image[] cdShoot;
    public Text cdTip;
}
