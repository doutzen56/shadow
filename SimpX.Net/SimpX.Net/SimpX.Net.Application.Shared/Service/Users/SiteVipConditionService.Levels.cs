using System.Text;
using System.Xml.Linq;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// VIP 等级 / 特权 / 升级日志缺口。等级存 site_VipConfig.VIPConditionSetting（&lt;root&gt;&lt;item .../&gt;&lt;/root&gt;）。
/// </summary>
public sealed partial class SiteVipConditionService
{
    /// <summary>等级列表。对齐 levels / getlevels。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等级行</returns>
    public async Task<List<SiteVipLevelItemDto>> ListLevelsAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        return ParseLevels(cfg.VIPConditionSetting)
            .OrderBy(t => t.Level)
            .Select(MapLevel)
            .ToList();
    }

    /// <summary>等级详情。对齐 levelinfo。</summary>
    /// <param name="id">Level；-1 或不存在则返回下一档空模板</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等级</returns>
    public async Task<SiteVipLevelItemDto> LevelInfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        var list = ParseLevels(cfg.VIPConditionSetting);
        var item = list.FirstOrDefault(p => p.Level == id);
        if (item != null)
            return MapLevel(item);
        var newId = list.Count > 0 ? list.Max(p => p.Level) + 1 : 0;
        return new SiteVipLevelItemDto
        {
            Level = (byte)Math.Clamp(newId, 0, 255),
            Name = string.Empty,
            Badge = string.Empty,
            GiftDescription = string.Empty,
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>保存等级。对齐 savelevel。</summary>
    /// <param name="input">等级字段 + 属性</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveLevelAsync(SiteVipLevelSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        var list = ParseLevels(cfg.VIPConditionSetting);
        var item = list.FirstOrDefault(p => p.Level == input.Level);
        if (item == null)
        {
            item = new VipLevelRaw { Level = input.Level };
            list.Add(item);
        }

        item.Name = input.Name ?? string.Empty;
        item.Badge = input.Badge ?? string.Empty;
        item.GiftDescription = input.GiftDescription ?? string.Empty;
        item.UpgradeFlag = input.UpgradeFlag;
        item.UpgradeClose = input.UpgradeClose;
        item.HoldFlag = input.HoldFlag;
        item.GiftFlag = input.GiftFlag;
        if (input.Attributes != null)
        {
            foreach (var kv in input.Attributes)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                item.Attributes[kv.Key] = kv.Value ?? string.Empty;
            }
        }

        cfg.VIPConditionSetting = BuildLevelsXml(list);
        await _db.Updateable(cfg).UpdateColumns(c => c.VIPConditionSetting).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除等级。对齐 deletelevel。</summary>
    /// <param name="level">等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteLevelAsync(byte level, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var used = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.Level == level).AnyAsync(cancellationToken);
        if (used)
            throw Oops.Oh("#该会员等级已绑定到会员,请解绑会员等级再删除。#");
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        var list = ParseLevels(cfg.VIPConditionSetting).Where(p => p.Level != level).ToList();
        cfg.VIPConditionSetting = BuildLevelsXml(list);
        await _db.Updateable(cfg).UpdateColumns(c => c.VIPConditionSetting).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>特权原文列表。对齐 rights（返回 VIPRightSetting 原文与等级列）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>RightsRaw / Levels</returns>
    public async Task<object> RightsAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        var levels = ParseLevels(cfg.VIPConditionSetting)
            .OrderBy(t => t.Level)
            .Select(t => new { t.Level, Name = DecodeMaybeBase64(t.Name) })
            .ToList();
        return new { RightsRaw = cfg.VIPRightSetting ?? string.Empty, Levels = levels };
    }

    /// <summary>特权详情占位。对齐 rightinfo（按 ID 字符串过滤原文节点若可解析）。</summary>
    /// <param name="id">特权枚举名或编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ID / RightsRaw</returns>
    public async Task<object> RightInfoAsync(string id, CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        return new { ID = id ?? string.Empty, RightsRaw = cfg.VIPRightSetting ?? string.Empty };
    }

    /// <summary>保存特权整段原文。对齐 saveright（本刀接受整段 XML/JSON）。</summary>
    /// <param name="input">原文</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveRightAsync(SiteVipRightSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        cfg.VIPRightSetting = input.VIPRightSetting ?? string.Empty;
        await _db.Updateable(cfg).UpdateColumns(c => c.VIPRightSetting).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>初始化/清缓存特权。对齐 initvipright / clearviprightcache（当前无缓存，重读并写回）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task InitVipRightAsync(CancellationToken cancellationToken = default)
    {
        var cfg = await EnsureVipConfigAsync(cancellationToken);
        cfg.VIPRightSetting ??= string.Empty;
        await _db.Updateable(cfg).UpdateColumns(c => c.VIPRightSetting).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>删除特权占位。对齐 deleteright（旧版已注释；这里拒绝空 ID）。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task DeleteRightAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        _ = cancellationToken;
        throw Oops.Oh("#特权删除未启用#");
    }

    /// <summary>升级日志分页。对齐 upgradelog / upgradelog1。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SiteVipUpgradeLogPageOutput> PageUpgradeLogAsync(
        SiteVipUpgradeLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteVipUpgradeLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 200);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<SiteVipUpGradeLog>().Where(l => l.SiteID == siteId);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(l => l.UserName.Contains(input.UserName.Trim()));
        if (input.Type > 0)
            query = query.Where(l => l.Type == input.Type);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(l => l.LogID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new SiteVipUpgradeLogPageOutput
        {
            Total = total,
            Items = rows.Select(l => new SiteVipUpgradeLogItemDto
            {
                LogID = l.LogID,
                Type = l.Type,
                UserID = l.UserID,
                UserName = l.UserName,
                OldLevel = l.OldLevel,
                NewLevel = l.NewLevel,
                CreateAt = l.CreateAt,
                Status = l.Status,
                Remark = l.Remark
            }).ToList()
        };
    }

    /// <summary>导出升级日志（返回当前页数据，文件导出留给前端）。对齐 exportupgrade。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SiteVipUpgradeLogItemDto>> ExportUpgradeAsync(
        SiteVipUpgradeLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SiteVipUpgradeLogPageInput();
        input.Page = 1;
        input.PageSize = 5000;
        var page = await PageUpgradeLogAsync(input, cancellationToken);
        return page.Items ?? [];
    }

    /// <summary>批量设置会员等级。对齐 setuserslevel。</summary>
    /// <param name="input">等级 + 用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功数</returns>
    public async Task<int> SetUsersLevelAsync(SiteVipSetUsersLevelInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var names = (input.UserNames ?? string.Empty)
            .Split(['\r', '\n', ',', ';', '，', '；'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToList();
        if (names.Count == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && names.Contains(u.UserName))
            .ToListAsync(cancellationToken);
        var n = 0;
        foreach (var u in users)
        {
            var old = u.Level;
            u.Level = input.Level;
            await _db.Updateable(u).UpdateColumns(x => x.Level).ExecuteCommandAsync(cancellationToken);
            await _db.Insertable(new SiteVipUpGradeLog
            {
                SiteID = siteId,
                Type = input.Level >= old ? 1 : 2,
                UserID = u.ID,
                UserName = u.UserName,
                OldLevel = old,
                NewLevel = input.Level,
                CreateAt = DateTime.Now,
                Status = true,
                Remark = "后台设置等级"
            }).ExecuteCommandAsync(cancellationToken);
            n++;
        }

        return n;
    }

    /// <summary>确保 VIP 配置行存在。</summary>
    private async Task<SiteVipConfig> EnsureVipConfigAsync(CancellationToken cancellationToken)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteVipConfig>()
            .Where(c => c.ID == siteId).Take(1).ToListAsync(cancellationToken);
        var cfg = rows.FirstOrDefault();
        if (cfg != null)
            return cfg;
        cfg = new SiteVipConfig
        {
            ID = siteId,
            CreateAt = DateTime.Now,
            VIPConditionSetting = "<root></root>",
            VIPRightSetting = string.Empty,
            RuleClause = string.Empty,
            Currency = 0,
            PointItemSettingString = string.Empty,
            ExperItemSettingString = string.Empty,
            PointRule = string.Empty,
            AutoReturnRule = string.Empty
        };
        await _db.Insertable(cfg).ExecuteCommandAsync(cancellationToken);
        return cfg;
    }

    /// <summary>解析等级 XML。</summary>
    private static List<VipLevelRaw> ParseLevels(string xml)
    {
        var list = new List<VipLevelRaw>();
        if (string.IsNullOrWhiteSpace(xml))
            return list;
        try
        {
            var root = XElement.Parse(xml);
            foreach (var el in root.Elements("item"))
            {
                var raw = new VipLevelRaw
                {
                    Level = byte.TryParse((string)el.Attribute("Level") ?? "0", out var lv) ? lv : (byte)0,
                    Name = (string)el.Attribute("Name") ?? string.Empty,
                    Badge = (string)el.Attribute("Badge") ?? string.Empty,
                    GiftDescription = (string)el.Attribute("GiftDescription") ?? string.Empty,
                    UpgradeFlag = string.Equals((string)el.Attribute("UpgradeFlag"), "true", StringComparison.OrdinalIgnoreCase)
                        || (string)el.Attribute("UpgradeFlag") == "1",
                    UpgradeClose = string.Equals((string)el.Attribute("UpgradeClose"), "true", StringComparison.OrdinalIgnoreCase)
                        || (string)el.Attribute("UpgradeClose") == "1",
                    HoldFlag = string.Equals((string)el.Attribute("HoldFlag"), "true", StringComparison.OrdinalIgnoreCase)
                        || (string)el.Attribute("HoldFlag") == "1",
                    GiftFlag = string.Equals((string)el.Attribute("GiftFlag"), "true", StringComparison.OrdinalIgnoreCase)
                        || (string)el.Attribute("GiftFlag") == "1"
                };
                foreach (var attr in el.Attributes())
                {
                    var name = attr.Name.LocalName;
                    if (name is "Level" or "Name" or "Badge" or "GiftDescription" or "UpgradeFlag" or "UpgradeClose" or "HoldFlag" or "GiftFlag")
                        continue;
                    raw.Attributes[name] = attr.Value ?? string.Empty;
                }

                list.Add(raw);
            }
        }
        catch
        {
            // 坏 XML 当空列表
        }

        return list;
    }

    /// <summary>拼等级 XML。</summary>
    private static string BuildLevelsXml(List<VipLevelRaw> list)
    {
        var sb = new StringBuilder();
        sb.Append("<root>");
        foreach (var it in list.OrderBy(t => t.Level))
        {
            sb.Append("<item");
            sb.AppendFormat(" Level=\"{0}\"", it.Level);
            sb.AppendFormat(" Name=\"{0}\"", XmlEscape(it.Name));
            sb.AppendFormat(" Badge=\"{0}\"", XmlEscape(it.Badge));
            sb.AppendFormat(" GiftDescription=\"{0}\"", XmlEscape(it.GiftDescription));
            sb.AppendFormat(" UpgradeFlag=\"{0}\"", it.UpgradeFlag);
            sb.AppendFormat(" UpgradeClose=\"{0}\"", it.UpgradeClose);
            sb.AppendFormat(" HoldFlag=\"{0}\"", it.HoldFlag);
            sb.AppendFormat(" GiftFlag=\"{0}\"", it.GiftFlag);
            foreach (var kv in it.Attributes)
                sb.AppendFormat(" {0}=\"{1}\"", kv.Key, XmlEscape(kv.Value));
            sb.Append(" />");
        }

        sb.Append("</root>");
        return sb.ToString();
    }

    private static SiteVipLevelItemDto MapLevel(VipLevelRaw t) => new()
    {
        Level = t.Level,
        Name = DecodeMaybeBase64(t.Name),
        Badge = t.Badge,
        GiftDescription = DecodeMaybeBase64(t.GiftDescription),
        UpgradeFlag = t.UpgradeFlag,
        UpgradeClose = t.UpgradeClose,
        HoldFlag = t.HoldFlag,
        GiftFlag = t.GiftFlag,
        Attributes = new Dictionary<string, string>(t.Attributes, StringComparer.OrdinalIgnoreCase)
    };

    private static string DecodeMaybeBase64(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(raw);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return raw;
        }
    }

    private static string XmlEscape(string value) =>
        (value ?? string.Empty)
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private sealed class VipLevelRaw
    {
        public byte Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string GiftDescription { get; set; } = string.Empty;
        public bool UpgradeFlag { get; set; }
        public bool UpgradeClose { get; set; }
        public bool HoldFlag { get; set; }
        public bool GiftFlag { get; set; }
        public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>VIP 等级行。</summary>
public sealed class SiteVipLevelItemDto
{
    /// <summary>等级。</summary>
    public byte Level { get; set; }

    /// <summary>名称（尽量已解码）。</summary>
    public string Name { get; set; }

    /// <summary>徽章。</summary>
    public string Badge { get; set; }

    /// <summary>豪礼说明。</summary>
    public string GiftDescription { get; set; }

    /// <summary>晋级关系。</summary>
    public bool UpgradeFlag { get; set; }

    /// <summary>自定义等级开关。</summary>
    public bool UpgradeClose { get; set; }

    /// <summary>保级关系。</summary>
    public bool HoldFlag { get; set; }

    /// <summary>是否豪礼。</summary>
    public bool GiftFlag { get; set; }

    /// <summary>其余属性（晋级/保级条件等）。</summary>
    public Dictionary<string, string> Attributes { get; set; }
}

/// <summary>保存等级。</summary>
public sealed class SiteVipLevelSaveInput
{
    /// <summary>等级。</summary>
    public byte Level { get; set; }

    /// <summary>名称（可直接存 Base64，与旧版一致）。</summary>
    public string Name { get; set; }

    /// <summary>徽章。</summary>
    public string Badge { get; set; }

    /// <summary>豪礼说明。</summary>
    public string GiftDescription { get; set; }

    /// <summary>晋级关系。</summary>
    public bool UpgradeFlag { get; set; }

    /// <summary>自定义等级开关。</summary>
    public bool UpgradeClose { get; set; }

    /// <summary>保级关系。</summary>
    public bool HoldFlag { get; set; }

    /// <summary>是否豪礼。</summary>
    public bool GiftFlag { get; set; }

    /// <summary>其余属性。</summary>
    public Dictionary<string, string> Attributes { get; set; }
}

/// <summary>保存特权原文。</summary>
public sealed class SiteVipRightSaveInput
{
    /// <summary>特权 XML/JSON 原文。</summary>
    public string VIPRightSetting { get; set; }
}

/// <summary>升级日志筛选。</summary>
public sealed class SiteVipUpgradeLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>类型；0=全部。</summary>
    public int Type { get; set; }
}

/// <summary>升级日志分页。</summary>
public sealed class SiteVipUpgradeLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SiteVipUpgradeLogItemDto> Items { get; set; }
}

/// <summary>升级日志行。</summary>
public sealed class SiteVipUpgradeLogItemDto
{
    /// <summary>编号。</summary>
    public int LogID { get; set; }

    /// <summary>类型。</summary>
    public int Type { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>旧等级。</summary>
    public byte OldLevel { get; set; }

    /// <summary>新等级。</summary>
    public byte NewLevel { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }

    /// <summary>状态。</summary>
    public bool Status { get; set; }

    /// <summary>说明。</summary>
    public string Remark { get; set; }
}

/// <summary>批量设等级。</summary>
public sealed class SiteVipSetUsersLevelInput
{
    /// <summary>目标等级。</summary>
    public byte Level { get; set; }

    /// <summary>用户名，多行或逗号分隔。</summary>
    public string UserNames { get; set; }
}
