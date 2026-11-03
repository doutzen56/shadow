using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>导入暂存列表筛选。</summary>
public sealed class MemberImportPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名模糊。</summary>
    public string UserName { get; set; }

    /// <summary>状态筛选；空不限。会员导入用 0/1/2；证件/银行用 0/1。</summary>
    public string Status { get; set; }
}

/// <summary>通用分页包装。</summary>
/// <typeparam name="T">行类型</typeparam>
public sealed class MemberImportPageOutput<T>
{
    /// <summary>总条数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<T> Items { get; set; } = [];
}

/// <summary>注册会员导入行。</summary>
public sealed class UserImportItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>上级代理名。</summary>
    public string AgentName { get; set; }

    /// <summary>密码明文（仅暂存展示）。</summary>
    public string Password { get; set; }

    /// <summary>手机。</summary>
    public string Mobile { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>导入额度（本刀申请时不落账）。</summary>
    public decimal Balance { get; set; }

    /// <summary>开户名。</summary>
    public string AccountName { get; set; }

    /// <summary>0 未导入 / 1 成功 / 2 失败。</summary>
    public byte Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>保存注册会员导入行。</summary>
public sealed class UserImportSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>上级代理名。</summary>
    public string AgentName { get; set; }

    /// <summary>密码明文。</summary>
    public string Password { get; set; }

    /// <summary>手机。</summary>
    public string Mobile { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>额度（暂存；申请不落账）。</summary>
    public decimal Balance { get; set; }

    /// <summary>开户名。</summary>
    public string AccountName { get; set; }
}

/// <summary>证件导入行。</summary>
public sealed class IdCardImportItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>证件号。</summary>
    public string CardNumber { get; set; }

    /// <summary>正面。</summary>
    public string CardFront { get; set; }

    /// <summary>背面。</summary>
    public string CardBack { get; set; }

    /// <summary>城市。</summary>
    public string CardCity { get; set; }

    /// <summary>发证日。</summary>
    public DateTime CardCreateAt { get; set; }

    /// <summary>类型。</summary>
    public int CardType { get; set; }

    /// <summary>生日。</summary>
    public DateTime CardBirthday { get; set; }

    /// <summary>是否已导入。</summary>
    public bool Status { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>保存证件导入。</summary>
public sealed class IdCardImportSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>证件号。</summary>
    public string CardNumber { get; set; }

    /// <summary>正面。</summary>
    public string CardFront { get; set; }

    /// <summary>背面。</summary>
    public string CardBack { get; set; }

    /// <summary>城市。</summary>
    public string CardCity { get; set; }

    /// <summary>发证日。</summary>
    public DateTime? CardCreateAt { get; set; }

    /// <summary>类型。</summary>
    public int CardType { get; set; }

    /// <summary>生日。</summary>
    public DateTime? CardBirthday { get; set; }
}

/// <summary>银行导入行。</summary>
public sealed class BankImportItemDto
{
    /// <summary>编号。</summary>
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>银行类型。</summary>
    public int Type { get; set; }

    /// <summary>银行名。</summary>
    public string BankName { get; set; }

    /// <summary>开户名。</summary>
    public string Name { get; set; }

    /// <summary>帐号。</summary>
    public string Account { get; set; }

    /// <summary>分行。</summary>
    public string AccountDetail { get; set; }

    /// <summary>类型。</summary>
    public string IsVir { get; set; }

    /// <summary>地址。</summary>
    public string BankAddress { get; set; }

    /// <summary>分行码。</summary>
    public string BranchCode { get; set; }

    /// <summary>凭证。</summary>
    public string BankInfo { get; set; }

    /// <summary>是否已导入。</summary>
    public bool Status { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>保存银行导入。</summary>
public sealed class BankImportSaveInput
{
    /// <summary>0=新增。</summary>
    public int ID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>银行类型。</summary>
    public int Type { get; set; }

    /// <summary>银行名。</summary>
    public string BankName { get; set; }

    /// <summary>开户名。</summary>
    public string Name { get; set; }

    /// <summary>帐号。</summary>
    public string Account { get; set; }

    /// <summary>分行。</summary>
    public string AccountDetail { get; set; }

    /// <summary>0/1/2。</summary>
    public string IsVir { get; set; } = "0";

    /// <summary>地址。</summary>
    public string BankAddress { get; set; }

    /// <summary>分行码。</summary>
    public string BranchCode { get; set; }

    /// <summary>凭证。</summary>
    public string BankInfo { get; set; }
}

/// <summary>批量申请入参（编号列表）。</summary>
public sealed class MemberImportApplyInput
{
    /// <summary>暂存编号。</summary>
    public List<int> Ids { get; set; } = [];
}

/// <summary>申请结果。</summary>
public sealed class MemberImportApplyOutput
{
    /// <summary>成功条数。</summary>
    public int SuccessCount { get; set; }

    /// <summary>说明。</summary>
    public string Message { get; set; }
}
