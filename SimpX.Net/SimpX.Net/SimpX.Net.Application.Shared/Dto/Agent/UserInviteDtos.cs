using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 邀请码列表筛选（对齐旧 /admin/user/agent/invite/list）。
/// </summary>
public sealed class UserInviteListInput
{
    /// <summary>邀请码。</summary>
    public string Invite { get; set; }

    /// <summary>邀请码（同 Invite）。</summary>
    public string ID { get; set; }

    /// <summary>代理用户名或编号。</summary>
    public string User { get; set; }

    /// <summary>会员类型整型；空不限。</summary>
    public string Type { get; set; }

    /// <summary>是否开放；null=全部。</summary>
    public bool? IsOpen { get; set; }
}

/// <summary>
/// 邀请码行。
/// </summary>
public sealed class UserInviteListItemDto
{
    /// <summary>邀请码。</summary>
    [JsonPropertyName("ID")]
    public string ID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>注册人数。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }

    /// <summary>是否开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>会员类型整型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }
}


/// <summary>
/// 邀请码详情（对齐旧版 /admin/user/agent/invite/info）。
/// </summary>
public sealed class UserInviteDetailDto
{
    /// <summary>邀请码。</summary>
    [JsonPropertyName("ID")]
    public string ID { get; set; }

    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>注册人数。</summary>
    [JsonPropertyName("Member")]
    public int Member { get; set; }

    /// <summary>是否开启。</summary>
    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>游戏设置 JSON（本刀原样回传）。</summary>
    [JsonPropertyName("Game")]
    public string Game { get; set; }
}

/// <summary>
/// 邀请码保存入参（对齐旧版 save；Lottery 按会员游戏配置封顶）。
/// </summary>
public sealed class UserInviteSaveInput
{
    /// <summary>邀请码；空=新建并自动生成。</summary>
    public string ID { get; set; }

    /// <summary>所属会员编号；新建必填。</summary>
    public int UserID { get; set; }

    /// <summary>用户名（新建时可按名解析编号）。</summary>
    public string User { get; set; }

    /// <summary>是否开启。</summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>游戏设置 JSON；空则写空对象。</summary>
    public string Game { get; set; }
}
