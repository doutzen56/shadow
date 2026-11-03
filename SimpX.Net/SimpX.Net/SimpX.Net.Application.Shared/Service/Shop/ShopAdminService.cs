using SimpX.Net.Data.Entity.Shop;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 积分商城后台：分类 / 商品 / 订单只读与维护。
/// </summary>
public sealed partial class ShopAdminService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    /// <param name="biz">业务库</param>
    /// <param name="site">当前站</param>
    public ShopAdminService(IBizDbAccessor biz, ICurrentSiteAccessor site)
    {
        _db = biz.Db;
        _site = site;
    }

    /// <summary>分类列表。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行列表</returns>
    public async Task<List<ShopCategoryItemDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<ShopProductCategory>()
            .Where(c => c.SiteID == siteId)
            .OrderBy(c => c.Sort).OrderBy(c => c.ID)
            .ToListAsync(cancellationToken);
        return rows.Select(c => new ShopCategoryItemDto
        {
            ID = c.ID,
            ParentID = c.ParentID,
            Name = c.Name,
            Sort = c.Sort,
            CreateAt = c.CreateAt
        }).ToList();
    }

    /// <summary>保存分类。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveCategoryAsync(ShopCategorySaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<ShopProductCategory>()
                .Where(c => c.SiteID == siteId && c.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            row.Name = input.Name.Trim();
            row.ParentID = input.ParentID;
            row.Sort = input.Sort;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        return await _db.Insertable(new ShopProductCategory
        {
            SiteID = siteId,
            Name = input.Name.Trim(),
            ParentID = input.ParentID,
            Sort = input.Sort,
            CreateAt = DateTime.Now
        }).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>删除分类。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<ShopProductCategory>()
            .Where(c => c.SiteID == siteId && c.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>旧 ProductStatus.Online。</summary>
    public const int ProductStatusOnline = 0;

    /// <summary>旧 OrderStatus.None / Finished。</summary>
    public const int OrderStatusNone = 0;

    /// <summary>旧 OrderStatus.Finished。</summary>
    public const int OrderStatusFinished = 2;

    /// <summary>商品分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<ShopProductPageOutput> PageProductsAsync(
        ShopProductPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new ShopProductPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<ShopProduct>().Where(p => p.SiteID == siteId);
        if (input.TypeID > 0)
            query = query.Where(p => p.TypeID == input.TypeID);
        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(p => p.Name.Contains(input.Name.Trim()));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(p => p.Sort).OrderBy(p => p.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new ShopProductPageOutput
        {
            Total = total,
            Items = rows.Select(MapProduct).ToList()
        };
    }

    /// <summary>会员侧：上架商品列表（对齐 /request/shop/product/list）。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>商品行</returns>
    public async Task<List<ShopProductItemDto>> ListOnlineProductsAsync(
        ShopProductPageInput input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new ShopProductPageInput();
        var siteId = _site.RequireSiteId();
        var query = _db.Queryable<ShopProduct>()
            .Where(p => p.SiteID == siteId && p.Status == ProductStatusOnline);
        if (input.TypeID > 0)
            query = query.Where(p => p.TypeID == input.TypeID);
        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(p => p.Name.Contains(input.Name.Trim()));
        var rows = await query.OrderBy(p => p.Sort, OrderByType.Desc)
            .OrderBy(p => p.CreateAt, OrderByType.Desc)
            .Take(500)
            .ToListAsync(cancellationToken);
        return rows.Select(MapProduct).ToList();
    }

    /// <summary>会员侧：一级分类字典（对齐 /request/shop/product/catedict）。</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ID→名称</returns>
    public async Task<Dictionary<string, string>> CateDictAsync(CancellationToken cancellationToken = default)
    {
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<ShopProductCategory>()
            .Where(c => c.SiteID == siteId && c.ParentID == 0)
            .OrderBy(c => c.Sort).OrderBy(c => c.ID)
            .ToListAsync(cancellationToken);
        var dict = new Dictionary<string, string>();
        foreach (var c in rows)
            dict[c.ID.ToString()] = c.Name ?? string.Empty;
        return dict;
    }

    /// <summary>会员侧下单（对齐 /request/shop/product/order；扣积分 + 锁库存）。</summary>
    /// <param name="userId">会员</param>
    /// <param name="userName">用户名</param>
    /// <param name="productId">商品</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提示文案</returns>
    public async Task<string> PlaceMemberOrderAsync(
        int userId,
        string userName,
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw Oops.Oh("#请登录再操作#");
        if (productId <= 0)
            throw Oops.Oh("#该商品不存在#");
        var siteId = _site.RequireSiteId();
        var products = await _db.Queryable<ShopProduct>()
            .Where(p => p.SiteID == siteId && p.ID == productId).Take(1).ToListAsync(cancellationToken);
        var product = products.FirstOrDefault() ?? throw Oops.Oh("#该商品不存在#");
        if (product.Status != ProductStatusOnline)
            throw Oops.Oh("#该商品不存在#");
        if (product.Stock <= 0)
            throw Oops.Oh("#库存不足#");

        var users = await _db.Queryable<SimpX.Net.Data.Entity.Users.User>()
            .Where(u => u.SiteID == siteId && u.ID == userId).Take(1).ToListAsync(cancellationToken);
        var user = users.FirstOrDefault() ?? throw Oops.Oh("#请登录再操作#");
        var points = Math.Abs(product.Points);
        if (points > 0 && user.Coin < points)
            throw Oops.Oh("#积分不足#");

        var order = new ShopOrder
        {
            SiteID = siteId,
            CreateAt = DateTime.Now,
            Status = product.IsAuto ? OrderStatusFinished : OrderStatusNone,
            UserID = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? user.UserName : userName,
            Remark = product.IsAuto ? "积分商城兑换通知" : string.Empty,
            EventID = 0,
            Money = 0,
            ProductName = product.Name ?? string.Empty,
            ProductId = product.ID
        };
        var orderId = await _db.Insertable(order).ExecuteReturnIdentityAsync(cancellationToken);
        if (orderId <= 0)
            throw Oops.Oh("#下单失败#");

        var stockRows = await _db.Updateable<ShopProduct>()
            .SetColumns(p => p.Stock == p.Stock - 1)
            .Where(p => p.SiteID == siteId && p.ID == product.ID && p.Stock >= 1)
            .ExecuteCommandAsync(cancellationToken);
        if (stockRows < 1)
            throw Oops.Oh("#锁定库存失败#");

        if (points > 0)
        {
            var coinRows = await _db.Updateable<SimpX.Net.Data.Entity.Users.User>()
                .SetColumns(u => u.Coin == u.Coin - points)
                .Where(u => u.SiteID == siteId && u.ID == userId && u.Coin >= points)
                .ExecuteCommandAsync(cancellationToken);
            if (coinRows < 1)
                throw Oops.Oh("#积分不足#");
        }

        return product.IsAuto ? "#下单成功#" : "#请联系在线客服提供收件信息#";
    }

    /// <summary>商品详情。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>行</returns>
    public async Task<ShopProductItemDto> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<ShopProduct>()
            .Where(p => p.SiteID == siteId && p.ID == id).Take(1).ToListAsync(cancellationToken);
        var p = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        return MapProduct(p);
    }

    /// <summary>保存商品。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>编号</returns>
    public async Task<int> SaveProductAsync(ShopProductSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw Oops.Oh("#名称错误#");
        var siteId = _site.RequireSiteId();
        if (input.ID > 0)
        {
            var rows = await _db.Queryable<ShopProduct>()
                .Where(p => p.SiteID == siteId && p.ID == input.ID).Take(1).ToListAsync(cancellationToken);
            var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
            FillProduct(row, input);
            row.UpdateAt = DateTime.Now;
            await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
            return row.ID;
        }

        var neu = new ShopProduct
        {
            SiteID = siteId,
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            ClickCount = 0,
            Settings = string.Empty,
            Content = string.Empty,
            Description = string.Empty,
            Pic = string.Empty,
            Price = string.Empty,
            Unit = string.Empty,
            TypeName = string.Empty
        };
        FillProduct(neu, input);
        return await _db.Insertable(neu).ExecuteReturnIdentityAsync(cancellationToken);
    }

    /// <summary>删除商品。</summary>
    /// <param name="id">编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        await _db.Deleteable<ShopProduct>()
            .Where(p => p.SiteID == siteId && p.ID == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>订单分页。</summary>
    /// <param name="input">筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页</returns>
    public async Task<ShopOrderPageOutput> PageOrdersAsync(
        ShopOrderPageInput input,
        CancellationToken cancellationToken = default)
    {
        input ??= new ShopOrderPageInput();
        var siteId = _site.RequireSiteId();
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 20 : input.PageSize, 1, 100);
        var page = Math.Max(1, input.Page <= 0 ? 1 : input.Page);
        var query = _db.Queryable<ShopOrder>().Where(o => o.SiteID == siteId);
        if (input.UserID > 0)
            query = query.Where(o => o.UserID == input.UserID);
        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(o => o.UserName.Contains(input.UserName.Trim()));
        if (input.Status >= 0)
            query = query.Where(o => o.Status == input.Status);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(o => o.ID, OrderByType.Desc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new ShopOrderPageOutput
        {
            Total = total,
            Items = rows.Select(o => new ShopOrderItemDto
            {
                ID = o.ID,
                UserID = o.UserID,
                UserName = o.UserName,
                ProductName = o.ProductName,
                ProductId = o.ProductId,
                Money = o.Money,
                Status = o.Status,
                Remark = o.Remark,
                CreateAt = o.CreateAt
            }).ToList()
        };
    }

    /// <summary>改订单状态与备注。</summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateOrderStatusAsync(ShopOrderStatusInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ID <= 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var rows = await _db.Queryable<ShopOrder>()
            .Where(o => o.SiteID == siteId && o.ID == input.ID).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault() ?? throw Oops.Oh("#记录不存在#");
        row.Status = input.Status;
        if (input.Remark != null)
            row.Remark = input.Remark;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }

    private static void FillProduct(ShopProduct row, ShopProductSaveInput input)
    {
        row.Name = input.Name.Trim();
        row.TypeID = input.TypeID;
        row.TypeName = input.TypeName ?? string.Empty;
        row.Pic = input.Pic ?? string.Empty;
        row.Price = input.Price ?? string.Empty;
        row.Points = input.Points;
        row.Description = input.Description ?? string.Empty;
        row.Content = input.Content ?? string.Empty;
        row.Status = input.Status;
        row.Stock = input.Stock;
        row.WashFlow = input.WashFlow;
        row.IsAuto = input.IsAuto;
        row.Unit = input.Unit ?? string.Empty;
        row.Sort = input.Sort;
        row.Settings ??= string.Empty;
    }

    private static ShopProductItemDto MapProduct(ShopProduct p) => new()
    {
        ID = p.ID,
        Name = p.Name,
        TypeID = p.TypeID,
        TypeName = p.TypeName,
        Points = p.Points,
        Price = p.Price,
        Stock = p.Stock,
        Status = p.Status,
        Sort = p.Sort,
        IsAuto = p.IsAuto,
        Unit = p.Unit,
        WashFlow = p.WashFlow,
        Pic = p.Pic,
        Description = p.Description
    };

    // --- merged from ShopAdminService.WaveGaps.cs ---



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


    /// <summary>
    /// 对齐旧 savesort。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> SavesortAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SavesortCoreAsync(input, cancellationToken);
    }


    /// <summary>
    /// 对齐旧 treelist。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<object?> TreelistAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return TreelistCoreAsync(input, cancellationToken);
    }


    /// <summary>核心：add —— 商品新增/保存。</summary>
    private async Task<object?> AddCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = await SaveProductAsync(new ShopProductSaveInput
        {
            ID = ReadInt(input, "ID", "Id"),
            Name = ReadString(input, "Name", "name"),
            TypeID = ReadInt(input, "TypeID", "CateID", "CategoryID"),
            Points = ReadInt(input, "Points", "Point", "Money"),
            Stock = ReadDecimal(input, "Stock", "stock"),
            Status = ReadInt(input, "Status", "status"),
            Sort = ReadInt(input, "Sort", "sort"),
            Price = ReadString(input, "Price", "price"),
            Unit = ReadString(input, "Unit", "unit"),
            IsAuto = ReadInt(input, "IsAuto") == 1
                || string.Equals(ReadString(input, "IsAuto"), "true", StringComparison.OrdinalIgnoreCase)
        }, cancellationToken);
        return new { ID = id };
    }


    /// <summary>核心：batchdeleteimportpoint</summary>
    private Task<object?> BatchdeleteimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _ = ReadInt(input, "ID", "Id");
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：catedict —— 分类字典。</summary>
    private async Task<object?> CatedictCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        return await CateDictAsync(cancellationToken);
    }


    /// <summary>核心：children —— 按 ParentID 取子分类。</summary>
    private async Task<object?> ChildrenCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var parentId = ReadInt(input, "ParentID", "ID", "Id");
        var all = await ListCategoriesAsync(cancellationToken);
        return all.Where(c => c.ParentID == parentId).ToList();
    }


    /// <summary>核心：createuserpoint</summary>
    private Task<object?> CreateuserpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input; _ = cancellationToken;
        return Task.FromResult<object?>(null);
    }


    /// <summary>核心：deleteimportpoint</summary>
    private Task<object?> DeleteimportpointCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _ = ReadInt(input, "ID", "Id");
        return Task.FromResult<object?>(null);
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


    /// <summary>核心：info —— 分类详情（无则空壳）。</summary>
    private async Task<object?> InfoCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var id = ReadInt(input, "ID", "Id");
        if (id <= 0)
            return new ShopCategoryItemDto { Name = string.Empty };
        var all = await ListCategoriesAsync(cancellationToken);
        return all.FirstOrDefault(c => c.ID == id)
            ?? new ShopCategoryItemDto { Name = string.Empty };
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


    /// <summary>核心：savesort —— ID 顺序即 Sort 从大到小（分类）。</summary>
    private async Task<object?> SavesortCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var raw = ReadString(input, "ID", "Ids", "Sort");
        var ids = raw.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
            throw Oops.Oh("#参数错误#");
        var siteId = _site.RequireSiteId();
        var sort = ids.Length;
        foreach (var id in ids)
        {
            await _db.Updateable<ShopProductCategory>()
                .SetColumns(c => c.Sort == sort)
                .Where(c => c.SiteID == siteId && c.ID == id)
                .ExecuteCommandAsync(cancellationToken);
            sort--;
        }

        return null;
    }


    /// <summary>核心：treelist —— 扁平分类（前端自组树）。</summary>
    private async Task<object?> TreelistCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        return await ListCategoriesAsync(cancellationToken);
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

/// <summary>分类行。</summary>
public sealed class ShopCategoryItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>父级。</summary>
    public int ParentID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>分类保存。</summary>
public sealed class ShopCategorySaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>父级。</summary>
    public int ParentID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }
}

/// <summary>商品筛选。</summary>
public sealed class ShopProductPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>分类；0=全部。</summary>
    public int TypeID { get; set; }

    /// <summary>名称模糊。</summary>
    public string Name { get; set; }
}

/// <summary>商品分页。</summary>
public sealed class ShopProductPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<ShopProductItemDto> Items { get; set; }
}

/// <summary>商品行。</summary>
public sealed class ShopProductItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>分类。</summary>
    public int TypeID { get; set; }

    /// <summary>分类名。</summary>
    public string TypeName { get; set; }

    /// <summary>积分。</summary>
    public int Points { get; set; }

    /// <summary>标价。</summary>
    public string Price { get; set; }

    /// <summary>库存。</summary>
    public decimal Stock { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }

    /// <summary>自动发放。</summary>
    public bool IsAuto { get; set; }

    /// <summary>单位。</summary>
    public string Unit { get; set; }

    /// <summary>流水倍数。</summary>
    public decimal WashFlow { get; set; }

    /// <summary>图片。</summary>
    public string Pic { get; set; }

    /// <summary>简述。</summary>
    public string Description { get; set; }
}

/// <summary>商品保存。</summary>
public sealed class ShopProductSaveInput
{
    /// <summary>编号；0=新建。</summary>
    public int ID { get; set; }

    /// <summary>名称。</summary>
    public string Name { get; set; }

    /// <summary>分类。</summary>
    public int TypeID { get; set; }

    /// <summary>分类名。</summary>
    public string TypeName { get; set; }

    /// <summary>图片。</summary>
    public string Pic { get; set; }

    /// <summary>标价。</summary>
    public string Price { get; set; }

    /// <summary>积分。</summary>
    public int Points { get; set; }

    /// <summary>简述。</summary>
    public string Description { get; set; }

    /// <summary>详情。</summary>
    public string Content { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>库存。</summary>
    public decimal Stock { get; set; }

    /// <summary>流水倍数。</summary>
    public decimal WashFlow { get; set; }

    /// <summary>自动发放。</summary>
    public bool IsAuto { get; set; }

    /// <summary>单位。</summary>
    public string Unit { get; set; }

    /// <summary>排序。</summary>
    public int Sort { get; set; }
}

/// <summary>订单筛选。</summary>
public sealed class ShopOrderPageInput
{
    /// <summary>页码。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>状态；-1=全部。</summary>
    public int Status { get; set; } = -1;
}

/// <summary>订单分页。</summary>
public sealed class ShopOrderPageOutput
{
    /// <summary>总数。</summary>
    public int Total { get; set; }

    /// <summary>行。</summary>
    public List<ShopOrderItemDto> Items { get; set; }
}

/// <summary>订单行。</summary>
public sealed class ShopOrderItemDto
{
    /// <summary>编号。</summary>
    public int ID { get; set; }

    /// <summary>会员。</summary>
    public int UserID { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; }

    /// <summary>商品名。</summary>
    public string ProductName { get; set; }

    /// <summary>商品编号。</summary>
    public int ProductId { get; set; }

    /// <summary>价值。</summary>
    public decimal Money { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }

    /// <summary>时间。</summary>
    public DateTime CreateAt { get; set; }
}

/// <summary>改订单状态。</summary>
public sealed class ShopOrderStatusInput
{
    /// <summary>订单编号。</summary>
    public int ID { get; set; }

    /// <summary>状态。</summary>
    public int Status { get; set; }

    /// <summary>备注。</summary>
    public string Remark { get; set; }
}
