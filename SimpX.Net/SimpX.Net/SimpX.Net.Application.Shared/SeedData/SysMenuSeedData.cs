namespace SimpX.Net.Application.Shared;

/// <summary>
/// 系统菜单表种子数据（租户后台框架菜单；业务菜单见 SiteAdminMenuSeedData）。
/// 工作台、开发工具、帮助文档、关于项目已从租户后台拿掉。
/// </summary>
public class SysMenuSeedData : ISqlSugarEntitySeedData<SysMenu>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysMenu> HasData()
    {
        return new[]
        {
            #region 系统管理

            new SysMenu{ Id=1300200000101, Pid=0, Title="系统管理", Path="/system", Name="system", Component="Layout", Icon="ele-Setting", Type=MenuTypeEnum.Dir, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=10000 },

            // 账号管理
            new SysMenu{ Id=1300200010101, Pid=1300200000101, Title="账号管理", Path="/system/user", Name="sysUser", Component="/system/user/index", Icon="ele-User", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010201, Pid=1300200010101, Title="查询", Permission="system:user:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010301, Pid=1300200010101, Title="编辑", Permission="system:user:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010401, Pid=1300200010101, Title="增加", Permission="system:user:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010501, Pid=1300200010101, Title="删除", Permission="system:user:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010701, Pid=1300200010101, Title="授权角色", Permission="system:user:grantRole", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010801, Pid=1300200010101, Title="重置密码", Permission="system:user:resetPwd", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200010901, Pid=1300200010101, Title="设置状态", Permission="system:user:setStatus", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200011001, Pid=1300200010101, Title="强制下线", Permission="system:onlineUser:forceOffline", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200011101, Pid=1300200010101, Title="解除锁定", Permission="system:user:unlockLogin", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // 角色管理
            new SysMenu{ Id=1300200020101, Pid=1300200000101, Title="角色管理", Path="/system/role", Name="sysRole", Component="/system/role/index", Icon="ele-Help", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=110 },
            new SysMenu{ Id=1300200020201, Pid=1300200020101, Title="查询", Permission="system:role:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020301, Pid=1300200020101, Title="编辑", Permission="system:role:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020401, Pid=1300200020101, Title="增加", Permission="system:role:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020501, Pid=1300200020101, Title="删除", Permission="system:role:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020601, Pid=1300200020101, Title="授权菜单", Permission="system:role:grantMenu", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020701, Pid=1300200020101, Title="授权数据", Permission="system:role:grantDataScope", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200020801, Pid=1300200020101, Title="设置状态", Permission="system:role:setStatus", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // 机构管理
            new SysMenu{ Id=1300200030101, Pid=1300200000101, Title="机构管理", Path="/system/org", Name="sysOrg", Component="/system/org/index", Icon="ele-OfficeBuilding", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=120 },
            new SysMenu{ Id=1300200030201, Pid=1300200030101, Title="查询", Permission="system:org:list", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200030301, Pid=1300200030101, Title="编辑", Permission="system:org:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200030401, Pid=1300200030101, Title="增加", Permission="system:org:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200030501, Pid=1300200030101, Title="删除", Permission="system:org:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // 职位管理
            new SysMenu{ Id=1300200040101, Pid=1300200000101, Title="职位管理", Path="/system/pos", Name="sysPos", Component="/system/pos/index",Icon="ele-Mug", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=130 },
            new SysMenu{ Id=1300200040201, Pid=1300200040101, Title="查询", Permission="system:pos:list", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200040301, Pid=1300200040101, Title="编辑", Permission="system:pos:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200040401, Pid=1300200040101, Title="增加", Permission="system:pos:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200040501, Pid=1300200040101, Title="删除", Permission="system:pos:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // 个人中心
            new SysMenu{ Id=1300200050101, Pid=1300200000101, Title="个人中心", Path="/system/userCenter", Name="sysUserCenter", Component="/system/user/component/userCenter",Icon="ele-Medal", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=140 },
            new SysMenu{ Id=1300200050201, Pid=1300200050101, Title="修改密码", Permission="system:user:changePwd", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200050301, Pid=1300200050101, Title="基本信息", Permission="system:user:baseInfo", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200050401, Pid=1300200050101, Title="电子签名", Permission="system:file:uploadSignature", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200050501, Pid=1300200050101, Title="上传头像", Permission="system:file:uploadAvatar", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // 通知公告
            new SysMenu{ Id=1300200060101, Pid=1300200000101, Title="通知公告", Path="/system/notice", Name="sysNotice", Component="/system/notice/index",Icon="ele-Bell", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=150 },
            new SysMenu{ Id=1300200060201, Pid=1300200060101, Title="查询", Permission="system:notice:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200060301, Pid=1300200060101, Title="编辑", Permission="system:notice:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200060401, Pid=1300200060101, Title="增加", Permission="system:notice:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200060501, Pid=1300200060101, Title="删除", Permission="system:notice:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200060601, Pid=1300200060101, Title="发布", Permission="system:notice:public", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            // AD域配置
            new SysMenu{ Id=1300200080101, Pid=1300200000101, Title="AD域配置", Path="/system/ldap", Name="sysLdap", Component="/system/ldap/index",Icon="ele-Place", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=170 },
            new SysMenu{ Id=1300200080201, Pid=1300200080101, Title="查询", Permission="system:ldap:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200080301, Pid=1300200080101, Title="详情", Permission="system:ldap:detail", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=110 },
            new SysMenu{ Id=1300200080401, Pid=1300200080101, Title="编辑", Permission="system:ldap:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=120 },
            new SysMenu{ Id=1300200080501, Pid=1300200080101, Title="增加", Permission="system:ldap:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=130 },
            new SysMenu{ Id=1300200080601, Pid=1300200080101, Title="删除", Permission="system:ldap:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=140 },
            new SysMenu{ Id=1300200080701, Pid=1300200080101, Title="同步域账户", Permission="system:ldap:syncUser", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=150 },
            new SysMenu{ Id=1300200080801, Pid=1300200080101, Title="同步域组织", Permission="system:ldap:syncDept", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=160 },

            // 系统参数、语言、翻译、文件、开放接口、缓存
            new SysMenu{ Id=1300200090101, Pid=1300200000101, Title="系统参数", Path="/system/tenantConfig", Name="sysTenantConfig", Component="/system/tenantConfig/index", Icon="ele-DocumentCopy", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=180 },
            new SysMenu{ Id=1300200090201, Pid=1300200090101, Title="查询", Permission="system:tenantConfig:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200090301, Pid=1300200090101, Title="编辑", Permission="system:tenantConfig:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200090401, Pid=1300200090101, Title="增加", Permission="system:tenantConfig:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200090501, Pid=1300200090101, Title="删除", Permission="system:tenantConfig:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200090601, Pid=1300200090101, Title="批量删除", Permission="system:tenantConfig:batchDelete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300200100101, Pid=1300200000101, Title="语言管理", Path="/system/lang", Name="sysLang", Component="/system/lang/index", Icon="iconfont icon-diqiu", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2025-06-28 00:00:00"), OrderNo=190 },
            new SysMenu{ Id=1300200100201, Pid=1300200100101, Title="查询", Permission="system:lang:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200100301, Pid=1300200100101, Title="编辑", Permission="system:lang:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200100401, Pid=1300200100101, Title="增加", Permission="system:lang:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200100501, Pid=1300200100101, Title="删除", Permission="system:lang:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200100601, Pid=1300200100101, Title="批量删除", Permission="system:lang:batchDelete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300200200101, Pid=1300200000101, Title="翻译管理", Path="/system/langText", Name="sysLangText", Component="/system/langText/index", Icon="iconfont icon-zhongyingwen", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2025-06-28 00:00:00"), OrderNo=200 },
            new SysMenu{ Id=1300200200201, Pid=1300200200101, Title="查询", Permission="system:langText:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200301, Pid=1300200200101, Title="编辑", Permission="system:langText:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200401, Pid=1300200200101, Title="增加", Permission="system:langText:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200501, Pid=1300200200101, Title="删除", Permission="system:langText:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200601, Pid=1300200200101, Title="批量删除", Permission="system:langText:batchDelete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200701, Pid=1300200200101, Title="导出", Permission="system:langText:export", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200200801, Pid=1300200200101, Title="导入", Permission="system:langText:import", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300200110101, Pid=1300200000101, Title="文件管理", Path="/system/file", Name="sysFile", Component="/system/file/index", Icon="ele-Folder", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=210 },
            new SysMenu{ Id=1300200110201, Pid=1300200110101, Title="查询", Permission="system:file:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200110301, Pid=1300200110101, Title="上传", Permission="system:file:uploadFile", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200110401, Pid=1300200110101, Title="下载", Permission="system:file:downloadFile", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200110501, Pid=1300200110101, Title="编辑", Permission="system:file:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200110601, Pid=1300200110101, Title="删除", Permission="system:file:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200120101, Pid=1300200000101, Title="开放接口", Path="/system/openAccess", Name="sysOpenAccess", Component="/system/openAccess/index", Icon="ele-Key", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=220 },
            new SysMenu{ Id=1300200120201, Pid=1300200120101, Title="查询", Permission="system:openAccess:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200120301, Pid=1300200120101, Title="增加", Permission="system:openAccess:add", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200120401, Pid=1300200120101, Title="编辑", Permission="system:openAccess:update", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300200120501, Pid=1300200120101, Title="删除", Permission="system:openAccess:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300300080101, Pid=1300200000101, Title="缓存管理", Path="/system/cache", Name="sysCache", Component="/system/cache/index", Icon="ele-Loading", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=225 },
            new SysMenu{ Id=1300300080201, Pid=1300300080101, Title="查询", Permission="system:cache:keyList", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300300080301, Pid=1300300080101, Title="删除", Permission="system:cache:delete", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300300080401, Pid=1300300080101, Title="清空", Permission="system:cache:clear", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            #endregion 系统管理

            #region 日志管理

            new SysMenu{ Id=1300400000101, Pid=0, Title="日志管理", Path="/log", Name="log", Component="Layout", Icon="ele-DocumentCopy", Type=MenuTypeEnum.Dir, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=12000 },

            new SysMenu{ Id=1300400010101, Pid=1300400000101, Title="访问日志", Path="/log/vislog", Name="sysVisLog", Component="/system/log/vislog/index", Icon="ele-Document", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400010201, Pid=1300400010101, Title="查询", Permission="system:logVis:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400010301, Pid=1300400010101, Title="清空", Permission="system:logVis:clear", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300400020101, Pid=1300400000101, Title="操作日志", Path="/log/oplog", Name="sysOpLog", Component="/system/log/oplog/index", Icon="ele-Document", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=110 },
            new SysMenu{ Id=1300400020201, Pid=1300400020101, Title="查询", Permission="system:logOp:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400020301, Pid=1300400020101, Title="清空", Permission="system:logOp:clear", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400020401, Pid=1300400020101, Title="导出", Permission="system:logOp:export", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300400030101, Pid=1300400000101, Title="异常日志", Path="/log/exlog", Name="sysExLog", Component="/system/log/exlog/index", Icon="ele-Document", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=120 },
            new SysMenu{ Id=1300400030201, Pid=1300400030101, Title="查询", Permission="system:logEx:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400030301, Pid=1300400030101, Title="清空", Permission="system:logEx:clear", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },
            new SysMenu{ Id=1300400030401, Pid=1300400030101, Title="导出", Permission="system:logEx:export", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            new SysMenu{ Id=1300400040101, Pid=1300400000101, Title="差异日志", Path="/log/difflog", Name="sysDiffLog", Component="/system/log/difflog/index", Icon="ele-Document", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=130 },
            new SysMenu{ Id=1300400040201, Pid=1300400040101, Title="查询", Permission="system:logDiff:page", Type=MenuTypeEnum.Btn, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=100 },

            #endregion 日志管理
        };
    }
}
