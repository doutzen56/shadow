<template>
	<div class="db-page">
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
					<h2 class="page-hero__title">数据库备份</h2>
					<p class="page-hero__desc">按库生成备份文件，可压缩后下载留存；删除与压缩操作只影响备份文件，不动线上数据。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="统计按当前备份列表计算">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statCount }}</span>
							<span class="page-hero__stat-k">备份文件</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ statSize }}</span>
							<span class="page-hero__stat-k">占用空间</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on is-key" :title="statLatest || '暂无备份'">{{ statLatest || '—' }}</span>
							<span class="page-hero__stat-k">最近备份</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 选择库 -->
		<el-card class="db-filter" shadow="hover">
			<el-form class="db-form" :model="state" label-width="auto">
				<el-form-item label="库名">
					<el-select v-model="state.configId" placeholder="请选择要备份的库" filterable class="db-select-db">
						<el-option v-for="item in state.dbData" :key="item.configId" :label="`${item.dbName}(${item.configId})`" :value="item.configId" />
					</el-select>
				</el-form-item>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'dbBackup/add'" :loading="state.adding"> 新增备份 </el-button>
				<el-tooltip content="刷新备份列表" placement="bottom">
					<el-button icon="ele-Refresh" circle plain @click="handleQuery" />
				</el-tooltip>
			</el-form>
		</el-card>

		<div class="db-table-wrap">
			<el-card class="full-table" shadow="hover">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
					<template #empty>
						<div class="db-empty">
							<span class="db-empty-ico"><el-icon><ele-Files /></el-icon></span>
							<p class="db-empty-text">还没有备份文件，选好库后点「新增备份」</p>
						</div>
					</template>
					<template #row_fileName="{ row }">
						<span class="db-file">
							<el-icon class="db-file-ico"><ele-Document /></el-icon>
							<span class="db-file-name">{{ row.fileName }}</span>
							<el-tag v-if="row.fileName.endsWith('.zip')" type="success" size="small">已压缩</el-tag>
						</span>
					</template>
					<template #row_dbName="{ row }">
						<span class="db-owner" :title="row.configId ? `库标识：${row.configId}` : '老版本备份文件，未记录所属库'">
							<span class="db-owner-name">{{ row.dbName || '—' }}</span>
							<el-tag v-if="row.dbTypeName" size="small" effect="plain" type="info">{{ row.dbTypeName }}</el-tag>
						</span>
					</template>
					<template #row_size="{ row }">
						<span>{{ formatFileSize(row.size) }}</span>
					</template>
					<template #row_createTime="{ row }">
						<el-tooltip :content="row.createTime" placement="top">
							<span>{{ formatPast(row.createTime) }}</span>
						</el-tooltip>
					</template>
					<template #row_buttons="{ row }">
						<el-tooltip content="下载备份文件" placement="top">
							<el-button icon="ele-Download" text type="primary" @click="handleDownload(row)"> </el-button>
						</el-tooltip>
						<el-tooltip content="压缩以减小体积" placement="top">
							<el-button icon="ele-Files" text type="primary" v-if="!row.fileName.endsWith('.zip')" :loading="state.compressing === row.fileName" @click="handleCompress(row)"> </el-button>
						</el-tooltip>
						<el-tooltip content="删除备份文件" placement="top">
							<el-button icon="ele-Delete" text type="danger" v-auth="'dbBackup/delete'" :loading="state.deleting === row.fileName" @click="handleDelete(row)"> </el-button>
						</el-tooltip>
					</template>
				</vxe-grid>
			</el-card>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { formatPast } from '/@/utils/formatTime';
import { downloadByUrl } from '/@/utils/download';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi, SysDbBackupApi } from '/@/api-services/system/api';
import { formatFileSize } from './database';

const xGrid = ref<VxeGridInstance>();

const state = reactive({
	dbData: [] as any,
	configId: '',
	rows: [] as any[],
	adding: false,
	// 用文件名标记正在操作的行，避免一个 loading 让所有按钮一起转
	compressing: '',
	deleting: '',
});

// 头图统计
const statCount = computed(() => state.rows.length);
const statSize = computed(() => formatFileSize(state.rows.reduce((sum, u) => sum + (u.size ?? 0), 0)));
// 接口不保证按时间排序，这里自己取最晚的一条，避免头图显示成第一次备份
const statLatest = computed(() => {
	if (!state.rows.length) return '';
	const latest = state.rows.reduce((a, b) => (String(a.createTime ?? '') > String(b.createTime ?? '') ? a : b));
	return formatPast(latest.createTime);
});

// 表格参数配置
const options = useVxeTable(
	{
		id: 'dbBackup',
		name: '备份信息',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 60, fixed: 'left' },
			{ field: 'fileName', title: '文件名称', minWidth: 240, showOverflow: 'tooltip', slots: { default: 'row_fileName' } },
			{ field: 'dbName', title: '所属库', width: 190, showOverflow: 'tooltip', slots: { default: 'row_dbName' } },
			{ field: 'size', title: '文件大小', width: 120, showOverflow: 'tooltip', slots: { default: 'row_size' } },
			{ field: 'createTime', title: '备份时间', width: 180, showOverflow: 'tooltip', slots: { default: 'row_createTime' } },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { enabled: true },
	}
);

// 页面初始化：库列表拉完直接选第一个，少一步操作
onMounted(async () => {
	options.loading = true;
	const res = await getAPI(SysDatabaseApi).apiSysDatabaseListGet();
	state.dbData = res.data.result ?? [];
	if (state.dbData.length > 0) state.configId = state.dbData[0].configId;
	options.loading = false;
});

// 查询api
const handleQueryApi = async () => {
	const res = await getAPI(SysDbBackupApi).apiSysDbBackupListGet();
	state.rows = (res.data.result ?? []) as any[];
	return res;
};

const handleQuery = async () => {
	await xGrid.value?.commitProxy('reload');
};

// 新增备份
const handleAdd = () => {
	if (!state.configId) {
		ElMessage.warning('请选择库名');
		return;
	}
	ElMessageBox.confirm(`确定要为库 ${state.configId} 新增一份备份吗？大库可能耗时较久。`, '新增备份', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			state.adding = true;
			try {
				const res = await getAPI(SysDbBackupApi).apiSysDbBackupAddPost(state.configId, { timeout: 0 /**永不超时 */ });
				const file = res.data.result;
				await handleQuery();
				ElMessage.success(file?.fileName ? `备份已生成：${file.fileName}（${formatFileSize(file.size)}）` : '备份已生成');
			} finally {
				state.adding = false;
			}
		})
		// 取消确认会 reject，这里必须接住，否则控制台抛未捕获的 Promise 异常
		.catch(() => {});
};

// 删除备份
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`确定要删除备份 ${row.fileName} 吗？删除后不可恢复。`, '删除备份', {
		confirmButtonText: '确定删除',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			state.deleting = row.fileName;
			try {
				await getAPI(SysDbBackupApi).apiSysDbBackupDeletePost(row.fileName);
				await handleQuery();
				ElMessage.success('备份已删除');
			} finally {
				state.deleting = '';
			}
		})
		.catch(() => {});
};

// 下载备份
const handleDownload = (row: any) => {
	const baseUrl = import.meta.env.VITE_API_URL;
	downloadByUrl({ url: `${baseUrl}/api/sysDbBackup/download/${row.fileName}`, fileName: row.fileName });
};

// 压缩备份
const handleCompress = (row: any) => {
	ElMessageBox.confirm(`确定要压缩备份 ${row.fileName} 吗？压缩后原文件会被替换。`, '压缩备份', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'info',
	})
		.then(async () => {
			state.compressing = row.fileName;
			try {
				await getAPI(SysDbBackupApi).apiSysDbBackupCompressPost(row.fileName, { timeout: 0 /**永不超时 */ });
				await handleQuery();
				ElMessage.success('备份已压缩');
			} finally {
				state.compressing = '';
			}
		})
		.catch(() => {});
};
</script>

<style lang="scss" scoped>
.db-page {
	position: relative;
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	padding-bottom: 4px;
	overflow: hidden;
}

.db-filter {
	flex-shrink: 0;
	margin-top: 14px;

	:deep(.el-card__body) {
		padding: 14px 18px;
	}
}

.db-form {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 0;
	}
}
.db-select-db {
	width: 260px;
}

.db-table-wrap {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;
	display: flex;
	flex-direction: column;

	:deep(.el-card) {
		height: 100%;
	}
}

.db-file {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}
.db-file-ico {
	font-size: 14px;
	color: var(--el-text-color-secondary);
}
.db-file-name {
	font-family: ui-monospace, Menlo, Consolas, monospace;
}

/* 所属库：库名 + 类型标签，老备份文件没记库信息就显示占位 */
.db-owner {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}
.db-owner-name {
	font-family: ui-monospace, Menlo, Consolas, monospace;
}

.db-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 10px;
	padding: 26px 0;
}
.db-empty-ico {
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
}
.db-empty-text {
	margin: 0;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}
</style>
