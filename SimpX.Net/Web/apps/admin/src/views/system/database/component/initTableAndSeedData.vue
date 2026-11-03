<template>
	<div class="sys-initTableAndSeedData-container">
		<vxe-modal v-model="state.visible" title="初始化库表结构及种子数据" fullscreen show-zoom resize width="100vw" height="100vh">
			<!-- 窗口偏窄时两栏挤在一起看不清，改成上下堆叠 -->
			<splitpanes class="default-theme its-panes" :class="{ 'is-stack': state.stack }" :horizontal="state.stack">
				<pane>
					<vxe-grid ref="xGridEntity" class="xGrid-style" v-bind="optionsEntity" v-on="gridEventsEntity">
						<template #toolbar_buttons>
							<el-button type="warning" icon="ele-Refresh" :loading="optionsEntity.loading" @click="handleInitTable">
								初始化表结构<span v-if="state.entitySelectedRows.length > 0">({{ state.entitySelectedRows.length }})</span>
							</el-button>
							<span class="its-hint">勾选实体后按库表结构建表 / 改表</span>
						</template>
						<template #row_columnCount="{ row }">
							<el-tag type="primary"> {{ row.columnCount }} </el-tag>
						</template>
						<template #empty>
							<div class="its-empty">
								<span class="its-empty-ico"><el-icon><ele-Memo /></el-icon></span>
								<p class="its-empty-text">没有读到实体，确认该库对应的程序集已编译</p>
							</div>
						</template>
					</vxe-grid>
				</pane>

				<pane>
					<vxe-grid ref="xGridSeed" class="xGrid-style" v-bind="optionsSeed" v-on="gridEventsSeed">
						<template #toolbar_buttons>
							<el-button type="warning" icon="ele-Refresh" :loading="optionsSeed.loading" @click="handleInitSeedData">
								初始化种子数据<span v-if="state.seedSelectedRows.length > 0">({{ state.seedSelectedRows.length }})</span>
							</el-button>
							<span class="its-hint">按勾选的种子写入初始数据</span>
						</template>
						<template #row_count="{ row }">
							<el-tag type="primary"> {{ row.count }} </el-tag>
						</template>
						<template #empty>
							<div class="its-empty">
								<span class="its-empty-ico"><el-icon><ele-Files /></el-icon></span>
								<p class="its-empty-text">没有读到种子数据</p>
							</div>
						</template>
					</vxe-grid>
				</pane>
			</splitpanes>
		</vxe-modal>
	</div>
</template>

<script lang="tsx" setup name="sysInitTableAndSeedData">
import { onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import { getAPI } from '/@/utils/axios-utils';
import { SysCodeGenApi, SysDatabaseApi } from '/@/api-services/system/api';

const xGridEntity = ref<VxeGridInstance>();
const xGridSeed = ref<VxeGridInstance>();

const state = reactive({
	visible: false,
	configId: '',
	entitySelectedRows: [] as any[],
	seedSelectedRows: [] as any[],
	// 窗口尺寸不够时把左右分栏改成上下分栏
	stack: false,
});

// 媒体查询查不到窗口高度，只能靠 JS 判断
const syncStack = () => {
	state.stack = window.innerWidth < 1280 || window.innerHeight < 760;
};

/** 表尾只显示一行合计，避免空表尾占高度 */
const footerMethod = (label: string): VxeGridPropTypes.FooterMethod => {
	return ({ columns, data }) => [columns.map((_, index) => (index === 1 ? `合计 ${data.length} ${label}` : ''))];
};

// 表格参数配置-实体数据
const optionsEntity = useVxeTable(
	{
		id: 'entity',
		name: '实体数据',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ type: 'seq', title: '序号', width: 50, fixed: 'left' },
			{ field: 'entityName', title: '实体名称', showOverflow: 'tooltip', align: 'left' },
			{ field: 'tableName', title: '库表名称', showOverflow: 'tooltip', align: 'left' },
			{ field: 'tableComment', title: '描述', showOverflow: 'tooltip', align: 'left' },
			{ field: 'columnCount', title: '字段个数', showOverflow: 'tooltip', slots: { default: 'row_columnCount' } },
			{ field: 'assemblyName', title: '所属程序集', showOverflow: 'tooltip', align: 'left' },
		],
	},
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => getEntityQueryApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
		// 多选配置
		checkboxConfig: { range: true, highlight: false },
		// 表尾配置
		showFooter: true,
		footerMethod: footerMethod('个实体'),
	}
);

// 查询所有实体列表
const getEntityQueryApi = () => getAPI(SysCodeGenApi).apiSysCodeGenTableListConfigIdGet(state.configId);

// 表格事件-实体数据
const gridEventsEntity: VxeGridListeners = {
	// 多选事件
	checkboxChange({ records }) {
		state.entitySelectedRows = records;
	},
	// 全选/全不选事件
	checkboxAll({ records }) {
		state.entitySelectedRows = records;
	},
};

// 初始化表结构
const handleInitTable = async () => {
	if (!state.configId) {
		ElMessage.warning('请先选择库名');
		return;
	}
	if (!state.entitySelectedRows.length) {
		ElMessage.warning('请至少选择一个实体');
		return;
	}

	optionsEntity.loading = true;
	try {
		await getAPI(SysDatabaseApi).apiSysDatabaseInitTablePost({
			configId: state.configId,
			entityNames: state.entitySelectedRows.map((row) => row.entityName),
		});
		ElMessage.success('初始化表结构操作成功');
	} catch {
		ElMessage.error('初始化表结构操作失败');
	} finally {
		optionsEntity.loading = false;
	}
};

// 表格参数配置-种子数据
const optionsSeed = useVxeTable(
	{
		id: 'seed',
		name: '种子数据',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ type: 'seq', title: '序号', width: 50, fixed: 'left' },
			{ field: 'name', title: '种子名称', showOverflow: 'tooltip', align: 'left' },
			{ field: 'description', title: '描述', showOverflow: 'tooltip', align: 'left' },
			{ field: 'order', title: '执行顺序', showOverflow: 'tooltip' },
			{ field: 'count', title: '种子个数', showOverflow: 'tooltip', slots: { default: 'row_count' } },
			{ field: 'assemblyName', title: '所属程序集', showOverflow: 'tooltip', align: 'left' },
		],
	},
	{
		// 代理配置
		proxyConfig: { autoLoad: true, ajax: { query: () => getSeedQueryApi() } },
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
		// 多选配置
		checkboxConfig: { range: true, highlight: false },
		// 表尾配置
		showFooter: true,
		footerMethod: footerMethod('个种子'),
	}
);

// 查询所有种子列表
const getSeedQueryApi = () => getAPI(SysDatabaseApi).apiSysDatabaseSeedDataListGet(state.configId);

// 表格事件-种子数据
const gridEventsSeed: VxeGridListeners = {
	// 多选事件
	checkboxChange({ records }) {
		state.seedSelectedRows = records;
	},
	// 全选/全不选事件
	checkboxAll({ records }) {
		state.seedSelectedRows = records;
	},
};

// 生成种子数据操作
const handleInitSeedData = async () => {
	if (!state.configId) {
		ElMessage.warning('请先选择库名');
		return;
	}
	if (!state.seedSelectedRows.length) {
		ElMessage.warning('请至少选择一个种子');
		return;
	}

	optionsSeed.loading = true;
	try {
		await getAPI(SysDatabaseApi).apiSysDatabaseInitSeedDataPost({
			configId: state.configId,
			seedNames: state.seedSelectedRows.map((row) => ({
				name: row.name,
				assemblyName: row.assemblyName,
				order: row.order,
			})),
		});
		ElMessage.success('生成种子数据操作成功');
	} catch {
		ElMessage.error('生成种子数据操作失败');
	} finally {
		optionsSeed.loading = false;
	}
};

// 打开弹窗
const openDialog = (configId: any) => {
	state.configId = configId;
	state.entitySelectedRows = [];
	state.seedSelectedRows = [];
	state.visible = true;
};

onMounted(() => {
	syncStack();
	window.addEventListener('resize', syncStack);
});
onBeforeUnmount(() => window.removeEventListener('resize', syncStack));

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
/* splitpanes 的 pane 自带 height:100%，但外层必须给出确定高度，
   否则全屏弹窗里两栏会塌成 0 高；窗口矮时改成上下堆叠 */
.its-panes {
	height: calc(100vh - 130px);
	min-height: 320px;

	:deep(.splitpanes__pane) {
		min-width: 0;
		background: transparent;
	}

	&.is-stack :deep(.splitpanes__pane) {
		height: auto !important;
	}
}

.its-hint {
	margin-left: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.its-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 10px;
	padding: 26px 0;
}
.its-empty-ico {
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
.its-empty-text {
	margin: 0;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}
</style>
