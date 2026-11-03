using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 调查问卷与答卷记录。
/// </summary>
public sealed partial class SurveyAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public SurveyAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>问卷列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SurveyItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteSurvery>()
            .Where(s => s.SiteID == siteId).OrderBy(s => s.ID, OrderByType.Desc)
            .ToListAsync(cancellationToken);
        return rows.Select(s => new SurveyItemDto
        {
            ID = s.ID,
            Title = s.Title,
            Description = s.Description,
            IsOpen = s.IsOpen,
            AutoPay = s.AutoPay,
            Money = s.Money,
            WashFlow = s.WashFlow,
            Questions = s.Questions,
            Answer = s.Answer
        }).ToList();
    }

    /// <summary>保存问卷。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveAsync(SurveySaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Title))
            throw Oops.Oh("#标题错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<SiteSurvery>()
                .Where(s => s.SiteID == siteId && s.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            Fill(row, input);
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        var neu = new SiteSurvery { SiteID = siteId };
        Fill(neu, input);
        return await _db.Insertable(neu).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>答卷分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<SurveyLogPageOutput> PageLogsAsync(
        SurveyLogPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new SurveyLogPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<SiteSurveryUser>().Where(u => u.SiteID == siteId);
        if (input.SurveryID > 0)
            query = query.Where(u => u.SurveryID == input.SurveryID);
        if (input.UserID > 0)
            query = query.Where(u => u.UserID == input.UserID);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(u => u.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.UserID).Distinct().ToList();
        var nameMap = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var names = await _db.Queryable<User>()
                .Where(u => u.SiteID == siteId && ids.Contains(u.ID))
                .Select(u => new { u.ID, u.UserName }).ToListAsync(cancellationToken);
            foreach (var n in names)
                nameMap[n.ID] = n.UserName;
        }

        return new SurveyLogPageOutput
        {
            Total = total,
            Items = rows.Select(u => new SurveyLogItemDto
            {
                ID = u.ID,
                SurveryID = u.SurveryID,
                UserID = u.UserID,
                UserName = nameMap.TryGetValue(u.UserID, out var un) ? un : string.Empty,
                IsPay = u.IsPay,
                Answer = u.Answer,
                CreateAt = u.CreateAt
            }).ToList()
        };
    }

    private static void Fill(SiteSurvery row, SurveySaveInput input)
    {
        row.Title = input.Title.Trim();
        row.Description = input.Description ?? string.Empty;
        row.Questions = input.Questions ?? string.Empty;
        row.Answer = input.Answer ?? string.Empty;
        row.IsOpen = input.IsOpen;
        row.AutoPay = input.AutoPay;
        row.Money = input.Money;
        row.WashFlow = input.WashFlow;
    }

    // --- merged from SurveyAdminService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 answerinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AnswerinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AnswerinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 pay。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PayAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PayCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 surveryinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SurveryinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SurveryinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：answerinfo —— 答卷详情。</summary>
    private async Task<object?> AnswerinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteSurveryUser>()
            .Where(u => u.SiteID == siteId && u.ID == id).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        var users = await _db.Queryable<User>()
            .Where(u => u.SiteID == siteId && u.ID == row.UserID).Take(1).ToListAsync(cancellationToken);
        return new
        {
            row.ID,
            row.SurveryID,
            row.UserID,
            UserName = users.FirstOrDefault()?.UserName ?? string.Empty,
            row.IsPay,
            row.Answer,
            row.CreateAt
        };
    }


    /// <summary>核心：pay —— 标记答卷已发放。</summary>
    private async Task<object?> PayCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<SiteSurveryUser>()
            .SetColumns(u => u.IsPay == true)
            .Where(u => u.SiteID == siteId && u.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：surveryinfo —— 问卷详情。</summary>
    private async Task<object?> SurveryinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<SiteSurvery>()
            .Where(s => s.SiteID == siteId && s.ID == id).Take(1).ToListAsync(cancellationToken);
        var s = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return new SurveyItemDto
        {
            ID = s.ID,
            Title = s.Title,
            Description = s.Description,
            IsOpen = s.IsOpen,
            AutoPay = s.AutoPay,
            Money = s.Money,
            WashFlow = s.WashFlow,
            Questions = s.Questions,
            Answer = s.Answer
        };
    }


    private static int ReadInt(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var n)) return n;
        }
        return 0;
    }
}

/// <summary>问卷行。</summary>
public sealed class SurveyItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>描述。</summary>
    public string Description { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>自动发奖。</summary>
    public bool AutoPay { get; set; }

    /// <summary>奖金。</summary>
    public decimal Money { get; set; }

    /// <summary>流水。</summary>
    public int WashFlow { get; set; }

    /// <summary>题目原文。</summary>
    public string Questions { get; set; }

    /// <summary>答案原文。</summary>
    public string Answer { get; set; }
}

/// <summary>保存问卷。</summary>
public sealed class SurveySaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>标题。</summary>
    public string Title { get; set; }

    /// <summary>描述。</summary>
    public string Description { get; set; }

    /// <summary>题目原文。</summary>
    public string Questions { get; set; }

    /// <summary>答案原文。</summary>
    public string Answer { get; set; }

    /// <summary>是否开放。</summary>
    public bool IsOpen { get; set; }

    /// <summary>自动发奖。</summary>
    public bool AutoPay { get; set; }

    /// <summary>奖金。</summary>
    public decimal Money { get; set; }

    /// <summary>流水。</summary>
    public int WashFlow { get; set; }
}

/// <summary>答卷筛选。</summary>
public sealed class SurveyLogPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>问卷编号。</summary>
    public int SurveryID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }
}

/// <summary>答卷分页。</summary>
public sealed class SurveyLogPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<SurveyLogItemDto> Items { get; set; }
}

/// <summary>答卷行。</summary>
public sealed class SurveyLogItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>问卷。</summary>
    public int SurveryID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>是否已发奖。</summary>
    public bool IsPay { get; set; }

    /// <summary>答案。</summary>
    public string Answer { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}
