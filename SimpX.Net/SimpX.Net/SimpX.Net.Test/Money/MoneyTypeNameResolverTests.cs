using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// Excel 资金类型中文名 / 数字解析。
/// </summary>
public sealed class MoneyTypeNameResolverTests
{
    /// <summary>数字与中文（带 #）都能解析。</summary>
    [Theory]
    [InlineData("250", 250)]
    [InlineData("手工加款", 250)]
    [InlineData("#手工加款#", 250)]
    [InlineData("手工减款", 251)]
    [InlineData("代客充值", 111)]
    public void Resolve_type_aliases(string raw, int expected)
    {
        Assert.True(MoneyTypeNameResolver.TryResolve(raw, out var type));
        Assert.Equal(expected, type);
    }

    /// <summary>未知名失败。</summary>
    [Fact]
    public void Unknown_name_fails()
    {
        Assert.False(MoneyTypeNameResolver.TryResolve("不存在的类型", out _));
    }

    /// <summary>币种空默认 1。</summary>
    [Fact]
    public void Currency_blank_defaults_to_one()
    {
        Assert.True(MoneyTypeNameResolver.TryResolveCurrency(null, out var c));
        Assert.Equal(1, c);
    }
}
