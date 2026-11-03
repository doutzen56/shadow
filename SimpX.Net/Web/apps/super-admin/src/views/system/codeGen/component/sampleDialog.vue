<template>
	<div class="sys-codeGenSample-container">
		<el-dialog v-model="state.isShowDialog" :close-on-click-modal="false" :show-close="false" width="900px" class="sp-dialog">
			<template #header>
				<div class="sp-hd">
					<span class="sp-hd-ico"><el-icon><ele-MagicStick /></el-icon></span>
					<div class="sp-hd-txt">
						<span class="sp-hd-title">示例配置一键克隆</span>
						<span class="sp-hd-sub">挑一个已经跑通的场景，换上你自己的表，立刻得到一份能直接生成的配置</span>
					</div>
					<el-button class="sp-hd-btn is-close" text title="关闭" @click="cancel"><el-icon><ele-Close /></el-icon></el-button>
				</div>
			</template>

			<div class="sp-body">
				<!-- 左：四套示例 -->
				<div class="sp-cards">
					<div
						v-for="s in codeGenSamples"
						:key="s.key"
						class="sp-card"
						:class="{ 'is-active': form.key === s.key }"
						@click="pick(s)"
					>
					<div class="sp-card-pic" title="点击看大图" @click.stop="openScenePreview(s)">
						<img class="sp-card-img" :src="sceneImage(s.scene)" :alt="s.name" />
						<span class="sp-card-zoom"><el-icon><ele-ZoomIn /></el-icon></span>
					</div>
					<div class="sp-card-body">
							<div class="sp-card-top">
								<span class="sp-card-name">{{ s.name }}</span>
								<span class="sp-card-scene">{{ sceneLabel(s.scene) }}</span>
							</div>
							<p class="sp-card-desc">{{ s.desc }}</p>
							<div class="sp-card-tags">
								<span v-for="t in s.tags" :key="t">{{ t }}</span>
							</div>
						</div>
						<span v-if="form.key === s.key" class="sp-card-check"><el-icon><ele-CircleCheckFilled /></el-icon></span>
					</div>
				</div>

				<!-- 右：参数 -->
				<div class="sp-form">
					<el-form label-position="top" v-loading="state.loading">
						<el-form-item label="业务名">
							<el-input v-model="form.busName" placeholder="用于菜单和页面标题" maxlength="30" />
						</el-form-item>
						<div class="sp-row">
							<el-form-item label="命名空间" class="sp-row-item">
								<el-select v-model="form.nameSpace" placeholder="后端命名空间" filterable class="w100">
									<el-option v-for="n in store.namespaceList ?? []" :key="n" :label="n" :value="n" />
								</el-select>
							</el-form-item>
							<el-form-item label="前端目录" class="sp-row-item">
								<el-input v-model="form.pagePath" placeholder="main">
									<template #prepend>src/views/</template>
								</el-input>
							</el-form-item>
						</div>
						<el-form-item label="主表">
							<el-select v-model="form.main" placeholder="选择主表实体" filterable class="w100" @change="onMainChange">
								<el-option v-for="t in state.tables" :key="t.entityName" :label="`${t.entityName} ( ${t.tableName} )`" :value="t.entityName" />
							</el-select>
						</el-form-item>
						<el-form-item v-if="needSlave" label="从表">
							<el-select v-model="form.slave" placeholder="选择从表实体" filterable class="w100">
								<el-option v-for="t in state.tables" :key="t.entityName" :label="`${t.entityName} ( ${t.tableName} )`" :value="t.entityName" />
							</el-select>
						</el-form-item>
						<el-form-item v-if="needTree" label="树标题">
							<el-input v-model="form.treeTitle" placeholder="左侧树面板的标题" maxlength="20" />
						</el-form-item>
						<el-form-item label="生成菜单">
							<el-switch v-model="form.generateMenu" />
							<span class="sp-hint">开启后会同时写入一条菜单记录，刷新页面即可看到入口</span>
						</el-form-item>
					</el-form>

					<div class="sp-tip">
						<el-icon class="sp-tip-ico"><ele-InfoFilled /></el-icon>
						<div class="sp-tip-txt">
							<p v-if="needTree">树配置会按表名自动推导父级字段 / 显示字段，取不到时创建后到「树组件配置」里补一下即可。</p>
							<p v-if="needSlave">主从联表字段会自动匹配 <b>{{ slaveLinkTip }}</b> 这类外键，创建后可在「从表配置」中确认。</p>
							<p>字段默认值已按常用习惯排好：备注类不进列表、外键 Id 不进列表、名称类默认可查询。</p>
						</div>
					</div>
				</div>
			</div>

			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="cancel">取消</el-button>
					<el-button icon="ele-MagicStick" type="primary" :loading="state.saving" @click="create">一键创建</el-button>
				</span>
			</template>
		</el-dialog>

		<!-- 场景大图预览：和上面的弹窗平级放，避免嵌套弹窗抢焦点 -->
		<el-dialog v-model="state.previewVisible" width="92vw" top="4vh" class="sp-preview-dialog" :show-close="true">
			<template #header>
				<div class="sp-hd">
					<span class="sp-hd-ico"><el-icon><ele-Picture /></el-icon></span>
					<div class="sp-hd-txt">
						<span class="sp-hd-title">{{ state.previewTitle }}</span>
						<span class="sp-hd-sub">{{ state.previewDesc }}</span>
					</div>
				</div>
			</template>
			<div class="sp-preview-body">
				<img :src="state.previewSrc" :alt="state.previewTitle" />
			</div>
		</el-dialog>
	</div>
</template>

<!-- 示例配置一键克隆 -->
<script lang="ts" setup name="sysCodeGenSample">
import { computed, reactive } from 'vue';
import { ElMessage } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { SysCodeGenApi } from '/@/api-services/system/api';
import { useUserInfo } from '/@/stores/userInfo';
import { useCodeGenStore } from './codeGenStore';
import {
	applySampleColumnDefaults,
	codeGenSamples,
	guessSlaveLinkField,
	guessTreeFields,
	isSlaveScene,
	isTreeScene,
} from './sampleTemplates';

const emits = defineEmits(['handleQuery']);
const store = useCodeGenStore();
const userInfo = useUserInfo();

const state = reactive({
	isShowDialog: false,
	loading: false,
	saving: false,
	tables: [] as any[],
	// 场景大图预览
	previewVisible: false,
	previewSrc: '',
	previewTitle: '场景预览',
	previewDesc: '',
});

const form = reactive({
	key: 'single',
	busName: '',
	nameSpace: '',
	pagePath: 'main',
	main: '',
	slave: '',
	treeTitle: '',
	generateMenu: false,
});

const sample = computed(() => codeGenSamples.find((x) => x.key === form.key) ?? codeGenSamples[0]);
const needTree = computed(() => isTreeScene(sample.value.scene));
const needSlave = computed(() => isSlaveScene(sample.value.scene));
const slaveLinkTip = computed(() => `${form.main.replace(/^Sys/, '')}Id`);

const BASE = import.meta.env.BASE_URL || '/';
const sceneImage = (scene: number) => `${BASE}images/codegen/scene-${scene}.svg`;
const sceneLabel = (scene: number) => userInfo.dictList?.['CodeGenSceneEnum']?.find((x: any) => Number(x.value) === scene)?.label ?? '';

// 点缩略图看大图：卡片本身的点击是「选中」，这里 stop 掉，避免点图时顺带切换示例
const openScenePreview = (s: any) => {
	state.previewSrc = sceneImage(s.scene);
	state.previewTitle = `${s.name}（${sceneLabel(s.scene)}）`;
	state.previewDesc = s.desc;
	state.previewVisible = true;
};

// 选一套示例：带出默认业务名 / 表名 / 树标题
const pick = (s: any) => {
	form.key = s.key;
	form.busName = s.busName;
	form.main = s.main;
	form.slave = s.slave ?? '';
	form.treeTitle = s.treeTitle ?? '';
};

// 换主表之后，主从场景的下游表名也跟着给个提示值（仍可手改）
const onMainChange = () => {
	if (!needSlave.value) return;
	const hit = state.tables.find((t) => t.entityName && t.entityName !== form.main && new RegExp(`^${form.main.replace(/^Sys/, '')}`).test(t.entityName));
	if (hit) form.slave = hit.entityName;
};

// 打开弹窗
const openDialog = async () => {
	state.isShowDialog = true;
	await store.init();
	form.nameSpace ||= store.namespaceList?.[0] ?? '';
	const configId = store.databaseList?.[0]?.configId ?? '';
	state.loading = true;
	try {
		state.tables = configId ? ((await store.tableListBy(configId)) ?? []) : [];
	} finally {
		state.loading = false;
	}
	pick(codeGenSamples[0]);
};

const cancel = () => {
	state.isShowDialog = false;
};

// 取某张表的默认字段配置，并把 config 还原成对象
const loadColumns = async (configId: string, tableName: string) => {
	const res = await getAPI(SysCodeGenApi).apiSysCodeGenDefaultColumnConfigListPost({ configId, tableName } as any);
	const list: any[] = (res.data.result ?? []) as any[];
	list.forEach((c: any) => {
		try {
			if (typeof c.config === 'string') c.config = JSON.parse(c.config);
		} catch (e) {
			/* 忽略 */
		}
		c.config ??= {};
	});
	return list;
};

// 一键创建
const create = async () => {
	if (!form.busName) return ElMessage.warning('请填写业务名');
	if (!form.main) return ElMessage.warning('请选择主表');
	if (needSlave.value && !form.slave) return ElMessage.warning('请选择从表');

	const configId = store.databaseList?.[0]?.configId ?? '';
	const findTable = (entityName: string) => state.tables.find((t) => t.entityName === entityName);
	const mainTable = findTable(form.main);
	if (!mainTable) return ElMessage.warning(`没找到实体 ${form.main}`);

	state.saving = true;
	try {
		const mainColumns = await loadColumns(configId, mainTable.tableName);
		const tables: any[] = [
			{
				configId,
				tableName: mainTable.tableName,
				entityName: mainTable.entityName,
				moduleName: mainTable.entityName,
				busName: form.busName,
				columnList: mainColumns,
			},
		];

		applySampleColumnDefaults(mainColumns);

		// 树配置：默认树与列表同一张表（先算树，主表的「上级联表字段」就是树的父子字段）
		let treeConfig: any = undefined;
		if (needTree.value) {
			const { parent, display, search } = guessTreeFields(mainColumns);
			if (!parent || !display) {
				const miss = [!parent ? '父级字段（Pid / ParentId）' : '', !display ? '显示字段（Name / Title）' : ''].filter(Boolean).join('、');
				return ElMessage.warning(`实体 ${form.main} 里没找到${miss}，换成带层级结构的表再试`);
			}
			const typeOf = (name?: string) => mainColumns.find((c: any) => c.propertyName === name)?.netType ?? 'long';
			treeConfig = {
				configId,
				entityName: mainTable.entityName,
				tableName: mainTable.tableName,
				tableComment: mainTable.tableComment,
				displayPropertyNames: display ?? '',
				linkPropertyName: 'Id',
				linkPropertyType: 'long',
				searchPropertyName: search ?? '',
				searchPropertyType: typeOf(search),
				parentPropertyName: parent ?? '',
				parentPropertyType: typeOf(parent),
				treeTitle: form.treeTitle || form.busName,
			};
			tables[0].lastLinkPropertyName = parent; // 点树节点：queryParams.<父级字段> = 节点 Id
		}

		if (needSlave.value) {
			const slaveTable = findTable(form.slave);
			if (!slaveTable) return ElMessage.warning(`没找到实体 ${form.slave}`);
			const slaveColumns = await loadColumns(configId, slaveTable.tableName);
			const linkField = guessSlaveLinkField(slaveColumns, mainTable.entityName);
			applySampleColumnDefaults(slaveColumns, linkField);
			// 点主表行：从表 queryParams.<从表外键> = 主表行.Id
			tables[0].nextLinkPropertyName = 'Id';
			tables.push({
				configId,
				tableName: slaveTable.tableName,
				entityName: slaveTable.entityName,
				moduleName: slaveTable.entityName,
				busName: form.busName,
				columnList: slaveColumns,
				lastLinkPropertyName: linkField ?? '',
			});
		}

		const body: any = {
			busName: form.busName,
			nameSpace: form.nameSpace,
			pagePath: form.pagePath,
			moduleName: mainTable.entityName,
			scene: sample.value.scene,
			generateMethod: 200,
			printType: 1,
			menuIcon: 'ele-Menu',
			menuPid: 0,
			authorName: 'SimpX',
			generateMenu: form.generateMenu,
			isApiService: false,
			treeConfig,
			tableList: tables,
		};
		tables.forEach((t: any) => t.columnList.forEach((c: any) => (c.config = JSON.stringify(c.config ?? {}))));

		await getAPI(SysCodeGenApi).apiSysCodeGenAddPost(body);
		ElMessage.success(`已按「${sample.value.name}」创建配置：${form.busName}`);
		state.isShowDialog = false;
		emits('handleQuery', { openBusName: form.busName });
	} finally {
		state.saving = false;
	}
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.sp-body {
	display: flex;
	gap: 16px;
	align-items: flex-start;
}
.sp-cards {
	flex: none;
	width: 380px;
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 10px;
}
.sp-card {
	position: relative;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	overflow: hidden;
	background: var(--el-bg-color);
	cursor: pointer;
	transition: all 0.2s ease;

	&:hover {
		border-color: var(--el-color-primary-light-5);
		transform: translateY(-2px);
		box-shadow: 0 6px 16px -8px rgba(0, 0, 0, 0.25);
	}
	&.is-active {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 2px color-mix(in srgb, var(--el-color-primary) 16%, transparent);
	}
}
/* 缩略图单独包一层：点图是「看大图」，点卡片其它位置才是「选中这套示例」 */
.sp-card-pic {
	position: relative;
	overflow: hidden;
	cursor: zoom-in;
}
.sp-card-img {
	display: block;
	width: 100%;
	height: 76px;
	object-fit: cover;
	object-position: top center;
	background: var(--el-fill-color-lighter);
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.sp-card-zoom {
	position: absolute;
	inset: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 18px;
	color: #fff;
	background: rgba(0, 0, 0, 0.35);
	opacity: 0;
	transition: opacity 0.2s ease;
}
.sp-card-pic:hover .sp-card-zoom {
	opacity: 1;
}
.sp-card-body {
	padding: 7px 9px 9px;
}
.sp-card-top {
	display: flex;
	align-items: center;
	gap: 5px;
}
.sp-card-name {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.sp-card-scene {
	flex: none;
	padding: 1px 6px;
	border-radius: 999px;
	font-size: 11px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}
.sp-card-desc {
	margin: 4px 0 6px;
	font-size: 11.5px;
	line-height: 1.55;
	color: var(--el-text-color-secondary);
	display: -webkit-box;
	-webkit-line-clamp: 2;
	line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}
.sp-card-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;

	span {
		padding: 1px 6px;
		border-radius: 4px;
		font-size: 11px;
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color-light);
	}
}
.sp-card-check {
	position: absolute;
	top: 6px;
	right: 6px;
	width: 18px;
	height: 18px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	color: #fff;
	background: var(--el-color-primary);
}
.sp-form {
	flex: 1 1 auto;
	min-width: 0;
}
.sp-row {
	display: flex;
	gap: 12px;

	:deep(.sp-row-item) {
		flex: 1 1 0;
		min-width: 0;
	}
}
.sp-hint {
	margin-left: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.sp-tip {
	display: flex;
	gap: 8px;
	margin-top: 4px;
	padding: 9px 12px;
	border-radius: 8px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
}
.sp-tip-ico {
	flex: none;
	margin-top: 2px;
	font-size: 14px;
	color: var(--el-color-primary);
}
.sp-tip-txt {
	font-size: 12px;
	line-height: 1.8;
	color: var(--el-text-color-secondary);

	p + p {
		margin-top: 2px;
	}
}
</style>

<style lang="scss">
/* 头部：全局 .el-dialog__header 是主题色底 + 白字，这里只接管内边距 */
.sp-dialog .el-dialog__header {
	padding: 0;
}
.sp-hd {
	display: flex;
	align-items: center;
	gap: 12px;
	width: 100%;
	padding: 13px 16px;
}
.sp-hd-ico {
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
.sp-hd-txt {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}
.sp-hd-title {
	font-size: 16px;
	font-weight: 600;
	line-height: 1.3;
}
.sp-hd-sub {
	font-size: 12px;
	line-height: 1.5;
	opacity: 0.85;
}
.sp-hd-btn {
	flex: none;
	height: 30px;
	padding: 0 10px;
	border-radius: 8px;
	font-size: 13px;
	// 按钮文字色由组件自己读的两个变量决定，改变量比写 color 更能压住 .el-button.is-text:hover
	--el-button-text-color: var(--el-color-white);
	--el-button-hover-text-color: var(--el-color-white);
	background: color-mix(in srgb, currentColor 16%, transparent);
	border: 1px solid color-mix(in srgb, currentColor 28%, transparent);

	&:hover {
		background: color-mix(in srgb, currentColor 28%, transparent);
		border-color: color-mix(in srgb, currentColor 45%, transparent);
	}
}
/* 关闭按钮单独走「危险色」：默认白描边，hover 整块变红（不透明底自己构成对比）。
   选择器带 .sp-dialog 提权，否则会被 element-plus 的 .el-button.is-text:hover 白底盖掉 */
.sp-dialog .sp-hd-btn.is-close {
	.el-icon {
		transition: transform 0.25s ease;
	}

	&:hover,
	&:focus {
		color: #fff !important;
		background: var(--el-color-danger) !important;
		border-color: var(--el-color-danger) !important;
		box-shadow: 0 2px 8px -2px color-mix(in srgb, var(--el-color-danger) 60%, transparent);

		.el-icon {
			transform: scale(1.15);
		}
	}
}
/* 顶部 label 表单：label 与控件间距收紧一点，两列并排才不散 */
.sp-dialog .el-form-item--label-top .el-form-item__label {
	height: auto;
	line-height: 1.4;
	padding: 0 0 6px;
	margin-bottom: 0;
}
.sp-dialog .el-form-item {
	margin-bottom: 14px;
}
.sp-dialog .el-dialog__body {
	max-height: calc(100vh - 200px) !important;
}
/* 场景大图预览 */
.sp-preview-dialog .el-dialog__body {
	padding: 12px !important;
	background: var(--el-fill-color-lighter);
}
.sp-preview-body {
	display: flex;
	align-items: center;
	justify-content: center;

	img {
		width: 100%;
		max-height: 78vh;
		object-fit: contain;
		border-radius: 8px;
		box-shadow: 0 10px 30px -20px rgba(0, 0, 0, 0.4);
	}
}
</style>
