using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using UnityEngine;
using System.Linq.Expressions;

public class HttpClient : MonoBehaviour
{
    public static HttpClient instance;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    #region constructors
    ///// <summary>
    ///// 构造新的HttpClient实例
    ///// </summary>
    //public HttpClient()
    //    : this(null)
    //{
    //}

    ///// <summary>
    ///// 构造新的HttpClient实例
    ///// </summary>
    ///// <param name="url">要获取的资源的地址</param>
    //public HttpClient(string url)
    //    : this(url, null)
    //{
    //}

    ///// <summary>
    ///// 构造新的HttpClient实例
    ///// </summary>
    ///// <param name="url">要获取的资源的地址</param>
    ///// <param name="context">Cookie及Referer</param>
    //public HttpClient(string url, HttpClientContext context)
    //    : this(url, context, false)
    //{
    //}

    ///// <summary>
    ///// 构造新的HttpClient实例
    ///// </summary>
    ///// <param name="url">要获取的资源的地址</param>
    ///// <param name="context">Cookie及Referer</param>
    ///// <param name="keepContext">是否自动在不同的请求间保留Cookie, Referer</param>
    //public HttpClient(string url, HttpClientContext context, bool keepContext)
    //{
    //    //this.url = url;
    //    //this.context = context;
    //    //this.keepContext = keepContext;
    //    //if (this.context == null)
    //    //    this.context = new HttpClientContext();
    //}
    #endregion


    //const string IP = "192.168.0.110";
    //const string Port = "8888";
    const string IP = "192.168.1.110";
    const string Port = "8888";


    HttpWebRequest myRequest = null;
    Encoding encode = null;
    string url = "";

    /// <summary>
    /// 向服务器发送数据
    /// </summary>
    /// <param name="method"></param>
    /// <param name="json"></param>
    public void SendComm(string json, Action<string> callback = null, RequestMethod sendType = RequestMethod.GET)
    {
        string url = "http://" + IP + ":" + Port + "/WebTestHttp.aspx";
        string data = json;
        string result = "";
        if (sendType == RequestMethod.POST)
        {
            result = HttpPost(url, data);
        }
        else
        {
            result = HttpGet(url, data);
        }
        //
        if (callback != null)
        {
            callback(result);
        }
        else
        {
            ReceivedLogic(result);
        }
    }

    private void ReceivedLogic(string receive)
    {
        Debug.LogError(receive);
    }

    /// <summary>  
    /// GET请求与获取结果  
    /// </summary>  
    public static string HttpGet(string Url, string postDataStr)
    {

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + "?" + postDataStr);
        //request.Proxy = null;
        request.Method = nameof(RequestMethod.GET);
        request.ContentType = "text/html;charset=UTF-8";

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
        string retString = reader.ReadLine();
        reader.Close();
        if (response != null)
        {
            response.Close();
        }
        if (request != null)
        {
            request.Abort();
        }

        return retString;
    }
    /// <summary>  
    /// POST请求与获取结果  
    /// </summary>  
    public static string HttpPost(string Url, string postDataStr)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
        //request.Proxy = null;

        request.Method = nameof(RequestMethod.POST);
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = postDataStr.Length;
        StreamWriter writer = new StreamWriter(request.GetRequestStream(), Encoding.ASCII);
        writer.Write(postDataStr);
        writer.Flush();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string encoding = response.ContentEncoding;
        if (encoding == null || encoding.Length < 1)
        {
            encoding = "UTF-8"; //默认编码  
        }
        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
        string retString = reader.ReadLine();
        reader.Close();

        writer.Close();
        if (response != null)
        {
            response.Close();
        }
        if (request != null)
        {
            request.Abort();
        }
        return retString;
    }







}
public enum RequestMethod
{
    POST,
    GET,
}
public static class TestExtension
{
    public static String nameof<T, TT>(this T obj, Expression<Func<T, TT>> propertyAccessor)
    {
        if (propertyAccessor.Body.NodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = propertyAccessor.Body as MemberExpression;
            if (memberExpression == null)
                return null;
            return memberExpression.Member.Name;
        }
        return null;
    }
}