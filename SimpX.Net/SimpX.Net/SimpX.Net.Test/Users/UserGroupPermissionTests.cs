using SimpX.Net.Application.Shared;
using System.Text.Json;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员分组权限常量与安全信息 DTO 序列化单测。
/// </summary>
public class UserGroupPermissionTests
{
    /// <summary>
    /// 分组相关权限 GUID 跟旧版 SitePermission 一致。
    /// </summary>
    [Fact]
    public void GroupPermissionGuids_MatchKnownCatalog()
    {
        Assert.Equal("30301d7ff5224fddbdecf18b6c1f4a65", UserGroupService.GroupListPermissionId);
        Assert.Equal("84997cf7c202dfec9ecf6b9bc7c3a3cb", UserGroupService.GroupEditPermissionId);
        Assert.Equal("bab2e8fe0bb20d9e8ba45ba245e5af99", UserGroupService.GroupDeletePermissionId);
    }

    /// <summary>
    /// 最高权限管理员应对编辑/删除返回 true。
    /// </summary>
    [Fact]
    public void SiteAdminHasAll_GroupIdZero_AllowsEditDelete()
    {
        Assert.True(OldApiPermissionEvaluator.SiteAdminHasAll(
            "demo001", 0, "whatever",
            [UserGroupService.GroupEditPermissionId, UserGroupService.GroupDeletePermissionId]));
    }

    /// <summary>
    /// 安全信息 DTO 在 camelCase 策略下仍输出旧版字段名（含 IsGoole）。
    /// </summary>
    [Fact]
    public void SafeInfoDto_KeepsOldApiPropertyNames()
    {
        var dto = new MemberSafeInfoDto
        {
            IsPayPassword = true,
            IsGoole = true,
            Is_Temporary_Password = false
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Assert.Contains("\"IsPayPassword\":true", json);
        Assert.Contains("\"IsGoole\":true", json);
        Assert.Contains("\"Is_Temporary_Password\":false", json);
        Assert.DoesNotContain("\"isPayPassword\"", json);
    }
}
