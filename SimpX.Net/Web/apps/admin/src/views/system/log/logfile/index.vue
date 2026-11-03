<template>
	<div class="lf-page">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">文件日志</h2>
					<p class="page-hero__desc">在线查看与下载服务器日志文件，大文件自动截取最后 10000 行，支持流式加载与实时进度。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ lfStats.files }}</span>
							<span class="page-hero__stat-k">日志文件</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ lfStats.lines }}</span>
							<span class="page-hero__stat-k">当前行数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ lfStats.size }}</span>
							<span class="page-hero__stat-k">已读取</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="lf-body">
			<splitpanes class="default-theme">
				<!-- ===== 左侧：日志文件列表 ===== -->
				<pane size="16">
					<el-card shadow="hover" class="lf-card lf-files" v-loading="state.loading" body-style="height:calc(100% - 62px);overflow:auto;padding:8px">
						<template #header>
							<div class="lf-head">
								<span class="lf-head-ico"><el-icon><ele-Folder /></el-icon></span>
								<span class="lf-head-title">日志文件列表</span>
								<div class="lf-head-acts">
									<el-tooltip content="刷新列表" placement="top">
										<el-button icon="ele-Refresh" size="small" plain circle @click="fetchFileList" />
									</el-tooltip>
								</div>
							</div>
						</template>
						<!-- 树形展示日志文件，点击节点触发加载 -->
						<el-tree :data="state.fileList" node-key="name" :props="{ children: 'children', label: 'name' }" highlight-current default-expand-all @node-click="loadLogContent">
							<template #default="{ node }">
								<span class="lf-node">
									<el-icon class="lf-node-ico"><ele-Document /></el-icon>
									<span class="lf-node-name">{{ node.label }}</span>
									<!-- 文件大小标签，根据大小显示不同颜色 -->
									<el-tag :type="getFileTagType(node.data.sizeKB)" effect="light" round size="small">
										{{ formatFileSize(node.data.sizeKB) }}
									</el-tag>
								</span>
							</template>
						</el-tree>
					</el-card>
				</pane>

				<!-- ===== 右侧：日志内容展示区 ===== -->
				<pane size="84">
					<el-card shadow="hover" class="lf-card lf-view" body-style="height:calc(100% - 62px);overflow:hidden;padding:0">
						<template #header>
							<div class="lf-head">
								<span class="lf-head-ico is-primary"><el-icon><ele-Document /></el-icon></span>
								<span class="lf-head-title">{{ state.currentFile?.name || '未选择文件' }}</span>
								<el-tag type="info" effect="light" round size="small">{{ state.currentFile?.lastModified || '最后修改时间' }}</el-tag>
								<div class="lf-head-acts">
									<el-progress v-if="state.progress > 0 && state.progress < 100" :percentage="state.progress" :text-inside="true" :stroke-width="20" style="width: 180px" striped striped-flow />
									<el-tag v-if="state.preparing" type="info" effect="plain" size="small">
										<el-icon><ele-Loading /></el-icon>准备中...
									</el-tag>
									<el-tag v-else-if="state.streaming" type="warning" effect="plain" size="small">
										<el-icon><ele-Loading /></el-icon>{{ state.lineCount }} 行
									</el-tag>
									<el-tag v-else-if="state.fileContent" type="success" effect="light" size="small">
										<el-icon><ele-Check /></el-icon>{{ state.lineCount }} 行
									</el-tag>
									<el-button icon="ele-Close" size="small" type="danger" plain @click="cancelLoad" v-if="state.streaming || state.preparing">取消</el-button>
									<el-button icon="ele-Download" size="small" type="primary" @click="downloadFile">下载</el-button>
								</div>
							</div>
						</template>

						<!-- ===== 日志内容主体 ===== -->
						<div class="log-container">
							<!-- 滚动容器 -->
							<div class="log-wrapper" ref="logWrapperRef">
								<!-- 日志文本，使用 ref 直接操作 DOM 提升性能 -->
								<pre v-show="state.fileContent" class="log-text" ref="logTextRef">{{ state.fileContent }}</pre>
								<div v-if="!state.fileContent" class="log-empty">
									<el-icon><ele-FolderOpened /></el-icon>
									<p>从左侧选择一个日志文件开始查看</p>
								</div>
							</div>
							<!-- 底部状态栏：显示行数、大小、加载状态 -->
							<div class="log-footer" v-show="state.fileContent || state.streaming || state.preparing">
								<div class="footer-right">
									<span
										><el-icon><ele-Document /></el-icon>{{ state.lineCount }} 行</span
									>
									<span
										><el-icon><ele-PieChart /></el-icon>{{ state.receivedSize }} / {{ state.totalSize }}</span
									>
									<span v-if="state.preparing" class="status-preparing">
										<el-icon><ele-Loading /></el-icon>准备中...
									</span>
									<span v-else-if="state.streaming" class="status-loading">
										<el-icon><ele-Loading /></el-icon>{{ state.progress }}%
									</span>
									<span v-else-if="state.fileContent" class="status-done">
										<el-icon><ele-Check /></el-icon>已加载
									</span>
									<span v-else class="status-idle">等待加载</span>
								</div>
							</div>
						</div>
					</el-card>
				</pane>
			</splitpanes>
		</div>
	</div>
</template>

<!-- 文件日志 -->
<script lang="ts" setup name="sysLogFile">
import { computed, onMounted, reactive, ref, nextTick, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { downloadByData, getFileName } from '/@/utils/download';

import { getAPI, getToken, serveConfig } from '/@/utils/axios-utils';
import { SysLogFileApi } from '/@/api-services/system/api';
import { formatBytes as formatBytesReadable, formatFileSize, getFileTagType } from '/@/utils/format';

const state = reactive({
	loading: false, // 文件列表加载中
	fileList: [] as any[], // 日志文件列表
	fileContent: '', // 日志内容（用于模板显示）；为空时右侧显示空态提示
	currentFile: {} as any, // 当前选中的文件信息
	progress: 0, // 加载进度 0-100
	streaming: false, // 是否正在流式加载中
	preparing: false, // 是否正在准备中
	lineCount: 0, // 已加载行数
	receivedSize: '0 B', // 已接收数据大小（格式化）
	totalSize: '0 B', // 文件总大小（格式化）
	reader: null as ReadableStreamDefaultReader<Uint8Array> | null, // 流读取器引用
	maxLines: 0, // 截取行数，0=全部加载，>0=截取模式
	estimatedTotalLines: 0, // 截取模式下的估算总行数
});

// 头图统计：文件数递归整棵树（含子节点），行数与已读大小取当前加载状态
const lfStats = computed(() => {
	let files = 0;
	const walk = (nodes: any[] | undefined) => {
		for (const n of nodes ?? []) {
			files += 1;
			walk(n.children);
		}
	};
	walk(state.fileList);
	return { files, lines: state.lineCount, size: state.receivedSize };
});

const logWrapperRef = ref<HTMLElement | null>(null); // 日志滚动容器的 DOM 引用
const logTextRef = ref<HTMLElement | null>(null); // 日志文本元素的 DOM 引用（用于直接操作 textContent）
let abortController: AbortController | null = null; // 取消控制器：用于中断 fetch 请求
let contentBuffer = ''; // 数据缓冲区：累积数据，批量刷新到 DOM
const LOADING_PLACEHOLDER = '正在读取文件...'; // 加载中的占位提示

// 清除“正在读取文件...”占位提示（写入真实内容前或加载结束时调用）
const clearPlaceholder = (): void => {
	if (logTextRef.value && logTextRef.value.textContent === LOADING_PLACEHOLDER) logTextRef.value.textContent = '';
};

// 组件挂载
onMounted(() => {
	fetchFileList();
});

// 组件卸载
onUnmounted(() => {
	cancelLoad();
});

// 获取日志文件列表
const fetchFileList = async (): Promise<void> => {
	state.loading = true;
	try {
		const res = await getAPI(SysLogFileApi).apiSysLogFileLogFileListGet();
		state.fileList = res.data.result || [];
	} catch {
		ElMessage.error('获取列表失败');
	} finally {
		state.loading = false;
	}
};

// 加载日志内容（流式读取）,使用 Fetch API + ReadableStream 实现逐块读取和实时显示
const loadLogContent = async (file: any): Promise<void> => {
	// 取消之前正在进行的加载
	cancelLoad();

	// ----- 判断是否为截取模式 -----
	// 文件超过 5MB 时截取最后 10000 行，否则加载全部
	const isTruncateMode = (file.sizeKB || 0) > 5 * 1024;
	const maxLines = isTruncateMode ? 10000 : 0;

	// ----- 重置状态 -----
	state.currentFile = file;
	state.fileContent = LOADING_PLACEHOLDER;
	state.progress = 0;
	state.streaming = false;
	state.preparing = true; // 进入准备状态
	state.lineCount = 0;
	state.receivedSize = '0 B';
	state.totalSize = '0 B';
	state.maxLines = maxLines;
	state.estimatedTotalLines = 0;
	contentBuffer = '';
	if (logTextRef.value) logTextRef.value.textContent = LOADING_PLACEHOLDER;

	try {
		// 创建取消控制器
		abortController = new AbortController();

		const url = `${serveConfig.basePath}/api/sysLogFile/logContentStream/${encodeURIComponent(file.name)}/${maxLines}`;
		const response = await fetch(url, {
			signal: abortController.signal,
			headers: { Authorization: 'Bearer ' + getToken(), Accept: 'text/plain' },
		});
		if (!response.ok) throw new Error('HTTP ' + response.status);

		// ----- 计算文件总大小（用于进度计算） -----
		let totalBytes = (file.sizeKB || 0) * 1024;
		const cl = response.headers.get('content-length');
		if (cl) {
			const v = parseInt(cl);
			if (v > 0) totalBytes = v;
		}
		if (totalBytes === 0) totalBytes = 100 * 1024 * 1024;
		state.totalSize = formatBytes(totalBytes);

		// ----- 如果是截取模式，估算总行数 -----
		// 通过文件大小和平均行长度估算，用于进度计算
		if (isTruncateMode) {
			// 假设平均每行 1KB（根据实际情况可调整）
			const avgLineSize = 1024;
			state.estimatedTotalLines = Math.ceil(totalBytes / avgLineSize);
		}

		// ----- 获取流读取器 -----
		const reader = response.body?.getReader();
		if (!reader) throw new Error('无法读取响应流');
		state.reader = reader;

		const decoder = new TextDecoder();
		let received = 0;
		let lines = 0;
		let lastProgressTime = 0;
		let hasReceivedData = false;

		// 性能参数：128KB 缓冲，平衡渲染性能和实时性
		const BUFFER_SIZE = 128 * 1024;

		// ----- 主循环：逐块读取数据 -----
		while (true) {
			const { done, value } = await reader.read();
			if (done) break;

			// 收到第一条数据，退出准备状态
			if (!hasReceivedData) {
				hasReceivedData = true;
				state.preparing = false;
				state.streaming = true;
			}

			// 解码并累积到缓冲区
			const chunk = decoder.decode(value, { stream: true });
			contentBuffer += chunk;
			received += value.length;
			const newLines = (chunk.match(/\n/g) || []).length;
			lines += newLines;

			// ----- 更新进度 -----
			const now = Date.now();
			if (now - lastProgressTime > 100) {
				if (isTruncateMode) {
					// 截取模式：基于行数估算进度
					state.progress = calcProgressByLines(lines, state.estimatedTotalLines);
				} else {
					// 全部加载模式：基于字节数精确计算
					state.progress = Math.min(Math.round((received / totalBytes) * 100), 99);
				}
				state.lineCount = lines;
				state.receivedSize = formatBytes(received);
				lastProgressTime = now;
			}

			// ----- 缓冲区满则刷新到 DOM -----
			if (contentBuffer.length >= BUFFER_SIZE) {
				flushBuffer();
			}
		}

		// ----- 加载完成 -----
		state.progress = 100;
		state.lineCount = lines;
		state.receivedSize = formatBytes(received);
		flushBuffer(); // 刷新剩余数据
		state.streaming = false;
		clearPlaceholder(); // 未收到任何内容时（如空文件）清除占位提示

		// 空文件处理
		if (!logTextRef.value?.textContent) {
			state.fileContent = '📄 文件为空';
			if (logTextRef.value) logTextRef.value.textContent = '📄 文件为空';
		} else {
			state.fileContent = logTextRef.value.textContent;
		}

		await nextTick();
		scrollToBottom();

		// 根据模式显示不同提示
		if (isTruncateMode) {
			ElMessage.success(`加载完成，显示最后 ${lines} 行`);
		} else {
			ElMessage.success(`加载完成，共 ${lines} 行`);
		}
	} catch (err: any) {
		state.preparing = false;
		// 只有非取消错误才显示提示，取消错误静默处理
		if (err.name !== 'AbortError') {
			ElMessage.error('加载失败: ' + err.message);
		}
		state.streaming = false;
	} finally {
		// 释放 reader 资源
		if (state.reader) {
			try {
				state.reader.releaseLock();
			} catch {}
			state.reader = null;
		}
		if (contentBuffer) flushBuffer();
	}
};

// 用户主动点击取消按钮
const cancelLoad = (): void => {
	// 取消 fetch 请求
	if (abortController) {
		try {
			abortController.abort();
		} catch {}
		abortController = null;
	}
	// 释放流读取器
	if (state.reader) {
		try {
			state.reader.releaseLock();
		} catch {}
		state.reader = null;
	}
	// 清空缓冲区
	if (contentBuffer) flushBuffer();
	state.streaming = false;
	state.preparing = false;
};

// 下载当前选中的日志文件
const downloadFile = async (): Promise<void> => {
	if (!state.currentFile?.name) {
		ElMessage.error('请先选择文件');
		return;
	}
	try {
		const res = await getAPI(SysLogFileApi).apiSysLogFileDownloadLogFileFileNamePost(state.currentFile.name, { responseType: 'blob' });
		const fileName = getFileName(res.headers) || state.currentFile.name;
		downloadByData(res.data as any, fileName);
		ElMessage.success('下载成功');
	} catch {
		ElMessage.error('下载失败');
	}
};

// 下载进度沿用 2 位小数（`1.50 KB`），与通用实现默认去掉末尾 0 的口径区分
const formatBytes = (bytes: number): string => formatBytesReadable(bytes, { trimZeros: false });

// 滚动日志容器到底部
const scrollToBottom = (): void => {
	const el = logWrapperRef.value;
	if (el) el.scrollTop = el.scrollHeight;
};

// 追加文本到 DOM（直接操作 textContent，绕过 Vue 响应式，性能远优于通过 Vue 响应式更新）
const appendText = (text: string): void => {
	if (logTextRef.value) {
		logTextRef.value.textContent += text;
	}
};

// 刷新缓冲区到 DOM（将累积的数据一次性追加，减少 DOM 操作次数）
const flushBuffer = (): void => {
	if (!contentBuffer) return;
	clearPlaceholder(); // 首次写入真实内容前清除“正在读取文件...”占位提示
	appendText(contentBuffer);
	contentBuffer = '';
	// 加载中时自动滚动到底部
	if (state.streaming) {
		requestAnimationFrame(scrollToBottom);
	}
};

/**
 * 计算截取模式的进度（基于行数）
 * @param loaded - 已加载行数
 * @param estimatedTotal - 估算总行数
 * @returns 进度百分比 0-99
 */
const calcProgressByLines = (loaded: number, estimatedTotal: number): number => {
	if (estimatedTotal <= 0) return 0;
	return Math.min(Math.round((loaded / estimatedTotal) * 100), 99);
};
</script>

<style lang="scss" scoped>
.lf-page {
	position: relative;
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;
	padding-bottom: 4px;
	overflow: hidden;
}

.lf-body {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;

	:deep(.splitpanes) {
		width: 100%;
		height: 100%;
	}
}

.lf-card {
	/* 撑满 pane：否则卡片高度只按内容走，左侧树只有几行高、右侧日志区也浮在中间 */
	height: 100%;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
}

/* ===================== 卡头：浅底图标 + 标题 + 右侧操作 ===================== */
.lf-head {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

/* 装饰规范：图标深色、外围浅色同色系 */
.lf-head-ico {
	flex: 0 0 auto;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--tint, var(--el-color-warning));
	background: color-mix(in srgb, var(--tint, var(--el-color-warning)) 13%, transparent);

	&.is-primary {
		--tint: var(--el-color-primary);
	}
}

.lf-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.lf-head-acts {
	margin-left: auto;
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

/* ===================== 文件树节点 ===================== */
.lf-node {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
}

.lf-node-ico {
	color: var(--el-color-primary);
}

.lf-node-name {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* ===================== 日志区 ===================== */
.log-container {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.log-wrapper {
	position: relative;
	flex: 1;
	overflow: auto;
	background: #1e1e1e;

	/* 深色底上的滚动条：轨道透明跟随底色，避免出现贯穿整列的浅色条 */
	&::-webkit-scrollbar-track,
	&::-webkit-scrollbar-track-piece {
		background-color: transparent;
	}
	&::-webkit-scrollbar-thumb {
		background-color: rgba(144, 147, 153, 0.55);
		border-radius: 5px;
	}
	&::-webkit-scrollbar-thumb:hover {
		background-color: rgba(144, 147, 153, 0.8);
	}
}

.log-text {
	margin: 0;
	padding: 16px;
	color: #d4d4d4;
	font-family: Consolas, Monaco, "Courier New", monospace;
	font-size: 13px;
	line-height: 1.75;
	white-space: pre-wrap;
	word-wrap: break-word;
}

/* 未选择文件时的空态 */
.log-empty {
	position: absolute;
	inset: 0;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 10px;
	color: #6b7075;

	.el-icon {
		font-size: 46px;
		opacity: 0.75;
	}

	p {
		margin: 0;
		font-size: 13px;
	}
}

.log-footer {
	display: flex;
	justify-content: flex-end;
	padding: 6px 20px;
	background: #252526;
	border-top: 1px solid #333;
}

.footer-right {
	display: flex;
	align-items: center;
	gap: 24px;
	color: #9e9e9e;
	font-size: 12px;
}

.footer-right span {
	display: flex;
	align-items: center;
	gap: 4px;
}
</style>
