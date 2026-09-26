/*==============================================================================
  Azure SQL 日巡检脚本 - HxMatching（SimpX 归档版）
  归档：F:\shadow\projects\examples\hx-matching\ops\azure-sql-daily-inspect.sql
  模式卡：F:\shadow\knowledge\patterns\reliability\azure-sql-dba-daily-inspect-v1.md

  用途：DBA 日清检出 → 把结果贴给开发（query_id / 等待 / 表体积 / 死锁）
  环境：Azure SQL Database（单库执行）
  不含：Running Requests / Blocking（不当下抓现行）
  建议：每天业务高峰后 + 凌晨 Job 后再各跑一次；避开 CPU/Log 已顶满时段
==============================================================================*/

SET NOCOUNT ON;
DECLARE @Now datetime2(0) = SYSUTCDATETIME();
DECLARE @LocalNow datetime2(0) = DATEADD(HOUR, 8, @Now); -- UTC+8
DECLARE @Since datetime2(0) = DATEADD(HOUR, -24, @Now);  -- 近24小时；若只要「今天」见文末改法

PRINT '======== 0. 巡检抬头 ========';
SELECT
  @LocalNow AS local_time_utc8,
  @Now       AS utc_now,
  DB_NAME()  AS database_name,
  @Since     AS window_start_utc,
  @Now       AS window_end_utc;

/*------------------------------------------------------------------------------
  1. 等待类型 Top20
------------------------------------------------------------------------------*/
PRINT '======== 1. Wait Stats Top20 ========';
SELECT TOP 20
  wait_type,
  waiting_tasks_count,
  CAST(wait_time_ms / 1000.0 AS decimal(18,1)) AS wait_s,
  CAST(signal_wait_time_ms / 1000.0 AS decimal(18,1)) AS signal_s,
  CASE
    WHEN wait_type LIKE 'WRITELOG%' OR wait_type = 'LOGBUFFER' THEN '写放大/大事务/LogIO'
    WHEN wait_type LIKE 'LCK_%' THEN '锁竞争'
    WHEN wait_type LIKE 'PAGEIOLATCH%' THEN '读盘/缺索引/大扫描'
    WHEN wait_type IN ('SOS_SCHEDULER_YIELD','THREADPOOL') THEN 'CPU/调度'
    WHEN wait_type LIKE 'CXPACKET%' OR wait_type LIKE 'CXCONSUMER%' THEN '并行'
    WHEN wait_type = 'ASYNC_NETWORK_IO' THEN '应用取数慢'
    ELSE '其他'
  END AS dba_hint
FROM sys.dm_os_wait_stats
WHERE wait_type NOT LIKE 'SLEEP%'
  AND wait_type NOT LIKE 'BROKER%'
  AND wait_type NOT IN (
    'CLR_AUTO_EVENT','LAZYWRITER_SLEEP','DIRTY_PAGE_POLL',
    'HADR_FILESTREAM_IOMGR_IOCOMPLETION','XE_TIMER_EVENT','XE_DISPATCHER_WAIT',
    'REQUEST_FOR_DEADLOCK_SEARCH','SQLTRACE_BUFFER_FLUSH','WAITFOR'
  )
ORDER BY wait_time_ms DESC;

/*------------------------------------------------------------------------------
  2. Query Store - 近24h Top CPU
------------------------------------------------------------------------------*/
PRINT '======== 2. Query Store Top CPU 24h ========';
IF EXISTS (SELECT 1 FROM sys.database_query_store_options WHERE actual_state_desc IN ('READ_WRITE','READ_ONLY'))
BEGIN
  ;WITH q AS (
    SELECT
      q.query_id,
      qt.query_sql_text,
      SUM(rs.count_executions) AS exec_count,
      SUM(rs.avg_cpu_time * rs.count_executions) AS total_cpu_us,
      SUM(rs.avg_duration * rs.count_executions) AS total_duration_us,
      SUM(rs.avg_logical_io_reads * rs.count_executions) AS total_logical_reads,
      SUM(rs.avg_logical_io_writes * rs.count_executions) AS total_logical_writes,
      MAX(rs.avg_cpu_time) AS max_avg_cpu_us,
      MAX(rs.avg_duration) AS max_avg_duration_us
    FROM sys.query_store_runtime_stats rs
    JOIN sys.query_store_plan p ON rs.plan_id = p.plan_id
    JOIN sys.query_store_query q ON p.query_id = q.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
    JOIN sys.query_store_runtime_stats_interval rsi ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
    WHERE rsi.start_time >= @Since
    GROUP BY q.query_id, qt.query_sql_text
  )
  SELECT TOP 20
    query_id,
    exec_count,
    CAST(total_cpu_us / 1000.0 AS decimal(18,1)) AS total_cpu_ms,
    CAST(total_duration_us / 1000.0 AS decimal(18,1)) AS total_duration_ms,
    CAST(total_logical_reads AS decimal(18,0)) AS total_logical_reads,
    CAST(total_logical_writes AS decimal(18,0)) AS total_logical_writes,
    CAST(max_avg_cpu_us / 1000.0 AS decimal(18,1)) AS max_avg_cpu_ms,
    CAST(max_avg_duration_us / 1000.0 AS decimal(18,1)) AS max_avg_duration_ms,
    LEFT(query_sql_text, 300) AS sql_preview
  FROM q
  ORDER BY total_cpu_us DESC;
END
ELSE
BEGIN
  SELECT 'Query Store 未开启或不可用，请先打开 Read Write' AS warning;
END;

/*------------------------------------------------------------------------------
  3. Query Store - 近24h Top Duration（谁比较慢）
------------------------------------------------------------------------------*/
PRINT '======== 3. Query Store Top Duration 24h ========';
IF EXISTS (SELECT 1 FROM sys.database_query_store_options WHERE actual_state_desc IN ('READ_WRITE','READ_ONLY'))
BEGIN
  ;WITH q AS (
    SELECT
      q.query_id,
      qt.query_sql_text,
      SUM(rs.count_executions) AS exec_count,
      SUM(rs.avg_duration * rs.count_executions) AS total_duration_us,
      SUM(rs.avg_cpu_time * rs.count_executions) AS total_cpu_us,
      MAX(rs.max_duration) AS max_duration_us
    FROM sys.query_store_runtime_stats rs
    JOIN sys.query_store_plan p ON rs.plan_id = p.plan_id
    JOIN sys.query_store_query q ON p.query_id = q.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
    JOIN sys.query_store_runtime_stats_interval rsi ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
    WHERE rsi.start_time >= @Since
    GROUP BY q.query_id, qt.query_sql_text
  )
  SELECT TOP 20
    query_id,
    exec_count,
    CAST(total_duration_us / 1000.0 AS decimal(18,1)) AS total_duration_ms,
    CAST(total_cpu_us / 1000.0 AS decimal(18,1)) AS total_cpu_ms,
    CAST(total_duration_us / NULLIF(exec_count,0) / 1000.0 AS decimal(18,1)) AS avg_duration_ms,
    CAST(max_duration_us / 1000.0 AS decimal(18,1)) AS max_duration_ms,
    LEFT(query_sql_text, 300) AS sql_preview
  FROM q
  ORDER BY total_duration_us DESC;
END;

/*------------------------------------------------------------------------------
  4. Query Store - 近24h 执行次数 Top
------------------------------------------------------------------------------*/
PRINT '======== 4. Query Store Top Exec Count 24h ========';
IF EXISTS (SELECT 1 FROM sys.database_query_store_options WHERE actual_state_desc IN ('READ_WRITE','READ_ONLY'))
BEGIN
  ;WITH q AS (
    SELECT
      q.query_id,
      qt.query_sql_text,
      SUM(rs.count_executions) AS exec_count,
      SUM(rs.avg_cpu_time * rs.count_executions) AS total_cpu_us,
      SUM(rs.avg_duration * rs.count_executions) AS total_duration_us
    FROM sys.query_store_runtime_stats rs
    JOIN sys.query_store_plan p ON rs.plan_id = p.plan_id
    JOIN sys.query_store_query q ON p.query_id = q.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
    JOIN sys.query_store_runtime_stats_interval rsi ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
    WHERE rsi.start_time >= @Since
    GROUP BY q.query_id, qt.query_sql_text
  )
  SELECT TOP 20
    query_id,
    exec_count,
    CAST(total_cpu_us / 1000.0 AS decimal(18,1)) AS total_cpu_ms,
    CAST(total_duration_us / 1000.0 AS decimal(18,1)) AS total_duration_ms,
    LEFT(query_sql_text, 300) AS sql_preview
  FROM q
  ORDER BY exec_count DESC;
END;

/*------------------------------------------------------------------------------
  5. 缺索引建议
------------------------------------------------------------------------------*/
PRINT '======== 5. Missing Index Suggestions ========';
SELECT TOP 30
  ROUND(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans), 0) AS improvement_score,
  mid.statement AS table_name,
  mid.equality_columns,
  mid.inequality_columns,
  mid.included_columns,
  migs.user_seeks,
  migs.user_scans,
  migs.avg_total_user_cost,
  migs.avg_user_impact
FROM sys.dm_db_missing_index_groups mig
JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY improvement_score DESC;

/*------------------------------------------------------------------------------
  6. 用户表体积 Top30
------------------------------------------------------------------------------*/
PRINT '======== 6. Table Size Top30 ========';
SELECT TOP 30
  s.name AS schema_name,
  t.name AS table_name,
  p.rows AS row_counts,
  CAST(ROUND(SUM(a.total_pages) * 8.0 / 1024, 1) AS decimal(18,1)) AS total_mb,
  CAST(ROUND(SUM(a.used_pages) * 8.0 / 1024, 1) AS decimal(18,1)) AS used_mb
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.indexes i ON t.object_id = i.object_id
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0 AND i.index_id <= 1
GROUP BY s.name, t.name, p.rows
ORDER BY SUM(a.total_pages) DESC;

/*------------------------------------------------------------------------------
  7. 最近死锁（best effort）
------------------------------------------------------------------------------*/
PRINT '======== 7. Recent Deadlocks (best effort) ========';
BEGIN TRY
  SELECT TOP 20
    DATEADD(HOUR, 8, xed.event_data.value('(@timestamp)[1]', 'datetime2')) AS local_time_utc8,
    xed.event_data.query('.') AS deadlock_xml
  FROM (
    SELECT CAST(target_data AS xml) AS target_data
    FROM sys.dm_xe_database_session_targets st
    JOIN sys.dm_xe_database_sessions s ON s.address = st.event_session_address
    WHERE s.name = 'system_health' AND st.target_name = 'ring_buffer'
  ) AS t
  CROSS APPLY t.target_data.nodes('RingBufferTarget/event[@name="xml_deadlock_report"]') AS xed(event_data)
  ORDER BY xed.event_data.value('(@timestamp)[1]', 'datetime2') DESC;
END TRY
BEGIN CATCH
  SELECT '本库取 system_health 死锁失败，请用 Azure 门户 Deadlocks / Log Analytics' AS info,
         ERROR_MESSAGE() AS err;
END CATCH;

PRINT '======== DONE ========';

/*
【辅助】取某 query 完整 SQL（截断预览不够时）：
SELECT q.query_id, qt.query_sql_text
FROM sys.query_store_query q
JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE q.query_id = <id>;

【改窗口】只要本地今天 0 点至今：
DECLARE @TodayLocal date = CAST(@LocalNow AS date);
DECLARE @Since datetime2(0) = DATEADD(HOUR, -8, CAST(@TodayLocal AS datetime2));

【前置】Query Store：
ALTER DATABASE CURRENT SET QUERY_STORE = ON;
ALTER DATABASE CURRENT SET QUERY_STORE (OPERATION_MODE = READ_WRITE);
*/
