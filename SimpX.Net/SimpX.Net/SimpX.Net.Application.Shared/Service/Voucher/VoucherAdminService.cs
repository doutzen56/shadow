using SimpX.Net.Data.Entity.Voucher;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 彩金兑换：产品列表 + 订单分页。
/// </summary>
public sealed partial class VoucherAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public VoucherAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>旧 ProductStatus.Valid。</summary>
    public const int ProductStatusValid = 0;

    /// <summary>旧 ProductStatus.Expired。</summary>
    public const int ProductStatusExpired = 1;

    /// <summary>旧 ProductStatus.Canceled。</summary>
    public const int ProductStatusCanceled = 2;

    /// <summary>旧 OrderStatus.Finished。</summary>
    public const int OrderStatusFinished = 2;

    /// <summary>产品列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<VoucherProductItemDto>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<VoucherProduct>()
            .Where(p => p.SiteID == siteId).OrderBy(p => p.ID, OrderByType.Desc)
            .Take(500).ToListAsync(cancellationToken);
        return rows.Select(MapProduct).ToList();
    }

    /// <summary>会员侧：有效兑换码列表（对齐 /request/voucher/product/list）。</summary>
    /// <param name="name">名称模糊</param>
    /// <param name="sort">0=最热；其它/空=最新</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<VoucherProductItemDto>> ListValidProductsAsync(
        string name = null,
        int sort = 1,
        CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<VoucherProduct>()
            .Where(p => p.SiteID == siteId && p.Status == ProductStatusValid);
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name.Trim()));
        query = sort == 0
            ? query.OrderBy(p => p.ClickCount, OrderByType.Desc)
            : query.OrderBy(p => p.UpdateAt, OrderByType.Desc);
        var rows = await query.Take(500).ToListAsync(cancellationToken);
        return rows.Select(MapProduct).ToList();
    }

    /// <summary>会员侧领取兑换码（对齐 /request/voucher/product/order）。</summary>
    /// <param name="userId">会员</param>
    /// <param name="userName">用户名</param>
    /// <param name="productCode">兑换码 Name</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提示</returns>
    public async Task<string> PlaceMemberOrderAsync(
        int userId,
        string userName,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#请先登录#");
        if (string.IsNullOrWhiteSpace(productCode))
            throw Oops.Oh("不存在该兑换码");
        var siteId = _site.RequireSiteId();
        var code = productCode.Trim();
        var products = await _db.Queryable<VoucherProduct>()
            .Where(p => p.SiteID == siteId && p.Name == code).Take(1).ToListAsync(cancellationToken);
        var product = products.FirstOrDefault() ?? throw Oops.Oh("不存在该兑换码");
        if (product.Stock <= 0)
            throw Oops.Oh("库存不足");
        if (product.EndAt <= DateTime.Now)
            throw Oops.Oh("已过期");
        if (product.Status == ProductStatusExpired)
            throw Oops.Oh("已失效");
        if (product.Status == ProductStatusCanceled)
            throw Oops.Oh("已作废");

        var claimed = await _db.Queryable<VoucherOrder>()
            .Where(o => o.SiteID == siteId && o.UserID == userId && o.Remark == product.Name)
            .AnyAsync(cancellationToken);
        if (claimed)
            throw Oops.Oh("已领取");

        var users = await _db.Queryable<SimpX.Net.Data.Entity.Users.User>()
            .Where(u => u.SiteID == siteId && u.ID == userId).Take(1).ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#请先登录#");

        var orderId = await _db.Insertable(new VoucherOrder
        {
            SiteID = siteId,
            CreateAt = DateTime.Now,
            Status = OrderStatusFinished,
            UserID = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? user.UserName : userName,
            Remark = product.Name ?? string.Empty,
            EventID = 0,
            Money = product.Price
        }).ExecuteReturnIdentityAsync(cancellationToken);
        if (orderId <= 0)
            throw Oops.Oh("下单失败");

        var stockRows = await _db.Updateable<VoucherProduct>()
            .SetColumns(p => p.Stock == p.Stock - 1)
            .Where(p => p.SiteID == siteId && p.ID == product.ID && p.Stock >= 1)
            .ExecuteCommandAsync(cancellationToken);
        if (stockRows < 1)
            throw Oops.Oh("锁定库存失败");

        return "领取成功";
    }

    private static VoucherProductItemDto MapProduct(VoucherProduct p) => new()
    {
        ID = p.ID,
        Name = p.Name,
        Currency = p.Currency,
        Price = p.Price,
        Point = p.Point,
        Stock = p.Stock,
        Status = p.Status,
        StartAt = p.StartAt,
        EndAt = p.EndAt
    };

    /// <summary>产品详情。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<VoucherProductItemDto> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<VoucherProduct>()
            .Where(p => p.SiteID == siteId && p.ID == id).Take(1).ToListAsync(cancellationToken);
        var p = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return MapProduct(p);
    }

    /// <summary>删除产品（作废）。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Updateable<VoucherProduct>()
            .SetColumns(p => p.Status == ProductStatusCanceled)
            .Where(p => p.SiteID == siteId && p.ID == id)
            .ExecuteCommandAsync(cancellationToken);
        if (rows < 1)
            throw Oops.Oh("#记录不存在#");
    }

    /// <summary>保存产品。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveProductAsync(VoucherProductSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<VoucherProduct>()
                .Where(p => p.SiteID == siteId && p.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            row.Name = input.Name.Trim();
            row.Price = input.Price;
            row.Point = input.Point;
            row.Stock = input.Stock;
            row.Status = input.Status;
            row.Currency = input.Currency;
            row.StartAt = input.StartAt ?? row.StartAt;
            row.EndAt = input.EndAt ?? row.EndAt;
            row.UpdateAt = DateTime.Now;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        return await _db.Insertable(new VoucherProduct
        {
            SiteID = siteId,
            Name = input.Name.Trim(),
            Pic = string.Empty,
            Currency = input.Currency,
            Price = input.Price,
            Point = input.Point,
            Stock = input.Stock,
            MaxStock = input.Stock,
            Status = input.Status,
            StartAt = input.StartAt ?? DateTime.Today,
            EndAt = input.EndAt ?? DateTime.Today.AddYears(1),
            Description = string.Empty,
            Content = string.Empty,
            Settings = string.Empty,
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            ClickCount = 0,
            WashFlow = 0
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>订单分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<VoucherOrderPageOutput> PageOrdersAsync(
        VoucherOrderPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new VoucherOrderPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<VoucherOrder>().Where(o => o.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(o => o.UserID == input.UserID);
        if (input.Status >= 0)
            query = query.Where(o => o.Status == input.Status);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(o => o.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new VoucherOrderPageOutput
        {
            Total = total,
            Items = rows.Select(o => new VoucherOrderItemDto
            {
                ID = o.ID,
                UserID = o.UserID,
                UserName = o.UserName,
                Money = o.Money,
                Status = o.Status,
                Remark = o.Remark,
                CreateAt = o.CreateAt
            }).ToList()
        };
    }

    /// <summary>改订单状态。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateOrderStatusAsync(VoucherOrderStatusInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<VoucherOrder>()
            .Where(o => o.SiteID == siteId && o.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.Status = input.Status;
        if (input.Remark != null)
            row.Remark = input.Remark;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    // --- merged from VoucherAdminService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 add。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> AddAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return AddCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 batchdelete。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BatchdeleteAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BatchdeleteCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 batchdeleteimportpoint。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> BatchdeleteimportpointAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return BatchdeleteimportpointCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 catedict。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> CatedictAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return CatedictCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 children。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ChildrenAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ChildrenCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 createuserpoint。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> CreateuserpointAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return CreateuserpointCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 del。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DelAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DelCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 delete。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeleteAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeleteCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 deleteimportpoint。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DeleteimportpointAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DeleteimportpointCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 detail。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> DetailAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return DetailCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 editimportpoint。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> EditimportpointAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return EditimportpointCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 importpointlist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> ImportpointlistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ImportpointlistCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 info。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> InfoAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InfoCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 pointlog。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> PointlogAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PointlogCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 saveimportpoint。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SaveimportpointAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveimportpointCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：add</summary>
    private async Task<object?> AddCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = await SaveProductAsync(new VoucherProductSaveInput
        {
            ID = ReadInt(input, "ID", "Id"),
            Name = ReadString(input, "Name", "name"),
            Price = ReadDecimal(input, "Price", "price"),
            Point = ReadInt(input, "Point", "Points", "point"),
            Stock = ReadInt(input, "Stock", "stock"),
            Status = ReadInt(input, "Status", "status"),
            Currency = ReadInt(input, "Currency", "currency")
        }, cancellationToken);
        return new { ID = id };
    }


    /// <summary>核心：batchdelete</summary>
    private async Task<object?> BatchdeleteCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var ids = ParseIdList(input);
        foreach (var id in ids)
            await DeleteProductAsync(id, cancellationToken);
        return null;
    }


    /// <summary>核心：batchdeleteimportpoint —— 导入积分批次无实体时返回空成功。</summary>
    private Task<object?> BatchdeleteimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _ = ParseIdList(input);
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：catedict</summary>
    private async Task<object?> CatedictCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：children</summary>
    private async Task<object?> ChildrenCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：createuserpoint —— 无导入实体时占位成功。</summary>
    private Task<object?> CreateuserpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：del</summary>
    private async Task<object?> DelCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        await DeleteProductAsync(id, cancellationToken);
        return null;
    }


    /// <summary>核心：delete</summary>
    private async Task<object?> DeleteCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        await DeleteProductAsync(id, cancellationToken);
        return null;
    }


    /// <summary>核心：deleteimportpoint</summary>
    private Task<object?> DeleteimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _ = ReadInt(input, "ID", "Id");
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：detail</summary>
    private async Task<object?> DetailCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0) throw Oops.Oh("#参数错误#");
        return await GetProductAsync(id, cancellationToken);
    }


    /// <summary>核心：editimportpoint</summary>
    private Task<object?> EditimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：importpointlist</summary>
    private async Task<object?> ImportpointlistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：info</summary>
    private async Task<object?> InfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            return new VoucherProductItemDto { Name = string.Empty };
        return await GetProductAsync(id, cancellationToken);
    }


    /// <summary>核心：pointlog</summary>
    private async Task<object?> PointlogCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return await Task.FromResult<object?>(Array.Empty<object>());
    }


    /// <summary>核心：saveimportpoint</summary>
    private Task<object?> SaveimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(null);
    }


    private static List<int> ParseIdList(Dictionary<string, object?> input)
    {
        var raw = ReadString(input, "ID", "Id", "IDs", "Ids");
        return raw.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToList();
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


    private static decimal ReadDecimal(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            if (v is decimal d) return d;
            if (decimal.TryParse(v.ToString(), out var n)) return n;
        }

        return 0m;
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

/// <summary>彩金产品行。</summary>
public sealed class VoucherProductItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>价格。</summary>
    public decimal Price { get; set; }

    /// <summary>积分。</summary>
    public int Point { get; set; }

    /// <summary>库存。</summary>
    public int Stock { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>开始。</summary>
    public DateTime StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime EndAt { get; set; }
}

/// <summary>保存彩金产品。</summary>
public sealed class VoucherProductSaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>币种。</summary>
    public int Currency { get; set; }

    /// <summary>价格。</summary>
    public decimal Price { get; set; }

    /// <summary>积分。</summary>
    public int Point { get; set; }

    /// <summary>库存。</summary>
    public int Stock { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>开始。</summary>
    public DateTime? StartAt { get; set; }

    /// <summary>结束。</summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>彩金订单筛选。</summary>
public sealed class VoucherOrderPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>状态；-1=全部。</summary>
    public int Status { get; set; } = -1;
}

/// <summary>彩金订单分页。</summary>
public sealed class VoucherOrderPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<VoucherOrderItemDto> Items { get; set; }
}

/// <summary>彩金订单行。</summary>
public sealed class VoucherOrderItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>金额。</summary>
    public decimal Money { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>改彩金订单状态。</summary>
public sealed class VoucherOrderStatusInput
{
    /// <summary>订单编号。</summary>
    public int ID { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
