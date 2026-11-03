<template>
	<div class="jf-page">
		<el-card shadow="hover" class="jf-surrogate-card">
			<template #header>
				<div class="jf-page-title">
					我的委托
					<el-button style="margin-left: 20px" v-auth="'surrogate/add'" type="primary" size="small" @click="openForm()">＋ 新增委托</el-button>
				</div>
			</template>
			<div v-if="loading" class="jf-loading">加载中...</div>
			<div v-else-if="errorMsg" class="jf-empty">{{ errorMsg }}</div>
			<template v-else>
				<el-table :data="rows" row-key="id" stripe style="width: 100%" border height="calc(100% - 35px)">
					<el-table-column label="流程" min-width="160">
						<template #default="{ row }">{{ row.processName || '全部流程' }}</template>
					</el-table-column>
					<el-table-column label="被委托人" min-width="120">
						<template #default="{ row }"
							><strong>{{ row.surrogate }}</strong></template
						>
					</el-table-column>
					<el-table-column label="生效时间" min-width="240">
						<template #default="{ row }"
							><span class="jf-muted">{{ fmtTime(row.startTime, true) }} ~ {{ fmtTime(row.endTime, true) }}</span></template
						>
					</el-table-column>
					<el-table-column label="状态" min-width="100">
						<template #default="{ row }"
							><JfBadge :type="row.enabled === 1 ? 'done' : 'info'">{{ row.enabled === 1 ? '启用' : '停用' }}</JfBadge></template
						>
					</el-table-column>
					<el-table-column label="操作" min-width="220" fixed="right">
						<template #default="{ row }">
							<div class="jf-btn-row">
								<el-button size="small" :icon="Switch" @click="toggleEnabled(row)">{{ row.enabled === 1 ? '停用' : '启用' }}</el-button>
								<el-button size="small" :icon="Edit" @click="openForm(row)">编辑</el-button>
								<el-button type="danger" size="small" :icon="Delete" @click="remove(row)">删除</el-button>
							</div>
						</template>
					</el-table-column>
				</el-table>

				<!-- 分页 -->
				<div class="jf-pagination">
					<el-pagination layout="prev, pager, next, total" :current-page="pageNum" :page-size="pageSize" :total="recordCount" size="small" background @current-change="go" />
				</div>
			</template>
		</el-card>

		<!-- 新增/编辑委托（body-class 还原 JfDrawer 抽屉体的 padding） -->
		<el-drawer v-model="formVisible" body-class="jf-approve-drawer-body" :title="formId ? '编辑委托' : '新增委托'" size="520px">
			<div class="jf-form-item">
				<label class="jf-form-label">流程编码（留空 = 全部流程）</label>
				<el-input v-model="form.processName" placeholder="leave" />
			</div>
			<div class="jf-form-item">
				<label class="jf-form-label">被委托人 *</label>
				<JfUserPicker v-model="surrogatePicked" scene="surrogate" placeholder="输入姓名/工号搜索" />
			</div>
			<div class="jf-form-row">
				<div class="jf-form-item">
					<label class="jf-form-label">生效时间 *</label>
					<el-input v-model="form.startTime" type="datetime-local" />
				</div>
				<div class="jf-form-item">
					<label class="jf-form-label">失效时间 *</label>
					<el-input v-model="form.endTime" type="datetime-local" />
				</div>
			</div>
			<div class="jf-form-item">
				<label class="jf-form-label">状态</label>
				<el-select v-model="form.enabled" style="width: 100%">
					<el-option :value="1" label="启用" />
					<el-option :value="0" label="停用" />
				</el-select>
			</div>
			<div class="jf-drawer-actions">
				<el-button @click="formVisible = false">取消</el-button>
				<el-button type="primary" :disabled="saving" @click="save">保存</el-button>
			</div>
		</el-drawer>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import JfBadge from '../core/ui/JfBadge.vue';
import JfUserPicker from '../core/ui/JfUserPicker.vue';

import '../core/style.css';
import { fmtTime } from '../core/helpers';
import { ElMessage } from 'element-plus';
import type { SurrogateRow, PageResult } from '../core/types';
import { Edit, Switch, Delete } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { JeeflowSurrogateApi } from '/@/api-services/jeeFlow/api';

defineOptions({ name: 'JfSurrogatePage' });

const loading = ref(false);
const errorMsg = ref('');
const rows = ref<SurrogateRow[]>([]);
const pageNum = ref(1);
const pageSize = 10;
const recordCount = ref(0);
const totalPage = ref(0);

const formVisible = ref(false);
const formId = ref<string | null>(null);
const form = ref<Record<string, any>>({});
const saving = ref(false);

// 单选语义：JfUserPicker 是数组模型，取首元素写入 form.surrogate
const surrogatePicked = computed<string[]>({
	get: () => (form.value.surrogate ? [form.value.surrogate] : []),
	set: (v) => {
		form.value.surrogate = v[0] ?? '';
	},
});

async function reload() {
	loading.value = true;
	errorMsg.value = '';
	try {
		// 委托列表：改用项目标准接口（api/jeeflowSurrogate/page；载荷在 res.data.result）
		const r = await getAPI(JeeflowSurrogateApi)
			.apiJeeflowSurrogatePagePost({ pageNum: pageNum.value, pageSize })
			.then((res) => (res.data?.result ?? { rows: [], recordCount: 0, totalPage: 0 }) as unknown as PageResult<SurrogateRow>);
		rows.value = r.rows;
		recordCount.value = r.recordCount;
		totalPage.value = r.totalPage;
	} catch (e) {
		// 后端未注册扩展仓储时 processSurrogate/* 不可用
		errorMsg.value = `委托功能不可用：${(e as Error).message}（后端需注册扩展仓储）`;
	} finally {
		loading.value = false;
	}
}

function go(p: number) {
	pageNum.value = p;
	reload();
}

function openForm(s?: SurrogateRow) {
	formId.value = s?.id ?? null;
	form.value = {
		processName: s?.processName ?? '',
		surrogate: s?.surrogate ?? '',
		startTime: s?.startTime ? s.startTime.slice(0, 16) : '',
		endTime: s?.endTime ? s.endTime.slice(0, 16) : '',
		enabled: s?.enabled ?? 1,
	};
	formVisible.value = true;
}

async function save() {
	if (!form.value.surrogate || !form.value.startTime || !form.value.endTime) {
		ElMessage.error('被委托人/生效时间/失效时间必填');
		return;
	}
	saving.value = true;
	try {
		const payload: Record<string, any> = {
			...form.value,
			startTime: form.value.startTime ? form.value.startTime.replace('T', ' ') + ':00' : null,
			endTime: form.value.endTime ? form.value.endTime.replace('T', ' ') + ':00' : null,
		};
		if (formId.value) payload.id = formId.value;
		// 保存：改用项目标准接口（api/jeeflowSurrogate/save；文档入参为整行）
		await getAPI(JeeflowSurrogateApi).apiJeeflowSurrogateSavePost(payload);
		ElMessage.success('已保存');
		formVisible.value = false;
		reload();
	} catch (e) {
		ElMessage.error(`保存失败：${(e as Error).message}`);
	} finally {
		saving.value = false;
	}
}

/** 快捷启停：facade save 带 id 时全量覆盖字段，须回传整行 */
async function toggleEnabled(s: SurrogateRow) {
	try {
		// 快捷启停：改用项目标准接口（api/jeeflowSurrogate/save；带 id 全量覆盖）
		await getAPI(JeeflowSurrogateApi).apiJeeflowSurrogateSavePost({
			id: s.id,
			processName: s.processName ?? '',
			surrogate: s.surrogate,
			startTime: s.startTime,
			endTime: s.endTime,
			enabled: s.enabled === 1 ? 0 : 1,
		});
		ElMessage.success(s.enabled === 1 ? '已停用' : '已启用');
		reload();
	} catch (e) {
		ElMessage.error(`操作失败：${(e as Error).message}`);
	}
}

async function remove(s: SurrogateRow) {
	if (!window.confirm('确认删除该委托？')) return;
	try {
		// 删除：改用项目标准接口（api/jeeflowSurrogate/remove；文档入参 { ids }）
		await getAPI(JeeflowSurrogateApi).apiJeeflowSurrogateRemovePost({ ids: [s.id] });
		ElMessage.success('已删除');
		reload();
	} catch (e) {
		ElMessage.error(`删除失败：${(e as Error).message}`);
	}
}

onMounted(reload);
</script>

<style scoped>
.jf-surrogate-card {
	height: 100%;
}
.jf-surrogate-card :deep(.el-card__body) {
	height: 100%;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding-bottom: 0;
}
</style>
