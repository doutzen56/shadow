using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员锁定位与改锁权限常量单测。
/// </summary>
public class MemberLockBitTests
{
    /// <summary>
    /// 「其他功能」权限 GUID 跟旧版一致。
    /// </summary>
    [Fact]
    public void OtherFeaturePermissionId_MatchesKnown()
    {
        Assert.Equal("d7d1323ff76427d0eaa3a189fef234a7", UserMemberService.OtherFeaturePermissionId);
    }

    /// <summary>
    /// 锁定位按位或/异或（跟旧版 HasFlag 一样）。
    /// </summary>
    [Theory]
    [InlineData(0, 1, true, 1)]
    [InlineData(1, 1, false, 0)]
    [InlineData(1, 2, true, 3)]
    [InlineData(3, 2, false, 1)]
    public void LockBitMath_MatchesExpected(int current, int bit, bool isLock, int expected)
    {
        var has = (current & bit) == bit;
        int next;
        if (isLock && !has)
            next = current | bit;
        else if (!isLock && has)
            next = current ^ bit;
        else
            next = current;
        Assert.Equal(expected, next);
    }
}
