using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧版鉴权失败时塞进 info 的结构（ErrorType 供 Layui 识别）。
/// </summary>
public sealed class OldApiErrorInfo
{
    /// <summary>
    /// 错误类型：Login / Permission 等。
    /// </summary>
    [JsonPropertyName("ErrorType")]
    public string ErrorType { get; set; }
}

/// <summary>
/// 标记兼容接口不用登录也能访问（跟旧版 Guest 一样）。可以和 JWT 的 AllowAnonymous 一起用。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class OldApiGuestAttribute : Attribute
{
}

/// <summary>
/// 标记兼容接口需要的权限 GUID（跟旧版 Permission 一样）。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class OldApiPermissionAttribute : Attribute
{
    /// <summary>
    /// 注入权限 GUID。
    /// </summary>
    /// <param name="permissionId">旧版权限 ID（无连字符 GUID）</param>
    public OldApiPermissionAttribute(string permissionId)
    {
        PermissionId = permissionId ?? string.Empty;
    }

    /// <summary>权限 GUID 字符串。</summary>
    public string PermissionId { get; }
}

/// <summary>
/// 旧版 Form+Query 读参（对齐 QF）。
/// </summary>
public static class OldApiRequestParam
{
    /// <summary>
    /// 先 Form 后 Query；都没有返回 null。
    /// </summary>
    /// <param name="http">当前请求</param>
    /// <param name="name">参数名</param>
    /// <returns>参数值</returns>
    public static string Read(HttpContext http, string name)
    {
        if (http == null)
            return null;

        if (http.Request.HasFormContentType)
        {
            var formValue = http.Request.Form[name].ToString();
            if (!string.IsNullOrEmpty(formValue))
                return formValue;
        }

        var queryValue = http.Request.Query[name].ToString();
        return string.IsNullOrEmpty(queryValue) ? null : queryValue;
    }

    /// <summary>
    /// 读整数参数；缺省或解析失败返回默认值（取值方式跟旧版 QF(name, default) 一样）。
    /// </summary>
    /// <param name="http">当前请求</param>
    /// <param name="name">参数名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>整数值</returns>
    public static int ReadInt(HttpContext http, string name, int defaultValue = 0)
    {
        var text = Read(http, name);
        if (string.IsNullOrWhiteSpace(text))
            return defaultValue;
        return int.TryParse(text.Trim(), out var value) ? value : defaultValue;
    }
}

/// <summary>
/// 旧版对外返回：success / msg / info。兼容接口返回这个结构时要加 NonUnify，避免再被 AdminResult 包一层。
/// </summary>
public sealed class OldApiResult
{
    /// <summary>
    /// 是否成功：1 成功，0 失败（与旧版 int 一致）。
    /// </summary>
    [JsonPropertyName("success")]
    public int Success { get; set; }

    /// <summary>
    /// 提示文案；可含旧版 #标签#。
    /// </summary>
    [JsonPropertyName("msg")]
    public string Msg { get; set; }

    /// <summary>
    /// 附加数据；失败鉴权时可为带 ErrorType 的对象。
    /// </summary>
    [JsonPropertyName("info")]
    public object Info { get; set; }

    /// <summary>
    /// 构造成功或失败结果。
    /// </summary>
    /// <param name="ok">是否成功</param>
    /// <param name="msg">提示</param>
    /// <param name="info">附加数据</param>
    /// <returns>旧版返回格式</returns>
    public static OldApiResult Of(bool ok, string msg, object info = null) => new()
    {
        Success = ok ? 1 : 0,
        Msg = msg ?? string.Empty,
        Info = info
    };

    /// <summary>
    /// 构造成功结果。
    /// </summary>
    /// <param name="msg">提示</param>
    /// <param name="info">附加数据</param>
    /// <returns>旧版返回格式</returns>
    public static OldApiResult Ok(string msg, object info = null) => Of(true, msg, info);

    /// <summary>
    /// 构造失败结果。
    /// </summary>
    /// <param name="msg">提示</param>
    /// <param name="info">附加数据</param>
    /// <returns>旧版返回格式</returns>
    public static OldApiResult Fail(string msg, object info = null) => Of(false, msg, info);

    /// <summary>
    /// 未登录拒绝（Layui 认 ErrorType=Login）。
    /// </summary>
    /// <param name="msg">提示；默认请先登录</param>
    /// <returns>旧版返回格式</returns>
    public static OldApiResult NeedLogin(string msg = "#请先登录#") =>
        Fail(msg, new OldApiErrorInfo { ErrorType = OldApiErrorType.Login });

    /// <summary>
    /// 无权限拒绝（Layui 认 ErrorType=Permission）。
    /// </summary>
    /// <param name="msg">提示</param>
    /// <returns>旧版返回格式</returns>
    public static OldApiResult NeedPermission(string msg = "#没有权限#") =>
        Fail(msg, new OldApiErrorInfo { ErrorType = OldApiErrorType.Permission });
}

/// <summary>
/// 旧版错误类型字符串，写入 info.ErrorType，供 Layui 识别。
/// </summary>
public static class OldApiErrorType
{
    /// <summary>未登录。</summary>
    public const string Login = "Login";

    /// <summary>无权限。</summary>
    public const string Permission = "Permission";
}

/// <summary>
/// 会员会话 Cookie / 头名称（对齐 BetModule）。
/// </summary>
public static class MemberSessionKeys
{
    /// <summary>会员 Cookie 名。</summary>
    public const string UserInfoCookie = "USERINFO";
}

/// <summary>
/// 站点后台会话 Cookie / 响应头名（对齐 BetModule.TOKEN）。
/// </summary>
public static class AdminSessionKeys
{
    /// <summary>站点后台 Cookie 与响应头名。</summary>
    public const string Token = "TOKEN";
}

/// <summary>
/// 总后台会话 Cookie 名。
/// </summary>
public static class SystemSessionKeys
{
    /// <summary>总后台 Cookie 名。</summary>
    public const string Token = "SYSTEM_ADMIN_TOKEN";
}
