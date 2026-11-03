using Magicodes.ExporterAndImporter.Core;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 批量加扣款 Excel 行（对齐旧 addmoneybatchbyexcel 主列：用户/类型/币种/金额/前后备注/流水）。
/// 类型列支持整型或中文名（如「手工加款」）。
/// </summary>
public sealed class MoneyOperateExcelImport
{
    /// <summary>用户名。</summary>
    [ImporterHeader(Name = "用户名")]
    public string UserName { get; set; }

    /// <summary>资金类型：整型数字或中文名。</summary>
    [ImporterHeader(Name = "类型")]
    public string Type { get; set; }

    /// <summary>币种（数字；可空默认 1）。</summary>
    [ImporterHeader(Name = "币种")]
    public string Currency { get; set; }

    /// <summary>金额。</summary>
    [ImporterHeader(Name = "金额")]
    public decimal Money { get; set; }

    /// <summary>前端备注。</summary>
    [ImporterHeader(Name = "前端备注")]
    public string Description { get; set; }

    /// <summary>后端备注。</summary>
    [ImporterHeader(Name = "后端备注")]
    public string DescriptionA { get; set; }

    /// <summary>流水倍率。</summary>
    [ImporterHeader(Name = "流水倍")]
    public decimal Flow { get; set; } = 1;

    /// <summary>最低流水。</summary>
    [ImporterHeader(Name = "最低流水")]
    public decimal MinFlow { get; set; }
}
