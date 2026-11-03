using System.Net;
using Microsoft.Extensions.Configuration;
using SimpX.Net.Application.Shared;
using SimpX.Net.Feature.Http;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// HttpJson 厂商转账骨架：FakeHttpCaller 成功/失败路径；未认领回落 Stub。
/// </summary>
public sealed class HttpJsonGameVendorTransferTests
{
    private const string TransferUrl = "http://vendor.local/transfer";

    /// <summary>HTTP 2xx + success=true → Ok。</summary>
    [Fact]
    public async Task Transfer_success_maps_ok()
    {
        HttpCallRequest captured = null;
        var http = new FakeHttpCaller(req =>
        {
            captured = req;
            return new HttpCallResponse
            {
                StatusCode = HttpStatusCode.OK,
                Body = """{"success":true,"message":"ok","resultId":"R-100"}"""
            };
        });

        var vendor = CreateVendor(http);
        Assert.True(vendor.CanHandle(HttpJsonGameVendorTransfer.HandledGameType));
        Assert.False(vendor.CanHandle(101));

        var outcome = await vendor.TransferAsync(new GameVendorTransferContext
        {
            SiteId = 1,
            UserId = 2,
            GameType = HttpJsonGameVendorTransfer.HandledGameType,
            OrderId = 100,
            Money = 12.5m,
            Currency = 1,
            Action = GameTransferService.ActionIn
        });

        Assert.True(outcome.Ok);
        Assert.Equal("ok", outcome.Message);
        Assert.Equal("R-100", outcome.ResultId);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal(TransferUrl, captured.Url);
        Assert.NotNull(captured.JsonContent);
    }

    /// <summary>HTTP 非 2xx → 失败。</summary>
    [Fact]
    public async Task Transfer_http_error_maps_fail()
    {
        var http = new FakeHttpCaller(_ => new HttpCallResponse
        {
            StatusCode = HttpStatusCode.BadGateway,
            Body = "upstream down"
        });

        var vendor = CreateVendor(http);
        var outcome = await vendor.TransferAsync(new GameVendorTransferContext
        {
            SiteId = 1,
            UserId = 2,
            GameType = HttpJsonGameVendorTransfer.HandledGameType,
            OrderId = 101,
            Money = 1m,
            Currency = 1,
            Action = GameTransferService.ActionOut
        });

        Assert.False(outcome.Ok);
        Assert.Contains("502", outcome.Message);
    }

    /// <summary>HTTP 2xx 但 success=false → 失败并带厂商说明。</summary>
    [Fact]
    public async Task Transfer_business_fail_maps_fail()
    {
        var http = new FakeHttpCaller(_ => new HttpCallResponse
        {
            StatusCode = HttpStatusCode.OK,
            Body = """{"ok":false,"msg":"#余额不足#","resultId":""}"""
        });

        var vendor = CreateVendor(http);
        var outcome = await vendor.TransferAsync(new GameVendorTransferContext
        {
            SiteId = 1,
            UserId = 2,
            GameType = HttpJsonGameVendorTransfer.HandledGameType,
            OrderId = 102,
            Money = 9m,
            Currency = 1,
            Action = GameTransferService.ActionIn
        });

        Assert.False(outcome.Ok);
        Assert.Equal("#余额不足#", outcome.Message);
    }

    /// <summary>未认领 GameType 走 Stub，不打 HTTP。</summary>
    [Fact]
    public async Task Resolve_unmatched_uses_stub()
    {
        var called = false;
        var http = new FakeHttpCaller(_ =>
        {
            called = true;
            return new HttpCallResponse { StatusCode = HttpStatusCode.OK, Body = "{}" };
        });
        var httpVendor = CreateVendor(http);
        var stub = new StubGameVendorTransfer { AlwaysSuccess = true };
        var svcVendors = new IGameVendorTransfer[] { httpVendor };

        // 直接验路由：101 不认领 → Stub；9999 认领 → HttpJson
        Assert.False(httpVendor.CanHandle(101));
        Assert.True(httpVendor.CanHandle(HttpJsonGameVendorTransfer.HandledGameType));

        var resolvedUnmatched = svcVendors.FirstOrDefault(v => v.CanHandle(101)) ?? stub;
        var unmatched = await resolvedUnmatched.TransferAsync(new GameVendorTransferContext
        {
            OrderId = 1,
            GameType = 101,
            Money = 1m
        });
        Assert.True(unmatched.Ok);
        Assert.StartsWith("stub-", unmatched.ResultId);
        Assert.False(called);

        var resolvedMatched = svcVendors.FirstOrDefault(v => v.CanHandle(HttpJsonGameVendorTransfer.HandledGameType)) ?? stub;
        Assert.Same(httpVendor, resolvedMatched);
    }

    /// <summary>建适配：FakeHttpCaller + 固定 TransferUrl，不读真实配置。</summary>
    private static HttpJsonGameVendorTransfer CreateVendor(IHttpCaller http)
    {
        var config = new ConfigurationBuilder().Build();
        return new HttpJsonGameVendorTransfer(http, config)
        {
            TransferUrl = TransferUrl
        };
    }
}
