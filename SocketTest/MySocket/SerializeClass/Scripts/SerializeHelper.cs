using ICSharpCode.SharpZipLib.GZip;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

public static class SerializeHelper
{
    /// <summary>
    /// 使用UTF8编码将byte数组转成字符串
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string ConvertToString(byte[] data)
    {
        return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    /// <summary>
    /// 使用指定字符编码将byte数组转成字符串
    /// </summary>
    /// <param name="data"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static string ConvertToString(byte[] data, Encoding encoding)
    {
        return encoding.GetString(data, 0, data.Length);
    }

    /// <summary>
    /// 使用UTF8编码将字符串转成byte数组
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] ConvertToByte(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    /// <summary>
    /// 使用指定字符编码将字符串转成byte数组
    /// </summary>
    /// <param name="str"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static byte[] ConvertToByte(string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }

    /// <summary>
    /// 将对象序列化为二进制数据 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte[] SerializeToBinary(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();

        //SurrogateSelector ss = new SurrogateSelector();
        //Vector3SerializationSurrogate v3Surrogate = new Vector3SerializationSurrogate();
        //ss.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), v3Surrogate);

        //bf.SurrogateSelector = ss;

        MemoryStream stream = new MemoryStream();
        bf.Serialize(stream, obj);

        byte[] data = stream.ToArray();
        stream.Close();

        return data;
    }

    /// <summary>
    /// 将对象序列化为XML数据
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte[] SerializeToXml(object obj)
    {
        MemoryStream stream = new MemoryStream();
        XmlSerializer xs = new XmlSerializer(obj.GetType());
        xs.Serialize(stream, obj);

        byte[] data = stream.ToArray();
        stream.Close();

        return data;
    }

    /// <summary>
    /// 将二进制数据反序列化
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static object DeserializeWithBinary(byte[] data)
    {
        BinaryFormatter bf = new BinaryFormatter();

        MemoryStream stream = new MemoryStream();

        stream.Write(data, 0, data.Length);
        stream.Position = 0;
        object obj = bf.Deserialize(stream);

        stream.Close();

        return obj;
    }

    /// <summary>
    /// 将二进制数据反序列化为指定类型对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T DeserializeWithBinary<T>(byte[] data)
    {
        return (T)DeserializeWithBinary(data);
    }

    /// <summary>
    /// 将XML数据反序列化为指定类型对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T DeserializeWithXml<T>(byte[] data)
    {
        MemoryStream stream = new MemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Position = 0;
        XmlSerializer xs = new XmlSerializer(typeof(T));
        object obj = xs.Deserialize(stream);

        stream.Close();

        return (T)obj;
    }
    /// <summary>
    /// 将byte转换成DataTime时间
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static DateTime BytesToDateTime(byte[] bytes)
    {
        if (bytes != null)
        {
            long ticks = BitConverter.ToInt64(bytes, 0);
            if (ticks < DateTime.MaxValue.Ticks && ticks > DateTime.MinValue.Ticks)
            {
                DateTime dt = new DateTime(ticks);
                return dt;
            }
        }
        return new DateTime();
    }
    /// <summary>
    /// 将DataTime转byte
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static byte[] DateTimeToBytes(DateTime dt)
    {
        return BitConverter.GetBytes(dt.Ticks);
    }


    /// <summary>
    /// 压缩字节数组
    /// </summary>
    /// <param name="str"></param>
    public static byte[] ByteCompress(byte[] inputBytes)
    {
        return GZip.ByteCompress(inputBytes);
    }

    /// <summary>
    /// 解压缩字节数组
    /// </summary>
    /// <param name="str"></param>
    public static byte[] ByteDecompress(byte[] inputBytes)
    {
        return GZip.ByteDecompress(inputBytes);
    }


    public static string ListCompose(System.Collections.Generic.List<string> list, bool isClearEmpty = true)
    {
        string compose = "";
        for (int i = 0; i < list.Count; i++)
        {
            if (isClearEmpty && string.IsNullOrEmpty(list[i]))
            {
                continue;
            }
            compose += list[i] + ";";
        }
        return compose;
    }


    /// <summary>
    /// 分割字符串
    /// </summary>
    /// <param name="info"></param>
    /// <param name="isClearEmpty"></param>
    /// <returns></returns>
    public static string[] ListSplit(string info, bool isClearEmpty = true)
    {
        string[] split = new string[] { };
        if (isClearEmpty)
        {
            split = info.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            split = info.Split(';');
        }
        return split;
    }

    //public static string VectorToString(Vector3 Pos)
    //{
    //    string info =
    //       (int)(Pos.x * 100) + "," +
    //       (int)(Pos.y * 100) + "," +
    //       (int)(Pos.z * 100) + "," +
    //       "";
    //    return info;
    //}



    // 将消息序列化为二进制的方法  
    // < param name="model">要序列化的对象< /param>  
    public static byte[] Serialize<T>(T model)
    {
        try
        {
            //涉及格式转换，需要用到流，将二进制序列化到流中  
            using (MemoryStream ms = new MemoryStream())
            {
                //使用ProtoBuf工具的序列化方法  
                ProtoBuf.Serializer.Serialize<T>(ms, model);
                //定义二级制数组，保存序列化后的结果  
                byte[] result = new byte[ms.Length];
                //将流的位置设为0，起始点  
                ms.Position = 0;
                //将流中的内容读取到二进制数组中  
                ms.Read(result, 0, result.Length);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("序列化失败: " + ex.ToString());
            return null;
        }
    }

    // 将收到的消息反序列化成对象  
    // < returns>The serialize.< /returns>  
    // < param name="msg">收到的消息.</param>  
    public static T Deserialize<T>(byte[] msg)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream())
            {
                //将消息写入流中  
                ms.Write(msg, 0, msg.Length);
                //将流的位置归0  
                ms.Position = 0;
                //使用工具反序列化对象  
                T result = ProtoBuf.Serializer.Deserialize<T>(ms);
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("反序列化失败: " + ex.ToString());
            return (T)new object();
        }
    }


    public static Vector3 BackVector(NetVector3 vec)
    {
        return new Vector3(vec.x, vec.y, vec.z);
    }

    public static bool EqualsByte(byte[] a, byte[] b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        else if (a != null && b != null)
        {
            if (a.Length == b.Length)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }
}

