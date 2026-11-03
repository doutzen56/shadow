using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 银行信息列表筛选（对齐旧 admin/user/member/bank/list）。
/// </summary>
public sealed class BankAccountAdminPageInput
{
    /// <summary>页码，从 1 起。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>开始时间；空不限。</summary>
    public string StartAt { get; set; }

    /// <summary>结束时间；空不限。</summary>
    public string EndAt { get; set; }

    /// <summary>银行帐号。</summary>
    public string UserNumber { get; set; }

    /// <summary>会员用户名。</summary>
    public string UserName { get; set; }

    /// <summary>审核状态整型；空不限。</summary>
    public string Status { get; set; }

    /// <summary>帐户类别：0 银行卡 / 其它虚拟电子；空不限。</summary>
    public string IsVir { get; set; }

    /// <summary>会员币种；空不限（对齐旧站 SINGLE 钱包筛）。</summary>
    public string Currency { get; set; }
}

/// <summary>
/// 银行信息列表行。
/// </summary>
public sealed class BankAccountAdminListItemDto
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

    /// <summary>开户名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>帐号（前缀 ' 对齐旧表展示）。</summary>
    [JsonPropertyName("Account")]
    public string Account { get; set; }

    /// <summary>开户地址/分行。</summary>
    [JsonPropertyName("AccountDetail")]
    public string AccountDetail { get; set; }

    /// <summary>银行类型名或编号说明。</summary>
    [JsonPropertyName("BankName")]
    public string BankName { get; set; }

    /// <summary>银行类型整型。</summary>
    [JsonPropertyName("Type")]
    public int Type { get; set; }

    /// <summary>0 银行卡 / 其它虚拟。</summary>
    [JsonPropertyName("IsVir")]
    public string IsVir { get; set; }

    /// <summary>审核状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>添加时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }

    /// <summary>审核时间。</summary>
    [JsonPropertyName("CheckDateTime")]
    public DateTime? CheckDateTime { get; set; }

    /// <summary>会员币种。</summary>
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }
}

/// <summary>
/// 银行信息分页结果。
/// </summary>
public sealed class BankAccountAdminPageOutput
{
    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>当前页。</summary>
    public List<BankAccountAdminListItemDto> Items { get; set; } = [];
}

/// <summary>
/// 银行信息详情（只读；不含密钥）。
/// </summary>
public sealed class BankAccountAdminDetailDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>帐号。</summary>
    [JsonPropertyName("Account")]
    public string Account { get; set; }

    /// <summary>开户名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>银行说明。</summary>
    [JsonPropertyName("BankName")]
    public string BankName { get; set; }

    /// <summary>开户地址。</summary>
    [JsonPropertyName("AccountDetail")]
    public string AccountDetail { get; set; }

    /// <summary>审核状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Remark")]
    public string Remark { get; set; }

    /// <summary>凭证路径（可能为空）。</summary>
    [JsonPropertyName("BankInfo")]
    public string BankInfo { get; set; }
}

/// <summary>
/// 审核银行信息（对齐旧 updateinfo）。
/// </summary>
public sealed class BankAccountUpdateStatusInput
{
    /// <summary>帐户编号。</summary>
    public int ID { get; set; }

    /// <summary>审核状态：0 待审 / 1 失败 / 2 成功。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}

/// <summary>
/// 删除银行信息（对齐旧 delete/reset）。
/// </summary>
public sealed class BankAccountDeleteInput
{
    /// <summary>帐户编号。</summary>
    public int ID { get; set; }
}

/// <summary>
/// 后台新增银行信息（对齐旧 createbank；按 IsVir 填不同字段）。
/// </summary>
public sealed class BankAccountCreateInput
{
    /// <summary>会员编号（先 verifyUser 得到）。</summary>
    public int ID { get; set; }

    /// <summary>用户名（校验用）。</summary>
    public string UserName { get; set; }

    /// <summary>0 银行卡 / 1 虚拟钱包 / 2 电子钱包 / 4 超商。</summary>
    public string IsVir { get; set; } = "0";

    /// <summary>银行卡类型。</summary>
    public int Type { get; set; }

    /// <summary>开户名（银行卡）。</summary>
    public string Name { get; set; }

    /// <summary>帐号（银行卡）。</summary>
    public string Account { get; set; }

    /// <summary>开户行/分行说明。</summary>
    public string AccountDetail { get; set; }

    /// <summary>银行地址。</summary>
    public string BankAddress { get; set; }

    /// <summary>分行代码。</summary>
    public string BranchCode { get; set; }

    /// <summary>凭证路径。</summary>
    public string BankInfo { get; set; }

    /// <summary>虚拟币种类型。</summary>
    public int VCurrency { get; set; }

    /// <summary>虚拟协议名。</summary>
    public string VAgreement { get; set; }

    /// <summary>虚拟地址。</summary>
    public string VAddress { get; set; }

    /// <summary>电子钱包类型。</summary>
    public int EType { get; set; }

    /// <summary>电子钱包用户名。</summary>
    public string EUserName { get; set; }

    /// <summary>电子钱包帐号。</summary>
    public string EAccount { get; set; }

    /// <summary>电子钱包地区。</summary>
    public string EArea { get; set; }

    /// <summary>超商类型（对齐旧 CType）。</summary>
    public int CType { get; set; }

    /// <summary>超商开户名（对齐旧 CName，可空）。</summary>
    public string CName { get; set; }

    /// <summary>超商账号（对齐旧 CAccount，可空）。</summary>
    public string CAccount { get; set; }

    /// <summary>超商县市（对齐旧 CCity）。</summary>
    public string CCity { get; set; }

    /// <summary>超商地区（对齐旧 CDistrict）。</summary>
    public string CDistrict { get; set; }

    /// <summary>超商门店编号（对齐旧 CBankAddress，存 SiteStore.ID）。</summary>
    public int CBankAddress { get; set; }
}

/// <summary>
/// 按用户名查会员（对齐旧 verifyuser）。
/// </summary>
public sealed class BankAccountVerifyUserOutput
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>开户姓名（可空）。</summary>
    [JsonPropertyName("AccountName")]
    public string AccountName { get; set; }
}
