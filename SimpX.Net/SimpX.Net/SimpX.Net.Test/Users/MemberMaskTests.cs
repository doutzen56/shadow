using SimpX.Net.Application.Shared;
using System.Text.Json;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员脱敏与站点 DTO PascalCase 序列化单测。
/// </summary>
public class MemberMaskTests
{
    /// <summary>
    /// 站点列表 DTO 序列化后应保留 ID / Name 大写，避免 camelCase。
    /// </summary>
    [Fact]
    public void SiteListDto_SerializesPascalCasePropertyNames()
    {
        var dto = new SiteListItemDto
        {
            ID = 1000,
            Name = "演示",
            CreateAt = new DateTime(2020, 1, 1),
            Status = 1,
            GameCredit = 0,
            Currency = 1
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        // JsonPropertyName 应压过 camelCase；中文可能被转成 \uXXXX
        Assert.Contains("\"ID\":1000", json);
        Assert.True(json.Contains("\"Name\":"), json);
        Assert.DoesNotContain("\"id\":", json);
        Assert.DoesNotContain("\"name\":", json);
    }

    /// <summary>
    /// 域名 Link 由 SSL 拼协议。
    /// </summary>
    [Fact]
    public void DomainDto_KeepsPascalCaseLink()
    {
        var dto = new SiteDomainItemDto
        {
            ID = 1,
            Domain = "a.example.com",
            Link = "https://a.example.com",
            Weights = 10,
            AgentName = string.Empty,
            IsVisible = true,
            IsSSL = true,
            IsAPP = false,
            Speed = 0
        };
        var json = JsonSerializer.Serialize(dto);
        Assert.Contains("\"Link\":\"https://a.example.com\"", json);
        Assert.Contains("\"IsSSL\":true", json);
    }

    /// <summary>
    /// 权限 GUID 常量跟旧版 Permission 一致。
    /// </summary>
    [Fact]
    public void PermissionGuids_MatchKnownCatalog()
    {
        Assert.Equal("8c446b9b544948ee88ba7cc44014b0f9", SystemSiteService.SiteHostPermissionId);
        Assert.Equal("0f41d60ea0a3416996249682e985dfe3", SiteDomainService.DomainManagePermissionId);
    }

    /// <summary>
    /// 联系方式脱敏：姓名 / 邮箱 / 手机（F5.5）。
    /// </summary>
    [Fact]
    public void ContactMask_NameEmailMobile()
    {
        Assert.Equal("张*丰", ContactMask.MaskName("张三丰"));
        Assert.Equal("a**c@example.com", ContactMask.MaskEmail("abc@example.com"));
        Assert.Equal("138****5678", ContactMask.MaskMobile("13812345678"));
        Assert.Equal("138****5678", ContactMask.MaskMobile("86,13812345678"));
    }
}
