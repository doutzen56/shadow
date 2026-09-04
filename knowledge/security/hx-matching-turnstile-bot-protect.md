# HX.Matching 无感 Turnstile 人机验证

## 适用场景

主要用于撞库、刷接口、偷到 JWT 后用脚本改凭证等案例。

管理端、商户后台这类浏览器接口：正常人调用无感；Cloudflare 判定有风险时 widget 自己弹出点击确认；后端校验失败时前端再弹出强制点击层并重放原请求。

不用于 MerchantApi、PartnerApi、渠道回调。那些是服务端签名，挂 widget 会打挂对接。

## 来源

HX.Matching `Admin.WebApi` / `MerchantAdmin.WebApi`。已有图形验证码、IP 白名单、谷歌验证、登录失败锁定，挡不住无头浏览器撞库、脚本刷读密钥/卡号、偷 JWT 后改密/解谷歌/重置商户 Key。

## 以后项目怎么挪用

1. 先问是不是浏览器后台。不是，停。
2. 协议整节落地：开关、header、`[Turnstile]`、错误码 `10071`、`GET /turnstileConfig`。
3. 登录贴 `[Turnstile("login")]`。HX 本期只拦登录；其余以后再贴 `[Turnstile("auth")]`。
4. 用「如何选接口」贴特性；HX 清单当分类样例，不要原样抄路径。
5. 图形验证码、谷歌、IP 白名单、登录锁定都独立保留，Turnstile 不替代、不跳过它们。
6. 超时或 Cloudflare 挂了，这次就算过，不能挡住正常使用；假 token 仍然拦。紧急把开关调成 off。

## 威胁与对策

- 撞库：登录必须带一次性 token。不能替代密码错误锁定。
- 刷接口：HX 本期先不加。以后读密钥/卡号/凭证再贴 `[Turnstile("auth")]`。下拉、普通列表、导出不加。
- 偷到 JWT 后改凭证：HX 本期先不加。以后改密、谷歌、重置 Key 再贴。不能替代谷歌验证本身。
- 内部人点一下补单：不加 Turnstile，继续走谷歌。

## 无感怎么做成

Turnstile 没有 0~1 分。风险判断在浏览器 widget：正常人静默发 token；有风险才弹出点击。后端 siteverify 只有成功/失败。两层：

1. 前端默认 `appearance: interaction-only` + `execution: execute`。
2. 缺 token / 伪造 / 过期 / 重放时返回 `10071` + `needClick=true`，前端弹强制点击层后重放。

## 协议

- 系统开关：`sys.account.turnstileOnOff`，值 `on` / `1` 开，`off` / `0` 关。未配时看 `Turnstile:OnOff`，默认关。
- 配置：`Turnstile:OnOff` / `SiteKey` / `SecretKey` / `Host` / `SkipIps` / `TimeoutSeconds`（默认 2 秒。超时或 Cloudflare 挂了按通过，假 token 仍拦）
- 拉配置：`GET /turnstileConfig`
- 请求头：`X-Hx-Nsp`（故意起得看不出用途）
- 特性：本期只贴 `[Turnstile("login")]`；`[Turnstile("auth")]` 以后再加
- 错误码：`TURNSTILE_ERROR = 10071`
- Redis：`System:Turnstile:Used:{hash}`，TTL 5 分钟，NX 防重放
- token 一次性，禁止缓存给下一个接口

`GET /turnstileConfig`：

```json
{
  "turnstileOnOff": "on",
  "siteKey": "0x4AAAA..."
}
```

失败：

```json
{
  "code": 10071,
  "msg": "请完成点击验证后重试",
  "data": { "needClick": true, "siteKey": "0x4AAAA..." }
}
```

## 后端结构

```
Infrastructure/Attribute/TurnstileAttribute.cs
ResultCode.TURNSTILE_ERROR = 10071
SysConfigKeyConst.TurnstileOnOff
ServiceCore/Security/Turnstile/*
ServiceCore/Middleware/TurnstileMiddleware.cs
```

中间件顺序：`UseRouting` → `Authentication` → `TurnstileMiddleware` → `JwtAuthMiddleware`。

逻辑：无特性放行 → 开关 off 放行 → SkipIps 放行 → 读 header → siteverify + Redis NX → 失败 10071。

## 如何选接口

HX 本期只拦登录。以后加接口时：会拿会话或改登录能力、会把密钥/卡号/证件拉走、会拆掉现有防护的，贴 `[Turnstile("auth")]`。

下拉、普通列表、导出、资金写操作、getInfo、logout、health、`turnstileConfig`：不加。

## HX 已贴 `[Turnstile]` 清单（和代码对齐，共 2）

只贴 `[Turnstile("login")]`：

- 总后台 `POST /login`
- 商户后台 `POST /login`

改密、谷歌、看密钥、Payment、凭证、改配置、IP 白名单、角色、踢人：以后再加。

## 前端约定

1. 进页拉 `/turnstileConfig`，`turnstileOnOff=on` 则加载 Cloudflare 脚本。
2. 隐藏容器：`appearance: 'interaction-only'`，`execution: 'execute'`。
3. 本期只有 `/login` 需要 token，`action=login`。以后贴 auth 时再用 `action=auth`。
4. 校验串放 header `X-Hx-Nsp`。
5. `code === 10071` 且 `needClick === true`：弹强制点击层，新 token 重放原请求一次。

## SimpX 复用方式

后台管理、商户后台，按这篇做属性中间件和 10071 拦截协议。不要按路径写死，不要给开放 API 挂 widget。

实现参考：`E:\work\撮合项目` 的 `TurnstileMiddleware`、`TurnstileVerifier`、`[Turnstile]`。
