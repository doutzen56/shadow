using Newtonsoft.Json;
using System.Security.Claims;

namespace SimpX.Net.Core;

/// <summary>
/// 防止重复请求过滤器特性(此特性使用了分布式锁，需确保系统支持分布式锁)
/// </summary>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// 请求间隔秒数。实际过期时间以幂等缓存定义为准（默认 5 秒），这里保留是为了已有特性写法还能编译。
    /// </summary>
    public int IntervalTime { get; set; } = 5;

    /// <summary>
    /// 错误提示内容
    /// </summary>
    public string Message { get; set; } = "你操作频率过快，请稍后重试！";

    /// <summary>
    /// 拼在请求指纹前面的额外标识。一般留空。
    /// </summary>
    public string CacheKey { get; set; } = "";

    /// <summary>
    /// 是否直接抛出异常：Ture是，False返回上次请求结果
    /// </summary>
    public bool ThrowBah { get; set; }

    /// <summary>
    /// 锁前缀
    /// </summary>
    public string LockPrefix { get; set; } = "lock_";

    public IdempotentAttribute()
    {
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path.Value.ToString();
        var userId = httpContext.User?.FindFirstValue(ClaimConst.UserId);

        var parameters = JsonConvert.SerializeObject(context.ActionArguments, Formatting.None, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include
        });

        var fingerprint = MD5Encryption.Encrypt($"{path}{userId}{parameters}");
        var cacheId = string.IsNullOrEmpty(CacheKey) ? fingerprint : $"{CacheKey}{fingerprint}";
        var state = httpContext.RequestServices.GetRequiredService<ICacheStateStore>();
        try
        {
            using var distributedLock = state.AcquireLock(CommonCacheKeys.Lock, $"{LockPrefix}{cacheId}");

            var cacheValue = state.Get<ResponseData>(CommonCacheKeys.Idempotent, cacheId);
            if (cacheValue != null)
            {
                if (ThrowBah) throw Oops.Oh(Message);
                context.Result = new ObjectResult(cacheValue.Value);
                return;
            }
            else
            {
                var resultContext = await next();
                // 缓存请求结果,null值不缓存。过期时间按幂等缓存定义，IntervalTime 只在和定义一致时才对得上。
                if (resultContext.Result is ObjectResult { Value: { } } objectResult)
                {
                    var typeName = objectResult.Value.GetType().Name;
                    var responseData = new ResponseData
                    {
                        Type = typeName,
                        Value = objectResult.Value
                    };
                    state.Add(CommonCacheKeys.Idempotent, cacheId, responseData, TimeSpan.FromSeconds(5));
                }
            }
        }
        catch (Exception ex)
        {
            throw Oops.Oh($"{Message}-{ex}");
        }
    }

    /// <summary>
    /// 请求结果数据
    /// </summary>
    private class ResponseData
    {
        /// <summary>
        /// 结果类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 请求结果
        /// </summary>
        public dynamic Value { get; set; }
    }
}