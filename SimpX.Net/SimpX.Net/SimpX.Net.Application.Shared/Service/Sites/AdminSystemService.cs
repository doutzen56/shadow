namespace SimpX.Net.Application.Shared;

/// <summary>
/// 后台系统辅助 Shared。
/// </summary>
public sealed partial class AdminSystemService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public AdminSystemService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    // --- merged from AdminSystemService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 billinfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BillinfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BillinfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 billlist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BilllistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BilllistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 functioninfo。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> FunctioninfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return FunctioninfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 kindupload。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> KinduploadAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return KinduploadCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 layupload。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> LayuploadAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return LayuploadCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatefunction。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatefunctionAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatefunctionCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatenews。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatenewsAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatenewsCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 upload。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UploadAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UploadCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：billinfo（JWT 包装，转调 Old）。</summary>
    private async Task<object?> BillinfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        var result = await BillinfoOldAsync(id, cancellationToken);
        return result.Success == 1 ? result.Info : throw Oops.Oh(result.Msg ?? "#编号错误#");
    }


    /// <summary>核心：billlist</summary>
    private async Task<object?> BilllistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, ReadInt(input, "PageIndex", "page"));
        var size = ReadInt(input, "PageSize", "size");
        if (size <= 0) size = 20;
        var result = await BilllistOldAsync(page, size, cancellationToken);
        return result.Info;
    }


    /// <summary>核心：functioninfo</summary>
    private async Task<object?> FunctioninfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var result = await FunctioninfoOldAsync(cancellationToken);
        return result.Info;
    }


    /// <summary>上传占位。</summary>
    private Task<object?> KinduploadCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        throw Oops.Oh("#请使用系统文件上传接口#");
    }


    /// <summary>上传占位。</summary>
    private Task<object?> LayuploadCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        throw Oops.Oh("#请使用系统文件上传接口#");
    }


    /// <summary>会话/系统辅助。对齐旧 updatefunction。</summary>
    private async Task<object?> UpdatefunctionCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var isOpen = input.TryGetValue("IsOpen", out var o) ? o?.ToString() : null;
        var value = input.TryGetValue("Value", out var v) ? v?.ToString() : null;
        var result = await UpdatefunctionOldAsync(isOpen, value, cancellationToken);
        if (result.Success != 1)
            throw Oops.Oh(result.Msg ?? "#操作失败#");
        return result.Info;
    }


    /// <summary>会话/系统辅助。对齐旧 updatenews。</summary>
    private async Task<object?> UpdatenewsCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, ReadInt(input, "PageIndex", "page"));
        var size = ReadInt(input, "PageSize", "size");
        if (size <= 0) size = 20;
        var result = await UpdatenewsOldAsync(page, size, cancellationToken);
        return result.Info;
    }


    /// <summary>上传占位。</summary>
    private Task<object?> UploadCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        throw Oops.Oh("#请使用系统文件上传接口#");
    }

    /// <summary>旧路径：更新通知分页（仅 Title/Content）。</summary>
    public async Task<OldApiResult> UpdatenewsOldAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _ = _site.RequireSiteId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        var query = _db.Queryable<SimpX.Net.Data.Entity.Systems.SystemUpdateNews>();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(n => n.CreateAt, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var list = rows.Select(n => new
        {
            Title = n.Title ?? string.Empty,
            Content = n.Content ?? string.Empty
        });
        return OldApiResult.Ok(string.Empty, OldApiShowResult.OfPage(list, total, page, pageSize));
    }

    /// <summary>旧路径：账单列表（本站暂无账单数据 → 空页）。</summary>
    public Task<OldApiResult> BilllistOldAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        return Task.FromResult(OldApiResult.Ok(
            string.Empty,
            OldApiShowResult.OfPage(Array.Empty<object>(), 0, page, pageSize)));
    }

    /// <summary>旧路径：账单详情。</summary>
    public Task<OldApiResult> BillinfoOldAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        // 本站无账单表数据；与旧站一样对无效 ID 回 #编号错误#
        return Task.FromResult(OldApiResult.Fail("#编号错误#"));
    }

    /// <summary>旧路径：功能开关目录（对齐 1002 抽检；checked 暂全 0）。</summary>
    public Task<OldApiResult> FunctioninfoOldAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        var function = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["RechargePopUp"] = new { name = "充值提醒", @checked = 0 },
            ["WithdrawPopUp"] = new { name = "提现提醒", @checked = 0 },
            ["Withdraw"] = new { name = "提现申请", @checked = 0 },
            ["WithdrawExecutor"] = new { name = "提现处理", @checked = 0 },
            ["Transfer"] = new { name = "入款审核", @checked = 0 },
            ["Event"] = new { name = "优惠申请", @checked = 0 },
            ["Rebate"] = new { name = "返水审核", @checked = 0 },
            ["Commission"] = new { name = "佣金审核", @checked = 0 },
            ["AgentApply"] = new { name = "代理申请", @checked = 0 },
            ["MoneyReview"] = new { name = "加减款审核", @checked = 0 },
            ["Exchange"] = new { name = "兑币审核", @checked = 0 },
            ["Kyc"] = new { name = "证件审核", @checked = 0 },
            ["Bank"] = new { name = "银行审核", @checked = 0 },
            ["Risk"] = new { name = "风险会员", @checked = 0 }
        };
        return Task.FromResult(OldApiResult.Ok(string.Empty, new { Function = function }));
    }

    /// <summary>旧路径：更新功能开关（只读壳阶段先回成功占位）。</summary>
    public Task<OldApiResult> UpdatefunctionOldAsync(
        string? isOpen,
        string? value,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(OldApiResult.Fail("#参数错误#"));
        _ = isOpen;
        return Task.FromResult(OldApiResult.Ok("#操作成功#"));
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
