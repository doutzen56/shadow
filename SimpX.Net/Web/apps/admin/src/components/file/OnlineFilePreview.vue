<template>
	<!-- 固定 light：避免 body[data-theme=dark] 使 github-markdown 使用浅色字 + 白底不可读 -->
	<div class="online-file-preview" data-theme="light" v-loading="loading">
		<file-viewer v-if="file && !imagePreview" :file="file" class="online-file-preview__viewer" />
		<file-viewer v-else-if="url && !imagePreview" :url="url" class="online-file-preview__viewer" />
		<div v-else-if="(file || url) && imagePreview" class="online-file-preview__image">
			<el-image :src="imageUrl" fit="contain" :preview-src-list="[imageUrl]" preview-teleported />
		</div>
		<el-empty v-else-if="!loading" description="暂无预览内容" />
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue';
import { FileViewer } from '@file-viewer/vue3';
import '@file-viewer/vue3/dist/file-viewer3.css';

const props = withDefaults(
	defineProps<{
		url?: string;
		file?: File;
		loading?: boolean;
		/** 与系统文件管理一致：图片使用 el-image 大图预览 */
		imagePreview?: boolean;
	}>(),
	{
		url: '',
		loading: false,
		imagePreview: false,
	}
);

const isImage = (nameOrUrl: string) => {
	const path = nameOrUrl.split('?')[0].toLowerCase();
	return /\.(jpg|jpeg|png|gif|bmp|webp|svg|ico)$/i.test(path);
};

const imagePreview = computed(() => {
	if (!props.imagePreview) return false;
	if (props.file?.name) return isImage(props.file.name);
	return !!props.url && isImage(props.url);
});

const imageUrl = computed(() => {
	if (props.file) return URL.createObjectURL(props.file);
	return props.url;
});
</script>

<style lang="scss" scoped>
.online-file-preview {
	width: 100%;
	height: 100%;
	min-height: 200px;
	position: relative;

	&__viewer {
		width: 100%;
		height: 100%;
	}

	&__image {
		width: 100%;
		height: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #000;

		:deep(.el-image) {
			width: 100%;
			height: 100%;
		}

		:deep(.el-image__inner) {
			object-fit: contain;
		}
	}

	/* Markdown: keep github-markdown-css variables in light mode. */
	// :deep(.markdown-body) {
	// 	--fgColor-default: #1f2328;
	// 	--fgColor-muted: #59636e;
	// 	--fgColor-accent: #0969da;
	// 	--fgColor-success: #1a7f37;
	// 	--fgColor-attention: #9a6700;
	// 	--fgColor-danger: #d1242f;
	// 	--fgColor-done: #8250df;
	// 	--bgColor-default: #ffffff;
	// 	--bgColor-muted: #f6f8fa;
	// 	--bgColor-neutral-muted: rgba(175, 184, 193, 0.2);
	// 	--borderColor-default: #d0d7de;
	// 	--borderColor-muted: #d8dee4;
	// 	--borderColor-accent-emphasis: #0969da;
	// 	--borderColor-success-emphasis: #1a7f37;
	// 	--borderColor-attention-emphasis: #bf8700;
	// 	--borderColor-danger-emphasis: #cf222e;
	// 	--borderColor-done-emphasis: #8250df;
	// 	color: var(--fgColor-default);
	// 	background-color: var(--bgColor-default);
	// 	color-scheme: light;
	// }

	// :deep(.markdown-body h1),
	// :deep(.markdown-body h2),
	// :deep(.markdown-body h3),
	// :deep(.markdown-body h4),
	// :deep(.markdown-body p),
	// :deep(.markdown-body li),
	// :deep(.markdown-body td),
	// :deep(.markdown-body th) {
	// 	color: inherit;
	// }

	// :deep(.markdown-body table tr) {
	// 	background-color: var(--bgColor-default);
	// 	border-top-color: var(--borderColor-muted);
	// }

	// :deep(.markdown-body table tr:nth-child(2n)),
	// :deep(.markdown-body table th) {
	// 	background-color: var(--bgColor-muted);
	// }

	// :deep(.markdown-body table th),
	// :deep(.markdown-body table td) {
	// 	border-color: var(--borderColor-default);
	// }

	// :deep(.markdown-body pre),
	// :deep(.markdown-body .highlight pre) {
	// 	color: var(--fgColor-default);
	// 	background-color: var(--bgColor-muted);
	// }

	// :deep(.markdown-body code),
	// :deep(.markdown-body tt) {
	// 	color: var(--fgColor-default);
	// 	background-color: var(--bgColor-neutral-muted);
	// }

	// :deep(.markdown-body pre code),
	// :deep(.markdown-body pre tt) {
	// 	background-color: transparent;
	// }
}
</style>
