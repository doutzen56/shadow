using Lazy.Captcha.Core;
using Lazy.Captcha.Core.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpX.Net.Feature.Captcha;

/// <summary>
/// 图形验证码存储：写到统一状态缓存的 <see cref="AuthCacheKeys.Captcha"/>。
/// 缓存配成 Redis 时，多实例都能校验同一张验证码；配成内存时只在本进程有效。
/// </summary>
public sealed class CacheCaptchaStorage : IStorage
{
    private readonly ICacheStateStore _state;

    /// <summary>
    /// 注入状态缓存。
    /// </summary>
    /// <param name="state">状态缓存</param>
    public CacheCaptchaStorage(ICacheStateStore state)
    {
        _state = state;
    }

    /// <summary>
    /// 按验证码库给出的到期时间写入。不到 1 秒按 1 秒，避免时钟误差写成立刻过期。
    /// </summary>
    /// <param name="key">验证码编号</param>
    /// <param name="value">验证码文本</param>
    /// <param name="absoluteExpiration">到期时间</param>
    public void Set(string key, string value, DateTimeOffset absoluteExpiration)
    {
        var ttl = absoluteExpiration - DateTimeOffset.UtcNow;
        if (ttl < TimeSpan.FromSeconds(1))
            ttl = TimeSpan.FromSeconds(1);

        _state.Add(AuthCacheKeys.Captcha, key, value, ttl);
    }

    /// <summary>
    /// 读取验证码文本。不存在时返回 null，由验证码库判为校验失败。
    /// </summary>
    /// <param name="key">验证码编号</param>
    /// <returns>验证码文本</returns>
    public string Get(string key)
    {
        return _state.Get<string>(AuthCacheKeys.Captcha, key);
    }

    /// <summary>
    /// 校验用过或换一张时删掉。
    /// </summary>
    /// <param name="key">验证码编号</param>
    public void Remove(string key)
    {
        _state.Remove(AuthCacheKeys.Captcha, key);
    }
}

/// <summary>
/// 图形验证码注册。
/// </summary>
public static class CaptchaCacheServiceCollectionExtensions
{
    /// <summary>
    /// 注册图形验证码，并用统一状态缓存替换验证码库自带的内存存储。
    /// 这样 Redis 模式下各实例共用验证码，不用再单独接一套 Redis。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCaptchaStateStorage(this IServiceCollection services)
    {
        services.AddCaptcha();
        services.Replace(ServiceDescriptor.Singleton<IStorage, CacheCaptchaStorage>());
        return services;
    }
}
