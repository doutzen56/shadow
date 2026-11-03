/**
 * VxeTable 统一配置 Hook —— 全站 vxe-grid 的唯一入口
 *
 * 提供两类能力：
 *   1. `useVxeTable`：表格参数化配置（分页、工具栏、导出、打印、列定制 …）；
 *   2. `useGridPageParam` / `gridPageEvents` / `useGridQuery`：
 *      标准列表页的分页偏好读写、偏好回写、查询刷新。
 *
 * 典型用法：
 * ```ts
 * // 1) 必须在 state / options 之前创建：sortConfig、pagerConfig 需要在 setup 阶段就拿到恢复后的值
 * const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysConfig', {
 * 	pageSize: 50 as number,
 * 	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
 * });
 * const state = reactive({ queryParams: {}, localPageParam, ... });
 *
 * // 2) 表格参数
 * const options = useVxeTable<SysConfig>({ id: 'sysConfig', name: '...', columns: [...] }, {
 * 	proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
 * 	sortConfig: { defaultSort: localPageParam.defaultSort },
 * 	pagerConfig: { pageSize: localPageParam.pageSize },
 * });
 *
 * // 3) 查询与事件
 * const handleQuery = useGridQuery(xGrid, options);
 * const gridEvents: VxeGridListeners<SysConfig> = {
 * 	...gridPageEvents(localPageParamKey, localPageParam),
 * 	async cellDblclick({ row }) { ... },
 * };
 * ```
 */
import { reactive, type Ref } from 'vue';
import { dayjs } from 'element-plus';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useUserInfo } from '/@/stores/userInfo';
import { merge } from 'lodash-es';
import { VxeGridInstance, VxeGridListeners, VxeGridProps, VxeGridPropTypes, VxeComponentSizeType } from 'vxe-table';
import { VxeTablePropTypes } from 'vxe-pc-ui/types/components/table';
import { getAPI } from '/@/utils/axios-utils';
import { Local } from '/@/utils/storage';
import { SysColumnCustomApi } from '/@/api-services/system/api';

// 根据主题配置获取组件大小
const vxeSize: VxeComponentSizeType = useThemeConfig().themeConfig.globalComponentSize == 'small' ? 'mini' : useThemeConfig().themeConfig.globalComponentSize == 'default' ? 'small' : 'medium';

/**
 * 扩展 VxeTable 列配置接口，增加自定义属性
 * 动态提取 VxeGridPropTypes.Columns 的元素类型进行扩展，确保包含 field、title 等所有官方属性
 */
export type CustomColumnOptions<D = any> = (VxeGridPropTypes.Columns<D> extends (infer U)[] ? U : any) & {
	/**
	 * 字典导出配置
	 */
	dictExportConfig?: {
		/** 字典编码 */
		code: string;
		/** 是否多选 (逗号分隔) */
		multiple?: boolean;
	};
	/** 嵌套列 */
	children?: CustomColumnOptions<D>[];
};

/**
 * @param {String} id 表格唯一标识，为空时自动随机产生;
 * @param {String} id name:表格名称，与导出文件名有关;
 * @param {CustomColumnOptions<any>[]} columns 列配置;
 * @param {Boolean} showFooter 是否显示表尾;
 * @param {any} footerData 表尾数据;
 * @param {VxeTablePropTypes.FooterMethod<any>} footerMethod 表尾方法;
 * @param {Boolean} remoteCustom 是否使用服务端列配置;
 */
interface iVxeOption {
	id?: string;
	name?: string;
	columns: CustomColumnOptions<any>[];
	data?: VxeTablePropTypes.Data<any>;
	sortConfig?: VxeTablePropTypes.SortConfig<any>;
	showFooter?: boolean;
	footerData?: VxeTablePropTypes.FooterData;
	footerMethod?: VxeTablePropTypes.FooterMethod<any>;
	remoteCustom?: boolean;
}

/**
 * Vxe表格参数化Hook
 * @param {iVxeOption} opt 参数
 * @param {VxeGridProps<T>} extras 要覆盖的参数
 * @returns
 */
export const useVxeTable = <T>(opt: iVxeOption, extras?: VxeGridProps<T>) => {
	// // 自动处理字典导出逻辑
	// const userStore = useUserInfo();
	// const processColumns = (columns: CustomColumnOptions<any>[]) => {
	// 	columns.forEach((col) => {
	// 		if (col.dictExportConfig?.code) {
	// 			// 如果没有自定义 exportMethod，则自动注入
	// 			if (!col.exportMethod) {
	// 				col.exportMethod = ({ row }: any) => {
	// 					const val = row[(col as any).field];
	// 					if (val === null || val === undefined || val === '') return '';
	// 					// 处理多选字典 (multiple = true)
	// 					if (col.dictExportConfig?.multiple) {
	// 						const values = String(val).split(',');
	// 						return values.map((v: string) => userStore.getDictItemByValue(col.dictExportConfig!.code, v.trim())?.label || v).join(', ');
	// 					}
	// 					// 处理单选字典
	// 					return userStore.getDictItemByValue(col.dictExportConfig!.code, String(val))?.label || val;
	// 				};
	// 			}
	// 		}
	// 		// 递归处理嵌套列（如果有）
	// 		if (col.children) {
	// 			processColumns(col.children);
	// 		}
	// 	});
	// };
	// if (opt.columns) {
	// 	processColumns(opt.columns);
	// }

	// 创建tableId,表格id固定才可以记录调整列宽，再次刷新仍有效。
	opt.id = opt.id ? opt.id : opt.name;
	const options = reactive<VxeGridProps>({
		stripe: false,
		id: opt.id,
		height: 'auto',
		autoResize: true,
		size: vxeSize,
		loading: false,
		align: 'center', // 自动监听父元素的变化去重新计算表格（对于父元素可能存在动态变化、显示隐藏的容器中、列宽异常等场景中的可能会用到）
		// data: [] as Array<T>,
		columns: opt.columns,
		showFooter: opt.showFooter,
		footerData: opt.footerData,
		footerMethod: opt.footerMethod,
		toolbarConfig: {
			size: vxeSize,
			slots: { buttons: 'toolbar_buttons', tools: 'toolbar_tools' },
			refresh: true,
			refreshOptions: {
				code: 'query',
			},
			export: true,
			print: true,
			zoom: true,
			custom: true,
		},
		checkboxConfig: { range: true },
		rowClassName: ({ row }) => {
			// 如果满足条件（如：非活动状态），返回自定义类名
			if (row.activated === false) {
				return 'row-deactivate-hightlight';
			}
			return '';
		},
		// sortConfig: { trigger: 'cell', remote: true },
		exportConfig: {
			remote: false, // 设置使用服务端导出
			original: false, // 使用格式化后的数据
			filename: `${opt.name || '列表'}导出_${dayjs().format('YYYYMMDDHHmmss')}`,
			modes: ['current', 'selected', 'all'], // 当前页、选中项、所有
		},
		pagerConfig: {
			enabled: true,
			size: vxeSize,
			pageSize: 50,
		},
		printConfig: { sheetName: '' },
		// proxyConfig: {
		// 	enabled: true,
		// 	autoLoad: false,
		// 	sort: true,
		// 	props: {
		// 		list: 'data.result', // 不分页时
		// 		result: 'data.result.items', // 分页时
		// 		total: 'data.result.total',
		// 		message: 'data.message',
		// 	},
		// },
		customConfig: {
			storage: {
				// 是否启用 localStorage 本地保存，会将列操作状态保留在本地（需要有 id）
				visible: true, // 启用显示/隐藏列状态
				resizable: true, // 启用列宽状态
				sort: true, // 启用列顺序缓存
				fixed: true, // 启用冻结列状态缓存
			},
		},
	});
	// data优先级高于proxyConfig
	if (opt.data) {
		options.data = opt.data;
	} else {
		options.proxyConfig = {
			seq: useThemeConfig().themeConfig.tablePageSequence, // 分页序号
			enabled: true,
			autoLoad: false,
			sort: true,
			response: {
				list: 'data.result', // 全量
				result: 'data.result.items', // 分页
				total: 'data.result.total',
				message: 'data.message',
			},
		};
	}
	if (opt.sortConfig) {
		options.sortConfig = opt.sortConfig;
	} else {
		options.sortConfig = { remote: true };
	}
	if (opt.remoteCustom && options.customConfig != undefined) {
		// 重写默认的恢复自定义配置逻辑
		options.customConfig.restoreStore = async ({ id, storeData }) => {
			const { data } = await getAPI(SysColumnCustomApi).apiSysColumnCustomDetailGet(id);
			if (data.result?.fixedData) storeData.fixedData = data.result.fixedData as any;
			if (data.result?.resizableData) storeData.resizableData = data.result.resizableData;
			if (data.result?.sortData) storeData.sortData = data.result.sortData;
			if (data.result?.visibleData) storeData.visibleData = data.result.visibleData;
			return storeData;
		};
		// 重写默认的保存方法
		options.customConfig.updateStore = async ({ id, type, storeData }) => {
			if (type === 'reset') await getAPI(SysColumnCustomApi).apiSysColumnCustomResetPost({ gridId: id });
			else {
				await getAPI(SysColumnCustomApi).apiSysColumnCustomStorePost({
					gridId: id,
					fixedData: storeData?.fixedData as any,
					resizableData: storeData?.resizableData,
					sortData: storeData?.sortData,
					visibleData: storeData?.visibleData,
				});
			}
		};
	}
	return extras ? merge(options, extras) : options;
};

/* -------------------------------------------------------------------------- */
/*                          列表页通用逻辑（分页 / 排序 / 查询）                    */
/* -------------------------------------------------------------------------- */

/**
 * 排序方向：与 vxe 的 `VxeTablePropTypes.SortOrder` 保持一致
 * （'asc' / 'desc' / '' / null，后两者表示"未排序"）
 */
export type GridSortOrder = VxeTablePropTypes.SortOrder;

/**
 * 单页表格的分页 / 排序偏好
 */
export interface GridPageParam {
	/** 每页条数 */
	pageSize: number;
	/** 默认排序（field 为空串表示未排序）；descStr 是历史遗留字段，vxe 不消费 */
	defaultSort: { field: string; order: GridSortOrder; descStr: string };
}

/**
 * 偏好在 Local 里的键名（沿用历史格式，老用户已存的偏好不会丢）
 */
export const gridPageParamKey = (id: string) => `localPageParam:${id}`;

/**
 * 读取本地分页 / 排序偏好
 *
 * ⚠️ 必须在创建表格 options 之前调用：旧实现是在 `onMounted` 里恢复的，
 * 而 `sortConfig` / `pagerConfig` 在 setup 阶段就已经取值，两者存在顺序隐患。
 * 这里改为「创建即恢复」，业务页不再需要 `onMounted` 那一行。
 *
 * @param id 表格唯一标识（与 `useVxeTable` 的 id 保持一致）
 * @param init 缺省偏好，仅首次访问时生效
 */
export function useGridPageParam(id: string, init?: Partial<GridPageParam>) {
	const key = gridPageParamKey(id);
	const param = reactive<GridPageParam>({
		pageSize: 50,
		defaultSort: { field: 'id', order: 'asc', descStr: 'desc' },
		...init,
	});
	Object.assign(param, Local.get(key) || {});
	return { key, param };
}

/**
 * 表格事件：分页 / 排序变化后把偏好写回本地
 * 直接展开进 `gridEvents` 即可，业务自定义事件（双击行等）写在后面覆盖或并存。
 */
export function gridPageEvents(key: string, param: GridPageParam): Pick<VxeGridListeners<any>, 'pageChange' | 'sortChange'> {
	return {
		// 只对 pager-config 配置时有效，分页发生改变时会触发该事件
		async pageChange({ pageSize }) {
			param.pageSize = pageSize;
			Local.set(key, param);
		},
		// 当排序条件发生变化时会触发该事件
		async sortChange({ field, order }) {
			param.defaultSort = { field: field, order: order!, descStr: 'desc' };
			Local.set(key, param);
		},
	};
}

/**
 * 查询 / 刷新表格
 * @param xGrid 表格实例
 * @param options 表格参数（用于切换 loading）
 * @returns 查询函数，reset = true 回到第一页（点「查询」按钮），false 为当前页刷新（增删改后刷新列表）
 */
export function useGridQuery(xGrid: Ref<VxeGridInstance<any> | undefined>, options: VxeGridProps<any>) {
	return async (reset = false) => {
		options.loading = true;
		reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
		options.loading = false;
	};
}
