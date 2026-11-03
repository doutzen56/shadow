using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 改禁止货币入参（对齐旧 updatecurrency：UserID / Value / IsOpen）。
/// </summary>
public sealed class MemberUpdateCurrencyInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>币种名或编号（写入 DisableCurrency）。</summary>
    public string Value { get; set; }

    /// <summary>是否禁止（1=写入名单）。</summary>
    public bool IsOpen { get; set; }
}

/// <summary>
/// 升级/降级会员类型入参。
/// </summary>
public sealed class MemberUserIdInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }
}

/// <summary>
/// 会员银行卡列表行（详情页 banklist）。
/// </summary>
public sealed class MemberBankListItemDto
{
    /// <summary>帐户编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>类型说明。</summary>
    [JsonPropertyName("Type")]
    public string Type { get; set; }

    /// <summary>开户名。</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    /// <summary>银行名。</summary>
    [JsonPropertyName("BankName")]
    public string BankName { get; set; }

    /// <summary>帐号。</summary>
    [JsonPropertyName("Account")]
    public string Account { get; set; }

    /// <summary>开户明细。</summary>
    [JsonPropertyName("AccountDetail")]
    public string AccountDetail { get; set; }

    /// <summary>凭证路径。</summary>
    [JsonPropertyName("BankInfo")]
    public string BankInfo { get; set; }

    /// <summary>审核状态。</summary>
    [JsonPropertyName("Status")]
    public int Status { get; set; }

    /// <summary>是否虚拟。</summary>
    [JsonPropertyName("IsVir")]
    public string IsVir { get; set; }

    /// <summary>创建时间。</summary>
    [JsonPropertyName("CreateAt")]
    public DateTime CreateAt { get; set; }
}

/// <summary>
/// 钱包摘要（对齐旧 wallet）。
/// </summary>
public sealed class MemberWalletDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }

    /// <summary>总余额。</summary>
    [JsonPropertyName("TotalMoney")]
    public decimal TotalMoney { get; set; }

    /// <summary>可用（旧 Money 字段，这里用 Total-Lock）。</summary>
    [JsonPropertyName("Money")]
    public decimal Money { get; set; }

    /// <summary>锁定金额。</summary>
    [JsonPropertyName("LockMoney")]
    public decimal LockMoney { get; set; }

    /// <summary>流水要求。</summary>
    [JsonPropertyName("WashFlow")]
    public decimal WashFlow { get; set; }
}

/// <summary>
/// 多币种流水（对齐旧 washflow）。
/// </summary>
public sealed class MemberWashFlowDto
{
    /// <summary>流水文案。</summary>
    [JsonPropertyName("WashFlow")]
    public string WashFlow { get; set; }

    /// <summary>币种文案。</summary>
    [JsonPropertyName("Currency")]
    public string Currency { get; set; }
}

/// <summary>
/// 手动锁钱入参（对齐旧 lockmoney）。
/// </summary>
public sealed class MemberLockMoneyInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>锁定金额。</summary>
    public decimal Money { get; set; }

    /// <summary>锁定备注。</summary>
    public string LockDesc { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }
}

/// <summary>
/// 手动解锁入参（对齐旧 unlockmoney：ID=锁记录编号）。
/// </summary>
public sealed class MemberUnlockMoneyInput
{
    /// <summary>锁记录编号。</summary>
    public int ID { get; set; }
}

/// <summary>
/// 改上级入参（对齐旧 updateagent）。
/// </summary>
public sealed class MemberUpdateAgentInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>上级用户名；「总代」表示清上级。</summary>
    public string Agent { get; set; }
}

/// <summary>
/// 用户名联想入参（对齐旧 query：Key）。
/// </summary>
public sealed class MemberQueryInput
{
    /// <summary>关键字。</summary>
    public string Key { get; set; }
}

/// <summary>
/// 按用户名取编号入参。
/// </summary>
public sealed class MemberGetUserIdInput
{
    /// <summary>用户名（旧 User）。</summary>
    public string User { get; set; }
}

/// <summary>
/// 按用户名取编号结果。
/// </summary>
public sealed class MemberGetUserIdOutput
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }
}

/// <summary>
/// 上级链节点。
/// </summary>
public sealed class MemberParentItemDto
{
    /// <summary>会员编号。</summary>
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; }
}

/// <summary>
/// 黑名单手机查询结果。
/// </summary>
public sealed class MemberBlackMobileDto
{
    /// <summary>是否黑名单。</summary>
    [JsonPropertyName("IsBlack")]
    public bool IsBlack { get; set; }

    /// <summary>备注。</summary>
    [JsonPropertyName("Demo")]
    public string Demo { get; set; }
}

/// <summary>
/// 多币种余额字典项。
/// </summary>
public sealed class MemberMoneyByCurrencyDto
{
    /// <summary>主钱包文案（detailmoney）。</summary>
    [JsonPropertyName("Master")]
    public string Master { get; set; }

    /// <summary>其它币种 JSON 字符串（对齐旧 Detail）。</summary>
    [JsonPropertyName("Detail")]
    public string Detail { get; set; }

    /// <summary>全部币种余额（moremoney）。</summary>
    [JsonPropertyName("All")]
    public Dictionary<string, decimal> All { get; set; }
}

/// <summary>
/// 批量备注：直接传行（Excel 下载本刀用 Items；旧 url 留给后续接文件）。
/// </summary>
public sealed class MemberBatchAddMarkInput
{
    /// <summary>旧版 Excel 地址（可空；本刀优先 Items）。</summary>
    public string Url { get; set; }

    /// <summary>行：UserName + Content。</summary>
    public List<MemberBatchAddMarkRow> Items { get; set; }
}

/// <summary>
/// 批量备注一行。
/// </summary>
public sealed class MemberBatchAddMarkRow
{
    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>备注。</summary>
    public string Content { get; set; }
}

/// <summary>
/// 导出结果：行列表（本刀不生成 Excel 文件流，返回与列表同形的数据供前端导出）。
/// </summary>
public sealed class MemberExportOutput
{
    /// <summary>导出行。</summary>
    public List<MemberListItemDto> Data { get; set; }

    /// <summary>条数。</summary>
    public int RecordCount { get; set; }
}
