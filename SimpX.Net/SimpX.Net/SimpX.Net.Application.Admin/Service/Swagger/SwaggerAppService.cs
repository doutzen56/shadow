using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpX.Net.Application.Admin;

[ApiDescriptionSettings(Module = "dev", Name = "swagger", Order = 100, Description = "Swagger")]
public class SwaggerAppService : IDynamicApiController, ITransient
{
    private readonly SwaggerGenOptions _genOptions;

    public SwaggerAppService(IOptions<SwaggerGenOptions> genOptions)
    {
        _genOptions = genOptions.Value;
    }

    [DisplayName("获取Swagger接口分组列表"), HttpGet]
    public async Task<IActionResult> GetSwaggerGroups()
    {
        var docs = _genOptions.SwaggerGeneratorOptions.SwaggerDocs;
        if (docs == null || !docs.Any()) Oops.Oh("No Swagger documents configured.");

        return new JsonResult(docs.Select(d => new
        {
            name = d.Key,
            url = $"/swagger/{d.Key}/swagger.json"
        }));
    }
}
