using Furion.UnifyResult;
using Microsoft.AspNetCore.Mvc;
using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.WebApi;

/// <summary>
/// 第三方授权旧路径：/request/api/auth/{gettype|login}。
/// </summary>
[OldApiAuth]
[ApiDescriptionSettings(Name = "api-auth", Order = 40)]
public class ApiAuthAppService : IDynamicApiController, ITransient
{
    private readonly SiteAuthorizationService _authz;

    public ApiAuthAppService(SiteAuthorizationService authz) => _authz = authz;

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/api/auth/gettype"), HttpGet("/request/api/auth/gettype")]
    [NonUnify, DisplayName("api-auth-gettype")]
    public Task<OldApiResult> Gettype() =>
        Run(ct => _authz.ListOpenTypesAsync(ct));

    [OldApiGuest, AllowAnonymous]
    [HttpPost("/request/api/auth/login"), HttpGet("/request/api/auth/login")]
    [NonUnify, DisplayName("api-auth-login")]
    public Task<OldApiResult> Login() =>
        Run<object>(async ct =>
        {
            _ = await _authz.ListOpenTypesAsync(ct);
            throw Oops.Oh("#未开放#");
        });

    private static async Task<OldApiResult> Run<T>(Func<CancellationToken, Task<T>> action) => await OldApiFormReader.RunAsync(action);
}
