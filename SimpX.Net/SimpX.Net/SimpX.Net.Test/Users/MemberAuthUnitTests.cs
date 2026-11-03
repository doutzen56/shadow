using SimpX.Net.Application.Shared;
using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 会员兼容：密码哈希与旧版返回格式（不连库）。
/// </summary>
public class MemberAuthUnitTests
{
    /// <summary>
    /// MD5 结果为大写 32 位十六进制，与 SP.Studio toMD5 一致。
    /// </summary>
    [Fact]
    public void PasswordHasher_ProducesUpperHexMd5()
    {
        var hash = Md5PasswordHasher.Hash("abc");
        Assert.Equal(32, hash.Length);
        Assert.Equal(hash.ToUpperInvariant(), hash);
        Assert.True(Md5PasswordHasher.Matches("abc", hash));
        Assert.True(Md5PasswordHasher.Matches("abc", hash.ToLowerInvariant()));
        Assert.False(Md5PasswordHasher.Matches("abd", hash));
    }

    /// <summary>
    /// 旧版返回字段名为 success/msg/info；Login/Permission 错误类型常量可用。
    /// </summary>
    [Fact]
    public void OldApiResult_FieldsAndErrorTypes()
    {
        var ok = OldApiResult.Ok("#登录成功#", new { Token = "x" });
        Assert.Equal(1, ok.Success);
        Assert.Equal("#登录成功#", ok.Msg);
        Assert.NotNull(ok.Info);

        var fail = OldApiResult.Fail("#用户名/密码错误#");
        Assert.Equal(0, fail.Success);

        var needLogin = OldApiResult.NeedLogin();
        Assert.Equal(0, needLogin.Success);
        var err = Assert.IsType<OldApiErrorInfo>(needLogin.Info);
        Assert.Equal(OldApiErrorType.Login, err.ErrorType);

        Assert.Equal("USERINFO", MemberSessionKeys.UserInfoCookie);
        Assert.Equal("TOKEN", AdminSessionKeys.Token);
        Assert.Equal("SYSTEM_ADMIN_TOKEN", SystemSessionKeys.Token);
    }

    /// <summary>
    /// TOTP：当前窗口生成的码应能通过校验；空码失败。
    /// </summary>
    [Fact]
    public void TotpValidator_RoundTrip_CurrentWindow()
    {
        var secret = Guid.NewGuid().ToString("N");
        Assert.False(GoogleTotpValidator.Validate(secret, null));
        var pin = GoogleTotpValidator.GenerateCurrentPin(secret);
        Assert.Equal(6, pin.Length);
        Assert.True(GoogleTotpValidator.Validate(secret, pin));
    }
}
