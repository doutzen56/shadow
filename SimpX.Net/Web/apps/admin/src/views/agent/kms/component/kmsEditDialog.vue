<template>
	<div class="kms-edit-dialog">
		<el-form :model="form" ref="formRef" :rules="rules" label-width="auto">
			<el-form-item label="知识库名称" prop="name">
				<el-input v-model="form.name" placeholder="请输入知识库名称" maxlength="128" clearable />
			</el-form-item>
			<el-form-item label="矢量化模型" prop="aiModelsId">
				<el-select v-model="form.aiModelsId" placeholder="请选择 Embedding 模型" filterable style="width: 100%">
					<el-option v-for="item in embeddingModels" :key="item.id" :label="`${item.modelName}（${item.endPoint}）`" :value="item.id!" />
				</el-select>
			</el-form-item>
			<el-form-item label="重排模型">
				<el-select v-model="form.aiRerankModelsId" placeholder="可不选；选择后先向量粗排再重排" filterable clearable style="width: 100%">
					<el-option v-for="item in rerankModels" :key="item.id" :label="`${item.modelName}（${item.endPoint}）`" :value="item.id!" />
				</el-select>
			</el-form-item>
			<el-row :gutter="10">
				<el-col :span="8">
					<el-form-item label="段落最大标记">
						<el-input-number v-model="form.maxTokensPerParagraph" :min="50" :max="4000" controls-position="right" style="width: 100%" />
					</el-form-item>
				</el-col>
				<el-col :span="8">
					<el-form-item label="每行最大标记">
						<el-input-number v-model="form.maxTokensPerLine" :min="10" :max="1000" controls-position="right" style="width: 100%" />
					</el-form-item>
				</el-col>
				<el-col :span="8">
					<el-form-item label="段落重叠标记">
						<el-input-number v-model="form.overlappingTokens" :min="0" :max="1000" controls-position="right" style="width: 100%" />
					</el-form-item>
				</el-col>
			</el-row>

			<el-divider content-position="left">知识库文档</el-divider>

			<div class="table-toolbar">
				<el-button icon="ele-Plus" type="primary" plain @click="addDetail('text')"> 添加文本 </el-button>
				<el-button icon="ele-Plus" type="primary" plain @click="addDetail('markdown')"> 添加 Markdown </el-button>
				<el-button icon="ele-Plus" type="primary" plain @click="addDetail('html')"> 添加 HTML </el-button>
				<el-button icon="ele-Upload" type="success" plain @click="triggerUpload"> 上传文件 </el-button>
				<el-button icon="ele-Link" type="warning" plain @click="addDetail('remote')"> 添加远程地址 </el-button>
				<input ref="fileInputRef" type="file" style="display: none" multiple @change="onFilesSelected" />
			</div>

			<vxe-grid v-bind="detailGridOptions" :data="form.aiKmssDetailsList">
				<template #detail_type="{ row }">
					<el-tag size="small">{{ row.fileType || 'text' }}</el-tag>
				</template>
				<template #detail_content="{ row }">
					<!-- 文本类型直接编辑 -->
					<el-input
						v-if="['text', 'markdown', 'html'].includes(row.fileType ?? '')"
						v-model="row.content"
						type="textarea"
						:rows="3"
						:placeholder="row.fileType === 'markdown' ? '请输入 Markdown 内容' : row.fileType === 'html' ? '请输入 HTML 内容' : '请输入文本内容'"
					/>
					<!-- 远程地址 -->
					<el-input v-if="row.fileType === 'remote'" v-model="row.url" placeholder="请输入可访问的文件地址，如 https://example.com/a.pdf" />
					<!-- 文件 -->
					<!-- 同上：fileType 可空，先 ?? '' 再参与 includes 判断 -->
					<span v-if="row.fileId || !['text', 'markdown', 'html', 'remote'].includes(row.fileType ?? '')">
						{{ row.contentName || '未命名文件' }}
						<el-tag size="small" type="success" v-if="row.fileId">已上传</el-tag>
						<el-tag size="small" type="info" v-else>待上传</el-tag>
					</span>
				</template>
				<template #detail_status="{ row }">
					<el-tag size="small" :type="statusTag(row.status)">{{ statusText(row.status) }}</el-tag>
					<el-tooltip v-if="row.errorMessage" :content="row.errorMessage" placement="top">
						<el-icon style="margin-left: 4px"><ele-Warning /></el-icon>
					</el-tooltip>
				</template>
				<template #detail_buttons="{ rowIndex }">
					<el-button icon="ele-Delete" text type="danger" @click="removeDetail(rowIndex)"> 移除 </el-button>
				</template>
				<template #empty>
					<el-empty :image-size="100" description="暂无文档，请至少添加一个" />
				</template>
			</vxe-grid>
			<div class="tip">保存后系统会自动对文档进行解析、分块与向量化（每 5 分钟扫描一次待处理文档）。</div>
		</el-form>

		<div class="dialog-footer is-inline">
			<el-button icon="ele-CircleCloseFilled" @click="emit('close')">取消</el-button>
			<el-button type="primary" icon="ele-CircleCheckFilled" :loading="saving" @click="handleSubmit">确定</el-button>
		</div>
	</div>
</template>

<script lang="ts" setup name="kmsEditDialog">
import { onMounted, reactive, ref } from 'vue';
import { ElMessage, FormInstance } from 'element-plus';
import { VxeGridProps } from 'vxe-table';

import { getAPI } from '/@/utils/axios-utils';
import { AgentKmsApi, AgentModelApi, AgentFileApi } from '/@/api-services/agent/api';
import { AgentKmsDto, AgentKmsDetailDto, AgentModelDto } from '/@/api-services/agent/models';

const props = defineProps<{ kmsId?: number }>();
const emit = defineEmits(['close', 'saved']);

const formRef = ref<FormInstance>();
const fileInputRef = ref<HTMLInputElement>();
const saving = ref(false);
const embeddingModels = ref<AgentModelDto[]>([]);
const rerankModels = ref<AgentModelDto[]>([]);

// 知识库文档列表（行内可编辑；不使用固定列，保证行高能随行内文本域自适应撑开）
const detailGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	maxHeight: 400,
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 60 },
		{ field: 'fileType', title: '类型', width: 100, slots: { default: 'detail_type' } },
		{ field: 'content', title: '内容', minWidth: 320, slots: { default: 'detail_content' } },
		{ field: 'status', title: '状态', width: 110, slots: { default: 'detail_status' } },
		{ field: 'buttons', title: '操作', width: 'auto', slots: { default: 'detail_buttons' } },
	],
});

/** 表单即实体：切片参数给的是常用默认值，新增时无需用户填写即可直接保存 */
const form = reactive({
	name: '',
	maxTokensPerParagraph: 299,
	maxTokensPerLine: 99,
	overlappingTokens: 49,
	aiModelsId: undefined as number | undefined,
	aiRerankModelsId: undefined as number | undefined,
	aiKmssDetailsList: [] as AgentKmsDetailDto[],
}) as Partial<AgentKmsDto>;

const rules = {
	name: [{ required: true, message: '请输入知识库名称', trigger: 'blur' }],
	aiModelsId: [{ required: true, message: '请选择矢量化模型', trigger: 'change' }],
};

/** 文档向量化状态：1=已完成 2=失败 其余（0）=待处理，由后端每 5 分钟扫描推进 */
const statusText = (status?: number) => (status === 1 ? '已完成' : status === 2 ? '失败' : '待处理');
const statusTag = (status?: number) => (status === 1 ? 'success' : status === 2 ? 'danger' : 'warning');

/** 依据后缀映射后端支持的文件类型 */
const mapFileType = (fileName: string) => {
	const ext = (fileName.split('.').pop() || '').toLowerCase();
	if (['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp', 'tiff', 'tif'].includes(ext)) return 'image';
	if (ext === 'pdf') return 'pdf';
	if (['doc', 'docx'].includes(ext)) return 'word';
	if (['xls', 'xlsx', 'xlsm', 'xlsb'].includes(ext)) return 'excel';
	if (['html', 'htm'].includes(ext)) return 'html';
	if (['md', 'markdown'].includes(ext)) return 'markdown';
	return 'text';
};

/** 新增一条文档：text/markdown/html 直接编辑正文，remote 填 URL，文件走上传（fileType 由后缀推断） */
const addDetail = (type: string) => {
	form.aiKmssDetailsList!.push({
		fileType: type,
		content: '',
		url: '',
		contentName: '',
		status: 0,
	} as AgentKmsDetailDto);
};

const removeDetail = (index: number) => {
	form.aiKmssDetailsList!.splice(index, 1);
};

const triggerUpload = () => fileInputRef.value?.click();

const onFilesSelected = async (event: Event) => {
	const input = event.target as HTMLInputElement;
	const files = Array.from(input.files ?? []);
	input.value = '';
	for (const file of files) {
		await uploadOneFile(file);
	}
};

/** 上传单个文件：先落 SysFile 拿到 Id，再把文件信息作为一条文档挂到当前知识库（保存时由后端解析向量化） */
const uploadOneFile = async (file: File) => {
	const res = await getAPI(AgentFileApi).apiAgentFileUploadPostForm(file, 'AgentKmsDetail', 'KmsDoc', 0);
	const sysFile = res.data.result;
	const fileName = `${sysFile?.fileName ?? ''}${sysFile?.suffix ?? ''}`;
	form.aiKmssDetailsList!.push({
		fileId: sysFile?.id,
		fileType: mapFileType(fileName),
		contentName: fileName,
		url: sysFile?.url ?? '',
		content: '',
		status: 0,
	} as AgentKmsDetailDto);
	ElMessage.success(`文件「${fileName}」上传成功`);
};

const handleSubmit = async () => {
	await formRef.value?.validate();
	if (!form.aiKmssDetailsList || form.aiKmssDetailsList.length === 0) {
		ElMessage.warning('请至少添加一个知识库文档');
		return;
	}
	saving.value = true;
	try {
		await getAPI(AgentKmsApi).apiAgentKmsAddEditPost({ ...form } as AgentKmsDto);
		ElMessage.success('保存成功');
		emit('saved');
	} finally {
		saving.value = false;
	}
};

onMounted(async () => {
	const embeddingRes = await getAPI(AgentModelApi).apiAgentModelListGet(2);
	embeddingModels.value = embeddingRes.data.result ?? [];
	const rerankRes = await getAPI(AgentModelApi).apiAgentModelListGet(4);
	rerankModels.value = rerankRes.data.result ?? [];

	if (props.kmsId) {
		const res = await getAPI(AgentKmsApi).apiAgentKmsDetailGet(props.kmsId);
		const detail = res.data.result;
		if (!detail) return;
		form.id = detail.id;
		form.name = detail.name ?? '';
		form.maxTokensPerParagraph = detail.maxTokensPerParagraph ?? 299;
		form.maxTokensPerLine = detail.maxTokensPerLine ?? 99;
		form.overlappingTokens = detail.overlappingTokens ?? 49;
		form.aiModelsId = detail.aiModelsId;
		form.aiRerankModelsId = detail.aiRerankModelsId;
		form.aiKmssDetailsList = detail.aiKmssDetailsList ?? [];
	}
});
</script>

<style lang="scss" scoped>
/* vxe 会按行高规格（mini 36px）给单元格写死高度，而行内文本域约 64px，
   不放开高度整行会被裁剪 */
:deep(.vxe-cell) {
	height: auto !important;
}

/* 表单项统一行距 */
.el-form :deep(.el-form-item) {
	margin-bottom: 18px;
}

/*
 * 与 appEditDialog 同一个坑：theme/element.scss 的
 *   .el-form .el-form-item:last-of-type { margin-bottom: 0 !important }
 * 是后代选择器，栅格里每个 el-col 只放一个表单项，于是全部命中 :last-of-type，
 * 三列参数那一行的下边距被清零、与下方内容贴死。这里用更长一级的选择器反压。
 */
.el-form :deep(.el-col > .el-form-item:last-of-type) {
	margin-bottom: 18px !important;
}
</style>
