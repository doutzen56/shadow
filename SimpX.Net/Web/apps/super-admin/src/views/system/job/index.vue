<template>
	<div class="jb-page">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">任务调度</h2>
					<p class="page-hero__desc">管理后台作业与触发器：可手动执行、启停单个作业或批量调度，也能唤醒调度器与强制持久化。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="作业总数取自分页结果，运行中触发器按当前页统计">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.statTotal }}</span>
							<span class="page-hero__stat-k">作业总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.groupsData.length }}</span>
							<span class="page-hero__stat-k">作业分组</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ state.statRunning }}</span>
							<span class="page-hero__stat-k">运行中触发器</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：全站统一的 .query-toolbar -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" class="query-form">
				<el-form-item label="作业编号" prop="jobId">
					<el-input v-model="state.queryParams.jobId" class="query-input" placeholder="作业编号" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
				<el-form-item label="组名称">
					<el-select v-model="state.queryParams.groupName" class="query-select" placeholder="组名称" clearable>
						<el-option v-for="item in state.groupsData" :key="item" :label="item" :value="item" />
					</el-select>
				</el-form-item>
				<el-form-item label="描述信息" prop="description">
					<el-input v-model="state.queryParams.description" class="query-input" placeholder="描述信息" clearable @keyup.enter.native="handleQuery(true)" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" v-auth="'sysJob/pageJobDetail'" :loading="optionsJob.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="optionsJob.loading"> 重置 </el-button>
				</el-button-group>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGridJob" class="xGrid-style" v-bind="optionsJob" v-on="gridEventsJob">
				<template #toolbar_buttons>
					<div class="jb-bar">
						<el-button type="primary" icon="ele-CirclePlus" @click="handleAdd" v-auth="'sysJob/addJobDetail'"> 新增作业 </el-button>
						<el-button-group>
							<el-tooltip content="启动所有作业" placement="bottom">
								<el-button icon="ele-VideoPlay" @click="startAllJob" />
							</el-tooltip>
							<el-tooltip content="暂停所有作业" placement="bottom">
								<el-button icon="ele-VideoPause" @click="pauseAllJob" />
							</el-tooltip>
						</el-button-group>
						<el-button-group>
							<el-tooltip content="强制唤醒作业调度器" placement="bottom">
								<el-button icon="ele-AlarmClock" @click="cancelSleep" />
							</el-tooltip>
							<el-tooltip content="强制触发所有作业持久化" placement="bottom">
								<el-button icon="ele-Connection" @click="persistAll" />
							</el-tooltip>
						</el-button-group>
						<el-button icon="ele-Coin" @click="openJobCluster" plain> 集群控制 </el-button>
						<el-button type="primary" icon="ele-Grid" @click="openJobDashboard" plain> 任务看板 </el-button>
						<el-button-group>
							<el-tooltip content="展开全部触发器" placement="bottom">
								<el-button icon="ele-Expand" @click="handleExpand" />
							</el-tooltip>
							<el-tooltip content="折叠全部触发器" placement="bottom">
								<el-button icon="ele-Fold" @click="handleFold" />
							</el-tooltip>
						</el-button-group>
					</div>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_content="{ row }">
					<!-- 表格子卡片：全站统一的 .entity-card（与 AI 智能体 · 工具管理同一套）。
					     这里刻意不套 .card-grid —— 它在卡片墙里是 flex:1 + 内部滚动，
					     放进 vxe 展开行会把行高压成固定值 -->
					<div class="jb-triggers">
						<el-empty v-if="!row.jobTriggers?.length" description="该作业暂无触发器" :image-size="60" />
						<el-row v-else :gutter="12" class="card-grid-row jb-trigger-row">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="8" v-for="(item, idx) in row.jobTriggers" :key="idx">
								<el-card shadow="never" class="entity-card entity-card--accent" :class="'is-primary'">
									<i class="entity-card__watermark ri-alarm-line"></i>

									<div class="entity-card__head">
										<div class="entity-card__icon">
											<el-icon><ele-AlarmClock /></el-icon>
										</div>
										<div class="entity-card__title-wrap">
											<div class="entity-card__title" :title="item.description || '触发器'">{{ item.description || '未命名触发器' }}</div>
											<div class="entity-card__sub">{{ shortTriggerType(item.triggerType) }}</div>
										</div>
										<el-tag size="small" effect="light" :type="triggerStatus(item.status).type">{{ triggerStatus(item.status).label }}</el-tag>
									</div>

									<div class="entity-card__body">
										<div class="code-preview cell-mono" :title="item.triggerId">{{ item.triggerId }}</div>

										<!-- 计数区：触发 / 出错 / 重试 / 间隔 -->
										<div class="metric-group">
											<div class="metric-item">
												<span>触发次数</span>
												<b>{{ item.numberOfRuns ?? 0 }} / {{ item.maxNumberOfRuns ?? 0 }}</b>
											</div>
											<div class="metric-item">
												<span>出错次数</span>
												<b :class="{ 'is-bad': Number(item.numberOfErrors) > 0 }">{{ item.numberOfErrors ?? 0 }} / {{ item.maxnumberOfErrors ?? 0 }}</b>
											</div>
											<div class="metric-item">
												<span>重试次数</span>
												<b>{{ item.numRetries ?? 0 }}</b>
											</div>
											<div class="metric-item">
												<span>重试间隔</span>
												<b>{{ item.retryTimeout ?? 0 }} ms</b>
											</div>
										</div>

										<!-- 时间区：上次 / 下次 / 起止 -->
										<div class="entity-card__meta">
											<span>上次运行 <b>{{ triggerTime(item.lastRunTime) }}</b></span>
											<span>下次运行 <b>{{ triggerTime(item.nextRunTime) }}</b></span>
											<span>起始 <b>{{ triggerTime(item.startTime) }}</b></span>
											<span>结束 <b>{{ triggerTime(item.endTime) }}</b></span>
										</div>

										<div class="tag-group">
											<el-tag size="small" :type="item.startNow ? 'success' : 'info'" :effect="item.startNow ? 'light' : 'plain'">立即启动</el-tag>
											<el-tag size="small" :type="item.runOnStart ? 'success' : 'info'" :effect="item.runOnStart ? 'light' : 'plain'">启动时执行</el-tag>
											<el-tag size="small" :type="item.resetOnlyOnce ? 'success' : 'info'" :effect="item.resetOnlyOnce ? 'light' : 'plain'">重置次数</el-tag>
										</div>
									</div>

									<div class="entity-card__foot">
										<span class="cell-sub">{{ shortTriggerType(item.triggerType) }}</span>
										<div class="entity-card__actions">
											<el-button size="small" text type="primary" icon="ele-VideoPlay" @click="startTrigger(item)">启动</el-button>
											<el-button size="small" text type="warning" icon="ele-VideoPause" @click="pauseTrigger(item)">暂停</el-button>
											<el-button size="small" text type="primary" icon="ele-Edit" @click="openEditJobTrigger(item)">编辑</el-button>
											<el-button size="small" text type="danger" icon="ele-Delete" @click="delJobTrigger(item)">删除</el-button>
										</div>
									</div>
								</el-card>
							</el-col>
						</el-row>
					</div>
				</template>
				<template #row_jobId="{ row }">
					<div style="display: flex; align-items: center">
						<el-icon><timer /></el-icon>
						<span style="margin-left: 5px">{{ (row as JobDetailOutput).jobDetail?.jobId }}</span>
					</div>
				</template>
				<template #row_concurrent="{ row }">
					<el-tag type="success" v-if="(row as JobDetailOutput).jobDetail?.concurrent == true"> 并行 </el-tag>
					<el-tag type="warning" v-else> 串行 </el-tag>
				</template>
				<template #row_createType="{ row }">
					<el-tag type="info" v-if="(row as JobDetailOutput).jobDetail?.createType == JobCreateTypeEnum.NUMBER_0"> 内置 </el-tag>
					<el-tag type="warning" v-if="(row as JobDetailOutput).jobDetail?.createType == JobCreateTypeEnum.NUMBER_1"> 脚本 </el-tag>
					<el-tag type="success" v-if="(row as JobDetailOutput).jobDetail?.createType == JobCreateTypeEnum.NUMBER_2"> HTTP请求 </el-tag>
				</template>
				<template #row_includeAnnotation="{ row }">
					<el-tag v-if="(row as JobDetailOutput).jobDetail?.includeAnnotation == true"> 是 </el-tag>
					<el-tag v-else> 否 </el-tag>
				</template>
				<template #row_properties="{ row }">
					<span v-if="(row as JobDetailOutput).jobDetail?.createType != JobCreateTypeEnum.NUMBER_2"> {{ (row as JobDetailOutput).jobDetail?.properties }} </span>
					<div v-else style="text-align: center">
						<el-popover placement="left" :width="400" trigger="hover">
							<template #reference>
								<el-tag effect="plain" type="info"> 请求参数 </el-tag>
							</template>
							<el-descriptions title="Http 请求参数" :column="1" size="small" :border="true">
								<el-descriptions-item label="请求地址" label-align="right" label-class-name="job-index-descriptions-label-style">
									{{ getHttpJobMessage((row as JobDetailOutput).jobDetail?.properties).requestUri }}
								</el-descriptions-item>
								<el-descriptions-item label="请求方法" label-align="right" label-class-name="job-index-descriptions-label-style">
									{{ getHttpMethodDesc(getHttpJobMessage((row as JobDetailOutput).jobDetail?.properties).httpMethod) }}
								</el-descriptions-item>
								<el-descriptions-item label="请求报文体" label-align="right" label-class-name="job-index-descriptions-label-style">
									{{ getHttpJobMessage((row as JobDetailOutput).jobDetail?.properties).body }}
								</el-descriptions-item>
								<el-descriptions-item label="超时时间" label-align="right" label-class-name="job-index-descriptions-label-style">
									{{ getHttpJobMessage((row as JobDetailOutput).jobDetail?.properties).timeout }}
								</el-descriptions-item>
							</el-descriptions>
						</el-popover>
					</div>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip content="执行记录">
						<el-button size="small" type="primary" icon="ele-Timer" text @click="openJobTriggerRecord(row)"> </el-button>
					</el-tooltip>
					<el-tooltip content="增加触发器">
						<el-button size="small" type="primary" icon="ele-CirclePlus" text @click="openAddJobTrigger(row)"> </el-button>
					</el-tooltip>
					<el-tooltip content="执行作业">
						<el-button size="small" type="primary" icon="ele-CircleCheck" text @click="runJob(row)" />
					</el-tooltip>
					<el-tooltip content="启动作业">
						<el-button size="small" type="primary" icon="ele-VideoPlay" text @click="startJob(row)" />
					</el-tooltip>
					<el-tooltip content="暂停作业">
						<el-button size="small" type="primary" icon="ele-VideoPause" text @click="pauseJob(row)" />
					</el-tooltip>
					<el-tooltip content="取消作业">
						<el-button size="small" type="primary" icon="ele-CircleClose" text @click="cancelJob(row)" />
					</el-tooltip>
					<el-tooltip content="编辑作业">
						<el-button size="small" type="primary" icon="ele-Edit" text @click="handleEdit(row)" v-auth="'sysJob/updateJobDetail'"> </el-button>
					</el-tooltip>
					<el-tooltip content="删除作业">
						<el-button size="small" type="danger" icon="ele-Delete" text @click="handleDelete(row)" v-auth="'sysJob/deleteJobDetail'"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<el-drawer v-model="state.isVisibleDrawer" :size="recordDrawerSize" class="jb-record-drawer">
			<template #header>
				<div class="dialog-title jb-record-title">
					<el-icon size="16"><ele-Clock /></el-icon>
					<span>作业触发器运行记录</span>
					<span v-if="state.currentJob?.jobDetail?.jobId" class="jb-record-key">{{ state.currentJob.jobDetail.jobId }}</span>
				</div>
				<div class="jb-record-acts">
					<el-tooltip :content="state.recordFullscreen ? '退出全屏' : '全屏'" placement="bottom">
						<el-button text @click="state.recordFullscreen = !state.recordFullscreen">
							<SvgIcon :name="state.recordFullscreen ? 'iconfont icon-tuichuquanping' : 'iconfont icon-fullscreen'" />
						</el-button>
					</el-tooltip>
				</div>
			</template>
			<el-card class="full-table" shadow="hover">
				<vxe-grid ref="xGridRecord" class="xGrid-style" v-bind="optionsRecord" v-on="gridEventsRecord">
					<template #toolbar_buttons>
						<el-button icon="ele-DeleteFilled" type="danger" @click="handleClearJobTriggerRecord"> 清空 </el-button>
					</template>
					<template #toolbar_tools> </template>
					<template #empty>
						<el-empty :image-size="200" />
					</template>
					<template #row_status="{ row }">
						<el-tag :type="triggerStatus((row as SysJobTrigger).status).type" effect="plain"> {{ triggerStatus((row as SysJobTrigger).status).label }} </el-tag>
					</template>
				</vxe-grid>
			</el-card>
		</el-drawer>

		<EditJobDetail ref="editJobDetailRef" :title="state.editJobDetailTitle" @handleQuery="handleQuery" />
		<EditJobTrigger ref="editJobTriggerRef" :title="state.editJobTriggerTitle" @handleQuery="handleQuery" />
		<JobCluster ref="editJobClusterRef" />
	</div>
</template>

<!-- 定时任务 -->
<script lang="ts" setup name="sysJob">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useRouter } from 'vue-router';
import { Timer } from '@element-plus/icons-vue';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { auth } from '/@/utils/authFunction';

import EditJobDetail from '/@/views/system/job/component/editJobDetail.vue';
import EditJobTrigger from '/@/views/system/job/component/editJobTrigger.vue';
import JobCluster from '/@/views/system/job/component/jobCluster.vue';

import { getAPI } from '/@/utils/axios-utils';
import { formatDate } from '/@/utils/formatTime';

import { SysJobApi } from '/@/api-services/system/api';
import { JobCreateTypeEnum, JobDetailOutput, SysJobTrigger, SysJobTriggerRecord, PageJobDetailInput, PageJobTriggerRecordInput } from '/@/api-services/system/models';

const xGridJob = ref<VxeGridInstance>();
const xGridTrigger = ref<VxeGridInstance>();
const xGridRecord = ref<VxeGridInstance>();
const router = useRouter();
const editJobDetailRef = ref<InstanceType<typeof EditJobDetail>>();
const editJobTriggerRef = ref<InstanceType<typeof EditJobTrigger>>();
const editJobClusterRef = ref<InstanceType<typeof JobCluster>>();
const state = reactive({
	queryParams: {
		jobId: undefined,
		groupName: undefined,
		description: undefined,
	},
	jobPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'id', order: 'asc', descStr: 'desc' } as any,
	},
	recordPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'id', order: 'desc', descStr: 'desc' } as any,
	},
	editJobDetailTitle: '',
	editJobTriggerTitle: '',
	/** 顶部统计：作业总数取自分页结果，运行中触发器按当前页统计 */
	statTotal: 0,
	statRunning: 0,
	loading2: false,
	isVisibleDrawer: false,
	recordFullscreen: false,
	triggerRecordData: [] as any,
	currentJob: {} as any,
	visible: false,
	title: '',
	groupsData: [] as Array<string>,
});

/** 触发器状态字典：13 个状态收敛成一张表，避免模板里堆 13 个 v-if */
type TagType = 'primary' | 'success' | 'info' | 'warning' | 'danger';
const TRIGGER_STATUS: Record<number, { label: string; type: TagType }> = {
	0: { label: '积压', type: 'warning' },
	1: { label: '就绪', type: 'info' },
	2: { label: '正在运行', type: 'success' },
	3: { label: '暂停', type: 'danger' },
	4: { label: '阻塞', type: 'danger' },
	5: { label: '由失败进入就绪', type: 'info' },
	6: { label: '归档', type: 'danger' },
	7: { label: '崩溃', type: 'danger' },
	8: { label: '超限', type: 'danger' },
	9: { label: '无触发时间', type: 'danger' },
	10: { label: '未启动', type: 'danger' },
	11: { label: '未知作业触发器', type: 'danger' },
	12: { label: '未知作业处理程序', type: 'danger' },
};

const triggerStatus = (status?: number | null) => TRIGGER_STATUS[Number(status)] ?? { label: '未知', type: 'info' as TagType };

// 触发器类型形如 Furion.Schedule.PeriodTrigger，卡片上只留最后一段
const shortTriggerType = (triggerType?: string | null) =>
	String(triggerType ?? '')
		.split('.')
		.pop() || '-';

const triggerTime = (value?: string | null) => (value ? formatDate(new Date(value), 'YYYY-mm-dd HH:MM:SS') : '无');

// 运行记录抽屉：窄屏或手动全屏时铺满
const viewportWidth = ref(window.innerWidth);
const onViewportResize = () => (viewportWidth.value = window.innerWidth);
const recordDrawerSize = computed(() => (viewportWidth.value < 900 || state.recordFullscreen ? '100%' : '60%'));

// 表格参数配置
const optionsJob = useVxeTable<JobDetailOutput>(
	{
		id: 'sysJob',
		name: '作业信息',
		columns: [
			// { type: 'checkbox', width: 40 },
			{ field: 'seq', type: 'seq', title: '序号', width: 50, fixed: 'left' },
			{ field: 'expand', type: 'expand', width: 40, slots: { content: 'row_content' } },
			{ field: 'jobDetail.jobId', title: '作业编号', minWidth: 250, showOverflow: 'tooltip', slots: { default: 'row_jobId' } },
			{ field: 'jobDetail.groupName', title: '组名称', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'jobDetail.jobType', title: '类型', minWidth: 250, showOverflow: 'tooltip', align: 'left', headerAlign: 'center' },
			// { field: 'jobDetail.assemblyName', title: '程序集', minWidth: 100, showOverflow: 'tooltip', sortable: true },
			{ field: 'jobDetail.description', title: '描述', minWidth: 250, showOverflow: 'tooltip', align: 'left', headerAlign: 'center' },
			{ field: 'jobDetail.concurrent', title: '执行方式', minWidth: 80, showOverflow: 'tooltip', slots: { default: 'row_concurrent' } },
			{ field: 'jobDetail.createType', title: '作业创建类型', minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_createType' } },
			{ field: 'jobDetail.includeAnnotation', title: '扫描特性触发器', minWidth: 120, showOverflow: 'tooltip', slots: { default: 'row_includeAnnotation' } },
			{ field: 'jobDetail.updatedTime', title: '更新时间', minWidth: 130, showOverflow: 'tooltip' },
			{ field: 'jobDetail.properties', title: '额外数据', showOverflow: 'tooltip', slots: { default: 'row_properties' } },
			{ field: 'buttons', title: '操作', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' }, fixed: 'right' },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: state.jobPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: state.jobPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {
	window.addEventListener('resize', onViewportResize);
	const { data } = await getAPI(SysJobApi).apiSysJobListJobGroupPost();
	state.groupsData = data.result ?? [];
});

onUnmounted(() => {
	window.removeEventListener('resize', onViewportResize);
});

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageJobDetailInput;
	const res = await getAPI(SysJobApi).apiSysJobPageJobDetailPost(params);
	// 顺带统计：总数用于头图，运行中触发器（status=2）按当前页汇总
	const items = res.data.result?.items ?? [];
	state.statTotal = res.data.result?.total ?? 0;
	state.statRunning = items.reduce((sum, job) => sum + (job.jobTriggers ?? []).filter((t) => Number(t.status) === 2).length, 0);
	return res;
};

// 查询操作
const handleQuery = async (reset = false) => {
	reset ? await xGridJob.value?.commitProxy('reload') : await xGridJob.value?.commitProxy('query');
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.jobId = undefined;
	state.queryParams.groupName = undefined;
	state.queryParams.description = undefined;
	await xGridJob.value?.commitProxy('reload');
};

// 打开新增作业页面
const handleAdd = () => {
	state.editJobDetailTitle = '添加作业';
	editJobDetailRef.value?.openDialog({ concurrent: true, includeAnnotations: true, groupName: 'default', createType: JobCreateTypeEnum.NUMBER_2 });
};

// 打开编辑作业页面
const handleEdit = (row: JobDetailOutput) => {
	state.editJobDetailTitle = '编辑作业';
	editJobDetailRef.value?.openDialog(row.jobDetail);
};

// 删除作业
const handleDelete = (row: JobDetailOutput) => {
	ElMessageBox.confirm(`确定删除作业：${row.jobDetail?.jobId}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysJobApi).apiSysJobDeleteJobDetailPost({ jobId: row.jobDetail?.jobId });
			await handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 作业表格事件
const gridEventsJob: VxeGridListeners<JobDetailOutput> = {
	// 只对 pager-config 配置时有效，分页发生改变时会触发该事件
	async pageChange({ pageSize }) {
		state.jobPageParam.pageSize = pageSize;
	},
	// 当排序条件发生变化时会触发该事件
	async sortChange({ field, order }) {
		state.jobPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
	// 双击行事件
	async cellDblclick({ row }) {
		if (auth('sysJob/updateJobDetail')) await handleEdit(row);
	},
};

// 打开新增触发器页面
const openAddJobTrigger = (row: JobDetailOutput) => {
	state.editJobTriggerTitle = '添加触发器';
	editJobTriggerRef.value?.openDialog({
		jobId: row.jobDetail?.jobId,
		retryTimeout: 1000,
		startNow: true,
		runOnStart: true,
		resetOnlyOnce: true,
		triggerType: 'Furion.Schedule.PeriodTrigger',
	});
};

// 打开编辑触发器页面
const openEditJobTrigger = (row: SysJobTrigger) => {
	state.editJobTriggerTitle = '编辑触发器';
	editJobTriggerRef.value?.openDialog(row);
};

// 删除触发器
const delJobTrigger = (row: SysJobTrigger) => {
	ElMessageBox.confirm(`确定删除触发器：${row.triggerId}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysJobApi).apiSysJobDeleteJobTriggerPost({ jobId: row.jobId, triggerId: row.triggerId });
			await handleQuery();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 启动所有作业
const startAllJob = async () => {
	await getAPI(SysJobApi).apiSysJobStartAllJobPost();
	ElMessage.success('启动所有作业');
};

// 暂停所有作业
const pauseAllJob = async () => {
	await getAPI(SysJobApi).apiSysJobPauseAllJobPost();
	ElMessage.success('暂停所有作业');
};

// 执行某个作业
const runJob = async (row: JobDetailOutput) => {
	await getAPI(SysJobApi).apiSysJobRunJobPost({ jobId: row.jobDetail?.jobId });
	ElMessage.success('执行作业');
};

// 启动某个作业
const startJob = async (row: JobDetailOutput) => {
	await getAPI(SysJobApi).apiSysJobStartJobPost({ jobId: row.jobDetail?.jobId });
	ElMessage.success('启动作业');
};

// 暂停某个作业
const pauseJob = async (row: JobDetailOutput) => {
	await getAPI(SysJobApi).apiSysJobPauseJobPost({ jobId: row.jobDetail?.jobId });
	ElMessage.success('暂停作业');
};

// 取消某个作业
const cancelJob = async (row: JobDetailOutput) => {
	await getAPI(SysJobApi).apiSysJobCancelJobPost({ jobId: row.jobDetail?.jobId });
	ElMessage.success('取消作业');
};

// 启动触发器
const startTrigger = async (row: SysJobTrigger) => {
	await getAPI(SysJobApi).apiSysJobStartTriggerPost({ jobId: row.jobId, triggerId: row.triggerId });
	ElMessage.success('启动触发器');
};

// 暂停触发器
const pauseTrigger = async (row: SysJobTrigger) => {
	await getAPI(SysJobApi).apiSysJobPauseTriggerPost({ jobId: row.jobId, triggerId: row.triggerId });
	ElMessage.success('暂停触发器');
};

// 强制唤醒作业调度器
const cancelSleep = async () => {
	await getAPI(SysJobApi).apiSysJobCancelSleepPost();
	ElMessage.success('强制唤醒作业调度器');
};

// 强制触发所有作业持久化
const persistAll = async () => {
	await getAPI(SysJobApi).apiSysJobPersistAllPost();
	ElMessage.success('强制触发所有作业持久化');
};

// 打开集群控制页面
const openJobCluster = () => {
	editJobClusterRef.value?.openDrawer();
};

// 打开任务看板
const openJobDashboard = () => {
	router.push({
		path: '/platform/job/dashboard',
	});
};

// 根据任务属性获取 HttpJobMessage
const getHttpJobMessage = (properties: string | undefined | null): HttpJobMessage => {
	return editJobDetailRef.value?.getHttpJobMessage(properties)!;
};

// 获取请求方法的对应描述
const getHttpMethodDesc = (httpMethodStr: string | undefined | null): string => {
	if (httpMethodStr === undefined || httpMethodStr === null || httpMethodStr === '') return '';
	for (const key in editJobDetailRef.value?.httpMethodDef) {
		if (editJobDetailRef.value?.httpMethodDef[key] === httpMethodStr) return key;
	}
	return '';
};

//全部展开
const handleExpand = () => {
	xGridJob.value?.setAllRowExpand(true);
};

//全部折叠
const handleFold = () => {
	xGridJob.value?.clearRowExpand();
};

// 打开作业触发器运行记录
const openJobTriggerRecord = async (row: any) => {
	state.currentJob = row;
	state.recordPageParam.jobId = row?.jobDetail?.jobId;
	state.isVisibleDrawer = true;
	await handleQueryRecord();
};

// 表格参数配置-触发器
const optionsRecord = useVxeTable<SysJobTriggerRecord>(
	{
		id: 'sysJobRecord',
		name: '执行记录',
		columns: [
			// { type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'seq', type: 'seq', title: '序号', width: 50, fixed: 'left' },
			{ field: 'jobId', title: '作业编号', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'triggerId', title: '触发器编号', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'numberOfRuns', title: '当前运行次数', minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'lastRunTime', title: '最近运行时间', minWidth: 130, showOverflow: 'tooltip' },
			{ field: 'nextRunTime', title: '下一次运行时间', minWidth: 130, showOverflow: 'tooltip' },
			{ field: 'status', title: '触发器状态', minWidth: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'result', title: '执行结果', minWidth: 200, showOverflow: 'title' },
			{ field: 'elapsedTime', title: '耗时(ms)', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'createdTime', title: '创建时间', minWidth: 130, showOverflow: 'tooltip' },
		],
	}, // vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryRecordApi(page, sort) } },
		// 排序配置
		sortConfig: { defaultSort: state.jobPageParam.defaultSort },
		// 分页配置
		pagerConfig: { pageSize: state.jobPageParam.pageSize },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 查询api-作业触发器运行记录
const handleQueryRecordApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.recordPageParam, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageJobTriggerRecordInput;
	return getAPI(SysJobApi).apiSysJobPageJobTriggerRecordPost(params);
};

// 查询操作-作业触发器运行记录
const handleQueryRecord = async () => {
	await xGridRecord.value?.commitProxy('query');
};

// 作业表格事件
const gridEventsRecord: VxeGridListeners<SysJobTriggerRecord> = {
	// 只对 pager-config 配置时有效，分页发生改变时会触发该事件
	async pageChange({ pageSize }) {
		state.recordPageParam.pageSize = pageSize;
	},
	// 当排序条件发生变化时会触发该事件
	async sortChange({ field, order }) {
		state.recordPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

// 清空作业触发器运行记录
const handleClearJobTriggerRecord = async () => {
	ElMessageBox.confirm(`确定要清空日志?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			optionsRecord.loading = true;
			await getAPI(SysJobApi).apiSysJobClearJobTriggerRecordPost();
			optionsRecord.loading = false;
			ElMessage.success('清空成功');
			await handleQueryRecord();
		})
		.catch(() => {});
};
</script>

<style>
/* 此样式不能为 scoped */
.job-index-descriptions-label-style {
	width: 80px;
}

/* 运行记录抽屉：头部用共享类 .dialog-title，
   抽屉头/体样式必须写在这里 —— 抽屉被传送到 body 之后 scoped 的 :deep() 会断链 */
.jb-record-drawer {
	.el-drawer__header {
		padding: 8px 12px !important;
	}

	.el-drawer__body {
		padding: 5px;
		display: flex;
		flex-direction: column;
		height: 100%;
		min-height: 0;
	}
}

.jb-record-title {
	flex: 1 1 auto;
	min-width: 0;
	gap: 8px;
	font-size: 15px;
	font-weight: 700;
}

.jb-record-key {
	flex-shrink: 0;
	padding: 0 8px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 11px;
	font-weight: 600;
	font-family: Consolas, Monaco, monospace;
	background: color-mix(in srgb, currentColor 24%, transparent);
}

.jb-record-acts {
	display: flex;
	flex-shrink: 0;
	gap: 2px;
	margin-left: 6px;

	.el-button {
		padding: 6px;
		/* 头部操作键改变量而不是写 color：el-button 自带 color，也不吃祖先色 */
		--el-button-text-color: var(--el-color-white);
		--el-button-hover-text-color: var(--el-color-white);
	}
}

/* 亮色下 is-text 的 hover/active 底是近白实色，压在主题色上就是一块白框；这里换成头部文字色的半透明膜。
 * 暗色那一档本就是抬亮膜、压在主题色上仍分得开，所以只覆盖亮色 */
html:not([data-theme='dark']) .jb-record-acts .el-button {
	--el-fill-color-light: color-mix(in srgb, var(--el-color-white) 15%, transparent);
	--el-fill-color: color-mix(in srgb, var(--el-color-white) 25%, transparent);
}
</style>

<style lang="scss" scoped>
/* 卡片与查询区全部走全局语汇（theme/page.scss 的 .query-toolbar / .entity-card 系列），
   与 AI 智能体 · 工具管理页保持同一套视觉，本页不再自造卡片样式 */
.jb-page {
	/* 页边距统一由 .layout-parent 的 padding:5px 提供（与打印模板页一致），这里不再叠加 */
	position: relative;
	padding-bottom: 4px;

	.page-hero {
		flex-shrink: 0;
	}

	> .query-toolbar {
		flex-shrink: 0;
		// margin-top: 14px;
	}

	> .full-table {
		// margin-top: 14px;
	}
}

/* vxe 工具栏按钮组：按钮数量多（新增 / 启停 / 唤醒 / 集群 / 看板 / 展开折叠），允许换行 */
.jb-bar {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

/* 表格子卡片区：展开行内不套 .card-grid（它是 flex:1 + 内部滚动，会把行高压死），
   只补一层与卡片墙一致的内边距，纵向间距交给 .card-grid-row 的 row-gap */
.jb-triggers {
	padding: 8px 6px 4px;
}

/* 出错计数：卡片骨架沿用全局 .metric-item，只给数值一个危险色 */
.metric-item b.is-bad {
	color: var(--el-color-danger);
}
</style>
