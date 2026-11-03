using SimpX.Net.Contracts.Jobs;
using Furion;
using Furion.Schedule;
using Furion.SpecificationDocument;
using Furion.VirtualFileServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using SimpX.Net.Core;
using SixLabors.ImageSharp.Web.DependencyInjection;
using System;
using System.IO;

namespace SimpX.Net.Hosting;

/// <summary>
/// 请求处理链骨架：所有宿主共用，中间件顺序只在这里定。
/// 可选能力：调度看板看有没有注册调度器，图片处理和静态文件看有没有 wwwroot，Hub 只映射已加载的。
/// </summary>
public sealed class HostPipelineComponent : IApplicationComponent
{
    /// <summary>
    /// 按固定顺序装配中间件：压缩 → 转发头 → 异常 → 静态文件 → 状态码 → 多语言 → 路由 → 跨域 → 认证授权 → 限流 → 看板 → 接口文档 → 终结点。
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">宿主环境</param>
    /// <param name="componentContext">组件上下文</param>
    public void Load(IApplicationBuilder app, IWebHostEnvironment env, ComponentContext componentContext)
    {
        JwtAuthenticationExtensions.EnsureAuthorizationRegistered(app.ApplicationServices);
        DefaultSecretChecker.EnsureChangedInProduction(env);

        var appName = env.ApplicationName;
        app.UseResponseCompression();
        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("SimpX.Net", "SimpX.Net");
            await next();
        });

        var caps = HostCapabilities.Current;

        // 图片处理和静态文件：点选了 ImageSharp 且有站点根目录才挂；只提供接口的宿主不必放空 wwwroot
        if (!string.IsNullOrEmpty(env.WebRootPath) && Directory.Exists(env.WebRootPath))
        {
            if (caps.ImageSharp)
                app.UseImageSharp();

            if (caps.VirtualFileServer)
            {
                app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = FS.GetFileExtensionContentTypeProvider() });
                if (!string.IsNullOrEmpty(App.Settings.VirtualPath))
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        RequestPath = App.Settings.VirtualPath,
                        FileProvider = env.WebRootFileProvider
                    });
                }
            }
            else
            {
                app.UseStaticFiles();
            }
        }

        app.UseUnifyResultStatusCodes();

        // 多语言要放在 UseRouting 之前；跨域要放在 UseRouting 和 UseAuthentication 之间
        if (caps.Localization)
            app.UseAppLocalization();
        app.UseRouting();
        app.UseCorsAccessor();
        // 按域名解析租户 / 站点都要在认证前：登录和不用登录的接口也要认当前租户和站点；两者并列，互不改写
        if (caps.ResolveTenantByHost)
            app.UseMiddleware<TenantHostResolveMiddleware>();
        if (caps.ResolveSiteByHost)
            app.UseMiddleware<SiteHostResolveMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.ApplicationServices.GetService<ISchedulerFactory>() != null)
        {
            // 看板登录凭据用这个密钥签名，写死在代码里等于人人都知道，所以从配置读
            var dashboardAppSecret = App.GetConfig<string>("ScheduleHost:DashboardAppSecret");
            if (string.IsNullOrEmpty(dashboardAppSecret) || dashboardAppSecret.Length < 16)
                throw new InvalidOperationException("调度看板需要配置 ScheduleHost:DashboardAppSecret，至少 16 位");

            app.UseScheduleUI(options =>
            {
                options.RequestPath = "/schedule";
                options.DisableOnProduction = false;
                options.DisplayEmptyTriggerJobs = true;
                options.DisplayHead = false;
                options.DefaultExpandAllJobs = false;
                options.EnableDirectoryBrowsing = false;
                options.Title = $"定时任务看板 ({appName})";
                options.LoginConfig.OnLoging = async (username, password, httpContext) =>
                {
                    var login = httpContext.RequestServices.GetService<IScheduleDashboardLogin>();
                    return login != null && await login.ValidateAsync(username, password);
                };
                options.LoginConfig.DefaultUsername = "";
                options.LoginConfig.DefaultPassword = "";
                options.LoginConfig.AppSecret = dashboardAppSecret;
            });
        }

        // 是否输出接口文档由 Furion 自带的 AppSettings:InjectSpecificationDocument 控制
        app.UseInject(string.Empty, options =>
        {
            options.ConfigureSwagger(m => m.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
        });

        app.UseEndpoints(endpoints =>
        {
            if (App.GetConfig<bool>("AppSettings:InjectSpecificationDocument", true))
            {
                endpoints.MapScalarApiReference("sapi", options =>
                {
                    options.WithTitle(appName);
                    foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
                        options.AddDocument(groupInfo.Group, groupInfo.Title, groupInfo.RouteTemplate);
                });
            }

            if (caps.SignalR)
                endpoints.MapHubs();
            endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
