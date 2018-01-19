using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

public class ClassGroup
{

    public static string ErrorSplit = nameof(ErrorType) + ":";
    public static byte[] ErrorBackByType(ErrorType type)
    {
        return SerializeHelper.ConvertToByte(ErrorSplit + (int)type);
    }
    public static ErrorType CheckIsError(MessageXieYi xieyi)
    {
        string message = SerializeHelper.ConvertToString(xieyi.MessageContent);
        string[] sp = message.Split(new string[] { ErrorSplit }, StringSplitOptions.None);
        if (sp.Length > 1)
        {
            return (ErrorType)int.Parse(sp[1]);
        }
        return ErrorType.none;
    }

    /// <summary>
    /// string[] 转换成Josn
    /// </summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string StringToJosn(string[] column, string[] value)
    {
        string sMsg = "";
        sMsg = "{";
        for (int i = 0; i < column.Length; i++)
        {
            value[i] = value[i].Replace("'", "");//删除插入sql字符时用到的'字符
            sMsg += "'" + column[i] + "':'" + value[i] + "',";
        }
        sMsg = sMsg.Substring(0, sMsg.Length - 1);
        sMsg += "}";
        return sMsg;
    }

    public static JObject StringCSToJObject(string value)
    {
        value = value.Replace("{", "");
        value = value.Replace("}", "");
        value = value.Replace("\"", "'");
        value = "{" + value + "}";
        return JObject.Parse(value);
    }

    /// <summary>
    /// 将数值转换成Http的 GET方式发送的数据
    /// </summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string StringToHttp(string[] column, string[] value)
    {
        string sMsg = "";
        for (int i = 0; i < column.Length; i++)
        {
            value[i] = value[i].Replace("'", "");//删除插入sql字符时用到的'字符
            sMsg += column[i] + "=" + value[i] + "&";
        }
        sMsg = sMsg.Substring(0, sMsg.Length - 1);
        return sMsg;
    }




}





/// <summary>
/// Http消息类型
/// </summary>
public enum Comm
{
    error = -1,
    register = 0,
    login,
}




#region 以下为客户端也需要的变量类型

//SQL表名称
public class TableName
{
    public static string register = "Register";
    public static string room = "Room";
}

//socket 消息类型
public enum MessageConvention
{
    error = -1,
    login,
    getHeartBeatTime,
    reConnectCheck,//检查是否重连
    reConnectIndex,//重连客户端Loading帧编号
    heartBeat,
    updateName,//更新用户昵称
    createRoom,
    joinRoom,
    updateRoom,
    getRoomInfo,
    getRoommateInfo,
    quitRoom,
    updateActorAnimation,
    updateActorState,
    prepareLocalModel,
    updateModelInfo,//发送玩家坐标，旋转和动画当前值
    getPreGameData,
    startGaming,
    shootBullet,
    bulletInfo,
    endGaming,
    moveDirection,
    rotateDirection,
    frameData,
    testConnect,
}
#endregion

