using Furion.HttpRemote;

namespace SimpX.Net.Contracts.Jobs;

/// <summary>
/// 租户后台调 Schedule 作业调度的内部 HTTP 客户端（Furion HttpRemote）。
/// Schedule 独立进程时保留本契约；同进程合并后可改为直接调领域服务。
/// 走命名 HttpClient "Schedule"：自动签名，返回值按统一结果自动拆包，对方业务失败直接抛友好异常。
/// </summary>
[HttpClientName(JobManagementRoutes.ScheduleService)]
public interface IJobManagementApi : IHttpDeclarative
{
    /// <summary>获取作业分页列表。</summary>
    /// <param name="input">查询条件</param>
    /// <returns>作业分页</returns>
    [Post("api/internal/job/pageJobDetail")]
    Task<SqlSugarPagedList<JobDetailOutput>> PageJobDetail([Body] PageJobDetailInput input);

    /// <summary>获取作业组名称集合。</summary>
    /// <returns>作业组名称</returns>
    [Post("api/internal/job/listJobGroup")]
    Task<List<string>> ListJobGroup();

    /// <summary>添加作业。</summary>
    /// <param name="input">作业信息</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/addJobDetail")]
    Task AddJobDetail([Body] AddJobDetailInput input);

    /// <summary>更新作业。</summary>
    /// <param name="input">作业信息</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/updateJobDetail")]
    Task UpdateJobDetail([Body] UpdateJobDetailInput input);

    /// <summary>删除作业。</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/deleteJobDetail")]
    Task DeleteJobDetail([Body] DeleteJobDetailInput input);

    /// <summary>获取触发器列表。</summary>
    /// <param name="jobId">作业编号</param>
    /// <returns>触发器列表</returns>
    [Get("api/internal/job/jobTriggerList")]
    Task<List<SysJobTrigger>> GetJobTriggerList([QueryParam("jobId")] string jobId);

    /// <summary>添加触发器。</summary>
    /// <param name="input">触发器信息</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/addJobTrigger")]
    Task AddJobTrigger([Body] AddJobTriggerInput input);

    /// <summary>更新触发器。</summary>
    /// <param name="input">触发器信息</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/updateJobTrigger")]
    Task UpdateJobTrigger([Body] UpdateJobTriggerInput input);

    /// <summary>删除触发器。</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/deleteJobTrigger")]
    Task DeleteJobTrigger([Body] DeleteJobTriggerInput input);

    /// <summary>暂停所有作业。</summary>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/pauseAllJob")]
    Task PauseAllJob();

    /// <summary>启动所有作业。</summary>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/startAllJob")]
    Task StartAllJob();

    /// <summary>暂停作业。</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/pauseJob")]
    Task PauseJob([Body] JobDetailInput input);

    /// <summary>启动作业。</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/startJob")]
    Task StartJob([Body] JobDetailInput input);

    /// <summary>取消作业。</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/cancelJob")]
    Task CancelJob([Body] JobDetailInput input);

    /// <summary>立即执行作业。</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/runJob")]
    Task RunJob([Body] JobDetailInput input);

    /// <summary>暂停触发器。</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/pauseTrigger")]
    Task PauseTrigger([Body] JobTriggerInput input);

    /// <summary>启动触发器。</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/startTrigger")]
    Task StartTrigger([Body] JobTriggerInput input);

    /// <summary>强制唤醒作业调度器。</summary>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/cancelSleep")]
    Task CancelSleep();

    /// <summary>强制触发所有作业持久化。</summary>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/persistAll")]
    Task PersistAll();

    /// <summary>获取集群列表。</summary>
    /// <returns>集群节点列表</returns>
    [Get("api/internal/job/jobClusterList")]
    Task<List<SysJobCluster>> GetJobClusterList();

    /// <summary>获取作业触发器运行记录分页列表。</summary>
    /// <param name="input">查询条件</param>
    /// <returns>运行记录分页</returns>
    [Post("api/internal/job/pageJobTriggerRecord")]
    Task<SqlSugarPagedList<SysJobTriggerRecord>> PageJobTriggerRecord([Body] PageJobTriggerRecordInput input);

    /// <summary>清空作业触发器运行记录。</summary>
    /// <returns>异步任务</returns>
    [Post("api/internal/job/clearJobTriggerRecord")]
    Task ClearJobTriggerRecord();
}
