using System.Text;
using System.Text.Json.Serialization;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// Layui 后台菜单节点（字段跟旧版菜单 JSON 一样）。
/// </summary>
public sealed class LayuiMenuNode
{
    /// <summary>显示名。</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>权限/菜单 GUID（旧版 ID）。</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>页面路径；目录可空。</summary>
    [JsonPropertyName("href")]
    public string Href { get; set; }

    /// <summary>图标 class。</summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    /// <summary>是否授权：1/0。</summary>
    [JsonPropertyName("checked")]
    public int Checked { get; set; }

    /// <summary>子节点。</summary>
    [JsonPropertyName("menu")]
    public List<LayuiMenuNode> Menu { get; set; }

    /// <summary>
    /// 序列化为旧版同形 JSON 片段（字段顺序与省略规则对齐 Menu.ToString）。
    /// </summary>
    /// <returns>单个节点 JSON</returns>
    public string ToOldApiJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.AppendFormat("\"name\":\"{0}\"", Escape(Name));
        sb.AppendFormat(",\"id\":\"{0}\"", Escape(Id));
        if (!string.IsNullOrEmpty(Href))
            sb.AppendFormat(",\"href\":\"{0}\"", Escape(Href));
        if (!string.IsNullOrEmpty(Icon))
            sb.AppendFormat(",\"icon\":\"{0}\"", Escape(Icon));
        sb.AppendFormat(",\"checked\":{0}", Checked);
        if (Menu is { Count: > 0 })
        {
            sb.Append(",\"menu\":[");
            sb.Append(string.Join(",", Menu.Select(m => m.ToOldApiJson())));
            sb.Append(']');
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string Escape(string value) =>
        (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
