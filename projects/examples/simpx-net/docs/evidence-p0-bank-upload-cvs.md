# Evidence: P0 银行信息 — 凭证上传 / 超商级联

日期：2026-11-03

## 对照旧站

- `user/member/bank.html` + `bank-create.html`
- 上传：`admin/system/layupload` → 新壳 `/admin/system/upload`
- 超商：`bankcountryinfo(isVir=4)` → `bankcitylist` → `bankdistrictlist` → `bankadresslist`
- 创建：`createbank`，`IsVir`：0 银行 / 1 虚拟 / 2 电子 / 4 超商

## 本次改动

| 层 | 内容 |
|----|------|
| FE `bank/index.vue` | 添加弹层四 Tab；银行凭证上传；超商三级联动；verify 兼容 `Id`/`ID` |
| FE `member-bank.ts` | upload / countryinfo / city / district / adress API |
| BE `CreateAsync` | 支持 `IsVir=4` |
| BE `Createbank` | 读 CType/CCity/CDistrict/CBankAddress、`isVir` |
| BE `BankCountryInfoAsync` | 返回 `{list:[{bank}]}`；1=虚拟段；4=SiteStore 类型 |

## 旁路清理

- report 编码损坏壳改为诚实「非 P0 未验收」占位；根 `admin-page` 用 UTF-8 Python 补丁

## Host

- Host.Admin 已重编并监听 `:5005`
