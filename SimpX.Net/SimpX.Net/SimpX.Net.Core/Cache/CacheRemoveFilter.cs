using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SimpX.Net.Caching;
using System.Reflection;

namespace SimpX.Net.Core;

/// <summary>
/// 读取方法上的 CacheRemoveAttribute，接口成功后执行待失效清单。
/// Order 必须小于 UnitOfWork 的 9999，才能包在事务外层、在提交之后跑。
/// </summary>
public sealed class CacheRemoveFilter : IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// 排在 Furion UnitOfWork（9999）外侧。
    /// </summary>
    public int Order => 9998;

    /// <summary>
    /// 执行业务；成功则把 Attribute 登记进清单并执行（无事务或事务已在内层提交）。
    /// </summary>
    /// <param name="context">执行上下文</param>
    /// <param name="next">请求处理链下一环</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var batch = context.HttpContext.RequestServices.GetService<CacheInvalidationBatch>();
        var executed = await next();

        if (batch is null)
            return;

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            batch.Discard();
            return;
        }

        var method = context.ActionDescriptor.EndpointMetadata
            .OfType<MethodInfo>()
            .FirstOrDefault()
            ?? (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo;

        // Furion 动态 API：从 EndpointMetadata / ControllerActionDescriptor 取
        method ??= GetDynamicApiMethod(context);

        if (method is not null)
        {
            var attrs = method.GetCustomAttributes<CacheRemoveAttribute>(inherit: true).ToArray();
            if (attrs.Length > 0)
            {
                var args = context.ActionArguments.Values.ToArray();
                // ActionArguments 是字典，顺序不一定等于参数顺序；按参数名对齐
                var parameters = method.GetParameters();
                var ordered = new object?[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var name = parameters[i].Name;
                    if (name is not null && context.ActionArguments.TryGetValue(name, out var val))
                        ordered[i] = val;
                }

                foreach (var attr in attrs)
                {
                    var value = CacheAttributeArgumentResolver.Resolve(method, ordered!, attr);
                    batch.Add(attr, value);
                }
            }
        }

        await batch.ExecuteAsync(context.HttpContext.RequestAborted);
    }

    private static MethodInfo? GetDynamicApiMethod(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor cad)
            return cad.MethodInfo;
        return null;
    }
}
