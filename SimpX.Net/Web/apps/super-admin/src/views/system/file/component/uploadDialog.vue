<template>
	<el-dialog v-model="visible" :lock-scroll="false" draggable :close-on-click-modal="false" width="760px" @close="handleClose">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
				<span> 上传文件(支持断点续传) </span>
			</div>
		</template>

		<!-- 文件选择区 -->
		<el-upload ref="uploadRef" drag :auto-upload="false" :show-file-list="false" action multiple :on-change="handleChange" :accept="accept">
			<el-icon class="el-icon--upload"><ele-UploadFilled /></el-icon>
			<div class="el-upload__text">将文件拖到此处，或<em>点击上传</em></div>
			<template #tip>
				<div class="el-upload__tip">{{ tip }}</div>
			</template>
		</el-upload>

		<!-- 批量操作条：统计 + 全部上传 / 暂停 / 取消 / 清除已完成 -->
		<div v-if="fileTasks.length > 0" class="upload-toolbar">
			<div class="upload-summary">
				<span class="summary-total">共 {{ fileTasks.length }} 个文件</span>
				<template v-if="uploadingCount > 0">
					<span class="summary-divider"></span>
					<span class="summary-item">上传中 {{ uploadingCount }}</span>
				</template>
				<template v-if="successCount > 0">
					<span class="summary-divider"></span>
					<span class="summary-item summary-item--success">已完成 {{ successCount }}</span>
				</template>
				<template v-if="failedCount > 0">
					<span class="summary-divider"></span>
					<span class="summary-item summary-item--danger">失败 {{ failedCount }}</span>
				</template>
			</div>

			<div class="upload-actions">
				<el-tooltip content="开始上传全部待上传及失败的文件" placement="top">
					<el-button type="primary" size="small" icon="ele-UploadFilled" :disabled="!hasWaitingTask" @click="uploadAll">全部上传</el-button>
				</el-tooltip>
				<el-tooltip content="暂停全部正在上传的文件" placement="top">
					<el-button size="small" icon="ele-VideoPause" plain :disabled="uploadingCount === 0" @click="pauseAll">全部暂停</el-button>
				</el-tooltip>
				<el-tooltip content="中止并移除全部未完成的文件" placement="top">
					<el-button type="danger" size="small" icon="ele-CircleClose" plain :disabled="!hasUnfinishedTask" @click="cancelAll">全部取消</el-button>
				</el-tooltip>
				<el-tooltip content="移除列表中已上传成功的文件" placement="top">
					<el-button type="info" size="small" icon="ele-Delete" plain :disabled="successCount === 0" @click="clearUploaded">清除已完成</el-button>
				</el-tooltip>
			</div>
		</div>

		<!-- 总进度：多文件批量上传时给出整体进度感知 -->
		<div v-if="fileTasks.length > 0" class="upload-overall">
			<el-progress :percentage="overallProgress" :stroke-width="6" :show-text="false" class="overall-bar" />
			<span class="overall-text">{{ overallProgress }}% · {{ formatBytes(totalUploadedBytes) }} / {{ formatBytes(totalBytes) }}</span>
		</div>

		<!-- 文件列表 -->
		<div v-if="fileTasks.length > 0" class="file-list">
			<div v-for="task in fileTasks" :key="task.id" class="file-item">
				<!-- 文件图标 -->
				<div class="file-icon">
					<FileIcons :name="task.suffix" :width="40" :height="40" />
				</div>

				<!-- 中间信息区 -->
				<div class="file-body">
					<div class="file-header">
						<span class="file-name" :title="task.name">{{ task.name }}</span>
						<el-tag :type="statusTagType(task.status)" size="small" effect="light">{{ statusText(task.status) }}</el-tag>
					</div>

					<el-progress :percentage="Number(task.progress.toFixed(2))" :status="progressStatus(task.status)" :stroke-width="18" :text-inside="true" class="file-progress">
						<span class="progress-label">{{ task.progress.toFixed(0) }}% · {{ formatDuration(task.duration) }}</span>
					</el-progress>

					<div class="file-meta">
						<span class="file-speed">{{ task.status === 'uploading' ? task.speed : '—' }}</span>
						<span class="file-size">{{ formatBytes(task.uploadedBytes) }} / {{ formatBytes(task.totalBytes) }}</span>
					</div>
				</div>

				<!-- 按钮组（竖排两个，上下对齐） -->
				<div class="file-actions">
					<!-- 待上传：开始 / 移除 -->
					<template v-if="task.status === 'pending'">
						<el-tooltip content="开始" placement="right"> <el-button type="primary" icon="ele-VideoPlay" plain circle @click="startTask(task)"></el-button></el-tooltip>
						<el-tooltip content="移除" placement="right"> <el-button type="danger" icon="ele-Delete" plain circle @click="removeTask(task)"></el-button></el-tooltip>
					</template>

					<!-- 上传中：暂停 / 取消 -->
					<template v-if="task.status === 'uploading'">
						<el-tooltip content="暂停" placement="right"> <el-button type="primary" icon="ele-VideoPause" plain circle @click="pauseTask(task)"></el-button></el-tooltip>
						<el-tooltip content="取消" placement="right"> <el-button type="danger" icon="ele-CircleClose" plain circle @click="cancelTask(task)"></el-button></el-tooltip>
					</template>

					<!-- 已暂停：继续 / 取消 -->
					<template v-if="task.status === 'paused'">
						<el-tooltip content="继续" placement="right"> <el-button type="primary" icon="ele-VideoPlay" plain circle @click="resumeTask(task)"></el-button></el-tooltip>
						<el-tooltip content="取消" placement="right"> <el-button type="danger" icon="ele-CircleClose" plain circle @click="cancelTask(task)"></el-button></el-tooltip>
					</template>

					<!-- 上传失败：重试 / 移除 -->
					<template v-if="task.status === 'error'">
						<el-tooltip content="重试" placement="right"> <el-button type="primary" icon="ele-RefreshRight" plain circle @click="retryTask(task)"></el-button></el-tooltip>
						<el-tooltip content="移除" placement="right"> <el-button type="danger" icon="ele-Delete" plain circle @click="removeTask(task)"></el-button></el-tooltip>
					</template>

					<!-- 已上传：只显示移除按钮，占位保持布局整齐 -->
					<template v-if="task.status === 'success'">
						<el-tooltip content="移除" placement="right"> <el-button type="info" icon="ele-Delete" plain circle @click="removeTask(task)"></el-button></el-tooltip>
					</template>
				</div>
			</div>
		</div>
	</el-dialog>
</template>

<!-- 文件上传（tus 断点续传） -->
<script lang="ts" setup name="uploadDialog">
import { computed, ref } from 'vue';
import { ElMessage, ElMessageBox, UploadInstance } from 'element-plus';
import * as tus from 'tus-js-client';
import FileIconsModule from 'file-icons-vue';
import { getToken } from '/@/utils/axios-utils';
import { formatBytes as formatBytesReadable } from '/@/utils/format';
const FileIcons = FileIconsModule.default || FileIconsModule;

// Tus 上传端点，固定在组件内部不对外暴露
const TUS_ENDPOINT = '/api/tusUpload/';

// 写入 tus metadata 的文件分类，当前页面固定为"相关文件"
const FILE_TYPE_LABEL = '相关文件';

// 任务状态类型
type TaskStatus = 'pending' | 'uploading' | 'paused' | 'success' | 'error';

// 文件任务数据结构
interface FileTask {
	id: string;
	name: string;
	suffix: string;
	file: File;
	status: TaskStatus;
	progress: number;
	uploadedBytes: number;
	totalBytes: number;
	speed: string;
	duration: number;
	upload: tus.Upload | null;
	lastLoaded: number;
	lastTime: number;
	startTimestamp: number;
	sysFileId?: number | null; // 系统文件ID
}

// 组件属性定义
const props = withDefaults(
	defineProps<{
		accept?: string;
		tip?: string;
		chunkSize?: number;
		/** 批量上传时的并发数，避免数十个文件同时打满浏览器连接 */
		concurrency?: number;
		metadata?: Record<string, string>;
	}>(),
	{
		accept: '.jpg,.png,.bmp,.gif,.txt,.xml,.pdf,.xlsx,.docx,.mp4,.mkv,.avi,.mov',
		tip: '',
		chunkSize: 5 * 1024 * 1024,
		concurrency: 3,
		metadata: () => ({}),
	}
);

// 组件事件定义
const emit = defineEmits<{
	(e: 'success', sysFileId: number | null): void;
	(e: 'error', error: Error): void;
}>();

// 组件状态
const uploadRef = ref<UploadInstance>();
const visible = ref(false);
const fileTasks = ref<FileTask[]>([]);

/* ------------------------------------------------------------------ *
 * 批量上传队列
 * ------------------------------------------------------------------ *
 * "全部上传"不是无脑并发：一次性 start 几十个 tus 任务会打满浏览器连接、
 * 拖慢每个分片。这里用「并发上限 + 等待队列」调度，任务成功/失败后让出
 * 名额并自动补位，直到队列排空。单个文件的手动开始/继续不走队列。        */
const waitingQueue = ref<string[]>([]);
/** 正在占用并发名额的任务 id；任务被暂停/移除时必须主动让出，否则名额不回收、队列会卡住 */
const runningTaskIds = new Set<string>();

/** 从等待队列取任务补位，直到达到并发上限或队列排空 */
const pumpQueue = () => {
	while (runningTaskIds.size < props.concurrency && waitingQueue.value.length > 0) {
		const taskId = waitingQueue.value.shift()!;
		const task = fileTasks.value.find((t) => t.id === taskId);
		// 任务已被移除或状态已变化（手动取消/暂停）则跳过，不占用名额
		if (!task || task.status !== 'pending') continue;
		runningTaskIds.add(taskId);
		startUpload(task, () => {
			runningTaskIds.delete(taskId);
			pumpQueue();
		});
	}
};

/**
 * 让出并发名额
 * @param autoPump 是否立即补位；"全部暂停"需要只让出不补位，否则会把队列里的下一个文件继续拉起来
 */
const releaseSlot = (taskId: string, autoPump = true) => {
	runningTaskIds.delete(taskId);
	if (autoPump) pumpQueue();
};

/** 清空队列并复位并发名额 */
const resetQueue = () => {
	waitingQueue.value = [];
	runningTaskIds.clear();
};

// 字节格式化，自动换算 B/KB/MB/GB
// 上传进度固定保留 2 位小数，与通用 formatBytes 默认的去零口径不同
const formatBytes = (bytes: number) => formatBytesReadable(bytes, { trimZeros: false });

// 时长格式化，超过 60 秒显示为 XmYs
const formatDuration = (seconds: number) => {
	if (!seconds || seconds <= 0) return '0s';
	if (seconds < 60) return `${Math.floor(seconds)}s`;
	const m = Math.floor(seconds / 60);
	const s = Math.floor(seconds % 60);
	return `${m}m${s}s`;
};

/* ---------------------------- 统计信息 ---------------------------- */
const countByStatus = (status: TaskStatus) => fileTasks.value.filter((t) => t.status === status).length;
const uploadingCount = computed(() => countByStatus('uploading'));
const successCount = computed(() => countByStatus('success'));
const failedCount = computed(() => countByStatus('error'));
/** 待上传或失败的文件，全部上传的处理对象 */
const hasWaitingTask = computed(() => fileTasks.value.some((t) => t.status === 'pending' || t.status === 'error'));
/** 尚未完成（待上传/上传中/已暂停/失败）的文件 */
const hasUnfinishedTask = computed(() => fileTasks.value.some((t) => t.status !== 'success'));

const totalBytes = computed(() => fileTasks.value.reduce((sum, t) => sum + t.totalBytes, 0));
const totalUploadedBytes = computed(() => fileTasks.value.reduce((sum, t) => sum + t.uploadedBytes, 0));
const overallProgress = computed(() => {
	if (!totalBytes.value) return 0;
	return Math.min(100, Number(((totalUploadedBytes.value / totalBytes.value) * 100).toFixed(2)));
});

/* ---------------------------- 状态展示 ---------------------------- */
// 状态文案映射
const statusText = (status: TaskStatus) => {
	return { pending: '待上传', uploading: '上传中', paused: '已暂停', success: '已上传', error: '上传失败' }[status];
};

// 状态标签颜色映射
const statusTagType = (status: TaskStatus) => {
	return { pending: 'info', uploading: 'primary', paused: 'warning', success: 'success', error: 'danger' }[status] as any;
};

// 进度条状态映射，用于切换进度条颜色
const progressStatus = (status: TaskStatus): '' | 'success' | 'exception' | 'warning' => {
	if (status === 'success') return 'success';
	if (status === 'error') return 'exception';
	if (status === 'paused') return 'warning';
	return '';
};

/* ---------------------------- 生命周期 ---------------------------- */
// 打开对话框，重置文件列表
const openDialog = () => {
	visible.value = true;
	fileTasks.value = [];
	resetQueue();
};

// 文件选择回调，去重后加入任务列表
const handleChange = (file: any) => {
	const raw = file.raw as File;
	if (!raw) return;

	// 同名同大小视为重复文件
	if (fileTasks.value.some((t) => t.name === raw.name && t.totalBytes === raw.size)) {
		ElMessage.warning(`文件 ${raw.name} 已在列表中`);
		return;
	}

	const suffix = '.' + raw.name.split('.').pop()?.toLowerCase();
	fileTasks.value.push({
		id: `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
		name: raw.name,
		suffix,
		file: raw,
		status: 'pending',
		progress: 0,
		uploadedBytes: 0,
		totalBytes: raw.size,
		speed: '0 B/s',
		duration: 0,
		upload: null,
		lastLoaded: 0,
		lastTime: 0,
		startTimestamp: 0,
	});

	uploadRef.value?.clearFiles();
};

/* ---------------------------- 上传调度 ---------------------------- */
/**
 * 创建 tus 上传实例，绑定进度、成功、失败回调
 * @param onSettle 任务结束（成功或失败）回调，供批量队列回收并发名额
 */
const createUpload = (task: FileTask, onSettle?: () => void): tus.Upload => {
	// 失败重试可能同时触发 onError + onSuccess 之外的路径，确保名额只回收一次
	let settled = false;
	const settle = () => {
		if (settled) return;
		settled = true;
		onSettle?.();
	};

	return new tus.Upload(task.file, {
		headers: {
			Authorization: 'Bearer ' + getToken(),
		},
		endpoint: TUS_ENDPOINT,
		chunkSize: props.chunkSize,
		retryDelays: [0, 3000, 5000, 10000, 20000],
		metadata: {
			filename: task.file.name,
			filetype: task.file.type,
			fileType: FILE_TYPE_LABEL,
			...props.metadata,
		},
		// 每次请求后执行捕获响应头
		onAfterResponse(req, res) {
			const idStr = res.getHeader('X-Tus-SysFileId'); // 获取自定义的响应头
			if (idStr) task.sysFileId = Number(idStr); // 把系统文件ID挂载到task上
		},
		onError(error) {
			task.status = 'error';
			task.speed = '—';
			ElMessage.error(`${task.name} 上传失败：${error.message}`);
			emit('error', error);
			settle();
		},
		onProgress(bytesUploaded, bytesTotal) {
			task.progress = (bytesUploaded / bytesTotal) * 100;
			task.uploadedBytes = bytesUploaded;
			task.totalBytes = bytesTotal;

			// 实时计算已用时
			if (task.startTimestamp) {
				task.duration = (Date.now() - task.startTimestamp) / 1000;
			}

			// 每秒刷新一次速度
			const now = Date.now();
			const interval = (now - task.lastTime) / 1000;
			if (interval >= 1) {
				const speed = (bytesUploaded - task.lastLoaded) / interval;
				task.speed = formatBytes(speed) + '/s';
				task.lastLoaded = bytesUploaded;
				task.lastTime = now;
			}
		},
		onSuccess() {
			task.progress = 100;
			task.status = 'success';
			task.speed = '—';
			const sysFileId = task.sysFileId ?? null;
			ElMessage.success(`${task.name} 上传成功`);
			emit('success', sysFileId);
			settle();
		},
	});
};

/** 启动 tus 传输：先查找历史会话，命中则断点续传 */
const runUpload = (task: FileTask) => {
	task.upload!
		.findPreviousUploads()
		.then((previousUploads) => {
			if (previousUploads.length > 0) {
				task.upload!.resumeFromPreviousUpload(previousUploads[0]);
			}
			task.upload!.start();
		})
		.catch(() => {
			task.upload!.start();
		});
};

/**
 * 开始上传，先查找历史会话实现断点续传
 * @param onSettle 任务结束回调（批量上传时用于回收并发名额）
 */
const startUpload = (task: FileTask, onSettle?: () => void) => {
	if (!task.upload) task.upload = createUpload(task, onSettle);

	task.status = 'uploading';
	task.startTimestamp = task.startTimestamp || Date.now();
	task.lastLoaded = task.uploadedBytes;
	task.lastTime = Date.now();

	runUpload(task);
};

/** 重置任务的进度与计时，回到待上传状态 */
const resetTask = (task: FileTask) => {
	task.progress = 0;
	task.uploadedBytes = 0;
	task.speed = '0 B/s';
	task.duration = 0;
	task.upload = null;
	task.status = 'pending';
	task.startTimestamp = 0;
};

/** 中断传输，标记为已暂停（会话保留，可继续） */
const abortTask = (task: FileTask) => {
	task.upload?.abort();
	task.status = 'paused';
	task.speed = '—';
	// 冻结已用时：把暂停时长排除在外，恢复后计时接着走
	if (task.startTimestamp) {
		task.duration = (Date.now() - task.startTimestamp) / 1000;
	}
};

/* ---------------------------- 单文件操作 ---------------------------- */
// 开始任务
const startTask = (task: FileTask) => startUpload(task);

// 暂停任务，中断传输但保留会话
const pauseTask = (task: FileTask) => {
	abortTask(task);
	releaseSlot(task.id);
};

// 继续任务，从已上传位置恢复
const resumeTask = (task: FileTask) => {
	if (!task.upload) return;
	task.status = 'uploading';
	// 计时接回暂停前的累计值，避免暂停时长被算进已用时
	task.startTimestamp = Date.now() - task.duration * 1000;
	task.lastLoaded = task.uploadedBytes;
	task.lastTime = Date.now();

	runUpload(task);
};

// 取消任务，中断并从列表移除
const cancelTask = (task: FileTask) => {
	task.upload?.abort();
	removeTask(task);
};

// 重试任务，重置进度后重新开始
const retryTask = (task: FileTask) => {
	resetTask(task);
	startUpload(task);
};

// 从列表移除任务
const removeTask = (task: FileTask) => {
	const index = fileTasks.value.findIndex((t) => t.id === task.id);
	if (index > -1) fileTasks.value.splice(index, 1);
	releaseSlot(task.id);
};

/* ---------------------------- 批量操作 ---------------------------- */
// 全部上传：待上传的直接入队，失败的先重置再入队；由队列按并发上限依次放行
const uploadAll = () => {
	const targets = fileTasks.value.filter((t) => t.status === 'pending' || t.status === 'error');
	if (targets.length === 0) return;

	targets.forEach((t) => {
		if (t.status === 'error') resetTask(t);
		waitingQueue.value.push(t.id);
	});
	pumpQueue();
};

// 全部暂停：中断所有正在上传的任务并清空待上传队列（保留会话，可单独继续）
const pauseAll = () => {
	waitingQueue.value = [];
	fileTasks.value
		.filter((t) => t.status === 'uploading')
		.forEach((t) => {
			abortTask(t);
			// 只让出名额不补位：否则暂停一个会立刻把队列里的下一个文件拉起来
			releaseSlot(t.id, false);
		});
};

// 全部取消：中止未完成的任务并从列表移除，已成功的保留
const cancelAll = async () => {
	try {
		await ElMessageBox.confirm('将中止全部未完成的文件上传并从列表中移除，已上传成功的文件不受影响。是否继续？', '取消上传', {
			type: 'warning',
			confirmButtonText: '确定取消上传',
			cancelButtonText: '继续上传',
		});
	} catch {
		return;
	}

	resetQueue();
	fileTasks.value.filter((t) => t.status !== 'success').forEach((t) => t.upload?.abort());
	fileTasks.value = fileTasks.value.filter((t) => t.status === 'success');
};

// 清除已完成：移除列表中已上传成功的文件
const clearUploaded = () => {
	fileTasks.value = fileTasks.value.filter((t) => t.status !== 'success');
};

/* ---------------------------- 关闭对话框 ---------------------------- */
// 关闭对话框，暂停所有进行中的上传并清空队列
const handleClose = () => {
	resetQueue();
	fileTasks.value.filter((t) => t.status === 'uploading').forEach(abortTask);
	visible.value = false;
};

defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
/* 批量操作条：左侧统计，右侧按钮组，窄屏自动换行 */
.upload-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	flex-wrap: wrap;
	gap: 8px;
	margin-top: 12px;
}

.upload-summary {
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	.summary-total {
		font-weight: 600;
		color: var(--el-text-color-regular);
	}

	.summary-divider {
		width: 1px;
		height: 12px;
		background: var(--el-border-color-light);
	}

	.summary-item--success {
		color: var(--el-color-success);
	}

	.summary-item--danger {
		color: var(--el-color-danger);
	}
}

.upload-actions {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;

	.el-button {
		margin-left: 0;
	}
}

/* 总进度条 */
.upload-overall {
	display: flex;
	align-items: center;
	gap: 12px;
	margin-top: 10px;

	.overall-bar {
		flex: 1;
		min-width: 0;
	}

	.overall-text {
		flex-shrink: 0;
		font-size: 12px;
		color: var(--el-text-color-secondary);
		font-variant-numeric: tabular-nums;
	}
}

.file-list {
	margin-top: 12px;
	max-height: min(480px, 50vh);
	overflow-y: auto;
}

.file-item {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px;
	background: var(--el-fill-color-lighter);
	border-radius: var(--next-radius-sm);
	margin-bottom: 10px;
	transition: background-color 0.2s ease;

	&:hover {
		background: var(--el-fill-color-light);
	}
}

.file-icon {
	flex-shrink: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 48px;
	height: 48px;
	background: var(--el-bg-color);
	border-radius: var(--el-border-radius-base);
}

.file-body {
	flex: 1;
	min-width: 0;
}

.file-header {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 6px;
}

.file-name {
	flex: 1;
	font-size: 13px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.file-progress {
	width: 100%;
}

.progress-label {
	font-size: 12px;
	color: var(--el-color-white);
}

.file-meta {
	display: flex;
	justify-content: space-between;
	font-size: 12px;
	margin-top: 4px;
}

/* 速度用默认主题色 */
.file-speed {
	color: var(--el-color-primary);
}

.file-size {
	color: var(--el-color-info);
}

/* 按钮组竖排，上下对齐，固定宽度保证多文件时布局整齐 */
.file-actions {
	flex-shrink: 0;
	display: flex;
	flex-direction: column;
	gap: 8px;
	min-height: 64px;
	justify-content: center;
}

.file-actions .el-button {
	width: 100%;
	margin-left: 0;
}
</style>
