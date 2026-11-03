<template>
	<div class="document-share-view-container">
		<div v-loading="state.loading" class="share-content">
			<!-- 密码验证 -->
			<el-card v-if="!state.verified && !state.error" class="verify-card">
				<template #header>
					<h2>访问分享文档</h2>
				</template>
				<div v-if="state.loading" style="text-align: center; padding: 20px">
					<el-icon class="is-loading"><ele-Loading /></el-icon>
					<p style="margin-top: 10px">正在检查分享信息...</p>
				</div>
				<el-form v-else :model="state.verifyForm" label-width="100px" style="max-width: 400px; margin: 0 auto">
					<el-form-item label="分享码">
						<el-input v-model="state.shareCode" disabled />
					</el-form-item>
					<el-form-item v-if="state.hasPassword" label="分享密码">
						<el-input v-model="state.verifyForm.password" type="password" placeholder="请输入分享密码" show-password @keyup.enter="handleVerify" />
					</el-form-item>
					<el-form-item>
						<el-button type="primary" @click="handleVerify" :loading="state.verifying">
							{{ state.hasPassword ? '验证并访问' : '访问' }}
						</el-button>
					</el-form-item>
				</el-form>
			</el-card>

			<!-- 文档详情 -->
			<el-card v-if="state.verified && state.document" class="document-card">
				<template #header>
					<div class="card-header">
						<h2>{{ state.document.name }}</h2>
						<div class="header-actions">
							<el-button v-if="state.shareInfo?.sharePermission === 2" type="primary" icon="ele-Download" @click="handleDownload">下载</el-button>
						</div>
					</div>
				</template>

				<div class="document-card__body">
					<!-- 文档信息 -->
					<div class="document-info">
						<el-descriptions :column="2" border>
							<el-descriptions-item label="文档名称">{{ state.document.name }}</el-descriptions-item>
							<el-descriptions-item label="文档类型">{{ getDocumentTypeName(state.document.documentType) }}</el-descriptions-item>
							<el-descriptions-item label="文件大小">{{ state.document.sizeInfo }}</el-descriptions-item>
							<el-descriptions-item label="创建时间">{{ formatDateTime(state.document.createTime) }}</el-descriptions-item>
							<el-descriptions-item label="分享权限" :span="2">
								<el-tag v-if="state.shareInfo?.sharePermission === 1" type="info">仅查看</el-tag>
								<el-tag v-else-if="state.shareInfo?.sharePermission === 2" type="success">可下载</el-tag>
							</el-descriptions-item>
							<el-descriptions-item label="访问次数" :span="2">{{ state.shareInfo?.accessCount || 0 }} 次</el-descriptions-item>
						</el-descriptions>
					</div>

					<!-- 文档预览 -->
					<div v-if="previewUrl || previewFile" class="document-preview">
						<h3>文档预览</h3>
						<OnlineFilePreview :url="previewUrl" :file="previewFile" class="document-preview__viewer" />
					</div>
					<div v-else-if="state.document.fileUrl" class="document-preview document-preview--empty">
						<el-alert type="info" :closable="false" title="暂无法加载预览地址" />
					</div>
				</div>
			</el-card>

			<!-- 错误提示 -->
			<el-card v-if="state.error" class="error-card">
				<el-alert type="error" :title="state.error" :closable="false" />
			</el-card>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentShareApi } from '/@/api-services/document/api';
import { reactive, onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { formatDateTime } from '/@/utils/format';
import { downloadStreamFile } from '/@/utils/download';
import { resolveShareDocumentPreviewUrl } from '/@/utils/documentPreview';
import { NextLoading } from '/@/utils/loading';
import OnlineFilePreview from '/@/components/file/OnlineFilePreview.vue';

const route = useRoute();

const state = reactive({
	loading: false,
	verifying: false,
	verified: false,
	hasPassword: false,
	shareCode: '',
	shareInfo: null as any,
	document: null as any,
	error: '',
	accessPassword: '',
	verifyForm: {
		password: '',
	},
});

const previewUrl = ref('');
const previewFile = ref<File | undefined>();

const clearPreview = () => {
	previewUrl.value = '';
	previewFile.value = undefined;
};

const applyPreviewUrl = (doc: any) => {
	clearPreview();
	const result = resolveShareDocumentPreviewUrl({
		fileUrl: doc?.fileUrl,
		preUrl: doc?.preUrl,
		name: doc?.name,
		suffix: doc?.suffix,
		fileExt: doc?.fileExt,
	});
	previewUrl.value = result.url || '';
	previewFile.value = result.file;
};

const getDocumentTypeName = (type: any) => {
	const typeNum = typeof type === 'number' ? type : parseInt(type);
	const typeMap: Record<number, string> = {
		1: 'Word',
		2: 'Excel',
		3: 'PDF',
		4: 'PPT',
		5: 'TXT',
		6: '视频',
		7: '音频',
		8: '图片',
		9: 'ZIP',
		10: 'RAR',
		99: '其他',
	};
	return typeMap[typeNum] || '未知';
};

/** 使用分享码（及密码）访问文档 */
const accessDocument = async (password?: string) => {
	const res = await getAPI(DocumentShareApi).apiDocumentShareDocumentByCodePost({
		shareCode: state.shareCode,
		password: password || undefined,
	});
	const document = res.data?.result;
	// 业务异常（密码错误、链接失效等）已由请求拦截器提示
	if (res.data?.code !== 200 || !document) return;

	state.document = document;
	state.shareInfo = document.shareInfo || state.shareInfo;
	state.hasPassword = !!state.shareInfo?.hasPassword;
	state.accessPassword = password || '';
	state.verified = true;
	applyPreviewUrl(document);
};

/** 读取分享公开信息，无密码时直接访问 */
const loadShareInfo = async () => {
	state.loading = true;
	try {
		const res = await getAPI(DocumentShareApi).apiDocumentShareInfoGet(state.shareCode);
		const shareInfo = res.data?.result;
		if (res.data?.code !== 200 || !shareInfo) {
			state.error = res.data?.message || '分享链接不存在或已失效';
			return;
		}

		state.shareInfo = shareInfo;
		state.hasPassword = !!shareInfo.hasPassword;
		if (!state.hasPassword) await accessDocument();
	} catch (error: any) {
		state.error = error?.response?.data?.message || error?.message || '加载分享信息失败';
	} finally {
		state.loading = false;
	}
};

const handleVerify = async () => {
	if (state.hasPassword && !state.verifyForm.password) {
		ElMessage.warning('请输入分享密码');
		return;
	}

	state.verifying = true;
	try {
		await accessDocument(state.verifyForm.password || undefined);
	} finally {
		state.verifying = false;
	}
};

const handleDownload = async () => {
	if (state.shareInfo?.sharePermission !== 2) {
		ElMessage.warning('该分享链接不允许下载');
		return;
	}

	try {
		const res = await getAPI(DocumentShareApi).apiDocumentShareDownloadPost(
			{
				shareCode: state.shareCode,
				password: state.accessPassword || undefined,
			},
			{ responseType: 'blob' }
		);
		if (await downloadStreamFile(res, state.document?.name || `document_${state.shareInfo?.documentId ?? ''}`)) ElMessage.success('开始下载');
	} catch (error: any) {
		ElMessage.error(error?.response?.data?.message || error?.message || '下载失败');
	}
};

/** 切换分享码时重置页面状态 */
const initShare = (shareCode?: string) => {
	clearPreview();
	state.loading = false;
	state.verifying = false;
	state.verified = false;
	state.hasPassword = false;
	state.shareInfo = null;
	state.document = null;
	state.error = '';
	state.accessPassword = '';
	state.verifyForm.password = '';

	const code = typeof shareCode === 'string' ? shareCode.trim() : '';
	if (!code) {
		state.error = '分享码不能为空';
		return;
	}
	state.shareCode = code;
	loadShareInfo();
};

onMounted(() => {
	NextLoading.done(600);
	initShare(route.params.shareCode as string);
});

// 同一个页签内切换到其他分享链接时重新加载
watch(
	() => route.params.shareCode,
	(code) => initShare(code as string)
);

onBeforeUnmount(() => {
	clearPreview();
});
</script>

<style lang="scss" scoped>
.document-share-view-container {
	height: 100vh;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	background-color: #f5f5f5;
	padding: 16px 24px;
	overflow: hidden;

	.share-content {
		flex: 1;
		display: flex;
		flex-direction: column;
		width: 100%;
		max-width: 1600px;
		margin: 0 auto;
		min-height: 0;
	}

	.verify-card {
		flex-shrink: 0;
		margin-bottom: 16px;

		h2 {
			margin: 0;
			text-align: center;
		}
	}

	.document-card {
		flex: 1;
		display: flex;
		flex-direction: column;
		min-height: 0;

		:deep(.el-card__body) {
			flex: 1;
			display: flex;
			flex-direction: column;
			min-height: 0;
			padding: 16px 20px 20px;
			overflow: hidden;
		}

		.card-header {
			display: flex;
			justify-content: space-between;
			align-items: center;

			h2 {
				margin: 0;
				font-size: 18px;
			}
		}

		&__body {
			flex: 1;
			display: flex;
			flex-direction: column;
			min-height: 0;
		}
	}

	.document-info {
		flex-shrink: 0;
		margin-bottom: 16px;
	}

	.document-preview {
		flex: 1;
		display: flex;
		flex-direction: column;
		min-height: 0;

		h3 {
			flex-shrink: 0;
			margin: 0 0 12px;
			font-size: 16px;
		}

		&--empty {
			flex: 0;
		}

		&__viewer {
			flex: 1;
			min-height: 0;
			height: calc(100vh - 300px);
			border: 1px solid var(--el-border-color);
			border-radius: 4px;
			overflow: hidden;

			:deep(.online-file-preview) {
				height: 100%;
				min-height: 100%;
			}

			:deep(.online-file-preview__viewer) {
				height: 100%;
			}

			:deep(.file-viewer),
			:deep(.viewer-stage) {
				height: 100% !important;
				min-height: 100%;
			}

			:deep(.content) {
				height: calc(100% - 48px) !important;
				min-height: 400px;
			}
		}
	}

	.error-card {
		flex-shrink: 0;
		margin-top: 16px;
	}
}
</style>
