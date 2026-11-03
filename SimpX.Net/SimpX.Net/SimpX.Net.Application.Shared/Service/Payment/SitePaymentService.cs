using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Data.Entity.Users;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 租户后台充值接口设定。对齐旧版 /admin/recharge/settinglist 与 savesetting 核心校验。
/// </summary>
public sealed partial class SitePaymentService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly RechargeOrderCheckService _check;

    /// <summary>
    /// 注入业务库与当前站。充值通道在 BetWin30 业务库，不能走主库 Sqlite。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    /// <param name="check">充值确认</param>
    public SitePaymentService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        RechargeOrderCheckService check)
    {
        _db = biz.Db;
        _site = site;
        _check = check ?? throw new ArgumentNullException(nameof(check));
    }

    /// <summary>
    /// 当前站未删除通道列表。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<SitePaymentListItemDto>> ListAsync(
        SitePaymentListInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var siteId = _site.RequireSiteId();

        var categories = await _db.Queryable<SitePaymentCategory>()
            .Where(c => c.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.ID, c => c.Name ?? "N/A");

        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);

        var query = _db.Queryable<SitePayment>()
            .Where(t => t.SiteID == siteId && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.Status) && int.TryParse(input.Status.Trim(), out var status))
            query = query.Where(t => t.Status == status);

        if (input.CateID != 0)
            query = query.Where(t => t.CateID == input.CateID);

        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(t => t.Name != null && t.Name.Contains(input.Name.Trim()));

        if (!string.IsNullOrWhiteSpace(input.Currency) && int.TryParse(input.Currency.Trim(), out var currency))
            query = query.Where(t => t.Currency == currency);

        var rows = await query
            .OrderBy(t => t.Sort, OrderByType.Desc)
            .ToListAsync(cancellationToken);

        return rows.Select(t =>
        {
            var idText = t.ID.ToString();
            var sysIdText = t.SysID > 0 ? t.SysID.ToString() : string.Empty;
            // 旧页：PayID 整段匹配；SysID 用裸 Contains（历史行为：17 会命中 1728）
            var groupStr = string.Join(",", groups
                .Where(g => PaymentCsvContains(g.Payment, idText)
                            || (!string.IsNullOrEmpty(sysIdText)
                                && !string.IsNullOrEmpty(g.Payment)
                                && g.Payment.Contains(sysIdText, StringComparison.Ordinal)))
                .Select(g => g.Name)
                .Where(n => !string.IsNullOrEmpty(n)));

            return new SitePaymentListItemDto
            {
                ID = t.ID,
                Category = categoryMap.TryGetValue(t.CateID, out var cn) ? cn : "N/A",
                GroupNames = groupStr,
                Name = t.Name ?? string.Empty,
                Currency = t.Currency,
                Remark = t.Remark ?? string.Empty,
                Payment = t.SysID.ToString(),
                MinMoney = t.MinMoney,
                MaxMoney = t.MaxMoney,
                MaxPay = t.MaxPay,
                Paid = 0m,
                Sort = t.Sort,
                Fee = t.Fee,
                Cost = t.Cost,
                Status = t.Status
            };
        }).ToList();
    }

    /// <summary>
    /// 详情；编号 0 返回空壳。
    /// </summary>
    /// <param name="id">通道编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>详情</returns>
    public async Task<SitePaymentDetailDto> InfoAsync(int id, CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        if (id <= 0)
        {
            return new SitePaymentDetailDto
            {
                Name = string.Empty,
                Remark = string.Empty,
                Setting = string.Empty,
                GroupIDs = []
            };
        }

        var rows = await _db.Queryable<SitePayment>()
            .Where(t => t.ID == id && t.SiteID == siteId && !t.IsDeleted)
            .Take(1)
            .ToListAsync(cancellationToken);
        var t = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
        var idText = t.ID.ToString();
        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        var groupIds = groups
            .Where(g => !string.IsNullOrEmpty(g.Payment) && g.Payment.Contains(idText))
            .Select(g => g.ID)
            .ToList();

        return new SitePaymentDetailDto
        {
            ID = t.ID,
            CateID = t.CateID,
            Name = t.Name ?? string.Empty,
            Remark = t.Remark ?? string.Empty,
            SysID = t.SysID,
            MinMoney = t.MinMoney,
            MaxMoney = t.MaxMoney,
            MaxPay = t.MaxPay,
            Status = t.Status,
            Currency = t.Currency,
            Sort = t.Sort,
            Fee = t.Fee,
            Cost = t.Cost,
            Setting = t.SettingString ?? string.Empty,
            IsNotify = t.IsNotify,
            GroupIDs = groupIds
        };
    }

    /// <summary>
    /// 保存。对齐旧 PaymentAgent.SavePaymentInfo 校验；并按分组改写 Payment 串。
    /// </summary>
    /// <param name="input">入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SitePaymentSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#请输入名称#");
        if (input.CateID == 0)
            throw Oops.Oh("#请选择分类#");
        if (input.MinMoney < 1 || input.MaxMoney < input.MinMoney)
            throw Oops.Oh("#最大最小金额错误#");
        if (input.SysID == 0)
            throw Oops.Oh("#没有设定充值渠道#");

        var siteId = _site.RequireSiteId();
        var cateOk = await _db.Queryable<SitePaymentCategory>()
            .AnyAsync(c => c.ID == input.CateID && c.SiteID == siteId, cancellationToken);
        if (!cateOk)
            throw Oops.Oh("#请选择分类#");

        int payId;
        int oldCateId = 0;
        if (input.ID == 0)
        {
            var entity = new SitePayment
            {
                SiteID = siteId,
                CateID = input.CateID,
                Name = input.Name.Trim(),
                Remark = input.Remark ?? string.Empty,
                SysID = input.SysID,
                MinMoney = input.MinMoney,
                MaxMoney = input.MaxMoney,
                MaxPay = input.MaxPay,
                Status = input.Status,
                Currency = input.Currency,
                Sort = input.Sort,
                Fee = input.Fee,
                Cost = input.Cost,
                SettingString = input.Setting ?? string.Empty,
                IsNotify = input.IsNotify,
                IsDeleted = false
            };
            payId = await _db.Insertable(entity).ExecuteReturnIdentityAsync(cancellationToken);
            await BumpCategoryCountAsync(siteId, input.CateID, 1, cancellationToken);
        }
        else
        {
            var rows = await _db.Queryable<SitePayment>()
                .Where(t => t.ID == input.ID && t.SiteID == siteId && !t.IsDeleted)
                .Take(1)
                .ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#编号错误#");
            oldCateId = row.CateID;
            row.CateID = input.CateID;
            row.Name = input.Name.Trim();
            row.Remark = input.Remark ?? string.Empty;
            row.SysID = input.SysID;
            row.MinMoney = input.MinMoney;
            row.MaxMoney = input.MaxMoney;
            row.MaxPay = input.MaxPay;
            row.Status = input.Status;
            row.Currency = input.Currency;
            row.Sort = input.Sort;
            row.Fee = input.Fee;
            row.Cost = input.Cost;
            row.SettingString = input.Setting ?? string.Empty;
            row.IsNotify = input.IsNotify;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            payId = row.ID;
            if (oldCateId != input.CateID)
            {
                await BumpCategoryCountAsync(siteId, input.CateID, 1, cancellationToken);
                await BumpCategoryCountAsync(siteId, oldCateId, -1, cancellationToken);
            }
        }

        if (input.GroupIDs != null)
            await SyncGroupPaymentsAsync(siteId, payId, input.GroupIDs, cancellationToken);
    }

    /// <summary>
    /// 分类通道数加减。
    /// </summary>
    private async Task BumpCategoryCountAsync(
        int siteId,
        int cateId,
        int delta,
        CancellationToken cancellationToken)
    {
        if (cateId <= 0 || delta == 0)
            return;
        var cols = await _db.Queryable<SitePaymentCategory>()
            .Where(c => c.ID == cateId && c.SiteID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var col = cols.FirstOrDefault();
        if (col == null)
            return;
        col.Count = Math.Max(0, col.Count + delta);
        await _db.Updateable(col).UpdateColumns(c => new { c.Count })
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 分组 Payment 是否包含通道号。兼容 CSV / JSON 数组，按整段数字匹配避免 172 误伤 1729。
    /// </summary>
    private static bool PaymentCsvContains(string? payment, string idText)
    {
        if (string.IsNullOrWhiteSpace(payment) || string.IsNullOrWhiteSpace(idText))
            return false;
        if (payment
            .Split([',', '|', ';', '[', ']', '"', '\'', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(p => string.Equals(p, idText, StringComparison.OrdinalIgnoreCase)))
            return true;
        // 回退：整数字边界 Contains（旧页常用）
        return System.Text.RegularExpressions.Regex.IsMatch(
            payment,
            $@"(?<!\d){System.Text.RegularExpressions.Regex.Escape(idText)}(?!\d)");
    }

    /// <summary>
    /// 按勾选分组重写各分组 Payment 串（多钱包：逗号分隔通道号）。
    /// </summary>
    private async Task SyncGroupPaymentsAsync(
        int siteId,
        int payId,
        List<int> groupIds,
        CancellationToken cancellationToken)
    {
        var idText = payId.ToString();
        var selected = new HashSet<int>(groupIds ?? []);
        var groups = await _db.Queryable<UserGroup>()
            .Where(g => g.SiteID == siteId)
            .ToListAsync(cancellationToken);
        foreach (var g in groups)
        {
            var parts = (g.Payment ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => p != idText)
                .ToList();
            if (selected.Contains(g.ID))
                parts.Add(idText);
            g.Payment = string.Join(",", parts);
            await _db.Updateable(g).UpdateColumns(x => new { x.Payment })
                .ExecuteCommandAsync(cancellationToken);
        }
    }

    // --- merged from SitePaymentService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 checkorder。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> CheckorderAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return CheckorderCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 deletecategory。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeletecategoryAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeletecategoryCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 deletepayment。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeletepaymentAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeletepaymentCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getpaymentsetting。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetpaymentsettingAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetpaymentsettingCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 getsyspaymentgroup。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> GetsyspaymentgroupAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetsyspaymentgroupCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 open。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> OpenAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return OpenCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 paymenttest。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PaymenttestAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PaymenttestCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 stop。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> StopAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return StopCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatecategorycount。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatecategorycountAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatecategorycountCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updateisrecommend。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdateisrecommendAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdateisrecommendCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 updatepaymentstatus。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> UpdatepaymentstatusAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return UpdatepaymentstatusCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：checkorder</summary>
    private async Task<object?> CheckorderCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = input.TryGetValue("ID", out var idv) ? idv?.ToString()
            : input.TryGetValue("Id", out var id2) ? id2?.ToString()
            : input.TryGetValue("OrderID", out var oid) ? oid?.ToString()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(id)) throw Oops.Oh("#参数错误#");
        _ = decimal.TryParse(
            input.TryGetValue("Paid", out var p) ? p?.ToString()
            : input.TryGetValue("Money", out var m) ? m?.ToString() : "0",
            out var paid);
        _ = decimal.TryParse(
            input.TryGetValue("Fee", out var f) ? f?.ToString() : "0",
            out var fee);
        await _check.CheckAsync(
            new RechargeOrderCheckInput { ID = id.Trim(), Paid = paid, Fee = fee },
            adminId: 0,
            cancellationToken: cancellationToken);
        return null;
    }


    /// <summary>核心：deletecategory</summary>
    private async Task<object?> DeletecategoryCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var used = await _db.Queryable<SitePayment>()
            .AnyAsync(p => p.SiteID == siteId && p.CateID == id, cancellationToken);
        if (used)
            throw Oops.Oh("#分类下还有通道#");
        var n = await _db.Deleteable<SitePaymentCategory>()
            .Where(c => c.SiteID == siteId && c.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#记录不存在#");
        return null;
    }


    /// <summary>核心：deletepayment</summary>
    private async Task<object?> DeletepaymentCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<SitePayment>()
            .SetColumns(p => new SitePayment { IsDeleted = true })
            .Where(p => p.SiteID == siteId && p.ID == id && !p.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#编号错误#");
        return null;
    }


    /// <summary>核心：getpaymentsetting —— 通道详情壳。</summary>
    private async Task<object?> GetpaymentsettingCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id", "PayID");
        return await InfoAsync(id, cancellationToken);
    }


    /// <summary>核心：getsyspaymentgroup —— 无系统组表时返回空。</summary>
    private Task<object?> GetsyspaymentgroupCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        _ = _site.RequireSiteId();
        return Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：open —— Status=1。</summary>
    private Task<object?> OpenCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken) =>
        SetStatusBatchAsync(input, status: 1, cancellationToken);


    /// <summary>核心：paymenttest</summary>
    private Task<object?> PaymenttestCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        throw Oops.Oh("#功能尚未接通#");
    }


    /// <summary>核心：stop —— Status=0。</summary>
    private Task<object?> StopCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken) =>
        SetStatusBatchAsync(input, status: 0, cancellationToken);


    /// <summary>核心：updatecategorycount —— 重算分类下渠道数。</summary>
    private async Task<object?> UpdatecategorycountCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var cats = await _db.Queryable<SitePaymentCategory>()
            .Where(c => c.SiteID == siteId)
            .ToListAsync(cancellationToken);
        foreach (var cat in cats)
        {
            var count = await _db.Queryable<SitePayment>()
                .Where(p => p.SiteID == siteId && p.CateID == cat.ID && !p.IsDeleted)
                .CountAsync(cancellationToken);
            cat.Count = count;
            await _db.Updateable(cat).UpdateColumns(c => c.Count).ExecuteCommandAsync(cancellationToken);
        }

        return null;
    }


    /// <summary>核心：updateisrecommend —— 分类推荐位。</summary>
    private async Task<object?> UpdateisrecommendCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id", "CateID");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var isRec = ReadInt(input, "IsRecommend") == 1
            || string.Equals(ReadString(input, "IsRecommend"), "true", StringComparison.OrdinalIgnoreCase);
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<SitePaymentCategory>()
            .SetColumns(c => new SitePaymentCategory { IsRecommend = isRec })
            .Where(c => c.SiteID == siteId && c.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#编号错误#");
        return null;
    }


    /// <summary>核心：updatepaymentstatus</summary>
    private async Task<object?> UpdatepaymentstatusCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        var status = ReadInt(input, "Status");
        var siteId = _site.RequireSiteId();
        var n = await _db.Updateable<SitePayment>()
            .SetColumns(p => new SitePayment { Status = status })
            .Where(p => p.SiteID == siteId && p.ID == id && !p.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
        if (n <= 0) throw Oops.Oh("#编号错误#");
        return null;
    }

    private async Task<object?> SetStatusBatchAsync(
        Dictionary<string, object?> input,
        int status,
        CancellationToken cancellationToken)
    {
        var raw = ReadString(input, "ID", "Id");
        var ids = raw.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            var single = ReadInt(input, "ID", "Id");
            if (single > 0) ids = [single];
        }

        if (ids.Length == 0) throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Updateable<SitePayment>()
            .SetColumns(p => new SitePayment { Status = status })
            .Where(p => p.SiteID == siteId && ids.Contains(p.ID) && !p.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
        return null;
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v.ToString();
            if (!string.IsNullOrEmpty(s)) return s!;
        }

        return string.Empty;
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
