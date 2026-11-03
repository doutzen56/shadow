<template>
	<div>
		<el-dialog v-model="state.isShowDialog" width="769px" :before-close="onCancel" @opened="onDialogOpened" @closed="onDialogClosed">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<div class="cropper-warp">
				<div class="cropper-warp-left">
					<cropper-canvas v-if="state.dialogOpened" :key="state.cropperImg" background>
						<cropper-image :src="state.cropperImg" alt="Picture" rotatable scalable skewable translatable></cropper-image>
						<cropper-shade></cropper-shade>
						<cropper-handle action="select" plain></cropper-handle>
						<cropper-selection id="cropper-selection" ref="selectionRef" initial-coverage="1" movable resizable>
							<cropper-grid role="grid" covered></cropper-grid>
							<cropper-crosshair centered></cropper-crosshair>
							<cropper-handle action="move" theme-color="rgba(255, 255, 255, 0.35)"></cropper-handle>
							<cropper-handle action="n-resize"></cropper-handle>
							<cropper-handle action="e-resize"></cropper-handle>
							<cropper-handle action="s-resize"></cropper-handle>
							<cropper-handle action="w-resize"></cropper-handle>
							<cropper-handle action="ne-resize"></cropper-handle>
							<cropper-handle action="nw-resize"></cropper-handle>
							<cropper-handle action="se-resize"></cropper-handle>
							<cropper-handle action="sw-resize"></cropper-handle>
						</cropper-selection>
					</cropper-canvas>
				</div>
				<div class="cropper-warp-right">
					<div class="cropper-warp-right-title">预览</div>
					<div class="cropper-warp-right-item">
						<div class="cropper-warp-right-value">
							<div class="cropper-circle">
								<img class="cropper-preview-img" :src="state.previewSrc" alt="预览" />
							</div>
						</div>
						<div class="cropper-warp-right-label">100 x 100</div>
					</div>
					<div class="cropper-warp-right-item">
						<div class="cropper-warp-right-value">
							<div class="cropper-circle cropper-size">
								<img class="cropper-preview-img" :src="state.previewSrc" alt="预览" />
							</div>
						</div>
						<div class="cropper-warp-right-label">50 x 50</div>
					</div>
				</div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-upload
						ref="uploadSignRef"
						accept=".jpg,.png"
						:limit="1"
						:show-file-list="false"
						:auto-upload="false"
						:on-change="selectPicture"
						:on-exceed="selectPictureExceed"
						style="display: inline-block; position: absolute; right: 182px"
					>
						<el-button icon="ele-Picture">选择图片</el-button>
					</el-upload>
					<el-button icon="ele-CircleCloseFilled" @click="onCancel">取 消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="onSubmit">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="cropper">
import { nextTick, reactive, ref, watch } from 'vue';
import 'cropperjs';
import type { CropperSelection } from 'cropperjs';
import { genFileId } from 'element-plus';
import type { UploadInstance, UploadProps, UploadRawFile } from 'element-plus';

const props = defineProps({
	title: {
		type: String,
		default: () => '',
	},
});
const emits = defineEmits(['uploadCropperImg']);
const uploadSignRef = ref<UploadInstance>();
// cropper-* 是原生自定义元素（light DOM），不是 Vue 组件，ref 拿到的是真实元素
const selectionRef = ref<CropperSelection>();

const state = reactive({
	isShowDialog: false,
	// 是否已等到弹窗完全打开（可见且完成布局），此时才挂载裁剪元素
	dialogOpened: false,
	// 当前待裁剪图片地址（objectURL 或远程地址）
	cropperImg: '',
	// 右侧圆形预览的裁剪结果（dataURL），两个尺寸共用
	previewSrc: '',
});

// 打开弹窗
const openDialog = (imgs: string) => {
	state.cropperImg = imgs;
	state.isShowDialog = true;
};
// 弹窗完全打开后（过渡结束、真实可见），再挂载 cropper 元素
const onDialogOpened = () => {
	state.dialogOpened = true;
	initPreview();
};
// 图片异步加载并按初始缩放布局完成后，生成一次裁剪预览；
const initPreview = async () => {
	await nextTick();
	const selection = selectionRef.value;
	if (!selection) return;
	await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
	selection.removeEventListener('change', updatePreview);
	selection.addEventListener('change', updatePreview);
	updatePreview();
};
// 用之前「裁剪取图」的方式（selection.$toCanvas）生成当前选区裁剪图，写入 previewSrc
const updatePreview = async () => {
	const selection = selectionRef.value;
	if (!selection) return;
	const canvas = await selection.$toCanvas();
	state.previewSrc = canvas.toDataURL('image/jpeg');
};
// 弹窗内换图后（图片重新加载）重新生成预览
watch(
	() => state.cropperImg,
	() => {
		if (state.dialogOpened) initPreview();
	}
);
// 弹窗关闭后卸载裁剪元素（v-if），下次打开全新挂载
const onDialogClosed = () => {
	state.dialogOpened = false;
};
// 关闭弹窗
const closeDialog = () => {
	state.isShowDialog = false;
};
// 取消
const onCancel = () => {
	closeDialog();
};
// 确认：取裁剪结果（cropper-selection.$toCanvas 返回裁剪区域的 canvas）
const onSubmit = async () => {
	if (!selectionRef.value) return;
	const canvas = await selectionRef.value.$toCanvas();
	canvas.toBlob((blob: Blob | null) => {
		if (blob) emits('uploadCropperImg', { img: blob });
		closeDialog();
	}, 'image/jpeg');
};

// 选择图片：直接更新 src，cropper-image 会自动重新加载
const selectPicture = async (file: any) => {
	const URL = window.URL || window.webkitURL;
	state.cropperImg = URL.createObjectURL(file.raw);
};

// 选择图片超出数量限制时执行
const selectPictureExceed: UploadProps['onExceed'] = (files) => {
	uploadSignRef.value!.clearFiles();
	const file = files[0] as UploadRawFile;
	file.uid = genFileId();
	uploadSignRef.value!.handleStart(file);
};

// 暴露变量
defineExpose({
	openDialog,
});
</script>

<style scoped lang="scss">
.cropper-warp {
	display: flex;
	.cropper-warp-left {
		position: relative;
		display: inline-block;
		height: 350px;
		flex: 1;
		border: 1px solid var(--el-border-color);
		background: var(--el-bg-color);
		overflow: hidden;
		background-repeat: no-repeat;
		cursor: move;
		border-radius: var(--el-border-radius-base);
		// cropper-canvas 撑满左侧容器
		cropper-canvas {
			display: block;
			width: 100%;
			height: 100%;
		}
		cropper-shade {
			outline: rgba(0, 0, 0, 0.65) solid 1920px !important; // !important 才能压过 :host 规则
		}
	}
	.cropper-warp-right {
		width: 150px;
		height: 350px;
		.cropper-warp-right-title {
			text-align: center;
			height: 20px;
			line-height: 20px;
		}
		.cropper-warp-right-item {
			margin: 15px 0;
			.cropper-warp-right-value {
				display: flex;
				justify-content: center;
				// 圆形裁剪放在普通 div 上更可靠，内部用 img 显示裁剪预览
				.cropper-circle {
					width: 100px;
					height: 100px;
					border-radius: 50%;
					overflow: hidden;
					.cropper-preview-img {
						display: block;
						width: 100%;
						height: 100%;
						object-fit: cover;
					}
				}
				.cropper-size {
					width: 50px;
					height: 50px;
				}
			}
			.cropper-warp-right-label {
				text-align: center;
				font-size: 12px;
				color: var(--el-text-color-primary);
				height: 30px;
				line-height: 30px;
			}
		}
	}
}
</style>
