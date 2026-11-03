namespace SimpX.Net.Data;

/// <summary>
/// 表模块特性  用于增加表的前缀
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TableModuleAttribute : Attribute
{
    public string Prefix { get; }

    public TableModuleAttribute(string prefix) => Prefix = prefix;
}