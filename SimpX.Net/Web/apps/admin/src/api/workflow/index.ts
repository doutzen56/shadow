import request from '/@/utils/request';

enum Api {
	// 流程管理
	FlowQuery = '/api/workflow/flow/page',
	Flow = '/api/workflow/flow',
	FlowList = '/api/workflow/flow/list',
	FlowDetail = '/api/workflow/flow/detail',
	FlowStatus = '/api/workflow/flow/status',
	FlowDesign = '/api/workflow/flow/design',

	// 流程定义
	GetDefinitions = '/api/workflow/definitions',
	RegisterWorkflow = '/api/workflow/registerWorkflow',
	SetDefinitionStatus = '/api/workflow/setDefinitionStatus',
	DefinitionStatus = '/api/workflow/definitions/status',
	DeleteDefinition = '/api/workflow/definitions',

	// 流程实例
	StartWorkflow = '/api/workflow/startWorkflow',
	GetWorkflowInstance = '/api/workflow/getWorkflowInstance',
	InstancesPage = '/api/workflow/instances/page',
	MyInstances = '/api/workflow/myInstances',
	DeleteInstance = '/api/workflow/deleteInstance',

	// 步骤和日志
	GetWorkflowSteps = '/api/workflow/getWorkflowSteps',
	GetWorkflowLogs = '/api/workflow/getWorkflowLogs',
	GetAllFlowNodes = '/api/workflow/getAllFlowNodes',

	// 待办任务
	Todos = '/api/workflow/todos',
	TodosCc = '/api/workflow/todos/cc',
	TodosHistory = '/api/workflow/todos/history',
	ApproveWorkflow = '/api/workflow/approveWorkflow',
	TodoStats = '/api/workflow/todos/stats',

	// 流程控制
	CancelWorkflow = '/api/workflow/cancelWorkflow',
	PauseWorkflow = '/api/workflow/pauseWorkflow',
	ResumeWorkflow = '/api/workflow/resumeWorkflow',

	// 表单
	FormPage = '/api/workflow/form/page',
	Form = '/api/workflow/form',
	FormDetail = '/api/workflow/form/detail',
	FormList = '/api/workflow/form/list',
	FormDesign = '/api/workflow/form/design',
	FormPublish = '/api/workflow/form/publish',
	FormUnpublish = '/api/workflow/form/unpublish',
	FormStatus = '/api/workflow/form/status',

	// 业务模块字段
	BusinessModuleFields = '/api/workflow/businessModule',
}

// ============ 流程管理相关 ============

// 分页查询流程
export const getFlowPage = (data: any) =>
	request({
		url: Api.FlowQuery,
		method: 'post',
		data,
	});

// 获取流程列表（下拉、分类筛选用）
export const getFlowList = (params?: any) =>
	request({
		url: Api.FlowList,
		method: 'get',
		params,
	});

// 创建流程
export const createFlow = (data: any) =>
	request({
		url: Api.Flow,
		method: 'post',
		data,
	});

// 更新流程
export const updateFlow = (data: any) =>
	request({
		url: Api.Flow,
		method: 'put',
		data,
	});

// 删除流程
export const deleteFlow = (data: any) =>
	request({
		url: Api.Flow,
		method: 'delete',
		data,
	});

// 获取流程详情
export const getFlowDetail = (params: any) =>
	request({
		url: Api.FlowDetail,
		method: 'get',
		params,
	});

// 更新流程状态
export const setFlowStatus = (data: any) =>
	request({
		url: Api.FlowStatus,
		method: 'post',
		data,
	});

// 保存流程设计
export const saveFlowDesign = (data: any) =>
	request({
		url: Api.FlowDesign,
		method: 'post',
		data,
	});

// ============ 流程定义相关 ============

// 获取流程定义列表
export const getDefinitions = (params?: any) =>
	request({
		url: Api.GetDefinitions,
		method: 'get',
		params,
	});

// 发布工作流
export const registerWorkflow = (data: any) =>
	request({
		url: Api.RegisterWorkflow,
		method: 'post',
		data,
	});

// 设置定义状态（启用/停用）
export const setDefinitionStatus = (data: any) =>
	request({
		url: Api.SetDefinitionStatus,
		method: 'post',
		data,
	});

// 设置流程定义状态（启用/停用）
export const setDefinitionStatusV2 = (data: any) =>
	request({
		url: Api.DefinitionStatus,
		method: 'post',
		data,
	});

// 删除流程定义
export const deleteDefinition = (data: any) =>
	request({
		url: Api.DeleteDefinition,
		method: 'delete',
		data,
	});

// ============ 流程实例相关 ============

// 启动工作流
export const startWorkflowInstance = (data: any) =>
	request({
		url: Api.StartWorkflow,
		method: 'post',
		data,
	});

// 获取工作流实例详情
export const getWorkflowInstance = (instanceCode: string) =>
	request({
		url: Api.GetWorkflowInstance,
		method: 'get',
		params: { instanceCode },
	});

// 分页查询工作流实例
export const instancesPage = (data: any) =>
	request({
		url: Api.InstancesPage,
		method: 'post',
		data,
	});

// 获取我发起的流程
export const myInstances = (data: any) =>
	request({
		url: Api.MyInstances,
		method: 'post',
		data,
	});

// 获取工作流步骤列表
export const getWorkflowSteps = (instanceCode: string) =>
	request({
		url: Api.GetWorkflowSteps,
		method: 'get',
		params: { instanceCode },
	});

// 获取工作流执行日志
export const getWorkflowLogs = (instanceCode: string) =>
	request({
		url: Api.GetWorkflowLogs,
		method: 'get',
		params: { instanceCode },
	});

// 获取流程的全部节点信息
export const getAllFlowNodes = (instanceCode: string) =>
	request({
		url: Api.GetAllFlowNodes,
		method: 'get',
		params: { instanceCode },
	});

// 删除工作流实例
export const deleteWorkflowInstance = (data: any) =>
	request({
		url: Api.DeleteInstance,
		method: 'delete',
		data,
	});

// ============ 待办任务相关 ============

// 分页查询待办任务
export const getTodos = (data: any) =>
	request({
		url: Api.Todos,
		method: 'post',
		data,
	});

// 分页查询抄送任务（抄送我的）
export const getCcTodos = (data: any) =>
	request({
		url: Api.TodosCc,
		method: 'post',
		data,
	});

// 分页查询历史待办任务
export const getTodosHistory = (data: any) =>
	request({
		url: Api.TodosHistory,
		method: 'post',
		data,
	});

// 删除抄送/通知待办
export const deleteTodo = (data: any) =>
	request({
		url: Api.Todos,
		method: 'delete',
		data,
	});

// 审批工作流
export const approveWorkflow = (data: any) =>
	request({
		url: Api.ApproveWorkflow,
		method: 'post',
		data,
	});

// 获取工作流数据统计
export const GetTodoStats = (data: any) =>
	request({
		url: Api.TodoStats,
		method: 'post',
		data,
	});

// ============ 流程控制相关 ============

// 取消流程
export const cancelWorkflow = (data: any) =>
	request({
		url: Api.CancelWorkflow,
		method: 'post',
		data,
	});

// 暂停流程
export const pauseWorkflow = (data: any) =>
	request({
		url: Api.PauseWorkflow,
		method: 'post',
		data,
	});

// 恢复流程
export const resumeWorkflow = (data: any) =>
	request({
		url: Api.ResumeWorkflow,
		method: 'post',
		data,
	});

// ============ 表单相关 ============

export const getWorkflowForms = (data: any) =>
	request({
		url: Api.FormPage,
		method: 'post',
		data,
	});

export const getWorkflowFormDetail = (params: any) =>
	request({
		url: Api.FormDetail,
		method: 'get',
		params,
	});

export const createWorkflowForm = (data: any) =>
	request({
		url: Api.Form,
		method: 'post',
		data,
	});

export const updateWorkflowForm = (data: any) =>
	request({
		url: Api.Form,
		method: 'put',
		data,
	});

export const deleteWorkflowForm = (data: any) =>
	request({
		url: Api.Form,
		method: 'delete',
		data,
	});

export const saveWorkflowFormDesign = (data: any) =>
	request({
		url: Api.FormDesign,
		method: 'post',
		data,
	});

export const publishWorkflowForm = (data: any) =>
	request({
		url: Api.FormPublish,
		method: 'post',
		data,
	});

export const listWorkflowForms = (params?: any) =>
	request({
		url: Api.FormList,
		method: 'get',
		params,
	});

export const unpublishWorkflowForm = (data: any) =>
	request({
		url: Api.FormUnpublish,
		method: 'post',
		data,
	});

export const setWorkflowFormStatus = (data: any) =>
	request({
		url: Api.FormStatus,
		method: 'post',
		data,
	});

// ============ 业务模块字段相关 ============

// 获取业务模块字段定义列表
export const getBusinessModuleFields = (businessModule: string) =>
	request({
		url: `${Api.BusinessModuleFields}/${businessModule}/fields`,
		method: 'get',
	});
