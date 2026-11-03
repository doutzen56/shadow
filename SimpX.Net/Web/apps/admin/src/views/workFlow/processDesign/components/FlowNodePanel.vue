<template>
	<div class="flow-node-panel">
		<div class="panel-head">
			<span class="panel-head__title">节点</span>
			<span class="panel-head__hint">点击或拖入画布</span>
		</div>
		<div class="panel-list">
			<div class="panel-group" v-for="group in nodeGroups" :key="group.name">
				<div class="group-title">{{ group.name }}</div>
				<div class="node-list">
					<div
						v-for="item in group.items"
						:key="item.type"
						class="node-item"
						:class="item.type"
						:title="item.desc"
						@mousedown.stop.prevent="(e) => startDrag(e, item.type)"
					>
						<div class="node-icon-wrapper" :class="item.type">
							<component :is="item.icon" class="node-icon-svg" />
						</div>
						<div class="node-text">
							<div class="node-label">{{ item.label }}</div>
							<div class="node-desc">{{ item.desc }}</div>
						</div>
						<el-icon class="node-drag"><ele-Rank /></el-icon>
					</div>
				</div>
			</div>
		</div>
		<div class="panel-foot">
			<div class="panel-shortcut"><kbd>Ctrl</kbd> + <kbd>Z</kbd> 撤销</div>
			<div class="panel-shortcut"><kbd>Delete</kbd> 删除选中</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { h } from 'vue';

const props = defineProps<{
	lf?: any;
}>();

// 开始节点图标
const StartIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('circle', {
				cx: '12',
				cy: '12',
				r: '10',
				fill: '#12a182',
				stroke: '#12a182',
			}),
			h('path', {
				d: 'M9 6L19 12L9 18Z',
				fill: '#ffffff', // 白色填充，与绿色背景形成对比
				stroke: 'none', // 无边框，纯色填充
			}),
		]
	);
};

// 结束节点图标
const EndIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('circle', {
				cx: '12',
				cy: '12',
				r: '10',
				fill: '#f56c6c',
				stroke: '#f56c6c',
			}),
			h('rect', {
				x: '7',
				y: '7',
				width: '10',
				height: '10',
				rx: '1',
				fill: '#fff',
			}),
		]
	);
};

// 审批节点图标
const UserIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('path', {
				d: 'M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z',
				fill: 'var(--el-color-primary-light-1)',
			}),
		]
	);
};

// 任务节点图标
const TaskIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('rect', {
				x: '4',
				y: '6',
				width: '16',
				height: '12',
				rx: '2',
				fill: '#e6a23c',
			}),
			h('path', {
				d: 'M8 10h8M8 14h6',
				stroke: '#fff',
				'stroke-width': '1.5',
				'stroke-linecap': 'round',
			}),
		]
	);
};

// 条件网关图标
const GatewayIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('polygon', {
				points: '12,2 22,12 12,22 2,12',
				fill: '#fcb70a',
			}),
			h(
				'text',
				{
					x: '12',
					y: '16',
					'text-anchor': 'middle',
					'font-size': '12',
					'font-weight': 'bold',
					fill: '#fff',
				},
				'?'
			),
		]
	);
};

// 并行网关图标
const ParallelIcon = () => {
	return h(
		'svg',
		{
			viewBox: '0 0 24 24',
			width: '20',
			height: '20',
			fill: 'currentColor',
		},
		[
			h('polygon', {
				points: '12,2 22,12 12,22 2,12',
				fill: '#12a182',
			}),
			h('rect', {
				x: '10',
				y: '6',
				width: '4',
				height: '12',
				fill: '#fff',
			}),
			h('rect', {
				x: '6',
				y: '10',
				width: '12',
				height: '4',
				fill: '#fff',
			}),
		]
	);
};

const nodeGroups = [
	{
		name: '基础',
		items: [
			{ label: '开始', type: 'start-node', desc: '流程开始节点', icon: StartIcon },
			{ label: '结束', type: 'end-node', desc: '流程结束节点', icon: EndIcon },
		],
	},
	{
		name: '审批节点',
		items: [{ label: '审批节点', type: 'user-node', desc: '人工审批节点（支持多种审批模式）', icon: UserIcon }],
	},
	{
		name: '控制节点',
		items: [
			{ label: '条件网关', type: 'gateway-node', desc: '根据条件分支流转', icon: GatewayIcon },
			{ label: '并行网关', type: 'parallel-node', desc: '同时执行多个分支', icon: ParallelIcon },
		],
	},
];

const startDrag = (e: MouseEvent, type: string) => {
	e.stopPropagation();
	e.preventDefault();
	if (!props.lf) return;
	props.lf.dnd.startDrag({
		type,
	});
};
</script>


<style scoped lang="scss">
/* 节点库：固定在画布左侧的竖向面板（与智能体编排器一致的左栏布局） */
.flow-node-panel {
	display: flex;
	flex-direction: column;
	width: 208px;
	flex-shrink: 0;
	min-height: 0;
	border-right: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
	overflow: hidden;
}

.panel-head {
	display: flex;
	align-items: baseline;
	justify-content: space-between;
	padding: 14px 12px 8px;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.panel-head__hint {
	font-size: 11px;
	font-weight: 400;
	color: var(--el-text-color-placeholder);
}

.panel-list {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 0 12px 10px;
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	scrollbar-width: thin;

	&::-webkit-scrollbar {
		width: 6px;
	}

	&::-webkit-scrollbar-thumb {
		background: var(--el-border-color);
		border-radius: 3px;
	}
}

.panel-group {
	display: flex;
	flex-direction: column;
	gap: 6px;

	& + & {
		margin-top: 6px;
	}
}

.group-title {
	padding: 2px 2px 0;
	font-size: 11px;
	font-weight: 600;
	letter-spacing: 0.3px;
	color: var(--el-text-color-placeholder);
}

.node-list {
	display: flex;
	flex-direction: column;
	gap: 6px;
}

.node-item {
	display: flex;
	align-items: center;
	gap: 9px;
	padding: 8px 9px;
	border-radius: 9px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-blank);
	cursor: grab;
	user-select: none;
	transition:
		border-color 0.2s ease,
		background-color 0.2s ease,
		box-shadow 0.2s ease,
		transform 0.2s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--nc, var(--el-color-primary)) 55%, transparent);
		background: color-mix(in srgb, var(--nc, var(--el-color-primary)) 5%, var(--el-fill-color-blank));
		box-shadow: 0 4px 12px -6px color-mix(in srgb, var(--nc, var(--el-color-primary)) 60%, transparent);
		transform: translateY(-1px);

		.node-drag {
			opacity: 1;
		}
	}

	&:active {
		cursor: grabbing;
		transform: translateY(0);
	}
}

.node-icon-wrapper {
	display: grid;
	place-items: center;
	flex-shrink: 0;
	width: 30px;
	height: 30px;
	border-radius: 8px;
	background: color-mix(in srgb, var(--nc, var(--el-color-primary)) 10%, transparent);
	border: 1px solid color-mix(in srgb, var(--nc, var(--el-color-primary)) 22%, transparent);

	.node-icon-svg {
		width: 18px;
		height: 18px;
	}
}

/* 节点类型 → 语义色：变量上移到 item，hover 与图标盒共用同一个 --nc
   取值与画布上各节点的主色保持一致，做到"面板图标 = 画布节点"的视觉对应 */
.node-item.start-node {
	--nc: #14b8a6;
}

.node-item.end-node {
	--nc: #f04438;
}

.node-item.user-node {
	--nc: #4b6bf7;
}

.node-item.task-node {
	--nc: #f79009;
}

.node-item.gateway-node {
	--nc: #f79009;
}

.node-item.parallel-node {
	--nc: #7a5af8;
}

.node-text {
	flex: 1;
	min-width: 0;
}

.node-label {
	font-size: 12.5px;
	font-weight: 600;
	line-height: 17px;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.node-desc {
	margin-top: 1px;
	font-size: 11px;
	line-height: 15px;
	color: var(--el-text-color-secondary);
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}

.node-drag {
	flex-shrink: 0;
	font-size: 13px;
	color: var(--el-text-color-placeholder);
	opacity: 0;
	transition: opacity 0.2s ease;
}

.panel-foot {
	margin-top: auto;
	padding: 10px 12px 12px;
	border-top: 1px dashed var(--el-border-color-lighter);
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.panel-shortcut {
	font-size: 11px;
	color: var(--el-text-color-placeholder);

	kbd {
		padding: 0 4px;
		border-radius: 3px;
		border: 1px solid var(--el-border-color);
		background: var(--el-fill-color-light);
		font-family: monospace;
		font-size: 10px;
	}
}
</style>
