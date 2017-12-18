using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ShopInfo : MonoBehaviour
{
    public ChargeItem myItem;

    public Image icon;
    public Text txAddNum;
    public Text txPrice;




    public void Init(ChargeItem item)
    {
        myItem = item;
        string spName = DataController.iconPathSkill + ItemManager.instance.GetItem((ItemID)item.GetAddType()).spriteName;
        icon.sprite = Resources.Load(spName, typeof(Sprite)) as Sprite;
        icon.SetNativeSize();
        txAddNum.text = "" + item.GetAddNum();
        txPrice.text = item.price + "";
    }

    public void OnClickPay()
    {
        AudioManager.instance.Play();
        ShopUI.instance.DoItemPay(myItem.GetAddType(), myItem.GetAddNum());
    }

}
