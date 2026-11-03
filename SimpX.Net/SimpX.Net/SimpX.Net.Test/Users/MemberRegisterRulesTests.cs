using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 注册密码哈希与用户名规则单测。
/// </summary>
public class MemberRegisterRulesTests
{
    /// <summary>
    /// 注册用密码哈希为大写 MD5，与登录校验同一套。
    /// </summary>
    [Fact]
    public void PasswordHash_IsUpperMd5()
    {
        var hash = Md5PasswordHasher.Hash("BwQa2026!x");
        Assert.Equal(32, hash.Length);
        Assert.True(Md5PasswordHasher.Matches("BwQa2026!x", hash));
        Assert.False(Md5PasswordHasher.Matches("other", hash));
    }

    /// <summary>
    /// 用户名要是 4～16 位英数、下划线或横杠。
    /// </summary>
    [Theory]
    [InlineData("ab12", true)]
    [InlineData("user_name-01", true)]
    [InlineData("ab", false)]
    [InlineData("中文用户", false)]
    [InlineData("toolongusernamestr", false)]
    public void UserNameRule_MatchesOldEnglishRule(string name, bool ok)
    {
        Assert.Equal(ok, MemberRegisterService.IsValidUserName(name));
    }

    /// <summary>
    /// Setting 子集能从 JSON 取出 Logo/Customer。
    /// </summary>
    [Fact]
    public void ParsePublicSetting_ReadsKnownFields()
    {
        var dto = RequestSiteService.ParsePublicSetting(
            "{\"Logo\":\"/a.png\",\"Customer\":\"https://cs.example\",\"Service\":\"s.svg\",\"MinOnlineMember\":3}");
        Assert.Equal("/a.png", dto.Logo);
        Assert.Equal("https://cs.example", dto.Customer);
        Assert.Equal("s.svg", dto.Service);
        Assert.Equal(3, dto.MinOnlineMember);
        Assert.Equal(string.Empty, RequestSiteService.ParsePublicSetting("").Logo);
    }

    /// <summary>
    /// 旧版 querystring 格式的 Setting 也能取出 Logo，并做 URL 解码。
    /// </summary>
    [Fact]
    public void ParsePublicSetting_ReadsQueryString()
    {
        var dto = RequestSiteService.ParsePublicSetting(
            "Logo=%2fa.png&Customer=https%3a%2f%2fcs.example&InviteCode=10102818&MinOnlineMember=3");
        Assert.Equal("/a.png", dto.Logo);
        Assert.Equal("https://cs.example", dto.Customer);
        Assert.Equal("10102818", dto.InviteCode);
        Assert.Equal(3, dto.MinOnlineMember);
    }
}
