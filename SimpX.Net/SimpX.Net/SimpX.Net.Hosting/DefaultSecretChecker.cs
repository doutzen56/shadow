using Furion;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SimpX.Net.Hosting;

/// <summary>
/// 生产环境默认密钥检查：仓库里的 JWT 签名密钥、国密私钥、宿主内部调用密钥、调度看板密钥都是公开的，
/// 谁拿到源码都能伪造令牌或签名。生产环境发现还在用这些默认值就直接启动失败，开发和测试环境不拦。
/// </summary>
internal static class DefaultSecretChecker
{
    /// <summary>
    /// 配置项和它在仓库里默认值的 SHA-256。只存摘要，不在代码里再写一遍密钥原文。
    /// </summary>
    private static readonly (string Key, string DefaultSha256)[] KnownDefaults =
    {
        ("JWTSettings:IssuerSigningKey", "e2d2fe941268558b57f598d600a54cab69fa8486540ace8f4feb8c019b244d0e"),
        ("Cryptogram:PrivateKey", "619ae1cc32c7350af0c7d397afe52f9d33e0818326971f28cfb6b0dfd94aa235"),
        ("InternalCall:AccessSecret", "de985a8768365f9dedcf242e6b482000216b2a9c35b647b7592b6abf5124294e"),
        ("ScheduleHost:DashboardAppSecret", "5e04f719d6b6f1479fb21b170f34f867456242c52200038524735e1d6160948c"),
    };

    /// <summary>
    /// 生产环境下检查上面这些配置是否还是仓库默认值，是就抛异常让宿主启动失败。
    /// 只报配置项名称，不把密钥值写进异常或日志。
    /// </summary>
    /// <param name="environment">宿主环境</param>
    /// <exception cref="InvalidOperationException">生产环境仍在使用默认密钥</exception>
    public static void EnsureChangedInProduction(IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        var stillDefault = KnownDefaults
            .Where(d =>
            {
                var value = App.Configuration[d.Key];
                return !string.IsNullOrEmpty(value) && Sha256Hex(value) == d.DefaultSha256;
            })
            .Select(d => d.Key)
            .ToList();

        if (stillDefault.Count > 0)
            throw new InvalidOperationException(
                $"生产环境还在使用仓库里的默认密钥，请换成自己生成的值后再启动：{string.Join("、", stillDefault)}。" +
                "改国密密钥时记得同步前端的 VITE_SM_PUBLIC_KEY；改内部调用密钥时各宿主要一起改。");
    }

    /// <summary>
    /// 计算字符串（UTF-8）的 SHA-256 小写十六进制。
    /// </summary>
    /// <param name="value">原文</param>
    /// <returns>摘要</returns>
    private static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
