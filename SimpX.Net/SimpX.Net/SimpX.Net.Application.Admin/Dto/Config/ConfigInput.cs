namespace SimpX.Net.Application.Admin;

public class ConfigInput : BaseIdInput
{
}

public class PageConfigInput : BasePageInput
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 分组编码
    /// </summary>
    public string GroupCode { get; set; }
}

public class AddConfigInput : SysConfig
{
}

public class UpdateConfigInput : AddConfigInput
{
}

public class DeleteConfigInput : BaseIdInput
{
}

/// <summary>
/// 批量配置参数输入
/// </summary>
public class BatchConfigInput
{
    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 属性值
    /// </summary>
    public string Value { get; set; }
}