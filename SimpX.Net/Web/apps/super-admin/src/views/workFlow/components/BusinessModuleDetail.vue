<template>
	<div v-if="businessModule && hasBusinessId" class="business-module-detail">
		<el-divider>业务模块详情</el-divider>
		<el-skeleton :loading="loading" animated>
			<template #default>
				<div v-if="error" class="error-message">
					<el-alert :title="error" type="error" :closable="false" />
				</div>
				<component v-else-if="detailComponent && detailData" :is="detailComponent" :detail="detailData" />
			</template>
		</el-skeleton>
	</div>
</template>

<script setup lang="ts" name="BusinessModuleDetail">
import { computed, ref, shallowRef, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { getBusinessModuleComponent, getBusinessModuleApi, isBusinessModuleSupported } from '../utils/businessModuleConfig';

const props = defineProps<{
	businessModule?: string;
	businessId?: string | number;
}>();

const loading = ref(false);
const error = ref('');
const detailComponent = shallowRef<any>(null);
const detailData = ref<any>(null);
let currentRequestId = 0;

const hasBusinessId = computed(() => props.businessId !== undefined && props.businessId !== null && props.businessId !== '');

const loadBusinessDetail = async () => {
	const requestId = ++currentRequestId;

	if (!props.businessModule || !hasBusinessId.value) {
		return;
	}

	// 检查业务模块是否支持
	if (!isBusinessModuleSupported(props.businessModule)) {
		error.value = `暂不支持业务模块：${props.businessModule}`;
		console.warn('业务模块详情：不支持的模块', props.businessModule);
		return;
	}

	loading.value = true;
	error.value = '';
	detailComponent.value = null;
	detailData.value = null;

	try {
		// 动态加载组件
		const componentLoader = getBusinessModuleComponent(props.businessModule);
		if (componentLoader) {
			const component = await componentLoader();
			if (requestId !== currentRequestId) return;
			detailComponent.value = component.default || component;
		}

		// 动态调用API获取数据
		const apiFunction = getBusinessModuleApi(props.businessModule);
		if (apiFunction) {
			const res = await apiFunction({ businessId: Number(props.businessId) });
			if (requestId !== currentRequestId) return;
			// 兼容不同的数据结构
			detailData.value = res.data?.result || res.data?.data || res.data;
		}
	} catch (err: any) {
		if (requestId !== currentRequestId) return;
		console.error('加载业务模块详情失败:', err);
		error.value = err?.message || '获取业务模块详情失败';
		ElMessage.error(error.value);
	} finally {
		if (requestId === currentRequestId) {
			loading.value = false;
		}
	}
};

// 监听 props 变化
watch(
	() => [props.businessModule, props.businessId],
	() => {
		loadBusinessDetail();
	},
	{ immediate: true }
);
</script>

<style scoped>
.business-module-detail {
	margin-top: 20px;
}

.error-message {
	margin: 20px 0;
}
</style>
