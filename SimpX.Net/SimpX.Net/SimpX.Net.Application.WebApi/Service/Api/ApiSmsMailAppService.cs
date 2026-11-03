using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 会员侧短信/邮件验证码旧路径：网关未迁完前挂「尚未接通」，避免 404。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "api-sms-mail", Order = 40)]
public class ApiSmsMailAppService : IDynamicApiController, ITransient
{
    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/api/sms/code"), HttpGet("/request/api/sms/code")]
    [HttpPost("/request/api/sms/register"), HttpGet("/request/api/sms/register")]
    [HttpPost("/request/api/mail/code"), HttpGet("/request/api/mail/code")]
    [HttpPost("/request/api/mail/register"), HttpGet("/request/api/mail/register")]
    [HttpPost("/request/api/mail/sendcode"), HttpGet("/request/api/mail/sendcode")]
    [NonUnify, DisplayName("api-sms-mail-stub")]
    public Task<OldApiResult> Stub() =>
        Run<object>(_ => throw Oops.Oh("#功能尚未接通#"));

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
