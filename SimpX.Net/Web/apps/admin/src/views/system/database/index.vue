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
					<h2 class="page-hero__title">数据库管理</h2>
					<p class="page-hero__desc">在线查看与维护库表结构：改字段、调顺序、生成实体与种子数据，不用再手写建表脚本。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="统计按当前已加载的数据计算">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.dbData.length }}</span>
							<span class="page-hero__stat-k">数据库</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.tableData.length }}</span>
							<span class="page-hero__stat-k">数据表</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on is-key" :title="state.tableName || '未选择表'">{{ state.tableName ? state.columnData.length : '—' }}</span>
							<span class="page-hero__stat-k">字段数</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 库 / 表选择 -->
		<el-card class="db-filter" shadow="hover">
			<template #header>
				<div class="db-head">
					<span class="db-head-ico is-blue"><el-icon><ele-Coin /></el-icon></span>
					<span class="db-head-title">选择库与表</span>
					<span class="db-head-sub">先选库，再选表，下方会列出这张表的全部字段</span>
					<div class="db-head-acts">
						<el-tooltip content="重新拉取表列表" placement="bottom">
							<el-button icon="ele-Refresh" size="small" circle plain @click="handleQueryTable" />
						</el-tooltip>
					</div>
				</div>
			</template>
			<el-form class="db-form" :model="state" label-width="auto">
				<el-form-item label="库名" prop="configId">
					<el-select v-model="state.configId" placeholder="请选择库名" filterable class="db-select-db" @change="handleQueryTable">
						<el-option v-for="item in state.dbData" :key="item.configId" :label="`${item.dbName}(${item.configId})`" :value="item.configId" />
					</el-select>
				</el-form-item>
				<el-tag v-if="curDb?.dbTypeName" class="db-type-tag" size="small" effect="plain" type="info" :title="`数据库类型：${curDb.dbTypeName}`">
					{{ curDb.dbTypeName }}
				</el-tag>
				<el-form-item label="表名" prop="tableName">
					<el-select v-model="state.tableName" placeholder="请选择表名" filterable clearable class="db-select-tb" @change="handleQueryColumn">
						<el-option v-for="item in state.tableData" :key="item.name" :label="`${item.name}${item.description ? '[' + item.description + ']' : ''}`" :value="item.name" />
					</el-select>
				</el-form-item>
			</el-form>
		</el-card>

		<div class="db-table-wrap">
			<el-card class="full-table" shadow="hover">
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #toolbar_buttons>
						<el-button-group>
							<el-button icon="ele-Plus" type="primary" @click="handleAddTable"> 增加表 </el-button>
							<el-button icon="ele-Edit" type="primary" @click="showEditTable"> 编辑表 </el-button>
							<el-button icon="ele-Delete" type="danger" @click="handleDeleteTable"> 删除表 </el-button>
						</el-button-group>
						<el-button-group class="db-tb-gap">
							<el-button icon="ele-CirclePlus" @click="showAddColumn"> 增加列 </el-button>
							<el-button icon="ele-CirclePlus" @click="showGenDialog"> 生成实体 </el-button>
							<el-tooltip content="如果是刚刚生成的实体，请重启后台服务后再生成种子" placement="bottom">
								<el-button icon="ele-CirclePlus" @click="showGenSeedDataDialog"> 生成种子 </el-button>
							</el-tooltip>
						</el-button-group>
						<!-- 下面三个都是库级 / 全局操作，各自独立，不并成按钮组（并组会被误读成一组互斥动作） -->
						<el-button class="db-tb-gap" icon="ele-Refresh" type="warning" plain @click="handleInitTableAndSeedData"> 初始化库表结构及种子数据 </el-button>
						<el-button icon="ele-View" type="primary" plain @click="visualTable"> 库表关系可视化 </el-button>
						<el-tooltip :content="backupTip" placement="bottom">
							<el-button class="db-tb-btn" icon="ele-FolderAdd" plain :loading="state.backing" @click="handleBackup"> 备份数据库 </el-button>
						</el-tooltip>
					</template>
					<template #toolbar_tools>
						<span class="db-cur" v-if="state.tableName">
							<el-icon><ele-FolderOpened /></el-icon>
							{{ curTableLabel }}
						</span>
					</template>
					<template #empty>
						<div class="db-empty">
							<span class="db-empty-ico"><el-icon><ele-Coin /></el-icon></span>
							<p class="db-empty-text">{{ emptyText }}</p>
						</div>
					</template>
					<template #row_dbColumnName="{ row }">
						<span class="db-col">
							<el-icon class="db-col-ico" :class="row.isPrimarykey ? 'is-pk' : ''"><ele-Key v-if="row.isPrimarykey" /><ele-Memo v-else /></el-icon>
							<span class="db-col-name">{{ row.dbColumnName }}</span>
						</span>
					</template>
					<template #row_dataType="{ row }">
						<span class="db-type" :class="`is-${typeTone(row.dataType)}`">{{ row.dataType }}</span>
					</template>
					<template #row_isPrimarykey="{ row }">
						<el-tag type="success" v-if="row.isPrimarykey === true">是</el-tag>
						<el-tag type="info" v-else>否</el-tag>
					</template>
					<template #row_isIdentity="{ row }">
						<el-tag type="success" v-if="row.isIdentity === true">是</el-tag>
						<el-tag type="info" v-else>否</el-tag>
					</template>
					<template #row_isNullable="{ row }">
						<el-tag v-if="row.isNullable === true">是</el-tag>
						<el-tag type="info" v-else>否</el-tag>
					</template>
					<template #row_buttons="{ row }">
						<el-tooltip content="上移" placement="top">
							<el-button icon="ele-Top" size="small" text type="primary" @click="moveColumn(row, 'up')" :disabled="row.$index === 0"></el-button>
						</el-tooltip>
						<el-tooltip content="下移" placement="top">
							<el-button icon="ele-Bottom" size="small" text type="primary" @click="moveColumn(row, 'down')" :disabled="row.$index === state.columnData.length - 1"></el-button>
						</el-tooltip>
						<el-tooltip content="编辑" placement="top">
							<el-button icon="ele-Edit" text type="primary" @click="showEditColumn(row)"> </el-button>
						</el-tooltip>
						<el-tooltip content="删除" placement="top">
							<el-button icon="ele-Delete" text type="danger" @click="handleDeleteColumn(row)"> </el-button>
						</el-tooltip>
					</template>
				</vxe-grid>
			</el-card>
		</div>

		<EditTable ref="editTableRef" @handleQueryTable="handleQueryTable" />
		<EditColumn ref="editColumnRef" @handleQueryColumn="handleQueryColumn" />
		<AddTable ref="addTableRef" @addTableSubmitted="addTableSubmitted" />
		<AddColumn ref="addColumnRef" @handleQueryColumn="handleQueryColumn" />
		<GenEntity ref="genEntityRef" :applicationNamespaces="state.appNamespaces" />
		<GenSeedData ref="genSeedDataRef" :applicationNamespaces="state.appNamespaces" />
		<InitTableAndSeedData ref="initTableAndSeedDataRef" />
	</div>
</template>

<!-- 数据库管理 -->
<script lang="ts" setup name="sysDatabase">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useRouter } from 'vue-router';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

import EditTable from '/@/views/system/database/component/editTable.vue';
import EditColumn from '/@/views/system/database/component/editColumn.vue';
import AddTable from '/@/views/system/database/component/addTable.vue';
import AddColumn from '/@/views/system/database/component/addColumn.vue';
import GenEntity from '/@/views/system/database/component/genEntity.vue';
import GenSeedData from '/@/views/system/database/component/genSeedData.vue';
import InitTableAndSeedData from '/@/views/system/database/component/initTableAndSeedData.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi, SysCodeGenApi, SysDbBackupApi } from '/@/api-services/system/api';
import { DbColumnOutput, DbTableInfo, DbColumnInput, DeleteDbTableInput, DeleteDbColumnInput, MoveDbColumnInput } from '/@/api-services/system/models';
import { formatFileSize } from './database';

const xGrid = ref<VxeGridInstance>();
const editTableRef = ref<InstanceType<typeof EditTable>>();
const editColumnRef = ref<InstanceType<typeof EditColumn>>();
const addTableRef = ref<InstanceType<typeof AddTable>>();
const addColumnRef = ref<InstanceType<typeof AddColumn>>();
const genEntityRef = ref<InstanceType<typeof GenEntity>>();
const genSeedDataRef = ref<InstanceType<typeof GenSeedData>>();
const initTableAndSeedDataRef = ref<InstanceType<typeof InitTableAndSeedData>>();
const router = useRouter();

const state = reactive({
	dbData: [] as any,
	configId: '',
	tableData: [] as Array<DbTableInfo>,
	tableName: '',
	columnData: [] as Array<DbColumnOutput>,
	appNamespaces: [] as Array<string>, // 实体 / 种子存放位置
	backing: false, // 正在备份：库大了要等一会儿，按钮得给出反馈
});

// 当前表（带注释）展示名
const curTableLabel = computed(() => {
	const hit = state.tableData.find((u: any) => u.name === state.tableName);
	return hit?.description ? `${state.tableName}（${hit.description}）` : state.tableName;
});

// 当前库：备份要按库名 + 类型提示，避免用户备错库
const curDb = computed(() => state.dbData.find((u: any) => u.configId === state.configId));

// 备份按钮的悬浮提示：把要备份的库直接写在上面，点下去之前就能看清
const backupTip = computed(() => {
	if (!state.configId) return '备份数据库：请先选择库名';
	const name = curDb.value?.dbName ?? state.configId;
	const type = curDb.value?.dbTypeName ?? '未知类型';
	return `备份当前所选库：${name}（${type}）`;
});

// 空态文案按进度给不同引导，避免只显示一句「暂无数据」
const emptyText = computed(() => {
	if (!state.configId) return '先在上方选择一个数据库';
	if (!state.tableName) return '再选择一张表，这里会列出它的全部字段';
	return '这张表还没有字段';
});

// 字段类型按用途着色，扫一眼就能分出字符串 / 数值 / 时间
const typeTone = (dataType?: string | null): string => {
	const t = (dataType ?? '').toLowerCase();
	if (['varchar', 'nvarchar', 'char', 'nchar', 'text', 'clob'].includes(t)) return 'str';
	if (['bigint', 'int', 'smallint', 'tinyint', 'decimal'].includes(t)) return 'num';
	if (['datetime', 'date', 'time', 'timestamp'].includes(t)) return 'time';
	if (['bit', 'boolean'].includes(t)) return 'bool';
	return 'other';
};

// 表格参数配置
const options = useVxeTable<DbColumnOutput>(
	{
		id: 'sysDatabase',
		name: '库表信息',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 50, fixed: 'left' },
			{ field: 'dbColumnName', title: '字段名', minWidth: 200, showOverflow: 'tooltip', slots: { default: 'row_dbColumnName' } },
			{ field: 'dataType', title: '数据类型', minWidth: 120, showOverflow: 'tooltip', slots: { default: 'row_dataType' } },
			{ field: 'isPrimarykey', title: '主键', minWidth: 70, slots: { default: 'row_isPrimarykey' } },
			{ field: 'isIdentity', title: '自增', minWidth: 70, slots: { default: 'row_isIdentity' } },
			{ field: 'isNullable', title: '可空', minWidth: 70, slots: { default: 'row_isNullable' } },
			{ field: 'length', title: '长度', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'decimalDigits', title: '精度', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'defaultValue', title: '默认值', minWidth: 80, showOverflow: 'tooltip' },
			{ field: 'columnDescription', title: '描述', minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', fixed: 'right', width: 'auto', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryColumnApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {
	options.loading = true;
	const res = await getAPI(SysDatabaseApi).apiSysDatabaseListGet();
	state.dbData = res.data.result ?? [];

	const appNamesRes = await getAPI(SysCodeGenApi).apiSysCodeGenApplicationNamespacesGet();
	state.appNamespaces = (appNamesRes.data.result ?? []) as Array<string>;

	// 只有一个库时直接选上，少一步操作
	if (state.dbData.length === 1) {
		state.configId = state.dbData[0].configId;
		await handleQueryTable();
	}
	options.loading = false;
});

// 查询列api
const handleQueryColumnApi = async () => {
	if (state.tableName == '' || typeof state.tableName == 'undefined') {
		return;
	}
	return getAPI(SysDatabaseApi).apiSysDatabaseColumnListTableNameConfigIdGet(state.tableName, state.configId);
};

// 查询列操作
const handleQueryColumn = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('query');
	// 上移/下移与头图统计都依赖这份数据：不落地的话 findIndex 永远取不到，
	// 「下移」会被当成末位而失效（原先 state.columnData 从未赋值）
	state.columnData = (xGrid.value?.getTableData().fullData as Array<DbColumnOutput>) ?? [];
	options.loading = false;
};

// 增加表
const addTableSubmitted = async (e: any) => {
	await handleQueryTable();
	state.tableName = e;
	await handleQueryColumn();
};

// 表查询操作
const handleQueryTable = async () => {
	state.tableName = '';
	state.columnData = [];
	xGrid.value?.loadData([]);
	if (!state.configId) return;

	options.loading = true;
	const res = await getAPI(SysDatabaseApi).apiSysDatabaseTableListConfigIdGet(state.configId);
	const tableData = res.data.result ?? [];
	// 排除 zero_ 开头的系统表
	state.tableData = tableData.filter((element: any) => !element.name.startsWith('zero_'));
	options.loading = false;
};

// 统一的选库 / 选表校验：返回 false 表示未选择（已经提示过），调用方直接 return
const requireDb = (): boolean => {
	if (state.configId) return true;
	ElMessage.warning('请先选择库名');
	return false;
};
const requireTable = (): boolean => {
	if (!requireDb()) return false;
	if (state.tableName) return true;
	ElMessage.warning('请先选择表名');
	return false;
};

// 打开表编辑页面
const showEditTable = () => {
	if (!requireTable()) return;
	const hit = state.tableData.find((u: any) => u.name == state.tableName);
	editTableRef.value?.openDialog({
		configId: state.configId,
		tableName: state.tableName,
		oldTableName: state.tableName,
		description: hit?.description,
	});
};

// 打开实体生成页面
const showGenDialog = () => {
	if (!requireTable()) return;
	genEntityRef.value?.openDialog({
		configId: state.configId,
		tableName: state.tableName,
		position: state.appNamespaces[0],
	});
};

// 生成种子数据页面
const showGenSeedDataDialog = () => {
	if (!requireTable()) return;
	genSeedDataRef.value?.openDialog({
		configId: state.configId,
		tableName: state.tableName,
		position: state.appNamespaces[0],
	});
};

// 打开表增加页面
const handleAddTable = () => {
	if (!requireDb()) return;
	addTableRef.value?.openDialog({
		configId: state.configId,
		tableName: '',
		oldTableName: '',
		description: '',
	});
};

// 打开列编辑页面
const showEditColumn = (row: any) => {
	editColumnRef.value?.openDialog({
		configId: state.configId,
		tableName: row.tableName ?? state.tableName,
		columnName: row.dbColumnName,
		oldColumnName: row.dbColumnName,
		description: row.columnDescription,
		defaultValue: row.defaultValue,
	});
};

// 打开列增加页面
const showAddColumn = () => {
	if (!requireTable()) return;
	const addRow: DbColumnInput = {
		configId: state.configId,
		tableName: state.tableName,
		columnDescription: '',
		dataType: 'varchar',
		dbColumnName: '',
		decimalDigits: 0,
		isIdentity: 0,
		isNullable: 1,
		isPrimarykey: 0,
		length: 64,
	};
	addColumnRef.value?.openDialog(addRow);
};

// 删除表
const handleDeleteTable = () => {
	if (!requireTable()) return;
	ElMessageBox.confirm(`将删除表 ${state.tableName} 及其全部数据，且不可恢复。`, '删除数据表', {
		confirmButtonText: '确定删除',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			const deleteDbTableInput: DeleteDbTableInput = {
				configId: state.configId,
				tableName: state.tableName,
			};
			await getAPI(SysDatabaseApi).apiSysDatabaseDeleteTablePost(deleteDbTableInput);
			await handleQueryTable();
			ElMessage.success('表删除成功');
		})
		.catch(() => {});
};

// 删除列
const handleDeleteColumn = (row: any) => {
	ElMessageBox.confirm(`将删除列 ${row.dbColumnName} 及其数据，且不可恢复。`, '删除字段', {
		confirmButtonText: '确定删除',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			const deleteDbColumnInput: DeleteDbColumnInput = {
				configId: state.configId,
				tableName: state.tableName,
				dbColumnName: row.dbColumnName,
			};
			await getAPI(SysDatabaseApi).apiSysDatabaseDeleteColumnPost(deleteDbColumnInput);
			await handleQueryColumn();
			ElMessage.success('列删除成功');
		})
		.catch(() => {});
};

// 初始化库表结构及种子数据
const handleInitTableAndSeedData = () => {
	if (!requireDb()) return;
	initTableAndSeedDataRef.value?.openDialog(state.configId);
};

// 备份当前所选库：后端按 configId 取连接，这里把库名和类型放进确认文案，避免备错库
const handleBackup = () => {
	if (!requireDb()) return;
	const dbName = curDb.value?.dbName ?? state.configId;
	const dbTypeName = curDb.value?.dbTypeName ?? '未知类型';

	ElMessageBox.confirm(`将为库「${dbName}（${state.configId}）」生成一份 ${dbTypeName} 备份，大库可能耗时较久。`, '备份数据库', {
		confirmButtonText: '开始备份',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			state.backing = true;
			try {
				const res = await getAPI(SysDbBackupApi).apiSysDbBackupAddPost(state.configId, { timeout: 0 /** 备份大库可能很久，不设超时 */ });
				const file = res.data.result;
				ElMessage.success(file?.fileName ? `备份完成：${file.fileName}（${formatFileSize(file.size)}）` : '备份完成');
			} finally {
				state.backing = false;
			}
		})
		// 取消确认会 reject，这里必须接住，否则控制台抛未捕获的 Promise 异常
		.catch(() => {});
};

// 上移下移列顺序
const moveColumn = (row: any, direction: 'up' | 'down') => {
	const list = state.columnData;
	const currentIndex = list.findIndex((item) => item.dbColumnName === row.dbColumnName);

	// 边界检查与反馈
	if (currentIndex < 0) return;
	if (direction === 'up' && currentIndex === 0) {
		ElMessage.warning('已处于首位，无法上移');
		return;
	}
	if (direction === 'down' && currentIndex === list.length - 1) {
		ElMessage.warning('已处于末位，无法下移');
		return;
	}

	// 计算目标位置
	const targetIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
	const targetColumn = list[targetIndex];
	const columnName = direction === 'up' ? targetColumn.dbColumnName : row.dbColumnName;
	const afterColumnName = direction === 'up' ? row.dbColumnName : targetColumn.dbColumnName;

	ElMessageBox.confirm(`确定将列：${row.dbColumnName}${direction === 'up' ? '上移' : '下移'}?`, '调整字段顺序', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			try {
				const moveParams: MoveDbColumnInput = {
					configId: state.configId,
					tableName: state.tableName,
					columnName,
					afterColumnName,
				};
				await getAPI(SysDatabaseApi).apiSysDatabaseMoveColumnPost(moveParams);
				await handleQueryColumn();
				ElMessage.success('列位置已更新');
			} catch (error: any) {
				ElMessage.error(`操作失败: ${error.message || '未知错误'}`);
			}
		})
		.catch(() => {});
};

// 表格事件
const gridEvents: VxeGridListeners<DbColumnOutput> = {
	// 双击行事件
	cellDblclick({ row }) {
		showEditColumn(row);
	},
};

// 可视化表
const visualTable = () => {
	if (!requireDb()) return;
	router.push(`/develop/database/visual?configId=${state.configId}`);
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

/* 查询卡：不参与压缩，否则表格会把上面的块挤掉 */
.db-filter {
	flex-shrink: 0;
	margin-top: 14px;

	:deep(.el-card__body) {
		padding: 14px 18px 4px;
	}
}

.db-head {
	display: flex;
	align-items: center;
	gap: 10px;
}
.db-head-ico {
	flex: none;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}
.db-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.db-head-sub {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.db-head-acts {
	flex: none;
	display: flex;
	gap: 6px;
}

/* 查询表单：flex 平铺，不用 el-row/el-col，避免栅格把控件拉得忽宽忽窄 */
.db-form {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 0;
	}
}
/* 库类型标签：紧跟库名下拉，一眼确认要备的是哪种库 */
.db-type-tag {
	align-self: center;
	font-family: ui-monospace, Menlo, Consolas, monospace;
}

.db-select-db {
	width: 240px;
}
.db-select-tb {
	width: 320px;
}

/* 表格容器：吃掉剩余高度 */
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

/* 工具栏分组之间的间隔 */
.db-tb-gap {
	margin-left: 12px;
}
/* 备份按钮被 el-tooltip 包了一层，兄弟相邻选择器命中不到，自己补间距 */
.db-tb-btn {
	margin-left: 12px;
}
.db-cur {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

/* 字段名：主键用钥匙图标区分 */
.db-col {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}
.db-col-ico {
	font-size: 13px;
	color: var(--el-text-color-placeholder);

	&.is-pk {
		color: var(--el-color-warning);
	}
}
.db-col-name {
	font-family: ui-monospace, Menlo, Consolas, monospace;
}

/* 字段类型标签：只做浅底 + 同色文字，不用实心色块 */
.db-type {
	display: inline-block;
	padding: 1px 8px;
	border-radius: 999px;
	font-size: 12px;
	font-family: ui-monospace, Menlo, Consolas, monospace;
	border: 1px solid color-mix(in srgb, var(--tint) 35%, transparent);
	color: var(--tint);
	background: color-mix(in srgb, var(--tint) 10%, transparent);

	&.is-str {
		--tint: #3b82f6;
	}
	&.is-num {
		--tint: #10b981;
	}
	&.is-time {
		--tint: #8b5cf6;
	}
	&.is-bool {
		--tint: #f59e0b;
	}
	&.is-other {
		--tint: #94a3b8;
	}
}

/* 空态：不用 el-empty（插画太大像裂图） */
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
