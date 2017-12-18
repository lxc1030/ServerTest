using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetInfoNeedBack : MonoBehaviour {


    /// <summary>
    /// 待处理的Socket信息
    /// </summary>
    private Queue<MessageXieYi> serverEvent = new Queue<MessageXieYi>();

    /// <summary>
    /// 该脚本要等待返回的socket数据协议
    /// </summary>
    private Queue<MessageConvention> messageHandle = new Queue<MessageConvention>();

    // Use this for initialization
    void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
