using Furion.AspNetCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统文件服务 🧩。文件存到哪由 <see cref="IFileStorage"/> 决定，默认实现是本地磁盘。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "file", Order = 410, Description = "系统文件")]
public class FileAppService : IDynamicApiController, ITransient
{
    private const string LocalBucketName = "Local";

    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysFile> _sysFileRep;
    private readonly UploadOptions _uploadOptions;
    private readonly string _imageType = ".jpeg.jpg.png.bmp.gif.tif";
    private readonly IFileStorage _fileStorage;

    /// <summary>
    /// 注入当前用户、文件仓储、上传配置和文件存储。
    /// </summary>
    /// <param name="userManager">当前用户</param>
    /// <param name="sysFileRep">文件仓储</param>
    /// <param name="uploadOptions">上传配置</param>
    /// <param name="fileStorage">文件存储</param>
    public FileAppService(UserManager userManager,
        SqlSugarRepository<SysFile> sysFileRep,
        IOptions<UploadOptions> uploadOptions,
        IFileStorage fileStorage)
    {
        _userManager = userManager;
        _sysFileRep = sysFileRep;
        _uploadOptions = uploadOptions.Value;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// 获取文件分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取文件分页列表")]
    public async Task<SqlSugarPagedList<SysFile>> Page(PageFileInput input)
    {
        // 获取所有公开附件
        var publicList = _sysFileRep.AsQueryable().ClearFilter().Where(u => u.IsPublic == true);
        // 获取私有附件
        var privateList = _sysFileRep.AsQueryable().Where(u => u.IsPublic == false);
        // 合并公开和私有附件并分页
        return await _sysFileRep.Context.UnionAll(publicList, privateList)
            .WhereIF(!string.IsNullOrWhiteSpace(input.FileName), u => u.FileName.Contains(input.FileName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.FilePath), u => u.FilePath.Contains(input.FilePath.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.StartTime.ToString()) && !string.IsNullOrWhiteSpace(input.EndTime.ToString()),
                u => u.CreateTime >= input.StartTime && u.CreateTime <= input.EndTime)
            .OrderBy(u => u.CreateTime, OrderByType.Desc)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 上传文件Base64 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("上传文件Base64")]
    public async Task<SysFile> UploadFileFromBase64(UploadFileFromBase64Input input)
    {
        var pattern = @"data:(?<type>.+?);base64,(?<data>[^""]+)";
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var match = regex.Match(input.FileDataBase64);

        byte[] fileData = Convert.FromBase64String(match.Groups["data"].Value);
        var contentType = match.Groups["type"].Value;
        if (string.IsNullOrEmpty(input.FileName))
            input.FileName = $"{YitIdHelper.NextId()}.{contentType.AsSpan(contentType.LastIndexOf('/') + 1)}";

        using var ms = new MemoryStream();
        ms.Write(fileData);
        ms.Seek(0, SeekOrigin.Begin);
        IFormFile formFile = new FormFile(ms, 0, fileData.Length, "file", input.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        var uploadFileInput = input.Adapt<UploadFileInput>();
        uploadFileInput.File = formFile;
        return await UploadFile(uploadFileInput);
    }

    /// <summary>
    /// 上传多文件 🔖
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    [DisplayName("上传多文件")]
    public async Task<List<SysFile>> UploadFiles([Required] List<IFormFile> files)
    {
        var fileList = new List<SysFile>();
        foreach (var file in files)
        {
            var uploadedFile = await UploadFile(new UploadFileInput { File = file });
            fileList.Add(uploadedFile);
        }
        return fileList;
    }

    /// <summary>
    /// 根据文件Id或Url下载 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据文件Id或Url下载")]
    public async Task<IActionResult> DownloadFile(SysFile input)
    {
        var file = input.Id > 0 ? await GetFile(input.Id) : await _sysFileRep.CopyNew().GetFirstAsync(u => u.Url == input.Url);
        var fileName = HttpUtility.UrlEncode(file.FileName, Encoding.GetEncoding("UTF-8"));
        return await GetFileStreamResult(file, fileName);
    }

    /// <summary>
    /// 文件预览 🔖
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("文件预览")]
    public async Task<IActionResult> GetPreview([FromRoute] long id)
    {
        var file = await GetFile(id);
        //var fileName = HttpUtility.UrlEncode(file.FileName, Encoding.GetEncoding("UTF-8"));
        return await GetFileStreamResult(file, file.Id + "");
    }

    /// <summary>
    /// 把文件内容包装成下载结果。
    /// </summary>
    /// <param name="file">文件记录</param>
    /// <param name="fileName">下载文件名（不含后缀）</param>
    /// <returns>文件下载结果</returns>
    private async Task<IActionResult> GetFileStreamResult(SysFile file, string fileName)
    {
        var stream = await _fileStorage.OpenReadAsync(GetStorageKey(file));
        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = fileName + file.Suffix };
    }

    /// <summary>
    /// 打开文件读取流，调用方负责释放。
    /// </summary>
    /// <param name="file">文件记录</param>
    /// <returns>文件读取流</returns>
    [NonAction]
    public Task<Stream> GetFileStream(SysFile file)
    {
        return _fileStorage.OpenReadAsync(GetStorageKey(file));
    }

    /// <summary>
    /// 文件记录对应的存储键：存储目录 + 文件Id + 后缀。
    /// </summary>
    /// <param name="file">文件记录</param>
    /// <returns>存储键</returns>
    private static string GetStorageKey(SysFile file) => $"{file.FilePath}/{file.Id}{file.Suffix}";

    /// <summary>
    /// 下载指定文件Base64格式 🔖
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    [DisplayName("下载指定文件Base64格式")]
    public async Task<string> DownloadFileBase64([FromBody] string url)
    {
        var sysFile = await _sysFileRep.CopyNew().GetFirstAsync(u => u.Url == url) ?? throw Oops.Oh($"文件不存在");
        await using var stream = await _fileStorage.OpenReadAsync(GetStorageKey(sysFile));
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return Convert.ToBase64String(buffer.ToArray());
    }

    /// <summary>
    /// 删除文件 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除文件")]
    public async Task DeleteFile(BaseIdInput input)
    {
        var file = await _sysFileRep.GetByIdAsync(input.Id) ?? throw Oops.Oh($"文件不存在");
        await _sysFileRep.DeleteAsync(file);
        await _fileStorage.DeleteAsync(GetStorageKey(file));
    }

    /// <summary>
    /// 更新文件 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新文件")]
    public async Task UpdateFile(SysFile input)
    {
        var isExist = await _sysFileRep.IsAnyAsync(u => u.Id == input.Id);
        if (!isExist) throw Oops.Oh(ErrorCodeEnum.D8000);

        await _sysFileRep.UpdateAsync(input);
    }

    /// <summary>
    /// 获取文件 🔖
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [DisplayName("获取文件")]
    public async Task<SysFile> GetFile([FromQuery] long id)
    {
        var file = await _sysFileRep.CopyNew().GetByIdAsync(id);
        return file ?? throw Oops.Oh(ErrorCodeEnum.D8000);
    }

    /// <summary>
    /// 根据文件Id集合获取文件 🔖
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [DisplayName("根据文件Id集合获取文件")]
    public async Task<List<SysFile>> GetFileByIds([FromQuery][FlexibleArray<long>] List<long> ids)
    {
        return await _sysFileRep.AsQueryable().Where(u => ids.Contains(u.Id)).ToListAsync();
    }

    /// <summary>
    /// 获取文件路径 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取文件路径")]
    public async Task<List<TreeNode>> GetFolder()
    {
        // 优化：直接在数据库层面获取不重复的文件路径
        var folders = await _sysFileRep.AsQueryable()
            .Select(u => u.FilePath)
            .Distinct()
            .ToListAsync();

        var pathTreeBuilder = new PathTreeBuilder();
        var tree = pathTreeBuilder.BuildTree(folders);
        return tree.Children;
    }

    /// <summary>
    /// 上传文件 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <param name="targetPath">存储目标路径</param>
    /// <returns></returns>
    [DisplayName("上传文件")]
    public async Task<SysFile> UploadFile([FromForm] UploadFileInput input, [BindNever] string targetPath = "")
    {
        if (input.File == null || input.File.Length <= 0) throw Oops.Oh(ErrorCodeEnum.D8000);

        if (input.File.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw Oops.Oh(ErrorCodeEnum.D8005);

        // 判断是否重复上传的文件
        var sizeKb = input.File.Length / 1024; // 大小KB
        var fileMd5 = string.Empty;
        if (_uploadOptions.EnableMd5)
        {
            await using (var fileStream = input.File.OpenReadStream())
            {
                // 存 Base64 形式的 MD5，和历史数据保持同一格式，重复上传判断才对得上
                fileMd5 = Convert.ToBase64String(await System.Security.Cryptography.MD5.HashDataAsync(fileStream));
            }
            // Mysql8 中如果使用了 utf8mb4_general_ci 之外的编码会出错，尽量避免在条件里使用.ToString()
            // 因为 Squsugar 并不是把变量转换为字符串来构造SQL语句，而是构造了CAST(123 AS CHAR)这样的语句，这样这个返回值是utf8mb4_general_ci，所以容易出错。
            var sysFile = await _sysFileRep.GetFirstAsync(u => u.FileMd5 == fileMd5 && u.SizeKb == sizeKb);
            if (sysFile != null) return sysFile;
        }

        // 验证文件类型
        if (!_uploadOptions.ContentType.Contains(input.File.ContentType)) throw Oops.Oh($"{ErrorCodeEnum.D8001}:{input.File.ContentType}");

        // 验证文件大小
        if (sizeKb > _uploadOptions.MaxSize) throw Oops.Oh($"{ErrorCodeEnum.D8002}，允许最大：{_uploadOptions.MaxSize}KB");

        // 获取文件后缀
        var suffix = Path.GetExtension(input.File.FileName).ToLower(); // 后缀
        if (string.IsNullOrWhiteSpace(suffix))
            suffix = string.Concat(".", input.File.ContentType.AsSpan(input.File.ContentType.LastIndexOf('/') + 1));
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            //var contentTypeProvider = FS.GetFileExtensionContentTypeProvider();
            //suffix = contentTypeProvider.Mappings.FirstOrDefault(u => u.Value == file.ContentType).Key;
            // 修改 image/jpeg 类型返回的 .jpeg、jpe 后缀
            if (suffix == ".jpeg" || suffix == ".jpe")
                suffix = ".jpg";
        }
        if (string.IsNullOrWhiteSpace(suffix)) throw Oops.Oh(ErrorCodeEnum.D8003);

        // 防止客户端伪造文件类型
        if (!string.IsNullOrWhiteSpace(input.AllowSuffix) && !input.AllowSuffix.Contains(suffix)) throw Oops.Oh(ErrorCodeEnum.D8003);
        //if (!VerifyFileExtensionName.IsSameType(file.OpenReadStream(), suffix)) throw Oops.Oh(ErrorCodeEnum.D8001);

        // 文件存储位置
        var path = string.IsNullOrWhiteSpace(targetPath) ? _uploadOptions.Path : targetPath;
        path = path.ParseToDateTimeForRep();

        var newFile = input.Adapt<SysFile>();
        newFile.Id = YitIdHelper.NextId();

        // 优先使用调用方指定的存储桶名称，没有指定时记为本地存储
        newFile.BucketName = string.IsNullOrEmpty(input.BucketName) ? LocalBucketName : input.BucketName;

        newFile.FileName = Path.GetFileNameWithoutExtension(input.File.FileName);
        newFile.Suffix = suffix;
        newFile.SizeKb = sizeKb;
        newFile.FilePath = path;
        newFile.FileMd5 = fileMd5;
        newFile.DataId = input.DataId;

        var storageKey = GetStorageKey(newFile);
        await using (var content = input.File.OpenReadStream())
        {
            await _fileStorage.SaveAsync(storageKey, content);
        }
        newFile.Provider = _fileStorage.Name;
        newFile.Url = _fileStorage.GetUrl(storageKey);

        await _sysFileRep.AsInsertable(newFile).ExecuteCommandAsync();
        return newFile;
    }

    /// <summary>
    /// 上传头像 🔖
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [DisplayName("上传头像")]
    public async Task<SysFile> UploadAvatar([Required] IFormFile file)
    {
        var sysFile = await UploadFile(new UploadFileInput { File = file, AllowSuffix = _imageType }, "upload/avatar");

        var sysUserRep = _sysFileRep.ChangeRepository<SqlSugarRepository<SysUser>>();
        var user = await sysUserRep.GetByIdAsync(_userManager.UserId);
        await sysUserRep.UpdateAsync(u => new SysUser() { Avatar = sysFile.Url }, u => u.Id == user.Id);
        // 删除已有头像文件
        if (!string.IsNullOrWhiteSpace(user.Avatar))
        {
            var fileId = Path.GetFileNameWithoutExtension(user.Avatar);
            if (long.TryParse(fileId, out var id))
            {
                try
                {
                    await DeleteFile(new BaseIdInput { Id = id });
                }
                catch
                {
                    // 忽略删除旧头像文件的错误，不影响新头像上传
                }
            }
        }

        return sysFile;
    }

    /// <summary>
    /// 上传电子签名 🔖
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [DisplayName("上传电子签名")]
    public async Task<SysFile> UploadSignature([Required] IFormFile file)
    {
        var sysFile = await UploadFile(new UploadFileInput { File = file, AllowSuffix = _imageType }, "upload/signature");

        var sysUserRep = _sysFileRep.ChangeRepository<SqlSugarRepository<SysUser>>();
        var user = await sysUserRep.GetByIdAsync(_userManager.UserId);
        // 删除已有电子签名文件
        if (!string.IsNullOrWhiteSpace(user.Signature) && user.Signature.EndsWith(".png"))
        {
            var fileId = Path.GetFileNameWithoutExtension(user.Signature);
            if (long.TryParse(fileId, out var id))
            {
                try
                {
                    await DeleteFile(new BaseIdInput { Id = id });
                }
                catch
                {
                    // 忽略删除旧签名文件的错误，不影响新签名上传
                }
            }
        }
        await sysUserRep.UpdateAsync(u => new SysUser() { Signature = sysFile.Url }, u => u.Id == user.Id);
        return sysFile;
    }

    #region 统一实体与文件关联时，业务应用实体只需要定义一个SysFile集合导航属性，业务增加和更新、删除分别调用即可

    /// <summary>
    /// 更新文件的业务数据Id
    /// </summary>
    /// <param name="dataId"></param>
    /// <param name="sysFiles"></param>
    /// <returns></returns>
    [NonAction]
    public async Task UpdateFileByDataId(long dataId, List<SysFile> sysFiles)
    {
        var newFileIds = sysFiles.Select(u => u.Id).ToList();

        // 求文件Id差集并删除（无效文件）
        var tmpFiles = await _sysFileRep.GetListAsync(u => u.DataId == dataId);
        var tmpFileIds = tmpFiles.Select(u => u.Id).ToList();
        var deleteFileIds = tmpFileIds.Except(newFileIds);
        foreach (var fileId in deleteFileIds)
            await DeleteFile(new BaseIdInput() { Id = fileId });

        await _sysFileRep.UpdateAsync(u => new SysFile() { DataId = dataId }, u => newFileIds.Contains(u.Id));
    }

    /// <summary>
    /// 删除业务数据对应的文件
    /// </summary>
    /// <param name="dataId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DeleteFileByDataId(long dataId)
    {
        // 删除冗余无效的物理文件
        var tmpFiles = await _sysFileRep.GetListAsync(u => u.DataId == dataId);
        foreach (var file in tmpFiles)
            await _fileStorage.DeleteAsync(GetStorageKey(file));
        await _sysFileRep.AsDeleteable().Where(u => u.DataId == dataId).ExecuteCommandAsync();
    }

    #endregion 统一实体与文件关联时，业务应用实体只需要定义一个SysFile集合导航属性，业务增加和更新、删除分别调用即可
}