using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// 系统通用服务：接口清单、国密密钥对、加密等（登录后可用）。压测已从租户后台拿掉。
/// </summary>
[ApiDescriptionSettings(Module = "system", Name = "common", Order = 101)]
public class CommonAppService : IDynamicApiController, ITransient
{
    private readonly IApiDescriptionGroupCollectionProvider _apiProvider;

    /// <summary>
    /// 注入接口元数据。
    /// </summary>
    /// <param name="apiProvider">接口元数据</param>
    public CommonAppService(IApiDescriptionGroupCollectionProvider apiProvider)
    {
        _apiProvider = apiProvider;
    }

    /// <summary>
    /// 获取国密公钥私钥对
    /// </summary>
    /// <returns>密钥对</returns>
    [DisplayName("获取国密公钥私钥对")]
    public SmKeyPairOutput GetSmKeyPair()
    {
        var kp = GM.GenerateKeyPair();
        var privateKey = Hex.ToHexString(((ECPrivateKeyParameters)kp.Private).D.ToByteArray()).ToUpper();
        var publicKey = Hex.ToHexString(((ECPublicKeyParameters)kp.Public).Q.GetEncoded()).ToUpper();

        return new SmKeyPairOutput
        {
            PrivateKey = privateKey,
            PublicKey = publicKey,
        };
    }

    /// <summary>
    /// 获取所有接口/动态API
    /// </summary>
    /// <returns>接口列表</returns>
    [DisplayName("获取所有接口/动态API")]
    public List<ApiOutput> GetApiList()
    {
        var apiList = new List<ApiOutput>();
        foreach (var item in _apiProvider.ApiDescriptionGroups.Items)
        {
            foreach (var apiDescription in item.Items)
            {
                var displayName = apiDescription.TryGetMethodInfo(out MethodInfo apiMethodInfo) ? apiMethodInfo.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName : "";

                apiList.Add(new ApiOutput
                {
                    GroupName = item.GroupName,
                    DisplayName = displayName,
                    RouteName = apiDescription.RelativePath
                });
            }
        }
        return apiList;
    }

    /// <summary>
    /// 下载标记错误的临时Excel（全局）
    /// </summary>
    /// <param name="fileName">下载文件名</param>
    /// <returns>文件结果</returns>
    [DisplayName("下载标记错误的临时Excel（全局）")]
    public async Task<IActionResult> DownloadErrorExcelTemp([FromQuery] string fileName = null)
    {
        var userId = App.User?.FindFirst(ClaimConst.UserId)?.Value;
        var bytes = App.GetRequiredService<ICacheStateStore>().Get<byte[]>(CommonCacheKeys.ExcelTemp, userId ?? string.Empty);

        if (bytes == null || bytes.Length == 0) throw Oops.Oh("错误标记文件已过期。");

        return await Task.FromResult(new FileContentResult(bytes, "application/octet-stream")
        {
            FileDownloadName = $"{(string.IsNullOrEmpty(fileName) ? "错误标记＿" + DateTime.Now.ToString("yyyyMMddhhmmss") : fileName)}.xlsx"
        });
    }

    /// <summary>
    /// 加密字符串
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <returns>密文</returns>
    [SuppressMonitor]
    [DisplayName("加密字符串")]
    public dynamic EncryptPlainText([Required] string plainText)
    {
        return CryptogramUtil.Encrypt(plainText);
    }
}
