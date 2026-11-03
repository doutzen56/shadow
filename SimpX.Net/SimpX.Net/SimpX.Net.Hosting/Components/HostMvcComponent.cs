using Furion;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using SimpX.Net.Core;
using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpX.Net.Hosting;

/// <summary>
/// MVC 与 Furion 注入（动态 API、统一结果、接口文档、JSON 规则）。依赖公共服务组件先加载。
/// </summary>
[DependsOn(typeof(HostServicesComponent))]
public sealed class HostMvcComponent : IServiceComponent
{
    /// <summary>
    /// 注册 MVC、Furion 注入和全局 JSON 规则。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IServiceCollection services, ComponentContext componentContext)
    {
        var mvc = services.AddControllersWithViews(options =>
            {
                options.Filters.Add<CacheRemoveFilter>();
            });

        if (HostCapabilities.Current.Localization)
            mvc.AddAppLocalization();

        mvc.AddInjectWithUnifyResult<AdminResultProvider>()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.AddDateTimeConverters("yyyy-MM-dd HH:mm:ss", localized: true);
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // 中文不转义
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.Converters.AddLongConverters(overMaxLengthOf17: true); // 超过 17 位的 long 转字符串，避免前端丢精度
                options.JsonSerializerOptions.Converters.AddDateOnlyConverters("yyyy-MM-dd");
                options.JsonSerializerOptions.Converters.AddTimeOnlyConverters("HH:mm:ss");
                options.JsonSerializerOptions.Converters.AddClayConverters();
                options.JsonSerializerOptions.Converters.AddEnumConverters();
                options.JsonSerializerOptions.Converters.AddDataTableConverters();
                options.JsonSerializerOptions.Converters.AddDataSetConverters();
            })
            .ConfigureApplicationPartManager(RemoveExcludedAssemblies);
    }

    /// <summary>
    /// 把 AppSettings:ExcludeAssemblies 里的程序集移出控制器来源。
    /// Furion 的排除只管它自己的扫描，ASP.NET 仍会把被引用的程序集当控制器来源，不移出就会把别的宿主的接口暴露出来。
    /// </summary>
    /// <param name="manager">MVC 程序集部件管理器</param>
    private static void RemoveExcludedAssemblies(ApplicationPartManager manager)
    {
        var excluded = App.Settings.ExcludeAssemblies ?? Array.Empty<string>();
        if (excluded.Length == 0) return;

        var parts = manager.ApplicationParts.Where(p => excluded.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        foreach (var part in parts) manager.ApplicationParts.Remove(part);
    }
}
