<template>
	<div v-if="flags.any" class="jf-extras">
		<div v-if="flags.selectUser" class="jf-form-item">
			<label class="jf-form-label">指定下一节点处理人</label>
			<JfUserPicker
				:model-value="nextOperators"
				:task-id="taskId"
				scene="nextOperator"
				:disabled="disabled"
				placeholder="搜索并选择处理人"
				@update:model-value="emit('update:nextOperators', $event)"
			/>
		</div>
		<div v-if="flags.cc" class="jf-form-item">
			<label class="jf-form-label">抄送给</label>
			<JfUserPicker :model-value="ccActors" :task-id="taskId" scene="cc" :disabled="disabled" placeholder="搜索并选择抄送人" @update:model-value="emit('update:ccActors', $event)" />
		</div>
		<div v-if="flags.reason" class="jf-form-item">
			<label class="jf-form-label">申请理由</label>
			<textarea class="jf-input" rows="3" :disabled="disabled" :value="applyReason" placeholder="请输入申请理由" @input="emit('update:applyReason', ($event.target as HTMLTextAreaElement).value)" />
		</div>
		<div v-if="flags.attachment" class="jf-form-item">
			<label class="jf-form-label">附件</label>
			<input v-if="!disabled" class="jf-input" type="file" @change="onFile" />
			<!-- 已上传附件：对齐文件管理页 fm-card 布局（缩略图 + 后缀角标 + 名称/大小/上传者/MD5/来源 + 时间），预览/下载常驻可见，无编辑删除 -->
			<div v-if="attCard.visible" class="jf-att-card">
				<div class="jf-att-thumb">
					<img v-if="attCard.isImage" class="jf-att-cover" :src="attCard.thumbUrl" :alt="attCard.name" loading="lazy" decoding="async" @error="onThumbError" />
					<div v-else class="jf-att-thumb-icon"><FileIcons :name="attCard.suffixFull" :width="54" :height="54" /></div>
					<span class="jf-att-suffix">{{ attCard.suffixLabel }}</span>
					<div v-if="attCard.isObject" class="jf-att-thumb-mask">
						<el-button circle icon="ele-View" title="预览" @click.stop="openPreview" />
						<el-button circle icon="ele-Download" title="下载" @click.stop="handleDownload" />
					</div>
				</div>
				<div class="jf-att-body">
					<h3 class="jf-att-name" :title="attCard.name">{{ attCard.name }}</h3>
					<div class="jf-att-meta">
						<span class="jf-att-size" :class="`is-${attCard.tagType}`">{{ attCard.sizeLabel }}</span>
						<span v-if="attCard.uploader" class="jf-att-user" :title="`上传者：${attCard.uploader}`">
							<el-icon><ele-UserFilled /></el-icon>
							<span class="jf-att-user-name">{{ attCard.uploader }}</span>
						</span>
					</div>
				</div>
				<footer v-if="attCard.time" class="jf-att-foot">
					<span class="jf-att-time">
						<el-icon><ele-Clock /></el-icon>
						{{ attCard.time }}
					</span>
				</footer>
			</div>
		</div>

		<!-- 附件预览：图片用 el-image-viewer，其它文件用在线预览抽屉（与文件管理页一致） -->
		<el-drawer v-model="previewDrawer" :title="previewName || '文件预览'" size="60%" destroy-on-close>
			<OnlineFilePreview :url="previewUrl" class="jf-att-preview" />
		</el-drawer>
		<el-image-viewer v-if="previewImage" :url-list="[previewUrl]" :hide-on-click-modal="true" @close="previewImage = false" />
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import JfUserPicker from './JfUserPicker.vue';
import FileIconsModule from 'file-icons-vue';
import OnlineFilePreview from '/@/components/file/OnlineFilePreview.vue';
import { formatFileSize, getFileTagType, formatDateTime } from '/@/utils/format';
import { downloadByUrl } from '/@/utils/download';
import { initiateExtraFlags } from '../helpers';
import { hostAdapters as adapters } from '../adapters';
import type { SysFile } from '/@/api-services/system/models/sys-file';

const FileIcons = FileIconsModule.default || FileIconsModule;

defineOptions({ name: 'JfInitiateExtras' });

const props = withDefaults(
	defineProps<{
		graph?: Record<string, any> | null;
		taskId?: string | null;
		disabled?: boolean;
		ccActors?: string[];
		nextOperators?: string[];
		applyReason?: string;
		attachment?: SysFile | string;
	}>(),
	{
		graph: null,
		taskId: null,
		disabled: false,
		ccActors: () => [],
		nextOperators: () => [],
		applyReason: '',
		attachment: '',
	}
);

const emit = defineEmits<{
	'update:ccActors': [v: string[]];
	'update:nextOperators': [v: string[]];
	'update:applyReason': [v: string];
	'update:attachment': [v: SysFile | string];
}>();

const flags = computed(() => initiateExtraFlags(props.graph));

// ── 附件展示：所有派生信息汇总在一个 computed 中（对齐文件管理页 fm-card）──
const IMAGE_RE = /\.(jpg|jpeg|png|gif|bmp|webp|svg|ico)$/i;

const attCard = computed(() => {
	const a = props.attachment;
	if (!a) {
		return { visible: false, isObject: false, isImage: false, suffixFull: '', suffixLabel: '', name: '', fileName: '', sizeKb: 0, sizeLabel: '', tagType: '', uploader: '', time: '', url: '', thumbUrl: '' };
	}
	if (typeof a === 'string') {
		// 字符串为上传失败兜底，无 url 等信息
		const sizeKb = 0;
		return { visible: true, isObject: false, isImage: false, suffixFull: '', suffixLabel: '文件', name: a, fileName: '', sizeKb, sizeLabel: formatFileSize(sizeKb), tagType: getFileTagType(sizeKb), uploader: '', time: '', url: '', thumbUrl: '' };
	}
	const file = a; // SysFile
	const suffixFull = file.suffix ?? '';
	const name = `${file.fileName ?? ''}${suffixFull}`;
	const sizeKb = Number(file.sizeKb ?? 0);
	// 缩略图地址：与文件管理页一致（缩略图走后端缩放参数，相对路径补前导斜杠）
	const url = file.url ?? '';
	const absUrl = /^(https?:)?\/\//i.test(url) || url.startsWith('/') ? url : `/${url}`;
	const createTime = file.createTime;
	return {
		visible: true,
		isObject: true,
		isImage: IMAGE_RE.test(suffixFull),
		suffixFull,
		suffixLabel: (suffixFull || '文件').replace(/^\./, ''),
		name,
		fileName: file.fileName ?? '',
		sizeKb,
		sizeLabel: formatFileSize(sizeKb),
		tagType: getFileTagType(sizeKb),
		uploader: file.createUserName ?? '',
		time: createTime ? formatDateTime(createTime, 'YYYY-mm-dd HH:MM') : '',
		url,
		thumbUrl: url ? `${absUrl}?format=jpg&width=480` : '',
	};
});

// 缩略图加载失败：隐藏图片，露出底层占位图标
const onThumbError = (e: Event) => {
	(e.target as HTMLImageElement).style.visibility = 'hidden';
};

// ── 附件预览（与文件管理页逻辑一致：图片用 el-image-viewer，其它用在线预览抽屉）──
const previewName = ref('');
const previewUrl = ref('');
const previewDrawer = ref(false);
const previewImage = ref(false);

function openPreview() {
	const c = attCard.value;
	if (!c.isObject) return; // 字符串（上传失败兜底）无 url，无法预览
	previewName.value = c.name;
	previewUrl.value = c.url;
	if (c.isImage) {
		previewImage.value = true;
		previewDrawer.value = false;
	} else {
		previewDrawer.value = true;
		previewImage.value = false;
	}
}

function handleDownload() {
	const c = attCard.value;
	if (!c.isObject) return;
	downloadByUrl({ url: c.url, fileName: c.fileName || undefined });
}

async function onFile(e: Event) {
	const file = (e.target as HTMLInputElement).files?.[0];
	if (!file) {
		emit('update:attachment', '');
		return;
	}
	if (adapters.upload) {
		try {
			emit('update:attachment', await adapters.upload(file));
		} catch {
			emit('update:attachment', file.name);
		}
	} else {
		emit('update:attachment', file.name);
	}
}
</script>

<style scoped>
/* 单附件卡片：对齐文件管理页 fm-card 布局（无编辑/删除按钮） */
.jf-att-card {
	--c1: var(--el-color-primary);

	position: relative;
	overflow: hidden;
	display: flex;
	flex-direction: column;
	width: 240px;
	max-width: 100%;
	margin-top: 6px;
	border-radius: 14px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}

/* 缩略图 */
.jf-att-thumb {
	position: relative;
	height: 130px;
	display: flex;
	align-items: center;
	justify-content: center;
	overflow: hidden;
	background: radial-gradient(circle at 50% 120%, color-mix(in srgb, var(--c1) 22%, transparent), transparent 70%), var(--el-fill-color-lighter);
}

.jf-att-cover {
	position: relative;
	z-index: 1;
	width: 100%;
	height: 100%;
	object-fit: cover;
}

.jf-att-thumb-icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	color: var(--el-text-color-secondary);
}

.jf-att-suffix {
	position: absolute;
	top: 8px;
	left: 8px;
	z-index: 3;
	padding: 2px 8px;
	border-radius: 6px;
	font-size: 11px;
	font-weight: 700;
	letter-spacing: 0.3px;
	text-transform: uppercase;
	color: var(--el-color-white);
	background: color-mix(in srgb, #000000 52%, transparent);
	/* 纯装饰，不能挡住下面图片的点击 */
	pointer-events: none;
}

/* 预览/下载：缩略图居中遮罩，鼠标移入缩略图才显示 */
.jf-att-thumb-mask {
	position: absolute;
	inset: 0;
	z-index: 2;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 10px;
	background: color-mix(in srgb, #000000 45%, transparent);
	opacity: 0;
	/* 隐藏时必须穿透，否则透明遮罩会拦住图片上的所有点击 */
	pointer-events: none;
	transition: opacity 0.25s ease;
}

.jf-att-thumb:hover .jf-att-thumb-mask {
	opacity: 1;
	pointer-events: auto;
}

/* 卡片正文 */
.jf-att-body {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 12px 14px 10px;
	flex: 1;
}

.jf-att-name {
	margin: 0;
	font-size: 13.5px;
	font-weight: 600;
	line-height: 1.45;
	color: var(--el-text-color-primary);
	display: -webkit-box;
	-webkit-line-clamp: 2;
	line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
	word-break: break-all;
}

.jf-att-meta {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

.jf-att-size {
	padding: 1px 7px;
	border-radius: 6px;
	font-size: 11.5px;
	font-weight: 600;

	&.is-success {
		color: var(--el-color-success);
		background: color-mix(in srgb, var(--el-color-success) 12%, transparent);
	}

	&.is-warning {
		color: var(--el-color-warning);
		background: color-mix(in srgb, var(--el-color-warning) 14%, transparent);
	}

	&.is-danger {
		color: var(--el-color-danger);
		background: color-mix(in srgb, var(--el-color-danger) 12%, transparent);
	}
}

.jf-att-time {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);

	.el-icon {
		font-size: 12px;
	}
}

.jf-att-foot {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding: 8px 10px 8px 14px;
	border-top: 1px dashed var(--el-border-color-lighter);
	background: color-mix(in srgb, var(--el-fill-color-lighter) 45%, transparent);
}

.jf-att-user {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	min-width: 0;
	max-width: 150px;
	font-size: 11.5px;
	color: var(--el-text-color-regular);

	.el-icon {
		font-size: 12px;
		flex-shrink: 0;
		color: var(--el-color-primary);
	}

	.jf-att-user-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.jf-att-preview {
	height: 100%;
}
</style>
