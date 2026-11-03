using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SimpX.Net.Data.Entity.Sites;
using SimpX.Net.Feature.Auth;
using SqlSugar;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 兼容鉴权：接口既不是「不用登录」，又没有会话时返回 ErrorType=Login；缺权限返回 ErrorType=Permission。
/// 直接写出旧版 JSON，避免再被 AdminResult 包一层。挂在兼容动态 API 类上。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class OldApiAuthAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
{
    /// <summary>尽量靠前，在业务动作前拦截。</summary>
    public int Order => -2000;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo;
        if (method == null)
        {
            await next();
            return;
        }

        if (method.IsDefined(typeof(OldApiGuestAttribute), inherit: true)
            || method.DeclaringType?.IsDefined(typeof(OldApiGuestAttribute), inherit: true) == true)
        {
            OldApiContract.RewriteExecutedResult(await next());
            return;
        }

        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var face = ResolveFace(path);
        if (face == AuthFace.Unknown)
        {
            OldApiContract.RewriteExecutedResult(await next());
            return;
        }

        var sp = context.HttpContext.RequestServices;
        switch (face)
        {
            case AuthFace.Member:
                {
                    var memberAuth = sp.GetRequiredService<MemberAuthService>();
                    var site = sp.GetRequiredService<ICurrentSiteAccessor>();
                    var siteId = site.SiteId;
                    if (siteId == null || await memberAuth.TryResolveUserIdFromCookieAsync(siteId.Value, context.HttpContext.RequestAborted) == null)
                    {
                        await WriteOldApiAsync(context, OldApiResult.NeedLogin());
                        return;
                    }
                    break;
                }
            case AuthFace.SiteAdmin:
                {
                    // 新壳一律 JWT；旧站 Cookie TOKEN 仍兼容。有 Cookie 走旧分组 GUID，否则认 JWT + 新权限码。
                    var adminAuth = sp.GetRequiredService<SiteAdminAuthService>();
                    var admin = await adminAuth.TryGetCurrentAdminAsync(context.HttpContext.RequestAborted);
                    if (admin != null)
                    {
                        var denied = await CheckSiteAdminPermissionAsync(sp, admin, method, context.HttpContext.RequestAborted);
                        if (denied != null)
                        {
                            await WriteOldApiAsync(context, denied);
                            return;
                        }
                        break;
                    }

                    if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
                    {
                        await WriteOldApiAsync(context, OldApiResult.NeedLogin());
                        return;
                    }

                    var jwtDenied = await CheckJwtPermissionAsync(sp, method);
                    if (jwtDenied != null)
                    {
                        await WriteOldApiAsync(context, jwtDenied);
                        return;
                    }
                    break;
                }
            case AuthFace.SystemAdmin:
                {
                    var systemMenu = sp.GetRequiredService<SystemAdminMenuService>();
                    var systemAdmin = await systemMenu.TryGetCurrentAdminAsync(context.HttpContext.RequestAborted);
                    if (systemAdmin == null)
                    {
                        await WriteOldApiAsync(context, OldApiResult.NeedLogin());
                        return;
                    }

                    var required = GetRequiredPermissions(method);
                    if (!OldApiPermissionEvaluator.SystemAdminHasAll(systemAdmin.Permission, required))
                    {
                        var msg = OldApiPermissionEvaluator.BuildPermissionDeniedMessage(required[0]);
                        await WriteOldApiAsync(context, OldApiResult.NeedPermission(msg));
                        return;
                    }
                    break;
                }
        }

        OldApiContract.RewriteExecutedResult(await next());
    }

    /// <summary>
    /// 按路径判断三端（单测可调用）。
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <returns>认证面</returns>
    public static AuthFace ResolveFace(string path)
    {
        if (path.StartsWith("/request/", StringComparison.OrdinalIgnoreCase))
            return AuthFace.Member;
        if (path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase))
            return AuthFace.SiteAdmin;
        if (path.StartsWith("/system/", StringComparison.OrdinalIgnoreCase))
            return AuthFace.SystemAdmin;
        return AuthFace.Unknown;
    }

    /// <summary>
    /// 站点后台权限：从业务库读分组权限串后交给判定器。分组表在 BetWin30，不能走主库 Sqlite。
    /// </summary>
    private static async Task<OldApiResult> CheckSiteAdminPermissionAsync(
        IServiceProvider sp,
        SiteAdmin admin,
        System.Reflection.MethodInfo method,
        CancellationToken cancellationToken)
    {
        var required = GetRequiredPermissions(method);
        if (required.Count == 0)
            return null;

        string groupCsv = null;
        if (!(string.Equals(admin.AdminName, "admin", StringComparison.OrdinalIgnoreCase) || admin.GroupID == 0))
        {
            var db = sp.GetRequiredService<IBizDbAccessor>().Db;
            var groups = await db.Queryable<SiteAdminGroup>()
                .Where(g => g.SiteID == admin.SiteID && g.ID == admin.GroupID)
                .Take(1)
                .ToListAsync(cancellationToken);
            groupCsv = groups.FirstOrDefault()?.PermissionString;
        }

        if (OldApiPermissionEvaluator.SiteAdminHasAll(admin.AdminName, admin.GroupID, groupCsv, required))
            return null;

        return OldApiResult.NeedPermission(OldApiPermissionEvaluator.BuildPermissionDeniedMessage(required[0]));
    }

    private static List<string> GetRequiredPermissions(System.Reflection.MethodInfo method)
    {
        var list = new List<string>();
        foreach (var attr in method.GetCustomAttributes(typeof(OldApiPermissionAttribute), inherit: true).Cast<OldApiPermissionAttribute>())
            list.Add(attr.PermissionId);
        if (method.DeclaringType != null)
        {
            foreach (var attr in method.DeclaringType.GetCustomAttributes(typeof(OldApiPermissionAttribute), inherit: true).Cast<OldApiPermissionAttribute>())
                list.Add(attr.PermissionId);
        }
        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// JWT 登录态：把特性上的旧 GUID 换成菜单新权限码再查；已是带冒号的码则原样用。
    /// </summary>
    private static async Task<OldApiResult> CheckJwtPermissionAsync(IServiceProvider sp, System.Reflection.MethodInfo method)
    {
        var required = GetRequiredPermissions(method);
        if (required.Count == 0)
            return null;

        var checker = sp.GetService<IPermissionChecker>();
        if (checker == null)
            return OldApiResult.NeedPermission("#没有权限#");

        foreach (var raw in required)
        {
            var code = MapToMenuPermissionCode(raw);
            if (!await checker.IsGrantedAsync(code))
                return OldApiResult.NeedPermission(OldApiPermissionEvaluator.BuildPermissionDeniedMessage(code));
        }

        return null;
    }

    /// <summary>
    /// 旧权限 GUID → 菜单权限码（用户已拍板：新壳只用新码）。
    /// </summary>
    /// <param name="permissionId">特性里的权限标识</param>
    /// <returns>给 IPermissionChecker 用的码</returns>
    internal static string MapToMenuPermissionCode(string permissionId)
    {
        if (string.IsNullOrWhiteSpace(permissionId))
            return permissionId;
        if (permissionId.Contains(':', StringComparison.Ordinal))
            return permissionId;

        // 会员列表（UserMemberFormAppService.MemberListPermissionId）
        if (string.Equals(permissionId, "1e7cc96020b843b988ed6df39024867e", StringComparison.OrdinalIgnoreCase))
            return "site:user:member:list";
        // 会员分组列表（UserGroupService.GroupListPermissionId）
        if (string.Equals(permissionId, "30301d7ff5224fddbdecf18b6c1f4a65", StringComparison.OrdinalIgnoreCase))
            return "site:user:member:group";

        return permissionId;
    }

    /// <summary>
    /// 写出旧版 JSON 并短路。
    /// </summary>
    private static async Task WriteOldApiAsync(ActionExecutingContext context, OldApiResult result)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(result, OldApiContract.JsonOptions);
        await context.HttpContext.Response.WriteAsync(json);
        context.Result = new EmptyResult();
    }

    /// <summary>按路径判断这是哪一端的兼容接口。</summary>
    public enum AuthFace
    {
        /// <summary>不是兼容路径。</summary>
        Unknown,
        /// <summary>会员接口。</summary>
        Member,
        /// <summary>站点后台。</summary>
        SiteAdmin,
        /// <summary>总后台。</summary>
        SystemAdmin
    }
}
