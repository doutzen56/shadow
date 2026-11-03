import request from '/@/utils/request';

enum Api {
	// 请假单管理
	AddLeave = '/api/demo/leave/add',
	UpdateLeave = '/api/demo/leave/update',
	DeleteLeave = '/api/demo/leave/delete',
	SubmitLeave = '/api/demo/leave/submit',
	GetLeaveDetail = '/api/demo/leave/detail',
	GetLeavePage = '/api/demo/leave/page',
	GetBusinessData = '/api/demo/leave/getBusinessData',
}

/**
 * 添加请假单
 */
export const addLeave = (data: any) => {
	return request({
		url: Api.AddLeave,
		method: 'post',
		data,
	});
};

/**
 * 提交请假单并发起工作流
 */
export const submitLeave = (data: any) => {
	return request({
		url: Api.SubmitLeave,
		method: 'post',
		data,
	});
};

/**
 * 获取请假单详情
 */
export const getLeaveDetail = (params: { id: number }) => {
	return request({
		url: Api.GetLeaveDetail,
		method: 'get',
		params,
	});
};

/**
 * 获取请假单分页列表
 */
export const getLeavePage = (data: any) => {
	return request({
		url: Api.GetLeavePage,
		method: 'post',
		data,
	});
};

/**
 * 更新请假单
 */
export const updateLeave = (data: any) => {
	return request({
		url: Api.UpdateLeave,
		method: 'post',
		data,
	});
};

/**
 * 删除请假单
 */
export const deleteLeave = (data: any) => {
	return request({
		url: Api.DeleteLeave,
		method: 'post',
		data,
	});
};

/**
 * 根据业务ID获取业务数据（用于工作流表单自动填充）
 */
export const getBusinessData = (params: { businessId: number }) => {
	return request({
		url: Api.GetBusinessData,
		method: 'get',
		params,
	});
};
