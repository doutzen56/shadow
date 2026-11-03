<template>
	<div class="layout-logo" v-if="setShowLogo" @click="onThemeConfigChange">
		<img :src="themeConfig.logoUrl" class="layout-logo-medium-img" v-if="showLogo" />
		<span>{{ themeConfig.globalTitle }}</span>
	</div>
	<div class="layout-logo-size" v-else @click="onThemeConfigChange">
		<img :src="themeConfig.logoUrl" class="layout-logo-size-img" />
	</div>
</template>

<script setup lang="ts" name="layoutLogo">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
// import logoMini from '/@/assets/logo-mini.svg';

// 定义变量内容
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// 设置 logo 的显示。classic 经典布局默认显示 logo
const setShowLogo = computed(() => {
	let { isCollapse, layout } = themeConfig.value;
	return !isCollapse || layout === 'classic' || document.body.clientWidth < 1000;
});
const showLogo = computed(() => {
	let { layout } = themeConfig.value;
	return layout !== 'columns';
});
// logo 点击实现菜单展开/收起
const onThemeConfigChange = () => {
	if (themeConfig.value.layout === 'transverse') return false;
	themeConfig.value.isCollapse = !themeConfig.value.isCollapse;
};
</script>

<style scoped lang="scss">
.layout-logo {
	width: 240px;
	height: 50px;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 8px;
	padding: 0 12px;
	// 与侧栏同语言：细分隔线代替原来的粗投影
	border-bottom: 1px solid var(--el-border-color-lighter);
	color: var(--el-color-primary);
	cursor: pointer;
	// animation: logoAnimation 0.3s ease-in-out;
	span {
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		display: inline-block;
		font-weight: 900;
		font-size: 18px;
	}
	&:hover {
		span {
			// color: var(--color-primary-light-1);
			animation: logoAnimation 0.5s ease-in-out;
		}
		img {
			animation: logoAnimation 0.5s ease-in-out;
		}
	}
	&-medium-img {
		flex: 0 0 auto;
		width: 30px;
		height: 30px;
		object-fit: contain;
	}
}
.layout-logo-size {
	width: 100%;
	height: 50px;
	display: flex;
	cursor: pointer;
	animation: logoAnimation 0.3s ease-in-out;
	&-img {
		width: 35px;
		margin: auto;
	}
	&:hover {
		img {
			animation: logoAnimation 0.3s ease-in-out;
		}
	}
}
</style>
