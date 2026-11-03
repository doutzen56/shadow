<template>
	<div class="sys-editCodeGen-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" fullscreen class="cge-dialog">
			<template #header>
				<div class="cge-hd">
					<span class="cge-hd-ico"><el-icon><ele-Cpu /></el-icon></span>
					<div class="cge-hd-txt">
						<span class="cge-hd-title">{{ state.title }}</span>
						<span class="cge-hd-sub" v-if="state.subtitle">{{ state.subtitle }}</span>
					</div>
					<span class="cge-hd-tip">选表 → 配字段 → 一键生成前后端代码</span>
				</div>
			</template>

			<el-form :model="store.ruleForm" ref="ruleFormRef" label-width="auto" v-loading="store.loading" class="cge-form">
				<el-tabs v-model="state.activeTab" class="cge-tabs">
					<!-- ============ 基础配置 ============ -->
					<el-tab-pane label="基础配置" name="0">
						<!-- 业务信息 -->
						<section class="cge-card">
							<header class="cge-card-hd">
								<span class="cge-ico"><el-icon><ele-Document /></el-icon></span>
								<div class="cge-card-tt">
									<h4>业务信息</h4>
									<p>业务名会成为页面标题与代码注释，作者信息写进文件头</p>
								</div>
							</header>
							<div class="cge-grid">
								<el-form-item label="业务名" prop="busName" :rules="busNameRules">
									<el-input v-model="store.ruleForm.busName" placeholder="如：巡检任务" class="w100" clearable maxlength="30" show-word-limit />
								</el-form-item>
								<el-form-item label="命名空间" prop="nameSpace" :rules="[{ required: true, message: '请选择命名空间', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.nameSpace" filterable clearable class="w100" placeholder="选择后端命名空间">
										<el-option v-for="(item, index) in store.namespaceList ?? []" :key="index" :label="item" :value="item" />
									</el-select>
								</el-form-item>
								<el-form-item label="作者姓名" prop="authorName">
									<el-input v-model="store.ruleForm.authorName" clearable placeholder="写进生成代码的文件头" class="w100" />
								</el-form-item>
								<el-form-item label="作者邮箱" prop="email">
									<el-input v-model="store.ruleForm.email" clearable placeholder="选填" class="w100" />
								</el-form-item>
							</div>
						</section>

						<!-- 场景类型 -->
						<section class="cge-card">
							<header class="cge-card-hd">
								<span class="cge-ico is-purple"><el-icon><ele-Grid /></el-icon></span>
								<div class="cge-card-tt">
									<h4>场景类型</h4>
									<p>决定生成几个页面、要不要树、要不要主从联动；点卡片切换，点左侧缩略图看大图</p>
								</div>
								<el-tag v-if="state.sceneLocked" size="small" type="info" class="cge-card-tag">已保存的配置不可改场景</el-tag>
							</header>
							<div class="cge-scenes">
								<div
									v-for="item in state.sceneList"
									:key="item.value"
									class="cge-scene"
									:class="{ 'is-active': Number(store.ruleForm.scene) === Number(item.value), 'is-disabled': state.sceneLocked }"
									@click="chooseScene(item)"
								>
								<el-icon class="cge-scene-check"><ele-CircleCheckFilled /></el-icon>
								<div class="cge-scene-pic" title="点击看大图" @click.stop="openScenePreview(item)">
									<img class="cge-scene-thumb" :src="item.image" :alt="item.label" />
									<span class="cge-scene-zoom"><el-icon><ele-ZoomIn /></el-icon></span>
								</div>
								<div class="cge-scene-body">
										<div class="cge-scene-name">
											{{ item.label }}
											<span class="cge-scene-code">{{ item.value }}</span>
										</div>
										<p class="cge-scene-desc">{{ item.desc }}</p>
										<div class="cge-scene-tags">
											<span v-for="t in item.tags" :key="t" class="cge-scene-tag">{{ t }}</span>
										</div>
									</div>
								</div>
							</div>
						</section>

						<!-- 输出位置 -->
						<section class="cge-card">
							<header class="cge-card-hd">
								<span class="cge-ico is-cyan"><el-icon><ele-FolderOpened /></el-icon></span>
								<div class="cge-card-tt">
									<h4>输出位置</h4>
									<p>代码生成到哪里：前端目录决定页面路径，生成方式决定写文件还是打包下载</p>
								</div>
							</header>
							<div class="cge-grid">
								<el-form-item label="前端目录" prop="pagePath" :rules="pagePathRules">
									<el-input v-model="store.ruleForm.pagePath" clearable placeholder="main" class="w100">
										<template #prepend>src/views/</template>
									</el-input>
									<div class="cge-hint">
										生成后页面路径：<b>src/views/{{ store.ruleForm.pagePath || 'main' }}/{{ state.moduleName || '模块名' }}/</b>
									</div>
								</el-form-item>
								<el-form-item label="生成方式" prop="generateMethod" :rules="[{ required: true, message: '请选择生成方式', trigger: 'change' }]">
									<g-sys-dict v-model="store.ruleForm.generateMethod" code="CodeGenMethodEnum" render-as="select" class="w100" />
									<div class="cge-hint">
										<b>推荐「生成到本项目」</b>：前后端文件直接写入，后端重启后生效；下载模式适合拿到别的项目里用。
									</div>
								</el-form-item>
								<el-form-item label="接口模式" prop="isApiService">
									<el-radio-group v-model="store.ruleForm.isApiService">
										<el-radio :value="true">接口服务</el-radio>
										<el-radio :value="false">传统模式</el-radio>
									</el-radio-group>
									<div class="cge-hint">
										接口服务：按 Swagger 自动生成前端请求文件（需执行 <b>api_build/build.bat</b>），推荐；传统模式：手写接口定义。
									</div>
								</el-form-item>
							</div>
						</section>

						<!-- 附加能力 -->
						<section class="cge-card">
							<header class="cge-card-hd">
								<span class="cge-ico is-amber"><el-icon><ele-MagicStick /></el-icon></span>
								<div class="cge-card-tt">
									<h4>附加能力</h4>
									<p>按需开启：打印、菜单、主从布局</p>
								</div>
							</header>
							<div class="cge-grid">
								<el-form-item label="支持打印" prop="printType">
									<g-sys-dict v-model="store.ruleForm.printType" code="CodeGenPrintTypeEnum" render-as="select" class="w100" />
								</el-form-item>
								<el-form-item label="打印模版" prop="printName" :rules="[{ required: store.ruleForm.printType == 2, message: '请选择打印模版', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.printName" filterable class="w100" :disabled="store.ruleForm.printType != 2" placeholder="选择打印模版">
										<el-option v-for="item in store.printList" :key="item.id" :label="item.name" :value="item.name" />
									</el-select>
								</el-form-item>
								<el-form-item label="生成菜单" prop="generateMenu">
									<el-radio-group v-model="store.ruleForm.generateMenu">
										<el-radio :value="true">是</el-radio>
										<el-radio :value="false">否</el-radio>
									</el-radio-group>
									<div class="cge-hint">开启后自动创建菜单与按钮权限，刷新页面即可看到入口。</div>
								</el-form-item>
								<el-form-item v-if="isMasterSlave" label="布局方向" prop="isHorizontal">
									<el-radio-group v-model="store.ruleForm.isHorizontal">
										<el-radio :value="false">水平（左右）</el-radio>
										<el-radio :value="true">垂直（上下）</el-radio>
									</el-radio-group>
								</el-form-item>
								<template v-if="store.ruleForm.generateMenu">
									<el-form-item label="父级菜单" prop="menuPid">
										<el-cascader
											:options="store.menuList"
											:props="cascaderProps"
											placeholder="请选择上级菜单"
											filterable
											clearable
											class="w100"
											v-model="store.ruleForm.menuPid"
										>
											<template #default="{ node, data }">
												<span>{{ data.title }}</span>
												<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
											</template>
										</el-cascader>
									</el-form-item>
									<el-form-item label="菜单图标" prop="menuIcon" :rules="[{ required: true, message: '请选择菜单图标', trigger: 'change' }]">
										<IconSelector v-model="store.ruleForm.menuIcon as any" :size="other.globalComponentSize()" placeholder="菜单图标" type="all" />
									</el-form-item>
								</template>
							</div>
						</section>
					</el-tab-pane>

					<!-- ============ 树组件配置 ============ -->
					<el-tab-pane v-if="store.showTree()" label="树组件配置" name="1">
						<section class="cge-card">
							<header class="cge-card-hd">
								<span class="cge-ico is-cyan"><el-icon><ele-Share /></el-icon></span>
								<div class="cge-card-tt">
									<h4>树组件</h4>
									<p>左侧树的来源表与字段：显示什么、以哪个字段为节点值、靠哪个字段分层</p>
								</div>
							</header>
							<div class="cge-grid">
								<el-form-item label="库定位器" prop="treeConfig.configId" :rules="[{ required: true, message: '请选择库定位器', trigger: 'change' }]">
									<el-select clearable v-model="store.ruleForm.treeConfig!.configId" placeholder="库名" filterable @change="(val: any) => dbChanged(val)" class="w100">
										<el-option v-for="item in store.databaseList ?? []" :key="item.configId" :label="item.configId" :value="item.configId" />
									</el-select>
								</el-form-item>
								<el-form-item label="数据库表" prop="treeConfig.tableName" :rules="[{ required: true, message: '请选择数据库表', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.treeConfig!.tableName" filterable clearable class="w100" placeholder="选择树数据来源表" @change="(val: any) => tableChanged(store.ruleForm.treeConfig!.configId, val, 'tree')">
										<el-option v-for="item in state.tableData" :key="item.entityName" :label="item.tableName + ' [' + item.tableComment + ']'" :value="item" />
									</el-select>
								</el-form-item>
								<el-form-item label="显示字段" prop="treeConfig.displayPropertyNames" :rules="[{ required: true, message: '请选择显示字段', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.treeConfig!.displayPropertyNames" filterable class="w100" placeholder="树节点上显示的文字">
										<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
									</el-select>
								</el-form-item>
								<el-form-item label="值字段" prop="treeConfig.linkPropertyName" :rules="[{ required: true, message: '请选择值字段', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.treeConfig!.linkPropertyName" filterable class="w100" placeholder="节点选中后传给列表的字段" @change="(val: any) => changeTreePropertyName(val, 'link')">
										<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item" />
									</el-select>
								</el-form-item>
								<el-form-item label="父级字段" prop="treeConfig.parentPropertyName" :rules="[{ required: true, message: '请选择父级字段', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.treeConfig!.parentPropertyName" filterable class="w100" placeholder="用于分层的自关联字段" @change="(val: any) => changeTreePropertyName(val, 'parent')">
										<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
									</el-select>
								</el-form-item>
								<el-form-item label="查询字段" prop="treeConfig.searchPropertyName" :rules="[{ required: true, message: '请选择查询字段', trigger: 'change' }]">
									<el-select v-model="store.ruleForm.treeConfig!.searchPropertyName" filterable class="w100" placeholder="树上方搜索框匹配的字段" @change="(val: any) => changeTreePropertyName(val, 'search')">
										<el-option v-for="item in state.columnList" :key="item.propertyName" :label="item.columnName + ' [' + item.columnComment + ']'" :value="item.propertyName" />
									</el-select>
								</el-form-item>
								<el-form-item label="树标题" prop="treeConfig.treeTitle" :rules="[{ required: true, message: '请填写树标题', trigger: 'blur' }]">
									<el-input v-model="store.ruleForm.treeConfig!.treeTitle" clearable placeholder="如：组织树" class="w100" />
								</el-form-item>
								<el-form-item label="是否多选">
									<el-radio-group v-model="store.ruleForm.treeConfig!.multiple">
										<el-radio :value="true">是</el-radio>
										<el-radio :value="false">否</el-radio>
									</el-radio-group>
								</el-form-item>
							</div>
						</section>
					</el-tab-pane>

					<!-- ============ 主表 / 从表 ============ -->
					<el-tab-pane v-if="store.showMaster()" label="主表配置" name="2">
						<TableConfig :index="0" />
					</el-tab-pane>
					<el-tab-pane v-if="store.showSlave()" label="从表配置" name="3">
						<TableConfig :index="1" />
					</el-tab-pane>
				</el-tabs>
			</el-form>

			<template #footer>
				<div class="cge-ft">
					<div class="cge-ft-sum">
						<el-icon class="is-ok" v-if="state.summary.ready"><ele-CircleCheckFilled /></el-icon>
						<el-icon class="is-warn" v-else><ele-WarningFilled /></el-icon>
						<span v-html="state.summary.text"></span>
					</div>
					<div class="cge-ft-acts">
						<el-button icon="ele-CircleCloseFilled" @click="cancel" :disabled="state.loading">取 消</el-button>
						<el-button icon="ele-CircleCheckFilled" @click="submit(false)" :disabled="state.loading" v-reclick="1000">保 存</el-button>
						<el-button icon="ele-VideoPlay" type="primary" @click="submit(true)" :disabled="state.loading" v-reclick="1000">保存并生成</el-button>
					</div>
				</div>
			</template>
		</el-dialog>

		<!-- 场景大图预览 -->
		<el-dialog v-model="state.scenePreviewVisible" width="92vw" top="4vh" class="cge-preview-dialog" :show-close="true">
			<template #header>
				<div class="cge-hd">
					<span class="cge-hd-ico"><el-icon><ele-Picture /></el-icon></span>
					<div class="cge-hd-txt">
						<span class="cge-hd-title">{{ state.scenePreviewTitle }}</span>
						<span class="cge-hd-sub">{{ state.scenePreviewDesc }}</span>
					</div>
				</div>
			</template>
			<div class="cge-preview-body">
				<img :src="state.scenePreviewSrc" :alt="state.scenePreviewTitle" />
			</div>
		</el-dialog>
	</div>
</template>

<!-- 代码生成配置 -->
<script lang="ts" setup name="sysEditCodeGen">
import { computed, defineAsyncComponent, nextTick, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import other from '/@/utils/other';
import IconSelector from '/@/components/iconSelector/index.vue';
import { useUserInfo } from '/@/stores/userInfo';

import { useCodeGenStore } from './codeGenStore';

const TableConfig = defineAsyncComponent(() => import('./tableConfig.vue'));
const emits = defineEmits(['handleQuery']);
const store = useCodeGenStore();
const userInfo = useUserInfo();
const ruleFormRef = ref();

const state = reactive({
	title: '编辑代码生成',
	subtitle: '',
	isShowDialog: false,
	tableData: [] as any,
	columnList: [] as any,
	loading: false,
	activeTab: '0',
	sceneLocked: false,
	sceneList: [] as any[],
	scenePreviewVisible: false,
	scenePreviewSrc: '',
	scenePreviewTitle: '场景预览',
	scenePreviewDesc: '',
	summary: { ready: false, text: '' },
});

// 级联选择器配置选项
const cascaderProps = { checkStrictly: true, emitPath: false, value: 'id', label: 'title' };

// ========== 场景元数据（图片 + 一句话说明） ==========
const BASE = import.meta.env.BASE_URL || '/';
const sceneMeta: Record<number, { desc: string; tags: string[] }> = {
	1000: { desc: '一张表生成列表页 + 编辑弹窗，90% 的业务都是这种', tags: ['列表页', '编辑弹窗'] },
	1010: { desc: '左侧树 + 右侧列表，点树节点筛选右侧数据', tags: ['树筛选', '联动列表'] },
	2000: { desc: '主表带明细：点主表行，从表自动过滤', tags: ['主从联动', '双弹窗'] },
	2010: { desc: '树 + 主表 + 从表，三级联动台账', tags: ['三级联动', '台账'] },
};

// 当前支持的场景：单表 / 单表树 / 主从表 / 主从表树
const SUPPORTED_SCENES = [1000, 1010, 2000, 2010];

const buildSceneList = () => {
	const dict = userInfo.dictList?.['CodeGenSceneEnum'] ?? [];
	return dict
		.filter((e: any) => SUPPORTED_SCENES.includes(Number(e.value)))
		.sort((a: any, b: any) => Number(a.value) - Number(b.value))
		.map((e: any) => {
			const v = Number(e.value);
			const meta = sceneMeta[v] ?? { desc: '暂无说明', tags: [] };
			return {
				value: v,
				label: e.label,
				desc: meta.desc,
				tags: meta.tags,
				image: `${BASE}images/codegen/scene-${sceneMeta[v] ? v : 'none'}.svg`,
			};
		});
};

const sceneItem = (val: any) => state.sceneList.find((x) => Number(x.value) === Number(val));

const isMasterSlave = computed(() => [2000, 2010].includes(Number(store.ruleForm.scene ?? 0)));

// ========== 实时校验 ==========
const validateBusName = (_rule: any, value: any, cb: any) => {
	if (!value) return cb(new Error('业务名不能为空'));
	const v = String(value).trim();
	if (v.length < 2) return cb(new Error('业务名至少 2 个字符'));
	if (v.length > 30) return cb(new Error('业务名不超过 30 个字符'));
	if (/[<>"\\/|?*:]/.test(v)) return cb(new Error('不能包含 < > " \\ / | ? * : 等字符'));
	cb();
};
const validatePagePath = (_rule: any, value: any, cb: any) => {
	if (!value) return cb(new Error('前端目录不能为空'));
	const v = String(value).trim();
	if (!/^[a-z][a-z0-9-]*(\/[a-z0-9-]+)*$/.test(v)) return cb(new Error('只允许小写字母开头，可含数字、中划线与 / 分隔'));
	cb();
};
const busNameRules = [{ validator: validateBusName, trigger: 'blur' }];
const pagePathRules = [{ validator: validatePagePath, trigger: 'blur' }];

// db改变
const dbChanged = async (configId: string) => {
	if (!configId) return;
	state.tableData = await store.tableListBy(configId);
};

// 场景切换：可点选（新建）或只可查看（编辑已有）
const chooseScene = (item: any) => {
	if (state.sceneLocked) {
		openScenePreview(item);
		return;
	}
	store.ruleForm.scene = item.value;
};

const openScenePreview = (item: any) => {
	if (!item) return;
	state.scenePreviewSrc = item.image;
	state.scenePreviewTitle = `${item.label}（${item.value}）`;
	state.scenePreviewDesc = item.desc;
	state.scenePreviewVisible = true;
};

// table改变
const tableChanged = async (configId: string, item: any, type: string) => {
	if (typeof item === `string`) item = state.tableData.find((i: any) => i.entityName == item || i.tableName == item);
	if (type == 'tree') {
		store.ruleForm.treeConfig ??= {} as any;
		store.ruleForm.treeConfig!.tableComment = item?.tableComment ?? undefined;
		store.ruleForm.treeConfig!.entityName = item?.entityName ?? undefined;
		if (store.ruleForm.treeConfig!.tableName != item?.tableName) {
			store.ruleForm.treeConfig!.tableName = item?.tableName ?? undefined;
			store.ruleForm.treeConfig!.displayPropertyNames = undefined as any;
			store.ruleForm.treeConfig!.parentPropertyName = undefined;
			store.ruleForm.treeConfig!.searchPropertyName = undefined;
			store.ruleForm.treeConfig!.linkPropertyName = undefined as any;
		}
	}
	state.columnList = (await store.columnListBy(configId, item?.tableName)) ?? [];
};

// 属性改变
const changeTreePropertyName = (item: any, type: string) => {
	if (type == 'display') {
		store.ruleForm.treeConfig!.displayPropertyNames = item.propertyName;
	} else if (type == 'link') {
		store.ruleForm.treeConfig!.linkPropertyName = item.propertyName;
		store.ruleForm!.treeConfig!.linkPropertyType = item.netType;
	} else if (type == 'search') {
		store.ruleForm.treeConfig!.searchPropertyName = item.propertyName;
		store.ruleForm!.treeConfig!.searchPropertyType = item.netType;
	} else if (type == 'parent') {
		store.ruleForm.treeConfig!.parentPropertyName = item.propertyName;
		store.ruleForm!.treeConfig!.parentPropertyType = item.netType;
	}
};

// 底部摘要：表 / 字段 / 列表字段统计，让用户一眼确认"配全了没"
const moduleName = computed(() => store.ruleForm.tableList?.[0]?.moduleName || '');

const refreshSummary = () => {
	const tables = store.ruleForm.tableList ?? [];
	const main = tables[0];
	const cols = main?.columnList ?? [];
	const listCols = cols.filter((c: any) => c.isTable).length;
	const tabs: number[] = [];
	if (store.showTree()) {
		const t = store.ruleForm.treeConfig;
		if (!t?.tableName || !t?.displayPropertyNames || !t?.linkPropertyName || !t?.parentPropertyName || !t?.searchPropertyName) tabs.push(1);
	}
	if (store.showMaster() && !main?.tableName) tabs.push(2);
	if (store.showSlave() && !tables[1]?.tableName) tabs.push(3);

	const ready = !tabs.length && !!store.ruleForm.busName && !!store.ruleForm.nameSpace && !!store.ruleForm.pagePath;
	let text = '';
	if (!main?.tableName) {
		text = '还没选表：切到「主表配置」选择数据库表后，字段会自动带出来';
	} else if (ready) {
		text = `主表 <b>${main.entityName ?? main.tableName}</b> · 共 ${cols.length} 个字段 · 列表显示 ${listCols} 个，配置完整可以生成`;
	} else {
		text = `主表 <b>${main.entityName ?? main.tableName}</b> · ${cols.length} 个字段，还有 ${tabs.length ? '必填项' : '基础信息'}待补充`;
	}
	state.summary = { ready, text };
};

watch(
	() => [store.ruleForm.scene, store.ruleForm.busName, store.ruleForm.nameSpace, store.ruleForm.pagePath, store.ruleForm.tableList?.map((t: any) => t?.tableName + ':' + (t?.columnList?.length ?? 0)).join(',')],
	() => refreshSummary(),
	{ deep: true }
);

// 打开弹窗
const openDialog = async (row: any) => {
	store.enabledWatch = !row?.id;
	state.sceneList = buildSceneList();
	state.sceneLocked = !!row?.id;

	let scene = 1000 as any;
	try {
		state.loading = true;
		state.activeTab = '0';
		ruleFormRef.value?.resetFields();
		state.title = (row.id ? '编辑' : '新增') + '代码生成';
		state.subtitle = row?.busName ? `${row.busName}` : '';
		store.ruleForm = row?.id ? await store.getDetail(row.id) : JSON.parse(JSON.stringify(row));
		scene = store.ruleForm.scene;
		store.ruleForm.scene = undefined;
		store.ruleForm.nameSpace ??= store.namespaceList?.[0] as any;

		// 使用配置的默认值（仅在新增时，直接覆盖）
		if (!row?.id && store.defaultConfig) {
			store.ruleForm.authorName = store.defaultConfig.authorName || store.ruleForm.authorName;
			store.ruleForm.email = store.defaultConfig.authorEmail || store.ruleForm.email;
			store.ruleForm.pagePath = store.defaultConfig.frontendDirectory || store.ruleForm.pagePath;
			scene = store.defaultConfig.sceneType;
		}

		state.isShowDialog = true;
	} finally {
		await nextTick(() => {
			store.ruleForm.scene = scene ?? 1000;
			refreshSummary();
		});
		state.loading = false;
	}
	store.enabledWatch = true;
};

// 关闭弹窗
const closeDialog = (generate = false) => {
	emits('handleQuery', { generate });
	state.isShowDialog = false;
};

// 取消
const cancel = () => {
	state.isShowDialog = false;
};

// 提交
const submit = (generate = false) => {
	store.ruleForm.busName ??= store.ruleForm.tableList?.[0]?.busName;
	const data = Object.assign(store.ruleForm, {
		moduleName: store.ruleForm.moduleName ?? store.ruleForm.tableList?.[0]?.moduleName,
	});
	ruleFormRef.value.validate(async (isValid: boolean, fields?: any) => {
		if (isValid) {
			data.treeConfig = <number>data.scene % 100 == 10 ? data.treeConfig : undefined;
			state.loading = true;
			try {
				await store.save(data);
			} finally {
				state.loading = false;
			}
			closeDialog(generate);
		} else {
			// 直接跳到第一个出错的页签，避免用户找不到哪里没填
			const first = Object.keys(fields ?? {})[0] ?? '';
			if (first.startsWith('treeConfig')) state.activeTab = '1';
			else if (first.startsWith('tableList[1]')) state.activeTab = '3';
			else if (first.startsWith('tableList')) state.activeTab = '2';
			else state.activeTab = '0';
			ElMessage({ message: `表单有 ${Object.keys(fields ?? {}).length} 处验证失败，请修改后再提交`, type: 'error' });
		}
	});
};

// 导出对象
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
:deep(.el-overlay) {
	z-index: 999 !important;
}

/* ===================== 弹窗骨架 ===================== */
.cge-dialog {
	:deep(.el-dialog__body) {
		padding: 0 16px;
		overflow: hidden;
	}
}

.cge-hd {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
}

.cge-hd-ico {
	flex: 0 0 auto;
	width: 28px;
	height: 28px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 15px;
	background: color-mix(in srgb, currentColor 20%, transparent);
}

.cge-hd-txt {
	flex: 0 1 auto;
	min-width: 0;
	display: flex;
	align-items: baseline;
	gap: 10px;
}

.cge-hd-title {
	font-size: 15px;
	font-weight: 700;
	letter-spacing: 0.3px;
}

.cge-hd-sub {
	font-size: 12px;
	opacity: 0.85;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.cge-hd-tip {
	margin-left: auto;
	font-size: 12px;
	opacity: 0.75;
	letter-spacing: 0.2px;
}

/* ===================== 页签与滚动 ===================== */
.cge-form {
	height: 100%;
	min-height: 0;
}

.cge-tabs {
	height: 100%;
	display: flex;
	flex-direction: column;

	:deep(> .el-tabs__header) {
		flex: 0 0 auto;
		margin: 0 0 4px;
	}

	:deep(> .el-tabs__content) {
		flex: 1 1 auto;
		min-height: 0;
		overflow: auto;
		padding: 10px 2px 16px;
	}
}

/* ===================== 分组卡片 ===================== */
.cge-card {
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 12px;
	padding: 16px 18px 2px;
	background: var(--el-bg-color);
	transition: border-color 0.25s ease;

	& + & {
		margin-top: 14px;
	}

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 26%, var(--el-border-color-lighter));
	}
}

.cge-card-hd {
	display: flex;
	align-items: center;
	gap: 10px;
	margin-bottom: 14px;
}

.cge-card-tt {
	flex: 1 1 auto;
	min-width: 0;

	h4 {
		margin: 0;
		font-size: 13.5px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		letter-spacing: 0.2px;
	}

	p {
		margin: 3px 0 0;
		font-size: 11.5px;
		line-height: 1.5;
		color: var(--el-text-color-secondary);
	}
}

.cge-card-tag {
	flex: 0 0 auto;
}

/* 图标装饰：浅色同色系底 + 深色图标 */
.cge-ico {
	flex: 0 0 auto;
	width: 30px;
	height: 30px;
	border-radius: 9px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
	--tint: var(--el-color-primary);
	color: var(--tint);
	background: color-mix(in srgb, var(--tint) 12%, transparent);

	&.is-cyan {
		--tint: #0ea5e9;
	}
	&.is-purple {
		--tint: #8b5cf6;
	}
	&.is-amber {
		--tint: #f59e0b;
	}
}

.cge-grid {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 0 22px;

	:deep(.el-form-item) {
		margin-bottom: 18px;
		align-items: flex-start;
	}

	:deep(.el-form-item__label) {
		height: 32px;
		line-height: 32px;
		color: var(--el-text-color-regular);
	}

	:deep(.el-form-item__content) {
		flex-direction: column;
		align-items: stretch;
	}
}

.cge-hint {
	margin-top: 4px;
	font-size: 11.5px;
	line-height: 1.5;
	color: var(--el-text-color-secondary);

	b {
		color: var(--el-color-primary);
		font-weight: 600;
	}
}

/* ===================== 场景卡片 ===================== */
.cge-scenes {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 12px;
	margin-bottom: 16px;
}

.cge-scene {
	position: relative;
	display: flex;
	align-items: stretch;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	overflow: hidden;
	background: var(--el-bg-color);
	cursor: pointer;
	transition:
		transform 0.22s ease,
		border-color 0.22s ease,
		box-shadow 0.22s ease;

	&:hover {
		transform: translateY(-2px);
		border-color: color-mix(in srgb, var(--el-color-primary) 35%, var(--el-border-color-lighter));
		box-shadow: 0 10px 22px -16px color-mix(in srgb, var(--el-color-primary) 70%, transparent);

		.cge-scene-zoom {
			opacity: 1;
		}
	}

	&.is-active {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 2px color-mix(in srgb, var(--el-color-primary) 20%, transparent);

		.cge-scene-check {
			opacity: 1;
			transform: scale(1);
		}
	}

	&.is-disabled {
		cursor: default;

		.cge-scene-thumb {
			filter: saturate(0.85);
		}
	}
}

/* 缩略图统一收成左侧小图，完整页面布局点开大图再看 */
.cge-scene-pic {
	position: relative;
	flex: none;
	width: 108px;
	align-self: stretch;
	min-height: 76px;
	background: #eef2f7;
	border-right: 1px solid var(--el-border-color-lighter);
	cursor: zoom-in;
}

.cge-scene-thumb {
	display: block;
	width: 100%;
	height: 100%;
	min-height: 76px;
	object-fit: cover;
	object-position: top center;
}

.cge-scene-check {
	position: absolute;
	top: 8px;
	left: 8px;
	font-size: 18px;
	color: var(--el-color-primary);
	background: #fff;
	border-radius: 50%;
	opacity: 0;
	transform: scale(0.8);
	transition:
		opacity 0.2s ease,
		transform 0.2s ease;
}

.cge-scene-zoom {
	position: absolute;
	right: 4px;
	bottom: 4px;
	width: 22px;
	height: 22px;
	border-radius: 6px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	color: var(--el-color-primary);
	background: rgba(255, 255, 255, 0.92);
	box-shadow: 0 2px 6px -2px rgba(0, 0, 0, 0.2);
	opacity: 0;
	transition: opacity 0.2s ease;
}

.cge-scene-body {
	flex: 1 1 auto;
	min-width: 0;
	padding: 9px 11px 10px;
}

.cge-scene-name {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.cge-scene-code {
	font-size: 10.5px;
	font-weight: 600;
	color: var(--el-text-color-placeholder);
	background: var(--el-fill-color-light);
	border-radius: 5px;
	padding: 0 5px;
	line-height: 16px;
}

.cge-scene-desc {
	margin: 4px 0 0;
	font-size: 11px;
	line-height: 1.55;
	color: var(--el-text-color-secondary);
	min-height: 34px;
}

.cge-scene-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
	margin-top: 6px;
}

.cge-scene-tag {
	font-size: 10.5px;
	line-height: 17px;
	padding: 0 6px;
	border-radius: 8px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

/* ===================== 底部 ===================== */
.cge-ft {
	display: flex;
	align-items: center;
	gap: 12px;
}

.cge-ft-sum {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
	text-align: left;

	:deep(b) {
		color: var(--el-text-color-primary);
		font-weight: 600;
	}

	.el-icon {
		font-size: 15px;
	}

	.is-ok {
		color: var(--el-color-success);
	}

	.is-warn {
		color: var(--el-color-warning);
	}
}

.cge-ft-acts {
	flex: 0 0 auto;
	display: flex;
	align-items: center;
	gap: 8px;

	:deep(.el-button + .el-button) {
		margin-left: 0;
	}
}

/* ===================== 场景大图 ===================== */
.cge-preview-dialog {
	:deep(.el-dialog__body) {
		padding: 12px;
		background: var(--el-fill-color-lighter);
	}
}

.cge-preview-body {
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

@media (max-width: 1280px) {
	.cge-scenes {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

@media (max-width: 900px) {
	.cge-grid {
		grid-template-columns: 1fr;
	}
}
</style>
