<template>
	<div class="document-list-container">
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
					<h2 class="page-hero__title">文档列表</h2>
					<p class="page-hero__desc">集中管理各类文档：上传后自动归类，支持在线预览、分享与精细化权限管控；删除的文档进入回收站，可随时恢复。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.total }}</span>
							<span class="page-hero__stat-k">文档总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.documentList.length }}</span>
							<span class="page-hero__stat-k">本页显示</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="{ 'is-off': selectedDocuments.length === 0 }">{{ selectedDocuments.length }}</span>
							<span class="page-hero__stat-k">已选中</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（按钮放这里，不进内容卡片，避免随内容滚动） -->
		<div class="query-toolbar">
			<el-form :model="state" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.searchKeyword" class="query-input" placeholder="文档名称" clearable @keyup.enter="handleSearch" />
				</el-form-item>
				<el-form-item label="标签">
					<el-select v-model="state.currentTagId" class="query-select" placeholder="全部标签" clearable @change="handleTagFilter">
						<el-option v-for="tag in state.tagList" :key="tag.id" :label="tag.name" :value="tag.id">
							<el-tag :color="tag.color || '#409EFF'" effect="dark" size="small">{{ tag.name }}</el-tag>
						</el-option>
					</el-select>
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleSearch" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery" :loading="state.loading"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Upload" @click="handleUpload" v-auth="'document/upload'"> 上传 </el-button>
				<el-tooltip content="在当前分类下创建新的子分类（文件夹）" placement="top">
					<el-button icon="ele-FolderAdd" @click="handleNewFolder"> 新建分类 </el-button>
				</el-tooltip>
				<el-button icon="ele-Delete" :disabled="selectedDocuments.length === 0" @click="handleBatchDelete"> 删除 </el-button>
				<el-button icon="ele-Download" :disabled="selectedDocuments.length === 0" @click="handleBatchDownload"> 下载 </el-button>
				<el-button icon="ele-Share" :disabled="selectedDocuments.length === 0" @click="handleBatchShare"> 分享 </el-button>
				<el-button icon="ele-FolderOpened" :disabled="selectedDocuments.length === 0" @click="handleBatchTransfer"> 转移 </el-button>
				<el-button-group>
					<el-button :type="state.viewMode === 'list' ? 'primary' : ''" icon="ele-Menu" title="列表视图" @click="state.viewMode = 'list'" />
					<el-button :type="state.viewMode === 'card' ? 'primary' : ''" icon="ele-Grid" title="卡片视图" @click="state.viewMode = 'card'" />
				</el-button-group>
			</div>
		</div>

		<!-- 内容区域：左侧分类树 + 右侧文档列表 -->
		<splitpanes class="default-theme doc-splitpanes">
			<!-- 左侧：分类树 -->
			<pane size="15" style="display: flex; flex-direction: column">
				<el-card shadow="hover" class="doc-tree-card">
					<template #header>
						<div class="doc-tree-card__header">
							<span class="doc-tree-card__title">分类管理</span>
							<el-button text size="small" icon="ele-Operation" @click="handleGoToCategoryManage">管理</el-button>
						</div>
					</template>
					<DocumentCategoryTree ref="categoryTreeRef" :current-category-id="state.currentCategoryId" @select="handleCategorySelect" />
				</el-card>
			</pane>

			<!-- 右侧：文档列表 -->
			<pane size="85" style="display: flex; flex-direction: column">
				<el-card shadow="hover" class="doc-content-card">
					<!-- 面包屑导航 + 全选（卡片视图下卡片自身带勾选框，这里给一个快捷全选） -->
					<div class="doc-content-bar">
						<el-breadcrumb separator="/" class="doc-breadcrumb">
							<el-breadcrumb-item>
								<el-button text @click="handleNavigateToCategory(null)">全部文档</el-button>
							</el-breadcrumb-item>
							<el-breadcrumb-item v-for="item in state.breadcrumbList" :key="item.id">
								<el-button text @click="handleNavigateToCategory(item.id)">{{ item.name }}</el-button>
							</el-breadcrumb-item>
						</el-breadcrumb>
						<el-checkbox
							v-if="state.viewMode === 'card'"
							v-model="isAllSelected"
							:indeterminate="isIndeterminate"
							@change="handleSelectAll"
							class="doc-select-all"
						>
							全选
						</el-checkbox>
					</div>

					<!-- 列表视图 -->
					<DocumentList
						v-if="state.viewMode === 'list'"
						:documents="state.documentList"
						:loading="state.loading"
						:selected-documents="selectedDocuments"
						@select="handleSelect"
						@preview="handlePreview"
						@download="handleDownload"
						@delete="handleDelete"
						@share="handleShare"
						@permission="handlePermission"
						@edit="handleEdit"
						@rename="handleRename"
					/>

					<!-- 卡片视图 -->
					<DocumentCard
						v-else
						:documents="state.documentList"
						:loading="state.loading"
						:selected-documents="selectedDocuments"
						@select="handleSelect"
						@preview="handlePreview"
						@download="handleDownload"
						@delete="handleDelete"
						@share="handleShare"
						@permission="handlePermission"
					/>
				</el-card>
			</pane>
		</splitpanes>

		<!-- 分页：统一 .table-pager（右对齐 + 上间距） -->
		<el-pagination
			v-model:current-page="state.page"
			v-model:page-size="state.pageSize"
			:page-sizes="[20, 50, 100, 200]"
			:total="state.total"
			layout="total, sizes, prev, pager, next, jumper"
			@size-change="handleQuery"
			@current-change="handleQuery"
			class="table-pager"
		/>

		<!-- 上传对话框 -->
		<DocumentUploadDialog ref="uploadDialogRef" @success="handleUploadSuccess" />

		<!-- 预览对话框 -->
		<DocumentPreviewDialog ref="previewDialogRef" />

		<!-- 编辑对话框 -->
		<DocumentEditDialog ref="editDialogRef" @handleQuery="handleQuery" />

		<!-- 权限设置对话框 -->
		<DocumentPermissionDialog ref="permissionDialogRef" />

		<!-- 分享对话框 -->
		<DocumentShareDialog ref="shareDialogRef" />

		<!-- 批量转移对话框 -->
		<DocumentBatchTransferDialog ref="batchTransferDialogRef" @success="handleTransferSuccess" />
	</div>
</template>

<script lang="ts" setup name="documentList">
import { getAPI } from '/@/utils/axios-utils';
import { DocumentApi, DocumentCategoryApi, DocumentTagApi } from '/@/api-services/document/api';
import { onMounted, reactive, ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { useRouter } from 'vue-router';
import '/@/views/document/styles/document.scss';

import DocumentList from '../components/DocumentList.vue';
import DocumentCard from '../components/DocumentCard.vue';
import DocumentUploadDialog from '../components/DocumentUploadDialog.vue';
import DocumentPreviewDialog from '../components/DocumentPreviewDialog.vue';
import DocumentEditDialog from '../components/DocumentEditDialog.vue';
import DocumentPermissionDialog from '../components/DocumentPermissionDialog.vue';
import DocumentShareDialog from '../components/DocumentShareDialog.vue';
import DocumentBatchTransferDialog from '../components/DocumentBatchTransferDialog.vue';
import DocumentCategoryTree from '../components/DocumentCategoryTree.vue';

import { downloadStreamFile } from '/@/utils/download';

const router = useRouter();

const state = reactive({
	viewMode: 'card' as 'list' | 'card',
	searchKeyword: '',
	currentCategoryId: null as number | null,
	currentTagId: null as number | null,
	breadcrumbList: [] as Array<{ id: number; name: string }>,
	documentList: [] as any[],
	tagList: [] as any[],
	loading: false,
	page: 1,
	pageSize: 20,
	total: 0,
});

const selectedDocuments = ref<number[]>([]);
const uploadDialogRef = ref();
const previewDialogRef = ref();
const editDialogRef = ref();
const permissionDialogRef = ref();
const shareDialogRef = ref();
const batchTransferDialogRef = ref();
const categoryTreeRef = ref();
const queryInFlight = ref<Promise<void> | null>(null);
const lastQuerySignature = ref('');
const lastCompletedQuerySignature = ref('');
const categoryTreeCache = ref<any[] | null>(null);

// 页面初始化
onMounted(() => {
	loadTagList();
	handleQuery();
});

// 加载标签列表
const loadTagList = async () => {
	try {
		const res = await getAPI(DocumentTagApi).apiDocumentTagListGet();
		state.tagList = res.data?.result || [];
	} catch (error: any) {
		console.error('加载标签列表失败:', error);
		state.tagList = [];
	}
};

// 查询文档列表
const handleQuery = async (options: { force?: boolean } = {}) => {
	const params = {
		page: state.page,
		pageSize: state.pageSize,
		keyword: state.searchKeyword || undefined,
		categoryId: state.currentCategoryId || undefined,
		tagId: state.currentTagId || undefined,
	};
	const querySignature = JSON.stringify(params);
	if (!options.force && !queryInFlight.value && querySignature === lastCompletedQuerySignature.value) {
		return;
	}
	// 避免初始化阶段由多个事件触发的同参重复请求
	if (queryInFlight.value && querySignature === lastQuerySignature.value) {
		await queryInFlight.value;
		return;
	}
	lastQuerySignature.value = querySignature;
	const queryTask = (async () => {
		state.loading = true;
		try {
			const res = await getAPI(DocumentApi).apiDocumentPagePost(params);
			if (res.data?.result) {
				state.documentList = res.data.result.items || [];
				state.total = res.data.result.total || 0;
			} else {
				state.documentList = [];
				state.total = 0;
			}
			// 更新面包屑导航
			await updateBreadcrumb();
			lastCompletedQuerySignature.value = querySignature;
		} catch (error: any) {
			ElMessage.error(error?.response?.data?.message || '查询失败');
			state.documentList = [];
			state.total = 0;
		} finally {
			state.loading = false;
		}
	})();
	queryInFlight.value = queryTask;
	try {
		await queryTask;
	} finally {
		if (queryInFlight.value === queryTask) {
			queryInFlight.value = null;
		}
	}
};

// 重置查询条件
const resetQuery = () => {
	state.searchKeyword = '';
	state.currentTagId = null;
	state.page = 1;
	handleQuery();
};

// 更新面包屑导航
const updateBreadcrumb = async () => {
	if (!state.currentCategoryId) {
		state.breadcrumbList = [];
		return;
	}

	try {
		// 获取分类树（优先使用缓存，避免频繁重复请求）
		if (!categoryTreeCache.value) {
			const res = await getAPI(DocumentCategoryApi).apiDocumentCategoryTreeGet();
			categoryTreeCache.value = res.data?.result || [];
		}
		const categoryTree = categoryTreeCache.value;

		// 查找分类路径
		const findPath = (tree: any[], targetId: number, path: Array<{ id: number; name: string }> = []): Array<{ id: number; name: string }> | null => {
			for (const item of tree) {
				const currentPath = [...path, { id: item.id, name: item.name }];
				if (item.id === targetId) {
					return currentPath;
				}
				if (item.children && item.children.length > 0) {
					const found = findPath(item.children, targetId, currentPath);
					if (found) return found;
				}
			}
			return null;
		};

		const path = findPath(categoryTree, state.currentCategoryId);
		state.breadcrumbList = path || [];
	} catch (error) {
		// 获取面包屑失败不影响主流程
		console.error('获取面包屑失败:', error);
	}
};

// 搜索
const handleSearch = () => {
	state.page = 1;
	handleQuery();
};

// 上传成功回调
const handleUploadSuccess = () => {
	// 清除分类树缓存，强制重新加载
	categoryTreeCache.value = null;
	// 强制刷新文档列表
	handleQuery({ force: true });
	// 刷新分类树（如果上传到了新分类，分类树可能需要更新）
	if (categoryTreeRef.value) {
		categoryTreeRef.value.loadCategoryTree();
	}
};

// 上传
const handleUpload = () => {
	uploadDialogRef.value?.open(state.currentCategoryId);
};

// 新建分类（在当前分类下创建子分类）
const handleNewFolder = async () => {
	try {
		const currentCategoryName = state.breadcrumbList.length > 0 ? state.breadcrumbList[state.breadcrumbList.length - 1].name : '根目录';
		const { value: categoryName } = await ElMessageBox.prompt(`将在"${currentCategoryName}"下创建新的分类（文件夹）`, '新建分类', {
			confirmButtonText: '确定',
			cancelButtonText: '取消',
			inputPlaceholder: '请输入分类名称',
			inputValidator: (value) => {
				if (!value || value.trim().length === 0) {
					return '分类名称不能为空';
				}
				if (value.length > 128) {
					return '分类名称长度不能超过128个字符';
				}
				return true;
			},
		});

		if (categoryName && categoryName.trim()) {
			// 调用新建分类API
			const res = await getAPI(DocumentCategoryApi).apiDocumentCategoryAddPost({
				name: categoryName.trim(),
				parentId: state.currentCategoryId || 0,
				sort: 0,
			});

			if (res.data?.result) {
				ElMessage.success('新建分类成功');
				categoryTreeCache.value = null;
				// 刷新分类树
				if (categoryTreeRef.value) {
					categoryTreeRef.value.loadCategoryTree();
				}
				// 刷新文档列表
				handleQuery({ force: true });
			}
		}
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '新建分类失败');
		}
	}
};

// 跳转到分类管理页面
const handleGoToCategoryManage = () => {
	router.push('/document/category');
};

// 批量删除
const handleBatchDelete = async () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要删除的文档');
		return;
	}
	try {
		await ElMessageBox.confirm(`确定要删除选中的 ${selectedDocuments.value.length} 个文档吗？`, '提示', {
			type: 'warning',
		});

		// 批量删除文档
		const deletePromises = selectedDocuments.value.map((id) => getAPI(DocumentApi).apiDocumentDeletePost({ id }));
		await Promise.all(deletePromises);

		ElMessage.success('删除成功');
		handleQuery({ force: true });
		selectedDocuments.value = [];
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '删除失败');
		}
	}
};

// 批量下载
const handleBatchDownload = async () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要下载的文档');
		return;
	}
	try {
		// 逐个下载文档
		for (const id of selectedDocuments.value) {
			try {
				const res = await getAPI(DocumentApi).apiDocumentDownloadPost({ id }, { responseType: 'blob' });
				// 获取文档信息以确定文件名
				const detailRes = await getAPI(DocumentApi).apiDocumentDetailGet(id);
				const fileName = detailRes.data?.result?.name || `document_${id}`;
				await downloadStreamFile(res, fileName);
				// 添加延迟，避免浏览器阻止多个下载
				await new Promise((resolve) => setTimeout(resolve, 300));
			} catch (error) {
				console.error(`下载文档 ${id} 失败:`, error);
			}
		}
		ElMessage.success('开始下载');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '下载失败');
	}
};

// 批量分享
const handleBatchShare = () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要分享的文档');
		return;
	}
	shareDialogRef.value?.open(selectedDocuments.value);
};

// 批量转移
const handleBatchTransfer = () => {
	if (selectedDocuments.value.length === 0) {
		ElMessage.warning('请选择要转移的文档');
		return;
	}
	batchTransferDialogRef.value?.open(selectedDocuments.value);
};

// 转移成功回调
const handleTransferSuccess = () => {
	handleQuery({ force: true });
	selectedDocuments.value = [];
	categoryTreeCache.value = null;
	// 刷新分类树（如果转移了文档，分类树可能需要更新）
	if (categoryTreeRef.value) {
		categoryTreeRef.value.loadCategoryTree();
	}
};

// 选择文档
const handleSelect = (ids: number[]) => {
	selectedDocuments.value = ids;
};

// 计算是否全选
const isAllSelected = computed({
	get: () => {
		if (state.documentList.length === 0) return false;
		return state.documentList.every((doc) => selectedDocuments.value.includes(doc.id));
	},
	set: (value: boolean) => {
		if (value) {
			selectedDocuments.value = state.documentList.map((doc) => doc.id);
		} else {
			selectedDocuments.value = [];
		}
	},
});

// 计算是否半选（部分选中）
const isIndeterminate = computed(() => {
	if (selectedDocuments.value.length === 0) return false;
	return selectedDocuments.value.length > 0 && selectedDocuments.value.length < state.documentList.length;
});

// 全选/取消全选
const handleSelectAll = (checked: boolean) => {
	if (checked) {
		selectedDocuments.value = state.documentList.map((doc) => doc.id);
	} else {
		selectedDocuments.value = [];
	}
};

// 预览
const handlePreview = (document: any) => {
	previewDialogRef.value?.open(document);
};

// 下载
const handleDownload = async (document: any) => {
	try {
		const res = await getAPI(DocumentApi).apiDocumentDownloadPost({ id: document.id }, { responseType: 'blob' });
		if (await downloadStreamFile(res, document.name)) ElMessage.success('下载成功');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || '下载失败');
	}
};

// 删除
const handleDelete = async (document: any) => {
	try {
		await ElMessageBox.confirm(`确定要删除文档"${document.name}"吗？`, '提示', {
			type: 'warning',
		});
		await getAPI(DocumentApi).apiDocumentDeletePost({ id: document.id });
		ElMessage.success('删除成功');
		handleQuery({ force: true });
	} catch (error: any) {
		if (error !== 'cancel') {
			ElMessage.error(error?.response?.data?.message || '删除失败');
		}
	}
};

// 分享
const handleShare = (document: any) => {
	shareDialogRef.value?.open([document.id]);
};

// 权限设置
const handlePermission = (document: any) => {
	permissionDialogRef.value?.open(document.id);
};

// 编辑文档
const handleEdit = async (document: any) => {
	editDialogRef.value?.openDialog(document);
};

// 重命名（保留向后兼容，实际调用编辑对话框）
const handleRename = (document: any) => {
	handleEdit(document);
};

// 导航到分类
const handleNavigateToCategory = (categoryId: number | null) => {
	state.currentCategoryId = categoryId;
	state.page = 1;
	handleQuery();
};

// 分类选择
const handleCategorySelect = (categoryId: number | null) => {
	handleNavigateToCategory(categoryId);
};

// 标签筛选
const handleTagFilter = () => {
	state.page = 1;
	handleQuery();
};
</script>

<style lang="scss" scoped>
.document-list-container {
	/* 根节点是 .layout-parent 的 flex 项，吃满剩余高度；splitpanes 需要父级确定高度 */
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
	overflow: hidden;

	.doc-splitpanes {
		flex: 1;
		min-height: 0;
		overflow: hidden;
	}

	.doc-tree-card {
		height: 100%;
		display: flex;
		flex-direction: column;

		:deep(.el-card__header) {
			padding: 10px 14px;
		}

		:deep(.el-card__body) {
			flex: 1;
			overflow: auto;
			display: flex;
			flex-direction: column;
		}
	}

	.doc-content-card {
		height: 100%;
		display: flex;
		flex-direction: column;
		overflow: hidden;

		:deep(.el-card__body) {
			flex: 1;
			display: flex;
			flex-direction: column;
			overflow: hidden;
			min-height: 0;
		}
	}
}

/* 树卡片头部：标题加粗 + 右侧管理按钮，两端对齐 */
.doc-tree-card__header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.doc-tree-card__title {
	font-weight: 600;
	font-size: 14px;
}

/* 内容区顶部条：面包屑在左、全选在右 */
.doc-content-bar {
	flex-shrink: 0;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	margin-bottom: 12px;
}

.doc-select-all {
	flex-shrink: 0;
	margin-right: 0;
}
</style>
