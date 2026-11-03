using System.Security.Claims;
using System.Security.Cryptography;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 开放接口身份服务 🧩
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "openAccess", Order = 244)]
public class OpenAccessAppService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<SysOpenAccess> _sysOpenAccessRep;
    private readonly ISimpXCache _cache;

    /// <summary>
    /// 开放接口身份服务构造函数
    /// </summary>
    public OpenAccessAppService(SqlSugarRepository<SysOpenAccess> sysOpenAccessRep,
        ISimpXCache cache)
    {
        _sysOpenAccessRep = sysOpenAccessRep;
        _cache = cache;
    }

    /// <summary>
    /// 生成签名
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("生成签名")]
    public string GenerateSignature(GenerateSignatureInput input)
    {
        // 密钥
        var appSecretByte = Encoding.UTF8.GetBytes(input.AccessSecret);

        // 拼接参数
        var parameter = $"{input.Method.ToString().ToUpper()}&{input.Url}&{input.AccessKey}&{input.Timestamp}&{input.Nonce}";
        // 使用 HMAC-SHA256 协议创建基于哈希的消息身份验证代码 (HMAC)，以appSecretByte 作为密钥，对上面拼接的参数进行计算签名，所得签名进行 Base-64 编码
        using HMAC hmac = new HMACSHA256();
        hmac.Key = appSecretByte;
        var sign = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(parameter)));
        return sign;
    }

    /// <summary>
    /// 获取开放接口身份分页列表 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取开放接口身份分页列表")]
    public async Task<SqlSugarPagedList<OpenAccessOutput>> Page(OpenAccessInput input)
    {
        return await _sysOpenAccessRep.AsQueryable()
            .LeftJoin<SysUser>((u, a) => u.BindUserId == a.Id)
            .LeftJoin<SysTenant>((u, a, b) => u.BindTenantId == b.Id)
            .LeftJoin<SysOrg>((u, a, b, c) => b.OrgId == c.Id)
            .WhereIF(!string.IsNullOrWhiteSpace(input.AccessKey?.Trim()), (u, a, b, c) => u.AccessKey.Contains(input.AccessKey))
            .Select((u, a, b, c) => new OpenAccessOutput
            {
                BindUserAccount = a.Account,
                BindTenantName = c.Name,
            }, true)
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加开放接口身份 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加开放接口身份")]
    public async Task AddOpenAccess(AddOpenAccessInput input)
    {
        if (await _sysOpenAccessRep.AsQueryable().AnyAsync(u => u.AccessKey == input.AccessKey && u.Id != input.Id))
            throw Oops.Oh(ErrorCodeEnum.O1000);

        var openAccess = input.Adapt<SysOpenAccess>();
        await _sysOpenAccessRep.InsertAsync(openAccess);
    }

    /// <summary>
    /// 更新开放接口身份 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新开放接口身份")]
    public async Task UpdateOpenAccess(UpdateOpenAccessInput input)
    {
        if (await _sysOpenAccessRep.AsQueryable().AnyAsync(u => u.AccessKey == input.AccessKey && u.Id != input.Id))
            throw Oops.Oh(ErrorCodeEnum.O1000);

        var openAccess = input.Adapt<SysOpenAccess>();
        await _sysOpenAccessRep.UpdateAsync(openAccess);
        _cache.Remove(TenantCacheKeys.OpenAccess, openAccess.AccessKey);
    }

    /// <summary>
    /// 删除开放接口身份 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除开放接口身份")]
    public async Task DeleteOpenAccess(DeleteOpenAccessInput input)
    {
        var openAccess = await _sysOpenAccessRep.GetFirstAsync(u => u.Id == input.Id);
        await _sysOpenAccessRep.DeleteAsync(u => u.Id == input.Id);
        if (openAccess != null)
            _cache.Remove(TenantCacheKeys.OpenAccess, openAccess.AccessKey);
    }

    /// <summary>
    /// 创建密钥 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("创建密钥")]
    public async Task<string> CreateSecret()
    {
        return await Task.FromResult(Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..^2]);
    }

    /// <summary>
    /// 根据 Key 获取对象
    /// </summary>
    /// <param name="accessKey"></param>
    /// <returns></returns>
    /// <summary>
    /// 按 AccessKey 取开放接口身份（转 Shared 实现）。
    /// </summary>
    /// <param name="accessKey">访问键</param>
    /// <returns>开放接口身份</returns>
    [NonAction]
    public Task<SysOpenAccess> GetByKey(string accessKey)
        => App.GetRequiredService<OpenAccessSignatureService>().GetByKey(accessKey);
}