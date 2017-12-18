using UnityEngine;
using System.Collections;

public class LookAtObject : MonoBehaviour
{
    public Transform target;
    public void SetTarget(Transform obj)
    {
        target = obj;
    }
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target);
    }
}
