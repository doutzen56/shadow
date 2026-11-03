<template>
	<div class="panel-control">
		<el-tooltip content="撤销（Ctrl+Z）" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-DArrowLeft" :disabled="state.undoDisable" @click="$_undo" />
		</el-tooltip>
		<el-tooltip content="恢复（Ctrl+Y）" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-DArrowRight" :disabled="state.redoDisable" @click="$_redo" />
		</el-tooltip>

		<span class="pc-divider"></span>

		<el-tooltip content="放大" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-ZoomIn" @click="$_zoomIn" />
		</el-tooltip>
		<el-tooltip content="缩小" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-ZoomOut" @click="$_zoomOut" />
		</el-tooltip>
		<el-tooltip content="大小适应" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-FullScreen" @click="$_zoomReset" />
		</el-tooltip>
		<el-tooltip content="定位还原" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-Aim" @click="$_translateRest" />
		</el-tooltip>

		<span class="pc-divider"></span>

		<el-tooltip content="下载图片" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-Download" @click="$_download" />
		</el-tooltip>
		<el-tooltip content="查看图数据" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-View" @click="$_catData" />
		</el-tooltip>
		<el-tooltip :content="miniMapVisible ? '隐藏缩略图' : '查看缩略图'" placement="bottom" :show-after="300">
			<el-button text circle size="small" icon="ele-Picture" :class="{ 'is-active': miniMapVisible }" @click="$_showMiniMap" />
		</el-tooltip>
	</div>
</template>

<script setup lang="ts">
import { reactive, ref, watch, onBeforeUnmount } from 'vue';

var props = defineProps({
	lf: Object,
});
const emit = defineEmits(['catData']);

const state = reactive({
	undoDisable: true,
	redoDisable: true,
});

/** 同步 history 状态到按钮禁用态 */
const syncHistoryState = () => {
	const lf = props.lf;
	if (!lf?.history) return;
	state.undoDisable = !lf.history.undoAble();
	state.redoDisable = !lf.history.redoAble();
};

// 当 lf 实例就绪后绑定 history:change 事件
let stopWatch: ReturnType<typeof watch> | undefined;
stopWatch = watch(
	() => props.lf,
	(lf) => {
		if (!lf) return;
		lf.on('history:change', syncHistoryState);
		syncHistoryState(); // 立即同步一次
		stopWatch?.();
	},
	{ immediate: true }
);

onBeforeUnmount(() => {
	props.lf?.off('history:change', syncHistoryState);
});

const $_zoomIn = () => {
	props.lf?.zoom(true);
};

const $_zoomOut = () => {
	props.lf?.zoom(false);
};

const $_zoomReset = () => {
	props.lf?.resetZoom();
};

const $_translateRest = () => {
	props.lf?.resetTranslate();
};

const $_undo = () => {
	props.lf?.undo();
};

const $_redo = () => {
	props.lf?.redo();
};

const $_download = () => {
	props.lf?.getSnapshot();
};

const $_catData = () => {
	emit('catData');
};

const miniMapVisible = ref(false);

const $_showMiniMap = () => {
	const miniMap = props.lf?.extension.miniMap;
	if (!miniMap) return;
	if (miniMapVisible.value) {
		miniMap.hide();
		miniMapVisible.value = false;
	} else {
		miniMap.show(props.lf?.graphModel.width - 218, 70);
		miniMapVisible.value = true;
	}
};
</script>

<style lang="scss" scoped>
/* 悬浮工具条：轻玻璃质感，压在画布上方，不遮挡节点操作区 */
.panel-control {
	position: absolute;
	top: 12px;
	left: 12px;
	z-index: 5;
	display: flex;
	align-items: center;
	gap: 2px;
	padding: 4px 6px;
	border-radius: 12px;
	background: color-mix(in srgb, var(--el-bg-color) 88%, transparent);
	backdrop-filter: blur(10px);
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 10px 28px -20px rgba(0, 0, 0, 0.35);
	white-space: nowrap;

	.el-button {
		margin-left: 0;
		color: var(--el-text-color-regular);

		&:hover:not(.is-disabled) {
			color: var(--el-color-primary);
			background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
		}

		&.is-active {
			color: var(--el-color-primary);
			background: color-mix(in srgb, var(--el-color-primary) 14%, transparent);
		}
	}
}

.pc-divider {
	width: 1px;
	height: 16px;
	margin: 0 4px;
	background: var(--el-border-color-lighter);
}
</style>
