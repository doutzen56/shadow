using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 总后台站点名解析与权限常量单测。
/// </summary>
public class SystemSiteServiceTests
{
    /// <summary>
    /// 站点名 JSON 应取出 CHN；普通字符串原样返回。
    /// </summary>
    [Fact]
    public void PickChineseName_ParsesChnOrPassthrough()
    {
        Assert.Equal("演示站", SystemSiteService.PickChineseName("{\"CHN\":\"演示站\",\"ENG\":\"Demo\"}"));
        Assert.Equal("plain", SystemSiteService.PickChineseName("plain"));
        Assert.Equal(string.Empty, SystemSiteService.PickChineseName(null));
        Assert.Equal("d8802dfc53aa4c448624aa242d40bc0c", SystemSiteService.SiteListPermissionId);
    }
}
