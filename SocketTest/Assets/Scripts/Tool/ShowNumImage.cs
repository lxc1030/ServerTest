using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowNumImage : MonoBehaviour
{
    public Transform parent;
    public Image prefab;
    public ImageAligned myAligned;
    public Vector2 imageSize;
    private string lastInfo;

    public enum ImageAligned
    {
        左对齐 = 0,
        中对齐 = 1,
        右对齐 = 2,
        默认UI
    }
    // Use this for initialization
    void Start()
    {
        prefab.gameObject.SetActive(false);
    }


    public void Show(string info, ImageAligned aligned = ImageAligned.默认UI)
    {
        if (imageSize == new Vector2(0, 0))
        {
            Debug.LogError("未设置数字图片大小");
        }
        if (lastInfo == info)//显示数字没变
        {
            return;
        }
        else
        {
            lastInfo = info;
        }

        if (aligned != ImageAligned.默认UI)
        {
            myAligned = aligned;
        }
        Common.Clear(parent);
        Image obj = null;
        
        for (int i = 0; i < info.Length; i++)
        {
            obj = Common.Generate(prefab.gameObject, parent).GetComponent<Image>();
            string c = "";
            if (info[i] == '.')
            {
                c = "dian";
            }
            else if (info[i] == '/')
            {
                c = "gang";
            }
            else if (info[i] == ':')
            {
                c = "maohao";
            }
            else
            {
                c = info[i] + "";
            }
            obj.sprite = Resources.Load(DataController.iconPathSkill + c, typeof(Sprite)) as Sprite;
            obj.rectTransform.sizeDelta = imageSize;
            obj.gameObject.SetActive(true);
        }

        switch (myAligned)
        {
            case ImageAligned.左对齐:
                for (int i = 0; i < parent.childCount; i++)
                {
                    parent.GetChild(i).localPosition = new Vector3(imageSize.x / 2 + i * imageSize.x, 0, 0);
                }
                break;
            case ImageAligned.中对齐:
                for (int i = 0; i < parent.childCount; i++)
                {
                    parent.GetChild(i).localPosition = new Vector3(-((float)(parent.childCount - 1) / 2) * imageSize.x + i * imageSize.x, 0, 0);
                }
                break;

        }
    }
}
