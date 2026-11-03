<template>
	<!-- 参考 system/file/index.vue 的预览抽屉：主题色头 + 图标标题 + 类型角标 + 体积 + 右侧全屏；
	     非图片文件走 OnlineFilePreview 直链预览，图片走 el-image-viewer。 -->
	<el-drawer v-model="visible" :size="drawerSize" class="doc-preview-drawer" destroy-on-close @closed="handleClosed">
		<template #header>
			<div class="dialog-title doc-preview-title">
				<el-icon size="16"><ele-Document /></el-icon>
				<span class="doc-preview-name" :title="state.fileName">{{ state.fileName }}</span>
				<span v-if="fileTypeLabel" class="doc-preview-chip">{{ fileTypeLabel }}</span>
				<span v-if="state.fileSize" class="doc-preview-size">{{ state.fileSize }}</span>
			</div>
			<div class="doc-preview-acts">
				<el-tooltip :content="state.fullscreen ? '退出全屏' : '全屏'" placement="bottom">
					<el-button text @click="state.fullscreen = !state.fullscreen">
						<el-icon><component :is="state.fullscreen ? 'ele-Aim' : 'ele-FullScreen'" /></el-icon>
					</el-button>
				</el-tooltip>
			</div>
		</template>
		<OnlineFilePreview v-if="!isImageState && previewUrl" :url="previewUrl" :image-preview="false" :loading="state.loading" class="file-preview-viewer" />
		<el-empty v-else-if="!isImageState && !previewUrl && !state.loading" description="无法获取文档预览地址" />
	</el-drawer>

	<!-- 图片类文档：与文件管理一致，用 el-image-viewer 直接大图预览，不占用抽屉 -->
	<el-image-viewer v-if="isImageState && previewUrl" :url-list="[previewUrl]" :hide-on-click-modal="true" @close="handleImageClose" />
</template>

<script lang="ts" setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import OnlineFilePreview from '/@/components/file/OnlineFilePreview.vue';
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi } from '/@/api-services/document/api';
import { resolveDocumentBrowserFileUrl } from '/@/utils/documentFileUrl';

interface DocPreviewInput {
	id?: number;
	name?: string | null;
	fileUrl?: string | null;
	preUrl?: string | null;
	documentType?: number | null;
	sizeInfo?: string | null;
	sizeKb?: number | null;
}

const visible = ref(false);
const imageViewer = ref(false);
const previewUrl = ref('');
const isImageState = ref(false);

const state = reactive({
	fileName: '',
	fileSuffix: '',
	fileSize: '',
	loading: false,
	/** 预览抽屉是否全屏（100% 宽），窄屏时自动视为全屏 */
	fullscreen: false,
});

// 预览抽屉宽度：窄屏或点过全屏按钮时铺满整屏，否则沿用 60%
const viewportWidth = ref(window.innerWidth);
const drawerSize = computed(() => (viewportWidth.value < 900 || state.fullscreen ? '100%' : '60%'));
const onViewportResize = () => (viewportWidth.value = window.innerWidth);

// 头部文件类型角标（去掉后缀的点、统一大写）
const fileTypeLabel = computed(() => (state.fileSuffix ? state.fileSuffix.replace('.', '').toUpperCase() : ''));

// 从名称解析后缀
const resolveSuffix = (name?: string | null): string => {
	const n = String(name ?? '');
	const m = n.match(/\.[^.]+$/);
	return m ? m[0].toLowerCase() : '';
};

// 是否图片：文档类型=图片(8) 或 名称后缀命中图片扩展名
const isImageDoc = (doc: DocPreviewInput): boolean => {
	if (doc.documentType === 8) return true;
	return /\.(jpg|jpeg|png|gif|bmp|webp|svg|ico)$/i.test(String(doc.name ?? ''));
};

// 打开预览：直接取文档 fileUrl（与 sysFile 的 url 同源，可直链预览），缺失时回退详情接口
const open = async (doc: DocPreviewInput) => {
	state.loading = true;
	state.fullscreen = false;
	isImageState.value = false;
	previewUrl.value = '';
	imageViewer.value = false;

	const suffix = resolveSuffix(doc.name);
	state.fileName = String(doc.name ?? '文档预览');
	state.fileSuffix = suffix;
	state.fileSize = doc.sizeInfo ? String(doc.sizeInfo) : doc.sizeKb ? `${doc.sizeKb} KB` : '';

	try {
		let fileUrl = doc.fileUrl;
		if (!fileUrl && doc.id) {
			const res = await getAPI(DocumentApi).apiDocumentDetailGet(doc.id);
			fileUrl = res.data?.result?.fileUrl || null;
		}
		if (!fileUrl) {
			ElMessage.warning('无法获取文档预览地址');
			return;
		}
		// resolveDocumentBrowserFileUrl：本地相对路径转代理路径、MinIO 外链保持原样
		previewUrl.value = resolveDocumentBrowserFileUrl(fileUrl, doc.preUrl);
		isImageState.value = isImageDoc(doc);

		if (isImageState.value) {
			// 图片走独立大图预览，抽屉不打开
			imageViewer.value = true;
			visible.value = false;
		} else {
			visible.value = true;
			imageViewer.value = false;
		}
	} catch (error: any) {
		console.error('文档预览失败:', error);
		ElMessage.error(error?.response?.data?.message || error?.message || '加载文档预览失败');
	} finally {
		state.loading = false;
	}
};

// 抽屉关闭：释放预览地址
const handleClosed = () => {
	previewUrl.value = '';
	isImageState.value = false;
};

// 大图预览关闭：释放预览地址
const handleImageClose = () => {
	imageViewer.value = false;
	previewUrl.value = '';
};

onMounted(() => window.addEventListener('resize', onViewportResize));
onUnmounted(() => window.removeEventListener('resize', onViewportResize));

defineExpose({
	open,
});
</script>

<!-- 抽屉头部/主体属于 el-drawer 内部节点，scoped 的 :deep 依赖祖先 data-v 才能命中；
     一旦抽屉被传送到 body 就会断链，故统一用独立类名写在非 scoped 块里（与 system/file 同一套写法） -->
<style lang="scss">
.doc-preview-drawer {
	.el-drawer__header {
		/* 主题色头 + 压扁，把高度尽量让给预览区；去掉默认下边距，避免与主体之间出现缝隙 */
		margin-bottom: 0 !important;
		padding: 8px 12px !important;
		background: var(--el-color-primary);
		color: var(--el-color-white);
	}

	.el-drawer__close {
		color: var(--el-color-white);
	}

	.el-drawer__body {
		/* 预览组件自身撑满，抽屉 body 去掉内边距，否则四周会出现一圈白边 */
		display: flex;
		padding: 0;
		min-height: 0;
	}
}

.doc-preview-title {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 15px;
	font-weight: 700;
	letter-spacing: 0.2px;
}

/* 文件名是唯一可伸缩项：先省略文件名，类型角标与体积始终可见 */
.doc-preview-name {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.doc-preview-chip {
	flex-shrink: 0;
	padding: 0 6px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 10px;
	font-weight: 700;
	letter-spacing: 0.5px;
	/* 主题色头上盖一层白膜做角标底：102 不支持 color-mix，这里用 rgb 兜底 */
	background: rgb(255 255 255 / 0.22);
	color: var(--el-color-white);
}

.doc-preview-size {
	flex-shrink: 0;
	font-size: 11px;
	font-weight: 500;
	opacity: 0.85;
}

.doc-preview-acts {
	display: flex;
	flex-shrink: 0;
	gap: 2px;
	margin-left: 6px;

	.el-button {
		padding: 6px;
		// 头部操作键改变量而不是写 color：el-button 自带 color，也不吃祖先色
		--el-button-text-color: var(--el-color-white);
		--el-button-hover-text-color: var(--el-color-white);
	}
}

// 亮色下 is-text 的 hover/active 底是近白实色，压在主题色上就是一块白框；这里换成头部文字色的半透明膜。
// 暗色那一档本就是抬亮膜、压在主题色上仍分得开，所以只覆盖亮色。
// 102 不支持 color-mix：用 @supports 包住，不支持时回落到 Element 默认 hover（仍可用）。
html:not([data-theme='dark']) .doc-preview-acts .el-button {
	@supports (background: color-mix(in srgb, red, blue)) {
		--el-fill-color-light: color-mix(in srgb, var(--el-color-white) 15%, transparent);
		--el-fill-color: color-mix(in srgb, var(--el-color-white) 25%, transparent);
	}
}
</style>
