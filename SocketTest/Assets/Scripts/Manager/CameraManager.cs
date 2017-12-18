using UnityEngine;
using System.Collections;
using DG.Tweening;
public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public Vector3 position = new Vector3(0, 80, -55);
    private Vector3 rotation = new Vector3(90, 0, 0);


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
    private Camera selfCamera;

    private Rect changeRect;
    #endregion

    public Light dLight;


    private void Awake()
    {
        instance = this;
        selfCamera = GetComponent<Camera>();
        changeRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
    }
    private void Start()
    {
        shakeTime = setShakeTime;
        fps = shakeFps;
        frameTime = 0.03f;
        shakeDelta = 0.005f;
        if (Application.isEditor)
        {
            dLight.intensity = 0.8f;
        }
    }

    private void OnEnable()
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(rotation);
    }


    public void Shake()
    {
        isshakeCamera = true;
    }
    public void StopShake()
    {
        changeRect.xMin = 0.0f;
        changeRect.yMin = 0.0f;
        selfCamera.rect = changeRect;
        isshakeCamera = false;
        shakeTime = setShakeTime;
        fps = shakeFps;
        frameTime = 0.03f;
        shakeDelta = 0.005f;
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
                        selfCamera.rect = changeRect;
                    }
                }
            }
        }
    }
}
