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
        uiControl.etcMove.cameraLookAt = GameManager.instance.GetMyControl().modelHead;
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
        //
        Vector3 direction = new Vector3(x, y, z);
        speed = GameManager.myActorMoveSpeed * speed;
        GameManager.instance.GetMyControl().UIMove(direction, speed);
    }


    /// <summary>
    /// 射击CD时间
    /// </summary>
    private const float shootCDLimet = 0.4f;
    private float shootTimeMark;
    public bool isAutomatic = false;

    public void OnRotate(Vector2 move)
    {
        Vector3 moveDir = new Vector3(move.x, 0, move.y);
        GameManager.instance.GetMyControl().SetDirectionEnable(true);
        double angleOfLine = Mathf.Atan2(-moveDir.z, moveDir.x) * 180 / Mathf.PI;
        angleOfLine += 90;
        UIRotate((float)angleOfLine);
    }
    public void OnRotateEnd()
    {
        if (isAutomatic)
            return;
        if (DataController.instance.ActorList[DataController.instance.MyLocateIndex].CurState == RoomActorState.Dead)
            return;
        GameManager.instance.GetMyControl().SetDirectionEnable(false);

        //
        if (shootTimeMark <= 0)
        {
            DoShoot();
            shootTimeMark = shootCDLimet;
            ShowShootCDAnimation();
        }
        else
        {
            uiControl.cdTip.gameObject.SetActive(true);
            Color color = Color.red;
            uiControl.cdTip.color = color;
            uiControl.cdTip.DOColor(new Color(color.r, color.g, color.b, 0), 0.2f);//CD时间为0.2秒
        }
    }
    private void UIRotate(float y)
    {
        GameManager.instance.GetMyControl().SetRotate(new Vector3(0, y, 0));
        //发送人物旋转
        GameManager.instance.GetMyControl().UIRotation();
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
    public void ChangeFireType()
    {
        isAutomatic = !isAutomatic;
        if (isAutomatic)
        {
            InvokeRepeating("DoShoot", 0, shootCDLimet);
            for (int i = 0; i < uiControl.cdShoot.Length; i++)
            {
                uiControl.cdShoot[i].gameObject.SetActive(false);
            }
        }
        else
        {
            CancelInvoke("DoShoot");
        }
    }
    /// <summary>
    /// 射击
    /// </summary>
    private void DoShoot()
    {
        if (GameManager.instance.CurrentPlayType == FramePlayType.游戏未开始)
        {
            CancelInvoke("DoShoot");
        }
        //发送射击
        GameManager.instance.GetMyControl().UIShot();
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

    private void Update()
    {
        if (shootTimeMark > 0)
        {
            shootTimeMark -= Time.deltaTime;
        }
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
