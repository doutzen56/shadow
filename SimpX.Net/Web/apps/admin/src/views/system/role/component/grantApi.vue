<template>
	<div class="ga-container">
		<el-drawer v-model="state.isVisible" size="40%" class="ga-drawer">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Link /></el-icon>
					<span>{{ state.drawerTitle }}</span>
				</div>
			</template>

			<NoticeBar leftIcon="iconfont icon-tongzhi2" text="非超管角色禁止授权系统核心模块接口资源" :scrollable="true" />

			<el-card class="ga-card" shadow="hover">
				<template #header>
					<div class="ga-head">
						<el-input v-model="filterText" prefix-icon="ele-Search" clearable placeholder="API路由" />
					</div>
				</template>
				<!-- 抽屉里唯一的滚动容器：外层不滚，只在树内上下滚 -->
				<div class="ga-tree" v-loading="state.loading">
					<el-tree
						ref="treeRef"
						class="filter-tree"
						:data="state.allApiData"
						:filter-node-method="filterNode"
						node-key="route"
						:props="{ children: 'children', label: 'desc' }"
						show-checkbox
						icon="ele-Menu"
						highlight-current
					>
						<template #default="{ node, data }">
							<span v-if="node.level == 1 || node.level == 2">{{ node.label }}</span>
							<span v-if="node.level == 3">
								<el-icon style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Link /></el-icon>
								{{ node.label }} 【{{ node.key }}】
								<el-tag type="primary" v-if="data.httpMethod === 'GET'">GET</el-tag>
								<el-tag type="success" v-else-if="data.httpMethod === 'POST'">POST</el-tag>
								<el-tag type="warning" v-else-if="data.httpMethod === 'PUT'">PUT</el-tag>
								<el-tag type="danger" v-else-if="data.httpMethod === 'DELETE'">DELETE</el-tag>
								<el-tag type="info" v-else>{{ data.httpMethod }}</el-tag>
							</span>
						</template>
					</el-tree>
				</div>
			</el-card>

			<template #footer>
				<div class="ga-foot">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
				</div>
			</template>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup>
import { reactive, onMounted, ref, watch } from 'vue';
import type { ElTree } from 'element-plus';
import NoticeBar from '/@/components/noticeBar/index.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysCommonApi, SysRoleApi } from '/@/api-services/system/api';

const filterText = ref('');
const treeRef = ref<InstanceType<typeof ElTree>>();
const state = reactive({
	loading: false,
	isVisible: false,
	allApiData: [] as any,
	ownApiList: [] as any,
	roleId: 0,
	selectRow: [] as any,
	drawerTitle: '',
	selectedTabName: 0,
});

// 页面初始化
onMounted(() => {
	initTreeData();
});

// 节点过滤
watch(filterText, (val) => {
	treeRef.value!.filter(val);
});

// 初始化树
const initTreeData = async () => {
	state.loading = true;
	var res = await getAPI(SysCommonApi).apiSysCommonAllSysApiListGet();
	var tData = res.data.result ?? [];
	treeRef.value?.setCheckedKeys([]); // 清空选中值
	state.allApiData = tData;
	state.loading = false;
};

// 打开页面
const openDrawer = async (row: any) => {
	state.selectedTabName = 0;
	state.roleId = row.id;
	state.drawerTitle = `设置角色接口黑名单 - ${row.name}`;

	state.loading = true;
	// 获取已有接口资源
	var res1 = await getAPI(SysRoleApi).apiSysRoleRoleApiListGet(state.roleId);
	state.ownApiList = res1.data.result ?? [];

	setTimeout(() => {
		treeRef.value?.setCheckedKeys(state.ownApiList ?? []);
	}, 200);

	state.loading = false;
	state.isVisible = true;
};

// 节点过滤
const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.route.includes(value);
};

// 取消
const cancel = () => {
	state.isVisible = false;
};

// 授权角色接口资源
const submit = async () => {
	state.ownApiList = treeRef.value?.getCheckedKeys(true) as Array<string>;
	await getAPI(SysRoleApi).apiSysRoleGrantApiPost({ id: state.roleId, apiList: state.ownApiList });
	cancel();
};

// 导出对象
defineExpose({ openDrawer });
</script>

<!-- 抽屉内容由 el-drawer 挂到 body 上，scoped 的 data-v 会断链，这里必须写全局样式 -->
<style lang="scss">
.ga-drawer {
	/* 外层整列 flex 且禁止滚动：滚动条只留给里面的树 */
	.el-drawer__body {
		display: flex;
		flex-direction: column;
		gap: 10px;
		min-height: 0;
		padding: 12px 16px;
		overflow: hidden;
	}

	.el-drawer__footer {
		padding: 10px 16px;
	}
}

/* 卡片吃掉抽屉剩余高度，再把剩余高度交给树 */
.ga-card {
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;

	.el-card__header {
		flex: 0 0 auto;
		padding: 10px 12px;
	}

	.el-card__body {
		flex: 1 1 auto;
		min-height: 0;
		overflow: hidden;
		padding: 6px 8px 8px;
	}
}

/* 查询框左右占满 */
.ga-head {
	display: flex;
	min-width: 0;

	.el-input {
		width: 100%;
	}
}

.ga-tree {
	height: 100%;
	min-height: 0;
	overflow: auto;
	padding-right: 2px;
}

.ga-foot {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
}
</style>
