<template>
	<div class="vt-page">
		<div class="vt-bar">
			<span class="vt-bar-ico"><el-icon><ele-View /></el-icon></span>
			<span class="vt-bar-text">库表关系图</span>
			<span class="vt-bar-sub">表结构取自系统实体，所选库只决定字段命名风格（下划线开关）</span>
			<span class="vt-bar-stat" v-if="state.tableCount">{{ state.tableCount }} 张表 · {{ state.lineCount }} 条关系</span>
		</div>

		<div class="vt-graph" v-loading="state.loading">
			<RelationGraph ref="graphRef" :options="graphOptions">
				<template #graph-plug>
					<div class="vt-legend">
						<span class="vt-legend-k">图例</span>
						<span class="vt-legend-i">
							一对多
							<i class="vt-legend-bar is-many"></i>
						</span>
						<span class="vt-legend-i">
							一对一
							<i class="vt-legend-bar is-one"></i>
						</span>
					</div>
				</template>

				<template #node="{ node }">
					<div class="vt-node">
						<div class="vt-node-hd">
							<span class="vt-node-name">{{ node.text }}</span>
							<span class="vt-node-count">{{ node.data?.columns?.length ?? 0 }} 列</span>
						</div>
						<table class="vt-node-tb">
							<tbody>
								<tr>
									<th>列名</th>
									<th>类型</th>
									<th>描述</th>
								</tr>
								<tr v-for="column of node.data?.columns ?? []" :key="column.columnName">
									<td>
										<div :id="`${node.id}-${column.columnName}`" class="vt-node-col">{{ column.columnName }}</div>
									</td>
									<td>{{ column.dataType }}</td>
									<td>{{ column.columnDescription }}</td>
								</tr>
							</tbody>
						</table>
					</div>
				</template>
			</RelationGraph>
		</div>
	</div>
</template>

<script lang="ts" setup name="databaseVisual">
import { onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';

import RelationGraph from 'relation-graph/vue3';
import type { RGOptions, RGJsonData, RelationGraphComponent } from 'relation-graph/vue3';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';

const state = reactive({
	loading: false,
	tableCount: 0,
	lineCount: 0,
});

// 主页面带 configId 跳进来，字段命名风格跟着所选库走
const configId = (useRoute().query.configId as string) ?? '';

const graphRef = ref<RelationGraphComponent | null>(null);
const graphOptions: RGOptions = {
	defaultJunctionPoint: 'border',
};

onMounted(() => {
	showGraph();
});

// 获取可视化表和字段
const showGraph = async () => {
	state.loading = true;
	try {
		const res = await getAPI(SysDatabaseApi).apiSysDatabaseVisualDbTableGet(configId);
		const visualTableList: any[] = res.data.result?.visualTableList ?? [];
		const visualColumnList: any[] = res.data.result?.visualColumnList ?? [];
		const columnRelationList: any[] = res.data.result?.columnRelationList ?? [];

		const graphNodes = visualTableList.map((table: any) => {
			const { tableName, tableComents, x, y } = table;
			return {
				id: tableName,
				text: tableComents || tableName,
				x,
				y,
				nodeShape: 1,
				data: {
					columns: visualColumnList.filter((col: any) => col.tableName === table.tableName),
				},
			};
		});
		const graphLines = columnRelationList.map((relation: any) => {
			return {
				from: relation.sourceTableName + '-' + relation.sourceColumnName, // HtmlElement id
				to: relation.targetTableName + '-' + relation.targetColumnName, // HtmlElement id
				color: relation.type === 'ONE_TO_ONE' ? 'rgba(239,68,68,0.55)' : 'rgba(16,185,129,0.55)',
				text: '',
				fromJunctionPoint: 'left',
				toJunctionPoint: 'lr',
				lineShape: 6,
				lineWidth: 3,
			};
		});

		state.tableCount = graphNodes.length;
		state.lineCount = graphLines.length;

		const graphJsonData: RGJsonData = {
			nodes: graphNodes,
			lines: [],
			elementLines: graphLines,
		};
		const graphInstance = graphRef.value?.getInstance();
		if (graphInstance) {
			await graphInstance.setJsonData(graphJsonData);
			await graphInstance.moveToCenter();
			await graphInstance.zoomToFit();
		}
	} finally {
		state.loading = false;
	}
};
</script>

<style lang="scss" scoped>
.vt-page {
	position: relative;
	display: flex;
	flex-direction: column;
	height: 100%;
}

.vt-bar {
	flex: none;
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
}
.vt-bar-ico {
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
.vt-bar-text {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.vt-bar-sub {
	flex: 1 1 auto;
	min-width: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.vt-bar-stat {
	flex: none;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

.vt-graph {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 12px;
	border-radius: 12px;
	overflow: hidden;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);

	:deep(.relation-graph) {
		.rel-node-shape-1 {
			overflow: hidden;
			border-radius: 10px;
		}
	}
	:deep(.rel-easy-view) {
		background-color: var(--el-bg-color) !important;
	}
}

/* 图例：原来是一大坨内联 style，抽成类 */
.vt-legend {
	z-index: 300;
	position: absolute;
	left: 12px;
	bottom: 12px;
	display: flex;
	align-items: center;
	gap: 14px;
	padding: 8px 14px;
	border-radius: 10px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color-overlay);
	box-shadow: 0 4px 14px -6px rgba(0, 0, 0, 0.2);
}
.vt-legend-k {
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.vt-legend-i {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}
.vt-legend-bar {
	display: inline-block;
	width: 46px;
	height: 5px;
	border-radius: 999px;

	&.is-many {
		background: rgba(16, 185, 129, 0.55);
	}
	&.is-one {
		background: rgba(239, 68, 68, 0.55);
	}
}

/* 表节点 */
.vt-node {
	width: 300px;
	border-radius: 10px;
	overflow: hidden;
	background: var(--el-bg-color);
	box-shadow: 0 6px 18px -8px rgba(0, 0, 0, 0.3);
}
.vt-node-hd {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	height: 30px;
	padding: 0 10px;
	color: var(--el-color-white);
	background: linear-gradient(135deg, color-mix(in srgb, var(--el-color-primary), #000000 30%) 0%, var(--el-color-primary) 100%);
}
.vt-node-name {
	font-size: 13px;
	font-weight: 600;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.vt-node-count {
	flex: none;
	padding: 1px 7px;
	border-radius: 999px;
	font-size: 11px;
	background: rgba(255, 255, 255, 0.22);
}
.vt-node-tb {
	width: 100%;
	border-collapse: collapse;
	background: var(--el-bg-color);
}
.vt-node-tb th,
.vt-node-tb td {
	padding: 4px 8px;
	font-size: 12px;
	text-align: left;
	border: 1px solid var(--el-border-color-lighter);
	color: var(--el-text-color-primary);
}
.vt-node-tb th {
	background: var(--el-fill-color-lighter);
	font-weight: 600;
	white-space: nowrap;
}
.vt-node-col {
	padding: 1px 6px;
	border-radius: 5px;
	font-family: ui-monospace, Menlo, Consolas, monospace;
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}
</style>
