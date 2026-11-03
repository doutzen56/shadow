namespace SimpX.Net.Application.Admin;

/// <summary>
/// 租户后台提现接口 / 提现银行（JWT）。对齐旧 /admin/money/withdraw/channel*。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "withdrawChannel", Order = 486, Description = "提现接口")]
public partial class WithdrawChannelAppService : IDynamicApiController, ITransient
{
    private readonly WithdrawChannelService _channels;
    private readonly WithdrawChannelOperateService _operate;
    private readonly WithdrawBankService _banks;

    /// <summary>注入。</summary>
    public WithdrawChannelAppService(
        WithdrawChannelService channels,
        WithdrawChannelOperateService operate,
        WithdrawBankService banks)
    {
        _channels = channels;
        _operate = operate;
        _banks = banks;
    }

    /// <summary>提现渠道列表。</summary>
    [ApiDescriptionSettings(Name = "List"), HttpPost]
    [DisplayName("提现接口列表")]
    public Task<List<WithdrawChannelListItemDto>> List() =>
        _channels.ListAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 提现渠道列表（旧方法名别名）。对齐旧 channelist。
    /// </summary>
    [ApiDescriptionSettings(Name = "ChannelList"), HttpPost]
    [DisplayName("提现接口列表ChannelList")]
    public Task<List<WithdrawChannelListItemDto>> ChannelList() => List();

    /// <summary>提现渠道详情。</summary>
    /// <param name="id">渠道编号</param>
    [ApiDescriptionSettings(Name = "Info"), HttpPost]
    [DisplayName("提现接口详情")]
    public Task<WithdrawChannelDetailDto> Info([FromQuery] int id) =>
        _operate.InfoAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 提现渠道详情（旧方法名别名）。对齐旧 channelinfo。
    /// </summary>
    /// <param name="id">渠道编号</param>
    [ApiDescriptionSettings(Name = "ChannelInfo"), HttpPost]
    [DisplayName("提现接口详情ChannelInfo")]
    public Task<WithdrawChannelDetailDto> ChannelInfo([FromQuery] int id) => Info(id);

    /// <summary>
    /// 审核页渠道详情。对齐旧 checkchannelinfo。
    /// </summary>
    /// <param name="id">渠道编号</param>
    [ApiDescriptionSettings(Name = "CheckChannelInfo"), HttpPost]
    [DisplayName("审核页提现接口详情")]
    public Task<WithdrawChannelDetailDto> CheckChannelInfo([FromQuery] int id) => Info(id);

    /// <summary>保存提现渠道。</summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "Save"), HttpPost]
    [DisplayName("保存提现接口")]
    public Task Save(WithdrawChannelSaveInput input) =>
        _operate.SaveAsync(input ?? new WithdrawChannelSaveInput(), App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 保存提现渠道（旧方法名别名）。对齐旧 savechannel。
    /// </summary>
    /// <param name="input">入参</param>
    [ApiDescriptionSettings(Name = "SaveChannel"), HttpPost]
    [DisplayName("保存提现接口SaveChannel")]
    public Task SaveChannel(WithdrawChannelSaveInput input) => Save(input);

    /// <summary>软删提现渠道。</summary>
    /// <param name="id">渠道编号</param>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除提现接口")]
    public Task Delete([FromQuery] int id) =>
        _operate.DeleteAsync(id, App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 软删提现渠道（旧方法名别名）。对齐旧 deletechannel。
    /// </summary>
    /// <param name="id">渠道编号</param>
    [ApiDescriptionSettings(Name = "DeleteChannel"), HttpPost]
    [DisplayName("删除提现接口DeleteChannel")]
    public Task DeleteChannel([FromQuery] int id) => Delete(id);

    /// <summary>
    /// 当前开放提现渠道。对齐旧 openchannelist。
    /// </summary>
    [ApiDescriptionSettings(Name = "OpenChannelList"), HttpPost]
    [DisplayName("开放提现渠道")]
    public Task<List<WithdrawChannelOpenItemDto>> OpenChannelList() =>
        _channels.ListOpenAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 排序最高的开放渠道。对齐旧 openchannefirst。
    /// </summary>
    [ApiDescriptionSettings(Name = "OpenChannelFirst"), HttpPost]
    [DisplayName("首个开放提现渠道")]
    public Task<WithdrawChannelOpenItemDto> OpenChannelFirst() =>
        _channels.OpenFirstAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>
    /// 开放且挂代付的渠道。对齐旧 openchannessystem。
    /// </summary>
    [ApiDescriptionSettings(Name = "OpenChannelSystem"), HttpPost]
    [DisplayName("开放代付提现渠道")]
    public Task<List<WithdrawChannelOpenItemDto>> OpenChannelSystem() =>
        _channels.ListOpenSystemAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>提现银行配置。</summary>
    [ApiDescriptionSettings(Name = "BankConfig"), HttpPost]
    [DisplayName("提现银行配置")]
    public Task<WithdrawBankConfigDto> BankConfig() =>
        _banks.GetAsync(App.HttpContext?.RequestAborted ?? default);

    /// <summary>保存提现银行配置。</summary>
    /// <param name="input">银行配置原文</param>
    [ApiDescriptionSettings(Name = "SaveBankConfig"), HttpPost]
    [DisplayName("保存提现银行配置")]
    public Task SaveBankConfig(WithdrawBankSaveInput input) =>
        _banks.SaveAsync(input?.Bank ?? string.Empty, App.HttpContext?.RequestAborted ?? default);

    // --- merged from WithdrawChannelAppService.WaveGaps.cs ---



    /// <summary>
    /// 对齐旧 withdrawtest。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Withdrawtest"), HttpPost]
    [DisplayName("withdrawtest")]
    public Task<object?> Withdrawtest(Dictionary<string, object?>? input = null) =>
        _channels.WithdrawtestAsync(input, App.HttpContext?.RequestAborted ?? default);
}
