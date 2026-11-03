using SqlSugar;

namespace SimpX.Net.Data.Entity.Systems;

/// <summary>
/// 系统标签（表 tag；复合主键 Type+Code）。对齐旧 BW.Common.Systems.Tag。
/// </summary>
[SugarTable("tag")]
public class SystemTag
{
    /// <summary>
    /// 标签类型（旧 TagType：1 系统权限 / 2 商户权限 / 3 枚举 / 4 报表）。
    /// </summary>
    [SugarColumn(ColumnName = "TagType", IsPrimaryKey = true)]
    public byte Type { get; set; }

    /// <summary>标签码。</summary>
    [SugarColumn(ColumnName = "Code", IsPrimaryKey = true, Length = 64)]
    public string Code { get; set; }

    /// <summary>多语名称（旧 Translate 序列化进 TagName）。</summary>
    [SugarColumn(ColumnName = "TagName")]
    public string Name { get; set; }

    /// <summary>排序，从大到小。</summary>
    [SugarColumn(ColumnName = "Sort")]
    public short Sort { get; set; }

    /// <summary>参数配置。</summary>
    [SugarColumn(ColumnName = "Setting")]
    public string Setting { get; set; }

    /// <summary>图片。</summary>
    [SugarColumn(ColumnName = "Image")]
    public string Image { get; set; }
}
