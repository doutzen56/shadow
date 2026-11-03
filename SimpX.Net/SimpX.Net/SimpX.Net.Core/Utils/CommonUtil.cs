using IPTools.Core;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SimpX.Net.Core;

/// <summary>
/// 通用工具类：哈希、百分比、站点根地址、XML 序列化、IP 归属地、设备信息。不依赖任何业务数据。
/// </summary>
public static class CommonUtil
{
    /// <summary>
    /// 根据字符串获取固定整型哈希值
    /// </summary>
    /// <param name="str"></param>
    /// <param name="startNumber"></param>
    /// <returns></returns>
    public static long GetFixedHashCode(string str, long startNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0;
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;
            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1) break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }
            return startNumber + Math.Abs(hash1 + (hash2 * 1566083941));
        }
    }

    /// <summary>
    /// 生成百分数
    /// </summary>
    /// <param name="passCount"></param>
    /// <param name="allCount"></param>
    /// <returns></returns>
    public static string ExecPercent(decimal passCount, decimal allCount)
    {
        string res = "";
        if (allCount > 0)
        {
            var value = (double)Math.Round(passCount / allCount * 100, 1);
            if (value < 0)
                res = Math.Round(value + 5 / Math.Pow(10, 0 + 1), 0, MidpointRounding.AwayFromZero).ToString();
            else
                res = Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString();
        }
        if (res == "") res = "0";
        return res + "%";
    }

    /// <summary>
    /// 获取服务地址
    /// </summary>
    /// <returns></returns>
    public static string GetLocalhost()
    {
        string result = $"{App.HttpContext.Request.Scheme}://{App.HttpContext.Request.Host.Value}";

        // 代理模式：获取真正的本机地址
        // X-Original-Host=原始请求
        // X-Forwarded-Server=从哪里转发过来
        if (App.HttpContext.Request.Headers.ContainsKey("Origin")) // 配置成完整的路径如（结尾不要带"/"）,比如 https://www.abc.com
            result = $"{App.HttpContext.Request.Headers["Origin"]}";
        else if (App.HttpContext.Request.Headers.ContainsKey("X-Original")) // 配置成完整的路径如（结尾不要带"/"）,比如 https://www.abc.com
            result = $"{App.HttpContext.Request.Headers["X-Original"]}";
        else if (App.HttpContext.Request.Headers.ContainsKey("X-Original-Host"))
            result = $"{App.HttpContext.Request.Scheme}://{App.HttpContext.Request.Headers["X-Original-Host"]}";
        return result + (string.IsNullOrWhiteSpace(App.Settings.VirtualPath) ? "" : App.Settings.VirtualPath);
    }

    /// <summary>
    /// 对象序列化XML
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string SerializeObjectToXml<T>(T obj)
    {
        if (obj == null) return string.Empty;

        var xs = new XmlSerializer(obj.GetType());
        var stream = new MemoryStream();
        var setting = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false), // 不包含BOM
            Indent = true // 设置格式化缩进
        };
        using (var writer = XmlWriter.Create(stream, setting))
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", ""); // 去除默认命名空间
            xs.Serialize(writer, obj, ns);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// 字符串转XML格式
    /// </summary>
    /// <param name="xmlStr"></param>
    /// <returns></returns>
    public static XElement SerializeStringToXml(string xmlStr)
    {
        try
        {
            return XElement.Parse(xmlStr);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析IP地址
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public static (string ipLocation, double? longitude, double? latitude) GetIpAddress(string ip)
    {
        try
        {
            var ipInfo = IpTool.SearchWithI18N(ip); // 国际化查询，默认中文 中文zh-CN、英文en
            var addressList = new List<string>() { ipInfo.Country, ipInfo.Province, ipInfo.City, ipInfo.NetworkOperator };
            return (string.Join(" ", addressList.Where(u => u != "0" && !string.IsNullOrWhiteSpace(u)).ToList()), ipInfo.Longitude, ipInfo.Latitude); // 去掉0及空并用空格连接
        }
        catch
        {
            // 不做处理
        }
        return ("未知", 0, 0);
    }

    /// <summary>
    /// 获取客户端设备信息（操作系统+浏览器）
    /// </summary>
    /// <param name="userAgent"></param>
    /// <returns></returns>
    public static string GetClientDeviceInfo(string userAgent)
    {
        try
        {
            if (userAgent != null)
            {
                var client = Parser.GetDefault().Parse(userAgent);
                if (client.Device.IsSpider)
                    return "爬虫";
                return $"{client.OS.Family} {client.OS.Major} {client.OS.Minor}" +
                    $"|{client.UA.Family} {client.UA.Major}.{client.UA.Minor} / {client.Device.Family}";
            }
        }
        catch
        { }
        return "未知";
    }
}
