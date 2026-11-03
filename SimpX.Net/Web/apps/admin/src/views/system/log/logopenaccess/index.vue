<template>
	<div class="syslogopenaccess-container">
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
						<el-form-item label="身份标识">
							<el-input v-model="state.queryParams.accessKey" placeholder="身份标识" clearable />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="账号名称">
							<el-input v-model="state.queryParams.account" placeholder="账号名称" clearable />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="IP地址">
							<el-input v-model="state.queryParams.remoteIp" placeholder="IP地址" clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysLogOpenAccess/page'" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-dblclick="handleView">
				<template #toolbar_buttons>
					<el-button icon="ele-DeleteFilled" type="danger" @click="handleClear" v-auth="'sysLogOpenAccess/clear'"> 清空 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_logLevel="{ row }">
					<el-tag v-if="row.logLevel === 1">调试</el-tag>
					<el-tag v-else-if="row.logLevel === 2">消息</el-tag>
					<el-tag v-else-if="row.logLevel === 3" type="warning">警告</el-tag>
					<el-tag v-else-if="row.logLevel === 4" type="danger">错误</el-tag>
					<el-tag v-else>其他</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === '失败'" type="danger">失败</el-tag>
					<el-tag v-else type="success">成功</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-InfoFilled" text type="primary" @click="handleView({ row })">详情</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.visible" draggable overflow destroy-on-close>
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Document /> </el-icon>
					<span> 日志详情 </span>
				</div>
			</template>
			<el-tabs v-model="state.activeTab">
				<el-tab-pane label="日志消息" name="message">
					<el-scrollbar height="calc(100vh - 250px)">
						<pre>{{ state.detail.message }}</pre>
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="请求参数" name="request" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.requestParam" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="返回内容" name="return" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.returnResult" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane label="签名诊断" name="signInfo" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty v-if="state.detail.signInfo" :data="state.detail.signInfo" showLength showIcon showLineNumber showSelectController />
						<el-empty v-else :image-size="100" description="无签名诊断信息" />
					</el-scrollbar>
				</el-tab-pane>
			</el-tabs>
		</el-dialog>
	</div>
</template>

<!-- 开放接口日志 -->
<script lang="ts" setup name="sysLogOpenAccess">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable, useGridPageParam, gridPageEvents, useGridQuery } from '/@/hooks/useVxeTableOptionsHook';
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import { StringToObj } from '/@/utils/json-utils';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogOpenAccessApi } from '/@/api-services/system/api';
import { SysLogOpenAccess, PageOpenAccessLogInput } from '/@/api-services/system/models';

const shortcuts = useDateTimeShortCust();
const xGrid = ref<VxeGridInstance>();
const { key: localPageParamKey, param: localPageParam } = useGridPageParam('sysLogOpenAccess', {
	pageSize: 50 as number,
	defaultSort: { field: 'id', order: 'desc', descStr: 'desc' },
});
const state = reactive({
	queryParams: {
		startTime: undefined as any,
		endTime: undefined as any,
		accessKey: undefined,
		account: undefined,
		remoteIp: undefined,
	},
	localPageParam,
	visible: false,
	detail: {
		requestParam: undefined,
		returnResult: undefined,
		message: '' as string | null | undefined,
		signInfo: undefined,
	},
	activeTab: 'message',
});

// 表格参数配置
const options = useVxeTable<SysLogOpenAccess>(
	{
		id: 'sysLogOpenAccess',
		name: '开放接口日志',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'logDateTime', title: '日志时间', minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'accessKey', title: '身份标识', minWidth: 120, showOverflow: 'tooltip', align: 'left', headerAlign: 'center' },
			{ field: 'httpMethod', title: '请求方式', minWidth: 90, showOverflow: 'tooltip' },
			{ field: 'requestUrl', title: '请求地址', minWidth: 200, showOverflow: 'tooltip', align: 'left', headerAlign: 'center' },
			{ field: 'status', title: '状态', minWidth: 70, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'logLevel', title: '级别', minWidth: 70, showOverflow: 'tooltip', slots: { default: 'row_logLevel' } },
			{ field: 'elapsed', title: '耗时(ms)', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'account', title: '账号名称', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'realName', title: '真实姓名', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'remoteIp', title: 'IP地址', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'location', title: '登录地点', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'traceId', title: '请求跟踪Id', minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: localPageParam.defaultSort },
		pagerConfig: { pageSize: localPageParam.pageSize },
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {
});

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageOpenAccessLogInput;
	return getAPI(SysLogOpenAccessApi).apiSysLogOpenAccessPagePost(params);
};

// 查询操作
const handleQuery = useGridQuery(xGrid, options);

// 重置操作
const resetQuery = async () => {
	state.queryParams.startTime = undefined;
	state.queryParams.endTime = undefined;
	state.queryParams.accessKey = undefined;
	state.queryParams.account = undefined;
	state.queryParams.remoteIp = undefined;
	await xGrid.value?.commitProxy('reload');
};

// 表格事件
const gridEvents: VxeGridListeners<SysLogOpenAccess> = {
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
			await getAPI(SysLogOpenAccessApi).apiSysLogOpenAccessClearPost();
			options.loading = false;
			ElMessage.success('清空成功');
			await handleQuery();
		})
		.catch(() => {});
};

// 查看详情
const handleView = async ({ row }: any) => {
	const { data } = await getAPI(SysLogOpenAccessApi).apiSysLogOpenAccessDetailIdGet(row.id);
	state.activeTab = 'message';
	state.detail.message = data?.result?.message;
	state.detail.requestParam = StringToObj(data?.result?.requestParam);
	state.detail.returnResult = StringToObj(data?.result?.returnResult);
	state.detail.signInfo = StringToObj(data?.result?.signInfo);
	state.visible = true;
};
</script>
