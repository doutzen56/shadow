using Microsoft.AspNetCore.Http;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧路径图片上传。对齐 /admin/system/upload、/admin/site/app/upload。
/// </summary>
public sealed class SiteUploadService : ITransient
{
    private readonly IFileStorage _files;
    private readonly IHttpContextAccessor _http;
    private readonly ICurrentSiteAccessor _site;

    public SiteUploadService(IFileStorage files, IHttpContextAccessor http, ICurrentSiteAccessor site)
    {
        _files = files;
        _http = http;
        _site = site;
    }

    /// <summary>
    /// 从当前请求 Form.Files[0] 上传，返回旧版 Image/Url。
    /// </summary>
    public async Task<object> UploadAsync(CancellationToken cancellationToken = default)
    {
        _ = _site.RequireSiteId();
        var file = _http.HttpContext?.Request.Form.Files.FirstOrDefault()
            ?? throw Oops.Oh("#请选择要上传的文件#");
        if (file.Length <= 0)
            throw Oops.Oh("#请选择要上传的文件#");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".bin";
        ext = ext.ToLowerInvariant();
        var now = DateTime.Now;
        var key = $"upload/{now:yyyy}/{now:MM}/{now:dd}/{Guid.NewGuid():N}{ext}";
        await using var stream = file.OpenReadStream();
        await _files.SaveAsync(key, stream, cancellationToken);
        var url = "/" + _files.GetUrl(key).TrimStart('/');
        return new { Image = key, Url = url };
    }
}
