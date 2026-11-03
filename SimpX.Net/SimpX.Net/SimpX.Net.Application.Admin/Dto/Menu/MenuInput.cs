namespace SimpX.Net.Application.Admin;

public class MenuInput
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 菜单类型（1目录 2菜单 3按钮）
    /// </summary>
    public MenuTypeEnum? Type { get; set; }

    /// <summary>
    /// 租户Id
    /// </summary>
    public virtual long TenantId { get; set; }
}

public class AddMenuInput : SysMenu
{
    /// <summary>
    /// 名称
    /// </summary>
    [Required(ErrorMessage = "菜单名称不能为空")]
    public override string Title { get; set; }

    /// <summary>
    /// 租户Id
    /// </summary>
    public long TenantId { get; set; }
}

public class UpdateMenuInput : AddMenuInput
{
}

public class DeleteMenuInput : BaseIdInput
{
}

public class MenuStatusInput : BaseStatusInput
{
}