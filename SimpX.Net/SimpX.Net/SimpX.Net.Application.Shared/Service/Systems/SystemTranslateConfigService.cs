using SimpX.Net.Data.Entity.Systems;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 总后台语种翻译配置（sys_Config 键值）。对齐旧 GetTranslateUrl 相关配置读写，不做机翻调用。
/// </summary>
public sealed class SystemTranslateConfigService : ITransient
{
    /// <summary>语种翻译入口 URL。</summary>
    public const string KeyLanguageTranslation = "LanguageTranslation";

    /// <summary>支持语种列表（逗号分隔）。</summary>
    public const string KeySupportedLanguages = "Supportedlanguages";

    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 注入业务库。语种配置在 BetWin30。
    /// </summary>
    /// <param name="biz">业务库</param>
    public SystemTranslateConfigService(IBizDbAccessor biz) => _db = biz.Db;

    /// <summary>
    /// 读取配置，并可拼预览 URL。
    /// </summary>
    /// <param name="channel">频道；空则不拼 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置</returns>
    public async Task<SystemTranslateConfigDto> GetAsync(
        string channel = null,
        CancellationToken cancellationToken = default)
    {
        var map = await LoadMapAsync(cancellationToken);
        map.TryGetValue(KeyLanguageTranslation, out var baseUrl);
        map.TryGetValue(KeySupportedLanguages, out var langs);
        baseUrl ??= string.Empty;
        langs ??= string.Empty;

        string url = null;
        if (!string.IsNullOrWhiteSpace(channel) && !string.IsNullOrWhiteSpace(baseUrl))
            url = $"{baseUrl}?channel={channel.Trim()}&language={langs}";

        return new SystemTranslateConfigDto
        {
            LanguageTranslation = baseUrl,
            SupportedLanguages = langs,
            PreviewUrl = url
        };
    }

    /// <summary>
    /// 保存语种翻译相关键。
    /// </summary>
    /// <param name="input">表单</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SaveAsync(SystemTranslateConfigSaveInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await UpsertAsync(KeyLanguageTranslation, input.LanguageTranslation ?? string.Empty, cancellationToken);
        await UpsertAsync(KeySupportedLanguages, input.SupportedLanguages ?? string.Empty, cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SystemConfigEntry>()
            .Where(c => c.Name == KeyLanguageTranslation || c.Name == KeySupportedLanguages)
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Name, r => r.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private async Task UpsertAsync(string name, string value, CancellationToken cancellationToken)
    {
        var rows = await _db.Queryable<SystemConfigEntry>()
            .Where(c => c.Name == name).Take(1).ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        if (row is null)
        {
            await _db.Insertable(new SystemConfigEntry { Name = name, Value = value })
                .ExecuteCommandAsync(cancellationToken);
            return;
        }

        row.Value = value;
        await _db.Updateable(row).ExecuteCommandAsync(cancellationToken);
    }
}

/// <summary>语种翻译配置。</summary>
public sealed class SystemTranslateConfigDto
{
    /// <summary>翻译入口 URL。</summary>
    public string LanguageTranslation { get; set; }

    /// <summary>支持语种。</summary>
    public string SupportedLanguages { get; set; }

    /// <summary>按频道拼出的预览地址；可空。</summary>
    public string PreviewUrl { get; set; }
}

/// <summary>保存语种翻译配置。</summary>
public sealed class SystemTranslateConfigSaveInput
{
    /// <summary>翻译入口 URL。</summary>
    public string LanguageTranslation { get; set; }

    /// <summary>支持语种（逗号分隔）。</summary>
    public string SupportedLanguages { get; set; }
}
