using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Testing : MonoBehaviour
{


    [ContextMenu("生成对应名称的XML")]
    void GXML()
    {
        //ChargeItem info = new ChargeItem()
        //{
        //    chargeId = 0,
        //    price = 6,
        //    addType = 1,
        //    addNum = 1,
        //    desc = "1",
        //    notice = "1"
        //};
        //List<ChargeItem> shop = new List<ChargeItem>() { info, info };
        //FileUtil.writeConfigToFile("ChargeConfig", shop, false);

        //LevelConfig info = new LevelConfig()
        //{
        //    level = 0,
        //    AllInfo = "",
        //    StoneTypeMin = 0,
        //    StoneTypeMax = 2,
        //    desc = "",
        //    notice = "",
        //};
        //List <LevelConfig> tip = new List<LevelConfig>();
        //tip.Add(info);
        //tip.Add(info);
        //FileUtil.writeConfigToFile("LevelConfig", tip, false);


    }

    //public float length;
    public Vector2 shoot;
    //public float angle;
    [ContextMenu("测试文本动画")]
    void Speed()
    {
        long timesec = 10;
        DateTime utcdt = DateTime.Parse(DateTime.UtcNow.ToString("1970-01-01 00:00:00")).AddSeconds(timesec);
        //转成本地时间  
        DateTime localdt = utcdt.ToLocalTime();
        string timeformat = localdt.ToString("mm:ss");
        Debug.LogError(timeformat);

    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            //GameManager.instance.EffectBallClone();
            //GameManager.instance.EffectPanelShoot();
            CameraManager.instance.Shake();
        }
        //RaycastHit2D hit = Physics2D.Linecast(transform.position, transform.position + new Vector3(1, 1, 1));
        //Debug.LogError(hit.collider.gameObject.name);

    }

    [ContextMenu("真实时间")]
    void AddCoin()
    {
        //byte[] intBuff = new byte[] { 10, 0, 0, 0 };
        //int index = BitConverter.ToInt32(intBuff, 0);
        //Debug.LogError(index);
        float time = Time.fixedDeltaTime;
        Debug.LogError(time);
    }


    public string id;
    public GameObject obj;

    [ContextMenu("测试材质")]
    void TestMait()
    {
        //obj.GetComponent<MeshRenderer>().material = (Material)Resources.Load(DataController.materialPathSkill + DataController.materialNameSkill + id);
        //string info = "8,0,172,0,0,0,0,1,0,0,0,255,255,255,255,1,0,0,0,0,0,0,0,12,2,0,0,0,64,83,101,114,105,97,108,105,122,101,44,32,86,101,114,115,105,111,110,61,49,46,48,46,48,46,48,44,32,67,117,108,116,117,114,101,61,110,101,117,116,114,97,108,44,32,80,117,98,108,105,99,75,101,121,84,111,107,101,110,61,110,117,108,108,5,1,0,0,0,17,65,99,116,111,114,78,101,116,65,110,105,109,97,116,105,111,110,3,0,0,0,9,114,111,111,109,73,110,100,101,120,9,117,115,101,114,73,110,100,101,120,14,97,110,105,109,97,116,105,111,110,73,110,100,101,120,0,0,0,8,8,8,2,0,0,0,0,0,0,0,0,0,0,0,16,0,0,0,11,7,0,19,0,0,0,48,44,48,44,48,44,45,57,50,44,45,51,44,48,44,48,44,48,44,7,0,19,0,0,0,48,44,48,44,48,44,45,57,49,44,45,53,44,48,44,48,44,48,44,7,0,20,0,0,0,48,44,48,44,45,49,44,45,57,49,44,45,57,44,48,44,48,44,48,44,7,0,21,0,0,0,48,44,48,44,45,51,44,45,57,49,44,45,49,51,44,48,44,48,44,48,44,44,48,44,48,44,48,44,7,0,18,0,0,0,48,44,48,44,48,44,45,57,49,44,48,44,48,44,48,44,48,44,7,0,18,0,0,0,48,44,48,44,48,44,45,57,49,44,48,44,48,44,48,44,48,44,7,0,18,0,0,0,4";
        //Debug.LogError(info.Split(',').Length);
        string sMsg = "{'ErrorType':'6'}";
        byte[] bm = SerializeHelper.ConvertToByte(sMsg);

        MessageXieYi xieyi = new MessageXieYi() { MessageContent = bm, MessageContentLength = bm.Length, XieYiFirstFlag = 0, XieYiSecondFlag = 0 };
        string message = SerializeHelper.ConvertToString(xieyi.MessageContent);
        JObject json = JObject.Parse(message);
        if (json != null)
        {
            Debug.LogError("eee");
        }
    }
    public int reducePower;
    [ContextMenu("减少体力值")]
    void SetPower()
    {
        PowerManager.instance.ReducePower(reducePower);
    }
    [ContextMenu("测试Frame")]
    public void TestFunc()
    {

    }


}
