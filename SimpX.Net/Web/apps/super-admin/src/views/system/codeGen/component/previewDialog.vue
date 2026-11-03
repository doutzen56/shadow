<template>
	<div class="sys-codeGenPreview-container">
		<el-dialog
			v-model="state.isShowDialog"
			:draggable="!state.max"
			:close-on-click-modal="false"
			:show-close="false"
			:width="state.max ? '100vw' : '96vw'"
			:top="state.max ? '0' : '2vh'"
			:fullscreen="state.max"
			:class="['pd-dialog', { 'is-max': state.max }]"
		>
			<template #header>
				<div class="pd-hd">
					<span class="pd-hd-ico"><el-icon><ele-Document /></el-icon></span>
					<div class="pd-hd-txt">
						<span class="pd-hd-title">{{ props.title }}</span>
						<span class="pd-hd-sub">按文件逐个查看生成结果，确认无误后再生成到项目里</span>
					</div>
					<span v-if="state.options.length" class="pd-hd-stat">共 {{ state.options.length }} 个文件</span>
					<!-- 关闭按钮自己做，才能和全屏按钮在同一行对齐（EP 原生关闭按钮是绝对定位的） -->
					<div class="pd-hd-ops">
						<el-button class="pd-hd-btn" text :title="state.max ? '退出全屏' : '全屏查看'" @click="toggleMax">
							<SvgIcon :name="state.max ? 'iconfont icon-tuichuquanping' : 'iconfont icon-fullscreen'" />
							<span>{{ state.max ? '退出全屏' : '全屏' }}</span>
						</el-button>
					<el-button class="pd-hd-btn is-close" text title="关闭" @click="cancel">
						<el-icon><ele-Close /></el-icon>
					</el-button>
					</div>
				</div>
			</template>
			<div class="pd-body">
				<div :class="`${kindOf(state.current)}-style`">
					<el-segmented v-model="state.current" :options="state.options" block @change="handleChange">
						<template #default="{ item }">
							<div class="pd4">
								<SvgIcon :name="item.icon" class="mb4" />
								<div>{{ item.label }}</div>
							</div>
						</template>
					</el-segmented>
				</div>
				<div class="pd-file">
					<el-icon class="pd-file-ico"><ele-FolderOpened /></el-icon>
					<span class="pd-file-name">{{ state.current }}</span>
					<span class="pd-file-tag">{{ fileSide }}</span>
					<el-button class="pd-file-copy" text type="primary" icon="ele-CopyDocument" @click="handleCopy">复制当前文件</el-button>
				</div>
				<div ref="monacoEditorRef" class="pd-editor"></div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">关闭</el-button>
					<el-button icon="ele-CopyDocument" type="primary" @click="handleCopy">复制</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<!-- 代码预览 -->
<script lang="ts" setup name="sysPreviewCode">
import { reactive, ref, nextTick, computed } from 'vue';
import * as monaco from 'monaco-editor';
import { getMonacoTheme } from '/@/utils/theme';
import EditorWorker from 'monaco-editor/editor/editor.worker?worker'
import commonFunction from '/@/utils/commonFunction';

import { getAPI } from '/@/utils/axios-utils';
import { SysCodeGenApi } from '/@/api-services/system/api';

const { copyText } = commonFunction();

const props = defineProps({
	title: String,
});
const monacoEditorRef = ref();
const state = reactive({
	isShowDialog: false,
	max: false,
	options: [] as any, // 分段器的选项
	current: '', // 选中的分段
	codes: [] as any, // 预览的代码
});

// 模板文件在真实扩展名后还挂了一层 .vm（如 xxx.cs.vm / index.vue.vm），判断前先剥掉，
// 否则 .cs.vm 既不会被认成 C#，还会被当成前端文件
const bareName = (fileName?: string): string => (fileName ?? '').replace(/\.vm$/i, '');

// 文件归类：cs / vue / 其它，用于分段器配色与图标
const kindOf = (fileName?: string): string => {
	const name = bareName(fileName);
	if (/\.cs$/i.test(name)) return 'cs';
	if (/\.vue$/i.test(name)) return 'vue';
	return 'js';
};

// 当前文件属于前端还是后端（.cs / .cs.vm / .cshtml / .json 都算后端）
const fileSide = computed(() => (/\.(cs|cshtml|json)$/i.test(bareName(state.current)) ? '后端' : '前端'));

// 按扩展名选择高亮语言，否则 vue / ts 也会按 C# 着色
const languageOf = (fileName: string): string => {
	const name = bareName(fileName);
	if (/\.cs$/i.test(name)) return 'csharp';
	if (/\.vue$/i.test(name)) return 'html';
	if (/\.(ts|js)$/i.test(name)) return 'typescript';
	return 'plaintext';
};

// 防止 monaco 报黄
self.MonacoEnvironment = {
	getWorker: (_: string, label: string) => new EditorWorker(),
};

// 初始化monacoEditor对象
var monacoEditor: any = null;
const initMonacoEditor = () => {
	monacoEditor = monaco.editor.create(monacoEditorRef.value, {
		theme: getMonacoTheme(), // vs / vs-dark 由主题决定
		value: '', // 默认显示的值
		language: 'csharp',
		formatOnPaste: true,
		wordWrap: 'on', // 自动换行，注意大小写
		wrappingIndent: 'indent',
		folding: true, // 是否折叠
		foldingHighlight: true, // 折叠等高线
		foldingStrategy: 'indentation', // 折叠方式  auto | indentation
		showFoldingControls: 'always', // 是否一直显示折叠 always | mouSEOver
		disableLayerHinting: true, // 等宽优化
		emptySelectionClipboard: false, // 空选择剪切板
		selectionClipboard: false, // 选择剪切板
		automaticLayout: true, // 自动布局
		codeLens: false, // 代码镜头
		scrollBeyondLastLine: false, // 滚动完最后一行后再滚动一屏幕
		colorDecorators: true, // 颜色装饰器
		accessibilitySupport: 'auto', // 辅助功能支持  "auto" | "off" | "on"
		lineNumbers: 'on', // 行号 取值： "on" | "off" | "relative" | "interval" | function
		lineNumbersMinChars: 5, // 行号最小字符   number
		//enableSplitViewResizing: false,
		readOnly: true, // 预览只读，避免误改后以为改到了生成结果
	});
};

// 打开弹窗
const openDialog = async (row: any) => {
	state.isShowDialog = true;
	const { data } = await getAPI(SysCodeGenApi).apiSysCodeGenPreviewPost({ id: row.id });
	state.codes = data.result ?? [];
	state.options = Object.keys(data.result as any).map((fileName: string) => ({
		value: fileName,
		// 分段器上只显示文件名，完整路径放在下面的说明条里
		label: fileName.split('/').pop() ?? fileName,
		icon: kindOf(fileName) === 'cs' ? 'fa fa-hashtag' : kindOf(fileName) === 'vue' ? 'fa fa-vimeo' : 'fa fa-file-code-o',
	}));
	state.current = state.options?.[0]?.value ?? '';
	if (!monacoEditor) initMonacoEditor();
	// 防止取不到
	nextTick(() => {
		monacoEditor.setValue(state.codes[state.current]);
		monaco.editor.setModelLanguage(monacoEditor.getModel(), languageOf(state.current));
	});
};

// 分段器改变时切换代码
const handleChange = (current: any) => {
	monacoEditor.setValue(state.codes[current]);
	monaco.editor.setModelLanguage(monacoEditor.getModel(), languageOf(current));
};

// 全屏切换：走 EP 原生 fullscreen（会自动加 .is-fullscreen），
// 项目主题把 .el-dialog 强制成 position:absolute，所以下面再用 is-max 兜住尺寸
const toggleMax = () => {
	state.max = !state.max;
	// 尺寸变化后要让 monaco 重新布局，否则编辑器会停在旧的宽高上
	nextTick(() => setTimeout(() => monacoEditor?.layout?.(), 60));
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 复制代码
const handleCopy = () => {
	copyText(state.codes[state.current]);
};

// 导出对象
defineExpose({ openDialog });
</script>

<style scoped>
.cs-style .el-segmented {
	--el-segmented-item-selected-bg-color: #5c2d91;
	--el-border-radius-base: 6px;
}
.vue-style .el-segmented {
	--el-segmented-item-selected-bg-color: #42b883;
	--el-border-radius-base: 6px;
}
.js-style .el-segmented {
	--el-segmented-item-selected-bg-color: #e44d26;
	--el-border-radius-base: 6px;
}
.pd-file {
	display: flex;
	align-items: center;
	gap: 8px;
	flex: none;
	margin-top: 10px;
	padding: 8px 12px;
	border-radius: 8px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
}
.pd-file-ico {
	font-size: 15px;
	color: var(--el-text-color-secondary);
}
.pd-file-name {
	font-size: 12.5px;
	font-family: ui-monospace, Menlo, Consolas, monospace;
	color: var(--el-text-color-regular);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.pd-file-tag {
	flex: none;
	padding: 2px 8px;
	border-radius: 999px;
	font-size: 11.5px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}
.pd-file-copy {
	margin-left: auto;
}
.pd-editor {
	width: 100%;
	/* 高度交给父级 .pd-body 的 flex，避免写死 vh 计算值 */
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 8px;
	border-radius: 8px;
	overflow: hidden;
}
</style>

<style lang="scss">
/* 弹窗头部：全局 .el-dialog__header 是主题色底 + 白字，这里只接管内边距 */
.pd-dialog .el-dialog__header {
	padding: 0;
}
/* 主题给 .el-dialog__body 加了 !important 的 padding，这里只接管高度与滚动 */
.pd-dialog .el-dialog__body {
	overflow: hidden;
}
.pd-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 13px 16px;
}
.pd-hd-ico {
	flex: none;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	background: color-mix(in srgb, currentColor 18%, transparent);
}
.pd-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.pd-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.pd-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.pd-hd-stat {
	flex: none;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	background: color-mix(in srgb, currentColor 18%, transparent);
}
/* 全屏 + 关闭并排，行内垂直居中，天然对齐 */
.pd-hd-ops {
	flex: none;
	display: flex;
	align-items: center;
	gap: 6px;
}
.pd-hd-btn {
	height: 30px;
	padding: 0 10px;
	border-radius: 8px;
	font-size: 12.5px;
	// 按钮文字色由组件自己读的两个变量决定，改变量比写 color 更能压住 .el-button.is-text:hover
	--el-button-text-color: var(--el-color-white);
	--el-button-hover-text-color: var(--el-color-white);
	background: color-mix(in srgb, currentColor 16%, transparent);
	border: 1px solid color-mix(in srgb, currentColor 28%, transparent);

	&:hover {
		background: color-mix(in srgb, currentColor 28%, transparent);
		border-color: color-mix(in srgb, currentColor 45%, transparent);
	}
}
/* 全屏按钮：hover 走「压暗 + 白字白图标」。
   EP 的 .el-button.is-text:not(.is-disabled):hover 会把背景刷成 --el-fill-color-light（近白），
   白字压在白底上直接看不见，所以要提权 + !important 把背景压回深色 */
.pd-dialog .pd-hd-btn:not(.is-close) {
	&:hover,
	&:focus {
		color: var(--el-color-white) !important;
		background: rgba(0, 0, 0, 0.18) !important;
		border-color: color-mix(in srgb, var(--el-color-white) 50%, transparent) !important;
	}
}
/* 关闭按钮走危险色：全屏按钮保持压暗，关闭 hover 整块变红，两者一眼能分开。
   选择器带 .pd-dialog 提权，避免被 element-plus 的 .el-button.is-text:hover 白底盖掉 */
.pd-dialog .pd-hd-btn.is-close {
	.el-icon {
		transition: transform 0.25s ease;
	}

	&:hover,
	&:focus {
		color: #fff !important;
		background: var(--el-color-danger) !important;
		border-color: var(--el-color-danger) !important;
		box-shadow: 0 2px 8px -2px color-mix(in srgb, var(--el-color-danger) 60%, transparent);

		.el-icon {
			transform: scale(1.15);
		}
	}
}
/* 正文：常态给固定高度让编辑器 flex 撑满；全屏时交给弹窗本身的高度 */
.pd-body {
	display: flex;
	flex-direction: column;
	/* 高度按「视口 - 头部60 - 正文内边距36 - 页脚62 - 顶部留白2vh」算出，
	   取 200px 留足余量：整弹窗不超出一屏，也不会触发 body 滚动 */
	height: calc(100vh - 200px);
	min-height: 300px;
}
/* 全屏：EP 原生 .is-fullscreen 只改了 width/height，
   项目主题又把它强制成 position:absolute + margin:0 auto，所以这里把尺寸彻底接管。
   选择器把「class 挂在 overlay 上」和「挂在 dialog 自身上」两种情况都覆盖 */
.pd-dialog .el-dialog.is-fullscreen,
.pd-dialog .el-dialog.is-max,
.el-dialog.pd-dialog.is-fullscreen,
.el-dialog.pd-dialog.is-max {
	position: fixed;
	inset: 0;
	width: 100vw !important;
	max-width: 100vw !important;
	height: 100vh !important;
	max-height: 100vh !important;
	margin: 0 !important;
	border-radius: 0;
	display: flex;
	flex-direction: column;
}
.pd-dialog .el-dialog.is-fullscreen .el-dialog__body,
.pd-dialog .el-dialog.is-max .el-dialog__body {
	flex: 1 1 auto;
	min-height: 0;
	max-height: none !important;
	display: flex;
	flex-direction: column;
}
.pd-dialog .el-dialog.is-fullscreen .pd-body,
.pd-dialog .el-dialog.is-max .pd-body {
	flex: 1 1 auto;
	min-height: 0;
	height: auto;
}
</style>
