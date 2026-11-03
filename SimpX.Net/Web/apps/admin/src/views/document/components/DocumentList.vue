<template>
	<div class="document-list" ref="tableContainerRef">
		<vxe-grid ref="gridRef" class="xGrid-style" v-bind="gridOptions" :data="documents" :loading="loading" :height="tableHeight" @checkbox-change="emitSelection" @checkbox-all="emitSelection">
			<template #row_thumbnail="{ row }">
				<div class="doc-thumb" @click="handleThumbnailClick(row)">
					<el-image v-if="row.thumbnail" :src="row.thumbnail" class="doc-thumb__img" fit="cover" lazy :preview-src-list="[row.thumbnail]" :preview-teleported="true" />
					<FileIcons v-else :name="row.name" :width="22" :height="22" />
				</div>
			</template>

			<template #row_name="{ row }">
				<div class="doc-name-cell">
					<span class="doc-name-cell__icon">
						<el-icon><component :is="iconOf(row)" /></el-icon>
					</span>
					<span class="doc-name-cell__link" :title="row.name" @click="$emit('preview', row)">{{ row.name }}</span>
				</div>
			</template>

			<template #row_status="{ row }">
				<span class="doc-state-pill" :class="`is-${documentStatusTag(row.status)}`">
					<i></i>
					{{ documentStatusText(row.status) }}
				</span>
			</template>

			<template #row_categoryName="{ row }">
				<el-tag v-if="row.categoryName" :color="row.categoryColor || '#409EFF'" effect="dark" size="small">
					{{ row.categoryName }}
				</el-tag>
				<span v-else class="doc-muted">未分类</span>
			</template>

			<template #row_tags="{ row }">
				<div class="tag-group" v-if="row.tags && row.tags.length > 0">
					<el-tag v-for="tag in row.tags" :key="tag.id" :color="tag.color || '#409EFF'" effect="dark" size="small">
						{{ tag.name }}
					</el-tag>
				</div>
				<span v-else class="doc-muted">无标签</span>
			</template>

			<template #row_createTime="{ row }">
				<div class="doc-time-cell">
					<span>{{ row.createTime }}</span>
				</div>
			</template>

			<template #row_buttons="{ row }">
				<div class="doc-row-actions">
					<el-button link type="primary" size="small" @click="$emit('preview', row)">预览</el-button>
					<el-button link type="primary" size="small" @click="$emit('download', row)">下载</el-button>
					<el-dropdown trigger="click" @command="(cmd: string | number) => handleCommand(cmd, row)" style="margin-left: 10px">
						<el-button link type="primary" size="small">
							更多
							<el-icon class="el-icon--right"><ele-ArrowDown /></el-icon>
						</el-button>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item command="edit">
									<el-icon><ele-Edit /></el-icon>
									<span>编辑</span>
								</el-dropdown-item>
								<el-dropdown-item command="share">
									<el-icon><ele-Share /></el-icon>
									<span>分享</span>
								</el-dropdown-item>
								<el-dropdown-item command="permission">
									<el-icon><ele-Lock /></el-icon>
									<span>权限</span>
								</el-dropdown-item>
								<el-dropdown-item command="delete" divided>
									<el-icon><ele-Delete /></el-icon>
									<span style="color: var(--el-color-danger)">删除</span>
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</div>
			</template>
		</vxe-grid>
	</div>
</template>

<script lang="ts" setup>
import { ref, reactive, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import { VxeGridInstance, VxeGridProps } from 'vxe-table';
import FileIconsModule from 'file-icons-vue';
import '/@/views/document/styles/document.scss';
import { documentStatusTag, documentStatusText, documentIconByName, documentTypeIcon, splitDateTime } from '../utils/documentMeta';

const FileIcons = FileIconsModule.default || FileIconsModule;

const props = defineProps<{
	documents: any[];
	loading: boolean;
	selectedDocuments: number[];
}>();

const emit = defineEmits<{
	select: [ids: number[]];
	preview: [document: any];
	download: [document: any];
	delete: [document: any];
	share: [document: any];
	permission: [document: any];
	edit: [document: any];
	rename: [document: any];
}>();

// 类型图标：后端给了 documentType 就用它，没有则按文件名后缀兜底
const iconOf = (row: any) => (row.documentType ? documentTypeIcon(row.documentType) : documentIconByName(row.name));

// 排序：取文件大小（字节），兼容 sizeBytes / size / sizeInfo 三种来源
const getSizeBytes = (row: any) => {
	if (row.sizeBytes !== undefined && row.sizeBytes !== null) return Number(row.sizeBytes);
	if (row.size !== undefined && typeof row.size === 'number') return row.size;

	// 解析 sizeInfo 字符串 (例如 "1.5 MB")
	const str = row.sizeInfo || '';
	if (!str) return 0;

	// 移除逗号，统一格式
	const normalized = str.replace(/,/g, '').trim();
	const units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];

	// 匹配数字和单位
	const match = normalized.match(/^([\d.]+)\s*([a-zA-Z]*)$/);
	if (match) {
		const num = parseFloat(match[1]);
		let unit = match[2] ? match[2].toUpperCase() : '';

		// 处理无单位情况，默认视为B
		if (!unit) return num;

		const index = units.indexOf(unit);
		return index !== -1 ? num * Math.pow(1024, index) : num;
	}
	return 0;
};

const compareLocale = (a: string, b: string) => String(a).localeCompare(String(b), 'zh-CN', { numeric: true });

// 各可排序列的比较器：名称、标签按 zh-CN 语序比较，大小按字节，排序码按数值
const sortComparators: Record<string, (a: any, b: any) => number> = {
	name: (a, b) => compareLocale(a.name || '', b.name || ''),
	sizeInfo: (a, b) => getSizeBytes(a) - getSizeBytes(b),
	tags: (a, b) => compareLocale(a.tags && a.tags.length > 0 ? a.tags[0].name : '', b.tags && a.tags.length > 0 ? b.tags[0].name : ''),
	sort: (a, b) => Number(a.sort ?? 0) - Number(b.sort ?? 0),
};

// 表格参数配置
const gridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	rowConfig: { keyField: 'id' },
	cellConfig: { height: 50 },
	checkboxConfig: { range: true },
	sortConfig: {
		remote: false,
		sortMethod: ({ data, sortList }) => {
			if (!sortList || sortList.length === 0) return data;
			const { field, order } = sortList[0] as any;
			const comparator = sortComparators[field];
			if (!comparator) return data;
			const dir = order === 'desc' ? -1 : 1;
			return [...data].sort((a, b) => dir * comparator(a, b));
		},
	},
	columns: [
		{ type: 'checkbox', width: 50, fixed: 'left' },
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'thumbnail', title: '缩略图', width: 90, slots: { default: 'row_thumbnail' } },
		{ field: 'name', title: '名称', minWidth: 180, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_name' } },
		{ field: 'documentNo', title: '编号', minWidth: 120, showOverflow: 'tooltip' },
		{ field: 'status', title: '状态', width: 100, slots: { default: 'row_status' } },
		{ field: 'sizeInfo', title: '大小', width: 100, sortable: true },
		{ field: 'categoryName', title: '分类', width: 120, showOverflow: 'tooltip', slots: { default: 'row_categoryName' } },
		{ field: 'tags', title: '标签', minWidth: 140, sortable: true, slots: { default: 'row_tags' } },
		{ field: 'createUserName', title: '创建人', width: 100, showOverflow: 'tooltip' },
		{ field: 'createTime', title: '创建时间', width: 150, sortable: true, slots: { default: 'row_createTime' } },
		{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', slots: { default: 'row_buttons' } },
	],
});

const tableContainerRef = ref<HTMLElement>();
const containerHeight = ref(0);
let resizeObserver: ResizeObserver | null = null;

// 计算表格高度
const tableHeight = computed(() => {
	return containerHeight.value > 0 ? containerHeight.value : undefined;
});

// 更新容器高度
const updateHeight = () => {
	nextTick(() => {
		if (tableContainerRef.value) {
			containerHeight.value = tableContainerRef.value.clientHeight;
		}
	});
};

// 监听窗口大小变化
const handleResize = () => {
	updateHeight();
};

onMounted(() => {
	window.addEventListener('resize', handleResize);
	// 等待 DOM 渲染完成后初始化
	nextTick(() => {
		updateHeight();
		// 使用 ResizeObserver 监听容器大小变化
		if (tableContainerRef.value) {
			resizeObserver = new ResizeObserver(() => {
				updateHeight();
			});
			resizeObserver.observe(tableContainerRef.value);
		}
	});
});

onUnmounted(() => {
	window.removeEventListener('resize', handleResize);
	if (resizeObserver) {
		resizeObserver.disconnect();
		resizeObserver = null;
	}
});

// 数据刷新会清空勾选，这里把当前勾选同步给父组件
const gridRef = ref<VxeGridInstance>();
const emitSelection = () => {
	emit(
		'select',
		(gridRef.value?.getCheckboxRecords() ?? []).map((item: any) => item.id)
	);
};

watch(
	() => props.documents,
	async () => {
		await nextTick();
		emitSelection();
	}
);

const handleCommand = (command: string | number, row: any) => {
	switch (command) {
		case 'edit':
			emit('edit', row);
			break;
		case 'download':
			emit('download', row);
			break;
		case 'share':
			emit('share', row);
			break;
		case 'permission':
			emit('permission', row);
			break;
		case 'delete':
			emit('delete', row);
			break;
	}
};

// 处理缩略图点击：没有缩略图时点击图标直接预览文档
const handleThumbnailClick = (row: any) => {
	if (!row.thumbnail) {
		emit('preview', row);
	}
};
</script>

<style lang="scss" scoped>
.document-list {
	flex: 1;
	min-height: 0;
	width: 100%;

	/* 操作列：预览/下载是最高频的两个，平铺出来；其余收进「更多」避免把列撑得太宽 */
	.doc-row-actions {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 2px;
	}
}
</style>
