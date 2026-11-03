<template>
	<el-dialog v-model="visible" class="worm-print__dialog" draggable overflow destroy-on-close fullscreen>
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
				<span> {{ title }} </span>
			</div>
		</template>
		<!-- 设计器容器 -->
		<div class="worm-print">
			<!-- ① 设计器画布 -->
			<PrintDesigner ref="designerRef" :initial-template="templateData" :fields="fields" :is-edit="true" :show-help="true" @preview="onPreview" @save="onSave" />

			<Teleport to="body">
				<div v-if="previewVisible" class="preview-mask">
					<div class="preview-panel">
						<div class="preview-head">
							<span class="preview-title">打印预览</span>
							<span class="preview-subtitle" v-if="previewPages > 0">{{ previewPages }} 页</span>
							<div class="preview-actions">
								<button type="button" class="preview-btn" @click="printPreview">打印</button>
								<button type="button" class="preview-btn ghost" @click="previewVisible = false">关闭</button>
							</div>
						</div>
						<PrintHtmlPreview ref="htmlPreviewRef" class="preview-body" :template-json="previewJson" :print-data="printData" :base-url="baseUrl" @rendered="(n: number) => (previewPages = n)" />
					</div>
				</div>
			</Teleport>
		</div>
	</el-dialog>
</template>

<script setup lang="ts">
// defineProps / defineEmits / defineExpose 是编译器宏，不需要（也不能）从 vue 导入
import { ref, computed, watch, type PropType } from 'vue';
import { PrintDesigner, PrintHtmlPreview, createDefaultTemplate } from '@worm-vue3-print/canvas';
import type { PrintBusinessField, TemplateData } from '@worm-vue3-print/core/designer';

// ============ 2. 外部数据：由父组件通过 prop 传进来 ============
const props = defineProps({
	/** 外部传入的打印数据，结构由调用方决定 */
	data: {
		type: Object as () => any,
		default: () => ({}),
	},
	/** 已有模板 JSON（编辑时由父组件传入）；不传则生成空白默认模板 */
	template: {
		type: [String, Object] as PropType<string | Record<string, any>>,
		default: '',
	},
	/** 弹窗标题 */
	title: {
		type: String,
		default: '打印模板设计',
	},
	/** 左侧「素材台」可绑定字段目录；由父组件传入，默认空数组 */
	//   fieldKey   ：字段标识，对应数据里的 key（模板用 {name} 这种占位符绑定它）
	//   fieldLabel ：设计器里显示的中文名字（只是给人看的，不是数据本身）
	//   fieldType  ：字段类型；list 表示「表格数据源」
	//   sortOrder  ：在素材台里从上到下的排列顺序
	fields: {
		type: Array as () => PrintBusinessField[],
		default: () => [],
	},
});

// 默认模板：若父组件传入了已有模板（编辑场景），用它初始化；否则空白默认模板
const normalizeTemplate = (t: string | Record<string, any>): TemplateData => {
	if (!t) return createDefaultTemplate();
	const obj = typeof t === 'string' ? JSON.parse(t) : t;
	return obj as TemplateData;
};
const templateData = ref<TemplateData>(normalizeTemplate(props.template));

// 监听父组件传入的 props：模板（编辑场景）变化时重新初始化，避免内部状态失效；
// fields / data 变化时打印出来便于排查（data 本身已由 printData 响应式同步）
watch(
	() => [props.template, props.fields, props.data],
	([template, fields, data]) => {
		if (template) templateData.value = normalizeTemplate(template);
	},
	{ immediate: true }
);

/**
 * transformData：把父组件传进来的数据「原样透传」给模板。
 * 这里不写死任何字段名——因为模板是固定的，数据 key 只要和模板占位符一致就行，
 * 所以直接把数据原样交给模板去填充即可（数组里的每一行也保持原样拷贝）。
 */
const transformData = (raw: Record<string, any>) => {
	const result: Record<string, any> = {};
	for (const [key, value] of Object.entries(raw ?? {})) {
		if (Array.isArray(value)) {
			result[key] = value.map((row) => (row && typeof row === 'object' ? { ...row } : row));
		} else {
			// 普通字段：直接保留
			result[key] = value;
		}
	}
	return result;
};

// 预览去渲染的数据（父组件 data 一变，这里自动跟着变）
const printData = computed(() => transformData(props.data));

// ============ 3. 预览弹窗：用 PrintHtmlPreview 把「模板 + 数据」渲染出来 ============
const htmlPreviewRef = ref<InstanceType<typeof PrintHtmlPreview> | null>(null);
const previewVisible = ref(false); // 预览弹窗是否显示

// ============ 弹窗自身显隐：由父组件通过 ref 控制 ============
const visible = ref(false); // 设计器弹窗是否显示
const open = (opts?: { template?: string | Record<string, any> }) => {
	// 每次打开都用传入（或最新）的 template 重新初始化设计器
	templateData.value = normalizeTemplate(opts?.template ?? props.template);
	visible.value = true;
}; 
const close = () => {
	visible.value = false;
}; 
const previewJson = ref<Record<string, any> | null>(null); // 当前要预览的模板 JSON
const previewPages = ref(0); // 渲染出来的总页数
const baseUrl = import.meta.env.VITE_API_URL || '';
const designerRef = ref<any>(null); // 设计器实例，用来取当前画好的模板

// 点「预览」时触发：把设计器里当前的模板取出来，打开预览弹窗
const onPreview = () => {
	const json = designerRef.value?.getTemplateJson?.();
	if (!json) return;
	previewJson.value = json as unknown as Record<string, any>;
	previewVisible.value = true;
};

// 预览弹窗里的「打印」按钮：调用 PrintHtmlPreview 自带的 iframe 打印（不新建浏览器窗口）
const printPreview = () => {
	// iframe 是独立文档，外层样式对它无效；打印前把表格边框修正样式注入 iframe 文档，
	// 否则 Chrome 打印时 border-collapse: collapse 的边框会丢失
	const preview = htmlPreviewRef.value as any;
	const root = preview?.$el ?? preview;
	const iframe: HTMLIFrameElement | undefined = root?.querySelector?.('iframe');
	if (iframe?.contentDocument?.head) {
		const doc = iframe.contentDocument;
		if (!doc.getElementById('worm-print-border-fix')) {
			const style = doc.createElement('style');
			style.id = 'worm-print-border-fix';
			style.textContent = `@media print {
  .print-table { border-collapse: separate !important; border-spacing: 0 !important; }
  .print-table td:not(:first-child), .print-table th:not(:first-child) { border-left: none !important; }
  .print-table tr:not(:first-child) > * { border-top: none !important; }
}`;
			doc.head.appendChild(style);
		}
	}
	preview?.print();
};

// ============ 4. 保存：把设计好的模板以 JSON 字符串交给父组件 ============
const emit = defineEmits<{
	save: [payload: { template: string }];
}>();

const onSave = (json: string) => {
	emit('save', { template: json }); // 父组件拿到后可落库或继续处理
};

// 把「取模板 JSON」、「当前数据」以及弹窗显隐暴露给父组件（如 editPrint）按需调用
defineExpose({
	open,
	close,
	getTemplateJson: () => designerRef.value?.getTemplateJson?.(),
	printData,
});
</script>

<style scoped>
.worm-print {
	position: relative;
	height: 100%;
}

.preview-trigger {
	margin: 8px 0;
}

.worm-print__header-title {
	display: flex;
	align-items: center;
	gap: 8px;
	color: #fff;
	font-size: 16px;
	font-weight: 600;
}

/* footer 里的工具栏：左页数、右打印按钮 */
.preview-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	width: 100%;
}
</style>

<style>
.el-dialog.print-preview__dialog {
	height: 80vh;
	display: flex;
	flex-direction: column;
	margin: 5vh auto 0;
}

.el-dialog.print-preview__dialog .el-dialog__body.print-preview__body {
	flex: 1;
	min-height: 0;
	overflow: auto;
	padding-bottom: 0;
}

.el-dialog.print-preview__dialog .el-dialog__footer {
	padding-top: 0;
}

/* 设计器弹窗（wormPrint 自身）：让 body 撑满，设计器占满高度 */
.el-dialog.worm-print__dialog {
	display: flex;
	flex-direction: column;
}
.el-overlay .el-overlay-dialog .worm-print__dialog .el-dialog__body {
	flex: 1;
	min-height: 0;
	padding: 0 !important;
	overflow: hidden;
}
.help-modal-overlay {
	z-index: 3000 !important;
}

/* ── 预览弹层（全屏，便于看清整页细节） ── */
.preview-mask {
	position: fixed;
	inset: 0;
	background: #fff;
	z-index: 9999;
}
.preview-panel {
	width: 100vw;
	height: 100vh;
	background: #fff;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}
.preview-head {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px 14px;
	border-bottom: 1px solid #e9ecf2;
	flex-shrink: 0;
}
/* PrintHtmlPreview 根节点：占满头部之外的剩余高度，内部 iframe 自行滚动 */
.preview-body {
	flex: 1;
	min-height: 0;
}
.preview-title {
	font-size: 14px;
	font-weight: 600;
	color: #2a2e37;
}
.preview-subtitle {
	font-size: 12px;
	color: #8b909c;
}
.preview-actions {
	margin-left: auto;
	display: flex;
	gap: 8px;
}
.preview-btn {
	padding: 5px 16px;
	border: none;
	border-radius: 6px;
	background: #165dff;
	color: #fff;
	font-size: 13px;
	cursor: pointer;
}
.preview-btn.ghost {
	background: #f4f6fa;
	color: #2a2e37;
	border: 1px solid #d9dde6;
}

.help-modal-overlay {
	z-index: 3000 !important;
}
</style>
