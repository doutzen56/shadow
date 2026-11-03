using SimpX.Net.Contracts.Jobs;

namespace SimpX.Net.Application.SuperAdmin;

/// <summary>
/// 总后台作业远程管理（路由 /api/platform/job/*）。
/// 签名转发给 Schedule 宿主；只允许总后台进程调用。
/// </summary>
[ApiDescriptionSettings(JobManagementRoutes.Group, Module = "platform", Name = JobManagementRoutes.Resource, Order = 40, Description = "作业任务")]
public class PlatformJobAppService : IDynamicApiController, ITransient
{
    private readonly IJobManagementApi _jobApi;

    /// <summary>
    /// 注入 Job 作业调度的声明式客户端。
    /// </summary>
    /// <param name="jobApi">Job 作业调度接口</param>
    public PlatformJobAppService(IJobManagementApi jobApi)
    {
        _jobApi = jobApi;
    }

    /// <summary>
    /// 只允许总后台进程调用。
    /// </summary>
    private static void EnsurePlatformHost()
    {
        if (!HostTenantMode.IsPlatformHost)
            throw Oops.Oh(ErrorCodeEnum.SA001);
    }

    /// <summary>获取作业分页列表 ⏰</summary>
    /// <param name="input">查询条件</param>
    /// <returns>作业分页</returns>
    [DisplayName("获取作业分页列表")]
    public Task<SqlSugarPagedList<JobDetailOutput>> PageJobDetail(PageJobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.PageJobDetail(input);
    }

    /// <summary>获取作业组名称集合 ⏰</summary>
    /// <returns>作业组名称</returns>
    [DisplayName("获取作业组名称集合")]
    public Task<List<string>> ListJobGroup()
    {
        EnsurePlatformHost();
        return _jobApi.ListJobGroup();
    }

    /// <summary>添加作业 ⏰</summary>
    /// <param name="input">作业信息</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "AddJobDetail"), HttpPost]
    [DisplayName("添加作业")]
    public Task AddJobDetail(AddJobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.AddJobDetail(input);
    }

    /// <summary>更新作业 ⏰</summary>
    /// <param name="input">作业信息</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "UpdateJobDetail"), HttpPost]
    [DisplayName("更新作业")]
    public Task UpdateJobDetail(UpdateJobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.UpdateJobDetail(input);
    }

    /// <summary>删除作业 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "DeleteJobDetail"), HttpPost]
    [DisplayName("删除作业")]
    public Task DeleteJobDetail(DeleteJobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.DeleteJobDetail(input);
    }

    /// <summary>获取触发器列表 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>触发器列表</returns>
    [DisplayName("获取触发器列表")]
    public Task<List<SysJobTrigger>> GetJobTriggerList([FromQuery] JobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.GetJobTriggerList(input?.JobId);
    }

    /// <summary>添加触发器 ⏰</summary>
    /// <param name="input">触发器信息</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "AddJobTrigger"), HttpPost]
    [DisplayName("添加触发器")]
    public Task AddJobTrigger(AddJobTriggerInput input)
    {
        EnsurePlatformHost();
        return _jobApi.AddJobTrigger(input);
    }

    /// <summary>更新触发器 ⏰</summary>
    /// <param name="input">触发器信息</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "UpdateJobTrigger"), HttpPost]
    [DisplayName("更新触发器")]
    public Task UpdateJobTrigger(UpdateJobTriggerInput input)
    {
        EnsurePlatformHost();
        return _jobApi.UpdateJobTrigger(input);
    }

    /// <summary>删除触发器 ⏰</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "DeleteJobTrigger"), HttpPost]
    [DisplayName("删除触发器")]
    public Task DeleteJobTrigger(DeleteJobTriggerInput input)
    {
        EnsurePlatformHost();
        return _jobApi.DeleteJobTrigger(input);
    }

    /// <summary>暂停所有作业 ⏰</summary>
    /// <returns>异步任务</returns>
    [DisplayName("暂停所有作业")]
    public Task PauseAllJob()
    {
        EnsurePlatformHost();
        return _jobApi.PauseAllJob();
    }

    /// <summary>启动所有作业 ⏰</summary>
    /// <returns>异步任务</returns>
    [DisplayName("启动所有作业")]
    public Task StartAllJob()
    {
        EnsurePlatformHost();
        return _jobApi.StartAllJob();
    }

    /// <summary>暂停作业 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("暂停作业")]
    public Task PauseJob(JobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.PauseJob(input);
    }

    /// <summary>启动作业 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("启动作业")]
    public Task StartJob(JobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.StartJob(input);
    }

    /// <summary>取消作业 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("取消作业")]
    public Task CancelJob(JobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.CancelJob(input);
    }

    /// <summary>执行作业 ⏰</summary>
    /// <param name="input">作业编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("执行作业")]
    public Task RunJob(JobDetailInput input)
    {
        EnsurePlatformHost();
        return _jobApi.RunJob(input);
    }

    /// <summary>暂停触发器 ⏰</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("暂停触发器")]
    public Task PauseTrigger(JobTriggerInput input)
    {
        EnsurePlatformHost();
        return _jobApi.PauseTrigger(input);
    }

    /// <summary>启动触发器 ⏰</summary>
    /// <param name="input">作业与触发器编号</param>
    /// <returns>异步任务</returns>
    [DisplayName("启动触发器")]
    public Task StartTrigger(JobTriggerInput input)
    {
        EnsurePlatformHost();
        return _jobApi.StartTrigger(input);
    }

    /// <summary>强制唤醒作业调度器 ⏰</summary>
    /// <returns>异步任务</returns>
    [DisplayName("强制唤醒作业调度器")]
    public Task CancelSleep()
    {
        EnsurePlatformHost();
        return _jobApi.CancelSleep();
    }

    /// <summary>强制触发所有作业持久化 ⏰</summary>
    /// <returns>异步任务</returns>
    [DisplayName("强制触发所有作业持久化")]
    public Task PersistAll()
    {
        EnsurePlatformHost();
        return _jobApi.PersistAll();
    }

    /// <summary>获取集群列表 ⏰</summary>
    /// <returns>集群节点列表</returns>
    [DisplayName("获取集群列表")]
    public Task<List<SysJobCluster>> GetJobClusterList()
    {
        EnsurePlatformHost();
        return _jobApi.GetJobClusterList();
    }

    /// <summary>获取作业触发器运行记录分页列表 ⏰</summary>
    /// <param name="input">查询条件</param>
    /// <returns>运行记录分页</returns>
    [DisplayName("获取作业触发器运行记录分页列表")]
    public Task<SqlSugarPagedList<SysJobTriggerRecord>> PageJobTriggerRecord(PageJobTriggerRecordInput input)
    {
        EnsurePlatformHost();
        return _jobApi.PageJobTriggerRecord(input);
    }

    /// <summary>清空作业触发器运行记录 🔖</summary>
    /// <returns>异步任务</returns>
    [ApiDescriptionSettings(Name = "ClearJobTriggerRecord"), HttpPost]
    [DisplayName("清空作业触发器运行记录")]
    public Task ClearJobTriggerRecord()
    {
        EnsurePlatformHost();
        return _jobApi.ClearJobTriggerRecord();
    }
}
