using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 兼容权限判定，以及按路径判断是哪一端。
/// </summary>
public class OldApiPermissionEvaluatorTests
{
    /// <summary>
    /// admin / GroupID=0 拥有全部权限；普通分组按 CSV。
    /// </summary>
    [Fact]
    public void SiteAdminHasAll_AdminOrGroupZero_OrCsv()
    {
        Assert.True(OldApiPermissionEvaluator.SiteAdminHasAll("admin", 1, "", ["abc"]));
        Assert.True(OldApiPermissionEvaluator.SiteAdminHasAll("ops", 0, "", ["abc"]));
        Assert.True(OldApiPermissionEvaluator.SiteAdminHasAll("ops", 2, "abc,def", ["abc"]));
        Assert.False(OldApiPermissionEvaluator.SiteAdminHasAll("ops", 2, "def", ["abc"]));
    }

    /// <summary>
    /// 总后台 Permission 空=全权限。
    /// </summary>
    [Fact]
    public void SystemAdminHasAll_EmptyMeansAll()
    {
        Assert.True(OldApiPermissionEvaluator.SystemAdminHasAll(null, ["x"]));
        Assert.True(OldApiPermissionEvaluator.SystemAdminHasAll("x,y", ["x"]));
        Assert.False(OldApiPermissionEvaluator.SystemAdminHasAll("y", ["x"]));
    }

    /// <summary>
    /// 拒绝文案能带上会员列表标题。
    /// </summary>
    [Fact]
    public void BuildPermissionDeniedMessage_UsesMenuTitleWhenKnown()
    {
        var msg = OldApiPermissionEvaluator.BuildPermissionDeniedMessage("1e7cc96020b843b988ed6df39024867e");
        Assert.Contains("会员列表", msg);
    }

    /// <summary>
    /// 路径面识别。
    /// </summary>
    [Fact]
    public void ResolveFace_ByPathPrefix()
    {
        Assert.Equal(OldApiAuthAttribute.AuthFace.Member, OldApiAuthAttribute.ResolveFace("/request/user/pingauth"));
        Assert.Equal(OldApiAuthAttribute.AuthFace.SiteAdmin, OldApiAuthAttribute.ResolveFace("/admin/info/menu"));
        Assert.Equal(OldApiAuthAttribute.AuthFace.SystemAdmin, OldApiAuthAttribute.ResolveFace("/system/config/account/menu"));
        Assert.Equal(OldApiAuthAttribute.AuthFace.Unknown, OldApiAuthAttribute.ResolveFace("/api/other"));
    }
}
