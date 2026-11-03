<template>
	<el-select v-model="tenantId" placeholder="请选择租户" filterable :clearable="false">
		<el-option v-for="item in state.tenantList" :key="item.id" :value="item.id" :label="item.host ? `${item.name} (${item.host})` : item.name" />
	</el-select>
</template>

<!-- 租户选择 -->
<script lang="ts" setup name="TenantSelect">
import { PropType, computed, reactive, onMounted } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysTenantApi } from '/@/api-services/system/api';

const emit = defineEmits(['update:modelValue']);
const props = defineProps({
	/**
	 * 绑定的租户Id
	 * @example
	 * <tenant-select v-model="value" code="xxxx" />
	 */
	modelValue: {
		type: [String, Number, null] as PropType<string | number | null>,
		default: null,
		required: true,
	},
});

// 租户Id绑定
const tenantId = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

const state = reactive({
	tenantList: [] as Array<any>,
	tenantId: Number as any,
});

onMounted(async () => {
	await getTenantList();
});

// 获取租户列表
const getTenantList = async () => {
	state.tenantList = await getAPI(SysTenantApi)
		.apiSysTenantListGet()
		.then((res) => res.data.result ?? []);
	// 默认第一个租户
	if (state.tenantList.length > 0) emit('update:modelValue', state.tenantList[0].id);
};

// 导出对象
defineExpose({ getTenantList });
</script>

<style lang="scss" scoped>
.el-select {
	--el-select-clear-icon: none;
}
</style>
