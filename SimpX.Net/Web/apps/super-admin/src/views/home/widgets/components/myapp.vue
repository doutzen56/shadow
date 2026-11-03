<template>
	<el-card class="myapp-card" shadow="hover">
		<template #header>
			<el-icon><ele-Guide /></el-icon>
			<span>快捷入口</span>
		</template>
		<ul class="myMods">
			<li v-for="mod in myMods" :key="mod.id!">
				<router-link :to="{ path: mod.path! }" :title="mod.path">
					<div class="mod-body">
						<div class="icon-wrapper">
							<SvgIcon :name="mod.meta?.icon" style="font-size: 16px" />
						</div>
						<div class="mod-info">
							<span class="mod-title">{{ mod.meta?.title }}</span>
							<span class="myMods-dir">{{ dirOf(mod) }}</span>
						</div>
					</div>
				</router-link>
			</li>
			<li class="modItem-add" @click="addMods">
				<a>
					<el-icon><ele-Plus /></el-icon>
				</a>
			</li>
		</ul>
		<div v-if="!myMods.length" class="myMods-empty">{{ '还没有快捷入口，点右侧 + 添加' }}</div>

		<MyappEdit v-model="modsDrawer" />
	</el-card>
</template>

<script lang="ts">
export default {
	title: '快捷入口',
	icon: 'ele-Guide',
	description: '可以配置的快捷入口',
};
</script>

<!-- 快捷入口 -->
<script setup lang="ts" name="myapp">
import { computed, onMounted, ref } from 'vue';
import { useQuickEntry } from '/@/stores/quickEntry';

import MyappEdit from './myappEdit.vue';

import { MenuOutput } from '/@/api-services/system/models';

const quickEntry = useQuickEntry();

const myMods = computed(() => quickEntry.entries); // 我的常用
const modsDrawer = ref<boolean>(false);

// 卡片副标题与抽屉已选项共用 store 里的「菜单 Id -> 目录链」映射；菜单停用或失权限时退回显示路由
const dirOf = (mod: MenuOutput) => quickEntry.pool.dirById.get(mod.id as number) || (mod.path ?? '');

onMounted(() => {
	quickEntry.load();
});

const addMods = () => {
	modsDrawer.value = true;
};
</script>

<style scoped lang="scss">
.myMods {
	list-style: none;
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
	gap: 10px;
}

.myMods li {
	width: 100%;
	height: 76px;
	border-radius: 10px;
	background: var(--el-fill-color-light);
	transition:
		transform 0.25s ease,
		box-shadow 0.25s ease;

	&:hover {
		transform: translateY(-2px);
		box-shadow: var(--el-box-shadow-light);
	}
}

.myMods li a {
	width: 100%;
	height: 100%;
	padding: 0 12px;
	display: flex;
	align-items: center;
	color: var(--el-text-color-primary);
}

/* 左图标 + 右文案 */
.mod-body {
	width: 100%;
	display: flex;
	align-items: center;
	min-width: 0;
}

.mod-info {
	display: flex;
	flex-direction: column;
	align-items: flex-start;
	margin-left: 10px;
	min-width: 0;
	flex: 1;
	gap: 2px;
}

.mod-title {
	font-size: 13px;
	font-weight: 600;
	max-width: 100%;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 副标题：所属目录链 */
.myMods-dir {
	display: block;
	max-width: 100%;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.icon-wrapper {
	flex-shrink: 0;
	width: 40px;
	height: 40px;
	border-radius: 12px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 20px;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
	transition: background-color 0.2s;
}

.myMods li:hover .icon-wrapper {
	background-color: var(--el-color-primary-light-8);
}

.modItem-add {
	border: 1px dashed var(--el-border-color);
	cursor: pointer;

	a {
		justify-content: center;
		font-size: 22px;
		color: var(--el-text-color-secondary);
	}

	&:hover {
		border-color: var(--el-color-primary);

		a {
			color: var(--el-color-primary);
		}
	}
}

.myMods-empty {
	margin-top: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

a {
	text-decoration: none;
}
</style>
