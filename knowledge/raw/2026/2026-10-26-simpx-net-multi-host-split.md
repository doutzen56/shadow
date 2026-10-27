---
source_task: SimpX.Net 多宿主拆分（Admin / H5 / Job / Payment）
date: 2026-10-26
project: SimpX.Net（未建项目档案，按未知业务仓处理）
status: raw
confirmed_by: "@@TL"
user_reviewed: false
---

# SimpX.Net 多宿主拆分：原始摘要

## 目标

模块化单体按职责拆成四个宿主进程，共用 Core；Admin 不再跑定时任务，只远程管理。

## 结果

- 宿主与端口：Admin 5005、H5 5006、Job 5007、Payment 5008；nginx 前缀 `/admin-api`、`/h5-api`、`/job-api`、`/pay-api`。
- 宿主开关读 `Host` 配置段：`Role`、`EnableSchedule`、`EnableJobExecution`、`EnableScheduleUI`、`EnableSignalR`。
- 只有 Job 注册并启动调度；Admin、H5、Payment 都不注册调度。
- `SysJobService` 标 `[HostVisible(HostRole.Job)]`，只在 Job 上注册和暴露。管理端前端把 `/api/sysJob/*` 改走 `VITE_JOB_API_URL`（默认 `/job-api`）。
- 动态 API 按宿主隐藏：H5 只露 `SimpX.Net.Application.H5`，Payment 只露 `SimpX.Net.Application.Payment`，Core 后台接口只露给 Admin 和 Job。
- 微信支付、支付宝支付整块删除；Payment 只保留 `IPaymentChannel` 抽象和 `DemoPaymentChannel`。

## 学到的教训

1. 按命名空间整块从 DI 里删服务会打断依赖链：大量 `Sys*Service` 既是动态 API，又被其它服务注入（如 `SysCacheService`、`SysFileProviderService`）。正确做法是：接口隐藏用 MVC 约定（`IApplicationModelConvention`），DI 只删除显式标了宿主的类型。
2. 关掉某个宿主的调度时，仍注入 `ISchedulerFactory` 的服务（`SysJobService`）要么从这个宿主移除，要么保留调度注册；只关不删会导致启动时 DI 校验失败。
3. Core 服务构造函数依赖 SignalR Hub 上下文，所以所有宿主都要注册 SignalR 服务；是否对外映射 Hub 再单独开关。
4. 宿主没有 `wwwroot` 时 ImageSharp 中间件启动失败，侧宿主要放空 `wwwroot`。

## 追加：Core 拆分第 1、2 阶段（2026-10-26）

- Admin 不跑调度：`/api/sysJob` 由 Admin 的 `JobManagementProxyService`（`Name = "sysJob"`，方法名与 Job 一致）检查按钮权限后，用内部签名（`InternalSignature`，Core `InternalCall`）转发给 Job；Job 的作业接口只认内部签名。
- Admin 引用 `Application.Job` 只为拿协议类型：`AppSettings:ExcludeAssemblies` 不让 Furion 扫描，再加 `[HostVisible]` 兜底。宿主配置目录要放在 Host 项目，不能放在被引用的 Application 项目，否则会把 `App.json` 带进引用方的输出目录。
- 教训：Furion 下给动态接口加 `[ProducesResponseType]` 会让该接口跳过统一结果包装（直接返回原始对象），前端拿不到 `{code, result}`。Swagger 类型靠方法返回类型即可生成 `AdminResult<T>`。用户已裁定（2026-10-26）：`coding.mdc` 改为“要有 Swagger 文档，加法按项目现有做法”，不再强制 `ProducesResponseType`。

## 追加：通用框架重构（2026-10-26 晚）

- 入口统一为 `Host.*`（Web.Entry → Host.Admin），宿主公共层 Web.Core → `SimpX.Net.Hosting`；`HostLauncher.Run()` 统一启动，IP 库随 Hosting 复制到各宿主。
- 宿主间调用改用 Furion HttpRemote 声明式接口（`IHttpDeclarative` + `[HttpClientName]`），签名放在命名 HttpClient 的 `DelegatingHandler`，统一结果靠 Furion `HttpClientOptions.JsonResponseWrapper(typeof(AdminResult<>), "Result")` 自动拆包。声明式参数用 `[Body]`、`[QueryParam]`（`[Query]` 只能贴方法）。
- 路由分模块用 Furion `[ApiDescriptionSettings(Module = "system", Name = "job")]` → `/api/system/job/*`，权限码随路由变为 `system:job:*`。
- 教训：路由改名但菜单按钮权限码没同步时，`JwtHandler`“未登记即放行”会让普通用户访问到新路由（实测复现后已修）。改路由必须同一步改种子权限码；SysMenuSeedData 未标 IgnoreUpdateSeed，开发库启动即更新，生产库需 SQL。
- 教训：“启动时跑一次”的作业（清在线用户、缓存租户、枚举转字典）本质是宿主启动初始化，调度挪走后必须在 Admin 用 HostedService 做，否则内存缓存下 Admin 拿不到租户缓存。

## 追加：实体放置规范（用户 2026-10-26 拍板）

- 不建统一 Model 层。数据库实体、ES 文档、MQ 消息按“谁拥有”放：各 Application 内分 `Entity/`（数据库）、`Document/`（ES）、`Message/`（MQ）、`Dto/`。
- 只有跨宿主收发的消息才算契约，需要收发双方共享；契约目前放在提供方的 Application 里（如 `Application.Job/Contracts`）。
- 事件、消息一律用有名字的类型，不用匿名对象（匿名对象切到 Redis/RabbitMQ 跨宿主后，接收方没有类型可接）。
- 原因：项目启动时会扫描已加载的程序集，把扫到的实体全部建表、写种子；实体放公共层，每个宿主都会建所有的表。
- 教训：基础设施类事件（如告警邮件）的订阅者不能跟着业务领域走。拆分后 Job 发出的告警事件没人接收，已改为 Core 里的 `ErrorAlertMailSubscriber` + `IEmailSender` 统一处理。旧代码里数据库日志写入器发事件用的是 `nameof(CommonConst.SendErrorMail)`，和订阅名对不上，告警邮件从来没发出过，已一并修正。

## 追加：移除 HostRole（2026-10-26，A 步完成，组件化 B 步待做）

- 删除 HostRole / HostVisible / HostVisibility / HostApiConvention / HostServicePruner / HostOptions。宿主能力由引用的应用决定；认证由各应用 AppStartup 用 `AddJwtAuthentication<THandler>()` 注册；接口文档用 Furion 自带 `InjectSpecificationDocument`；调度看板按是否注册了 `ISchedulerFactory`；限流按是否带 Limit.json；标题用 `ApplicationName`。
- 教训 1：`ExcludeAssemblies` 只挡 Furion 扫描，挡不住 ASP.NET 把被引用程序集当控制器来源。Admin 引用 Application.Job 时要在 `ConfigureApplicationPartManager` 里移除该程序集，否则 Admin 暴露 `/api/internal/job/*`。
- 教训 2（高风险）：Furion `AddJwt` 必须在 `AddControllersWithViews/AddInject` 之前注册，否则按钮权限检查不执行，普通用户能访问所有接口（实测复现，403 变 200）。现在靠 Hosting `[AppStartup(0)]` 排在应用（100）之后保证顺序；组件化（B 步）时改由依赖声明保证。

## 追加：第三方登录改为抽象 + 演示实现（2026-10-26）

- 删除微信公众号/小程序/Gitee OAuth 全部代码、SKIT 与 AspNet.Security.OAuth.* 包、OAuth.json/Wechat.json、“三方账号”菜单与前端扫码登录。
- 新设计：Core `IExternalLoginProvider` + `ExternalUserInfo` + `ExternalLoginOptions`；默认实现 `DemoExternalLoginProvider`（只在 `ExternalLogin:EnableDemo=true` 时注册）；Admin `ExternalLoginAppService`（/api/system/externalLogin/signIn|callback）+ 绑定表 `SysUserExternalLogin`。
- 安全要点：state 一次性（缓存 10 分钟）；跳回地址白名单，空白名单只允许站内相对路径（拒绝 `//evil.com`）；自动建用户用随机密码（原实现用系统默认密码，任何人可用默认密码登录）；删除用户时同步删绑定。
- 实测：登录、二次登录同一用户、state 重放、外站跳回、未知提供方、默认密码登录、demo 关闭、普通权限 403，均符合预期；测试用户与绑定已清理。

## 追加：Hosting 组件化（B 步，2026-10-26）

- Hosting 不再有 AppStartup：`HostServicesComponent`（公共服务）、`HostMvcComponent`（MVC + Furion 注入）、`HostPipelineComponent`（唯一请求处理链骨架）；各宿主 `Program.cs` 写 `HostLauncher.Run<认证组件>()`（Admin/H5/内部签名三种认证组件）。
- 根因更正（高风险）：越权不是“AddJwt 要在 MVC 前”这么简单，而是 Furion 用 TryAdd 注册授权处理器和策略提供方，只要之前有任何代码（如应用 AppStartup 里的 `AddMvcCore()`）注册过 ASP.NET 默认授权服务，Furion 的就被跳过，`AppAuthorizeHandler` 根本不在容器里。Furion 执行顺序是 AppStartup 先于 RunOptions 组件，组件顺序无法保证。
- 修法：`AddJwtAuthentication` 里先 `RemoveAll<IAuthorizationPolicyProvider>()` 再 `AddJwt`，之后补注册缺失的授权处理器，与注册先后无关（Furion 的 `AppAuthorizationPolicyProvider` 是 internal，不能直接 Replace）。诊断方法：运行时打印 `IAuthorizationHandler` 列表和 `IAuthorizationPolicyProvider` 类型。
- 根治（用户 2026-10-26 拍板，替换上面的 RemoveAll 补丁）：抢先注册默认授权的有两处，一是 Admin Startup 的 `AddMvcCore()`，二是公共服务组件里的 `AddSignalR()`（ASP.NET 源码 `AddSignalRCore` 直接调 `services.AddAuthorization()`），四个宿主都中。RemoveAll 把两处都盖住了，只去掉 AddMvcCore 不够。现在：认证组件在 `HostLauncher` 里排在公共服务和 MVC 前面；排除程序集的控制器统一由 `HostMvcComponent` 按 `AppSettings:ExcludeAssemblies` 处理，应用层不再调 AddMvcCore；`HostPipelineComponent` 启动时调 `EnsureAuthorizationRegistered` 自检，策略提供方不是 Furion 的或没有 `AppAuthorizeHandler` 就直接启动失败。实测：只去掉 AddMvcCore 时四个宿主全被自检拦下；调整顺序后全部启动，普通用户 403、超管 200、无令牌 401、内部签名 401、Admin 不暴露 Job 接口、H5 会员令牌 200。
- 附带发现（原有，未改）：`OnlineUserHub` 没有 `[Authorize]`，Furion 全局授权只管控制器，不带令牌也能连上集线器，只是连接时不记在线用户。

## 追加：框架内不出现具体第三方平台名（用户 2026-10-26）

- 抽象层注释、示例、枚举值不写微信、钉钉、支付宝等具体平台名；接入说明写成“接入一家就新增一个实现并注册”。已清理：支付/第三方登录注释、`ConfigConst.AlipayAuthPageUrl`、`TemplateTypeEnum` 的三个平台值、前端 `api-plugins/dingTalk`、`goView`（内容实为钉钉接口）及打包脚本分支、api-services 中 OAuth/微信/小程序/支付宝的生成文件与导出行。
- 教训：删除前端生成的接口文件时要同步删 `api-services/api.ts`、`models/index.ts` 的导出行，并跑 `pnpm run build` 验证；上一轮漏了这一步，打包失败到这轮才发现。
- 注释规范自查：“配置节名”改为“从配置里的 xx 这一段读取”，“管道”改为“请求处理链”，“防重放”改为“同一请求不能重复提交”，“必须”改为“要/一定要”。

## 追加：短信、文件存储改抽象，删在线更新（2026-10-26）

- 短信：Core `ISmsSender` + `SmsMessage` + `SmsOptions`（配置段 `Sms`）；默认实现 `DemoSmsSender`，只在 `Sms:Demo:Enabled=true` 时注册，验证码固定，日志只记脱敏手机号。没有注册通道时发送直接报“未配置短信发送通道”。删除三家云短信实现与两个 SDK 包。
- 文件存储：只保留 `ICustomFileProvider` + `DefaultFileProvider`（本地磁盘），`SysFileService` 直接注入接口；删除对象存储、SSH 存储、多存储、存储提供方管理接口与实体、OnceMi 与 SSH.NET 包、docker 里的 MinIO 容器。原来算文件 MD5 用的是云存储 SDK 的工具方法（Base64 格式），换成 .NET 自带 MD5 时要保持 Base64，重复上传判断才对得上历史数据。
- 在线更新（拉代码仓库自动更新）整套删除：服务、帮助类、配置、缓存键、菜单种子、前端页面与生成接口。
- 坑：种子数据只增改不删，已上线库里“系统更新”菜单和存储提供方表还在，要在生产迁移脚本里删。
- 坑（安全）：Furion 全局请求日志（LoggingMonitor）会把请求地址和入参原样写日志。手机号放在路由里时 `IgnorePropertyNames` 管不到，要在动作上加 `[SuppressMonitor]`。已给发短信、校验验证码加上。
- 登录密码明文进日志（原有问题，用户 2026-10-26 拍板两种都做）：Core `LoggingSetup` 的 `IgnorePropertyNames` 加上 Password/PasswordOld/PasswordNew/AccessSecret/SecretKey/PrivateKey/AccessToken/RefreshToken（属性名匹配，swagger 登录的小写 password 也能去掉）；手机号登录加 `[SuppressMonitor]`。不加 `Code`，因为机构、角色、字典的编码也叫 Code，加了操作日志就没用了。实测后台登录、swagger 登录、H5 注册登录、手机号登录，日志里都搜不到密码、手机号、验证码、返回的令牌。
- 文件存储做成基础功能（用户 2026-10-26）：Core `Storage/` 放 `IFileStorage`（按存储键存取，不依赖业务表和 MVC）+ 默认 `LocalFileStorage`（站点根目录，拒绝 `..` 跳出存储根）+ `AddFileStorage()`（没注册才注册，应用在 Startup 里先注册别的实现即可覆盖），在公共服务组件里调用；`SysFileService` 只依赖 `IFileStorage`。用户先选“都放 Core 并保留腾讯云”，最后改口“不要了，默认给个本地存储”，所以不接任何云存储；项目无历史文件，不做按文件区分存储。
- AI 翻译改成 Core `ITextTranslator` + 默认 `ChatCompletionTextTranslator`（兼容 chat/completions 协议，配置段 `TextTranslator` 的地址、密钥、模型都填了才注册），路由 `aiTranslateText` 不变；配置样例 `TextTranslator.example`，真实 `TextTranslator.json` 入 gitignore。实测用本地模拟接口验证了请求格式、成功、上游报错三条路，密钥不进日志。
- 平台名清理范围要比“代码里的集成”更广：配置样例（smtp.163.com、xxx@qq.com）、注释（阿里云 Redis、阿里字体）、Dockerfile 镜像源、README、前端外链图片和带 key 的地图脚本注释、vite 翻译器注释（火山、有道）、前端多语言包（构建时自动清理不用的条目）。扫描时注意二进制文件（IP 库、锁屏图片）和随机 key（wx 开头）会误报。
- 文件头清理（用户 2026-10-26）：删掉 C# 版权头、前端生成接口里的项目宣传语。生成文件不能只改结果：swagger-codegen 3 的 typescript-axios 文件头来自 `licenseInfo` 子模板，在 `Web/api_build/templates/licenseInfo.mustache` 覆盖并在四个生成脚本里加 `-t`，用最小接口文档试生成确认生效。第三方库（Font Awesome、Waves）的许可证头按许可证要求保留。
- 安全排查已按用户拍板修复（2026-10-26）：集线器加 `[Authorize]` 并只保留连接、断开，管理操作走 HTTP；`SysCommonService` 去掉类级匿名；看板 AppSecret 改读 `JobHost:DashboardAppSecret`；Hosting 新增 `DefaultSecretChecker`（原名带 Guard，用户不喜欢，已改），生产环境用仓库默认密钥直接启动失败（代码里只存默认值的 SHA-256）；删广告和死代码页面及专用依赖；去掉第三方 HMAC 网站、上游作者 IP 菜单、配置样例内网 IP。实测：开发环境全量回归通过；生产环境默认密钥启动失败且日志不含密钥；换掉密钥后生产环境正常启动。项目里集线器令牌走查询参数 `token`，不是 SignalR 常见的 `access_token`。
- 项目改名（用户 2026-10-26）：H5→WebApi、Job→Schedule、Payment→Channels，Application 和 Host 各三个，目录、项目文件、命名空间、解决方案、引用、`ExcludeAssemblies`、launchSettings、docker-compose 同步；类名、路由、配置段名（JobHost、InternalCall:Services:Job）未改。整目录搬（连开发库一起），删掉旧 bin/obj 再 restore。
- 坑（改名暴露）：调度集群 Id 取宿主程序集名，改名后变成新节点；旧节点被强杀时没走 Stop，表里一直是 Working，新节点永远等待，作业不加载（报 scheduler of <job_log> is not found）。同名重启不受影响（启动先置 Waiting）。根因是集群没有心跳/超时接管，`UpdatedTime` 列没用上。上线迁移要把旧 ClusterId 置为 Crashed 或删掉；内置作业的 AssemblyName/JobType 会在加载时按代码自动更新，数据库里的脚本作业若写了旧命名空间要手改。
- 集群心跳已修（用户 2026-10-26 拍板）：工作中的节点每 `JobHost:ClusterHeartbeatSeconds`（默认 10 秒）写 `UpdatedTime`；等待中的节点把超过 `ClusterTimeoutSeconds`（默认 30 秒，至少 2 倍间隔）没心跳的工作中节点标成已宕机再接管；节点写心跳时发现自己已不是工作中（被接管），调 `ISchedulerFactory.PauseAll()` 暂停本节点作业防止重复执行。SQLite 下时间按文本比较，格式前缀一致就能比。实测四个场景：宕机节点被接管、心跳推进、被接管后自停、存活节点不被抢且停心跳后超时接管。
- 用户 2026-10-26 决定：被删的 LICENSE / README / DISCLAIMER 都不恢复，许可证责任用户自担。类名、路由、配置段名里的 H5/Job/Payment 放到第 5 步一起改。
- 去掉 `.gitkeep`（用户 2026-10-26）：当初为了让只提供接口的宿主能启动，在空 `wwwroot` 放了占位文件；现在请求处理链只在有 `wwwroot` 时挂图片处理和静态文件，占位文件和空目录已删。坑：删目录后只做增量编译，旧的静态资源清单（`*.staticwebassets.runtime.json`）还登记着项目 `wwwroot`，开发环境启动在创建宿主时就报 `DirectoryNotFoundException`；完整重编（`--no-incremental`）后清单不再生成。别人拉代码首次编译不受影响。
- 第 5 步（用户 2026-10-26，无历史数据不写迁移）：
  - 新增 `SimpX.Net.Caching`（只依赖 Furion、NewLife.Redis）：`CacheOptions`、`CacheTypeEnum`、`AddCache()`、内部用的 `CacheManager`；Core 引用它；Admin 的 `CacheAppService`（system/cache）包管理接口。缓存管理接口不再出现在其他宿主上。
  - Admin 对外服务 `SysXxxService` → `XxxAppService` + `ApiDescriptionSettings(Module = "system"/"dev", Name)`，路由 `/api/{模块}/{名称}/{动作}`；权限码由路由自动算（`/api/` 后面的 `/` 换成 `:`），种子同步成 `system:user:page`。内部服务 → `XxxManager`。日志类旧权限码本来就和路由对不上（如 `sysVislog`），这次对齐。
  - WebApi：`Member`、`MemberAppService`（/api/member/*）；Channels：/api/pay/order/*；Schedule：配置段 `ScheduleHost`、内部调用键 `Schedule`/`Channels`；网关前缀 /webapi /channels /schedule。
  - Admin 的 `System/` 并到根目录，DTO 集中到 `Dto/<模块>/`，命名空间统一 `SimpX.Net.Application.Admin`。坑：换命名空间后 `[Enum]` 在 Core 和数据注解之间有歧义，原来靠“外层命名空间优先”掩盖，用全局别名解决。
  - 前端：`build.bat` 重新生成接口（机器上没有 pwsh，PowerShell 5 读不了无 BOM 的中文 .ps1）；按旧生成代码的路由对照新方法名、类名，脚本替换 309 个方法、44 个类，权限码同表替换；核对“页面调用的方法、导入的名字都存在”代替类型检查（项目没装 vue-tsc）。
  - 坑：`dotnet run` 中间隔一层 `dotnet` 进程，只停直接子进程停不掉宿主，统一按进程名 `SimpX.Net.Host.*` 停。`dotnet sln add` 会给 .sln 加 BOM。
  - 按钮权限补种子（用户 2026-10-26）：补了平台参数、系统参数的“批量删除”，翻译管理的“批量删除、导出、导入”，实测进了菜单、租户菜单、角色菜单，系统管理员拿得到。插件、开放接口、打印、文件管理 4 个页面在菜单种子里根本没有菜单项（谁都进不去），它们的 17 个按钮没法挂；语言管理“批量删除”后端没接口、前端调用也注释掉了，没补。
  - 页面取舍（用户 2026-10-26）：文件管理、开放接口挂到“系统管理”下并补 9 个按钮权限；插件管理（含启动时加载动态程序集）、打印模板（后端服务实体、前端页面、hiprint 库、代码生成的打印选项和字典）整个删掉；通用表格打印用的是 print-js，保留。坑：字典值种子按下标 `typeList[n]` 找字典类型，删字典类型要同步把后面的下标往前挪。
  - 原有缺陷：代码生成预览模板编译报 CS1977（模型按动态类型编译，模板里的 lambda 不能用），用 git worktree 在提交版本上复现确认原来就坏；后来代码生成整体删除，这条不用再修。前端有 23 个按钮权限码在菜单种子里没登记。
  - 删插件和代码生成（用户 2026-10-26）：前端审批流插件（`src/api-plugins`、审批流页面、logicflow 依赖）、代码生成（服务、实体、DTO、模板目录、`CodeGen.json`、`CodeGenOptions`、5 个字典类型）、库表管理（看表改表、生成实体和种子）、表单设计（vform3，会生成 Vue 代码）前后端全删；“开发工具”只剩接口压测、系统接口。`api_build` 工具保留，生成文件头不再写“自动生成”说明（`licenseInfo.mustache` 置空）。“关于”页去掉“模块插件式开发”，旧的多语言条目已经没有页面用到，一并删。
  - 坑：多语言文件按文本算键，页面文案改了旧条目不会自动删，要手工清。Cursor 的搜索工具有缓存，刚删的文件还会搜出来，以磁盘为准再核对一遍。
  - 验证方法：开发库只增不删、旧菜单还在，要验证种子数据就用新库。直接跑编译好的 dll，工作目录指向临时目录（SQLite 的 `./SimpX.Net.db` 按工作目录算），`--contentRoot` 指回宿主项目（配置文件从内容根读），再设 `ASPNETCORE_ENVIRONMENT=Development`，不然默认按生产环境启动，会被默认密钥检查拦下。用环境变量改连接串没用，配置文件优先。
- Hosting 点选（用户 2026-10-27）：HostCapabilityOptions + Admin/WebApi/Worker 预设；Program 显式传入。Schedule/Channels 不再默认挂验证码/ImageSharp/SignalR/IP 内存库。
- Contracts.Jobs（用户 2026-10-27）：新建 SimpX.Net.Contracts.Jobs（IJobManagementApi、IAdminMaintenanceApi、JobManagementRoutes、作业 DTO/实体/枚举、IScheduleDashboardLogin）。Admin 去掉对 Application.Schedule 的项目引用与 ExcludeAssemblies；Hosting 改引契约接口。验证：编译 0 错误；四宿主起来；Admin 登录后作业分页转发 200。开关表见 knowledge/raw/2026/2026-10-27-simpx-net-host-feature-matrix.md。
- 架构拍板（用户 2026-10-27）：不做按域/按宿主分库；数据为中心库+凡租户一律独立库（B）；拆分优先用 Furion 自带能力。正式决定见 `knowledge/decisions/2026/ADR-2026-10-27-simpx-net-tenant-db-and-furion-first.md`。
- Core 按需拆模块（用户 2026-10-27）：抽出 SimpX.Net.ElasticSearch（客户端注册/工厂/配置，Admin 引用并在 Startup 注册；Hosting 不再全局 AddElasticSearch）。抽出 SimpX.Net.EventBus.RabbitMQ（RabbitMQEventSourceStore + CreateEventSourceStorer；默认宿主仍用内存/Redis，要用时再引用）。Magicodes/MiniExcel 与 XlsxFileResult、MiniExcelUtil、BaseImportInput 挪到 Application.Admin。Core 已去掉 Elastic、Magicodes、MiniExcel、RabbitMQ 包引用。验证：整仓编译 0 错误；四宿主端口起来；Admin 登录和菜单 200。坑：dotnet sln add 会给 .sln 写 UTF-8 BOM，按编码规则要去掉。
- 收尾小批（用户 2026-10-27）：旧名注释里的 Job/Payment 宿主改成调度/渠道；删掉整文件注释掉的 `AutoVersionUpdate` 和空的 `UpdateScripts/1.0.0.sql`；`.gitignore` 去掉已不存在的 CodeGen、SongMana、Database.json、Bat、发布条目。语言管理补上 `BatchDelete` 接口、种子权限和前端调用。菜单种子删掉 4 个后端和前端都没有的按钮权限：`system:user:detail`、`system:notice:cancel`、`system:dictType:move`、`system:logDiff:clear`。日志脱敏：新增 `LoggingSecretMasker`，控制台和文件用 `MessageProcess`，数据库和 ES 写入器在落库前打码；实测操作日志 `Message` 里是 `Bearer ****`，没有完整令牌。坑：本机新开的 shell 有时找不到 `java`，`api_build/build.bat` 会先删掉 `api-services` 再生成失败，要先把 JDK 的 bin 加进 PATH，或用 git 把目录捡回来。
- 第 6 步（用户 2026-10-27）：
  - NuGet：删掉 8 个没人用的包（AngleSharp、Hardware.Info、Hashids.net、log4net、MiniWord、QRCoder、Magicodes.IE.Pdf/Word）；LDAP、AspectCore 反射挪到 Application.Admin；ImageSharp 挪到 Hosting。其余 Core 自己在用，留着。
  - 配置目录：四个宿主的 `Configuration` 都放在各自 Host 项目（Application 只当类库），Host 项目文件用 `Content Update` 复制到输出；`.example` 这类非 json 要另写 `None Update`。
  - 部署覆盖：`ConfigurationScanDirectories` 改成 `Configuration`、根目录、`Deploy/Shared`、`Deploy/Host`。坑：Furion 按程序所在目录（`AppContext.BaseDirectory`，即 bin 输出目录）找这些目录，不是内容根目录，放项目目录不生效；同名文件（如 `App.json`）也会被后扫到的盖掉，数组按下标覆盖单个元素（只盖 `ConnectionConfigs[0].ConnectionString` 可行）；目录不存在直接跳过。用环境变量改配置无效，扫描进来的 json 优先级更高。
  - docker：MySQL 换 SQL Server，四个宿主共用一个库和一个 Redis，按 Admin → Schedule → WebApi → Channels 依次启动避免同时建表；`docker/shared` 挂到 `/app/Deploy/Shared`，各宿主目录挂到 `/app/Deploy/Host`；密码在 `docker/.env`、密钥和连接串在 `docker/shared/Secrets.json`，两者不进 git，只提交模板。修了编译镜像（SDK 9 → 10、npm → pnpm、node_modules 用容器自己的）。原来四个容器把同一个只有 6 个文件的旧配置目录整个挂到 `/app/Configuration`，等于把 JWT、Swagger 等配置全盖没了，而且四个都按 5005 端口启动。
  - 坑（换库暴露）：会员表密码列长度 128，SM2 密文放不下；SQLite 不检查长度，开发环境一直没暴露，SQL Server 直接报截断。改成和后台用户表一样的 512。新实体的加密字段要按密文长度留够。
  - 安全：SqlSugar 初始化日志原来会打完整连接串（换 SQL Server 后带 sa 密码），改成只打库类型和库标识。
  - 验证：本机没有 docker，用 LocalDB 模拟共用库：四个宿主依次启动都连同一个库，超管登录、菜单、Admin 经内部调用查调度作业、会员注册登录、渠道带签名下单查单都通过，库里 44 张表四个宿主的都在。生产环境没换密钥启动失败，`Deploy/Shared/Secrets.json` 换掉后正常启动且日志不含密钥。容器本身没实际跑。
- 坑：Core 是 net8.0;net10.0 双目标，只用 `-f net10.0` 编译会漏掉 net8 的错误（如 `LoadIntoBufferAsync(CancellationToken)` 只有 .NET 9+ 才有），收口要编整个解决方案。
- 原始发现：`OnlineUserHub` 没加授权，且向客户端公开了强制下线、给所有人/某人发消息等方法，前端并没调用；`SysCommonService` 类级 `[AllowAnonymous]`，导致接口清单、密码同算法的加密、错误 Excel 下载都不用登录；HMAC 帮助页引导去第三方网站算签名（会泄露密钥）；配置里的 JWT 签名密钥、国密私钥、内部调用密钥、看板 AppSecret 是公开默认值。
- 还剩（待用户拍板）：登录后的请求，LoggingMonitor 的“授权信息”段会打完整 `Bearer` 令牌，Furion 没有开关，要在控制台、文件、数据库三处日志输出时统一脱敏。

## 证据

- 四个宿主 `dotnet build -f net10.0`：都是 0 错误。
- 本地冒烟：四个宿主 `/api/host/info` 都返回 200；Admin、H5 上的 `sysJob` 返回 404；H5、Payment 上的 `sysUser` 返回 404；只有 Job 日志里有 `Schedule hosted service is running`；Payment demo 下单 200；H5 注册、登录 200。
- 删插件和代码生成后（2026-10-26）：整个解决方案编译 0 错误；`build.bat` 重新生成 42 个接口文件、321 个模型文件，文件头不含“自动生成”；前端 213 处接口调用都能对上（语言管理“批量删除”是已知缺接口）；`pnpm run build` 通过；Admin 用临时新库回归 13/13 通过（菜单里没有三项、`/api/dev/codeGen`、`codeGenConfig`、`database` 都返回 404，org_type 有 5 条，新库 38 张表里没有代码生成表）；WebApi、Schedule、Channels 都能正常启动；改动文件编码检查 474 个，0 个问题。
- 未验证：Admin token 远程调用 Job `sysJob` 被登录验证码挡住；Admin 与 Job 的 `JWT.json`、`Database.json`、`Cache.json` 哈希一致。
