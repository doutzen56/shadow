using SimpX.Net.Application.Shared;

namespace SimpX.Net.Application.Admin;

/// <summary>
/// surveyAdmin 矩阵缺口旧动作名。
/// </summary>
public partial class SurveyAdminAppService
{

    /// <summary>
    /// 对齐旧 answerinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Answerinfo"), HttpPost]
    [DisplayName("answerinfo")]
    public Task<object?> Answerinfo(Dictionary<string, object?>? input = null) =>
        _svc.AnswerinfoAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 pay。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Pay"), HttpPost]
    [DisplayName("pay")]
    public Task<object?> Pay(Dictionary<string, object?>? input = null) =>
        _svc.PayAsync(input, App.HttpContext?.RequestAborted ?? default);


    /// <summary>
    /// 对齐旧 surveryinfo。
    /// </summary>
    /// <param name="input">表单（兼容旧 QF）</param>
    /// <returns>旧版信息</returns>
    [ApiDescriptionSettings(Name = "Surveryinfo"), HttpPost]
    [DisplayName("surveryinfo")]
    public Task<object?> Surveryinfo(Dictionary<string, object?>? input = null) =>
        _svc.SurveryinfoAsync(input, App.HttpContext?.RequestAborted ?? default);

}
