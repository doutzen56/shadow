using System.Text;
using SimpX.Net.Data.Entity.Reports;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 报表下载文件。对齐旧 site/file report + downfile。
/// </summary>
public sealed class SiteFileDownloadService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly IFileStorage _files;

    public SiteFileDownloadService(IBizDbAccessor biz, ICurrentSiteAccessor site, IFileStorage files)
    {
        _db = biz.Db;
        _site = site;
        _files = files;
    }

    /// <summary>最近报表文件列表（最多 30）。</summary>
    public async Task<object> ReportListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteFileDownload>()
            .Where(f => f.SiteID == siteId)
            .OrderBy(f => f.CreateAt, OrderByType.Desc)
            .Take(30)
            .ToListAsync(cancellationToken);
        return rows.Select(t => new
        {
            t.ID,
            t.CreateAt,
            Size = FormatSize(t.FileSize),
            t.FilePath,
            Name = t.FileName,
            FileName = Path.GetFileName(t.FilePath ?? string.Empty),
            t.Frequency
        }).ToList();
    }

    /// <summary>下载计数 +1。对齐旧 UpdateStatus。</summary>
    public async Task<object?> DownfileAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("~发生错误~");
        var siteId = _site.RequireSiteId();
        var affected = await _db.Updateable<SiteFileDownload>()
            .SetColumns(f => f.Frequency == f.Frequency + 1)
            .Where(f => f.SiteID == siteId && f.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (affected != 1)
            throw Oops.Oh("~发生错误~");
        return true;
    }

    /// <summary>
    /// 用户统计导出 CSV 并写入 site_FileDownload。对齐旧 exceluserstatistics（精简：按日投注汇总）。
    /// </summary>
    public async Task<object?> ExcelUserStatisticsAsync(
        DateTime? startAt,
        DateTime? endAt,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var start = (startAt ?? DateTime.Today).Date;
        var end = (endAt ?? DateTime.Now);
        if (end < start) end = start.AddDays(1).AddTicks(-1);

        var games = await _db.Queryable<UserDateGame>()
            .Where(g => g.SiteID == siteId && g.Date >= start && g.Date <= end)
            .ToListAsync(cancellationToken);
        var byUser = games
            .GroupBy(g => g.UserID)
            .Select(g => new
            {
                UserID = g.Key,
                BetMoney = g.Sum(x => x.BetMoney),
                Money = g.Sum(x => x.Money)
            })
            .OrderBy(x => x.UserID)
            .Take(5000)
            .ToList();

        var userIds = byUser.Select(x => x.UserID).ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && userIds.Contains(u.ID))
                .ToListAsync(cancellationToken);
        var nameMap = users.ToDictionary(u => u.ID, u => u.UserName ?? string.Empty);

        var parents = userIds.Count == 0
            ? new List<UserDepth>()
            : await _db.Queryable<UserDepth>()
                .Where(d => d.SiteID == siteId && userIds.Contains(d.ChildID) && d.UserID != d.ChildID)
                .ToListAsync(cancellationToken);
        var parentIds = parents.Select(p => p.UserID).Distinct().ToList();
        var parentUsers = parentIds.Count == 0
            ? new List<User>()
            : await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && parentIds.Contains(u.ID) && u.AgentID == 0)
                .ToListAsync(cancellationToken);
        var topAgentByChild = new Dictionary<int, string>();
        foreach (var p in parents)
        {
            if (topAgentByChild.ContainsKey(p.ChildID)) continue;
            var name = parentUsers.FirstOrDefault(u => u.ID == p.UserID)?.UserName;
            if (!string.IsNullOrEmpty(name))
                topAgentByChild[p.ChildID] = name;
        }

        var sb = new StringBuilder();
        sb.AppendLine("UserID,UserName,总代,BetMoney,Money");
        foreach (var row in byUser)
        {
            var un = nameMap.TryGetValue(row.UserID, out var n) ? n : string.Empty;
            var agent = topAgentByChild.TryGetValue(row.UserID, out var a) ? a : string.Empty;
            sb.Append(row.UserID).Append(',')
                .Append(Csv(un)).Append(',')
                .Append(Csv(agent)).Append(',')
                .Append(row.BetMoney.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Money.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"UserStatistics[{start:yyyy-MM-dd}--{end:yyyy-MM-dd}].csv";
        var key = $"reports/{siteId}/{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.csv";
        await using (var ms = new MemoryStream(bytes))
            await _files.SaveAsync(key, ms, cancellationToken);

        await _db.Insertable(new SiteFileDownload
        {
            SiteID = siteId,
            FileName = fileName,
            FilePath = "/" + _files.GetUrl(key).TrimStart('/'),
            FileSize = bytes.Length > int.MaxValue ? int.MaxValue : bytes.Length,
            Frequency = 0,
            CreateAt = DateTime.Now
        }).ExecuteCommandAsync(cancellationToken);

        return true;
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.##} KB";
        return $"{bytes / (1024.0 * 1024):0.##} MB";
    }
}
