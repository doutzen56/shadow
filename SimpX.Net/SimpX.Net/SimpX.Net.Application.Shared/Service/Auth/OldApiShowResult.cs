using System.Collections;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧版 IHandlerBase.ShowResult 的 info 形态：RecordCount / list（及可选分页、data、other）。
/// </summary>
public static class OldApiShowResult
{
    /// <summary>
    /// 无分页列表（对齐 IEnumerable ShowResult：RecordCount/data/other/list）。
    /// </summary>
    /// <param name="list">当前页或全量行</param>
    /// <param name="data">附加 data</param>
    /// <param name="other">附加 other</param>
    /// <returns>旧版 info</returns>
    public static object OfList(IEnumerable list, object data = null, object other = null)
    {
        var rows = list?.Cast<object>().ToList() ?? [];
        return new
        {
            RecordCount = rows.Count,
            data,
            other,
            list = rows
        };
    }

    /// <summary>
    /// 分页列表（对齐 IOrderedQueryable ShowResult：RecordCount/PageIndex/PageSize/data/list）。
    /// </summary>
    /// <param name="pageRows">当前页行</param>
    /// <param name="recordCount">总条数</param>
    /// <param name="pageIndex">页码（从 1）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="data">附加 data</param>
    /// <returns>旧版 info</returns>
    public static object OfPage(
        IEnumerable pageRows,
        int recordCount,
        int pageIndex,
        int pageSize,
        object data = null)
    {
        var rows = pageRows?.Cast<object>().ToList() ?? [];
        return new
        {
            RecordCount = recordCount,
            PageIndex = pageIndex < 1 ? 1 : pageIndex,
            PageSize = pageSize < 1 ? 20 : pageSize,
            data,
            list = rows
        };
    }

    /// <summary>
    /// 从全量序列切一页再包 OfPage。
    /// </summary>
    /// <param name="all">全量（已排序）</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="data">附加 data</param>
    /// <returns>旧版 info</returns>
    public static object SlicePage(IEnumerable all, int pageIndex, int pageSize, object data = null)
    {
        var rows = all?.Cast<object>().ToList() ?? [];
        var index = pageIndex < 1 ? 1 : pageIndex;
        var size = pageSize < 1 ? 20 : Math.Min(pageSize, 5000);
        var page = rows.Skip((index - 1) * size).Take(size).ToList();
        return OfPage(page, rows.Count, index, size, data);
    }

    /// <summary>
    /// 读旧版 PageIndex / PageSize（默认 1 / 20，页大小上限 5000）。
    /// </summary>
    /// <param name="http">当前请求</param>
    /// <returns>页码与页大小</returns>
    public static (int PageIndex, int PageSize) ReadPaging(Microsoft.AspNetCore.Http.HttpContext http)
    {
        var pageIndex = Math.Max(1, OldApiRequestParam.ReadInt(http, "PageIndex", 1));
        if (pageIndex == 1)
        {
            var alt = OldApiRequestParam.ReadInt(http, "pageIndex", 0);
            if (alt > 0)
                pageIndex = alt;
            else
            {
                alt = OldApiRequestParam.ReadInt(http, "page", 0);
                if (alt > 0)
                    pageIndex = alt;
            }
        }

        var pageSize = OldApiRequestParam.ReadInt(http, "PageSize", 0);
        if (pageSize <= 0)
            pageSize = OldApiRequestParam.ReadInt(http, "pageSize", 20);
        if (pageSize <= 0)
            pageSize = 20;
        pageSize = Math.Min(Math.Max(1, pageSize), 5000);
        return (pageIndex, pageSize);
    }
}
