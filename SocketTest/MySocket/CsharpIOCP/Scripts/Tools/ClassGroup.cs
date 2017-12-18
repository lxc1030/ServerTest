using System;
using System.Data.SqlClient;
using System.Text;
using Newtonsoft.Json.Linq;

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
    /// DataReader转换为Json   
    /// </summary>   
    /// <param name="dataReader">DataReader对象</param>   
    /// <returns>Json字符串(数组）</returns>   
    public static string ReaderToJson(SqlDataReader dataReader)
    {
        StringBuilder jsonString = new StringBuilder();
        try
        {
            //jsonString.Append("[");
            while (dataReader.Read())
            {
                jsonString.Append("{");
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    Type type = dataReader.GetFieldType(i);
                    string strKey = dataReader.GetName(i);
                    string strValue = dataReader[i].ToString();
                    jsonString.Append("\"" + strKey + "\":");
                    strValue = String.Format(strValue, type);
                    strValue = strValue.Trim();
                    //datetime不能出现为空的情况,所以将其转换成字符串来进行处理。
                    //需要加""的
                    if (type == typeof(string) || type == typeof(DateTime))
                    {
                        if (i <= dataReader.FieldCount - 1)
                        {
                            jsonString.Append("\"" + strValue + "\",");
                        }
                        else
                        {
                            jsonString.Append(strValue);
                        }
                    }
                    //不需要加""的
                    else
                    {
                        if (i <= dataReader.FieldCount - 1)
                        {
                            jsonString.Append("" + strValue + ",");
                        }
                        else
                        {
                            jsonString.Append(strValue);
                        }
                    }
                }

                jsonString.Append("},");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(nameof(ClassGroup)+ "ReaderToJson：" + e.Message);
        }
        dataReader.Close();
        if (jsonString.Length > 3)
        {
            jsonString.Remove(jsonString.Length - 3, 3);
            jsonString.Append("}");
        }
        //jsonString.Append("]");
        return jsonString.ToString();
    }
    /// <summary>
    /// string[] 转换成Josn
    /// </summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string StringToJson(string[] column, string[] value)
    {
        string sMsg = "";
        sMsg = "{";
        for (int i = 0; i < column.Length; i++)
        {
            value[i] = value[i].Replace("'", "");//删除插入sql字符时用到的'字符
            sMsg += "\"" + column[i] + "\":\"" + value[i] + "\",";
        }
        sMsg = sMsg.Substring(0, sMsg.Length - 1);
        sMsg += "}";
        return sMsg;
    }

    public static string AddStringToJson(string[] column, string[] value, string json)
    {
        string sMsg = json;
        sMsg = sMsg.Substring(1, sMsg.Length - 2);

        for (int i = 0; i < column.Length; i++)
        {
            value[i] = value[i].Replace("'", "");//删除插入sql字符时用到的'字符
            sMsg += ",\"" + column[i] + "\":\"" + value[i] + "\"";
        }
        sMsg = "{" + sMsg + "}";
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
    createRoom,
    enterRoom,
    outRoom,
    updateUserInfo,
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
    reConnect,//重连
    heartBeat,
    createRoom,
    joinRoom,
    updateRoom,
    quitRoom,
    getRoommateInfo,
    updateActorAnimation,
    updateActorState,
    prepareLocalModel,
    updateModelInfo,//发送玩家坐标，旋转和动画当前值
    getPreGameData,
    startGaming,
    gamingTime,
    shootBullet,
    bulletInfo,
    endGaming,
    moveDirection,
    rotateDirection,
    frameData,
}

#endregion