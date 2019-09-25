using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRegid : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        Test1();
        if (Input.GetKeyUp(KeyCode.Q))
        {
            Test2();
        }

    }

    public void RaycastTest()
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
    public CharacterController myControl;
    public float ySpeed = 0;
    public float Gra;
    public void Test1()
    {
        if (myControl.isGrounded && ySpeed > 0)
        {//判断人物是否在地面上 
            ySpeed = 0;
        }
        else
        {
            float s = ySpeed * DataController.FrameFixedTime + 0.5f * DataController.Gravity * Mathf.Pow(DataController.FrameFixedTime, 2);
            myControl.Move(new Vector3(0, -s, 0));
            ySpeed = ySpeed + DataController.Gravity * DataController.FrameFixedTime;
        }
    }

    public void Test2()
    {
        ySpeed = -Gra;
    }

}
