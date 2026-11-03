using SimpX.Net.Application.Shared;
using System.Text.Json;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// Setting querystring 编解码与旧版 SettingBase 行为对齐的冒烟。
/// </summary>
public class SiteSettingQueryTests
{
    /// <summary>
    /// 往返：解析后再写出，关键键值仍在。
    /// </summary>
    [Fact]
    public void Parse_Then_ToQueryString_KeepsPasswordAndBool()
    {
        var raw = "Password=123456&IsMaintain=True&Logo=%2Fimages%2Fa.png";
        var map = SiteSettingQuery.Parse(raw);
        Assert.Equal("123456", SiteSettingQuery.Get(map, "Password"));
        Assert.True(SiteSettingQuery.GetBool(map, "IsMaintain"));

        var again = SiteSettingQuery.Parse(SiteSettingQuery.ToQueryString(map));
        Assert.Equal("123456", SiteSettingQuery.Get(again, "Password"));
        Assert.True(SiteSettingQuery.GetBool(again, "IsMaintain"));
    }

    /// <summary>
    /// JSON 合并写入 Setting 表，布尔写成 True/False。
    /// </summary>
    [Fact]
    public void MergeJson_WritesBoolAsTrueFalse()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse("{\"IsMaintain\":true,\"InviteCode\":\"ABC\"}");
        var dict = doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value);
        SiteSettingQuery.MergeJson(map, dict);
        Assert.Equal(bool.TrueString, map["IsMaintain"]);
        Assert.Equal("ABC", map["InviteCode"]);
    }
}
