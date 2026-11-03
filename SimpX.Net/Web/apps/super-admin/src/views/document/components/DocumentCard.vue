<template>
	<!-- 滚动容器复用全站 .card-grid（page.scss）：它自带 flex:1 / overflow-y / padding，
	     没有那层 padding，第一排卡片 hover 上浮 4px 会被容器裁掉顶部 -->
	<div class="card-grid" v-loading="loading">
		<el-row :gutter="12" class="card-grid-row">
			<el-col v-for="document in documents" :key="document.id" :xs="24" :sm="12" :md="8" :lg="8" :xl="6">
				<el-card
					class="entity-card is-primary doc-card"
					:class="{ 'is-selected': selectedDocuments.includes(document.id) }"
					shadow="never"
					@click="handleCardClick(document)"
				>
					<div class="entity-card__head">
						<!-- 勾选框：批量操作是主路径，常显但弱化；阻止冒泡避免勾选时触发预览 -->
						<div class="doc-card__check" @click.stop>
							<el-checkbox :model-value="selectedDocuments.includes(document.id)" @change="handleCheckboxChange(document, $event)" />
						</div>
						<div class="entity-card__icon">
							<el-icon :size="19"><component :is="iconOf(document)" /></el-icon>
						</div>
						<div class="entity-card__title-wrap">
							<div class="entity-card__title" :title="document.name">{{ document.name || '未命名文档' }}</div>
							<div class="entity-card__sub">{{ documentTypeText(document.documentType) }} · {{ document.sizeInfo || '—' }}</div>
						</div>
						<el-tag :type="documentStatusTag(document.status)" size="small" effect="light">
							{{ documentStatusText(document.status) }}
						</el-tag>
					</div>

					<div class="entity-card__body">
						<div class="doc-card__preview">
							<el-image
								v-if="document.thumbnail"
								:src="document.thumbnail"
								fit="cover"
								lazy
								class="doc-card__preview-img"
								:preview-src-list="[document.thumbnail]"
								:preview-teleported="true"
							/>
							<div v-else class="doc-card__preview-icon">
								<FileIcons :name="document.name" :width="52" :height="52" />
							</div>
						</div>
						<div class="doc-card__tags">
							<el-tag v-if="document.categoryName" :color="document.categoryColor || '#409EFF'" effect="dark" size="small">
								{{ document.categoryName }}
							</el-tag>
							<el-tag v-for="tag in (document.tags || []).slice(0, 3)" :key="tag.id" :color="tag.color || '#409EFF'" effect="dark" size="small">
								{{ tag.name }}
							</el-tag>
						</div>
					</div>

					<div class="entity-card__meta">
						<span v-if="document.documentNo">编号 {{ document.documentNo }}</span>
						<span>{{ document.createUserName || '—' }}</span>
					</div>

					<div class="entity-card__foot">
						<span class="doc-muted">{{ formatDate(document.createTime) }}</span>
						<div class="entity-card__actions">
							<el-button link type="primary" size="small" @click.stop="$emit('preview', document)">预览</el-button>
							<el-button link type="primary" size="small" @click.stop="$emit('download', document)">下载</el-button>
							<el-button link type="primary" size="small" @click.stop="$emit('share', document)">分享</el-button>
							<el-button link type="primary" size="small" @click.stop="$emit('permission', document)">权限</el-button>
							<el-button link type="danger" size="small" @click.stop="$emit('delete', document)">删除</el-button>
						</div>
					</div>
				</el-card>
			</el-col>
		</el-row>

		<el-empty v-if="!loading && documents.length === 0" :image-size="140" description="暂无文档，点击「上传」添加" />
	</div>
</template>

<script lang="ts" setup>
import FileIconsModule from 'file-icons-vue';
import '/@/views/document/styles/document.scss';
import { documentStatusTag, documentStatusText, documentTypeText, documentIconByName, documentTypeIcon, formatDate } from '../utils/documentMeta';

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
}>();

// 类型图标：后端给了 documentType 就用它，没有则按文件名后缀兜底
const iconOf = (document: any) => (document.documentType ? documentTypeIcon(document.documentType) : documentIconByName(document.name));

// 点击卡片预览
const handleCardClick = (document: any) => {
	emit('preview', document);
};

// 勾选/取消勾选：复制一份再改，不直接改 props
const handleCheckboxChange = (document: any, checked: boolean) => {
	const currentIds = [...(props.selectedDocuments || [])];
	if (checked) {
		if (!currentIds.includes(document.id)) currentIds.push(document.id);
	} else {
		const index = currentIds.indexOf(document.id);
		if (index > -1) currentIds.splice(index, 1);
	}
	emit('select', currentIds);
};
</script>
