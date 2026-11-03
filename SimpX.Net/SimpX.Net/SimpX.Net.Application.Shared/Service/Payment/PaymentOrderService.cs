using System.Text.Json;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 支付单 Attach JSON：回调入账需要的站、会员、币种、资金类型。
/// </summary>
public sealed class PaymentOrderAttach
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>站点。</summary>
    public int SiteId { get; set; }

    /// <summary>会员。</summary>
    public int UserId { get; set; }

    /// <summary>币种，默认 1。</summary>
    public int Currency { get; set; } = 1;

    /// <summary>资金类型，默认充值=1。</summary>
    public int MoneyType { get; set; } = 1;

    /// <summary>是否已具备入账字段。</summary>
    public bool CanCredit => SiteId > 0 && UserId > 0;

    /// <summary>
    /// 从 JSON 解析；非法或空返回 null。
    /// </summary>
    /// <param name="json">Attach 原文</param>
    /// <returns>对象或 null</returns>
    public static PaymentOrderAttach TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<PaymentOrderAttach>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 序列化为 Attach。
    /// </summary>
    /// <returns>JSON</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);
}

/// <summary>
/// 支付单领域服务（无 DynamicApi）。按当前租户写库；回调按单号跨租户查找后更新；成功时可走账本入账。
/// </summary>
public sealed class PaymentOrderService : ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IEnumerable<IPaymentChannel> _channels;
    private readonly ICurrentTenantAccessor _currentTenant;
    private readonly IMoneyLedger _money;

    /// <summary>
    /// 注入库、渠道、租户与账本。
    /// </summary>
    public PaymentOrderService(
        ISqlSugarClient db,
        IEnumerable<IPaymentChannel> channels,
        ICurrentTenantAccessor currentTenant,
        IMoneyLedger money)
    {
        _db = db;
        _channels = channels;
        _currentTenant = currentTenant;
        _money = money;
    }

    /// <summary>
    /// 创建支付单并返回调起参数。调用方要先按域名确定好租户。
    /// </summary>
    /// <param name="input">创建支付请求</param>
    /// <returns>支付单号与渠道调起参数</returns>
    public async Task<PaymentCreateResult> Create(PaymentCreateRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.BusinessOrderNo))
            throw Oops.Oh("业务订单号不能为空");
        if (input.Amount <= 0)
            throw Oops.Oh("支付金额必须大于 0");

        var tenantId = RequireTenantId();
        var channel = ResolveChannel(input.ChannelCode);
        var existsList = await _db.Queryable<PaymentOrder>()
            .Where(u => u.BusinessOrderNo == input.BusinessOrderNo && u.ChannelCode == channel.ChannelCode)
            .Take(1)
            .ToListAsync();
        var exists = existsList.FirstOrDefault();
        if (exists != null && exists.Status == PaymentStatus.Paid)
            throw Oops.Oh("该业务单已支付成功，请勿重复下单");

        var attach = BuildAttach(input);
        var now = DateTime.Now;
        var order = exists ?? new PaymentOrder
        {
            Id = SnowFlakeSingle.Instance.NextId(),
            TenantId = tenantId,
            PaymentOrderNo = $"P{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            BusinessOrderNo = input.BusinessOrderNo,
            ChannelCode = channel.ChannelCode,
            Amount = input.Amount,
            Subject = input.Subject ?? input.BusinessOrderNo,
            Status = PaymentStatus.Created,
            Attach = attach,
            CreateTime = now,
            UpdateTime = now
        };
        if (exists != null)
            order.Attach = string.IsNullOrWhiteSpace(attach) ? order.Attach : attach;

        var payParams = await channel.CreateAsync(order);
        if (exists == null)
            await _db.Insertable(order).ExecuteCommandAsync();
        else
            await _db.Updateable(order).ExecuteCommandAsync();

        return new PaymentCreateResult
        {
            PaymentOrderNo = order.PaymentOrderNo,
            ChannelCode = order.ChannelCode,
            Status = order.Status,
            PayParams = payParams
        };
    }

    /// <summary>
    /// 按支付单号查询（当前租户过滤）。
    /// </summary>
    /// <param name="paymentOrderNo">支付单号</param>
    /// <returns>支付单</returns>
    public async Task<PaymentOrder> Get(string paymentOrderNo)
    {
        var list = await _db.Queryable<PaymentOrder>()
            .Where(u => u.PaymentOrderNo == paymentOrderNo)
            .Take(1)
            .ToListAsync();
        return list.FirstOrDefault() ?? throw Oops.Oh("支付单不存在");
    }

    /// <summary>
    /// 处理渠道回调。回调不带租户域名，按单号全局查找后更新；Paid 且 Attach 含会员则入账一次。
    /// </summary>
    /// <param name="channelCode">渠道编码</param>
    /// <param name="rawBody">原始正文</param>
    /// <returns>渠道约定的成功应答</returns>
    public async Task<string> Notify(string channelCode, string rawBody)
    {
        var channel = ResolveChannel(channelCode);
        var notify = await channel.ParseNotifyAsync(rawBody);

        var list = await _db.Queryable<PaymentOrder>().ClearFilter()
            .Where(u => u.PaymentOrderNo == notify.PaymentOrderNo)
            .Take(1)
            .ToListAsync();
        if (list.Count == 0)
            throw Oops.Oh("支付单不存在");
        var payOrder = list[0];

        if (payOrder.Status == PaymentStatus.Paid)
            return "SUCCESS";

        payOrder.Status = notify.Status;
        payOrder.ChannelTradeNo = notify.ChannelTradeNo;
        payOrder.UpdateTime = DateTime.Now;
        await _db.Updateable(payOrder)
            .UpdateColumns(u => new { u.Status, u.ChannelTradeNo, u.UpdateTime })
            .ExecuteCommandAsync();

        if (notify.Status == PaymentStatus.Paid)
            await TryCreditAsync(payOrder);

        return "SUCCESS";
    }

    /// <summary>
    /// Attach 能解析出站/会员时走账本；否则只改支付单状态。
    /// </summary>
    private async Task TryCreditAsync(PaymentOrder payOrder)
    {
        if (IsCredited(payOrder.ExtraJson))
            return;

        var attach = PaymentOrderAttach.TryParse(payOrder.Attach);
        if (attach == null || !attach.CanCredit)
            return;

        var moneyType = attach.MoneyType <= 0 ? 1 : attach.MoneyType;
        var currency = attach.Currency <= 0 ? 1 : attach.Currency;
        await _money.AddMoneyLogAsync(
            attach.SiteId,
            attach.UserId,
            payOrder.Amount,
            moneyType,
            TruncateSource(payOrder.PaymentOrderNo),
            payOrder.Subject ?? "支付入账",
            currency);

        payOrder.ExtraJson = MarkCredited(payOrder.ExtraJson);
        payOrder.UpdateTime = DateTime.Now;
        await _db.Updateable(payOrder)
            .UpdateColumns(u => new { u.ExtraJson, u.UpdateTime })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 合并显式站/会员字段与 Attach JSON。
    /// </summary>
    private static string BuildAttach(PaymentCreateRequest input)
    {
        var parsed = PaymentOrderAttach.TryParse(input.Attach) ?? new PaymentOrderAttach();
        if (input.SiteId > 0)
            parsed.SiteId = input.SiteId;
        if (input.UserId > 0)
            parsed.UserId = input.UserId;
        if (input.Currency > 0)
            parsed.Currency = input.Currency;
        if (input.MoneyType > 0)
            parsed.MoneyType = input.MoneyType;
        if (!parsed.CanCredit)
            return input.Attach;
        return parsed.ToJson();
    }

    private static bool IsCredited(string extraJson)
    {
        if (string.IsNullOrWhiteSpace(extraJson))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(extraJson);
            if (doc.RootElement.TryGetProperty("credited", out var c) ||
                doc.RootElement.TryGetProperty("Credited", out c))
                return c.ValueKind == JsonValueKind.True ||
                       (c.ValueKind == JsonValueKind.String &&
                        string.Equals(c.GetString(), "true", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static string MarkCredited(string extraJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(extraJson) ? "{}" : extraJson);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var p in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(p.Name, "credited", StringComparison.OrdinalIgnoreCase))
                        continue;
                    writer.WritePropertyName(p.Name);
                    p.Value.WriteTo(writer);
                }

                writer.WriteBoolean("credited", true);
                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return """{"credited":true}""";
        }
    }

    private static string TruncateSource(string s) =>
        string.IsNullOrEmpty(s) || s.Length <= 20 ? (s ?? string.Empty) : s[..20];

    /// <summary>
    /// 按渠道编码解析实现。
    /// </summary>
    private IPaymentChannel ResolveChannel(string channelCode)
    {
        var code = string.IsNullOrWhiteSpace(channelCode) ? "demo" : channelCode.Trim();
        var channel = _channels.FirstOrDefault(u => u.ChannelCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        return channel ?? throw Oops.Oh($"未注册支付渠道：{code}");
    }

    /// <summary>
    /// 写支付单时要先确定好租户。
    /// </summary>
    private long RequireTenantId()
    {
        if (_currentTenant.TenantId is > 0 and var tid)
            return tid;
        throw Oops.Oh(ErrorCodeEnum.Z1005);
    }
}
