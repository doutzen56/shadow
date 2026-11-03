using SimpX.Net.Application.Shared;
using SimpX.Net.Core;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 兼容菜单目录：可读码种子组树与权限勾选。
/// </summary>
public class LayuiMenuCatalogTests
{
    /// <summary>
    /// 全权限时顶级菜单有 name/id，且 checked=1。
    /// </summary>
    [Fact]
    public void BuildSiteAdminMenu_AllChecked_HasRequiredFields()
    {
        var nodes = LayuiMenuCatalog.BuildSiteAdminMenu(null);
        Assert.NotEmpty(nodes);
        var first = nodes[0];
        Assert.False(string.IsNullOrWhiteSpace(first.Name));
        Assert.False(string.IsNullOrWhiteSpace(first.Id));
        Assert.Equal(1, first.Checked);
        Assert.Contains("\"name\":", first.ToOldApiJson());
        Assert.Contains("\"checked\":1", first.ToOldApiJson());
    }

    /// <summary>
    /// 仅授予某个 id 时，其它节点 checked=0。
    /// </summary>
    [Fact]
    public void BuildSiteAdminMenu_PartialPermission_MarksChecked()
    {
        var all = LayuiMenuCatalog.BuildSiteAdminMenu(null);
        var targetId = all[0].Id;
        var filtered = LayuiMenuCatalog.BuildSiteAdminMenu([targetId]);
        Assert.Equal(1, filtered[0].Checked);
        if (filtered.Count > 1)
            Assert.Equal(0, filtered[1].Checked);
    }

    /// <summary>
    /// 扁平枚举仍含旧 GUID（XML 原始 id，仅迁入对照）。
    /// </summary>
    [Fact]
    public void EnumerateFlat_ContainsKnownMemberListGuid()
    {
        var ids = LayuiMenuCatalog.EnumerateFlat(LayuiMenuCatalog.SiteAdminResourceSuffix)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("1e7cc96020b843b988ed6df39024867e", ids);
    }

    /// <summary>
    /// 构建菜单时对外 id 为可读码（不是 32 位 GUID）。
    /// </summary>
    [Fact]
    public void BuildSiteAdminMenu_UsesReadablePermissionCodes()
    {
        var nodes = LayuiMenuCatalog.BuildSiteAdminMenu(null);
        Assert.NotEmpty(nodes);
        Assert.StartsWith("site:", nodes[0].Id);
        Assert.False(nodes[0].Id.Length == 32 && nodes[0].Id.All(Uri.IsHexDigit));
    }

    /// <summary>
    /// 种子 HasData 使用独立 Id 段、Permission 为可读码，且有父子 Pid。
    /// </summary>
    [Fact]
    public void SiteAdminMenuSeed_UsesBetWinIdSegmentAndTree()
    {
        var rows = new SiteAdminMenuSeedData().HasData().ToList();
        Assert.NotEmpty(rows);
        Assert.All(rows, r =>
        {
            Assert.True(r.Id >= SiteAdminMenuSeedData.IdBase);
            Assert.False(string.IsNullOrWhiteSpace(r.Permission));
            Assert.Equal(LayuiMenuCatalog.SiteAdminRemark, r.Remark);
        });
        Assert.Contains(rows, r => r.Pid == 0);
        Assert.Contains(rows, r => r.Pid != 0);
        Assert.Contains(rows, r => r.Permission == "site:user:member:list");
        Assert.Contains(rows, r => r.Type != MenuTypeEnum.Btn && r.Component == "/menu/placeholder");
    }

    /// <summary>
    /// 总后台种子 Id 段与站点段隔离，Permission 为 platform: 前缀。
    /// </summary>
    [Fact]
    public void SystemAdminMenuSeed_UsesOwnIdSegment()
    {
        var rows = new SystemAdminMenuSeedData().HasData().ToList();
        Assert.NotEmpty(rows);
        Assert.All(rows, r =>
        {
            Assert.True(r.Id >= SystemAdminMenuSeedData.IdBase);
            Assert.Equal(LayuiMenuCatalog.SystemAdminRemark, r.Remark);
        });
        Assert.Contains(rows, r => r.Permission != null && r.Permission.StartsWith("platform:"));
    }

    /// <summary>
    /// 从种子行组树，与 BuildSiteAdminMenu 顶级数量一致。
    /// </summary>
    [Fact]
    public void BuildFromSysMenus_MatchesXmlTopLevelCount()
    {
        var xml = LayuiMenuCatalog.BuildSiteAdminMenu(null);
        var fromDb = LayuiMenuCatalog.BuildFromSysMenus(new SiteAdminMenuSeedData().HasData(), null);
        Assert.Equal(xml.Count, fromDb.Count);
        Assert.Equal(xml[0].Id, fromDb[0].Id);
        Assert.Equal(1, fromDb[0].Checked);
    }
}
