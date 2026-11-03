using Furion.FriendlyException;
using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Data;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 站点后台兼容会员列表/详情：挂「会员列表」权限；只操作当前站点。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "user-member", Order = 22)]
public class UserMemberFormAppService : IDynamicApiController, ITransient
{
    /// <summary>会员列表权限 GUID。</summary>
    public const string MemberListPermissionId = "1e7cc96020b843b988ed6df39024867e";

    private readonly MemberProfileService _profile;
    private readonly UserMemberService _svc;
    private readonly MemberMarkService _marks;
    private readonly BankAccountService _banks;

    /// <summary>
    /// 注入会员资料、会员服务与备注。
    /// </summary>
    /// <param name="profile">会员详情</param>
    /// <param name="svc">会员服务（列表/改锁/改分组/等级）</param>
    /// <param name="marks">会员备注</param>
    /// <param name="banks">银行账户</param>
    public UserMemberFormAppService(
        MemberProfileService profile,
        UserMemberService svc,
        MemberMarkService marks,
        BankAccountService banks)
    {
        _profile = profile;
        _svc = svc;
        _marks = marks;
        _banks = banks;
    }

    /// <summary>
    /// 会员列表摘要。对应旧版 /admin/user/member/list。
    /// </summary>
    /// <returns>旧版返回格式；info 含 RecordCount/data（字段名大写开头）</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/list")]
    [HttpGet("/admin/user/member/list")]
    [NonUnify]
    [DisplayName("站点后台会员列表")]
    public async Task<OldApiResult> List() => await ListCoreAsync();

    /// <summary>
    /// 会员列表别名。对应旧版 /admin/user/member/info/list。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/list")]
    [HttpGet("/admin/user/member/info/list")]
    [NonUnify]
    [DisplayName("站点后台会员列表info")]
    public async Task<OldApiResult> InfoList() => await ListCoreAsync();

    /// <summary>
    /// 会员导出。对应旧版 /admin/user/member/info/export（最多 5000 行，前端落 CSV）。
    /// </summary>
    /// <returns>旧版返回格式；info.list 为导出行</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/export")]
    [HttpGet("/admin/user/member/info/export")]
    [NonUnify]
    [DisplayName("站点后台会员导出")]
    public async Task<OldApiResult> InfoExport() => await ExportCoreAsync();

    /// <summary>
    /// 会员导出别名。对应旧版 /admin/user/member/export。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/export")]
    [HttpGet("/admin/user/member/export")]
    [NonUnify]
    [DisplayName("站点后台会员导出别名")]
    public async Task<OldApiResult> Export() => await ExportCoreAsync();

    /// <summary>
    /// 会员详情精简。对应旧版 /admin/user/member/info/detail。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/detail")]
    [HttpGet("/admin/user/member/info/detail")]
    [NonUnify]
    [DisplayName("站点后台会员详情")]
    public async Task<OldApiResult> Detail()
    {
        var userId = OldApiRequestParam.ReadInt(App.HttpContext, "UserID");
        var result = await _profile.AdminDetailAsync(userId, App.HttpContext?.RequestAborted ?? default);
        return result;
    }

    /// <summary>
    /// 设置锁定位。对应旧版 /admin/user/member/info/updatelock；服务里再检查是否有「其他功能」权限。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updatelock")]
    [NonUnify]
    [DisplayName("站点后台会员改锁")]
    public Task<OldApiResult> UpdateLock()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var lockBit = OldApiRequestParam.ReadInt(http, "Value");
        var isLock = OldApiRequestParam.ReadInt(http, "IsLock") == 1;
        return _svc.UpdateLockAsync(userId, lockBit, isLock, http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 改会员状态字段（GroupID / Level / InitLevel）。对应旧版 /admin/user/member/info/updatestatus。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updatestatus")]
    [NonUnify]
    [DisplayName("站点后台会员改状态")]
    public Task<OldApiResult> UpdateStatus()
    {
        var http = App.HttpContext;
        return _svc.UpdateStatusAsync(
            OldApiRequestParam.ReadInt(http, "UserID"),
            OldApiRequestParam.Read(http, "Field"),
            OldApiRequestParam.Read(http, "Value"),
            http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 会员备注列表。对应旧版 /admin/user/member/info/marklist。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/marklist")]
    [HttpGet("/admin/user/member/info/marklist")]
    [NonUnify]
    [DisplayName("站点后台会员备注列表")]
    public Task<OldApiResult> MarkList()
    {
        var userId = OldApiRequestParam.ReadInt(App.HttpContext, "UserID");
        return _marks.ListAsync(userId, App.HttpContext?.RequestAborted ?? default);
    }

    /// <summary>
    /// 添加会员备注。对应旧版 /admin/user/member/info/addmark。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/addmark")]
    [NonUnify]
    [DisplayName("站点后台添加会员备注")]
    public Task<OldApiResult> AddMark()
    {
        var http = App.HttpContext;
        return _marks.AddAsync(
            OldApiRequestParam.ReadInt(http, "UserID"),
            OldApiRequestParam.Read(http, "Content"),
            http?.RequestAborted ?? default);
    }

    /// <summary>
    /// 删除会员备注。对应旧版 /admin/user/member/info/deletemark。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/deletemark")]
    [NonUnify]
    [DisplayName("站点后台删除会员备注")]
    public Task<OldApiResult> DeleteMark()
    {
        var markId = OldApiRequestParam.ReadInt(App.HttpContext, "MarkID");
        return _marks.DeleteAsync(markId, App.HttpContext?.RequestAborted ?? default);
    }

    /// <summary>按用户名查。对应旧 query。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/query")]
    [HttpGet("/admin/user/member/info/query")]
    [NonUnify]
    [DisplayName("站点后台会员查询")]
    public async Task<OldApiResult> Query()
    {
        var key = OldApiRequestParam.Read(App.HttpContext, "UserName")
            ?? OldApiRequestParam.Read(App.HttpContext, "User")
            ?? OldApiRequestParam.Read(App.HttpContext, "Key");
        var result = await RunAsync(async ct =>
        {
            var name = await _svc.QueryUserNameAsync(key ?? string.Empty, ct);
            return new { UserName = name };
        });
        return result;
    }

    /// <summary>会员银行卡。对应旧 banklist。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/banklist")]
    [HttpGet("/admin/user/member/info/banklist")]
    [NonUnify]
    [DisplayName("站点后台会员银行卡")]
    public async Task<OldApiResult> Banklist()
    {
        var userId = OldApiRequestParam.ReadInt(App.HttpContext, "UserID");
        var result = await RunAsync(async ct =>
        {
            var rows = await _svc.BankListAsync(userId, ct);
            return OldApiShowResult.OfList(rows);
        });
        return result;
    }

    /// <summary>上级链。对应旧 getparentlist。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/getparentlist")]
    [HttpGet("/admin/user/member/info/getparentlist")]
    [NonUnify]
    [DisplayName("站点后台会员上级链")]
    public async Task<OldApiResult> Getparentlist()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var userName = OldApiRequestParam.Read(http, "UserName") ?? OldApiRequestParam.Read(http, "User");
        // 旧站 info 直接是上级链数组（含自身）
        var result = await RunAsync(async ct => await _svc.GetParentListAsync(userId, userName, ct));
        return result;
    }

    /// <summary>多币种余额。对应旧 detailmoney。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/detailmoney")]
    [HttpGet("/admin/user/member/info/detailmoney")]
    [NonUnify]
    [DisplayName("站点后台会员多币种余额")]
    public async Task<OldApiResult> Detailmoney()
    {
        var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
        if (id <= 0)
            id = OldApiRequestParam.ReadInt(App.HttpContext, "UserID");
        var result = await RunAsync(async ct => await _svc.DetailMoneyAsync(id, ct));
        return result;
    }

    /// <summary>钱包。对应旧 wallet。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/wallet")]
    [HttpGet("/admin/user/member/info/wallet")]
    [NonUnify]
    [DisplayName("站点后台会员钱包")]
    public async Task<OldApiResult> Wallet()
    {
        var userId = OldApiRequestParam.ReadInt(App.HttpContext, "UserID");
        var result = await RunAsync(async ct => await _svc.WalletAsync(userId, ct));
        return result;
    }

    /// <summary>会员流水要求。对应旧 washflow。</summary>
    [AllowAnonymous]
    [OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/washflow")]
    [HttpGet("/admin/user/member/info/washflow")]
    [NonUnify]
    [DisplayName("站点后台会员流水")]
    public async Task<OldApiResult> Washflow()
    {
        var http = App.HttpContext;
        var userId = OldApiRequestParam.ReadInt(http, "UserID");
        var currency = OldApiRequestParam.ReadInt(http, "Currency", 2);
        var result = await RunAsync(async ct => await _svc.WashFlowAsync(userId, currency, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updatecurrency")]
    [NonUnify, DisplayName("站点后台改会员币种开关")]
    public async Task<OldApiResult> Updatecurrency()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateCurrencyAsync(
                OldApiRequestParam.ReadInt(http, "UserID"),
                OldApiRequestParam.Read(http, "Currency") ?? string.Empty,
                OldApiRequestParam.Read(http, "IsOpen") is "1" or "true" or "True",
                ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updatetype")]
    [NonUnify, DisplayName("站点后台升会员类型")]
    public async Task<OldApiResult> Updatetype()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateTypeAsync(OldApiRequestParam.ReadInt(App.HttpContext, "UserID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/degradetype")]
    [NonUnify, DisplayName("站点后台降会员类型")]
    public async Task<OldApiResult> Degradetype()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.DegradeTypeAsync(OldApiRequestParam.ReadInt(App.HttpContext, "UserID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updateagent")]
    [NonUnify, DisplayName("站点后台改会员上级")]
    public async Task<OldApiResult> Updateagent()
    {
        var http = App.HttpContext;
        var result = await RunAsync(async ct =>
        {
            await _svc.UpdateAgentAsync(
                OldApiRequestParam.ReadInt(http, "UserID"),
                OldApiRequestParam.Read(http, "Agent") ?? string.Empty,
                ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/lockmoney")]
    [NonUnify, DisplayName("站点后台锁钱")]
    public async Task<OldApiResult> Lockmoney()
    {
        var http = App.HttpContext;
        _ = decimal.TryParse(OldApiRequestParam.Read(http, "Money"), out var money);
        var result = await RunAsync(async ct =>
        {
            await _svc.LockMoneyAsync(new MemberLockMoneyInput
            {
                UserID = OldApiRequestParam.ReadInt(http, "UserID"),
                Money = money,
                Currency = OldApiRequestParam.ReadInt(http, "Currency"),
                LockDesc = OldApiRequestParam.Read(http, "LockDesc") ?? string.Empty
            }, ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/unlockmoney")]
    [NonUnify, DisplayName("站点后台解锁钱")]
    public async Task<OldApiResult> Unlockmoney()
    {
        var result = await RunAsync(async ct =>
        {
            await _svc.UnlockMoneyAsync(OldApiRequestParam.ReadInt(App.HttpContext, "ID"), ct);
            return null;
        });
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/moremoney"), HttpGet("/admin/user/member/info/moremoney")]
    [NonUnify, DisplayName("站点后台多币余额")]
    public async Task<OldApiResult> Moremoney()
    {
        var result = await RunAsync(async ct =>
            await _svc.MoreMoneyAsync(OldApiRequestParam.ReadInt(App.HttpContext, "UserID"), ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/isblackmobile"), HttpGet("/admin/user/member/info/isblackmobile")]
    [NonUnify, DisplayName("站点后台手机黑名单判断")]
    public async Task<OldApiResult> Isblackmobile()
    {
        var result = await RunAsync(async ct =>
            await _svc.IsBlackMobileAsync(OldApiRequestParam.Read(App.HttpContext, "Mobile") ?? string.Empty, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/batchaddmark")]
    [NonUnify, DisplayName("站点后台批量备注")]
    public async Task<OldApiResult> Batchaddmark()
    {
        var http = App.HttpContext;
        var items = new List<MemberBatchAddMarkRow>();
        var itemsJson = OldApiRequestParam.Read(http, "Items") ?? OldApiRequestParam.Read(http, "items");
        if (!string.IsNullOrWhiteSpace(itemsJson) && itemsJson.TrimStart().StartsWith('['))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<MemberBatchAddMarkRow>>(itemsJson);
                if (parsed is { Count: > 0 })
                    items = parsed;
            }
            catch
            {
                /* 非 JSON 时走 Url / 空 Items 由 Shared 报错 */
            }
        }

        var result = await RunAsync(async ct =>
            await _svc.BatchAddMarkAsync(new MemberBatchAddMarkInput
            {
                Url = OldApiRequestParam.Read(http, "Url") ?? OldApiRequestParam.Read(http, "url") ?? string.Empty,
                Items = items
            }, ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/detailfavoritegame"), HttpGet("/admin/user/member/info/detailfavoritegame")]
    [NonUnify, DisplayName("站点后台偏好游戏")]
    public async Task<OldApiResult> Detailfavoritegame()
    {
        var result = await RunAsync(async ct =>
            await _svc.DetailFavoriteGameAsync(OldApiRequestParam.ReadInt(App.HttpContext, "UserID"), ct));
        return result;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/updatefunction")]
    [NonUnify, DisplayName("站点后台功能开关")]
    public async Task<OldApiResult> Updatefunction()
    {
        var http = App.HttpContext;
        var r = await _svc.UpdateFunctionAsync(
            OldApiRequestParam.ReadInt(http, "UserID"),
            OldApiFormReader.ReadInt(http, "Function", "Bit"),
            OldApiRequestParam.Read(http, "IsOpen") is "1" or "true" or "True",
            http?.RequestAborted ?? default);
        return r;
    }

    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/reset")]
    [NonUnify, DisplayName("站点后台重置字段")]
    public async Task<OldApiResult> Reset()
    {
        var http = App.HttpContext;
        var r = await _svc.ResetAsync(
            OldApiRequestParam.ReadInt(http, "UserID"),
            OldApiFormReader.ReadString(http, "Field", "Name") ?? string.Empty,
            http?.RequestAborted ?? default);
        return r;
    }

    /// <summary>删除会员银行卡。对齐旧 info/bankdelete。</summary>
    [AllowAnonymous, OldApiPermission(MemberListPermissionId)]
    [HttpPost("/admin/user/member/info/bankdelete")]
    [NonUnify, DisplayName("站点后台会员银行卡删")]
    public async Task<OldApiResult> Bankdelete()
    {
        try
        {
            var id = OldApiRequestParam.ReadInt(App.HttpContext, "ID");
            await _banks.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);
            return OldApiResult.Ok(string.Empty, null);
        }
        catch (AppFriendlyException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读旧版请求参数，委托会员服务做分页。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    private Task<OldApiResult> ListCoreAsync() =>
        _svc.PageForOldApiAsync(BuildPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 读旧版请求参数，委托会员服务导出（最多 5000 行）。
    /// </summary>
    /// <returns>旧版返回格式</returns>
    private Task<OldApiResult> ExportCoreAsync()
    {
        var input = BuildPageInput();
        input.Page = 1;
        input.PageSize = 5000;
        return _svc.ExportForOldApiAsync(input, App.HttpContext?.RequestAborted ?? default);
    }

    /// <summary>
    /// 从 Form/Query 读会员列表筛选（list / export 共用）。
    /// </summary>
    private static UserMemberPageInput BuildPageInput()
    {
        var http = App.HttpContext;
        var page = OldApiRequestParam.ReadInt(http, "PageIndex", 0);
        if (page <= 0)
            page = OldApiRequestParam.ReadInt(http, "page", 1);
        var pageSize = OldApiRequestParam.ReadInt(http, "PageSize", 0);
        if (pageSize <= 0)
            pageSize = OldApiRequestParam.ReadInt(http, "pageSize", 20);

        static DateTime? ReadTime(Microsoft.AspNetCore.Http.HttpContext ctx, string name)
        {
            var text = OldApiRequestParam.Read(ctx, name);
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return DateTime.TryParse(text, out var dt) ? dt : null;
        }

        static decimal? ReadDecimal(Microsoft.AspNetCore.Http.HttpContext ctx, string name)
        {
            var text = OldApiRequestParam.Read(ctx, name);
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return decimal.TryParse(text, out var n) ? n : null;
        }

        static int? ReadOptionalInt(Microsoft.AspNetCore.Http.HttpContext ctx, string name)
        {
            var text = OldApiRequestParam.Read(ctx, name);
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return int.TryParse(text, out var n) ? n : null;
        }

        return new UserMemberPageInput
        {
            Page = page,
            PageSize = pageSize,
            SearchType = OldApiRequestParam.Read(http, "SearchType"),
            User = OldApiRequestParam.Read(http, "User") ?? OldApiRequestParam.Read(http, "UserName"),
            UserName = OldApiRequestParam.Read(http, "UserName") ?? OldApiRequestParam.Read(http, "User"),
            Agent = OldApiRequestParam.Read(http, "Agent"),
            BelongUser = OldApiRequestParam.ReadInt(http, "BelongUser", 0),
            Type = OldApiRequestParam.Read(http, "Type"),
            GroupID = ReadOptionalInt(http, "GroupID"),
            Level = ReadOptionalInt(http, "Level"),
            RegMethod = ReadOptionalInt(http, "RegMethod"),
            TimeType = OldApiRequestParam.Read(http, "TimeType"),
            StartAt = ReadTime(http, "StartAt"),
            EndAt = ReadTime(http, "EndAt"),
            MinMoney = ReadDecimal(http, "MinMoney"),
            MaxMoney = ReadDecimal(http, "MaxMoney"),
            Depth = ReadOptionalInt(http, "Depth"),
            Active = ReadOptionalInt(http, "Active"),
            Domain = OldApiRequestParam.Read(http, "Domain"),
            InviterName = OldApiRequestParam.Read(http, "InviterName"),
            Currency = ReadOptionalInt(http, "Currency"),
            IsLimit = OldApiRequestParam.ReadInt(http, "IsLimit", 1),
            CommissionID = ReadOptionalInt(http, "CommissionID"),
            ReturnID = ReadOptionalInt(http, "ReturnID"),
            RechargeCount = ReadOptionalInt(http, "RechargeCount")
        };
    }

    private static async Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action)
    {
        try
        {
            return OldApiResult.Ok(string.Empty, await action(App.HttpContext?.RequestAborted ?? default));
        }
        catch (AppFriendlyException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }
}
