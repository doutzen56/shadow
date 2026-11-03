<template>
	<div class="agents-my-app-container">
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
					<h2 class="page-hero__title">我的智能体</h2>
					<p class="page-hero__desc">这里是你被授权使用的智能体，点击卡片可查看能力配置与运行参数，直接与它对话。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.apps }}</span>
							<span class="page-hero__stat-k">可用智能体</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.memory }}</span>
							<span class="page-hero__stat-k">开启记忆</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ heroStats.stream }}</span>
							<span class="page-hero__stat-k">流式输出</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty :image-size="160" description="暂无可用智能体，请联系管理员授权" v-if="!state.list.length" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.list" :key="item.id">
						<el-card shadow="never" class="entity-card entity-card--accent my-app-card is-clickable" :class="cardAccentClass(item.name)" @click="handleView(item)">
							<i class="ri-robot-2-fill entity-card__watermark" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i class="ri-robot-2-fill" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ item.name }}</div>
									<div class="entity-card__sub">{{ item.type || '通用助手' }} · {{ formatTime(item.createTime) }}</div>
								</div>
								<el-tag size="small" effect="light" :type="cardAccent(item.name)">{{ item.aiPromptName || '未绑定提示词' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc">{{ item.describe || '暂无描述' }}</div>

								<!-- 与「智能体管理」同一组指标：温度 / 输出 / 能力项数。
									原先这里显示工具/技能/MCP 数量，但列表接口不装配这三个绑定列表（只有详情接口才装配），
									卡片上永远是 0 —— 换成 DTO 上真实存在的开关与输出类型 -->
								<div class="metric-group">
									<div class="metric-item">
										<span>温度</span><b>{{ ((item.temperature ?? 0) / 100).toFixed(2) }}</b>
									</div>
									<div class="metric-item">
										<span>输出</span><b>{{ item.msgType === 2 ? '流式' : '非流式' }}</b>
									</div>
									<div class="metric-item">
										<span>能力</span><b>{{ agentCapabilityTags(item).length }} 项</b>
									</div>
								</div>

								<div class="tag-group">
									<el-tag v-for="cap in agentCapabilityTags(item)" :key="cap.label" size="small" effect="light" :type="cap.type">{{ cap.label }}</el-tag>
									<span v-if="!agentCapabilityTags(item).length" class="cell-sub">未开启任何能力</span>
								</div>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ item.kmsId ? '已绑定知识库' : '未绑定知识库' }}</span>
								<div class="entity-card__actions">
									<el-button icon="ele-View" text type="primary">详情</el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>
		</el-card>

		<!-- 只读详情：字段与「智能体管理」的编辑项一一对应，开着哪些能力、按什么参数跑、绑了什么，一屏看完 -->
		<el-drawer v-model="state.viewVisible" size="640px" class="app-detail-drawer">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-View /></el-icon>
					<span>智能体详情</span>
				</div>
			</template>

			<div v-if="state.current" class="app-detail">
				<div class="app-detail__hero">
					<div class="app-detail__avatar"><i class="ri-robot-2-fill" /></div>
					<div class="app-detail__hero-main">
						<div class="app-detail__name">{{ state.current.name }}</div>
						<div class="app-detail__sub">{{ state.current.type || '通用助手' }}</div>
					</div>
				</div>
				<p class="app-detail__desc">{{ state.current.describe || '暂无描述' }}</p>
				<div class="tag-group">
					<el-tag v-for="cap in agentCapabilityTags(state.current)" :key="cap.label" size="small" effect="light" :type="cap.type">{{ cap.label }}</el-tag>
					<span v-if="!agentCapabilityTags(state.current).length" class="cell-sub">未开启任何能力</span>
				</div>

				<div class="app-detail__section">基础信息</div>
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="提示词">{{ state.current.aiPromptName || '未绑定' }}</el-descriptions-item>
					<el-descriptions-item label="知识库">{{ state.current.kmsName || (state.current.kmsId ? '已绑定' : '未绑定') }}</el-descriptions-item>
					<el-descriptions-item label="会话模型" :span="2">{{ state.current.chatModelName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="输出类型">{{ state.current.msgType === 2 ? '流式文本' : '非流式文本' }}</el-descriptions-item>
					<el-descriptions-item label="创建人">{{ state.current.createUserName || '-' }}</el-descriptions-item>
					<el-descriptions-item label="创建时间" :span="2">{{ formatDateTime(state.current.createTime, 'YYYY-mm-dd HH:MM') }}</el-descriptions-item>
				</el-descriptions>

				<div class="app-detail__section">运行参数</div>
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="温度">{{ ((state.current.temperature ?? 0) / 100).toFixed(2) }}</el-descriptions-item>
					<el-descriptions-item label="相似度">{{ state.current.relevance ?? 0 }}%</el-descriptions-item>
					<el-descriptions-item label="向量匹配数">{{ state.current.maxMatchesCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="重排候选数">{{ state.current.rerankCount ?? 0 }}</el-descriptions-item>
					<el-descriptions-item label="最大重试">{{ state.current.maxRetries ?? 0 }} 次</el-descriptions-item>
					<el-descriptions-item label="请求超时">{{ state.current.networkTimeout ?? 0 }} 分钟</el-descriptions-item>
					<el-descriptions-item label="对话消息上限">{{ state.current.chatMessageLimit ?? 0 }} 条</el-descriptions-item>
					<el-descriptions-item label="内容长度限制">{{ formatNumber(state.current.contentLengthLimit ?? 0) }}</el-descriptions-item>
					<el-descriptions-item label="返回格式">{{ state.current.responseFormat || '默认（不限制）' }}</el-descriptions-item>
					<el-descriptions-item label="思考能力">{{ reasoningText(EFFORT_LABELS, state.current.reasoningEffort) }}</el-descriptions-item>
					<el-descriptions-item label="思考输出">{{ reasoningText(OUTPUT_LABELS, state.current.reasoningOutput) }}</el-descriptions-item>
					<el-descriptions-item label="请求白名单">{{ state.current.authorizedDomains || '*' }}</el-descriptions-item>
				</el-descriptions>

				<div class="app-detail__section">模型能力</div>
				<el-descriptions :column="2" border size="small">
					<el-descriptions-item label="识图">{{ bindingText('vision') }}</el-descriptions-item>
					<el-descriptions-item label="文生图">{{ bindingText('image_gen') }}</el-descriptions-item>
				</el-descriptions>

				<div class="app-detail__section">已绑定</div>
				<el-descriptions :column="1" border size="small">
					<el-descriptions-item label="工具">{{ selectedNames(state.current.tools) || '未绑定' }}</el-descriptions-item>
					<el-descriptions-item label="技能">{{ selectedNames(state.current.skills) || '未绑定' }}</el-descriptions-item>
					<el-descriptions-item label="MCP">{{ selectedNames(state.current.mcps) || '未绑定' }}</el-descriptions-item>
					<el-descriptions-item label="子智能体">{{ subAgentCount ? `${subAgentCount} 个` : '未挂载' }}</el-descriptions-item>
				</el-descriptions>

				<div class="app-detail__section">日志与安全</div>
				<div class="tag-group">
					<el-tag v-for="item in logSwitches" :key="item.label" size="small" effect="light" :type="item.on ? 'success' : 'info'">{{ item.label }} · {{ item.on ? '开' : '关' }}</el-tag>
				</div>

				<template v-if="state.current.isAIMessageCompaction">
					<div class="app-detail__section">压缩策略</div>
					<el-descriptions :column="2" border size="small">
						<el-descriptions-item label="摘要注入">{{ state.current.isAutoGetAIMessageCompaction ? '自动注入系统提示词' : '作为工具按需获取' }}</el-descriptions-item>
						<el-descriptions-item label="轮次阈值">{{ state.current.conversationTurnsExceed ?? 0 }} 轮</el-descriptions-item>
					</el-descriptions>
					<div class="app-detail__block">
						<div class="app-detail__block-title">压缩提示词</div>
						<pre class="app-detail__pre">{{ state.current.aiMessageCompactionPrompt || '（未配置）' }}</pre>
					</div>
				</template>
			</div>
		</el-drawer>
	</div>
</template>

<script lang="ts" setup name="agentMyApp">
import { computed, onMounted, reactive } from 'vue';

import { ElMessage } from 'element-plus';

import { cardAccent, cardAccentClass } from '/@/views/agent/cardAccent';
import { agentCapabilityTags } from '/@/views/agent/capabilityTags';
import { getAPI } from '/@/utils/axios-utils';
import { formatDateTime } from '/@/utils/format';
import { AgentApi } from '/@/api-services/agent/api';
import { AgentAppDto } from '/@/api-services/agent/models';

/**
 * 详情接口在列表 DTO 之上多返回三个名称字段（后端 AgentAppDto.ChatModelName / KmsName / BindingModelNames）：
 * 详情要给人看，只有雪花 Id 读不懂；列表卡片用不到这些名字，所以没有并进生成出来的 DTO
 */
type AgentAppDetailDto = AgentAppDto & {
	chatModelName?: string | null;
	kmsName?: string | null;
	bindingModelNames?: Record<string, string> | null;
};

/** current 只用于详情抽屉：列表接口返回的是精简字段，点开时再拉一次详情 */
const state = reactive({
	loading: false,
	list: [] as AgentAppDto[],
	viewVisible: false,
	current: undefined as AgentAppDetailDto | undefined,
});

/** 思考能力 / 思考输出：null 表示不配置，交给模型默认行为 */
const EFFORT_LABELS = ['None', 'Low', 'Medium', 'High', 'ExtraHigh'];
const OUTPUT_LABELS = ['None', 'Summary', 'Full'];
const reasoningText = (labels: string[], value?: number | null) => (value === null || value === undefined ? '默认' : (labels[value] ?? String(value)));

/** 千分位：5 万这类长度限制直接读会数不清位数 */
const formatNumber = (value: number) => value.toLocaleString();

/** 子智能体数量：授权绑定里 agent_ 前缀的那几项会被挂载成可调用的工具 */
const subAgentCount = computed(() => (state.current?.bindIds ?? []).filter((t) => t.startsWith('agent_')).length);

/** 日志与安全开关：对应编辑弹窗「开关配置」里的后四项 */
const logSwitches = computed(() => {
	const app = state.current;
	if (!app) return [];
	return [
		{ label: '思考过程', on: !!app.isThinkingLog },
		{ label: '工具调用', on: !!app.isToolLog },
		{ label: '安全拦截', on: !!app.isSecurityIntercept },
		{ label: 'AI 请求日志', on: !!app.isHttpLog },
	];
});

/**
 * 能力绑定（识图 / 文生图）的展示文案。
 * 只有「后端回填出了模型名」才算启用 —— 与后端 IsBindingEnabled 的判定一致（有绑定项 + 已启用 + 模型还在）
 */
const bindingText = (capability: string) => {
	const modelName = state.current?.bindingModelNames?.[capability];
	return modelName ? `已启用 · ${modelName}` : '未启用';
};

/** 卡片上只用到日期：完整时间戳会把标题挤成省略号 */
const formatTime = (value?: string | Date | null) => (value ? String(value).replace('T', ' ').substring(0, 10) : '-');

/** 头图统计：列表接口不返回绑定明细，只统计 DTO 上真实存在的开关与输出类型 */
const heroStats = computed(() => ({
	apps: state.list.length,
	memory: state.list.filter((a) => a.isMemory).length,
	stream: state.list.filter((a) => a.msgType === 2).length,
}));

/** 已勾选的能力名称，详情里按「、」并列展示 */
const selectedNames = (list?: AgentAppDto['tools']) =>
	(list ?? [])
		.filter((t) => t.isSelect)
		.map((t) => t.aiSkillToolManagementName)
		.join('、');

const handleQuery = async () => {
	state.loading = true;
	try {
		const res = await getAPI(AgentApi).apiAgentMyListGet();
		state.list = res.data.result ?? [];
	} finally {
		state.loading = false;
	}
};

/** 打开详情：卡片只显示开关与数量，完整配置放在抽屉里看 */
const handleView = async (row: AgentAppDto) => {
	try {
		const res = await getAPI(AgentApi).apiAgentDetailGet(row.id!);
		state.current = res.data.result;
		state.viewVisible = true;
	} catch (err: any) {
		// 详情拉不到时不弹空抽屉：留在列表上给一句提示，比开了个空白面板更好排查
		ElMessage.error(err?.message ?? '智能体详情加载失败');
	}
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.full-table {
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

/* ===== 详情抽屉 ===== */

.app-detail__hero {
	display: flex;
	align-items: center;
	gap: 12px;
}

/* 头像方块与卡片图标同一套语言：主色浅底 + 主色图标 */
.app-detail__avatar {
	display: grid;
	place-items: center;
	width: 44px;
	height: 44px;
	border-radius: 12px;
	font-size: 24px;
	line-height: 1;
	color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.app-detail__hero-main {
	min-width: 0;
}

.app-detail__name {
	font-size: 16px;
	font-weight: 600;
	line-height: 22px;
	color: var(--el-text-color-primary);
}

.app-detail__sub {
	margin-top: 2px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.app-detail__desc {
	margin: 12px 0 10px;
	font-size: 13px;
	line-height: 20px;
	color: var(--el-text-color-regular);
}

/* 分区标题：主色竖条 + 向右渐隐的细线，与智能体编辑弹窗的分区标题同一套写法 */
.app-detail__section {
	display: flex;
	align-items: center;
	margin: 20px 0 10px;
	font-size: 13px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
}

.app-detail__section::before {
	content: '';
	width: 3px;
	height: 14px;
	margin-right: 8px;
	border-radius: 2px;
	background-color: var(--el-color-primary);
}

.app-detail__section::after {
	content: '';
	flex: 1;
	height: 1px;
	margin-left: 6px;
	background: linear-gradient(to right, var(--el-border-color), transparent);
}

.app-detail__block {
	margin-top: 10px;
}

.app-detail__block-title {
	margin-bottom: 6px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

/* 压缩提示词是长文本：限高滚动，保留换行，不撑破抽屉 */
.app-detail__pre {
	max-height: 200px;
	margin: 0;
	padding: 10px 12px;
	border-radius: 6px;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-light);
	white-space: pre-wrap;
	word-break: break-word;
	overflow: auto;
}
</style>
