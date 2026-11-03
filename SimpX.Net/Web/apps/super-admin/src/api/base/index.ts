import { service, cancelRequest } from '/@/utils/request';
import { AxiosRequestConfig, AxiosResponse } from 'axios';

// 接口基类
export const useBaseApi = (module: string) => {
	const baseUrl = `/api/${module}/`;
	const request = <T>(config: AxiosRequestConfig<T>, cancel: boolean = false) => {
		if (cancel) {
			cancelRequest(config.url || '');
			return Promise.resolve({} as AxiosResponse<any, any>);
		}
		return service(config);
	};
	return {
		baseUrl: baseUrl,
		request: request,
		// 自定义的其它接口，使用时 custom(接口名，Method)
		custom: function (apiName:string, method:string='post') {
			return function (data: any, cancel: boolean = false) {
				return request(
					{
						url: baseUrl + apiName,
						method: method,
						data,
					},
					cancel
				);
			}
		},
		page: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'page',
					method: 'post',
					data,
				},
				cancel
			);
		},
		list: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'list',
					method: 'post',
					data,
				},
				cancel
			);
		},
		detail: function (id: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'detail',
					method: 'get',
					data: { id },
				},
				cancel
			);
		},
		dropdownData: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'dropdownData',
					method: 'post',
					data,
				},
				cancel
			);
		},
		add: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'add',
					method: 'post',
					data,
				},
				cancel
			);
		},
		update: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'update',
					method: 'post',
					data,
				},
				cancel
			);
		},
		setStatus: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'setStatus',
					method: 'post',
					data,
				},
				cancel
			);
		},
		delete: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'delete',
					method: 'post',
					data,
				},
				cancel
			);
		},
		batchDelete: function (data: any, cancel: boolean = false) {
			return request(
				{
					url: baseUrl + 'batchDelete',
					method: 'post',
					data,
				},
				cancel
			);
		},
		exportData: function (data: any, cancel: boolean = false) {
			return request(
				{
					responseType: 'arraybuffer',
					url: baseUrl + 'export',
					method: 'post',
					data,
				},
				cancel
			);
		},
		downloadTemplate: function (config?: any, cancel: boolean = false) {
			return request(
				{
					responseType: 'arraybuffer',
					url: baseUrl + 'import',
					method: 'get',
					...config,
				},
				cancel
			);
		},
		importData: function (file: any, config?: any, cancel: boolean = false) {
			const formData = new FormData();
			formData.append('file', file);
			return request(
				{
					responseType: 'arraybuffer',
					url: baseUrl + 'import',
					method: 'post',
					data: formData,
					// 先展开 config，然后设置 headers，确保 headers 不被覆盖
					...config,
					// 不手动设置 Content-Type，让浏览器自动设置（包括 boundary）
					headers: {
						ignoreCancelToken: true,
						...(config?.headers || {}),
					},
				},
				cancel
			);
		},
		uploadFile: function (params: any, action: string, cancel: boolean = false) {
			const formData = new FormData();
			formData.append('file', params.file);
			// 自定义参数
			if (params.data) {
				Object.keys(params.data).forEach((key) => {
					const value = params.data![key];
					if (Array.isArray(value)) {
						value.forEach((item) => formData.append(`${key}[]`, item));
						return;
					}
					formData.append(key, params.data![key]);
				});
			}
			return request(
				{
					url: baseUrl + action,
					method: 'POST',
					data: formData,
					headers: {
						'Content-Type': 'multipart/form-data;charset=UTF-8',
						ignoreCancelToken: true,
					},
				},
				cancel
			);
		},
	};
};
