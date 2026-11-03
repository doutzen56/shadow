namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台会员列表/详情/改锁改等级/重置字段（JWT）。
/// 业务字段对齐旧版 /admin/user/member/*；路由用 userMember，避开框架 Member。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "userMember", Order = 456, Description = "会员列表")]
public class UserMemberAppService : IDynamicApiController, ITransient
{
    private readonly UserMemberService _svc;
    private readonly MemberMarkService _marks;
    private readonly BankAccountService _banks;

    /// <summary>
    /// 注入会员服务、备注与银行信息。
    /// </summary>
    /// <param name="svc">会员服务</param>
    /// <param name="marks">备注</param>
    /// <param name="banks">银行信息（bankdelete）</param>
    public UserMemberAppService(UserMemberService svc, MemberMarkService marks, BankAccountService banks)
    {
        _svc = svc;
        _marks = marks;
        _banks = banks;
    }

    /// <summary>
    /// 当前站会员分页。对齐旧版 list。
    /// </summary>
    /// <param name="input">分页与用户名</param>
    /// <returns>分页结果</returns>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("会员分页列表")]
    public Task<UserMemberPageOutput> Page(UserMemberPageInput input) =>
        _svc.PageAsync(input, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 当前站会员详情。对齐旧版 info/detail。
    /// </summary>
    /// <param name="userId">会员编号（旧版 UserID）</param>
    /// <returns>详情</returns>
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    [DisplayName("会员详情")]
    public Task<UserMemberDetailDto> Detail([FromQuery] int userId) =>
        _svc.DetailAsync(userId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 改锁定位。对齐旧版 info/updatelock。
    /// </summary>
    /// <param name="input">UserID、Value=锁定位、IsLock</param>
    [ApiDescriptionSettings(Name = "UpdateLock"), HttpPost]
    [DisplayName("会员改锁")]
    public async Task UpdateLock(MemberUpdateLockInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = await _svc.UpdateLockAsync(
            input.UserID,
            input.Value,
            input.IsLock,
            App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 改功能位。对齐旧版 info/updatefunction。
    /// </summary>
    /// <param name="input">UserID、Value=功能位、IsOpen</param>
    [ApiDescriptionSettings(Name = "UpdateFunction"), HttpPost]
    [DisplayName("会员改功能位")]
    public async Task UpdateFunction(MemberUpdateFunctionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = await _svc.UpdateFunctionAsync(
            input.UserID,
            input.Value,
            input.IsOpen,
            App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 改 GroupID / Level / InitLevel。对齐旧版 info/updatestatus。
    /// </summary>
    /// <param name="input">UserID、Field、Value</param>
    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("会员改状态")]
    public async Task UpdateStatus(MemberUpdateStatusInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = await _svc.UpdateStatusAsync(
            input.UserID,
            input.Field,
            input.Value,
            App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 重置会员资料字段。对齐旧版 ResetUserInfo。
    /// </summary>
    /// <param name="input">UserID、Field</param>
    [ApiDescriptionSettings(Name = "Reset"), HttpPost]
    [DisplayName("会员重置字段")]
    public async Task Reset(MemberResetInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = await _svc.ResetAsync(
            input.UserID,
            input.Field,
            App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 会员备注列表。对齐旧版 marklist。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <returns>备注行</returns>
    [ApiDescriptionSettings(Name = "MarkList"), HttpGet]
    [DisplayName("会员备注列表")]
    public async Task<object> MarkList([FromQuery] int userId)
    {
        var result = await _marks.ListAsync(userId, App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
        return result.Info;
    }

    /// <summary>
    /// 添加会员备注。对齐旧版 addmark。
    /// </summary>
    /// <param name="input">UserID、Content</param>
    [ApiDescriptionSettings(Name = "AddMark"), HttpPost]
    [DisplayName("添加会员备注")]
    public async Task AddMark(MemberMarkWriteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = await _marks.AddAsync(
            input.UserID,
            input.Content,
            App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 删除会员备注。对齐旧版 deletemark。
    /// </summary>
    /// <param name="markId">备注编号</param>
    [ApiDescriptionSettings(Name = "DeleteMark"), HttpPost]
    [DisplayName("删除会员备注")]
    public async Task DeleteMark([FromQuery] int markId)
    {
        var result = await _marks.DeleteAsync(markId, App.HttpContext?.RequestAborted ?? default);
        ThrowIfFailed(result);
    }

    /// <summary>
    /// 禁止货币。对齐旧 updatecurrency。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "UpdateCurrency"), HttpPost]
    [DisplayName("会员禁止货币")]
    public Task UpdateCurrency(MemberUpdateCurrencyInput input) =>
        _svc.UpdateCurrencyAsync(
            input?.UserID ?? 0,
            input?.Value,
            input?.IsOpen ?? false,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 升级为代理。对齐旧 updatetype。
    /// </summary>
    /// <param name="input">会员编号</param>
    [ApiDescriptionSettings(Name = "UpdateType"), HttpPost]
    [DisplayName("会员升级代理")]
    public Task UpdateType(MemberUserIdInput input) =>
        _svc.UpdateTypeAsync(input?.UserID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 代理降为会员。对齐旧 degradetype。
    /// </summary>
    /// <param name="input">会员编号</param>
    [ApiDescriptionSettings(Name = "DegradeType"), HttpPost]
    [DisplayName("代理降级会员")]
    public Task DegradeType(MemberUserIdInput input) =>
        _svc.DegradeTypeAsync(input?.UserID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 会员银行卡列表。对齐旧 banklist。
    /// </summary>
    /// <param name="userId">会员编号</param>
    /// <returns>行</returns>
    [ApiDescriptionSettings(Name = "BankList"), HttpPost]
    [DisplayName("会员银行卡列表")]
    public Task<List<MemberBankListItemDto>> BankList([FromQuery] int userId) =>
        _svc.BankListAsync(userId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 删除会员银行卡。对齐旧 bankdelete（委托银行服务）。
    /// </summary>
    /// <param name="input">帐户编号</param>
    [ApiDescriptionSettings(Name = "BankDelete"), HttpPost]
    [DisplayName("删除会员银行卡")]
    public Task BankDelete(BankAccountDeleteInput input) =>
        _banks.DeleteAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 钱包摘要。对齐旧 wallet。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <returns>钱包</returns>
    [ApiDescriptionSettings(Name = "Wallet"), HttpPost]
    [DisplayName("会员钱包")]
    public Task<MemberWalletDto> Wallet([FromQuery] int userId) =>
        _svc.WalletAsync(userId, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 流水。对齐旧 washflow。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="currency">币种</param>
    /// <returns>流水</returns>
    [ApiDescriptionSettings(Name = "WashFlow"), HttpPost]
    [DisplayName("会员流水")]
    public Task<MemberWashFlowDto> WashFlow([FromQuery] int userId, [FromQuery] int currency) =>
        _svc.WashFlowAsync(userId, currency, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 手动锁钱。对齐旧 lockmoney。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "LockMoney"), HttpPost]
    [DisplayName("会员锁钱")]
    public Task LockMoney(MemberLockMoneyInput input) =>
        _svc.LockMoneyAsync(input ?? new MemberLockMoneyInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 手动解锁。对齐旧 unlockmoney。
    /// </summary>
    /// <param name="input">锁编号</param>
    [ApiDescriptionSettings(Name = "UnlockMoney"), HttpPost]
    [DisplayName("会员解锁钱")]
    public Task UnlockMoney(MemberUnlockMoneyInput input) =>
        _svc.UnlockMoneyAsync(input?.ID ?? 0, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 改上级。对齐旧 updateagent。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "UpdateAgent"), HttpPost]
    [DisplayName("会员改上级")]
    public Task UpdateAgent(MemberUpdateAgentInput input) =>
        _svc.UpdateAgentAsync(
            input?.UserID ?? 0,
            input?.Agent,
            App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 用户名联想。对齐旧 query。
    /// </summary>
    /// <param name="input">关键字</param>
    /// <returns>JSON 数组字符串</returns>
    [ApiDescriptionSettings(Name = "Query"), HttpPost]
    [DisplayName("会员用户名联想")]
    public Task<string> Query(MemberQueryInput input) =>
        _svc.QueryUserNameAsync(input?.Key, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 按用户名取编号。对齐旧 getuserid。
    /// </summary>
    /// <param name="input">用户名</param>
    /// <returns>编号</returns>
    [ApiDescriptionSettings(Name = "GetUserId"), HttpPost]
    [DisplayName("会员取编号")]
    public Task<MemberGetUserIdOutput> GetUserId(MemberGetUserIdInput input) =>
        _svc.GetUserIdAsync(input?.User, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 上级链。对齐旧 getparentlist。
    /// </summary>
    /// <param name="userId">会员</param>
    /// <param name="userName">用户名</param>
    /// <returns>上级</returns>
    [ApiDescriptionSettings(Name = "GetParentList"), HttpPost]
    [DisplayName("会员上级链")]
    public Task<List<MemberParentItemDto>> GetParentList([FromQuery] int userId, [FromQuery] string userName) =>
        _svc.GetParentListAsync(userId, userName, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 多币种余额。对齐旧 moremoney。
    /// </summary>
    /// <param name="id">会员编号（旧 ID）</param>
    /// <returns>币种余额</returns>
    [ApiDescriptionSettings(Name = "MoreMoney"), HttpPost]
    [DisplayName("会员多币种余额")]
    public async Task<object> MoreMoney([FromQuery] int id) =>
        await _svc.MoreMoneyAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 主/辅币种余额。对齐旧 detailmoney。
    /// </summary>
    /// <param name="id">会员编号</param>
    /// <returns>主/明细</returns>
    [ApiDescriptionSettings(Name = "DetailMoney"), HttpPost]
    [DisplayName("会员资金明细")]
    public Task<MemberMoneyByCurrencyDto> DetailMoney([FromQuery] int id) =>
        _svc.DetailMoneyAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 收藏游戏。对齐旧 detailfavoritegame。
    /// </summary>
    /// <param name="id">会员编号</param>
    /// <returns>名称行</returns>
    [ApiDescriptionSettings(Name = "DetailFavoriteGame"), HttpPost]
    [DisplayName("会员收藏游戏")]
    public Task<List<object>> DetailFavoriteGame([FromQuery] int id) =>
        _svc.DetailFavoriteGameAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 黑名单手机。对齐旧 isblackmobile。
    /// </summary>
    /// <param name="mobile">手机</param>
    /// <returns>结果</returns>
    [ApiDescriptionSettings(Name = "IsBlackMobile"), HttpPost]
    [DisplayName("会员黑名单手机")]
    public Task<MemberBlackMobileDto> IsBlackMobile([FromQuery] string mobile) =>
        _svc.IsBlackMobileAsync(mobile, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 批量备注。对齐旧 batchaddmark。
    /// </summary>
    /// <param name="input">行或 Url</param>
    /// <returns>结果文案</returns>
    [ApiDescriptionSettings(Name = "BatchAddMark"), HttpPost]
    [DisplayName("会员批量备注")]
    public Task<string> BatchAddMark(MemberBatchAddMarkInput input) =>
        _svc.BatchAddMarkAsync(input ?? new MemberBatchAddMarkInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 导出。对齐旧 export。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>导出行</returns>
    [ApiDescriptionSettings(Name = "Export"), HttpPost]
    [DisplayName("会员导出")]
    public Task<MemberExportOutput> Export(UserMemberPageInput input) =>
        _svc.ExportAsync(input ?? new UserMemberPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 受限搜索。对齐旧 limitusersearch。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "LimitUserSearch"), HttpPost]
    [DisplayName("会员受限搜索")]
    public Task<UserMemberPageOutput> LimitUserSearch(UserMemberPageInput input) =>
        _svc.LimitUserSearchAsync(input ?? new UserMemberPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 普通搜索。对齐旧 usersearch。
    /// </summary>
    /// <param name="input">筛选</param>
    /// <returns>分页</returns>
    [ApiDescriptionSettings(Name = "UserSearch"), HttpPost]
    [DisplayName("会员搜索")]
    public Task<UserMemberPageOutput> UserSearch(UserMemberPageInput input) =>
        _svc.UserSearchAsync(input ?? new UserMemberPageInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 旧版失败则抛业务异常（JWT 统一包装）。
    /// </summary>
    private static void ThrowIfFailed(OldApiResult result)
    {
        if (result == null || result.Success == 1)
            return;
        throw Oops.Oh(string.IsNullOrEmpty(result.Msg) ? "#操作失败#" : result.Msg);
    }
}

/// <summary>
/// 重置字段入参（对齐旧版 UserID / Field）。
/// </summary>
public sealed class MemberResetInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>字段：Mobile / Email / PayPassword / Password。</summary>
    public string Field { get; set; }
}

/// <summary>
/// 改锁入参（对齐旧版 UserID / Value / IsLock）。
/// </summary>
public sealed class MemberUpdateLockInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>锁定位数值。</summary>
    public int Value { get; set; }

    /// <summary>是否锁定。</summary>
    public bool IsLock { get; set; }
}

/// <summary>
/// 改功能位入参（对齐旧版 UserID / Value / IsOpen）。
/// </summary>
public sealed class MemberUpdateFunctionInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>功能位数值。</summary>
    public int Value { get; set; }

    /// <summary>是否打开该位。</summary>
    public bool IsOpen { get; set; }
}

/// <summary>
/// 改状态入参（对齐旧版 UserID / Field / Value）。
/// </summary>
public sealed class MemberUpdateStatusInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>字段：GroupID / Level / InitLevel。</summary>
    public string Field { get; set; }

    /// <summary>新值。</summary>
    public string Value { get; set; }
}

/// <summary>
/// 添加备注入参。
/// </summary>
public sealed class MemberMarkWriteInput
{
    /// <summary>会员编号。</summary>
    public int UserID { get; set; }

    /// <summary>备注内容。</summary>
    public string Content { get; set; }
}
