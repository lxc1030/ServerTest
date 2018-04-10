using UnityEngine;
using System.Collections;
using DG.Tweening;
public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    


    #region  相机震动相关参数
    // 震动标志位
    private bool isshakeCamera = false;

    // 震动幅度
    public float shakeLevel = 3f;
    // 震动时间
    public float setShakeTime = 0.2f;
    // 震动的FPS
    public float shakeFps = 45f;

    private float fps;
    private float shakeTime = 0.0f;
    private float frameTime = 0.0f;
    private float shakeDelta = 0.005f;
    public Camera SelfCamera;

    private Rect changeRect;
    #endregion

    
    public Vector3 followPosition;
    public Vector3 followRotation;


    private void Awake()
    {
        instance = this;
        instance.Init();
    }
    public void Init()
    {
        //
        SelfCamera = GetComponent<Camera>();
        SetCameraEnable(false);
        //
        changeRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        shakeTime = setShakeTime;
        fps = shakeFps;
        frameTime = 0.03f;
        shakeDelta = 0.005f;
    }
    public void SetCameraEnable(bool enable)
    {
        SelfCamera.enabled = enable;
    }
    public void SetCameraFollow(Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }


    public void Shake()
    {
        isshakeCamera = true;
    }
    public void StopShake()
    {
        changeRect.xMin = 0.0f;
        changeRect.yMin = 0.0f;
        SelfCamera.rect = changeRect;
        isshakeCamera = false;
        shakeTime = setShakeTime;
        fps = shakeFps;
        frameTime = 0.03f;
        shakeDelta = 0.005f;
    }


    public void RayShoot()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // 如果射线与平面碰撞，打印碰撞物体信息  
            Debug.Log("碰撞对象: " + hit.collider.name);
            // 在场景视图中绘制射线  
            Debug.DrawLine(ray.origin, hit.point, Color.red);
        }
    }
    


    void Update()
    {
        if (isshakeCamera)
        {
            if (shakeTime > 0)
            {
                shakeTime -= Time.deltaTime;
                if (shakeTime <= 0)
                {
                    StopShake();
                }
                else
                {
                    frameTime += Time.deltaTime;

                    if (frameTime > 1.0 / fps)
                    {
                        frameTime = 0;
                        changeRect.xMin = shakeDelta * (-1.0f + shakeLevel * Random.value);
                        changeRect.yMin = shakeDelta * (-1.0f + shakeLevel * Random.value);
                        SelfCamera.rect = changeRect;
                    }
                }
            }
        }
    }
}
