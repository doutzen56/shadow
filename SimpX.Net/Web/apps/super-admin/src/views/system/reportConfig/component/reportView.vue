<template>
	<div>
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', height: '100%' }">
			<TableSearch ref="tableSearch" :search="state.search" @search="onSearch" v-model="state.queryParams" />
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" v-bind="options" :loading="state.isLoading">
				<!-- <template #top>					
				</template> -->
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-FolderOpened" @click="handleExport"> {{ '导出' }} </el-button>
				</template>
				<template #toolbar_tools> </template>
			</vxe-grid>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="sysReportView">
import { onBeforeMount, reactive, ref, watch } from 'vue';
import { ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes, VxeTableDefines, VxeTablePropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useRoute } from 'vue-router';
import TableSearch from '/@/components/table/search.vue';
import { downloadByData, getFileName } from '/@/utils/download';

import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysReportConfigApi } from '/@/api-services/system/api';
import { SysReportField, SysReportLayoutConfig, SysReportParam } from '/@/api-services/system/models';

const xGrid = ref<VxeGridInstance>();
const tableSearch = ref<InstanceType<typeof TableSearch>>();

// 定义变量内容
const route = useRoute();
const reportConfigId: string = route.params.reportConfigId as string;

const state = reactive({
	search: [] as Array<TableSearchType>,
	/** 查询参数 */
	queryParams: {} as EmptyObjectType,
	/** 是否加载中 */
	isLoading: false,
	/** 布局配置 */
	layoutConfig: {} as SysReportLayoutConfig,
	/** 汇总信息 */
	totalInfo: {},
});

/** 监听 totalInfo 改变 */
watch(
	() => state.totalInfo,
	(val) => {
		if (state.totalInfo != null) options.showFooter = Object.keys(state.totalInfo).length > 0;
	}
);

onBeforeMount(() => {
	// 获取布局配置
	getAPI(SysReportConfigApi)
		.apiSysReportConfigGetLayoutConfigGet(Number(reportConfigId))
		.then((res) => {
			state.layoutConfig = res.data.result!;

			if (state.layoutConfig.fields) {
				options.columns!.push({ type: 'seq', width: 60, fixed: 'left' });

				const columns = state.layoutConfig.fields.map((r) => {
					return convertSysReportFieldToVxeColumn(r);
				});

				// 分组Id
				let groupIndex = 0;
				const newColumn: VxeTableDefines.ColumnOptions[] = [];
				state.layoutConfig.fields!.forEach((r) => {
					const column = columns.find((c) => c.field === r.fieldName)!;
					// 没有分组
					if (!r.groupTitle) {
						newColumn.push(column);
						return;
					}

					// 有分组
					let groupColumn = newColumn.find((c) => c.field?.startsWith('group') && c.title === r.groupTitle);
					if (!groupColumn) {
						groupColumn = { field: 'group' + groupIndex++, title: r.groupTitle, children: [] };
						newColumn.push(groupColumn);
					}

					// 添加到分组中
					groupColumn.children!.push(column);
				});

				// 添加默认记录组件
				newColumn.push({ field: 'record', title: '修改记录', width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } });

				options.columns!.push(...newColumn);
			}

			if (state.layoutConfig.params) {
				state.search = state.layoutConfig.params!.map((r) => {
					return convertSysReportParamToTableSearchType(r);
				});

				// 默认值处理
				state.layoutConfig.params!.forEach((r) => {
					if (r.defaultValue) state.queryParams[r.paramName!] = r.defaultValue;
				});
			}
		});
});

/** 表尾数据获取函数 */
const footerMethod: VxeTablePropTypes.FooterMethod<any> = ({ columns }) => {
	return [
		columns!.map((column, columnIndex) => {
			if (columnIndex === 0) return '合计';
			if (state.totalInfo && (state.totalInfo as any)[column.field] !== undefined) return (state.totalInfo as any)[column.field];
			return '';
		}),
	];
};

// 表格参数配置
const options = useVxeTable<any>(
	{
		id: `sysReportView_${reportConfigId}`,
		name: '报表查看',
		columns: [],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// columnConfig: {
		// 	width: 100, // 列默认宽度
		// },
		pagerConfig: { enabled: false },
		showFooter: false,
		footerMethod: footerMethod,
	}
);

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	return getAPI(SysReportConfigApi)
		.apiSysReportConfigExecuteSqlScriptPost({ id: Number(reportConfigId), execParams: state.queryParams })
		.then((res) => {
			state.totalInfo = res.data.extras;
			return res;
		});
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

/** 搜索点击时表单回调 */
const onSearch = (data: EmptyObjectType) => {
	handleQuery(true);
};

/**导出 */
const handleExport = () => {
	ElMessageBox.confirm('确定导出?', '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(() => {
		state.isLoading = true;
		getAPI(SysReportConfigApi)
			.apiSysReportConfigExportToExcelPost({ id: Number(reportConfigId), execParams: state.queryParams }, { responseType: 'blob' })
			.then((res) => {
				var fileName = getFileName(res.headers);
				downloadByData(res.data as any, fileName);
			})
			.finally(() => {
				state.isLoading = false;
			});
	});
};

/**
 * SysReportField 转换成 ColumnOptions
 * @param column 报表字段
 */
const convertSysReportFieldToVxeColumn = (column: SysReportField): VxeTableDefines.ColumnOptions => {
	return {
		field: column.fieldName,
		title: column.title ?? column.fieldName,
		width: column.width && column.width > 0 ? column.width : undefined,
		visible: column.visible,
	} as VxeTableDefines.ColumnOptions;
};

/**
 * SysReportParam 转换成 TableSearchType
 * @param column 报表参数
 * @returns
 */
const convertSysReportParamToTableSearchType = (column: SysReportParam) => {
	// 转换控件类型
	let type: string = '';
	switch (column.inputCtrl as unknown as string) {
		case 'Input':
			type = 'input';
			break;
		case 'Date':
			type = 'date';
			break;
		case 'Select':
			type = 'select';
			break;
		default:
			console.warn(`控件类型：${column.inputCtrl} 未支持，默认使用 input 控件`);
			type = 'input';
			break;
	}
	// // 处理下拉选项
	// let options: SelectOptionType[] = [];
	// if (column.options && column.options.length > 0) {
	// 	options = column.options.map((o) => ({ label: o.title!, value: o.value! }));
	// }
	return {
		label: column.title,
		prop: column.paramName,
		type: type,
		// options: options,
	} as TableSearchType;
};
</script>

<style scoped lang="scss"></style>
