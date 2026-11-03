using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SimpX.Net.Data.Entity.Sites;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 后台会话 Shared。对齐旧 /admin/info/*。
/// </summary>
public sealed partial class AdminSessionService : ITransient
{
    /// <summary>站点停用（旧 Site.SiteStauts.Stop=1；Normal=0 Maintain=2）。</summary>
    private const int SiteStatusStop = 1;

    private readonly ISqlSugarClient _db;
    private readonly ICurrentSiteAccessor _site;
    private readonly SiteAdminAuthService _auth;
    private readonly SiteAdminMenuService _menus;
    private readonly IHttpContextAccessor _http;

    /// <summary>注入。</summary>
    public AdminSessionService(
        IBizDbAccessor biz,
        ICurrentSiteAccessor site,
        SiteAdminAuthService auth,
        SiteAdminMenuService menus,
        IHttpContextAccessor http)
    {
        _db = biz.Db;
        _site = site;
        _auth = auth;
        _menus = menus;
        _http = http;
    }

    /// <summary>对齐旧 channel。</summary>
    public Task<object?> ChannelAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return ChannelCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 face（上传延后）。</summary>
    public Task<object?> FaceAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return FaceCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 get。</summary>
    public Task<object?> GetAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 getcode。</summary>
    public Task<object?> GetcodeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return GetcodeCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 init。</summary>
    public Task<object?> InitAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return InitCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 newnotify。</summary>
    public Task<object?> NewnotifyAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return NewnotifyCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 password。</summary>
    public Task<object?> PasswordAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return PasswordCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 save。</summary>
    public Task<object?> SaveAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SaveCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 savecode。</summary>
    public Task<object?> SavecodeAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return SavecodeCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 test。</summary>
    public Task<object?> TestAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return TestCoreAsync(input, cancellationToken);
    }

    /// <summary>对齐旧 menuall。</summary>
    public Task<object?> MenuallAsync(
        Dictionary<string, object?>? input = null,
        CancellationToken cancellationToken = default)
    {
        input ??= new Dictionary<string, object?>();
        return MenuallCoreAsync(input, cancellationToken);
    }

    private async Task<object?> ChannelCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = await RequireAdminAsync(cancellationToken);
        return new
        {
            Setting = "{}",
            Channel = "[]"
        };
    }

    private async Task<object?> FaceCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        var face = input.TryGetValue("Face", out var f) ? f?.ToString()?.Trim()
            : input.TryGetValue("Url", out var u) ? u?.ToString()?.Trim()
            : null;
        if (!string.IsNullOrEmpty(face))
        {
            admin.Face = face;
            await _db.Updateable(admin).UpdateColumns(a => a.Face).ExecuteCommandAsync(cancellationToken);
        }

        return new { Face = admin.Face ?? string.Empty };
    }

    private async Task<object?> GetCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var admin = await RequireAdminAsync(cancellationToken);
        var menu = await _menus.GetMenuAsync(cancellationToken);
        object menuInfo = menu.Info ?? "[]";
        return new
        {
            admin.ID,
            Name = admin.NickName ?? admin.AdminName ?? string.Empty,
            NickName = admin.NickName ?? string.Empty,
            admin.AdminName,
            Face = admin.Face ?? string.Empty,
            Sign = admin.Sign ?? string.Empty,
            IsCode = admin.SecretKey != Guid.Empty,
            Menu = menuInfo
        };
    }

    private async Task<object?> GetcodeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.SecretKey != Guid.Empty)
            throw Oops.Oh("#已绑定谷歌验证码#");
        var siteId = _site.RequireSiteId();
        var key = Guid.NewGuid().ToString("N");
        var otpauth =
            $"otpauth://totp/{Uri.EscapeDataString(siteId + ":" + admin.AdminName)}?secret={key}&issuer={Uri.EscapeDataString(siteId.ToString())}";
        return new
        {
            SecretKey = key,
            Code = otpauth
        };
    }

    private async Task<object?> InitCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var siteId = _site.RequireSiteId();
        var sites = await _db.Queryable<Site>()
            .Where(s => s.ID == siteId)
            .Take(1)
            .ToListAsync(cancellationToken);
        var site = sites.FirstOrDefault() ?? throw Oops.Oh("#站点不存在#");
        if (site.Status == SiteStatusStop)
            throw Oops.Oh("#站点已停用#");

        return new
        {
            SiteID = site.ID,
            site.Name,
            site.Status,
            Enum = "{}"
        };
    }

    private Task<object?> NewnotifyCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        return Task.FromResult<object?>(new { NewMessage = 0 });
    }

    private async Task<object?> PasswordCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        var oldPwd = ReadString(input, "Password");
        var n1 = ReadString(input, "NewPassword1", "NewPassword");
        var n2 = ReadString(input, "NewPassword2");
        if (string.IsNullOrEmpty(n1) || n1 != n2)
            throw Oops.Oh("#两次密码输入不一致#");
        if (!Md5PasswordHasher.Matches(oldPwd, admin.Password))
            throw Oops.Oh("#原密码不正确#");
        admin.Password = Md5PasswordHasher.Hash(n1);
        await _db.Updateable(admin).UpdateColumns(a => a.Password).ExecuteCommandAsync(cancellationToken);
        return null;
    }

    private async Task<object?> SaveCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        admin.NickName = ReadString(input, "NickName");
        admin.Sign = ReadString(input, "Sign");
        await _db.Updateable(admin)
            .UpdateColumns(a => new { a.NickName, a.Sign })
            .ExecuteCommandAsync(cancellationToken);
        return null;
    }

    private async Task<object?> SavecodeCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        var secret = ReadString(input, "SecretKey");
        var code = ReadString(input, "Code");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
            throw Oops.Oh("#参数错误#");
        if (!Guid.TryParseExact(secret, "N", out var key) && !Guid.TryParse(secret, out key))
        {
            // 允许 32 位 hex；解析失败时用前 32 拼 Guid
            var hex = secret.Length >= 32 ? secret[..32] : secret.PadRight(32, '0');
            if (!Guid.TryParseExact(hex, "N", out key))
                throw Oops.Oh("#密钥格式错误#");
        }

        if (!GoogleTotpValidator.Validate(key.ToString("N"), code))
            throw Oops.Oh("#验证码错误#");

        admin.SecretKey = key;
        await _db.Updateable(admin).UpdateColumns(a => a.SecretKey).ExecuteCommandAsync(cancellationToken);
        return null;
    }

    private Task<object?> TestCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        _ = cancellationToken;
        var scheme = _http.HttpContext?.Request.Scheme ?? "http";
        return Task.FromResult<object?>(scheme);
    }

    private async Task<object?> MenuallCoreAsync(Dictionary<string, object?> input, CancellationToken cancellationToken)
    {
        _ = input;
        var menu = await _menus.GetMenuAsync(cancellationToken);
        if (menu.Success != 1)
            throw Oops.Oh(menu.Msg ?? "#请先登录#");
        return menu.Info;
    }

    private async Task<SiteAdmin> RequireAdminAsync(CancellationToken cancellationToken)
    {
        var admin = await _auth.TryGetCurrentAdminAsync(cancellationToken);
        return admin ?? throw Oops.Oh("#请先登录#");
    }

    private static string ReadString(Dictionary<string, object?> input, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!input.TryGetValue(k, out var v) || v is null) continue;
            var s = v switch
            {
                JsonElement je => je.ValueKind == JsonValueKind.String ? je.GetString() : je.ToString(),
                _ => v.ToString()
            };
            if (!string.IsNullOrEmpty(s))
                return s;
        }

        return string.Empty;
    }
}
