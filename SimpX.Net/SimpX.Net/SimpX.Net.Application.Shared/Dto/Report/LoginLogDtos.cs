using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 登录日志筛选（对齐旧版 /admin/report/user/loginlog）。
/// </summary>
public sealed class LoginLogAdminPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>开始时间；空默认十年前。</summary>
    public string StartAt { get; set; }

    /// <summary>结束时间；空默认现在。</summary>
    public string EndAt { get; set; }

    /// <summary>用户名或编号。</summary>
    public string User { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>IP 归属模糊。</summary>
    public string IPAddress { get; set; }

    /// <summary>仅最近一次；传 1。</summary>
    public string IsLast { get; set; }

    /// <summary>平台；null=全部（0电脑/1手机/2APP）。</summary>
    public int? Platform { get; set; }
}

/// <summary>
/// 登录日志行。
/// </summary>
public sealed class LoginLogAdminListItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>登录时间。</summary>
    [JsonPropertyName("LoginAt")]
    public DateTime LoginAt { get; set; }

    /// <summary>IP。</summary>
    [JsonPropertyName("IP")]
    public string IP { get; set; }

    /// <summary>设备号。</summary>
    [JsonPropertyName("DeviceID")]
    public string DeviceID { get; set; }

    /// <summary>IP 归属。</summary>
    [JsonPropertyName("IPAddress")]
    public string IPAddress { get; set; }

    /// <summary>平台。</summary>
    [JsonPropertyName("Platform")]
    public int Platform { get; set; }

    /// <summary>登录域名。</summary>
    [JsonPropertyName("LoginDomain")]
    public string LoginDomain { get; set; }
}

/// <summary>
/// 登录日志分页。
/// </summary>
public sealed class LoginLogAdminPageOutput
{
    /// <summary>行。</summary>
    public List<LoginLogAdminListItemDto> Data { get; set; }

    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>页码。</summary>
    public int Page { get; set; }

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; }
}
