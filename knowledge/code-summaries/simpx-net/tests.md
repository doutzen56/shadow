# 测试项目

<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->

- project_id: `simpx-net`
- summary_group: `tests`
- host_or_layer: `test`
- solution_file: `SimpX.Net/SimpX.Net.sln`
- last_ingestion_run: `20261028T041009Z-project-simpx-net`
- file_count: 5
- project_count: 1
- risk_tags: `user-data`

## 职责摘要

框架测试入口。

## 扫描根目录

- `SimpX.Net/SimpX.Net.Test`

## 项目文件

- `SimpX.Net/SimpX.Net.Test/SimpX.Net.Test.csproj`

## 定位样例

按路径里的高风险关键词优先挑选，最多列 50 个。

- `SimpX.Net/SimpX.Net.Test/BaseTest.cs`：符号 `BaseTest, WaitExecutorCompleteAsync, Login, GoToUrlAsync, WaitEnter, Dispose`；风险 `无`
- `SimpX.Net/SimpX.Net.Test/SimpX.Net.Test.csproj`：符号 `未提取到符号`；风险 `无`
- `SimpX.Net/SimpX.Net.Test/User/UserTest.cs`：符号 `UserTest, Login_User, AddUser`；风险 `user-data`
- `SimpX.Net/SimpX.Net.Test/Utils/DateTimeUtilTests.cs`：符号 `DateTimeUtilTests, Init_WithTimeSpan_ReturnsCorrectDateTime, Init_WithDateTime_ReturnsCorrectDateTime, GetTodayRange_ReturnsCorrectRange, GetMonthRange_ReturnsCorrectRange, GetFirstDayOfMonth_ReturnsCorrectDate, GetLastDayOfMonth_ReturnsCorrectDate, GetYearRange_ReturnsCorrectRange`；风险 `无`
- `SimpX.Net/SimpX.Net.Test/Utils/SafeMathTests.cs`：符号 `SafeMathTests, Add_IntAndDouble_ReturnsCorrectResult, Add_StringAndDecimal_ReturnsCorrectResult, Sub_DoubleAndInt_ReturnsCorrectResult, Mult_DecimalAndFloat_ReturnsCorrectResult, Div_IntAndInt_ReturnsCorrectResult, Div_ByZero_ReturnsDefaultValue, Div_ByZero_ThrowsException`；风险 `无`

## 跳过项

- pruned_directory 2 项；明细只写入 `rag/init-reports/20261028T041009Z-project-simpx-net.json`。

## 使用约束

- 本摘要只用于定位和初筛，不替代当前源码。
- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。
- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。
