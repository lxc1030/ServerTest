using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrefabLoad : MonoBehaviour
{
    public RoomActor myActor;
    public Image imgBG;
    public Slider slider;
    public Text txName;
    

    public void Init(RoomActor actor)
    {
        myActor = actor;
        txName.text = myActor.Register.name;
        slider.value = 0;
    }

    public void UpdateSlider(int value)
    {
        slider.value = value;
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
