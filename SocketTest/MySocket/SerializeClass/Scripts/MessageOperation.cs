using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class MessageOperation
{
    public static int lengthID = 4;
    public static int lengthLength = 4;
    public static int lengthMessage = 1024 - 4 - 4;
    public static byte markStart = 35;
    public byte[] Id;
    public byte[] Length;
    public byte[] Message;
    public MessageOperation()
    {
        Id = new byte[lengthID];
        Length = new byte[lengthLength];
        Message = new byte[lengthMessage];
    }

    public static MessageOperation FromBytes(byte[] buffer)
    {
        int bufferLength = buffer.Length;

        if (bufferLength < lengthID + lengthLength + 1)//1是 markStart
        {
            return null;
        }

        MessageOperation message = new MessageOperation();
        using (MemoryStream memoryStream = new MemoryStream(buffer)) //将字节数组填充至内存流
        {
            BinaryReader binaryReader = new BinaryReader(memoryStream); //以二进制读取器读取该流内容

            int mark = binaryReader.ReadByte();//标志
            message.Id = binaryReader.ReadBytes(lengthID);
            message.Length = binaryReader.ReadBytes(lengthLength);

            int dataLength = message.GetLength();
            //如果【进来的Bytes长度】小于【一个完整的MessageXieYi长度】
            if ((bufferLength - (lengthID + lengthLength)) < dataLength)
            {
                Console.WriteLine("数据接收不齐.");
                return null;
            }

            message.Message = binaryReader.ReadBytes(dataLength);

            binaryReader.Close(); //关闭二进制读取器，是否资源
        }
        return message; //返回消息协议对象
    }

    public static MessageOperation FromBytes(int id, byte[] buffer)
    {
        if (buffer == null)
        {
            return null;
        }

        int bufferLength = buffer.Length;
        int fillLength = bufferLength > lengthMessage ? lengthMessage : bufferLength;

        MessageOperation message = new MessageOperation();

        using (MemoryStream memoryStream = new MemoryStream(buffer)) //将字节数组填充至内存流
        {
            BinaryReader binaryReader = new BinaryReader(memoryStream); //以二进制读取器读取该流内容

            message.Length = BitConverter.GetBytes(fillLength);
            message.Id = BitConverter.GetBytes(id);

            message.Message = binaryReader.ReadBytes(fillLength);

            binaryReader.Close(); //关闭二进制读取器，是否资源
        }
        return message;
    }

    public byte[] ToBytes()
    {
        byte[] _bytes; //自定义字节数组，用以装载消息协议
        using (MemoryStream memoryStream = new MemoryStream()) //创建内存流
        {
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream); //以二进制写入器往这个流里写内容

            binaryWriter.Write(markStart);//标识,  占1个字符
            binaryWriter.Write(Id); //写入编号，占4个字节
            binaryWriter.Write(Length);//写入实际消息长度，占4个字节

            if (lengthMessage > 0)//消息主体
            {
                binaryWriter.Write(Message); //写入实际消息内容
            }
            else
            {
                return null;
            }
            _bytes = memoryStream.ToArray(); //将流内容写入自定义字节数组

            binaryWriter.Close(); //关闭写入器释放资源
        }
        return _bytes; //返回填充好消息协议对象的自定义字节数组
    }

    public int GetId()
    {
        return BitConverter.ToInt32(Id, 0);
    }
    public int GetLength()
    {
        return BitConverter.ToInt32(Length, 0);
    }
}

