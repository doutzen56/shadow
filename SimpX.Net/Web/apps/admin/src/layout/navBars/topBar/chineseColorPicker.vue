<template>
	<el-dialog v-model="visible" :width="width" append-to-body>
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-MagicStick />
				</el-icon>
				<span>{{ title }}</span>
			</div>
		</template>

		<div class="color-picker-body">
			<el-row :gutter="20">
				<el-col v-for="(item, index) in chineseColorArr" :key="item.hex" :xs="6" :sm="4" :md="4" :lg="4" :xl="3" class="mb15" @click="handleSelect(item, index)">
					<div class="traditionalColors" :style="{ background: item.hex }">
						<el-icon v-if="modelValue === item.hex" class="traditionalColors-circleCheck" size="20" color="#fff">
							<ele-CircleCheck />
						</el-icon>
					</div>
					<div class="traditionalColors-chines mt10">{{ item.name }}</div>
					<div class="traditionalColors-chines traditionalColors-hex">{{ item.hex }}</div>
				</el-col>
			</el-row>
		</div>
	</el-dialog>
</template>

<!-- 中国颜色选择器 -->
<script setup lang="ts" name="ChineseColorPicker">
import { computed } from 'vue';
// import { ElMessage } from 'element-plus';
import chineseColorArr from './chineseColor.json';

// 颜色项类型
export interface ChineseColorItem {
	name: string;
	hex: string;
	[key: string]: any;
}

const props = withDefaults(
	defineProps<{
		/** 当前选中颜色值，绑定的是 hex 字符串 */
		modelValue?: string;
		/** 控制弹窗显隐 */
		dialogVisible: boolean;
		title?: string;
		width?: string | number;
		/** 选中后是否复制 hex 到剪贴板 */
		copyOnSelect?: boolean;
	}>(),
	{
		modelValue: '',
		title: '中国传统颜色',
		// width: '70%',
		copyOnSelect: true,
	}
);

const emit = defineEmits<{
	/** v-model 更新：仍然抛出 hex 字符串 */
	(e: 'update:modelValue', value: string): void;
	(e: 'update:dialogVisible', value: boolean): void;
	/** change 事件：抛出整个对象 + 下标，应用层可 e.hex / e.name 直接用 */
	(e: 'change', value: ChineseColorItem, index: number): void;
}>();

const visible = computed({
	get: () => props.dialogVisible,
	set: (val) => emit('update:dialogVisible', val),
});

const handleSelect = (item: ChineseColorItem, index: number) => {
	// if (props.copyOnSelect) {
	// 	navigator.clipboard.writeText(item.hex);
	// 	ElMessage.success(`主题色已复制：${item.hex}`);
	// }
	// v-model 仍然回传 hex 字符串
	emit('update:modelValue', item.hex);
	// change 事件回传整个对象 + 下标
	emit('change', item, index);
};
</script>

<style scoped lang="scss">
.color-picker-body {
	height: 70vh;
	overflow-y: auto;
	overflow-x: hidden;
}

.traditionalColors {
	height: 50px;
	position: relative;
	transition: transform 0.3s ease;
	cursor: pointer;

	&:hover {
		transform: scale(1.2);
		z-index: 10;
	}
}

.traditionalColors-chines {
	text-align: center;
	cursor: grab;
}

.traditionalColors-hex {
	color: gray;
}

.traditionalColors-circleCheck {
	position: absolute;
	left: calc(50% - 10px);
	top: calc(50% - 10px);
}
</style>
