<template>
	<div class="fm-page">
		<splitpanes class="default-theme">
			<pane size="16" min-size="10" style="display: flex; min-width: 0">
				<FileTree @node-click="handleNodeChange" />
			</pane>

			<pane size="84" style="display: flex; flex-direction: column; min-width: 0">
				<div class="fm-main">
					<!-- 顶部头图：全站统一规范（theme/hero.scss），此处只做贴顶不收缩 -->
					<div class="page-hero fm-hero">
						<div class="page-hero__deco">
							<span class="page-hero__blob is-1"></span>
							<span class="page-hero__blob is-2"></span>
							<span class="page-hero__blob is-3"></span>
							<span class="page-hero__mesh"></span>
							<span class="page-hero__shine"></span>
						</div>
						<div class="page-hero__main">
							<div class="page-hero__left">
								<h2 class="page-hero__title">文件管理</h2>
								<p class="page-hero__desc">按文件路径浏览平台内的全部附件，支持在线预览、下载与元数据维护。</p>
							</div>
							<div class="page-hero__right">
								<div class="page-hero__stats" title="图片数量与占用空间按当前已加载数据统计">
									<div class="page-hero__stat">
										<span class="page-hero__stat-v">{{ statTotal }}</span>
										<span class="page-hero__stat-k">文件总数</span>
									</div>
									<div class="page-hero__stat">
										<span class="page-hero__stat-v is-on">{{ statImageCount }}</span>
										<span class="page-hero__stat-k">图片文件</span>
									</div>
									<div class="page-hero__stat">
										<span class="page-hero__stat-v">{{ statTotalSize }}</span>
										<span class="page-hero__stat-k">占用空间</span>
									</div>
								</div>
							</div>
						</div>
					</div>

					<!-- 搜索与操作 -->
					<div class="fm-toolbar">
						<el-form :model="state.queryParams" :show-message="false" :inline-message="true" label-width="auto" class="fm-form">
							<el-form-item label="文件名称" prop="fileName">
								<el-input v-model="state.queryParams.fileName" placeholder="请输入文件名称" clearable @keyup.enter.native="handleSearch">
									<template #prefix
										><el-icon class="fm-input-ico"><ele-Search /></el-icon
									></template>
								</el-input>
							</el-form-item>
							<el-form-item label="开始时间" prop="startTime">
								<el-date-picker v-model="state.queryParams.startTime" type="datetime" placeholder="开始时间" format="YYYY-MM-DD HH:mm:ss" value-format="YYYY-MM-DD HH:mm:ss" style="width: 190px" />
							</el-form-item>
							<el-form-item label="结束时间" prop="endTime">
								<el-date-picker v-model="state.queryParams.endTime" type="datetime" placeholder="结束时间" format="YYYY-MM-DD HH:mm:ss" value-format="YYYY-MM-DD HH:mm:ss" style="width: 190px" />
							</el-form-item>
						</el-form>
						<div class="fm-toolbar-actions">
							<el-button-group>
								<el-button icon="ele-Search" type="primary" @click="handleSearch" v-auth="'sysFile/page'" :loading="state.loading">查询</el-button>
								<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading">重置</el-button>
							</el-button-group>
							<el-button icon="ele-UploadFilled" type="primary" class="fm-upload-btn" @click="showUpload">上传文件</el-button>
						</div>
					</div>

					<!-- 文件卡片列表 -->
					<div class="fm-scroll">
						<div class="fm-cards" v-loading="state.loading">
							<article class="fm-card" v-for="(row, index) in state.cardList" :key="row.id" :style="{ animationDelay: Math.min(index * 0.04, 0.6) + 's' }" @dblclick="onCardDblclick(row)">
								<div class="fm-thumb">
									<!-- 缩略图：用原生 img，避免 el-image 在长列表里挂一堆观察器 -->
									<img v-if="isImage(row.suffix)" class="fm-cover" :src="thumbUrl(row)" :alt="row.fileName" loading="lazy" decoding="async" @error="onThumbError" />
									<div v-else class="fm-thumb-icon">
										<FileIcons :name="row.suffix" :width="54" :height="54" />
									</div>

									<span class="fm-suffix">{{ (row.suffix || '文件').replace(/^\./, '') }}</span>

									<!-- 遮罩层本身也承接点击：hover 时它盖在图片上，只让按钮可点会显得"点不动" -->
									<div class="fm-thumb-mask" @click.stop="showPreviewDialog(row)">
										<el-button circle icon="ele-View" title="预览" v-auth="'sysFile/preview'" @click.stop="showPreviewDialog(row)" />
										<el-button circle icon="ele-Download" title="下载" @click.stop="handleDownload(row)" />
									</div>
								</div>

								<div class="fm-body">
									<h3 class="fm-name" :title="`${row.fileName ?? ''}${row.suffix ?? ''}`">{{ row.fileName }}{{ row.suffix }}</h3>
									<div class="fm-meta">
										<span class="fm-size" :class="`is-${getFileTagType(row.sizeKb ?? 0)}`">{{ formatFileSize(row.sizeKb ?? 0) }}</span>
										<span class="fm-user" :title="`上传者：${row.createUserName || '—'}`">
											<el-icon><ele-UserFilled /></el-icon>
											<span class="fm-user-name">{{ row.createUserName || '未知用户' }}</span>
										</span>
									</div>
									<div class="fm-finger" :title="row.fileMd5 ? `MD5：${row.fileMd5}` : 'MD5：—'">
										<span class="fm-finger-k">MD5</span>
										<span class="fm-finger-v">{{ row.fileMd5 || '—' }}</span>
									</div>
									<div class="fm-provider" :title="row.filePath ?? ''">
										<el-icon><ele-Coin /></el-icon>
										<span>{{ row.provider || '—' }}</span>
									</div>
								</div>

								<footer class="fm-foot">
									<span class="fm-time">
										<el-icon><ele-Clock /></el-icon>
										{{ formatDateTime(row.createTime, 'YYYY-mm-dd HH:MM') }}
									</span>
									<div class="fm-ops">
										<el-button icon="ele-Edit" text type="primary" title="编辑" v-auth="'sysFile/update'" @click.stop="handleEdit(row)" />
										<el-button icon="ele-Delete" text type="danger" title="删除" v-auth="'sysFile/delete'" @click.stop="handleDelete(row)" />
									</div>
								</footer>
							</article>
						</div>

						<div class="fm-empty" v-if="!state.loading && state.cardList.length === 0">
							<el-empty description="没有找到匹配的文件" :image-size="140" />
						</div>

						<div class="fm-pager" v-if="cardPager.total > 0">
							<el-pagination
								v-model:current-page="cardPager.page"
								v-model:page-size="cardPager.pageSize"
								:page-sizes="[12, 24, 48, 96]"
								:total="cardPager.total"
								layout="total, sizes, prev, pager, next, jumper"
								background
								@size-change="onCardSizeChange"
								@current-change="loadCardList"
							/>
						</div>
					</div>
				</div>
			</pane>
		</splitpanes>

		<!-- 文件预览抽屉：头部与「在线用户」抽屉保持一致（主题色头 + 图标标题 + 右侧操作），追加全屏切换 -->
		<el-drawer v-model="state.filePreviewViewer" :size="previewDrawerSize" class="fm-preview-drawer" destroy-on-close>
			<template #header>
				<div class="dialog-title fm-preview-title">
					<el-icon size="16"><ele-Document /></el-icon>
					<span class="fm-preview-name" :title="state.fileName">{{ state.fileName }}</span>
					<span v-if="fileTypeLabel" class="fm-preview-chip">{{ fileTypeLabel }}</span>
					<span v-if="state.fileSizeKb" class="fm-preview-size">{{ formatFileSize(state.fileSizeKb) }}</span>
				</div>
				<div class="fm-preview-acts">
					<el-tooltip :content="state.previewFullscreen ? '退出全屏' : '全屏'" placement="bottom">
						<el-button text @click="state.previewFullscreen = !state.previewFullscreen">
							<SvgIcon :name="state.previewFullscreen ? 'iconfont icon-tuichuquanping' : 'iconfont icon-fullscreen'" />
						</el-button>
					</el-tooltip>
				</div>
			</template>
			<OnlineFilePreview :url="state.filePreviewUrl" class="file-preview-viewer" />
		</el-drawer>

		<el-image-viewer v-if="state.imagePreviewViewer" :url-list="[state.filePreviewUrl]" :hideOnClickModal="true" @close="state.imagePreviewViewer = false" />

		<EditFile ref="editRef" title="编辑文件" @handleQuery="loadCardList" />

		<!-- 上传文件对话框 -->
		<UploadDialog ref="uploadDialogRef" @success="handleUploadSuccess" />
	</div>
</template>

<!-- 文件管理 -->
<script lang="ts" setup name="sysFile">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';

import { downloadByUrl } from '/@/utils/download';
import { formatFileSize, getFileTagType, formatDateTime } from '/@/utils/format';
import { auth } from '/@/utils/authFunction';
import FileIconsModule from 'file-icons-vue';
const FileIcons = FileIconsModule.default || FileIconsModule;

import FileTree from './component/fileTree.vue';
import EditFile from './component/editFile.vue';
import UploadDialog from './component/uploadDialog.vue';
import OnlineFilePreview from '/@/components/file/OnlineFilePreview.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system/api';
import { SysFile, PageFileInput } from '/@/api-services/system/models';

// 判断是否是图片类型
const isImage = (suffix?: string | null) => /\.(jpg|jpeg|png|gif|bmp|webp|svg|ico)$/i.test(String(suffix ?? ''));

const editRef = ref<InstanceType<typeof EditFile>>();
const uploadDialogRef = ref<InstanceType<typeof UploadDialog>>();

const state = reactive({
	loading: false,
	queryParams: {
		fileName: undefined as string | undefined,
		filePath: undefined as string | undefined,
		startTime: undefined as string | undefined,
		endTime: undefined as string | undefined,
	},
	fileName: '',
	fileSuffix: '',
	fileSizeKb: 0,
	filePreviewViewer: false,
	/** 预览抽屉是否全屏（100% 宽），窄屏时自动视为全屏 */
	previewFullscreen: false,
	imagePreviewViewer: false,
	filePreviewUrl: '',
	/** 卡片列表数据 */
	cardList: [] as SysFile[],
});

// 预览抽屉宽度：窄屏或点过全屏按钮时铺满整屏，否则沿用 60%
const viewportWidth = ref(window.innerWidth);
const previewDrawerSize = computed(() => (viewportWidth.value < 900 || state.previewFullscreen ? '100%' : '60%'));
const onViewportResize = () => (viewportWidth.value = window.innerWidth);

// 头部文件类型角标（去掉后缀的点、统一大写）
const fileTypeLabel = computed(() =>
	String(state.fileSuffix ?? '')
		.replace('.', '')
		.toUpperCase()
);

// 分页参数
const cardPager = reactive({ page: 1, pageSize: 24, total: 0 });

// 加载文件列表
const loadCardList = async () => {
	state.loading = true;
	try {
		const params = {
			...state.queryParams,
			page: cardPager.page,
			pageSize: cardPager.pageSize,
			field: 'createTime',
			order: 'desc',
			descStr: 'desc',
		} as PageFileInput;
		const res = await getAPI(SysFileApi).apiSysFilePagePost(params);
		state.cardList = (res.data.result?.items ?? []) as SysFile[];
		cardPager.total = Number(res.data.result?.total ?? 0);
	} catch {
		state.cardList = [];
		cardPager.total = 0;
		ElMessage.error('文件列表加载失败');
	} finally {
		state.loading = false;
	}
};

// 查询按钮
const handleSearch = async () => {
	cardPager.page = 1;
	await loadCardList();
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.fileName = undefined;
	state.queryParams.filePath = undefined;
	state.queryParams.startTime = undefined;
	state.queryParams.endTime = undefined;
	cardPager.page = 1;
	await loadCardList();
};

// 每页条数变化
const onCardSizeChange = () => {
	cardPager.page = 1;
	loadCardList();
};

// 打开上传对话框
const showUpload = () => {
	uploadDialogRef.value?.openDialog();
};

// 上传成功回调
const handleUploadSuccess = () => {
	loadCardList();
};

// 下载
const handleDownload = (row: SysFile) => {
	downloadByUrl({ url: row.url ?? '', fileName: row.fileName ?? undefined });
};

// 删除
const handleDelete = (row: SysFile) => {
	ElMessageBox.confirm(`确定删除文件：${row.fileName}?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysFileApi).apiSysFileDeletePost({ id: row.id! });
			await loadCardList();
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

// 树组件点击
const handleNodeChange = async (node: any) => {
	state.queryParams.fileName = undefined;
	state.queryParams.filePath = node.path;
	state.queryParams.startTime = undefined;
	state.queryParams.endTime = undefined;
	cardPager.page = 1;
	await loadCardList();
};

// 打开编辑页面
const handleEdit = (row: SysFile) => {
	editRef.value?.openDialog(row);
};

// 打开文件预览页面
const showPreviewDialog = (row: SysFile) => {
	state.fileName = `${row.fileName ?? ''}${row.suffix ?? ''}`;
	state.fileSuffix = row.suffix ?? '';
	state.fileSizeKb = Number(row.sizeKb ?? 0);
	state.filePreviewUrl = row.url ?? '';
	// 每次打开都回到默认宽度，避免上次全屏状态被带进新文件
	state.previewFullscreen = false;
	if (isImage(row.suffix)) {
		state.imagePreviewViewer = true;
		state.filePreviewViewer = false;
	} else {
		state.filePreviewViewer = true;
		state.imagePreviewViewer = false;
	}
};

/* ------------------------------ 展示辅助 ------------------------------ */
// 缩略图地址：走后端的按需缩放参数，避免列表直接拉原图
const thumbUrl = (row: SysFile) => {
	const url = row.url ?? '';
	if (!url) return '';
	// 后端返回的是相对路径（upload/yyyy/MM/dd/xxx.png），补前导斜杠避免受当前路由影响
	const abs = /^(https?:)?\/\//i.test(url) || url.startsWith('/') ? url : `/${url}`;
	return `${abs}?format=jpg&width=480`;
};

// 缩略图加载失败时隐藏图片，露出底层的占位图标（不弹任何提示，避免整页刷屏）
const onThumbError = (e: Event) => {
	const el = e.target as HTMLImageElement;
	el.style.visibility = 'hidden';
};

// 顶部统计
const statTotal = computed(() => cardPager.total);
const statImageCount = computed(() => state.cardList.filter((r) => isImage(r.suffix)).length);
const statTotalSize = computed(() => {
	const kb = state.cardList.reduce((sum, r) => sum + Number(r.sizeKb ?? 0), 0);
	return formatFileSize(kb);
});

// 卡片双击 = 预览
const onCardDblclick = (row: SysFile) => {
	if (auth('sysFile/preview')) showPreviewDialog(row);
};

// 页面初始化
onMounted(() => {
	loadCardList();
	window.addEventListener('resize', onViewportResize);
});

onUnmounted(() => {
	window.removeEventListener('resize', onViewportResize);
});
</script>

<style lang="scss" scoped>
.fm-page {
	/* 不要再用 position:absolute + inset:5px：
	   路由组件根节点会被 parent.vue 加 .w100，而 theme/app.scss 生成的
	   .w100 { width: 100% !important } 在绝对定位下按「padding box」算宽度（比内容区宽 10px），
	   right 失效 → 页面右侧永远多出一条横向滚动条。
	   改成普通 flex 项：.layout-parent 是 flex 列容器，flex:1 拿到剩余高度（splitpanes 需要确定高度），
	   宽度由 align-items:stretch 得到内容区宽度，与打印模板页一致。 */
	position: relative;
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	/* 页边距全部由 .layout-parent 的 padding:5px 提供，这里不能再叠 padding；
	   只补底部 4px，与打印模板页（.pt-page）的底部合计 9px 对齐 */
	padding-bottom: 4px;
	overflow: hidden;

	:deep(.splitpanes) {
		flex: 1;
		min-height: 0;
	}

	:deep(.splitpanes__pane) {
		min-width: 0;
		min-height: 0;
		overflow: hidden;
	}
}

.fm-main {
	flex: 1;
	display: flex;
	flex-direction: column;
	min-width: 0;
	min-height: 0;
	overflow: hidden;
	padding-left: 10px;
}

/* ===================== 顶部头图 ===================== */
/* 视觉部分走全站统一规范（theme/hero.scss 的 .page-hero），
 * 这里只保留本页特有的布局约束：头图在纵向 flex 里不参与收缩 */
.fm-hero {
	flex-shrink: 0;
}

/* ===================== 搜索栏 ===================== */
.fm-toolbar {
	display: flex;
	align-items: center;
	justify-content: flex-start;
	gap: 16px;
	flex-wrap: wrap;
	flex-shrink: 0;
	/* 块间距与内边距对齐打印模板页的 .pt-toolbar（14px / 14px 18px） */
	margin-top: 14px;
	padding: 14px 18px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		border-color 0.25s ease,
		box-shadow 0.25s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 28%, var(--el-border-color-lighter));
		box-shadow: 0 10px 24px -20px color-mix(in srgb, var(--el-color-primary) 80%, transparent);
	}
}

/* 不设定宽度：表单按控件自身内容撑开，右侧按钮紧随其后 */
.fm-form {
	flex: 0 1 auto;
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 8px 12px;

	:deep(.el-form-item) {
		margin-bottom: 0;
		margin-right: 0;
	}
}

.fm-input-ico {
	color: var(--el-text-color-placeholder);
}

.fm-toolbar-actions {
	display: flex;
	align-items: center;
	gap: 8px;
}

.fm-upload-btn {
	position: relative;
	overflow: hidden;

	&::after {
		content: '';
		position: absolute;
		top: 0;
		left: -60%;
		width: 40%;
		height: 100%;
		background: linear-gradient(100deg, transparent, rgba(255, 255, 255, 0.45), transparent);
		transform: skewX(-20deg);
		transition: left 0.55s ease;
	}

	&:hover::after {
		left: 120%;
	}
}

/* ===================== 卡片区 ===================== */
.fm-scroll {
	flex: 1;
	min-height: 120px;
	overflow-y: auto;
	overscroll-behavior: contain;
	margin-top: 14px;
	padding-right: 4px;
}

.fm-cards {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
	gap: 14px;
	/* 空列表时容器高度为 0，v-loading 遮罩无处可画，给个兜底高度 */
	min-height: 160px;
}

.fm-card {
	--c1: var(--el-color-primary);
	--c2: color-mix(in srgb, var(--el-color-primary), #ffffff 45%);

	position: relative;
	overflow: hidden;
	display: flex;
	flex-direction: column;
	border-radius: 14px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	cursor: default;
	/* backwards 而非 both：动画结束后不保留 transform，
	   否则会压住 :hover 的 translateY，导致悬停上浮失效 */
	animation: fmRise 0.45s ease backwards;
	transition:
		transform 0.3s ease,
		box-shadow 0.3s ease,
		border-color 0.3s ease;

	&:hover {
		transform: translateY(-4px);
		border-color: color-mix(in srgb, var(--c1) 45%, var(--el-border-color-lighter));
		box-shadow: 0 18px 34px -22px color-mix(in srgb, var(--c1) 90%, transparent);

		.fm-thumb-mask {
			opacity: 1;
			pointer-events: auto;
		}
	}
}

/* 缩略图 */
.fm-thumb {
	position: relative;
	height: 130px;
	display: flex;
	align-items: center;
	justify-content: center;
	overflow: hidden;
	background: radial-gradient(circle at 50% 120%, color-mix(in srgb, var(--c1) 22%, transparent), transparent 70%), var(--el-fill-color-lighter);
}

.fm-cover {
	position: relative;
	z-index: 1;
	width: 100%;
	height: 100%;
	object-fit: cover;
	transition: transform 0.45s ease;

	.fm-thumb:hover & {
		transform: scale(1.06);
	}
}

.fm-thumb-icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	transition: transform 0.35s ease;

	&:hover {
		transform: translateY(-3px);
	}
}

.fm-suffix {
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

.fm-thumb-mask {
	position: absolute;
	inset: 0;
	z-index: 2;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 10px;
	background: color-mix(in srgb, #000000 45%, transparent);
	opacity: 0;
	/* 关键：隐藏时必须穿透，否则透明遮罩会拦住图片上的所有点击 */
	pointer-events: none;
	transition: opacity 0.25s ease;
}

/* 卡片正文 */
.fm-body {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 12px 14px 10px;
	flex: 1;
}

.fm-name {
	margin: 0;
	font-size: 13.5px;
	font-weight: 600;
	line-height: 1.45;
	color: var(--el-text-color-primary);
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
	word-break: break-all;
}

.fm-meta {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

.fm-size {
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

.fm-time {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);

	.el-icon {
		font-size: 12px;
	}
}

.fm-provider {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	min-width: 0;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);

	span {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.el-icon {
		font-size: 12px;
		flex-shrink: 0;
	}
}

.fm-foot {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding: 8px 10px 8px 14px;
	border-top: 1px dashed var(--el-border-color-lighter);
	background: color-mix(in srgb, var(--el-fill-color-lighter) 45%, transparent);
}

.fm-user {
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

	.fm-user-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

/* 文件指纹（MD5）：独占一行铺满卡片宽度，只在触达卡片边缘时才省略 */
.fm-finger {
	display: flex;
	align-items: center;
	gap: 5px;
	align-self: stretch;
	min-width: 0;
	padding: 2px 7px;
	border-radius: 6px;
	background: var(--el-fill-color-lighter);
	border: 1px solid transparent;
	font-family: var(--el-font-family-code, monospace);
	font-size: 11px;
	color: var(--el-text-color-secondary);
	transition:
		border-color 0.25s ease,
		color 0.25s ease;
}

.fm-card:hover .fm-finger {
	border-color: color-mix(in srgb, var(--c1) 32%, transparent);
}

.fm-finger-k {
	flex-shrink: 0;
	font-family: var(--el-font-family);
	font-size: 10px;
	font-weight: 700;
	letter-spacing: 0.5px;
	color: var(--el-text-color-placeholder);
}

.fm-finger-v {
	flex: 1 1 auto;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.fm-ops {
	display: inline-flex;
	align-items: center;
	gap: 0;
	flex-shrink: 0;
	margin-right: -6px;

	:deep(.el-button) {
		padding: 5px;
	}
}

/* ===================== 空态 / 分页 ===================== */
.fm-empty {
	margin-top: 14px;
	padding: 40px 0;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px dashed var(--el-border-color-lighter);
}

.fm-pager {
	display: flex;
	justify-content: flex-end;
	margin-top: 14px;
	padding: 10px 14px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
}

/* ===================== 预览 ===================== */
.file-preview-viewer {
	width: 100%;
	height: 100%;
}

/* ===================== 动画 ===================== */
@keyframes fmRise {
	from {
		opacity: 0;
		transform: translateY(14px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@media (max-width: 900px) {
	.fm-cards {
		grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
	}

	.fm-pager {
		justify-content: center;
	}
}
</style>

<!-- 抽屉头部/主体属于 el-drawer 内部节点，scoped 的 :deep 依赖祖先 data-v 才能命中；
     一旦抽屉被传送到 body 就会断链，故统一用独立类名写在非 scoped 块里（与 onlineUser 抽屉同一套写法） -->
<style lang="scss">
.fm-preview-drawer {
	.el-drawer__header {
		/* 头部压扁，把高度尽量让给预览区 */
		padding: 8px 12px !important;
	}

	.el-drawer__body {
		/* 预览组件自身撑满，抽屉 body 去掉内边距，否则四周会出现一圈白边 */
		display: flex;
		padding: 0;
		min-height: 0;
	}
}

.fm-preview-title {
	flex: 1 1 auto;
	min-width: 0;
	gap: 8px;
	font-size: 15px;
	font-weight: 700;
	letter-spacing: 0.2px;
}

/* 文件名是唯一可伸缩项：先省略文件名，类型角标与体积始终可见 */
.fm-preview-name {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.fm-preview-chip {
	flex-shrink: 0;
	padding: 0 6px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 10px;
	font-weight: 700;
	letter-spacing: 0.5px;
	background: color-mix(in srgb, currentColor 24%, transparent);
}

.fm-preview-size {
	flex-shrink: 0;
	font-size: 11px;
	font-weight: 500;
	opacity: 0.78;
}

.fm-preview-acts {
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

/* 亮色下 is-text 的 hover/active 底是近白实色，压在主题色上就是一块白框；这里换成头部文字色的半透明膜。
 * 暗色那一档本就是抬亮膜、压在主题色上仍分得开，所以只覆盖亮色 */
html:not([data-theme='dark']) .fm-preview-acts .el-button {
	--el-fill-color-light: color-mix(in srgb, var(--el-color-white) 15%, transparent);
	--el-fill-color: color-mix(in srgb, var(--el-color-white) 25%, transparent);
}
</style>
