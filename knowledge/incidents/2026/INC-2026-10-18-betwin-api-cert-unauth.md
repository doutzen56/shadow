---
id: inc-2026-10-18-betwin-api-cert-unauth
title: BetWin Api 面证书私钥可无鉴权下载
doc_type: incident
projects:
  - betwin
lifecycle_status: confirmed
confirmed_by: "@@TL"
user_reviewed: false
risk_tags:
  - permission
  - production-config
  - secret
---

# INC-2026-10-18：BetWin Api 面证书私钥可无鉴权下载

## 现象

`/api` 请求面在分发层不做登录或权限校验；证书下载接口在 Type=key 时直接输出私钥内容。

## 证据

- `Library/BetWin.Handler/HandlerBase/ResultInterface.cs`
  - `case "Api":` 分支为空，无 Guest/登录/权限处理（约 170-172 行）。
- `Library/Handler/BetWin.Handler.Api/server/cert.cs`
  - `down()`：`Type=key` 时 `context.Response.Write(cert.Key)`（约 42-45 行），方法上未见鉴权特性。

## 影响

- 影响对象：站点证书私钥、依赖该证书的 HTTPS/业务签名。
- 触发条件：攻击者或内网人员知道或猜到证书 Name/ID，请求下载接口。
- 失败后果：私钥泄露，可伪造站点身份或解密相关流量（取决于证书用途）。

## 相关同族问题

1. 站点管理员 token 读取不校验 `admin.SiteID == SiteInfo.ID`（`Sites/AdminAgent.GetAdminInfo`）。
2. 管理端操作日志 RawData 可能包含完整 POST（含密码）与 Cookie。
3. 部分 `[Guest]` 调试/上传接口扩大攻击面。

## 建议修复方向

1. Api 面默认拒绝；每个方法显式声明 IP 白名单、APIKey 或签名校验。
2. 证书私钥下载增加鉴权，或改为运维机本地导出，禁止 HTTP 出私钥。
3. 管理员 token 绑定站点；日志脱敏。

## 沉淀

- 已写入项目 `boundaries.md` 的权限与安全边界。
