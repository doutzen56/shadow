<template>
	<div style="height: 100%; overflow: hidden">
		<div class="widgets-toolbar">
			<div class="widgets-toolbar-title">我的工作台</div>
			<el-button v-if="customizing" type="primary" round plain icon="ele-Check" @click="save">保存布局</el-button>
			<el-button v-else type="primary" round plain icon="ele-Edit" @click="custom">编辑布局</el-button>
		</div>

		<div :class="['widgets-home', customizing ? 'customizing' : '']" ref="main">
			<div class="widgets-content">
				<!-- <div class="widgets-top">
				<div class="widgets-top-title">控制台</div>
				<div class="widgets-top-actions">
					<el-button v-if="customizing" type="primary" icon="ele-Check" round @click="save">完成</el-button>
					<el-button v-else type="primary" icon="ele-Edit" round @click="custom">自定义</el-button>
				</div>
			</div> -->
				<div class="widgets" ref="widgetsRef">
					<div class="widgets-wrapper">
						<div v-if="nowCompsList.length <= 0" class="no-widgets">
							<el-empty description="没有部件啦" :image-size="300"></el-empty>
						</div>
						<el-row :gutter="10">
							<el-col v-for="(item, index) in grid.layout" :key="index" :md="item" :xs="24">
								<VueDraggable v-model="grid.copmsList[index]" :animation="200" group="grid" handle=".customize-overlay" class="draggable-box">
									<div v-for="item in grid.copmsList[index]" :key="item">
										<div class="widgets-item">
											<component :is="allComps[item]"></component>
											<div v-if="customizing" class="customize-overlay">
												<el-button class="close" type="danger" plain icon="ele-Close" @click="remove(item)"></el-button>
												<label v-if="allComps[item]">
													<el-icon> <component :is="allComps[item].icon" /> </el-icon>{{ allComps[item].title }}
												</label>
											</div>
										</div>
									</div>
								</VueDraggable>
							</el-col>
						</el-row>
					</div>
				</div>
			</div>

			<div v-if="customizing" class="widgets-aside">
				<div class="widgets-top">
					<div class="widgets-aside-title">
						<el-icon><ele-CirclePlusFilled /></el-icon>添加部件
					</div>
					<div class="widgets-top-actions">
						<div class="widgets-aside-close" @click="close">
							<el-icon><ele-Close /></el-icon>
						</div>
					</div>
				</div>
				<el-container>
					<el-header style="height: auto">
						<div class="selectLayout">
							<div class="selectLayout-item item01" :class="{ active: grid.layout.join(',') === '12,6,6' }" @click="setLayout([12, 6, 6])">
								<el-row :gutter="2">
									<el-col :span="12"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
								</el-row>
							</div>
							<div class="selectLayout-item item02" :class="{ active: grid.layout.join(',') === '24,16,8' }" @click="setLayout([24, 16, 8])">
								<el-row :gutter="2">
									<el-col :span="24"><span></span></el-col>
									<el-col :span="16"><span></span></el-col>
									<el-col :span="8"><span></span></el-col>
								</el-row>
							</div>
							<div class="selectLayout-item item03" :class="{ active: grid.layout.join(',') === '24' }" @click="setLayout([24])">
								<el-row :gutter="2">
									<el-col :span="24"><span></span></el-col>
									<el-col :span="24"><span></span></el-col>
									<el-col :span="24"><span></span></el-col>
								</el-row>
							</div>
							<div class="selectLayout-item item01" :class="{ active: grid.layout.join(',') === '6,12,6' }" @click="setLayout([6, 12, 6])">
								<el-row :gutter="2">
									<el-col :span="6"><span></span></el-col>
									<el-col :span="12"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
								</el-row>
							</div>
							<div class="selectLayout-item item02" :class="{ active: grid.layout.join(',') === '24,6,12,6' }" @click="setLayout([24, 6, 12, 6])">
								<el-row :gutter="2">
									<el-col :span="24"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
									<el-col :span="12"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
								</el-row>
							</div>
							<div class="selectLayout-item item05" :class="{ active: grid.layout.join(',') === '24,6,12,6,24' }" @click="setLayout([24, 6, 12, 6, 24])">
								<el-row :gutter="2">
									<el-col :span="24"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
									<el-col :span="12"><span></span></el-col>
									<el-col :span="6"><span></span></el-col>
									<el-col :span="24"><span></span></el-col>
								</el-row>
							</div>
						</div>
					</el-header>
					<el-main class="nopadding">
						<div class="widgets-list">
							<div v-if="myCompsList.length <= 0" class="widgets-list-nodata">
								<el-empty description="没有部件啦" :image-size="60"></el-empty>
							</div>
							<div v-for="item in myCompsList" :key="item.title" class="widgets-list-item">
								<div class="item-logo">
									<el-icon>
										<component :is="item.icon" />
									</el-icon>
								</div>
								<div class="item-info">
									<h2>{{ item.title }}</h2>
									<p>{{ item.description }}</p>
								</div>
								<div class="item-actions">
									<el-button type="primary" icon="ele-Plus" @click="push(item)"></el-button>
								</div>
							</div>
						</div>
					</el-main>
					<el-footer style="height: 51px">
						<el-button @click="backDefault">恢复默认</el-button>
					</el-footer>
				</el-container>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="home">
import { ref, computed, onMounted, nextTick } from 'vue';
import { VueDraggable } from 'vue-draggable-plus';
import { clone } from '/@/utils/arrayOperation';
import { Local } from '/@/utils/storage';

import allComps from './components/index';

interface Grid {
	layout: number[];
	copmsList: string[][];
}
const defaultGrid = {
	layout: [12, 6, 6],
	copmsList: [
		['welcome', 'myapp', 'oplogChart', 'vislogChart', 'commit'],
		['statsData', 'schedule', 'oplogList', 'vislogMap'],
		['profile', 'notice', 'task', 'orgTree', 'vislogList'],
	],
};

const customizing = ref<boolean>(false);
const allCompsList = ref(allComps);
const widgetsRef = ref<HTMLElement | null>(null);
const grid = ref<Grid>(clone(defaultGrid));

onMounted(() => {
	const savedGrid = Local.get('grid');
	if (savedGrid) {
		grid.value = savedGrid;
	}
});
// 关键：用 import.meta.glob 扫描文件名，不 eager 导入内容
const modules = import.meta.glob('./components/*.vue');

// 从路径中提取文件名
const fileNames = Object.keys(modules).map((path) => path.replace('./components/', '').replace('.vue', ''));
const availableCompsList = computed(() => {
	const compsList = [];
	for (const key in allCompsList.value) {
		const comp = allCompsList.value[key];
		compsList.push({
			key,
			title: comp.title,
			icon: comp.icon,
			description: comp.description,
		});
	}
	const activeComps = grid.value.copmsList.flat();
	return compsList.map((comp) => ({
		...comp,
		disabled: activeComps.includes(comp.key),
	}));
});

const myCompsList = computed(() => {
	return availableCompsList.value.filter((comp) => !comp.disabled && fileNames.includes(comp.key));
});

const nowCompsList = computed(() => grid.value.copmsList.flat());

// 开启自定义
const custom = () => {
	customizing.value = true;
	const oldWidth = widgetsRef.value?.offsetWidth || 0;
	nextTick(() => {
		if (widgetsRef.value) {
			const scale = widgetsRef.value.offsetWidth / oldWidth;
			widgetsRef.value.style.setProperty('transform', `scale(${scale})`);
		}
	});
};

// 设置布局
const setLayout = (layout: number[]) => {
	grid.value.layout = layout;
	const diff = grid.value.layout.length - grid.value.copmsList.length;
	if (diff < 0) {
		grid.value.copmsList = [...grid.value.copmsList.slice(0, grid.value.layout.length - 1), grid.value.copmsList.slice(grid.value.layout.length - 1).flat()];
	} else if (diff > 0) {
		grid.value.copmsList = grid.value.copmsList.concat(Array.from({ length: diff }, () => []));
	}
};

// 追加
const push = (item: any) => {
	grid.value.copmsList[0].push(item.key);
};

// 隐藏组件
const remove = (item: string) => {
	grid.value.copmsList = grid.value.copmsList.map((list) => list.filter((comp) => comp !== item));
};

// 保存
const save = () => {
	customizing.value = false;
	widgetsRef.value?.style.removeProperty('transform');
	Local.set('grid', grid.value);
};

// 恢复默认
const backDefault = () => {
	customizing.value = false;
	widgetsRef.value?.style.removeProperty('transform');
	grid.value = clone(defaultGrid);
	Local.remove('grid');
};

// 关闭
const close = () => {
	customizing.value = false;
	widgetsRef.value?.style.removeProperty('transform');
	grid.value = Local.get('grid') ? Local.get('grid') : defaultGrid;
};
</script>

<style scoped lang="scss">
.widgets-home {
	display: flex;
	flex-direction: row;
	flex: 1;
	height: 100%;
}
.widgets-content {
	flex: 1;
	overflow: auto;
	overflow-x: hidden;
	/* 左右只留 5px：el-row 的 -5px 外边距 + el-col 的 5px 内边距正好抵消到页边距，
	   再补 padding 就会比其它页面"缩一圈"（实测原 10px 时卡片距边 10px，其它页面仅 5px） */
	padding: 10px 5px;
}

/* 部件之间的垂直间距与横向 gutter 保持一致 */
.widgets-item {
	margin-bottom: 10px;
}
/* ==========================================================
   工作台部件卡「统一外壳」：卡片 / 卡头 / 卡头图标 / 卡体间距
   —— 各部件只负责自己的内容，外壳样式一律收口在这里，
      避免 17 个部件各写一套导致圆角、描边、图标色不统一
   ========================================================== */
/* 欢迎卡自带主色渐变无边框外壳，不参与统一描边 */
.widgets-content :deep(.el-card:not(.welcome-card)) {
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	transition:
		border-color 0.25s ease,
		box-shadow 0.25s ease;

	/* hover 边框/阴影用 Element 色阶而非 color-mix()：
	   可信浏览器内核 Chromium 102 不支持 color-mix，会让整条声明失效 → 悬停时毫无反馈 */
	&:hover {
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 12px 26px -18px var(--el-color-primary-light-5);
	}
}

.widgets-content :deep(.el-card__header) {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	padding: 10px 14px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	border-bottom: 1px solid var(--el-border-color-lighter);
	/* 极淡的主色渐层，让卡头有层次又不抢内容
	   （终点写 --el-bg-color 而不是 transparent：老内核不支持 color-mix 时会露白） */
	background: linear-gradient(180deg, var(--el-color-primary-light-9) 0%, var(--el-bg-color) 100%);
}

/* 卡头图标：深色图标 + 同色系浅底圆角块（与全站装饰规范一致）
   必须用「直接子元素」>：按钮内部的 <el-icon> 同样是它父元素的 :first-child，
   用后代选择器会连按钮图标一起命中，hover 时按钮变实底就把图标冲掉了 */
.widgets-content :deep(.el-card__header > .el-icon:first-child) {
	flex: 0 0 auto;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

/* 卡头被 .card-header 包了一层时（statsData / orgTree 等），图标同样只认直接子元素 */
.widgets-content :deep(.el-card__header .card-header > .el-icon:first-child) {
	flex: 0 0 auto;
	width: 24px;
	height: 24px;
	border-radius: 7px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

/* 卡头内容块撑满宽度（.card-header 是全局类，这里是它在卡头里的表现）
   全局 .card-header 是 space-between（页面表格卡「标题 + 右侧按钮」用的），
   部件卡头用它、却只有「图标 + 标题」两个子元素时，标题会被甩到最右边，故改回靠左 */
.widgets-content :deep(.el-card__header .card-header) {
	flex: 1 1 auto;
	min-width: 0;
	justify-content: flex-start;
	flex-wrap: nowrap;
}

/* 原来各部件用 float:right，卡头改成 flex 后失效，右侧元素统一靠 margin-left:auto 推右 */
.widgets-content :deep(.el-card__header .el-button),
.widgets-content :deep(.el-card__header .el-tag) {
	margin-left: auto;
}

/* 卡头标题：窄列下省略而不是把计数标签挤出卡外 */
.widgets-content :deep(.el-card__header .card-header > span) {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.widgets-content :deep(.el-card__body) {
	padding: 12px 14px;
}

/* 列表型部件（通知 / 登录日志 / 操作记录）卡体不留内边距，
   让行分隔线通到卡片两边 —— 这三个部件统一用 body-style="padding:0" */
.widgets-aside {
	width: 360px;
	background: var(--el-bg-color);
	box-shadow: -1px 0 0 var(--el-border-color-lighter), -8px 0 24px rgb(0 21 41 / 6%);
	position: relative;
	overflow: auto;
	padding-top: 10px;
}
.widgets-aside-title {
	margin-top: 10px;
	margin-left: 10px;
	font-size: 14px;
	display: flex;
	align-items: center;
	justify-content: center;
}
.widgets-aside-title i {
	margin-right: 10px;
	font-size: 18px;
}
.widgets-aside-close {
	font-size: 18px;
	width: 30px;
	height: 30px;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 3px;
	cursor: pointer;
}
.widgets-aside-close:hover {
	background: var(--el-fill-color-light);
}

.widgets-top {
	margin-bottom: 8px;
	display: flex;
	justify-content: space-between;
	align-items: center;
}


.widgets {
	transform-origin: top left;
	transition: transform 0.15s;
}

.draggable-box {
	height: 100%;
}

.customizing .widgets-wrapper {
	margin-right: -360px;
}
.customizing .widgets-wrapper .el-col {
	padding-bottom: 15px;
}
.customizing .widgets-wrapper .draggable-box {
	border: 1px dashed var(--el-color-primary);
	padding: 15px;
}
.customizing .widgets-wrapper .no-widgets {
	display: none;
}
.customizing .widgets-item {
	position: relative;
}

.customize-overlay {
	position: absolute;
	top: 0;
	right: 0;
	bottom: 0;
	left: 0;
	z-index: 1;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	background: rgba(255, 255, 255, 0.9);
	cursor: move;
}
.customize-overlay label {
	background: var(--el-color-primary);
	color: #fff;
	height: 40px;
	padding: 0 30px;
	border-radius: 40px;
	font-size: 18px;
	display: flex;
	align-items: center;
	justify-content: center;
	cursor: move;
}
.customize-overlay label i {
	margin-right: 15px;
	font-size: 24px;
}
.customize-overlay .close {
	position: absolute;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 0;
	width: 30px;
	height: 30px;
	top: 15px;
	right: 15px;
}

.widgets-list-item {
	display: flex;
	flex-direction: row;
	padding: 15px;
	align-items: center;
}
.widgets-list-item .item-logo {
	width: 40px;
	height: 40px;
	border-radius: 50%;
	background: var(--el-fill-color-light);
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 18px;
	margin-right: 15px;
	color: var(--el-color-primary);
}
.widgets-list-item .item-info {
	flex: 1;
}
.widgets-list-item .item-info h2 {
	font-size: 15px;
	font-weight: 600;
	cursor: default;
	color: var(--el-text-color-primary);
}
.widgets-list-item .item-info p {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	cursor: default;
}
.widgets-list-item:hover {
	background: var(--el-fill-color-light);
}

.widgets-wrapper .sortable-ghost {
	opacity: 0.5;
}

.layout-list {
	height: 120px;
}

.selectLayout {
	width: 100%;
	height: auto;
	display: flex;
	flex-wrap: wrap;
}
.selectLayout-item {
	margin: 5px;
	width: 60px;
	height: 60px;
	border: 2px solid var(--el-border-color-light);
	padding: 5px;
	cursor: pointer;
	margin-right: 15px;
}
.selectLayout-item span {
	display: block;
	background: var(--el-border-color-light);
	height: 46px;
}
.selectLayout-item.item02 span {
	height: 30px;
}
.selectLayout-item.item02 .el-col:nth-child(1) span {
	height: 14px;
	margin-bottom: 2px;
}
.selectLayout-item.item03 span {
	height: 14px;
	margin-bottom: 2px;
}
.selectLayout-item.item05 span {
	height: 15px;
}
.selectLayout-item.item05 .el-col:first-child span {
	height: 14px;
	margin-bottom: 2px;
}
.selectLayout-item.item05 .el-col:last-child span {
	height: 14px;
	margin-top: 2px;
}
.selectLayout-item:hover {
	border-color: var(--el-color-primary);
}
.selectLayout-item.active {
	border-color: var(--el-color-primary);
}
.selectLayout-item.active span {
	background: var(--el-color-primary);
}

.dark {
	.widgets-aside {
		background: #2b2b2b;
	}
	.customize-overlay {
		background: rgba(43, 43, 43, 0.9);
	}
}

@media (max-width: 992px) {
	.customizing .widgets {
		transform: scale(1) !important;
	}
	.customizing .widgets-aside {
		width: 100%;
		position: absolute;
		top: 50%;
		right: 0;
		bottom: 0;
	}
	.customizing .widgets-wrapper {
		margin-right: 0;
	}
}
.widgets-toolbar {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 12px;
	/* 左右与卡片左边缘对齐（页边距 5px），标题才不会比卡片缩进一截 */
	padding: 10px 5px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.widgets-toolbar-title {
	position: relative;
	display: inline-flex;
	align-items: center;
	padding-left: 11px;
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-primary);

	/* 左侧主色竖条，与全站 card-header 的标题语言保持一致 */
	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 50%;
		transform: translateY(-50%);
		width: 3px;
		height: 14px;
		border-radius: 3px;
		background: var(--el-color-primary);
	}
}
</style>

<!-- 工作台部件通用空态：el-empty 默认插画在 300px 高的窄卡里既大又灰，
     统一换成「同色系浅底圆角图标 + 一行说明」的克制版本 -->
<style lang="scss">
.w-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	width: 100%;
	height: 100%;
	/* 与有数据时的部件高度对齐，避免空态把卡片压成一条 */
	min-height: var(--next-widget-height, 300px);
	padding: 12px 0;

	.w-empty-ico {
		flex: 0 0 auto;
		width: 34px;
		height: 34px;
		border-radius: 11px;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 17px;
		color: var(--tint, var(--el-color-primary));
		background: var(--el-color-primary-light-9);
	}

	.w-empty-text {
		font-size: 12.5px;
		line-height: 1.5;
		color: var(--el-text-color-placeholder);
	}
}
</style>
