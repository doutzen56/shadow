<template>
	<div class="sys-codeGenResult-container">
		<el-dialog v-model="state.isShowDialog" :close-on-click-modal="false" width="820px" top="6vh" class="grd-dialog">
			<template #header>
				<div class="grd-hd">
					<span class="grd-hd-ico"><el-icon><ele-SuccessFilled /></el-icon></span>
					<div class="grd-hd-txt">
						<span class="grd-hd-title">生成成功</span>
						<span class="grd-hd-sub">{{ state.busName }} · {{ state.modeText }}</span>
					</div>
					<span v-if="state.elapsed" class="grd-hd-stat">耗时 {{ state.elapsed }} ms</span>
				</div>
			</template>

			<!-- 概览 -->
			<div class="grd-sum">
				<div class="grd-sum-item">
					<span class="grd-sum-ico is-be"><el-icon><ele-Cpu /></el-icon></span>
					<div>
						<b>{{ state.backEnd.length }}</b>
						<span>后端文件</span>
					</div>
				</div>
				<div class="grd-sum-item">
					<span class="grd-sum-ico is-fe"><el-icon><ele-Monitor /></el-icon></span>
					<div>
						<b>{{ state.frontEnd.length }}</b>
						<span>前端文件</span>
					</div>
				</div>
				<div class="grd-sum-item">
					<span class="grd-sum-ico is-all"><el-icon><ele-Files /></el-icon></span>
					<div>
						<b>{{ state.files.length }}</b>
						<span>文件合计</span>
					</div>
				</div>
				<div v-if="state.menuName" class="grd-sum-item">
					<span class="grd-sum-ico is-menu"><el-icon><ele-Menu /></el-icon></span>
					<div>
						<b>{{ state.menuName }}</b>
						<span>已建菜单</span>
					</div>
				</div>
			</div>

			<!-- 提醒 -->
			<div v-if="state.warnings.length" class="grd-warn">
				<el-icon><ele-WarningFilled /></el-icon>
				<div>
					<p class="grd-warn-t">生成成功，但有 {{ state.warnings.length }} 处建议检查</p>
					<p v-for="(w, i) in state.warnings" :key="i" class="grd-warn-i">· {{ w }}</p>
				</div>
			</div>

			<!-- 文件清单 -->
			<div class="grd-sec">
				<div class="grd-sec-hd">
					<span>输出文件</span>
					<el-radio-group v-model="state.tab" size="small">
						<el-radio-button value="all">全部 {{ state.files.length }}</el-radio-button>
						<el-radio-button value="后端">后端 {{ state.backEnd.length }}</el-radio-button>
						<el-radio-button value="前端">前端 {{ state.frontEnd.length }}</el-radio-button>
					</el-radio-group>
				</div>
				<div class="grd-list">
					<div v-for="f in showFiles" :key="f.path" class="grd-file">
						<span class="grd-file-ico" :class="f.side === '后端' ? 'is-be' : 'is-fe'">
							<el-icon><ele-Document /></el-icon>
						</span>
						<div class="grd-file-main">
							<div class="grd-file-name">
								{{ f.name }}
								<span class="grd-file-kind">{{ f.kind }}</span>
							</div>
							<div class="grd-file-path" :title="f.path">{{ f.path }}</div>
						</div>
						<span class="grd-file-lines">{{ f.lines }} 行</span>
						<el-button text size="small" icon="ele-CopyDocument" title="复制路径" @click="copyPath(f.path)" />
					</div>
					<div v-if="showFiles.length === 0" class="grd-none">没有该分类的文件</div>
				</div>
			</div>

			<!-- 下一步 -->
			<div class="grd-next">
				<div class="grd-next-t"><el-icon><ele-InfoFilled /></el-icon>接下来做什么</div>
				<ol class="grd-next-ol">
					<li v-for="(s, i) in state.nextSteps" :key="i">{{ s }}</li>
				</ol>
			</div>

			<template #footer>
				<span class="dialog-footer">
					<el-button v-if="state.url" icon="ele-Download" @click="downloadAgain">重新下载</el-button>
					<el-button icon="ele-CircleCloseFilled" @click="cancel">关闭</el-button>
					<el-button icon="ele-Refresh" type="primary" @click="cancel">知道了</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<!-- 代码生成结果 -->
<script lang="ts" setup name="sysCodeGenResult">
import { reactive, computed } from 'vue';
import commonFunction from '/@/utils/commonFunction';
import { downloadByUrl } from '/@/utils/download';

const { copyText } = commonFunction();

const state = reactive({
	isShowDialog: false,
	busName: '',
	modeText: '',
	url: '' as string,
	elapsed: 0,
	menuName: '',
	tab: 'all' as 'all' | '前端' | '后端',
	files: [] as Array<{ name: string; path: string; side: string; kind: string; lines: number }>,
	warnings: [] as string[],
	nextSteps: [] as string[],
});

const backEnd = computed(() => state.files.filter((f) => f.side === '后端'));
const frontEnd = computed(() => state.files.filter((f) => f.side === '前端'));
const showFiles = computed(() => (state.tab === 'all' ? state.files : state.tab === '后端' ? backEnd.value : frontEnd.value));

// 打开结果弹窗
const openDialog = (result: any, busName: string) => {
	const files: Array<any> = result.files ?? [];
	// 后端返回的是结构化清单；老数据（字符串数组）也要能兜住
	state.files = files.map((f: any) =>
		typeof f === 'string' ? { name: f.split('/').pop(), path: f, side: /\.(cs|json)$/i.test(f) ? '后端' : '前端', kind: '', lines: 0 } : f
	);
	state.busName = busName || '代码生成';
	state.url = result.url ?? '';
	state.elapsed = result.elapsed ?? 0;
	state.menuName = result.menuName ?? '';
	state.warnings = result.warnings ?? [];
	state.tab = 'all';
	state.modeText = state.url ? '已打包为压缩包下载' : '已直接写入项目目录';

	state.nextSteps = state.url
		? ['解压压缩包，按目录中的 SimpX.xxx 与 src/views 分别放入后端与前端项目', '后端重新编译启动，前端重新构建后即可访问']
		: ['后端重新编译并启动（dotnet run），新接口才会生效', '前端若开启了「接口服务」模式，执行 api_build/build.bat 重新生成请求文件'];
	if (result.generateMenu) state.nextSteps.push('刷新浏览器页面，左侧菜单会出现「' + (state.menuName || '新菜单') + '」入口');
	if (state.warnings.length) state.nextSteps.push('按上方提醒补齐字段配置后，可再次生成覆盖');

	state.isShowDialog = true;
};

// 复制路径
const copyPath = (p: string) => copyText(p);

// 重新下载
const downloadAgain = () => {
	if (state.url) downloadByUrl({ url: state.url });
};

// 关闭
const cancel = () => {
	state.isShowDialog = false;
};

defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.grd-dialog :deep(.el-dialog__body) {
	padding: 14px 18px 6px;
}

/* ===================== 概览 ===================== */
.grd-sum {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 10px;
}

.grd-sum-item {
	display: flex;
	align-items: center;
	gap: 9px;
	padding: 10px 12px;
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);

	> div {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	b {
		font-size: 18px;
		line-height: 1.2;
		color: var(--el-text-color-primary);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	span {
		font-size: 11.5px;
		color: var(--el-text-color-secondary);
	}
}

.grd-sum-ico {
	flex: none;
	width: 30px;
	height: 30px;
	border-radius: 9px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 15px;
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 12%, transparent);

	&.is-be {
		--tint: #3b82f6;
	}
	&.is-fe {
		--tint: #10b981;
	}
	&.is-all {
		--tint: #8b5cf6;
	}
	&.is-menu {
		--tint: #f59e0b;
	}
}

/* ===================== 提醒 ===================== */
.grd-warn {
	display: flex;
	gap: 9px;
	margin-top: 12px;
	padding: 10px 12px;
	border-radius: 10px;
	border: 1px solid rgba(245, 158, 11, 0.35);
	background: rgba(245, 158, 11, 0.08);

	> .el-icon {
		flex: none;
		margin-top: 2px;
		font-size: 15px;
		color: var(--el-color-warning);
	}
}

.grd-warn-t {
	margin: 0;
	font-size: 12.5px;
	font-weight: 600;
	color: #b45309;
}

.grd-warn-i {
	margin: 3px 0 0;
	font-size: 12px;
	line-height: 1.7;
	color: #b45309;
}

/* ===================== 文件清单 ===================== */
.grd-sec {
	margin-top: 14px;
}

.grd-sec-hd {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.grd-list {
	margin-top: 8px;
	max-height: 232px;
	overflow: auto;
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
}

.grd-file {
	display: flex;
	align-items: center;
	gap: 9px;
	padding: 7px 10px;
	border-bottom: 1px solid var(--el-border-color-extra-light);

	&:last-child {
		border-bottom: none;
	}
}

.grd-file-ico {
	flex: none;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 13px;
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 12%, transparent);

	&.is-be {
		--tint: #3b82f6;
	}
	&.is-fe {
		--tint: #10b981;
	}
}

.grd-file-main {
	flex: 1 1 auto;
	min-width: 0;
}

.grd-file-name {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.grd-file-kind {
	font-size: 10.5px;
	font-weight: 500;
	padding: 0 6px;
	line-height: 17px;
	border-radius: 8px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-darker);
}

.grd-file-path {
	font-size: 11.5px;
	line-height: 1.5;
	color: var(--el-text-color-secondary);
	font-family: ui-monospace, Menlo, Consolas, monospace;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.grd-file-lines {
	flex: none;
	font-size: 11.5px;
	color: var(--el-text-color-placeholder);
}

.grd-none {
	padding: 24px 0;
	text-align: center;
	font-size: 12.5px;
	color: var(--el-text-color-placeholder);
}

/* ===================== 下一步 ===================== */
.grd-next {
	margin-top: 14px;
	padding: 10px 12px;
	border-radius: 10px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
}

.grd-next-t {
	display: flex;
	align-items: center;
	gap: 5px;
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);

	.el-icon {
		font-size: 14px;
		color: var(--el-color-primary);
	}
}

.grd-next-ol {
	margin: 6px 0 0;
	padding-left: 18px;
	font-size: 12px;
	line-height: 1.85;
	color: var(--el-text-color-secondary);
}
</style>

<style lang="scss">
/* 弹窗头部：全局 .el-dialog__header 是主题色底 + 白字，这里只接管内边距 */
.grd-dialog .el-dialog__header {
	padding: 0;
}
.grd-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 14px 46px 14px 18px;
}
.grd-hd-ico {
	flex: none;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 17px;
	background: color-mix(in srgb, currentColor 18%, transparent);
}
.grd-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.grd-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.grd-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.grd-hd-stat {
	flex: none;
	padding: 3px 10px;
	border-radius: 999px;
	font-size: 12px;
	background: color-mix(in srgb, currentColor 18%, transparent);
}
</style>
