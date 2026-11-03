<template>
	<div class="jf-page jf-page--full" :class="{ 'jf-page--designing': designing }">
		<el-card class="jf-design-card" shadow="hover">
			<template #header>
				<div class="jf-page-title">
					流程设计
					<el-input v-model="keyword" class="jf-page-search" placeholder="搜索编码/显示名..." @keyup.enter="goSearch" clearable />
					<el-button v-auth="'design/add'" type="primary" size="small" @click="createNew">＋ 新建流程</el-button>
					<el-button v-auth="'design/add'" size="small" title="导入 LogicFlow JSON（演示稿：/showcase-leave.json）" @click="triggerImport">导入 JSON</el-button>
					<input ref="importInput" type="file" accept="application/json" style="display: none" @change="importJson" />
				</div>
			</template>
			<template v-if="!designing">
				<div v-if="loading" class="jf-loading">加载中...</div>
				<div v-else-if="errorMsg" class="jf-empty">{{ errorMsg }}</div>
				<template v-else>
					<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
						<el-table-column label="编码" min-width="120">
							<template #default="{ row }"
								><span class="jf-muted">{{ row.name }}</span></template
							>
						</el-table-column>
						<el-table-column label="显示名" min-width="160">
							<template #default="{ row }"
								><strong>{{ row.displayName }}</strong></template
							>
						</el-table-column>
						<el-table-column label="类型" min-width="100">
							<template #default="{ row }"
								><span class="jf-muted">{{ row.type }}</span></template
							>
						</el-table-column>
						<el-table-column label="部署" min-width="100">
							<template #default="{ row }"
								><JfBadge :type="row.isDeployed === 1 ? 'done' : 'info'">{{ row.isDeployed === 1 ? '已部署' : '未部署' }}</JfBadge></template
							>
						</el-table-column>
						<el-table-column label="更新时间" min-width="160">
							<template #default="{ row }"
								><span class="jf-muted">{{ fmtTime(row.updateTime || row.createTime, true) }}</span></template
							>
						</el-table-column>
						<el-table-column label="操作" min-width="200" fixed="right">
							<template #default="{ row }">
								<div class="jf-btn-row">
									<el-button size="small" :icon="Edit" @click="edit(row)">编辑</el-button>
									<el-button v-auth="'design/publish'" type="primary" size="small" :icon="Upload" @click="deploy(row)">{{ row.isDeployed === 1 ? '重新发布' : '发布' }}</el-button>
									<el-button v-auth="'design/delete'" type="danger" size="small" :icon="Delete" @click="remove(row)">删除</el-button>
								</div>
							</template>
						</el-table-column>
					</el-table>

					<!-- 分页 -->
					<div class="jf-pagination">
						<el-pagination layout="prev, pager, next, total" :current-page="pageNum" :page-size="pageSize" :total="recordCount" size="small" background @current-change="go" />
					</div>
				</template>
			</template>
			<!-- 设计器（编辑模式：mldong-flow-designer-dingtalk） -->
			<div v-else class="jf-designer-wrap">
				<div class="jf-designer-bar">
					<el-input v-model="designName" placeholder="流程编码（唯一，如 leave）" style="width: 200px" />
					<el-input v-model="designDisplayName" placeholder="显示名（如 请假审批）" style="width: 200px" />
					<span class="jf-muted" style="font-size: 13px">{{ design?.isDeployed === 1 ? '（已发布，改动后需重新发布）' : '（未发布）' }}</span>
					<div style="flex: 1"></div>
					<el-button
						@click="
							designing = false;
							reload();
						"
					>
						返回列表
					</el-button>
					<el-button :disabled="saving" @click="saveDraft">保存草稿</el-button>
					<el-button type="primary" :disabled="saving" @click="saveAndDeploy">保存并发布</el-button>
				</div>
				<div class="jf-designer-body">
					<FlowDesigner ref="designerRef" v-if="designing" v-model:value="designJson" mode="dingtalk" :dnd-panel="dndPanel" @on-save="handleDesignerSave" />
				</div>
			</div>
		</el-card>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, h } from 'vue';
import FlowDesigner from 'mldong-flow-designer-dingtalk';
import type { FDPatternItem, FDFormType } from 'mldong-flow-designer-dingtalk';
import 'mldong-flow-designer-dingtalk/lib/style.css';
import JfBadge from '../core/ui/JfBadge.vue';
import JfAssigneeField from '../core/ui/JfAssigneeField.vue';

import '../core/style.css';
import { fmtTime } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { DesignRow, PageResult } from '../core/types';
import { Edit, Upload, Delete } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowDesignApi, JeeflowFormDesignApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfProcessDesignPage' });

// 审批节点（task）编辑面板：dndPanel 的 form 是"整体替换"而非合并，
// 所以这里复刻内置 schema.task 的完整字段集，只覆写「表单」「参与人」两项
// - 表单：Input → Select 下拉（选项来自 /api/formDesign/list，见 loadFormOptions）
// - 参与人：Input → JfUserPicker（多选，存储为逗号分隔 userId 串）

// 表单设计下拉选项：数组引用直接传给设计器 props，设计器挂载时读取；
// 进入设计视图（designing=true）前 onMounted 已加载完毕，因此下拉即为实时数据。
const formOptions: { label: string; value: number }[] = [];

async function loadFormOptions() {
	try {
		const res = await getAPI(JeeflowFormDesignApi).apiJeeflowFormDesignListGet();
		const list = res.data.result ?? [];
		formOptions.length = 0;
		formOptions.push(...list.map((f) => ({ label: f.name ?? '', value: f.id as number })));
	} catch {
		// 加载失败保留空选项，不阻断设计器使用
	}
}

const taskForm: FDFormType = {
	labelWidth: 'fd.labelWidth.narrow',
	formItems: [
		{ name: 'name', label: 'fd.field.code', component: 'Input' },
		{ name: 'displayName', label: 'fd.field.displayName', component: 'Input' },
		{ name: 'preInterceptors', label: 'fd.field.preInterceptors', component: 'Input' },
		{ name: 'postInterceptors', label: 'fd.field.postInterceptors', component: 'Input' },
		{
			// ↓ 覆写项 1：表单下拉（选项由 /api/formDesign/list 动态加载，见 loadFormOptions）
			name: 'form',
			label: 'fd.field.form',
			component: 'Select',
			componentProps: {
				placeholder: 'fd.field.form.ph',
				options: formOptions,
			},
		},
		{
			// ↓ 覆写项 2：参与人选人组件（render 返回的组件会被 v-model 绑到该字段值）
			name: 'assignee',
			label: 'fd.field.assignee',
			render: () => h(JfAssigneeField),
		},
		{ name: 'assignmentHandler', label: 'fd.field.assignmentHandler', component: 'Input' },
		{
			name: 'candidateUsers',
			label: 'fd.field.candidateUsers',
			component: 'Input',
			helpMessage: 'fd.help.candidateUsers',
			componentProps: { placeholder: 'fd.field.candidateUsers.ph' },
		},
		{
			name: 'candidateGroups',
			label: 'fd.field.candidateGroups',
			component: 'Input',
			helpMessage: 'fd.help.candidateGroups',
			componentProps: { placeholder: 'fd.field.candidateGroups.ph' },
		},
		{ name: 'candidateHandler', label: 'fd.field.candidateHandler', component: 'Input' },
		{
			name: 'taskType',
			label: 'fd.field.taskType',
			component: 'Select',
			componentProps: {
				options: [
					{ label: 'fd.opt.major', value: 'Major' },
					{ label: 'fd.opt.aidant', value: 'Aidant' },
				],
			},
		},
		{
			name: 'performType',
			label: 'fd.field.performType',
			component: 'Select',
			componentProps: {
				options: [
					{ label: 'fd.opt.performAny', value: 'ANY' },
					{ label: 'fd.opt.performAll', value: 'ALL' },
				],
			},
		},
		{
			name: 'countersignType',
			label: 'fd.field.countersignType',
			component: 'Select',
			defaultValue: 'PARALLEL',
			helpMessage: 'fd.help.countersignType',
			componentProps: {
				options: [
					{ label: 'fd.opt.csParallel', value: 'PARALLEL' },
					{ label: 'fd.opt.csSequential', value: 'SEQUENTIAL' },
				],
			},
		},
		{
			name: 'countersignCompletionCondition',
			label: 'fd.field.countersignCompletionCondition',
			component: 'Input',
			helpMessage: 'fd.help.countersignCompletionCondition',
		},
		{
			name: 'actionBtns',
			label: 'fd.field.actionBtns',
			component: 'Input',
			helpMessage: ['fd.help.actionBtns.1', 'fd.help.actionBtns.2'],
			componentProps: { placeholder: 'fd.field.actionBtns.ph' },
		},
		{ name: 'reminderTime', label: 'fd.field.reminderTime', component: 'Input' },
		{ name: 'reminderRepeat', label: 'fd.field.reminderRepeat', component: 'Input' },
		{ name: 'expireTime', label: 'fd.field.expireTime', component: 'Input' },
		{
			name: 'autoExecute',
			label: 'fd.field.autoExecute',
			component: 'Select',
			componentProps: {
				options: [
					{ label: 'fd.opt.yes', value: 'Y' },
					{ label: 'fd.opt.no', value: 'N' },
				],
			},
		},
		{ name: 'callback', label: 'fd.field.callback', component: 'Input' },
	],
};

// 按 type 合并进默认面板：只覆盖 task 项的 form，不影响其它节点类型
const dndPanel: FDPatternItem[] = [{ type: 'task', form: taskForm }];

const loading = ref(false);
const errorMsg = ref('');
const rows = ref<DesignRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);
const keyword = ref('');

// 设计器状态
const designing = ref(false);
const design = ref<(DesignRow & { jsonObject?: any }) | null>(null);
const designName = ref('');
const designDisplayName = ref('');
const designJson = ref<any>(null);
const saving = ref(false);
const importInput = ref<HTMLInputElement | null>(null);

// 设计器实例引用：保存时经 getGraphData() 取实时画布数据（v-model 会滞后，见 saveDraft 注释）
const designerRef = ref<any>(null);

function triggerImport() {
	importInput.value?.click();
}

async function importJson(e: Event) {
	const file = (e.target as HTMLInputElement).files?.[0];
	(e.target as HTMLInputElement).value = '';
	if (!file) return;
	try {
		const json = JSON.parse(await file.text());
		if (!json || typeof json !== 'object') throw new Error('不是有效的流程 JSON');
		design.value = { id: '', name: json.name || '', displayName: json.displayName || '', type: json.type || 'approval', isDeployed: 0, jsonObject: json };
		designName.value = json.name || 'imported';
		designDisplayName.value = json.displayName || '导入流程';
		designJson.value = json;
		designing.value = true;
		ElMessage.success('已导入设计稿，请保存并发布');
	} catch (err) {
		ElMessage.error((err as Error).message || '导入失败');
	}
}

async function reload() {
	loading.value = true;
	errorMsg.value = '';
	try {
		// 流程设计列表：改用项目标准接口（api/jeeflowDesign/page；载荷在 res.data.result）
		const r = await getAPI(JeeflowDesignApi)
			.apiJeeflowDesignPagePost({
				pageNum: pageNum.value,
				pageSize,
				orderBy: 't.update_time desc',
				...(keyword.value.trim() ? { m_LIKE_displayName: keyword.value.trim() } : {}),
			})
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<DesignRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		// 后端未注册扩展仓储（IProcessExtRepository）时 processDesign/* 不可用
		errorMsg.value = `流程设计功能不可用：${(e as Error).message}（后端需注册扩展仓储）`;
	} finally {
		loading.value = false;
	}
}

function go(p: number) {
	pageNum.value = p;
	reload();
}

function goSearch() {
	pageNum.value = 1;
	reload();
}

async function createNew() {
	design.value = { id: '', name: '', displayName: '', type: 'approval', isDeployed: 0, jsonObject: null };
	designName.value = '';
	designDisplayName.value = '';
	designJson.value = null;
	designing.value = true;
}

async function edit(d: DesignRow) {
	// 流程设计详情：改用项目标准接口（api/jeeflowDesign/detail；载荷在 res.data.result）
	const detail = await getAPI(JeeflowDesignApi)
		.apiJeeflowDesignDetailPost({ id: d.id })
		.then((res) => (res.data?.result ?? {}) as unknown as DesignRow & { jsonObject?: Record<string, any> | null });
	design.value = { ...d, jsonObject: detail.jsonObject };
	designName.value = d.name;
	designDisplayName.value = d.displayName;
	// 设计器 v-model 数据：取最新设计稿内容（含 name/displayName/type 补齐）
	designJson.value = detail.jsonObject ? { ...detail.jsonObject } : null;
	designing.value = true;
}

/** 发布/重新发布：未部署走 deploy；已部署走 redeploy（改版重发布，对齐 E2E S8） */
async function deploy(d: DesignRow) {
	const re = d.isDeployed === 1;
	if (!window.confirm(`确认${re ? '重新' : ''}发布设计「${d.displayName}」？（${re ? '已部署流程改版重发布' : '生成流程定义，版本+1'}）`)) return;
	try {
		if (re) await getAPI(JeeflowDesignApi).apiJeeflowDesignRedeployPost({ id: d.id });
		else await getAPI(JeeflowDesignApi).apiJeeflowDesignDeployPost({ id: d.id });
		ElMessage.success(re ? '重新发布成功' : '发布成功');
		reload();
	} catch (e) {
		ElMessage.error((e as Error).message || '发布失败');
	}
}

async function remove(d: DesignRow) {
	if (!window.confirm(`确认删除设计「${d.displayName}」？`)) return;
	try {
		// 删除：改用项目标准接口（api/jeeflowDesign/remove；文档入参 { ids }）
		await getAPI(JeeflowDesignApi).apiJeeflowDesignRemovePost({ ids: [d.id] });
		ElMessage.success('已删除');
		reload();
	} catch (e) {
		ElMessage.error((e as Error).message || '删除失败');
	}
}

/** 设计器保存（@on-save）：先确保设计存在（新建先 save 拿 id），再 updateDefine 存草稿 */
async function handleDesignerSave(data: any) {
	saving.value = true;
	try {
		if (!design.value?.id) {
			// 保存设计（拿 id）：改用项目标准接口（api/jeeflowDesign/save；载荷在 res.data.result）
			const r = await getAPI(JeeflowDesignApi)
				.apiJeeflowDesignSavePost({
					name: designName.value || data?.name || 'new-flow',
					displayName: designDisplayName.value || data?.displayName || '新流程',
					type: data?.type || 'approval',
				})
				.then((res) => (res.data?.result ?? { id: '' }) as unknown as { id: string });
			design.value = { ...design.value!, id: r.id, isDeployed: 0 };
		} else {
			// 更新基本信息：改用项目标准接口（api/jeeflowDesign/update；文档入参 { id, name, displayName }）
			await getAPI(JeeflowDesignApi).apiJeeflowDesignUpdatePost({
				id: design.value.id,
				name: designName.value,
				displayName: designDisplayName.value,
			});
		}
		// 内容快照：流程 JSON（name/displayName/type 与顶部输入对齐）
		// 更新内容快照：改用项目标准接口（api/jeeflowDesign/updateDefine；文档入参 { id, content,... }）
		await getAPI(JeeflowDesignApi).apiJeeflowDesignUpdateDefinePost({
			id: design.value.id,
			...data,
			name: designName.value || data?.name,
			displayName: designDisplayName.value || data?.displayName,
			type: data?.type || design.value?.type || 'approval',
		});
		reload();
	} finally {
		saving.value = false;
	}
}

/** 保存草稿：以当前画布内容入库（未进设计器时仅保存基本信息） */
async function saveDraft() {
	if (designJson.value) {
		// 关键修复：节点编辑抽屉里改的 表单/参与人 等属性只写入设计器内部 treeData，
		// 设计器不会为此 emit update:value（v-model 滞后），直接存 designJson 会丢字段；
		// 必须经 getGraphData() 实时导出最新画布数据（内部已含全部 properties）
		const data = designerRef.value?.getDesignerApi?.()?.getGraphData?.() || designJson.value;
		await handleDesignerSave(data);
	} else {
		saving.value = true;
		try {
			if (!design.value?.id) {
				// 保存设计（拿 id）：改用项目标准接口（api/jeeflowDesign/save；载荷在 res.data.result）
				const r = await getAPI(JeeflowDesignApi)
					.apiJeeflowDesignSavePost({
						name: designName.value || 'new-flow',
						displayName: designDisplayName.value || '新流程',
						type: 'approval',
					})
					.then((res) => (res.data?.result ?? { id: '' }) as unknown as { id: string });
				design.value = { ...design.value!, id: r.id, isDeployed: 0 };
			} else {
				// 更新基本信息：改用项目标准接口（api/jeeflowDesign/update；文档入参 { id, name, displayName }）
				await getAPI(JeeflowDesignApi).apiJeeflowDesignUpdatePost({
					id: design.value.id,
					name: designName.value,
					displayName: designDisplayName.value,
				});
			}
			reload();
		} finally {
			saving.value = false;
		}
	}
}

/** 保存并发布：画布内容入库 + 生成流程定义（version+1） */
async function saveAndDeploy() {
	await saveDraft();
	if (design.value?.id) {
		try {
			await getAPI(JeeflowDesignApi).apiJeeflowDesignDeployPost({ id: design.value.id });
			ElMessage.success('发布成功');
			reload();
		} catch (e) {
			ElMessage.error((e as Error).message || '发布失败');
		}
	}
}

onMounted(async () => {
	reload();
	await loadFormOptions();
});
</script>

<style scoped>
.jf-page-search {
	width: 220px;
	font-weight: normal;
}
.jf-page-title {
	gap: 10px;
}
.jf-page-title .el-button + .el-button {
	margin-left: 0;
}
.jf-design-card {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}
.jf-design-card :deep(.el-card__header) {
	flex-shrink: 0;
}
.jf-design-card :deep(.el-card__body) {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}
</style>
