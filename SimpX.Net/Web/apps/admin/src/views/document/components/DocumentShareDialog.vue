<template>
	<el-dialog v-model="visible" title="分享文档" width="700px" draggable @close="handleClose">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Share /> </el-icon>
				<span> 分享文档 </span>
			</div>
		</template>

		<el-form :model="form" label-width="auto" label-position="top">
			<el-form-item label="有效期">
				<el-select v-model="form.validDays" placeholder="请选择有效期" style="width: 100%">
					<el-option label="1天" :value="1" />
					<el-option label="7天" :value="7" />
					<el-option label="30天" :value="30" />
					<el-option label="永久" :value="0" />
				</el-select>
			</el-form-item>

			<el-form-item label="分享密码">
				<el-input v-model="form.password" type="password" placeholder="可选，设置后访问需要密码" clearable show-password />
			</el-form-item>

			<el-form-item label="分享权限">
				<el-radio-group v-model="form.sharePermission">
					<el-radio :label="1" :value="1">仅查看</el-radio>
					<el-radio :label="2" :value="2">可下载</el-radio>
				</el-radio-group>
			</el-form-item>

			<el-form-item v-if="shareInfoList.length > 0" label="分享链接">
				<vxe-grid v-bind="shareGridOptions" :data="shareInfoList">
					<template #share_url="{ row }">
						<el-input v-model="row.shareUrl" readonly>
							<template #append>
								<el-button @click="handleCopyLink(row.shareUrl)">复制</el-button>
							</template>
						</el-input>
					</template>
					<template #share_expireTime="{ row }">
						<span v-if="row.expireTime">{{ formatDateTime(row.expireTime) }}</span>
						<span v-else>永久有效</span>
					</template>
					<template #share_buttons="{ row }">
						<el-button link type="danger" icon="ele-RefreshLeft" @click="handleCancelShare(row)">取消</el-button>
					</template>
				</vxe-grid>
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button icon="ele-CircleCloseFilled" @click="handleClose">取消</el-button>
			<el-button type="primary" icon="ele-CircleCheckFilled" @click="handleCreateShare" :loading="creating">生成分享</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { getAPI } from '/@/utils/axios-utils';
import { DocumentShareApi } from '/@/api-services/document/api';
import { ref, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridProps } from 'vxe-table';
import { resolveShareLink } from '/@/utils/documentShareUrl';
import { formatDateTime } from '/@/utils/format';

const visible = ref(false);
const creating = ref(false);
const documentIds = ref<number[]>([]);
const shareInfoList = ref<any[]>([]);

// 分享链接表格
const shareGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'shareCode', title: '分享码', width: 150, showOverflow: 'tooltip' },
		{ field: 'shareUrl', title: '分享链接', minWidth: 200, slots: { default: 'share_url' } },
		{ field: 'accessCount', title: '访问次数', width: 90 },
		{ field: 'expireTime', title: '过期时间', width: 150, showOverflow: 'tooltip', slots: { default: 'share_expireTime' } },
		{ field: 'buttons', title: '操作', width: 'auto', slots: { default: 'share_buttons' } },
	],
});

const form = reactive({
	validDays: 7,
	password: '',
	sharePermission: 1,
});

const open = async (ids: number[]) => {
	documentIds.value = ids;
	visible.value = true;
	shareInfoList.value = [];
	form.validDays = 7;
	form.password = '';
	form.sharePermission = 1;
};

const handleClose = () => {
	visible.value = false;
	documentIds.value = [];
	shareInfoList.value = [];
};

const handleCreateShare = async () => {
	if (documentIds.value.length === 0) {
		ElMessage.warning('请选择要分享的文档');
		return;
	}

	creating.value = true;
	try {
		const res = await getAPI(DocumentShareApi).apiDocumentShareBatchCreatePost({
			documentIds: documentIds.value,
			validDays: form.validDays,
			password: form.password || undefined,
			sharePermission: form.sharePermission,
		});

		// 处理响应格式：可能是 res.data 或 res.data.result
		const shareList = Array.isArray(res.data) ? res.data : Array.isArray(res.data?.result) ? res.data.result : [];

		shareList.forEach((share: any) => {
			share.shareUrl = resolveShareLink(share);
		});

		shareInfoList.value = shareList;

		if (shareList.length > 0) {
			ElMessage.success(`成功生成 ${shareList.length} 个分享链接`);
		} else {
			ElMessage.warning('未能生成分享链接，请检查文档是否存在');
		}
	} catch (error: any) {
		console.error('生成分享链接失败:', error);
		ElMessage.error(error?.response?.data?.message || '生成分享链接失败');
	} finally {
		creating.value = false;
	}
};

const handleCopyLink = (url: string) => {
	if (!url) {
		ElMessage.warning('分享链接为空');
		return;
	}

	navigator.clipboard
		.writeText(url)
		.then(() => {
			ElMessage.success('链接已复制到剪贴板');
		})
		.catch(() => {
			// 降级方案
			const textarea = document.createElement('textarea');
			textarea.value = url;
			document.body.appendChild(textarea);
			textarea.select();
			document.execCommand('copy');
			document.body.removeChild(textarea);
			ElMessage.success('链接已复制到剪贴板');
		});
};

const handleCancelShare = async (share: any) => {
	try {
		await ElMessageBox.confirm('确定要取消该分享链接吗？', '提示', {
			type: 'warning',
		});

		await getAPI(DocumentShareApi).apiDocumentShareCancelPost({ id: share.id });
		shareInfoList.value = shareInfoList.value.filter((s) => s.id !== share.id);
		ElMessage.success('已取消分享');
	} catch (error: any) {
		if (error !== 'cancel') {
			console.error('取消分享失败:', error);
			ElMessage.error('取消分享失败');
		}
	}
};

defineExpose({
	open,
});
</script>

<style lang="scss" scoped>
:deep(.el-dialog__body) {
	padding: 20px;
}
</style>
