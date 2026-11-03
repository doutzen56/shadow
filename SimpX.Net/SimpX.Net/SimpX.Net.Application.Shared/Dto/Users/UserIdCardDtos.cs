using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 证件信息筛选（对齐旧版 /admin/user/member/idcard/list）。
/// </summary>
public sealed class UserIdCardAdminPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>开始时间；空不限。</summary>
    public string StartAt { get; set; }

    /// <summary>结束时间；空不限。</summary>
    public string EndAt { get; set; }

    /// <summary>证件号。</summary>
    public string UserNumber { get; set; }

    /// <summary>会员用户名。</summary>
    public string UserName { get; set; }

    /// <summary>审核状态整型；空不限。</summary>
    public string Status { get; set; }
}

/// <summary>
/// 证件信息行（列表不返回证件照路径）。
/// </summary>
public sealed class UserIdCardAdminListItemDto
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

    /// <summary>证件姓名。</summary>
    [JsonPropertyName("CardName")]
    public string CardName { get; set; }

    /// <summary>证件号。</summary>
    [JsonPropertyName("CardNumber")]
    public string CardNumber { get; set; }

    /// <summary>发证城市。</summary>
    [JsonPropertyName("CardCity")]
    public string CardCity { get; set; }

    /// <summary>发证日期。</summary>
    [JsonPropertyName("CardCreateAt")]
    public DateTime CardCreateAt { get; set; }

    /// <summary>生日。</summary>
    [JsonPropertyName("CardBirthday")]
    public DateTime CardBirthday { get; set; }

    /// <summary>证件类型整型。</summary>
    [JsonPropertyName("CardType")]
    public int CardType { get; set; }

    /// <summary>审核状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>审核人编号。</summary>
    [JsonPropertyName("Checker")]
    public int? Checker { get; set; }

    /// <summary>审核时间。</summary>
    [JsonPropertyName("CheckDateTime")]
    public DateTime? CheckDateTime { get; set; }
}

/// <summary>
/// 证件信息分页。
/// </summary>
public sealed class UserIdCardAdminPageOutput
{
    /// <summary>行。</summary>
    public List<UserIdCardAdminListItemDto> Data { get; set; }

    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>页码。</summary>
    public int Page { get; set; }

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; }
}


/// <summary>
/// 证件保存入参（对齐旧 createidcard：正反面路径、国家、姓名、证件号必填）。
/// </summary>
public sealed class UserIdCardSaveInput
{
    /// <summary>编号；0=新增。</summary>
    public int ID { get; set; }

    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>证件姓名。</summary>
    public string CardName { get; set; }

    /// <summary>证件号。</summary>
    public string CardNumber { get; set; }

    /// <summary>证件类型。</summary>
    public int CardType { get; set; }

    /// <summary>审核状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>城市。</summary>
    public string CardCity { get; set; }

    /// <summary>国家。</summary>
    public string Country { get; set; }

    /// <summary>性别。</summary>
    public int Gender { get; set; }

    /// <summary>证件正面图路径（对齐旧 CardFront）。</summary>
    public string CardFront { get; set; }

    /// <summary>证件背面图路径（对齐旧 CardBack）。</summary>
    public string CardBack { get; set; }

    /// <summary>用户名（新建时写入）。</summary>
    public string UserName { get; set; }

    /// <summary>发证日期。</summary>
    public DateTime? CardCreateAt { get; set; }

    /// <summary>生日。</summary>
    public DateTime? CardBirthday { get; set; }
}

/// <summary>
/// 证件删除入参。
/// </summary>
public sealed class UserIdCardDeleteInput
{
    /// <summary>编号。</summary>
    public int ID { get; set; }
}
