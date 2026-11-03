<template>
	<div class="syslogevent-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px' }">
			<scEcharts v-if="echartsOption.series.data" height="200px" :option="echartsOption" @clickData="clickData"></scEcharts>
		</el-card>

		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="开始时间" prop="name">
							<el-date-picker
								v-model="state.queryParams.startTime"
								type="datetime"
								placeholder="开始时间"
								format="YYYY-MM-DD HH:mm:ss"
								value-format="YYYY-MM-DD HH:mm:ss"
								:shortcuts="shortcuts"
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="结束时间" prop="code">
							<el-date-picker
								v-model="state.queryParams.endTime"
								type="datetime"
								placeholder="结束时间"
								format="YYYY-MM-DD HH:mm:ss"
								value-format="YYYY-MM-DD HH:mm:ss"
								:shortcuts="shortcuts"
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="运行ID">
							<el-input v-model="state.queryParams.runId" placeholder="运行ID" clearable />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="事件ID">
							<el-input v-model="state.queryParams.eventId" placeholder="事件ID" clearable />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="事件状态">
							<g-sys-dict v-model="state.queryParams.status" code="EventStatusEnum" renderAs="select" placeholder="事件状态" clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysLogEvent/page'" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-dblclick="handleView">
				<template #toolbar_buttons>
					<el-button icon="ele-FolderOpened" type="primary" @click="exportLog" v-auth="'sysLogEvent/export'"> 导出 </el-button>
					<el-button icon="ele-DeleteFilled" type="danger" @click="handleClear" v-auth="'sysLogEvent/clear'"> 清空 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<g-sys-dict v-model="row.status" code="EventStatusEnum" />
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-InfoFilled" text type="primary" @click="handleView({ row })">详情</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.visible" draggable overflow destroy-on-close style="width: 1000px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Document /> </el-icon>
					<span> 日志详情 </span>
				</div>
			</template>
			<el-tabs v-model="state.activeTab">
				<el-tab-pane label="基本信息" name="basic">
					<el-descriptions :column="2" border labelWidth="110">
						<el-descriptions-item label="运行ID">{{ state.detail.runId }}</el-descriptions-item>
						<el-descriptions-item label="事件ID">{{ state.detail.eventId }}</el-descriptions-item>
						<el-descriptions-item label="事件创建时间">{{ state.detail.createTimeOfEvent }}</el-descriptions-item>
						<el-descriptions-item label="执行前时间">{{ state.detail.executingTime }}</el-descriptions-item>
						<el-descriptions-item label="执行后时间">{{ state.detail.executedTime }}</el-descriptions-item>
						<el-descriptions-item label="耗时(毫秒)">{{ state.detail.elapsed }}</el-descriptions-item>
						<el-descriptions-item label="触发方法">{{ state.detail.handlerMethod }}</el-descriptions-item>
						<el-descriptions-item label="订阅特性">{{ state.detail.attribute }}</el-descriptions-item>
						<el-descriptions-item label="是否只消费一次">{{ state.detail.consumeOnce ? '是' : '否' }}</el-descriptions-item>
						<el-descriptions-item label="状态">
							<g-sys-dict v-model="state.detail.status" code="EventStatusEnum" />
						</el-descriptions-item>
					</el-descriptions>
				</el-tab-pane>
				<el-tab-pane label="事件参数" name="payload" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.payload" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="异常信息" name="exception" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<pre>{{ state.detail.exception }}</pre>
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="重试记录" name="retryRecord" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<pre>{{ state.detail.retryRecord }}</pre>
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="共享上下文" name="properties" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.properties" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
			</el-tabs>
		</el-dialog>
	</div>
</template>

<!-- 事件日志 -->
<script lang="ts" setup name="sysLogEvent">
import { defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import { downloadByData, getFileName } from '/@/utils/download';
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import { StringToObj } from '/@/utils/json-utils';
import { formatDate } from '/@/utils/formatTime';
import { resolveCssColor } from '/@/utils/theme';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogEventApi } from '/@/api-services/system/api';
import { SysLogEvent, PageEventLogInput } from '/@/api-services/system/models';

const scEcharts = defineAsyncComponent(() => import('/@/components/scEcharts/index.vue'));

const shortcuts = useDateTimeShortCust();
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysEventLog', {
	pageSize: 50 as number,
	defaultSort: { field: 'id', order: 'desc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		startTime: undefined as any,
		endTime: undefined as any,
		runId: undefined,
		eventId: undefined,
		status: undefined,
	},
	localPageParam,
	visible: false,
	detail: {
		runId: '',
		eventId: '',
		payload: undefined,
		createTimeOfEvent: '',
		executingTime: '',
		executedTime: '',
		elapsed: undefined,
		exception: '',
		retryRecord: '',
		attribute: '',
		handlerMethod: '',
		properties: undefined,
		consumeOnce: false,
		status: 1,
	} as any,
	activeTab: 'basic',
	logMaxValue: 1,
});

// 获取主题颜色变量
const colors = ['--el-color-primary-light-9', '--el-color-primary-light-7', '--el-color-primary-light-5', '--el-color-primary-light-3', '--el-color-primary-light-1', '--el-color-primary'].map(
	(variable) => resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue(variable).trim())
);
// 读取 CSS 变量
const cssVar = (name: string) => resolveCssColor(getComputedStyle(document.documentElement).getPropertyValue(name).trim());
const echartsOption = ref({
	title: {
		top: 30,
		left: 'center',
		text: '事件日志统计',
		show: false,
	},
	tooltip: {
		formatter: function (p: any) {
			return p.data[1] + ' 数据量：' + p.data[0];
		},
	},
	visualMap: {
		show: true,
		inRange: {
			color: colors,
		},
		outOfRange: {
			color: ['transparent'],
		},
		min: 0.001,
		max: 1000,
		maxOpen: {
			type: 'piecewise',
		},
		type: 'piecewise',
		orient: 'horizontal',
		left: 'right',
	},
	calendar: {
		top: 30,
		left: 30,
		right: 30,
		bottom: 30,
		cellSize: ['auto', 20],
		range: ['', ''],
		splitLine: true,
		dayLabel: {
			firstDay: 1,
			nameMap: 'ZH',
		},
		itemStyle: {
			
			color: cssVar('--el-fill-color-light'),
			borderWidth: 2,
			borderColor: cssVar('--el-bg-color'),
		},
		monthLabel: {
			nameMap: ['一月', '二月', '三月', '四月', '五月', '六月', '七月', '八月', '九月', '十月', '十一月', '十二月'],
		},
		yearLabel: {
			show: false,
		},
	},
	series: {
		type: 'heatmap',
		coordinateSystem: 'calendar',
		data: [],
	},
});

// 表格参数配置
const options = useVxeTable<SysLogEvent>(
	{
		id: 'sysEventLog',
		name: '事件日志',
		columns: [
			// { type: 'checkbox', width: 40 },
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'createTime', title: '创建时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'runId', title: '运行ID', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'eventId', title: '事件ID', minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'createTimeOfEvent', title: '事件创建时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'executingTime', title: '执行前时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'executedTime', title: '执行后时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'elapsed', title: '耗时(毫秒)', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'status', title: '状态', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'handlerMethod', title: '触发方法', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'attribute', title: '订阅特性', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'consumeOnce', title: '只消费一次', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: localPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: localPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {

	await getYearDayStatsData();
});

// 获取统计日志数据
const getYearDayStatsData = async () => {
	let data = [] as any;
	var res = await getAPI(SysLogEventApi).apiSysLogEventYearDayStatsGet();
	res.data.result?.forEach((item: any) => {
		data.push([item.date, item.count]);

		if (item.count > state.logMaxValue) state.logMaxValue = item.count; // 计算最大值
	});
	echartsOption.value.visualMap.max = state.logMaxValue;
	echartsOption.value.series.data = data;
	echartsOption.value.calendar.range = [data[0][0], data[data.length - 1][0]];
};

// 点击统计日志返回的数据
const clickData = (e: any) => {
	if (e[1] < 1) return ElMessage.warning('没有日志数据');
	state.queryParams.startTime = e[0];
	var today = new Date(state.queryParams.startTime);
	let endTime = today.setDate(today.getDate() + 1);
	state.queryParams.endTime = formatDate(new Date(endTime), 'YYYY-mm-dd');
	xGrid.value?.commitProxy('query');
};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageEventLogInput;
	return getAPI(SysLogEventApi).apiSysLogEventPagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.startTime = undefined;
	state.queryParams.endTime = undefined;
	state.queryParams.runId = undefined;
	state.queryParams.eventId = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 表格事件
const gridEvents: VxeGridListeners<SysLogEvent> = {
	...gridPageEvents(localPageParamKey, localPageParam),
};

// 清空日志
const handleClear = async () => {
	ElMessageBox.confirm(`确定要清空日志?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			options.loading = true;
			await getAPI(SysLogEventApi).apiSysLogEventClearPost();
			options.loading = false;
			ElMessage.success('清空成功');
			await handleQuery();
		})
		.catch(() => {});
};

// 查看详情
const handleView = async ({ row }: any) => {
	const { data } = await getAPI(SysLogEventApi).apiSysLogEventDetailIdGet(row.id);
	state.activeTab = 'basic';
	state.detail.runId = data?.result?.runId;
	state.detail.eventId = data?.result?.eventId;
	// 如果参数是JSON字符串，则尝试转为JSON对象
	state.detail.payload = StringToObj(data?.result?.payload);
	state.detail.createTimeOfEvent = data?.result?.createTimeOfEvent;
	state.detail.executingTime = data?.result?.executingTime;
	state.detail.executedTime = data?.result?.executedTime;
	state.detail.elapsed = data?.result?.elapsed;
	state.detail.exception = data?.result?.exception;
	state.detail.retryRecord = data?.result?.retryRecord;
	state.detail.attribute = data?.result?.attribute;
	state.detail.handlerMethod = data?.result?.handlerMethod;
	state.detail.properties = StringToObj(data?.result?.properties);
	state.detail.consumeOnce = data?.result?.consumeOnce;
	state.detail.status = data?.result?.status;
	state.visible = true;
};

// 导出日志
const exportLog = async () => {
	options.loading = true;
	var res = await getAPI(SysLogEventApi).apiSysLogEventExportPost(state.queryParams, { responseType: 'blob' });
	options.loading = false;

	var fileName = getFileName(res.headers);
	downloadByData(res.data as any, fileName);
};
</script>
