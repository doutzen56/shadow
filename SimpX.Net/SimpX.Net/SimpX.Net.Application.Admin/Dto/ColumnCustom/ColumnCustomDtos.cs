namespace SimpX.Net.Application.Admin;

/// <summary>
/// 获取/重置表格列配置的公共入参。
/// </summary>
public class BaseColumnCustomInput
{
    /// <summary>
    /// 表格 Id（前端 vxe grid id）
    /// </summary>
    [Required(ErrorMessage = "表格Id不能为空")]
    public virtual string GridId { get; set; }
}

/// <summary>
/// 获取用户表格列配置。
/// </summary>
public class GetColumnCustomInput : BaseColumnCustomInput
{
}

/// <summary>
/// 保存用户表格列配置。
/// </summary>
public class StoreColumnCustomInput
{
    /// <summary>
    /// 表格 Id
    /// </summary>
    [Required(ErrorMessage = "表格Id不能为空")]
    public virtual string GridId { get; set; }

    /// <summary>
    /// 冻结列状态
    /// </summary>
    public virtual Dictionary<string, string>? FixedData { get; set; }

    /// <summary>
    /// 列宽状态
    /// </summary>
    public virtual Dictionary<string, int>? ResizableData { get; set; }

    /// <summary>
    /// 列顺序
    /// </summary>
    public virtual Dictionary<string, int>? SortData { get; set; }

    /// <summary>
    /// 显示/隐藏列
    /// </summary>
    public virtual Dictionary<string, bool>? VisibleData { get; set; }
}

/// <summary>
/// 清除用户表格列配置。
/// </summary>
public class ResetColumnCustomInput : BaseColumnCustomInput
{
}

/// <summary>
/// 用户表格列配置输出。
/// </summary>
public class ColumnCustomOutput
{
    /// <summary>
    /// 用户 Id
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 表格 Id
    /// </summary>
    public string GridId { get; set; }

    /// <summary>
    /// 冻结列状态
    /// </summary>
    public Dictionary<string, string>? FixedData { get; set; }

    /// <summary>
    /// 列宽状态
    /// </summary>
    public Dictionary<string, int>? ResizableData { get; set; }

    /// <summary>
    /// 列顺序
    /// </summary>
    public Dictionary<string, int>? SortData { get; set; }

    /// <summary>
    /// 显示/隐藏列
    /// </summary>
    public Dictionary<string, bool>? VisibleData { get; set; }
}
