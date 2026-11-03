<template>
	<el-select v-model="regWayId" placeholder="请选择注册方案" filterable :clearable="false">
		<el-option v-for="item in state.regWayList" :key="item.id" :value="item.id" :label="`${item.tenantName} (${item.name})`" />
	</el-select>
</template>

<!-- 注册方案选择 -->
<script lang="ts" setup name="TenantSelect">
import { PropType, computed, reactive, onMounted } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserRegWayApi } from '/@/api-services/system/api';

const emit = defineEmits(['update:modelValue']);
const props = defineProps({
	/**
	 * 绑定的注册方案Id
	 * @example
	 * <tenant-select v-model="value" code="xxxx" />
	 */
	modelValue: {
		type: [String, Number, null] as PropType<string | number | null>,
		default: null,
		required: true,
	},
});

// 注册方案Id绑定
const regWayId = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

const state = reactive({
	regWayList: [] as Array<any>,
	regWayId: Number as any,
});

onMounted(async () => {
	await getRegWayList();
});

// 获取注册方案列表
const getRegWayList = async () => {
	state.regWayList = await getAPI(SysUserRegWayApi)
		.apiSysUserRegWayListPost()
		.then((res) => res.data.result ?? []);
	// 默认第一个注册方案
	if (state.regWayList.length > 0) emit('update:modelValue', state.regWayList[0].id);
};

// 导出对象
defineExpose({ getRegWayList });
</script>

<style lang="scss" scoped>
.el-select {
	--el-select-clear-icon: none;
}
</style>
