/**
 * 业务模块配置
 * 定义业务模块与详情组件、API函数的映射关系
 * 工作流插件通过此配置中心获取业务模块的处理逻辑，实现高内聚低耦合
 *
 * 注意：此文件是工作流插件特有的工具函数，放在工作流插件目录下，避免覆盖原项目
 */

// ==================== TypeScript 接口定义 ====================

/**
 * 业务模块选项
 */
export interface BusinessModuleOption {
	label: string;
	value: string;
}

/**
 * 业务数据列表查询参数
 */
export interface BusinessDataListParams {
	page: number;
	pageSize: number;
	keyword?: string;
	[key: string]: any;
}

/**
 * 业务数据列表响应
 */
export interface BusinessDataListResponse {
	items: any[];
	total: number;
}

/**
 * 业务数据详情参数
 */
export interface BusinessDataDetailParams {
	businessId: number;
	[key: string]: any;
}

/**
 * 业务数据提交参数
 */
export interface BusinessDataSubmitParams {
	businessId: number;
	definitionId: number;
	workflowCode?: string;
	remark?: string;
	[key: string]: any;
}

/**
 * 表格列配置
 */
export interface TableColumnConfig {
	prop?: string;
	label: string;
	width?: number | string;
	minWidth?: number | string;
	align?: 'left' | 'center' | 'right';
	showOverflowTooltip?: boolean;
	fixed?: boolean | 'left' | 'right';
	/** 格式化函数，参数：row(行数据), column(列配置), cellValue(单元格值) */
	formatter?: (row: any, column?: any, cellValue?: any) => string;
	/** 自定义渲染函数，返回要渲染的内容（可以是字符串、对象 {type, text} 用于 el-tag 等） */
	render?: (row: any) => any;
}

/**
 * 业务模块完整配置
 */
export interface BusinessModuleConfig {
	/** 模块显示名称 */
	label: string;
	/** 模块值（唯一标识） */
	value: string;
	/** 获取业务数据列表的 API 函数 */
	getListApi: (params: BusinessDataListParams) => Promise<{ data: BusinessDataListResponse | any }>;
	/** 获取业务数据详情的 API 函数（用于填充表单） */
	getDetailApi: (params: BusinessDataDetailParams) => Promise<{ data: any }>;
	/** 提交业务数据并发起工作流的 API 函数 */
	submitApi: (params: BusinessDataSubmitParams) => Promise<{ data: any }>;
	/** 业务模块详情组件（可选） */
	detailComponent?: () => Promise<any>;
	/** 是否启用该业务模块 */
	enabled?: boolean;
	/** 原业务表单预览页的路由地址 */
	formPreviewRoute?: string;
}

// ==================== 工厂函数：创建业务模块配置 ====================

/**
 * 业务模块 API 接口集合
 * 定义业务模块所需的三个核心 API 函数的导入路径
 */
export interface BusinessModuleApis {
	/** 分页列表 API 导出名称，如 'getLeavePage' */
	getListApiName: string;
	/** 详情 API 导出名称，如 'getLeaveDetail' */
	getDetailApiName: string;
	/** 提交 API 导出名称，如 'submitLeave' */
	submitApiName: string;
}

/**
 * 业务模块配置参数
 * 用于创建业务模块配置的完整参数
 */
export interface BusinessModuleCreateOptions {
	/** 模块显示名称，如 '请假模块' */
	label: string;
	/** 模块值（唯一标识），如 'Leave' */
	value: string;
	/** API 文件路径，如 '/@/api/workflow/demo_leave' */
	apiPath: string;
	/** API 接口名称集合 */
	apis: BusinessModuleApis;
	/** 详情组件路径（可选），如 '/@/views/demo/leave/LeaveDetail.vue' */
	detailComponentPath?: string;
	/** 是否启用该业务模块（默认 true），为 false 时不会出现在业务模块选项中 */
	enabled?: boolean;
	/** 原业务表单预览页的路由地址（可选），用于在工作流详情页中跳转查看原表单 */
	formPreviewRoute?: string;
	/** 详情接口参数适配器（可选），用于兼容非标准 id 参数（可返回对象或原始值） */
	detailParamsResolver?: (params: BusinessDataDetailParams) => any;
	/** 提交接口参数适配器（可选），用于兼容非标准 submit 入参 */
	submitParamsResolver?: (params: BusinessDataSubmitParams) => any;
}

// 通过 import.meta.glob 预收集模块，避免运行时直接 import('/@/xxx')
// 在生产环境中这类别名路径会被浏览器按 URL 请求，反向代理或子路径部署时很容易失效。
const workflowApiGlob = import.meta.glob(['/src/api/**/*.ts', '/src/api-services/**/*.ts']);
const workflowViewGlob = import.meta.glob('/src/views/**/*.vue');

const apiFunctionCache = new Map<string, Promise<any>>();
const moduleCache = new Map<string, Promise<any>>();

const normalizeGlobKey = (key: string) => key.replace(/\\/g, '/');

const resolveWorkflowImportPath = (aliasedPath: string, extension: '.ts' | '.vue'): string => {
	const normalizedPath = normalizeGlobKey(aliasedPath.trim());
	const srcPath = normalizedPath.startsWith('/@/') ? normalizedPath.replace(/^\/@\//, '/src/') : normalizedPath;

	return srcPath.endsWith(extension) ? srcPath : `${srcPath}${extension}`;
};

const findGlobLoader = (globMap: Record<string, () => Promise<any>>, modulePath: string): (() => Promise<any>) | undefined => {
	const normalizedPath = normalizeGlobKey(modulePath);
	return globMap[normalizedPath];
};

const importWorkflowModule = async (globMap: Record<string, () => Promise<any>>, modulePath: string, extension: '.ts' | '.vue') => {
	const resolvedPath = resolveWorkflowImportPath(modulePath, extension);
	const cacheKey = `${extension}:${resolvedPath}`;

	if (!moduleCache.has(cacheKey)) {
		const loader = findGlobLoader(globMap, resolvedPath);
		if (!loader) {
			throw new Error(`[workflow] 未找到模块: ${modulePath}（解析为 ${resolvedPath}）`);
		}

		moduleCache.set(cacheKey, loader());
	}

	return moduleCache.get(cacheKey)!;
};

/**
 * 动态导入 API 函数
 * 根据 API 名称从指定的 API 路径中导入对应的函数
 */
const importApiFunction = async (apiPath: string, apiName: string) => {
	const cacheKey = `${apiPath}#${apiName}`;
	if (!apiFunctionCache.has(cacheKey)) {
		apiFunctionCache.set(
			cacheKey,
			importWorkflowModule(workflowApiGlob, apiPath, '.ts').then((module) => {
				// 1) 优先查找直接导出的独立函数（兼容手写 API 文件，如 demo_leave）
				let fn = module[apiName];
				if (typeof fn === 'function') {
					return fn;
				}

				// 2) 兼容 Swagger Codegen 生成的 Factory 模式：
				//    函数名如 apiStkAdjustmentSubmitPost 不是直接导出的，
				//    而是通过 XxxApiFactory() 返回的对象上的方法
				const factoryKey = Object.keys(module).find(
					(k) => k.endsWith('ApiFactory') && typeof module[k] === 'function'
				);
				if (factoryKey) {
					const factoryObj = module[factoryKey]();
					fn = factoryObj[apiName];
					if (typeof fn === 'function') {
						return fn;
					}
				}

				throw new Error(`[workflow] API 模块 ${apiPath} 未导出函数: ${apiName}`);
			})
		);
	}

	return apiFunctionCache.get(cacheKey)!;
};

/**
 * 【工厂函数】创建业务模块配置
 * 通过配置参数自动生成标准的业务模块配置对象
 *
 * @param options - 业务模块配置参数
 * @returns 业务模块完整配置
 *
 * @example
 * // 创建请假模块配置
 * const leaveConfig = createBusinessModuleConfig({
 *   label: '请假模块',
 *   value: 'Leave',
 *   apiPath: '/@/api/workflow/demo_leave',
 *   apis: {
 *     getListApiName: 'getLeavePage',
 *     getDetailApiName: 'getLeaveDetail',
 *     submitApiName: 'submitLeave'
 *   },
 *   tableColumns: [...],
 *   detailComponentPath: '/@/views/demo/leave/LeaveDetail.vue'
 * });
 */
export const createBusinessModuleConfig = (options: BusinessModuleCreateOptions): BusinessModuleConfig => {
	const { label, value, apiPath, apis, detailComponentPath, enabled = true, formPreviewRoute, detailParamsResolver, submitParamsResolver } = options;

	return {
		label,
		value,
		enabled,
		formPreviewRoute,
		// 获取业务数据列表
		getListApi: async (params: BusinessDataListParams) => {
			const apiFunction = await importApiFunction(apiPath, apis.getListApiName);
			return apiFunction({
				...params, // 支持传递其他自定义参数
				page: params.page,
				pageSize: params.pageSize,
				keyword: params.keyword || undefined,
			});
		},
		// 获取业务数据详情
		getDetailApi: async (params: BusinessDataDetailParams) => {
			const apiFunction = await importApiFunction(apiPath, apis.getDetailApiName);
			const args = detailParamsResolver ? detailParamsResolver(params) : { ...params, id: params.businessId };
			return Array.isArray(args) ? apiFunction(...args) : apiFunction(args);
		},
		// 提交业务数据并发起工作流
		submitApi: async (params: BusinessDataSubmitParams) => {
			const apiFunction = await importApiFunction(apiPath, apis.submitApiName);
			const args = submitParamsResolver
				? submitParamsResolver(params)
				: {
						...params,
						businessId: params.businessId,
						definitionId: params.definitionId,
						workflowCode: params.workflowCode || undefined,
						remark: params.remark || undefined,
					};
			return Array.isArray(args) ? apiFunction(...args) : apiFunction(args);
		},
		// 详情组件（可选）
		detailComponent: detailComponentPath ? () => importWorkflowModule(workflowViewGlob, detailComponentPath, '.vue') : undefined,
	};
};

// ==================== 业务模块选项配置 ====================

/**
 * 业务模块选项列表
 * 【自动从 workflowForm 目录读取】
 * 新增业务模块时，只需在 workflowForm 目录下创建配置文件，并设置 enabled: true
 */
import { workflowFormConfigs } from '/@/views/workFlow/workflowForm';

export const businessModuleOptions: BusinessModuleOption[] = Object.values(workflowFormConfigs)
	.filter((config) => config.enabled !== false) // 只保留启用的业务模块
	.map((config) => ({
		label: config.label,
		value: config.value,
	}))
	.sort((a, b) => a.label.localeCompare(b.label, 'zh-CN'));

// ==================== 业务模块完整配置映射 ====================

/**
 * 业务模块完整配置映射
 * 每个业务模块在此注册自己的完整配置
 *
 * 使用工厂函数 createBusinessModuleConfig 可以大大减少重复代码：
 * - 只需传递一次 API 路径
 * - 只需指定 API 函数的导出名称
 * - 配置更加简洁、易维护
 *
 * 【重要】从 v2.0 开始，业务表单配置已移至 /@/views/workFlow/workflowForm 目录
 * 新增业务模块时，请在 workflowForm 目录下创建独立的配置文件
 * 无需修改此文件
 */

export const businessModuleConfigMap: Record<string, BusinessModuleConfig> = {
	// 从 workflowForm 目录自动加载所有业务模块配置
	...Object.fromEntries(Object.entries(workflowFormConfigs).map(([key, config]) => [key, createBusinessModuleConfig(config)])),
};

// ==================== 工具函数 ====================

/**
 * 获取业务模块选项列表
 */
export const getBusinessModuleOptions = (): BusinessModuleOption[] => {
	return businessModuleOptions;
};

/**
 * 获取业务模块完整配置
 */
export const getBusinessModuleConfig = (businessModule: string): BusinessModuleConfig | undefined => {
	return businessModuleConfigMap[businessModule];
};

/**
 * 获取业务模块详情组件
 */
export const getBusinessModuleComponent = (businessModule: string) => {
	const config = businessModuleConfigMap[businessModule];
	return config?.detailComponent;
};

/**
 * 获取业务模块详情API
 */
export const getBusinessModuleApi = (businessModule: string) => {
	const config = businessModuleConfigMap[businessModule];
	if (config?.getDetailApi) {
		return async (params: { businessId: number }) => {
			return config.getDetailApi({ businessId: params.businessId });
		};
	}
	return undefined;
};

/**
 * 检查业务模块是否支持
 */
export const isBusinessModuleSupported = (businessModule: string): boolean => {
	return !!businessModuleConfigMap[businessModule];
};
