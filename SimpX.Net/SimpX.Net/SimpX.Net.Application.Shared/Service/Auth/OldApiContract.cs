using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧站兼容契约：字段名按声明写出（不驼峰）。
/// 由 <see cref="OldApiAuthAttribute"/> 在结果阶段统一套用，业务方法直接返回 <see cref="OldApiResult"/>。
/// </summary>
public static class OldApiContract
{
    /// <summary>兼容接口序列化：保留 ID / RecordCount 等声明名。</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>把动作结果里的 <see cref="OldApiResult"/> 写成声明名 JSON。</summary>
    public static void RewriteExecutedResult(ActionExecutedContext executed)
    {
        if (executed.Canceled || executed.Exception != null)
            return;

        OldApiResult? api = executed.Result switch
        {
            ObjectResult { Value: OldApiResult o } => o,
            JsonResult { Value: OldApiResult j } => j,
            _ => null
        };
        if (api == null)
            return;

        executed.Result = new JsonResult(api, JsonOptions)
        {
            ContentType = "application/json; charset=utf-8"
        };
    }
}
