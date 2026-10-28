# 可点选 Feature

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `features`
- host_or_layer: `infrastructure`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 69
- project_count: 9
- risk_tags: `cache-consistency`, `login`, `mq`, `permission`, `user-data`

## 职责摘要

认证、验证码、存储、短信、邮件、日志、事件等可点选能力。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Feature.Auth`
- `SimpX.Net/SimpX.Net.Feature.Captcha`
- `SimpX.Net/SimpX.Net.Feature.Storage`
- `SimpX.Net/SimpX.Net.Feature.Sms`
- `SimpX.Net/SimpX.Net.Feature.Email`
- `SimpX.Net/SimpX.Net.Feature.Logging`
- `SimpX.Net/SimpX.Net.Feature.EventBus`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin`
- `SimpX.Net/SimpX.Net.Feature.Translation`

## 项目文件

- `SimpX.Net/SimpX.Net.Feature.Auth/SimpX.Net.Feature.Auth.csproj`
- `SimpX.Net/SimpX.Net.Feature.Captcha/SimpX.Net.Feature.Captcha.csproj`
- `SimpX.Net/SimpX.Net.Feature.Storage/SimpX.Net.Feature.Storage.csproj`
- `SimpX.Net/SimpX.Net.Feature.Sms/SimpX.Net.Feature.Sms.csproj`
- `SimpX.Net/SimpX.Net.Feature.Email/SimpX.Net.Feature.Email.csproj`
- `SimpX.Net/SimpX.Net.Feature.Logging/SimpX.Net.Feature.Logging.csproj`
- `SimpX.Net/SimpX.Net.Feature.EventBus/SimpX.Net.Feature.EventBus.csproj`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/SimpX.Net.Feature.ExternalLogin.csproj`
- `SimpX.Net/SimpX.Net.Feature.Translation/SimpX.Net.Feature.Translation.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Feature.Auth/DefaultJwtHandler.cs`：符号 `DefaultJwtHandler, HandleAsync, PipelineAsync, CheckAuthorizeAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/GlobalUsings.cs`：符号 `未提取到符号`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/IPermissionChecker.cs`：符号 `IPermissionChecker`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/ITokenLifetimeProvider.cs`：符号 `ITokenLifetimeProvider`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallAuthenticationExtensions.cs`：符号 `InternalCallAuthenticationExtensions, AddInternalCallAuthentication`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallDefaults.cs`：符号 `InternalCallDefaults`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallHandler.cs`：符号 `InternalCallHandler, SendAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallOptions.cs`：符号 `InternalCallOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallServiceCollectionExtensions.cs`：符号 `InternalCallServiceCollectionExtensions, AddInternalCallClient, TryAddInternalCallHandler`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCall/InternalCallSigner.cs`：符号 `InternalCallSigner`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/InternalCallAuthenticationComponent.cs`：符号 `InternalCallAuthenticationComponent, Load`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/JwtAuthenticationExtensions.cs`：符号 `JwtAuthenticationExtensions, EnsureAuthorizationRegistered`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/GetAccessSecretContext.cs`：符号 `GetAccessSecretContext`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureAuthenticationDefaults.cs`：符号 `SignatureAuthenticationDefaults`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureAuthenticationEvent.cs`：符号 `SignatureAuthenticationEvent, GetAccessSecret, Challenge, Validated`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureAuthenticationExtensions.cs`：符号 `SignatureAuthenticationExtensions, AddSignatureAuthentication`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureAuthenticationHandler.cs`：符号 `SignatureAuthenticationHandler, CreateEventsAsync, HandleAuthenticateAsync, HandleChallengeAsync, GetMessageForSign, SignData, AuthenticateResultFailAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureAuthenticationOptions.cs`：符号 `SignatureAuthenticationOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureChallengeContext.cs`：符号 `SignatureChallengeContext`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SignatureAuth/SignatureValidatedContext.cs`：符号 `SignatureValidatedContext`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Auth/SimpX.Net.Feature.Auth.csproj`：符号 `未提取到符号`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Captcha/Captcha/CacheCaptchaStorage.cs`：符号 `CacheCaptchaStorage, CaptchaCacheServiceCollectionExtensions, Set, Get, Remove, AddCaptchaStateStorage`；风险 `cache-consistency, login, permission`
- `SimpX.Net/SimpX.Net.Feature.Email/Email/ErrorAlertMailSubscriber.cs`：符号 `ErrorAlertMailSubscriber, SendErrorAlertMail`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/Email/ErrorAlertMessage.cs`：符号 `ErrorAlertMessage`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/Email/IEmailSender.cs`：符号 `IEmailSender`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/Email/SmtpEmailSender.cs`：符号 `SmtpEmailSender, SendAsync`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/EmailOptions.cs`：符号 `EmailOptions`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/GlobalUsings.cs`：符号 `未提取到符号`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.Email/SimpX.Net.Feature.Email.csproj`：符号 `未提取到符号`；风险 `login, permission, user-data`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBus/EventConsumer.cs`：符号 `EventConsumer, Start, Stop, Dispose`；风险 `login, mq, permission`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBus/EventHandlerMonitor.cs`：符号 `EventHandlerMonitor, OnExecutingAsync, OnExecutedAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBus/RedisEventSourceStorer.cs`：符号 `RedisEventSourceStorer, OnConsumeBroadcast, ConsumeChannelEventSourceAsync, WriteAsync, ReadAsync, Dispose`；风险 `cache-consistency, login, permission`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBus/RedisQueue.cs`：符号 `RedisQueue, RollbackAllAck, BeginCacheLock`；风险 `cache-consistency, login, mq, permission`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBus/RetryEventHandlerExecutor.cs`：符号 `RetryEventHandlerExecutor, ExecuteAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.EventBus/EventBusOptions.cs`：符号 `EventBusOptions, RabbitMQSettings`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/ExternalLogin/DemoExternalLoginProvider.cs`：符号 `DemoExternalLoginProvider, BuildAuthorizeUrlAsync, GetUserInfoAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/ExternalLogin/ExternalLoginOptions.cs`：符号 `ExternalLoginOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/ExternalLogin/IExternalLoginProvider.cs`：符号 `IExternalLoginProvider, ExternalUserInfo`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/GlobalUsings.cs`：符号 `未提取到符号`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.ExternalLogin/SimpX.Net.Feature.ExternalLogin.csproj`：符号 `未提取到符号`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Logging/Logging/ExceptionSanitizer.cs`：符号 `ExceptionSanitizer, Snapshot, Build, ToJson`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Logging/Logging/LogExceptionHandler.cs`：符号 `LogExceptionHandler, OnExceptionAsync`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Logging/Logging/LoggingMonitorDto.cs`：符号 `LoggingMonitorDto, LoggingAuthorizationClaimsDto, LoggingParametersDto, LoggingReturnInformationDto, LoggingUserInfo`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Logging/Logging/LoggingSecretMasker.cs`：符号 `LoggingSecretMasker, Mask`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Logging/Logging/LoggingSetup.cs`：符号 `LoggingSetup, AddLoggingSetup`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Sms/Sms/DemoSmsSender.cs`：符号 `DemoSmsSender, SendAsync, MaskPhone`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Sms/Sms/ISmsSender.cs`：符号 `ISmsSender, SmsMessage`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Sms/Sms/SmsOptions.cs`：符号 `SmsOptions, SmsDemoOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Storage/Storage/FileStorageOptions.cs`：符号 `FileStorageOptions`；风险 `login, permission`
- `SimpX.Net/SimpX.Net.Feature.Storage/Storage/FileStorageServiceCollectionExtensions.cs`：符号 `FileStorageServiceCollectionExtensions, AddFileStorage`；风险 `login, permission`
- 还有 19 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。

## 跳过项

- pruned_directory 18 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
