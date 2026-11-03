using SimpX.Net.Data;
using Xunit;

namespace SimpX.Net.Test.Data;

/// <summary>
/// 租户绑定域名工具行为：拆分、规范化、本机别名、重叠校验。
/// </summary>
public class TenantHostBindingTests
{
    /// <summary>
    /// 逗号/分号/空白分隔应拆开并去端口、小写、去重。
    /// </summary>
    [Fact]
    public void SplitHosts_MixedSeparators_NormalizesAndDedups()
    {
        var hosts = TenantHostBinding.SplitHosts("A.COM:443; b.com , a.com\n127.0.0.1:5005");

        Assert.Equal(new[] { "a.com", "b.com", "127.0.0.1" }.OrderBy(u => u).ToArray(),
            hosts.OrderBy(u => u).ToArray());
    }

    /// <summary>
    /// 空字段得到空列表，不抛异常。
    /// </summary>
    [Fact]
    public void SplitHosts_Blank_ReturnsEmpty()
    {
        Assert.Empty(TenantHostBinding.SplitHosts(null));
        Assert.Empty(TenantHostBinding.SplitHosts("  "));
    }

    /// <summary>
    /// 入库字符串应按规范化后的域名排序并用逗号连接。
    /// </summary>
    [Fact]
    public void NormalizeStore_SortsAndJoins()
    {
        var stored = TenantHostBinding.NormalizeStore(["Zed.com", "alpha.com:80", "alpha.com"]);
        Assert.Equal("alpha.com,zed.com", stored);
    }

    /// <summary>
    /// localhost 与 127.0.0.1 互为候选。
    /// </summary>
    [Fact]
    public void RequestCandidates_LocalAliases()
    {
        Assert.Contains("127.0.0.1", TenantHostBinding.RequestCandidates("localhost"));
        Assert.Contains("localhost", TenantHostBinding.RequestCandidates("127.0.0.1"));
    }

    /// <summary>
    /// 请求候选命中绑定列表任一域名则匹配。
    /// </summary>
    [Fact]
    public void MatchesAny_HitOrMiss()
    {
        Assert.True(TenantHostBinding.MatchesAny("a.com,b.com", ["b.com"]));
        Assert.False(TenantHostBinding.MatchesAny("a.com", ["c.com"]));
        Assert.False(TenantHostBinding.MatchesAny("", ["a.com"]));
    }

    /// <summary>
    /// 两段 Host 有交集才算重叠（用于唯一性校验）。
    /// </summary>
    [Fact]
    public void Overlaps_IntersectionOnly()
    {
        Assert.True(TenantHostBinding.Overlaps("a.com,b.com", "b.com,c.com"));
        Assert.False(TenantHostBinding.Overlaps("a.com", "c.com"));
        Assert.False(TenantHostBinding.Overlaps("", "a.com"));
    }
}
