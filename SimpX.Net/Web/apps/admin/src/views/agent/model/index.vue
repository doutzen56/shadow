<template>
	<div class="agents-model-container">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">模型管理</h2>
					<p class="page-hero__desc">统一维护对话、矢量、文生图与重排模型；密钥只写不读，列表接口不会回传。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.total }}</span>
							<span class="page-hero__stat-k">模型总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.chat }}</span>
							<span class="page-hero__stat-k">本页 Chat</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.embedding }}</span>
							<span class="page-hero__stat-k">本页矢量</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增按钮放这里，不进卡片区，避免随卡片滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="模型名称 / 部署名" clearable @keyup.enter="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'agent/model/addEdit'"> 新增模型 </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty :image-size="160" description="暂无模型，新增后即可在智能体里选用" v-if="!state.tableData.length" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.tableData" :key="item.id">
						<el-card shadow="never" class="entity-card entity-card--accent" :class="`is-${modelAccent(item.aiModelType)}`">
							<i class="ri-cpu-fill entity-card__watermark" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i class="ri-cpu-fill" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ item.modelName }}</div>
									<div class="entity-card__sub">{{ aiTypeText(item.aiType) }}</div>
								</div>
								<el-tag size="small" :type="modelAccent(item.aiModelType)" effect="light">{{ modelTypeText(item.aiModelType) }}</el-tag>
							</div>

							<div class="entity-card__body">
								<!-- 地址是这张卡最需要核对的信息，两行足够放下常见域名 -->
								<div class="code-preview model-endpoint">{{ item.endPoint }}</div>

								<!-- 不同模型类型关心的参数不同：对话模型看上下文/输出，矢量模型只看维度 -->
								<div class="metric-group">
									<template v-if="item.aiModelType === 1">
										<div class="metric-item">
											<span>提问 Token</span><b>{{ formatNumber(item.maxAskPromptSize) }}</b>
										</div>
										<div class="metric-item">
											<span>回答 Token</span><b>{{ formatNumber(item.answerTokens) }}</b>
										</div>
									</template>
									<div v-else-if="item.aiModelType === 2" class="metric-item">
										<span>向量维度</span><b>{{ formatNumber(item.embeddingValueSize) }}</b>
									</div>
								</div>

								<!-- 能力标签：每类能力一个颜色，扫读时先分清"能不能收图片 / 音频 / 视频" -->
								<div class="tag-group" v-if="capabilityTags(item.capabilities).length">
									<el-tag v-for="cap in capabilityTags(item.capabilities)" :key="cap.key" size="small" effect="light" :type="cap.type">{{ cap.label }}</el-tag>
								</div>
								<!-- 聊天模型没勾任何能力时给一句说明：空白会让人以为是没显示出来 -->
								<span v-else-if="item.aiModelType === 1" class="cell-sub">未声明能力 · 按纯文本模型处理</span>

								<div class="entity-card__meta" v-if="item.modelDescription">
									<span
										>部署名 <b>{{ item.modelDescription }}</b></span
									>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ formatTime(item.createTime) }}</span>
								<div class="entity-card__actions">
									<el-button icon="ele-Edit" text type="primary" @click="handleEdit(item)" v-auth="'agent/model/addEdit'"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" @click="handleDelete(item)" v-auth="'agent/model/delete'"> 删除 </el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				class="table-pager"
				background
				v-model:current-page="state.queryParams.page"
				v-model:page-size="state.queryParams.pageSize"
				:page-sizes="[12, 24, 48, 96]"
				layout="total, sizes, prev, pager, next, jumper"
				:total="state.total"
				@change="handleQuery"
			/>
		</el-card>

		<!-- 新增/编辑 -->
		<el-dialog v-model="state.editVisible" width="720px" draggable>
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>{{ state.editTitle }}</span>
				</div>
			</template>
			<el-form :model="state.form" ref="editFormRef" :rules="rules" label-width="auto">
				<!--规避浏览器自动填充账号：假框必须带 username/password 信号，Chrome 才会优先把保存的账号密码填进假框；display:none 会被跳过，故用屏幕外定位-->
				<input type="text" name="username" autocomplete="username" tabindex="-1" aria-hidden="true" style="position: absolute; top: -9999px; left: -9999px" />
				<input type="password" name="password" autocomplete="current-password" tabindex="-1" aria-hidden="true" style="position: absolute; top: -9999px; left: -9999px" />
				<el-form-item label="模型类型" prop="aiModelType">
					<el-select v-model="state.form.aiModelType" placeholder="请选择模型类型" style="width: 100%" @change="onModelTypeChange">
						<el-option label="Chat 对话模型" :value="1" />
						<el-option label="Embedding 矢量模型" :value="2" />
						<el-option label="TextToImage 文生图模型" :value="3" />
						<el-option label="Rerank 重排模型" :value="4" />
						<el-option label="ImageTo3D 图生三维模型" :value="5" />
					</el-select>
				</el-form-item>
				<el-form-item label="AI类型" prop="aiType">
					<el-select v-model="state.form.aiType" placeholder="请选择AI类型" style="width: 100%">
						<el-option v-for="item in aiTypeOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<el-form-item label="模型地址" prop="endPoint">
					<div class="endpoint-row">
						<el-input v-model="state.form.endPoint" placeholder="如：https://api.deepseek.com/v1" clearable />
						<el-button icon="ele-VideoPlay" :loading="state.probing" @click="handleProbe">测试连接</el-button>
						<el-button icon="ele-Download" :loading="state.fetching" @click="handleFetchModels">拉取模型</el-button>
					</div>
					<div class="tip">需指向 OpenAI 兼容端点；Ollama 为 http://127.0.0.1:11434/v1，末尾的 /v1 不能少</div>
				</el-form-item>
				<el-form-item label="模型名称" prop="modelName">
					<!-- 拉取过模型列表后可以直接选，没拉取时也能手动输入（allow-create） -->
					<el-select v-model="state.form.modelName" filterable allow-create default-first-option clearable placeholder="输入模型标识，或先拉取服务端模型列表" style="width: 100%">
						<el-option v-for="name in state.models" :key="name" :label="name" :value="name" />
					</el-select>
					<div class="tip">填服务端的真实模型标识，不是供应商名称：Ollama 执行 ollama list 查看本机已有模型，填成 ollama 会返回 404 model not found</div>
				</el-form-item>
				<el-form-item v-if="state.probe" label=" ">
					<div class="probe" :class="state.probe.success ? 'is-ok' : 'is-fail'">
						<div class="probe__head">
							<i :class="state.probe.success ? 'ri-checkbox-circle-fill' : 'ri-error-warning-fill'" />
							<span class="probe__msg">{{ state.probe.message }}</span>
						</div>
						<div class="probe__meta">
							<span>耗时 {{ state.probe.latencyMs }} ms</span>
							<span v-if="state.probe.source">来源 {{ state.probe.source }}</span>
							<span v-if="state.models.length">{{ state.models.length }} 个可选模型</span>
							<span v-if="state.probe.embeddingSize">实测 {{ state.probe.embeddingSize }} 维</span>
						</div>
					</div>
				</el-form-item>
				<el-form-item label="模型秘钥">
					<el-input v-model="state.form.modelKey" type="password" autocomplete="new-password" show-password placeholder="原生 Ollama 等无鉴权模型可留空" clearable />
				</el-form-item>
				<el-form-item label="部署名">
					<el-input v-model="state.form.modelDescription" placeholder="Azure 等平台需要使用" clearable />
				</el-form-item>
				<el-form-item label="矢量精度" v-if="state.form.aiModelType === 2">
					<el-input-number v-model="state.form.embeddingValueSize" :min="64" :max="8192" :step="64" controls-position="right" style="width: 100%" />
					<div class="tip">需与 Embedding 模型输出维度一致（默认 2048 对应智谱 embedding-3）</div>
				</el-form-item>
				<el-form-item label="提问Token" v-if="state.form.aiModelType === 1">
					<el-input-number v-model="state.form.maxAskPromptSize" :min="1024" :step="1024" controls-position="right" style="width: 100%" />
					<div class="tip">模型上下文窗口预算，控制历史对话与补充上下文总量</div>
				</el-form-item>
				<el-form-item label="回答Token" v-if="state.form.aiModelType === 1">
					<el-input-number v-model="state.form.answerTokens" :min="256" :step="256" controls-position="right" style="width: 100%" />
					<div class="tip">模型最大输出长度，超出模型范围会直接报错，可参考：4096 / 8192 / 16384 / 32768</div>
				</el-form-item>
				<el-form-item label="能力标签" v-if="state.form.aiModelType === 1">
					<el-select v-model="capabilityList" multiple clearable placeholder="可多选" style="width: 100%">
						<el-option v-for="item in capabilityOptions" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
					<div class="tip">
						「图片输入」：工具产出的图片（如三维场景截图）可直接作为多模态内容交给该模型，不勾选则退化为文字提示；「音频输入」：用户上传的音频可直接交给该模型，不勾选则忽略音频；「视频输入」：仅作能力声明，上传投喂链路暂未接入
					</div>
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.saving" @click="handleSubmit">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="agentModel">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance } from 'element-plus';

import { CardAccent } from '/@/views/agent/cardAccent';
import { axiosInstance, getAPI } from '/@/utils/axios-utils';
import { AgentModelApi } from '/@/api-services/agent/api';
import { AgentModelDto, AgentPageInput } from '/@/api-services/agent/models';

/** 探测接口返回，字段与后端 AgentModelProbeOutput 对齐 */
type ProbeResult = {
	success: boolean;
	message: string;
	latencyMs: number;
	source?: string | null;
	modelVerified?: boolean;
	embeddingSize?: number;
	models?: string[];
};

const editFormRef = ref<FormInstance>();

const aiTypeOptions = [
	{ label: 'Open AI（兼容大部分平台）', value: 1 },
	{ label: 'Azure Open AI', value: 2 },
	{ label: '智谱AI', value: 3 },
	{ label: 'Bge Embedding', value: 7 },
	{ label: 'Bge Rerank', value: 8 },
	{ label: 'Ollama', value: 10 },
	{ label: 'OllamaEmbedding', value: 11 },
	{ label: 'Ali Rerank', value: 12 },
];

/** 能力标签选项：与后端 Framework.ModelCapabilities 常量一致，新增能力时两边各加一项即可，不用改表结构 */
const capabilityOptions = [
	{ label: '图片输入', value: 'vision' },
	{ label: '音频输入', value: 'audio' },
	{ label: '视频输入', value: 'video' },
];

/** 能力标签在表单里是数组，存储形态是逗号分隔字符串 */
const capabilityList = computed<string[]>({
	get: () => (state.form.capabilities ? state.form.capabilities.split(',').filter(Boolean) : []),
	set: (value) => {
		state.form.capabilities = value.join(',');
	},
});

/**
 * 卡片显示用的标签文案：表单只开放图片/音频/视频三项，
 * 但库里历史数据还出现过 image_gen / tool 这类键，同样给中文名，免得卡片上直接露出英文键
 */
const capabilityLabelsMap: Record<string, string> = {
	...Object.fromEntries(capabilityOptions.map((item) => [item.value, item.label])),
	image_gen: '文生图',
	tool: '工具调用',
};

/**
 * 每类能力固定一个语义色：颜色不承载状态，只用来在卡片墙上快速区分是哪种能力。
 * 「工具调用」沿用智能体卡片里「工具」的 primary，两个页面的同一概念不换色；未登记的键落到 info
 */
const CAPABILITY_TYPES: Record<string, CardAccent> = {
	tool: 'primary',
	vision: 'success',
	audio: 'warning',
	video: 'danger',
	image_gen: 'info',
};

/** 能力标签：键去重后带上中文名与颜色，同一类能力在一块卡片上只出现一次 */
const capabilityTags = (capabilities?: string | null): { key: string; label: string; type: CardAccent }[] => {
	if (!capabilities) return [];
	const keys = [...new Set(capabilities.split(',').map((key) => key.trim()).filter(Boolean))];
	return keys.map((key) => ({ key, label: capabilityLabelsMap[key] ?? key, type: CAPABILITY_TYPES[key] ?? 'info' }));
};

const rules = {
	aiModelType: [{ required: true, message: '请选择模型类型', trigger: 'change' }],
	aiType: [{ required: true, message: '请选择AI类型', trigger: 'change' }],
	endPoint: [{ required: true, message: '请输入模型地址', trigger: 'blur' }],
	modelName: [{ required: true, message: '请输入模型名称', trigger: 'blur' }],
};

const state = reactive({
	loading: false,
	saving: false,
	total: 0,
	tableData: [] as AgentModelDto[],
	queryParams: { page: 1, pageSize: 24, searchKey: '' } as AgentPageInput,
	editVisible: false,
	editTitle: '',
	form: {} as AgentModelDto,
	probing: false,
	fetching: false,
	models: [] as string[],
	probe: null as ProbeResult | null,
});

/** 卡片副标题只放平台名：选项里的括号说明太长，会把标题挤成一行省略号 */
const aiTypeText = (value?: number) => aiTypeOptions.find((item) => item.value === value)?.label.replace(/（.*?）/g, '') ?? '-';

/** 模型类型文案：与新增/编辑弹窗里的选项一一对应（1=Chat 2=Embedding 3=文生图 4=Rerank 5=图生三维） */
const modelTypeText = (value?: number) => {
	if (value === 2) return 'Embedding';
	if (value === 3) return '文生图';
	if (value === 4) return 'Rerank';
	if (value === 5) return '图生三维';
	return 'Chat';
};

/** 整卡统一主题色：类型信息已由右上角文字标签承载，配色不再重复表达一遍（五种类型色混排会让列表显得杂） */
const modelAccent = (_value?: number): CardAccent => 'primary';

const formatTime = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 10) : '-');

/** Token 数动辄六位数，加千分位后一眼能读出量级 */
const formatNumber = (value?: number | null) => (value === null || value === undefined ? '-' : value.toLocaleString());

/** 头图统计：总数取接口返回值，Chat 与矢量模型按当前页汇总 */
const heroStats = computed(() => ({
	total: state.total,
	chat: state.tableData.filter((t) => t.aiModelType === 1).length,
	embedding: state.tableData.filter((t) => t.aiModelType === 2).length,
}));

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await getAPI(AgentModelApi).apiAgentModelPagePost(state.queryParams);
		state.tableData = res.data.result?.items ?? [];
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

const resetQuery = () => {
	state.queryParams.searchKey = '';
	state.queryParams.page = 1;
	handleQuery();
};

const handleAdd = () => {
	state.editTitle = '新增模型';
	state.form = {
		aiModelType: 1, // Chat
		aiType: 1, // OpenAI 兼容
		endPoint: '',
		modelName: '',
		embeddingValueSize: 2048,
		maxAskPromptSize: 131072,
		answerTokens: 8192,
		capabilities: '',
	} as AgentModelDto;
	state.models = [];
	state.probe = null;
	state.editVisible = true;
};

const handleEdit = (row: AgentModelDto) => {
	state.editTitle = '编辑模型';
	// 密钥不回显（列表接口已清空），需重新填写才会更新
	state.form = { ...row, modelKey: '' };
	state.models = [];
	state.probe = null;
	state.editVisible = true;
};

/** 探测/拉取共用一个接口：带模型名时额外校验模型是否存在，不带则只取列表 */
const runProbe = async (withModel: boolean) => {
	if (!state.form.endPoint?.trim()) {
		ElMessage.warning('请先填写模型地址');
		return;
	}

	// 地址或鉴权变了，之前拉到的列表与结论都不再可信
	state.models = [];
	state.probe = null;
	state.probing = withModel;
	state.fetching = !withModel;
	try {
		const res = await axiosInstance.post('/api/agentModel/probe', {
			// 编辑已有模型时秘钥不回填，带上 Id 让后端沿用库里的秘钥
			id: state.form.id ?? 0,
			endPoint: state.form.endPoint,
			modelKey: state.form.modelKey ?? '',
			modelName: withModel ? (state.form.modelName ?? '') : '',
			aiModelType: state.form.aiModelType ?? 1,
		});
		const result = res.data?.result as ProbeResult | undefined;
		if (!result) {
			// 业务错误已由响应拦截器弹过一次，这里只把结论落到面板
			state.probe = { success: false, message: res.data?.message || '探测失败：接口未返回结果', latencyMs: 0 };
			return;
		}

		state.probe = result;
		state.models = result.models ?? [];
		if (result.success) ElMessage.success(withModel ? '连接正常' : `已拉取 ${state.models.length} 个模型`);
	} catch (error) {
		// 业务错误提示由响应拦截器统一弹出，这里只把结论落到面板上
		state.probe = { success: false, message: error instanceof Error ? error.message : '探测请求失败', latencyMs: 0 };
	} finally {
		state.probing = false;
		state.fetching = false;
	}
};

const handleProbe = () => runProbe(true);
const handleFetchModels = () => runProbe(false);

const onModelTypeChange = () => {
	if (state.form.aiModelType === 2 && !state.form.embeddingValueSize) {
		state.form.embeddingValueSize = 2048;
	}
	if (state.form.aiModelType === 1 && !state.form.maxAskPromptSize) {
		state.form.maxAskPromptSize = 131072;
		state.form.answerTokens = 8192;
	}
};

const handleSubmit = async () => {
	await editFormRef.value?.validate();
	state.saving = true;
	try {
		await getAPI(AgentModelApi).apiAgentModelAddEditPost(state.form);
		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery();
	} finally {
		state.saving = false;
	}
};

const handleDelete = async (row: AgentModelDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除模型「${row.modelName}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentModelApi).apiAgentModelDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
	margin-top: 14px;
}

/* 卡片 hover 顶部色带：卡片本身只上浮 4px 动静偏小，这条色带让密集卡片墙里「当前指向哪张」更明确 */
.entity-card--accent::before {
	content: '';
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	height: 2px;
	background-image: linear-gradient(90deg, var(--card-accent), transparent 72%);
	opacity: 0;
	transition: opacity 0.25s ease;
	pointer-events: none;
}

.entity-card--accent:hover::before {
	opacity: 0.9;
}

/* 地址两行锁高：一行放不下完整域名，不锁高又会让同排卡片高度参差 */
.model-endpoint {
	-webkit-line-clamp: 2;
	height: 52px;
}

/* 地址输入与两个探测按钮同一行：按钮不参与伸缩，输入框吃掉剩余宽度 */
.endpoint-row {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;

	:deep(.el-input) {
		flex: 1;
		min-width: 0;
	}

	:deep(.el-button) {
		flex: none;
		margin-left: 0;
	}
}

/* 探测结论面板：成功/失败只用左侧色条与图标区分，正文保持常规阅读色 */
.probe {
	width: 100%;
	padding: 10px 12px;
	border: 1px solid color-mix(in srgb, var(--el-color-success) 32%, var(--el-border-color-lighter));
	border-left-width: 3px;
	border-radius: 8px;
	background: color-mix(in srgb, var(--el-color-success) 7%, var(--el-fill-color-blank));
	line-height: 1.6;
}

.probe.is-fail {
	border-color: color-mix(in srgb, var(--el-color-danger) 32%, var(--el-border-color-lighter));
	border-left-color: var(--el-color-danger);
	background: color-mix(in srgb, var(--el-color-danger) 7%, var(--el-fill-color-blank));
}

.probe.is-ok {
	border-left-color: var(--el-color-success);
}

.probe__head {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	font-size: 13px;
	color: var(--el-text-color-primary);
}

.probe__head i {
	flex: none;
	margin-top: 3px;
	color: var(--el-color-success);
}

.probe.is-fail .probe__head i {
	color: var(--el-color-danger);
}

.probe__msg {
	word-break: break-all;
}

.probe__meta {
	display: flex;
	flex-wrap: wrap;
	gap: 2px 14px;
	margin-top: 4px;
	padding-left: 20px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
