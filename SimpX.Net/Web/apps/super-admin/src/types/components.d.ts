// 申明外部 npm 插件模块
import sysDict from '/@/components/sysDict/sysDict.vue';

declare module 'vue' {
	export interface GlobalComponents {
		GSysDict: typeof sysDict;
	}
}
