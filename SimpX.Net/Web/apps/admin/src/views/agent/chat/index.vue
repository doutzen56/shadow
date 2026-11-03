<template>
	<div class="agents-chat-container">
		<el-container class="chat-layout">
			<!-- 左侧：对话列表 -->
			<el-aside width="286px" class="chat-aside">
				<div class="aside-header">
					<div class="aside-brand">
						<el-icon><ele-ChatDotRound /></el-icon>
						<span>我的 AI 对话</span>
					</div>
					<el-button type="primary" icon="ele-Plus" class="new-chat-btn" @click="handleNewChat" v-auth="'agent/chat/add'"> 新建对话 </el-button>
				</div>
				<el-scrollbar class="aside-list">
					<el-empty :image-size="80" description="暂无对话" v-if="!state.chatList.length" />
					<div v-for="item in state.chatList" :key="String(item.id)" class="chat-item" :class="{ active: String(item.id) === String(state.activeChat?.id) }" @click="handleSelectChat(item)">
						<div class="chat-item-main">
							<div class="chat-item-row">
								<div class="chat-item-title">{{ item.name || '新对话' }}</div>
								<span class="chat-item-time">{{ formatTime(item.createTime).slice(5, 16) }}</span>
							</div>
							<div class="chat-item-sub">
								<span class="chat-item-agent"><i class="ri-robot-2-line" />{{ appName(item.appId) }}</span>
								<span class="chat-item-msg">{{ item.lastMessage || '暂无消息' }}</span>
							</div>
						</div>
						<el-button class="chat-item-del" icon="ele-Delete" text type="danger" size="small" @click.stop="handleDeleteChat(item)" v-auth="'agent/chat/delete'" />
					</div>
				</el-scrollbar>
			</el-aside>

			<!-- 右侧：消息区 -->
			<el-main class="chat-main">
				<div class="chat-header">
					<div class="chat-title">
						<i class="ri-robot-2-fill chat-title-icon" />
						<span>{{ state.activeChat ? appName(state.activeChat.appId) : '请选择或新建对话' }}</span>
						<el-tag v-if="state.activeChat" size="small" type="success" effect="plain">在线</el-tag>
					</div>
					<div class="chat-header-right">
						<!-- P1-6 Token 用量：胶囊 + 超限提示（纯展示，已抽为子组件） -->
						<QuotaPill :quota="quota" :percent="quotaPercent" />
						<div class="chat-status" v-if="activeRun?.process">{{ activeRun.process }}</div>
					</div>
				</div>

				<el-scrollbar ref="scrollRef" class="chat-body">
					<div v-if="!state.activeChat" class="chat-empty">
						<el-empty :image-size="140" description="选择左侧对话，或新建一个对话开始提问">
							<el-button type="primary" icon="ele-Plus" @click="handleNewChat"> 新建对话 </el-button>
						</el-empty>
					</div>

					<div v-for="item in state.historyList" :key="String(item.id)" class="msg-row" :class="item.isSend ? 'msg-user' : 'msg-ai'">
						<div class="msg-main">
							<!-- 气泡交给 TinyRobot：用户消息走纯文本，AI 回复走内置 Markdown 渲染器；
							     AI 还没吐字（且已不在生成中）时不渲染空壳，失败信息交给下方提示 -->
							<TrBubble
								v-if="item.isSend || bubbleContentOf(item)"
								:avatar="bubbleAvatar(item.isSend)"
								:content="bubbleContentOf(item)"
								:placement="item.isSend ? 'end' : 'start'"
								shape="corner"
								:fallback-content-renderer="item.isSend ? undefined : BubbleRenderers.Markdown"
							/>

							<!-- 气泡外的附加信息：等待动效 / 思考过程 / 工具调用 / 附件 / 失败 / 元信息 / 推荐问题 -->
							<div class="msg-extras">
								<!-- 还没开始吐字：三点跳动 + 耗时，替代原来一句静态的「正在思考…」 -->
								<div v-if="isThinking(item) && !item.aiReasoningContent" class="msg-wait">
									<span class="msg-wait__dots"><i></i><i></i><i></i></span>
									<span class="msg-wait__text">{{ activeRun?.process || '正在思考…' }}</span>
									<span class="msg-wait__time" v-if="thinkingSeconds">{{ thinkingSeconds }}s</span>
								</div>

								<!-- 思考过程：生成中直接展开并跟随滚动，结束后收成可点开的卡片 -->
								<div v-if="item.aiReasoningContent && isThinking(item)" class="think is-live">
									<div class="think__head">
										<i class="ri-brain-line think__icon" />
										<span class="think__title">正在思考</span>
										<span class="think__count">{{ thinkingSeconds ? thinkingSeconds + 's · ' : '' }}{{ item.aiReasoningContent.length }} 字</span>
										<span class="think__pulse"></span>
									</div>
									<div ref="liveThinkRef" class="think__body">
										<pre class="think__text">{{ item.aiReasoningContent }}</pre>
									</div>
								</div>
								<details v-else-if="item.aiReasoningContent" class="think">
									<summary class="think__head">
										<i class="ri-brain-line think__icon" />
										<span class="think__title">思考过程</span>
										<span class="think__count">{{ item.aiReasoningContent.length }} 字</span>
										<i class="ri-arrow-down-s-line think__arrow" />
									</summary>
									<div class="think__body">
										<pre class="think__text">{{ item.aiReasoningContent }}</pre>
									</div>
								</details>

								<!-- 工具调用：与思考过程同一套卡片语言，用警示色区分 -->
								<details v-if="item.aiToolsContent" class="think think--tools">
									<summary class="think__head">
										<i class="ri-tools-line think__icon" />
										<span class="think__title">工具调用</span>
										<span class="think__count">{{ item.aiToolsContent.length }} 字</span>
										<i class="ri-arrow-down-s-line think__arrow" />
									</summary>
									<div class="think__body">
										<pre class="think__text">{{ item.aiToolsContent }}</pre>
									</div>
								</details>

								<!-- 附件 -->
								<div v-if="item.fileNames" class="msg-files">
									<el-tag size="small" effect="plain" v-for="(name, index) in item.fileNames.split(',')" :key="index">
										<el-icon style="margin-right: 3px; vertical-align: middle"><ele-Paperclip /></el-icon>{{ name }}
									</el-tag>
								</div>

								<!-- 失败提示 -->
								<div v-if="item.sendStatus === 1 || item.sendStatus === 2" class="msg-fail">
									<el-alert type="error" :closable="false" show-icon>
										<template #title>
											<span>发送失败：{{ item.failReason || '未知原因' }}</span>
											<el-button link type="primary" size="small" @click="handleRetry(item)"> 点击重试 </el-button>
										</template>
									</el-alert>
								</div>

								<!-- 元信息 -->
								<div class="msg-meta">
									<span>{{ formatTime(item.createTime) }}</span>
									<span v-if="item.totalTokenCount">消耗 {{ item.totalTokenCount }} tokens</span>
									<span v-if="!item.isSend && item.durationMs">耗时 {{ formatDuration(item.durationMs) }}</span>
									<el-popover v-if="item.aiChatHistorysBindLogs?.length" placement="top" :width="600" trigger="click">
										<template #reference>
											<button type="button" class="msg-log-link">
												<el-icon :size="12"><ele-Document /></el-icon>
												相关日志 {{ item.aiChatHistorysBindLogs.length }}
											</button>
										</template>
										<div class="msg-log">
											<div class="msg-log__head">
												<span class="msg-log__title"><i class="ri-file-list-3-line" />AI 调用日志</span>
												<span class="msg-log__badge">{{ item.aiChatHistorysBindLogs.length }} 条</span>
											</div>
											<div class="msg-log__list">
												<div v-for="(log, li) in item.aiChatHistorysBindLogs" :key="String(log.id)" class="msg-log-item" :class="`is-type-${log.logType}`">
													<div class="msg-log-item__head">
														<el-tag size="small" effect="light" :type="logTagType(log.logType)">{{ logTypeText(log.logType) }}</el-tag>
														<span class="msg-log-item__idx">#{{ li + 1 }}</span>
													</div>
													<pre class="msg-log-item__content">{{ formatLogContent(log) }}</pre>
												</div>
											</div>
										</div>
									</el-popover>
								</div>

								<!-- 推荐问题：后台二次问 AI 生成，实时走 recommendmsg 事件、历史从回复记录的绑定日志回显，点一下直接作为一轮提问发送 -->
								<div v-if="!item.isSend && isLastAi(item) && item.recommendQuestions?.length" class="msg-recommend">
									<div class="msg-recommend-title">你可能还想问</div>
									<div class="msg-recommend-list">
										<button v-for="(q, qi) in item.recommendQuestions" :key="qi" type="button" class="msg-recommend-chip" @click="useRecommendQuestion(q)">
											{{ q }}
										</button>
									</div>
								</div>
							</div>
						</div>
					</div>
				</el-scrollbar>

				<!-- 输入区：TinyRobot TrSender，loading 时自带停止按钮（@cancel 中止请求） -->
				<div class="chat-input">
					<TrSender
						v-model="state.inputContent"
						mode="multiple"
						submit-type="enter"
						clearable
						:loading="!!activeRun"
						:disabled="!state.activeChat"
						:auto-size="{ minRows: 2, maxRows: 6 }"
						stop-text="停止生成"
						placeholder="请输入问题，例如：帮我总结本周工单情况"
						@submit="handleSend()"
						@cancel="handleStop"
					>
						<!-- 底部一行：附件按钮（左下角）+ 已选附件标签 + 推荐问题/联网搜索开关 -->
						<template #footer>
							<div class="input-tags">
								<input ref="fileInputRef" type="file" multiple hidden @change="onFilesSelected" />
								<el-tooltip content="上传附件" placement="top">
									<button type="button" class="chat-tool" :disabled="!state.activeChat" @click="fileInputRef?.click()">
										<el-icon :size="17"><ele-Paperclip /></el-icon>
									</button>
								</el-tooltip>
								<el-tooltip :content="asrActive ? '停止语音输入' : '语音输入（识别结果回填输入框）'" placement="top">
									<button type="button" class="chat-tool" :class="{ 'is-asr-active': asrActive }" :disabled="!state.activeChat" @click="toggleVoice">
										<el-icon :size="17"><ele-Microphone /></el-icon>
									</button>
								</el-tooltip>
								<el-tag v-for="(file, index) in state.files" :key="file.url + index" closable size="small" effect="plain" @close="state.files.splice(index, 1)">
									{{ file.fileName }}
								</el-tag>
								<div class="input-tags-spacer"></div>
								<div class="toggle-chip">
									<span>推荐问题</span>
									<el-switch v-model="state.isRecommendQuestion" size="small" />
								</div>
								<div class="toggle-chip">
									<span>联网搜索</span>
									<el-switch v-model="state.isOnlineSearch" size="small" />
								</div>
							</div>
						</template>

						<!-- 空草稿时右侧给一句快捷键提示，与发送键同一行 -->
						<template #footer-right>
							<span v-if="!state.inputContent.trim()" class="send-hint">Enter 发送 · Shift + Enter 换行</span>
						</template>
					</TrSender>
				</div>
			</el-main>
		</el-container>

		<!-- 选择智能体：做成可点击的卡片，名称/描述/能力一眼可见 -->
		<el-dialog v-model="state.agentSelectVisible" width="760px" draggable>
			<template #header>
				<div class="dialog-title">
					<i class="ri-robot-2-fill" />
					<span>新建对话 · 选择智能体</span>
				</div>
			</template>
			<el-empty :image-size="120" description="暂无可用的智能体，请联系管理员授权" v-if="!state.agentList.length" />
			<template v-else>
				<div class="agent-pick-tip">选择要对话的智能体，每个智能体的提示词、知识库与工具配置不同。</div>
				<div class="agent-pick-grid">
					<div
						v-for="item in state.agentList"
						:key="String(item.id)"
						class="agent-pick-card"
						:class="{ active: String(item.id) === state.selectedAgentId }"
						@click="state.selectedAgentId = String(item.id)"
					>
						<div class="agent-pick-icon">
							<i class="ri-robot-2-fill" />
						</div>
						<div class="agent-pick-body">
							<div class="agent-pick-name">{{ item.name }}</div>
							<div class="agent-pick-desc">{{ item.describe || '暂无描述' }}</div>
							<div class="tag-group agent-pick-tags">
								<template v-if="agentCapabilities(item).length">
									<el-tag v-for="cap in agentCapabilities(item)" :key="cap.label" size="small" effect="light" :type="cap.type">{{ cap.label }}</el-tag>
								</template>
								<span v-else class="agent-pick-none">未开启任何能力</span>
							</div>
						</div>
						<el-icon class="agent-pick-check"><ele-CircleCheckFilled /></el-icon>
					</div>
				</div>
			</template>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.agentSelectVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.creating" @click="handleCreateChat">确定</el-button>
			</template>
		</el-dialog>

		<!-- 工具执行审批（P0-2）：危险工具执行前弹出人工确认，弹窗不可手动关闭，避免误关导致工具调用挂起 -->
		<ToolApprovalDialog :request="currentApproval" :queue-count="approvalQueue.length" @resolve="handleApprovalResolve" />
	</div>
</template>

<script lang="ts" setup name="agentChat">
import { computed, h, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { ElAvatar, ElIcon, ElMessage, ElMessageBox } from 'element-plus';
import { User } from '@element-plus/icons-vue';
import * as signalR from '@microsoft/signalr';
import { BubbleRenderers, TrBubble, TrSender } from '@opentiny/tiny-robot';
import { Local } from '/@/utils/storage';

import type { ToolApprovalRequest, TokenQuotaView, ChatHistoryItem } from './types';
import type { CardAccent } from '/@/views/agent/cardAccent';
import ToolApprovalDialog from './component/toolApprovalDialog.vue';
import QuotaPill from './component/quotaPill.vue';

import { getAPI } from '/@/utils/axios-utils';
import { AgentApi, AgentAsrApi, AgentChatApi, AgentChatHistoryApi, AgentFileApi } from '/@/api-services/agent/api';
import { AgentAppDto, AgentChatDto, AgentChatHistoryDto, AgentChatHistoryBindLogDto, SysFile } from '/@/api-services/agent/models';
import { createAliAsr, type AsrState } from '/@/utils/aliyunAsr';

const scrollRef = ref<{ wrapRef?: HTMLElement }>();
const fileInputRef = ref<HTMLInputElement>();
/** 生成中思考面板的滚动容器：新内容从底部追加，需要跟着往下滚（v-for 内模板 ref 可能是数组） */
const liveThinkRef = ref<HTMLElement | HTMLElement[]>();

/** 每秒更新一次的时钟：等待秒数按每轮的 startedAt 现算，多轮并行时互不覆盖 */
const clockTick = ref(Date.now());
let thinkingTimer: ReturnType<typeof setInterval> | null = null;

/** agentList 两个用途：顶部显示当前对话所属智能体的名字、新建对话时作为可选项 */
const state = reactive({
	agentList: [] as AgentAppDto[],
	chatList: [] as AgentChatDto[],
	activeChat: undefined as AgentChatDto | undefined,
	historyList: [] as ChatHistoryItem[],
	agentSelectVisible: false,
	selectedAgentId: '',
	creating: false,
	inputContent: '',
	isOnlineSearch: false,
	// 推荐问题开关：默认开启，开启后服务端在提问时就并行“二次问 AI”生成 3 个追问建议，回答结束后一并下发
	isRecommendQuestion: true,
	files: [] as SysFile[],
});

/**
 * 单轮问答的运行态：流式缓冲与消息都挂在「发起它的对话」上。
 * 切换对话后旧对话的分片继续落到自己的气泡，新对话也能立刻再发一轮。
 */
interface ChatRun {
	/** 本轮预生成的消息 Id：同时是 SignalR 分组键 */
	id: string;
	/** 所属对话 Id（雪花 Id 统一按字符串比较） */
	chatId: string;
	ask: ChatHistoryItem;
	answer: ChatHistoryItem;
	content: string;
	tools: string;
	reasoning: string;
	process: string;
	recommend: string[];
	startedAt: number;
	abort: AbortController | null;
	/** 已定稿：不再接收分片，也不参与切回对话时的本地注入 */
	done: boolean;
}

/** 进行中的各轮问答，key 为本轮消息 Id */
const runs = reactive(new Map<string, ChatRun>());

/** 各轮实时连接：不能放进响应式状态，代理包装会破坏 SignalR 实例的内部状态与方法调用 */
const hubConnections = new Map<string, signalR.HubConnection>();

const chatIdOf = (chat?: AgentChatDto) => (chat && chat.id !== undefined && chat.id !== null ? String(chat.id) : '');

const isActiveChat = (chatId: string) => !!chatId && chatIdOf(state.activeChat) === chatId;

/** 当前对话正在生成的那一轮（没有则为 undefined）：输入区 loading、思考计时、进度文案都以它为准 */
const activeRun = computed(() => {
	const chatId = chatIdOf(state.activeChat);
	if (!chatId) return undefined;
	for (const run of runs.values()) if (run.chatId === chatId) return run;
	return undefined;
});

/** 某个对话是否正在生成：按对话判断，别的对话在跑不影响当前对话发送 */
const isChatRunning = (chat: AgentChatDto | undefined) => {
	const chatId = chatIdOf(chat);
	if (!chatId) return false;
	for (const run of runs.values()) if (run.chatId === chatId) return true;
	return false;
};

/** 等待秒数：跟随当前对话的那一轮，切走立即归零 */
const thinkingSeconds = computed(() => {
	const run = activeRun.value;
	return run ? Math.max(0, Math.floor((clockTick.value - run.startedAt) / 1000)) : 0;
});

/** 审批项额外记住来自哪一轮：批准时要找回那一轮的实时连接回传 */
type ApprovalItem = ToolApprovalRequest & { runId: string };
/** 工具审批队列（P0-2）：危险工具执行前推到此，用户批准/拒绝后回传后端唤醒等待 */
const approvalQueue = ref<ApprovalItem[]>([]);
/** 当前待审批项（队列取第一个，依次处理） */
const currentApproval = computed(() => approvalQueue.value[0]);
// 审批弹窗的显隐由子组件按 request 是否为 null 自行控制（request 即队列首项），这里不再单独维护开关

/** Token 额度初始值：切对话 / 重置用量时都从这一份复制，避免两处手写对象漂移 */
const emptyQuota = (): TokenQuotaView => ({
	enabled: false,
	exceeded: false,
	exceededMessage: '',
	sessionUsed: 0,
	sessionLimit: 0,
	tenantDailyUsed: 0,
	tenantDailyLimit: 0,
	lastTotal: 0,
});

/** P1-6 Token 额度用量：由后端 tokenUsage 事件推送，驱动顶部用量条与超限提示 */
const quota = ref<TokenQuotaView>(emptyQuota());

/** 用量紧张度：取「本对话」与「租户当日」两个维度里更紧张的一个（未设限的维度按 0 计），≥80% 时用量条转警示色 */
const quotaPercent = computed(() => {
	const session = quota.value.sessionLimit > 0 ? (quota.value.sessionUsed / quota.value.sessionLimit) * 100 : 0;
	const tenant = quota.value.tenantDailyLimit > 0 ? (quota.value.tenantDailyUsed / quota.value.tenantDailyLimit) * 100 : 0;
	return Math.max(session, tenant);
});

const appName = (appId?: number) => state.agentList.find((item) => String(item.id) === String(appId))?.name ?? '智能体';

/** 已开启的能力：与「智能体管理」卡片同一套标签（语义色 + light 效果），两个入口看到的样式保持一致 */
const agentCapabilities = (row: AgentAppDto): { label: string; type: CardAccent }[] => {
	const list: { label: string; type: CardAccent }[] = [];
	if (row.isAITools) list.push({ label: '工具', type: 'primary' });
	if (row.isSkill) list.push({ label: '技能', type: 'success' });
	if (row.isMcp) list.push({ label: 'MCP', type: 'warning' });
	if (row.isMemory) list.push({ label: '记忆', type: 'info' });
	if (row.isAIMessageCompaction) list.push({ label: '压缩', type: 'danger' });
	return list;
};

const logTypeText = (type: number) => (type === 1 ? '知识库' : type === 2 ? '网络搜索' : type === 3 ? '系统提示词' : type === 5 ? '推荐问题' : '文件内容');

/** 日志类型的标签配色：知识库主题色、搜索成功色、提示词中性、推荐警示色、文件危险色，一眼能分辨来源 */
const logTagType = (type: number): 'primary' | 'success' | 'info' | 'warning' | 'danger' => (type === 1 ? 'primary' : type === 2 ? 'success' : type === 3 ? 'info' : type === 5 ? 'warning' : 'danger');

/** 推荐问题日志存的是 JSON 数组，摊平成多行更好读；其它类型原样显示 */
const formatLogContent = (log: AgentChatHistoryBindLogDto) => {
	const raw = String(log.logContent ?? '');
	if (Number(log.logType) !== 5) return raw;
	try {
		const arr = JSON.parse(raw);
		return Array.isArray(arr) ? arr.map((item) => `· ${item}`).join('\n') : raw;
	} catch {
		return raw;
	}
};

/**
 * 时间统一解析成毫秒：历史消息是服务端字符串（2026-10-29T02:08:53），本地临时消息是 Date 对象，
 * 统一走 Date 解析，避免 String(Date) 截出 "Thu Oct 29 2026 04:" 这种残片；非法值返回 0
 */
const toTimestamp = (value?: string | Date) => {
	if (!value) return 0;
	const date = value instanceof Date ? value : new Date(String(value).replace('T', ' ').replace(/-/g, '/'));
	return Number.isNaN(date.getTime()) ? 0 : date.getTime();
};

/** 时间格式化（yyyy-MM-dd HH:mm:ss） */
const formatTime = (value?: string | Date) => {
	const time = toTimestamp(value);
	if (!time) return value ? String(value).replace('T', ' ').substring(0, 19) : '';
	const date = new Date(time);
	const pad = (n: number) => String(n).padStart(2, '0');
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
};

/** 回答耗时：一分钟内按秒保留一位小数，超过后按「分 + 秒」展示 */
const formatDuration = (ms?: number) => {
	if (!ms) return '';
	if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
	const total = Math.round(ms / 1000);
	const minutes = Math.floor(total / 60);
	const seconds = total % 60;
	return seconds ? `${minutes}m ${seconds}s` : `${minutes}m`;
};

/** 把 unknown 异常收窄成可读结构，避免用 any 直接取属性 */
const asErrorLike = (e: unknown): { message?: string; code?: string; name?: string } => (e && typeof e === 'object' ? (e as { message?: string; code?: string; name?: string }) : {});

const scrollToBottom = () => {
	nextTick(() => {
		const wrap = scrollRef.value?.wrapRef;
		if (wrap) wrap.scrollTop = wrap.scrollHeight;
	});
};

/* ---------------------------- TinyRobot 气泡 ---------------------------- */

/** 头像：每次渲染新建 VNode（同一 VNode 复用到多行会触发 Vue 复用告警）；
    用户走人像图标，AI 用菜单「AI 智能体」同款 remixicon 机器人，红底区分身份 */
const bubbleAvatar = (isSend: boolean) =>
	h(ElAvatar, { size: 32, class: isSend ? 'is-user' : 'is-ai' }, { default: () => (isSend ? h(ElIcon, null, () => h(User)) : h('i', { class: 'ri-robot-2-fill' })) });

/** 气泡内容：用户消息纯文本；AI 回复走 Markdown 渲染器。
    还没有任何内容时不渲染空气泡，等待态交给 msg-extras 里的动效占位 */
const bubbleContentOf = (item: ChatHistoryItem) => item.content ?? '';

/* ---------------------------- 推荐问题 ---------------------------- */

/** 只给最后一条 AI 回复留推荐 chip：点一下就直接发送，旧回复上的推荐误触代价太高 */
const isLastAi = (item: ChatHistoryItem) => {
	for (let i = state.historyList.length - 1; i >= 0; i--) {
		if (!state.historyList[i].isSend) return state.historyList[i].id === item.id;
	}
	return false;
};

/** 该条回复是否处于「生成中」：只有当前对话那一轮的回复转圈，历史消息不会跟着显示等待态 */
const isThinking = (item: ChatHistoryItem) => {
	const run = activeRun.value;
	return !!run && !item.isSend && !item.content && String(item.id) === String(run.answer.id);
};

/** 思考过程实时跟随：面板锁高，新内容追加在底部，不跟着滚就只能看到开头几行 */
const scrollThinkingLive = () => {
	nextTick(() => {
		const el = Array.isArray(liveThinkRef.value) ? liveThinkRef.value[0] : liveThinkRef.value;
		if (el) el.scrollTop = el.scrollHeight;
	});
};

/** 计时随当前对话的运行态启停：切走即停，秒数按本轮 startedAt 现算 */
watch(activeRun, (run) => {
	if (thinkingTimer) {
		clearInterval(thinkingTimer);
		thinkingTimer = null;
	}
	if (!run) return;

	clockTick.value = Date.now();
	thinkingTimer = setInterval(() => {
		clockTick.value = Date.now();
	}, 1000);
});

/** 从回复记录的绑定日志里取出推荐问题（logType=5，logContent 为 JSON 字符串数组） */
const extractRecommendLogs = (logs?: AgentChatHistoryBindLogDto[] | null): string[] => {
	const raw = (logs ?? []).find((t) => Number(t.logType) === 5)?.logContent;
	if (!raw) return [];
	try {
		const arr = JSON.parse(raw);
		return Array.isArray(arr) ? arr.filter(Boolean).slice(0, 3) : [];
	} catch {
		return [];
	}
};

/** recommendmsg 事件：解析 JSON 数组后挂到这一轮自己的回复气泡上（气泡还没挂进列表也要留住，切回来能看到） */
const handleRecommendMsg = (run: ChatRun, msg: string) => {
	let list: string[] = [];
	try {
		const arr = JSON.parse(msg);
		list = Array.isArray(arr) ? arr.filter(Boolean).slice(0, 3) : [];
	} catch {
		return;
	}
	if (!list.length) return;
	run.recommend = list;
	run.answer.recommendQuestions = list;
	if (isActiveChat(run.chatId)) scrollToBottom();
};

/** 推荐 chip 点击：直接当一轮提问发出去（与手输回车走同一条链路） */
const useRecommendQuestion = async (q: string) => {
	if (activeRun.value) {
		ElMessage.warning('当前对话还在生成回复，请稍候');
		return;
	}
	state.inputContent = q;
	await handleSend();
};

/* ---------------------------- 工具审批（P0-2） ---------------------------- */

/** 审批弹窗的决策入口：把子组件传来的用户决策转交给下面的回传逻辑 */
const handleApprovalResolve = async (approved: boolean) => {
	const item = currentApproval.value;
	if (item) await resolveApproval(item, approved);
};

/** 用户审批决策：批准/拒绝都回传后端，唤醒对应等待中的工具调用（失败也移除，避免弹窗卡死） */
const resolveApproval = async (item: ApprovalItem, approved: boolean) => {
	try {
		const conn = hubConnections.get(item.runId);
		if (!conn) throw new Error('实时通道未连接');
		await conn.invoke('ApproveToolCall', item.requestId, approved);
		ElMessage.success(approved ? '已批准该工具执行' : '已拒绝该工具执行');
	} catch (e: unknown) {
		ElMessage.error(`审批回传失败：${asErrorLike(e).message ?? String(e)}`);
	} finally {
		approvalQueue.value = approvalQueue.value.filter((t) => t.requestId !== item.requestId);
		// 队列清空后把进度文案交还给后续 processmsg
		const run = runs.get(item.runId);
		if (!approvalQueue.value.length && run) run.process = '';
	}
};

/* ---------------------------- SignalR 连接 ---------------------------- */

/**
 * 建立本轮的 SignalR 连接：服务端按 IdentityId（本轮消息 Id）分组推送，所以必须在发请求前连上，否则本轮分片会丢。
 * 每轮各占一条连接：切换对话后旧对话仍在推流，两条连接互不干扰。
 */
const connectHub = async (runId: string) => {
	const base = (window as Window & { __env__?: { VITE_API_URL?: string } }).__env__?.VITE_API_URL ?? '';
	const conn = new signalR.HubConnectionBuilder()
		.withUrl(`${base}/api/agentHub?IdentityId=${runId}`, {
			accessTokenFactory: () => (Local.get('access-token') as string) ?? '',
		})
		.withAutomaticReconnect()
		.build();

	/** 取本轮运行态：已定稿或已被清掉时丢弃后续分片 */
	const current = () => {
		const run = runs.get(runId);
		return run && !run.done ? run : undefined;
	};
	// 本轮所属对话：用量结算只在它处于前台时展示
	const chatId = runs.get(runId)?.chatId ?? '';

	// 正文分片
	conn.on('aimsg', (msg: string) => {
		const run = current();
		if (!run) return;
		run.content += msg;
		applyRunToAnswer(run);
	});
	// 工具调用日志
	conn.on('aIToolsContentMsg', (msg: string) => {
		const run = current();
		if (!run) return;
		run.tools += msg;
		applyRunToAnswer(run);
	});
	// 思考过程日志
	conn.on('aIReasoningContentMsg', (msg: string) => {
		const run = current();
		if (!run) return;
		run.reasoning += msg;
		applyRunToAnswer(run);
	});
	// 进度文案
	conn.on('processmsg', (msg: string) => {
		const run = current();
		if (run) run.process = msg;
	});
	// 推荐问题（回答结束后下发）
	conn.on('recommendmsg', (msg: string) => {
		const run = current();
		if (run) handleRecommendMsg(run, msg);
	});
	// 工具执行审批请求（P0-2 危险工具人工确认）：推到此即弹窗展示工具名+参数，用户决策后调 ApproveToolCall 回传
	conn.on('toolApprovalRequest', (msg: string) => {
		const run = current();
		if (!run) return;
		try {
			const data = JSON.parse(msg) as Partial<ToolApprovalRequest>;
			if (!data?.requestId) return;
			approvalQueue.value.push({ requestId: data.requestId, toolName: data.toolName ?? '未知工具', arguments: data.arguments ?? '', runId });
			run.process = `等待工具「${data.toolName ?? '未知工具'}」执行审批…`;
		} catch {
			// 非法 payload 直接忽略，不阻塞主流程
		}
	});
	// Token 用量 / 额度（P1-6）：结算后推本轮消耗与两个维度的已用/上限；exceeded=true 表示本轮被额度或并发拦下
	conn.on('tokenUsage', (msg: string) => {
		// 用量按对话结算：后台对话的结算不覆盖当前对话的用量条
		if (!isActiveChat(chatId)) return;
		try {
			const data = JSON.parse(msg) as Partial<TokenQuotaView>;
			quota.value = {
				enabled: !!data?.enabled,
				exceeded: !!data?.exceeded,
				exceededMessage: data?.exceededMessage ?? '',
				sessionUsed: Number(data?.sessionUsed ?? 0),
				sessionLimit: Number(data?.sessionLimit ?? 0),
				tenantDailyUsed: Number(data?.tenantDailyUsed ?? 0),
				tenantDailyLimit: Number(data?.tenantDailyLimit ?? 0),
				lastTotal: Number(data?.lastTotal ?? 0),
			};
			// 被拦下时后端还会抛业务异常（接口层已弹一次提示），这里补一条持久化的头部警示，
			// 避免提示一闪而过、用户不知道为什么发不出去
			if (quota.value.exceeded) {
				ElMessage.error(quota.value.exceededMessage || 'Token 额度已用尽，无法继续本次对话');
				const run = current();
				if (run) run.process = '';
			}
		} catch {
			// 非法 payload 直接忽略，不阻塞主流程
		}
	});

	await conn.start();
	hubConnections.set(runId, conn);
};

/** 关闭本轮的连接：连接按轮次独占，结束后必须释放，避免重连堆积 */
const closeRunHub = async (runId: string) => {
	// 本轮已收尾：仍在等审批的请求随之作废，别把弹窗挂在一个已断开的连接上
	approvalQueue.value = approvalQueue.value.filter((t) => t.runId !== runId);
	const conn = hubConnections.get(runId);
	if (!conn) return;
	hubConnections.delete(runId);
	try {
		await conn.stop();
	} catch {
		// 忽略断开异常
	}
};

/** 把本轮缓冲同步到它自己的回复气泡上；只有该对话在前台时才抢滚动，后台对话不打扰当前视图 */
const applyRunToAnswer = (run: ChatRun) => {
	run.answer.content = run.content || run.answer.content;
	if (run.tools) run.answer.aiToolsContent = run.tools;
	if (run.reasoning) run.answer.aiReasoningContent = run.reasoning;
	if (!isActiveChat(run.chatId)) return;
	if (run.reasoning) scrollThinkingLive();
	scrollToBottom();
};

/* ------------------------------ 数据加载 ------------------------------ */

/** 按 Id 去重（保留首次出现的记录）：列表以 Id 作为 v-for 的 key，重复会让 Vue 告警并可能错乱渲染 */
const dedupeById = (list: ChatHistoryItem[]): ChatHistoryItem[] => {
	const seen = new Set<string>();
	return list.filter((item) => {
		const key = String(item?.id);
		if (seen.has(key)) return false;
		seen.add(key);
		return true;
	});
};

/** 会话列表：一次取 200 条（分页在这个场景没有意义，用户只会翻最近几个会话） */
const loadChatList = async () => {
	const res = await getAPI(AgentChatApi).apiAgentChatPagePost({ page: 1, pageSize: 200 });
	state.chatList = res.data.result?.items ?? [];
};

/**
 * 把本对话里仍在生成的那一轮补回消息列表。
 * 提问行后端在请求早期就已落库（Id 与本轮预生成 Id 相同），先剔掉同名记录再挂本地气泡，避免同一句出现两次。
 */
const injectRunningMessages = () => {
	const chatId = chatIdOf(state.activeChat);
	if (!chatId) return;
	const live = [...runs.values()].filter((run) => run.chatId === chatId && !run.done);
	if (!live.length) return;
	const pendingIds = new Set(live.map((run) => run.id));
	state.historyList = state.historyList.filter((item) => !pendingIds.has(String(item.id)));
	live.forEach((run) => state.historyList.push(run.ask, run.answer));
};

/** 本轮实测耗时（毫秒），key 为提问记录 Id：实时结果比落库时间差更精细；页面刷新后该表为空，回退到时间差估算 */
const answerDurations = new Map<string, number>();

/**
 * 给历史里的 AI 回复补上耗时：提问行在请求开始时就已落库、回复行在收尾时落库，两者时间差即生成长度。
 * 优先用本轮实测值（同一提问 Id），否则按相邻「提问 → 回复」两条记录的 CreateTime 相减。
 */
const applyAnswerDurations = (list: ChatHistoryItem[]) => {
	let lastAsk: ChatHistoryItem | undefined;
	for (const item of list) {
		if (item.isSend) {
			lastAsk = item;
			continue;
		}
		if (!lastAsk) continue;
		const measured = answerDurations.get(String(lastAsk.id));
		const ms = measured && measured > 0 ? measured : toTimestamp(item.createTime) - toTimestamp(lastAsk.createTime);
		if (ms > 0) item.durationMs = ms;
		lastAsk = undefined;
	}
};

/** 历史消息：按对话取 500 条，刷新后推荐问题要从绑定日志回显（实时事件只在本轮有效） */
const loadHistory = async () => {
	const chat = state.activeChat;
	if (!chat) {
		state.historyList = [];
		return;
	}
	const res = await getAPI(AgentChatHistoryApi).apiAgentChatHistoryPagePost({ whereId: chat.id, page: 1, pageSize: 500 });
	// 请求期间已经切到别的对话：这次结果作废，避免把旧对话的消息盖到新对话上
	if (chatIdOf(state.activeChat) !== chatIdOf(chat)) return;
	state.historyList = dedupeById(res.data.result ?? []);
	// 刷新后从绑定日志里回显推荐问题（只给最新一条 AI 回复展示）
	state.historyList.forEach((item) => {
		if (!item.isSend) item.recommendQuestions = extractRecommendLogs(item.aiChatHistorysBindLogs);
	});
	applyAnswerDurations(state.historyList);
	// 切回正在生成的对话时，把还在跑的那一轮接回来
	injectRunningMessages();
	scrollToBottom();
};

/** 清掉某个对话的运行态：删除对话时用，避免请求与连接挂在已删除的对话上 */
const dropChatRuns = (chatId: string) => {
	for (const [runId, run] of runs) {
		if (run.chatId !== chatId) continue;
		run.done = true;
		run.abort?.abort();
		void closeRunHub(runId);
		runs.delete(runId);
	}
};

const handleSelectChat = async (chat: AgentChatDto) => {
	state.activeChat = chat;
	// 用量属于上一个对话，切换后清空：等本会话下一轮结算推来再显示
	quota.value = emptyQuota();
	await loadHistory();
};

const handleNewChat = async () => {
	const res = await getAPI(AgentApi).apiAgentMyListGet();
	state.agentList = res.data.result ?? [];
	state.selectedAgentId = state.agentList.length ? String(state.agentList[0].id) : '';
	state.agentSelectVisible = true;
};

const handleCreateChat = async () => {
	if (!state.selectedAgentId) {
		ElMessage.warning('请选择智能体');
		return;
	}
	state.creating = true;
	try {
		// 雪花 Id 超出 JS 安全整数范围，保持字符串原值提交（Number 转换会丢精度）
		await getAPI(AgentChatApi).apiAgentChatAddPost({ appId: state.selectedAgentId as unknown as number, name: '', isHidden: false });
		state.agentSelectVisible = false;
		await loadChatList();
		// 默认打开最新创建的对话
		if (state.chatList.length) {
			await handleSelectChat(state.chatList[0]);
		}
	} catch (e: unknown) {
		ElMessage.error(`创建对话失败：${asErrorLike(e).message ?? String(e)}`);
	} finally {
		state.creating = false;
	}
};

const handleDeleteChat = async (chat: AgentChatDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除对话「${chat.name || '新对话'}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentChatApi).apiAgentChatDeleteIdPost(chat.id!);
	dropChatRuns(chatIdOf(chat));
	if (String(state.activeChat?.id) === String(chat.id)) {
		state.activeChat = undefined;
		state.historyList = [];
	}
	ElMessage.success('删除成功');
	await loadChatList();
};

/* ------------------------------ 附件处理 ------------------------------ */

const onFilesSelected = async (event: Event) => {
	const input = event.target as HTMLInputElement;
	const files = Array.from(input.files ?? []);
	input.value = '';
	for (const file of files) {
		// 后端返回的字段名与 SysFile 一致，取 url 作为后续解析地址
		const res = await getAPI(AgentFileApi).apiAgentFileUploadPostForm(file, 'AgentChat', 'ChatFile', 0);
		// SysFile 的文件名字段是 fileName（不是 name），右侧 file.name 是 input 选中的原生文件对象
		state.files.push({ fileName: file.name, url: res.data.result?.url ?? '' });
	}
	ElMessage.success(`已上传 ${files.length} 个文件`);
};

/* ------------------------------ 发送消息 ------------------------------ */

const handleSend = async (retryOfId?: number, retryContent?: string) => {
	const chat = state.activeChat;
	if (!chat) return;
	const content = retryOfId ? (retryContent ?? '') : state.inputContent.trim();
	if (!content && !retryOfId) {
		ElMessage.warning('请输入内容');
		return;
	}
	// 只拦当前对话：别的对话还在生成不影响这里发送
	if (isChatRunning(chat)) {
		ElMessage.warning('当前对话还有回复正在生成，请稍候');
		return;
	}
	if (!retryOfId) state.inputContent = '';

	// 1. 预生成记录 Id，并据此建立 SignalR 分组（必须在请求前完成连接，否则本轮分片会丢）
	//    注意：雪花 Id 超出 JS 安全整数范围，服务端以字符串下发，全程按原值传递，不能做 Number 转换
	const idRes = await getAPI(AgentChatApi).apiAgentChatNewChatIdGet();
	const id = String(idRes.data.result);

	// 2. 本轮运行态：缓冲与本地消息都挂在对话上，切换对话后仍按原样继续接收
	const askMessage = {
		id: `temp-ask-${id}`,
		aiChatsId: chat.id!,
		content,
		isSend: true,
		sendStatus: 0,
		createTime: new Date(),
	} as unknown as ChatHistoryItem;
	const answerMessage = {
		id: `temp-answer-${id}`,
		aiChatsId: chat.id!,
		content: '',
		isSend: false,
		sendStatus: 0,
		createTime: new Date(),
	} as unknown as ChatHistoryItem;
	runs.set(id, {
		id,
		chatId: chatIdOf(chat),
		ask: askMessage,
		answer: answerMessage,
		content: '',
		tools: '',
		reasoning: '',
		process: '正在连接...',
		recommend: [],
		startedAt: Date.now(),
		abort: null,
		done: false,
	});
	clockTick.value = Date.now();
	// 之后的状态读写一律走 runs.get：reactive Map 里取出来的才是响应式代理
	const live = runs.get(id)!;

	try {
		await connectHub(id);
	} catch (e: unknown) {
		runs.delete(id);
		ElMessage.error(`实时通道连接失败：${asErrorLike(e).message ?? String(e)}`);
		return;
	}

	live.process = '正在提交...';
	// 只在本对话可见时插入气泡；在后台跑的对话等切回来时由 loadHistory 注入
	if (isActiveChat(live.chatId)) {
		state.historyList.push(live.ask, live.answer);
		scrollToBottom();
	}

	// 附件归本轮所有：从输入区摘掉这几份即可，期间用户可能在别的对话里另选了附件
	const files = [...state.files];
	state.files = state.files.filter((t) => !files.includes(t));

	try {
		// 用 AbortController 承载「停止生成」：SignalR 只负责推内容，中止请求才能真正让后端停掉模型调用
		const controller = new AbortController();
		live.abort = controller;
		const payload = {
			id,
			aiChatsId: chat.id!,
			content,
			isOnlineSearch: state.isOnlineSearch,
			fileNames: files.map((t) => t.fileName).join(','),
			contentFileUrls: files.map((t) => t.url).join(','),
			retryOfId: retryOfId ?? 0,
			isRecommendQuestion: state.isRecommendQuestion,
		} as unknown as AgentChatHistoryDto;

		const addRes = await getAPI(AgentChatHistoryApi).apiAgentChatHistoryAddPost(payload, { signal: controller.signal });
		const result = addRes.data.result;
		// 业务异常（内容为空、额度超限等）时统一返回结果里没有 result，接口层已弹过提示，这里按失败收尾
		if (!result) throw new Error('未获取到服务端返回结果，请点击重试');

		// 3. 服务端已落库本轮问答：标记定稿；该对话在前台就按服务端数据重载（本地临时行随之替换）
		//    定稿时记下本轮实测耗时：历史重载以提问 Id 为键回填到回复行上（落库时间差只有秒级精度）
		const elapsed = Date.now() - live.startedAt;
		answerDurations.set(live.id, elapsed);
		live.answer.durationMs = elapsed;
		live.done = true;
		live.process = '';
		if (isActiveChat(live.chatId)) await loadHistory();
		await loadChatList();
	} catch (e: unknown) {
		// 中止属于正常操作：保留已接收的半截内容
		const err = asErrorLike(e);
		const isAbort = err.message === 'canceled' || err.code === 'ERR_CANCELED' || err.name === 'CanceledError' || err.name === 'AbortError';
		const current = runs.get(id);
		if (current) {
			current.done = true;
			current.answer.content = current.content || (isAbort ? '（已停止生成）' : current.answer.content);
			if (current.tools) current.answer.aiToolsContent = current.tools;
			if (current.reasoning) current.answer.aiReasoningContent = current.reasoning;
			current.answer.durationMs = Date.now() - current.startedAt;
			current.answer.sendStatus = 1;
			current.answer.failReason = isAbort ? '已手动停止生成' : (err.message ?? '请求失败');
			current.process = '';
		}
		if (!isAbort) ElMessage.error(`发送失败：${err.message ?? String(e)}`);
	} finally {
		await closeRunHub(id);
		runs.delete(id);
	}
};

/** 停止生成：只中止当前对话那一轮，别的对话的推流不受影响 */
const handleStop = async () => {
	const run = activeRun.value;
	if (!run) return;
	run.process = '已停止';
	run.abort?.abort();
};

const handleRetry = async (message: ChatHistoryItem) => {
	// 提问行重试：把该提问记录 Id 作为 retryOfId 回传，服务端会废弃旧的失败问答后重新生成
	if (!message.isSend || String(message.id).startsWith('temp-')) {
		ElMessage.warning('该记录尚未落库，无法重试');
		return;
	}
	await handleSend(message.id, message.content ?? '');
};

onMounted(async () => {
	await loadChatList();
	const agentRes = await getAPI(AgentApi).apiAgentMyListGet();
	state.agentList = agentRes.data.result ?? [];
	if (state.chatList.length) {
		await handleSelectChat(state.chatList[0]);
	}
});

onBeforeUnmount(async () => {
	if (thinkingTimer) {
		clearInterval(thinkingTimer);
		thinkingTimer = null;
	}
	stopVoice();
	// 离开页面：中止所有仍在跑的轮次，别把请求与连接挂到组件销毁之后
	for (const [runId, run] of [...runs]) {
		run.done = true;
		run.abort?.abort();
		await closeRunHub(runId);
	}
	runs.clear();
});

/* ---------------------------- 语音输入（阿里云 NLS 实时转写） ---------------------------- */

/** 语音识别状态；idle=未开启 connecting=连接中 listening=识别中 */
const asrState = ref<AsrState>('idle');
const asrActive = computed(() => asrState.value === 'listening' || asrState.value === 'connecting');
/** 当前识别实例：每次开始都按新 Token 重建（Token 24h 过期，缓存实例会拿到失效凭据） */
let asrInstance: ReturnType<typeof createAliAsr> | null = null;
/** 开始录音时的输入框内容：识别文本追加在它后面，不覆盖用户已输入的内容 */
let inputContentBeforeAsr = '';

const toggleVoice = async () => {
	if (asrActive.value) {
		stopVoice();
		return;
	}
	try {
		// 服务端只签发临时 Token；未配置阿里云语音时接口返回 400，这里给出明确提示
		const res = await getAPI(AgentAsrApi).apiAgentAsrTokenGet();
		const { token, appKey } = res.data.result ?? {};
		if (!token || !appKey) {
			ElMessage.warning('语音服务未配置，请联系管理员在 Agent 配置中填写阿里云语音参数');
			return;
		}
		stopVoice();
		asrInstance = createAliAsr({
			appKey,
			token,
			onResult: (text) => {
				// 中间结果是「已累积文本 + 当前句」，直接整体回填输入框
				state.inputContent = (inputContentBeforeAsr + text).slice(0, 8000);
			},
			onError: (message) => {
				ElMessage.error(message || '语音识别失败');
				asrState.value = 'idle';
			},
			onStateChange: (s) => {
				asrState.value = s;
				if (s === 'listening') ElMessage.success('正在聆听，再次点击麦克风结束');
				if (s === 'stopped' || s === 'error') asrState.value = 'idle';
			},
		});
		inputContentBeforeAsr = state.inputContent;
		await asrInstance.start();
	} catch {
		// 业务异常（如未配置阿里云语音）已由请求拦截器统一弹提示，这里不再重复打扰；仅复位状态即可
		asrState.value = 'idle';
	}
};

const stopVoice = () => {
	asrInstance?.stop();
	asrState.value = 'idle';
};
</script>

<style scoped>
/* ============================== 整体骨架 ============================== */
.agents-chat-container {
	/* 根节点是 .layout-parent 的 flex 项：吃满剩余高度，不再用 100vh 减固定值（页脚/标签栏高度变化会留白） */
	flex: 1 1 auto;
	min-height: 0;
	display: flex;
	flex-direction: column;
}

.chat-layout {
	flex: 1;
	min-height: 0;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	overflow: hidden;
	background-color: var(--el-bg-color);
}

/* ============================== 左侧会话列表 ============================== */
.chat-aside {
	display: flex;
	flex-direction: column;
	border-right: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);
}

.aside-header {
	display: flex;
	flex-direction: column;
	gap: 10px;
	padding: 14px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.aside-brand {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.aside-brand .el-icon {
	color: var(--el-color-primary);
}

.new-chat-btn {
	width: 100%;
}

.aside-list {
	flex: 1;
	padding: 8px;
}

.chat-item {
	position: relative;
	display: flex;
	align-items: center;
	padding: 9px 10px;
	margin-bottom: 4px;
	border-radius: 6px;
	cursor: pointer;
	transition:
		background-color 0.18s ease,
		box-shadow 0.18s ease;
}

.chat-item:hover {
	background-color: var(--el-fill-color-light);
}

/* 选中态用内阴影画左侧色条：不占宽度，列表文字不会因为选中而右移 */
.chat-item.active {
	background-color: var(--el-color-primary-light-9);
	box-shadow: inset 3px 0 0 var(--el-color-primary);
}

.chat-item.active .chat-item-title {
	color: var(--el-color-primary);
}

/* 删除按钮不占列表宽度：平时隐藏，悬停时浮到时间位置，正文可以铺满整行 */
.chat-item-del {
	position: absolute;
	top: 6px;
	right: 4px;
	width: 24px;
	height: 24px;
	padding: 0;
	opacity: 0;
	pointer-events: none;
	transition: opacity 0.18s ease;
}

.chat-item:hover .chat-item-del,
.chat-item:focus-within .chat-item-del {
	opacity: 1;
	pointer-events: auto;
}

.chat-item-main {
	flex: 1;
	min-width: 0;
}

.chat-item-row {
	display: flex;
	align-items: baseline;
	gap: 8px;
}

.chat-item-title {
	flex: 1;
	min-width: 0;
	font-size: 13px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.chat-item-time {
	flex-shrink: 0;
	font-size: 11px;
	color: var(--el-text-color-placeholder);
	transition: opacity 0.18s ease;
}

/* 删除按钮浮在同一位置，悬停时让出时间，避免文字叠在图标下面 */
.chat-item:hover .chat-item-time,
.chat-item:focus-within .chat-item-time {
	opacity: 0;
}

.chat-item-sub {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-top: 3px;
}

/* 智能体名称：小号主题色文字，悬停/选中底色下都能看清 */
.chat-item-agent {
	flex-shrink: 0;
	max-width: 96px;
	font-size: 11px;
	line-height: 17px;
	color: var(--el-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.chat-item-agent .ri-robot-2-line {
	margin-right: 2px;
	font-size: 12px;
	vertical-align: -1px;
}

.chat-item-msg {
	flex: 1;
	min-width: 0;
	font-size: 12px;
	line-height: 17px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* ============================== 右侧消息区 ============================== */
.chat-main {
	display: flex;
	flex-direction: column;
	padding: 0;
	/* 消息区不加背景色：直接透出容器的 --el-bg-color，亮暗主题都干净 */
}

.chat-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	height: 52px;
	padding: 0 16px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);
}

.chat-title {
	display: flex;
	align-items: center;
	gap: 8px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.chat-title-icon {
	font-size: 18px;
	color: var(--el-color-primary);
}

.chat-status {
	font-size: 12px;
	color: var(--el-color-primary);
}

/* 头部右侧：用量胶囊 / 超限提示 / 进度文案共用一行，min-width:0 让长文案可截断而不是撑破布局 */
.chat-header-right {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
}

/* 用量胶囊与超限提示的样式已随组件抽到 ./component/quotaPill.vue */

.chat-body {
	flex: 1;
	padding: 18px 20px;
}

.chat-empty {
	padding-top: 60px;
}

/* ---- TinyRobot 气泡桥接：文本变量接 EP，亮暗主题自动跟随 ---- */
.msg-row {
	display: flex;
	margin-bottom: 18px;
	--tr-text-primary: var(--el-text-color-primary);
	--tr-text-secondary: var(--el-text-color-secondary);
	--tr-text-tertiary: var(--el-text-color-placeholder);
}

/* 用户提问靠右、AI 回复靠左 */
.msg-user {
	justify-content: flex-end;
}

.msg-main {
	display: flex;
	flex-direction: column;
	gap: 6px;
	max-width: 78%;
	min-width: 0;
}

.msg-user .msg-main {
	align-items: flex-end;
}

/* 头像是 TinyRobot 内部渲染的，作用域选择器够不到，用 :deep 补配色：
   用户主题色、AI 红色渐变（用户指定），与两侧气泡底色区分开 */
.msg-row :deep(.el-avatar.is-user) {
	color: var(--el-color-white);
	background-color: var(--el-color-primary);
}

.msg-row :deep(.el-avatar.is-ai) {
	color: var(--el-color-white);
	background-image: linear-gradient(135deg, var(--el-color-danger) 0%, var(--el-color-danger-light-3) 100%);
}

/* AI 侧是 remixicon 字体图标，给到和 el-icon 视觉等大的字号 */
.msg-row :deep(.el-avatar.is-ai .ri-robot-2-fill) {
	font-size: 16px;
	line-height: 1;
}

/* 气泡本体：AI 白底描边（内容多，需要边界感）、用户主题色渐变底（短句，用颜色区分"我说的话"） */
.msg-row :deep(.tr-bubble) {
	--tr-bubble-max-width: 100%;
}

.msg-row :deep(.tr-bubble[data-placement='end']) {
	--tr-bubble-text-color: var(--el-color-white);
	--tr-bubble-box-shadow: 0 2px 8px color-mix(in srgb, var(--el-color-primary) 28%, transparent);
}

.msg-row :deep(.tr-bubble[data-placement='end'] .tr-bubble__box) {
	background-image: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
}

.msg-row :deep(.tr-bubble[data-placement='start']) {
	--tr-bubble-box-bg: var(--el-bg-color);
	--tr-bubble-box-border: 1px solid var(--el-border-color-lighter);
	--tr-bubble-box-shadow: none;
}

/* 用户消息保留换行与空格 */
.msg-row :deep(.tr-bubble__text) {
	white-space: pre-wrap;
	word-break: break-word;
}

/* Markdown 正文：渲染器只管结构，排版在这里补全（亮暗都走 EP 变量） */
.msg-row :deep(.markdown-body) {
	font-size: 14px;
	line-height: 1.7;
	color: var(--el-text-color-primary);
}

.msg-row :deep(.markdown-body > *:first-child) {
	margin-top: 0;
}

.msg-row :deep(.markdown-body > *:last-child) {
	margin-bottom: 0;
}

.msg-row :deep(.markdown-body p) {
	margin: 0 0 10px;
}

.msg-row :deep(.markdown-body h1),
.msg-row :deep(.markdown-body h2),
.msg-row :deep(.markdown-body h3),
.msg-row :deep(.markdown-body h4) {
	margin: 14px 0 8px;
	font-weight: 600;
	line-height: 1.4;
}

.msg-row :deep(.markdown-body h1) {
	font-size: 17px;
}

.msg-row :deep(.markdown-body h2) {
	font-size: 16px;
}

.msg-row :deep(.markdown-body h3) {
	font-size: 14.5px;
}

.msg-row :deep(.markdown-body h4) {
	font-size: 14px;
}

.msg-row :deep(.markdown-body ul),
.msg-row :deep(.markdown-body ol) {
	margin: 0 0 10px;
	padding-left: 20px;
}

.msg-row :deep(.markdown-body li) {
	margin: 2px 0;
}

.msg-row :deep(.markdown-body code) {
	padding: 1px 5px;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12.5px;
	color: var(--el-color-danger);
	background-color: var(--el-fill-color-light);
	border-radius: 4px;
}

.msg-row :deep(.markdown-body pre) {
	margin: 0 0 10px;
	padding: 10px 12px;
	overflow: auto;
	background-color: var(--el-fill-color-darker);
	border-radius: 8px;
}

.msg-row :deep(.markdown-body pre code) {
	padding: 0;
	font-size: 12.5px;
	line-height: 1.6;
	color: var(--el-text-color-primary);
	background: none;
}

.msg-row :deep(.markdown-body table) {
	margin: 0 0 10px;
	border-collapse: collapse;
	font-size: 13px;
}

.msg-row :deep(.markdown-body th),
.msg-row :deep(.markdown-body td) {
	padding: 5px 10px;
	border: 1px solid var(--el-border-color-lighter);
	text-align: left;
}

.msg-row :deep(.markdown-body th) {
	font-weight: 600;
	background-color: var(--el-fill-color-light);
}

.msg-row :deep(.markdown-body blockquote) {
	margin: 0 0 10px;
	padding: 4px 12px;
	color: var(--el-text-color-secondary);
	border-left: 3px solid var(--el-color-primary-light-5);
	background-color: var(--el-fill-color-light);
	border-radius: 0 6px 6px 0;
}

.msg-row :deep(.markdown-body a) {
	color: var(--el-color-primary);
	text-decoration: none;
}

.msg-row :deep(.markdown-body a:hover) {
	text-decoration: underline;
}

.msg-row :deep(.markdown-body hr) {
	margin: 12px 0;
	border: none;
	border-top: 1px solid var(--el-border-color-lighter);
}

.msg-row :deep(.markdown-body img) {
	max-width: 100%;
	border-radius: 6px;
}

/* ---- 气泡外的附加信息 ---- */
.msg-extras {
	display: flex;
	flex-direction: column;
	gap: 6px;
	width: 100%;
	min-width: 0;
}

/* ---- 等待动效：三点跳动 + 秒数 ---- */
.msg-wait {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	width: fit-content;
	padding: 8px 14px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 12px;
	background-color: var(--el-fill-color-lighter);
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.msg-wait__dots {
	display: inline-flex;
	align-items: center;
	gap: 3px;
}

.msg-wait__dots i {
	width: 5px;
	height: 5px;
	border-radius: 50%;
	background-color: var(--el-color-primary);
	animation: msg-wait-bounce 1.1s ease-in-out infinite;
}

.msg-wait__dots i:nth-child(2) {
	animation-delay: 0.15s;
}

.msg-wait__dots i:nth-child(3) {
	animation-delay: 0.3s;
}

.msg-wait__text {
	color: var(--el-text-color-regular);
}

.msg-wait__time {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	font-variant-numeric: tabular-nums;
}

@keyframes msg-wait-bounce {
	0%,
	80%,
	100% {
		transform: translateY(0);
		opacity: 0.35;
	}

	40% {
		transform: translateY(-4px);
		opacity: 1;
	}
}

/* ---- 思考过程 / 工具调用卡片：同一套结构，靠图标与描边色区分来源 ---- */
.think {
	width: 100%;
	overflow: hidden;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background-color: var(--el-fill-color-lighter);
}

.think__head {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 7px 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	user-select: none;
	list-style: none;
	transition: background-color 0.18s ease;
}

.think__head::-webkit-details-marker {
	display: none;
}

.think__head:hover {
	background-color: var(--el-fill-color-light);
}

.think__icon {
	font-size: 14px;
	color: var(--el-color-primary);
}

.think__title {
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.think__count {
	margin-left: auto;
	padding: 0 6px;
	border-radius: 8px;
	background-color: var(--el-fill-color);
	font-size: 11px;
	line-height: 16px;
	font-variant-numeric: tabular-nums;
}

.think__arrow {
	font-size: 15px;
	transition: transform 0.2s ease;
}

.think[open] .think__arrow {
	transform: rotate(180deg);
}

.think__body {
	max-height: 260px;
	overflow: auto;
	padding: 8px 12px 10px;
	border-top: 1px dashed var(--el-border-color-lighter);
}

/* 思考文本用弱化色：它是过程信息，不该比回答正文更抢眼 */
.think__text {
	margin: 0;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 19px;
	white-space: pre-wrap;
	word-break: break-word;
	color: var(--el-text-color-secondary);
}

/* 生成中：主题色描边 + 呼吸点，标题改成「正在思考」 */
.think.is-live {
	border-color: color-mix(in srgb, var(--el-color-primary) 32%, var(--el-border-color-lighter));
	background-color: color-mix(in srgb, var(--el-color-primary) 4%, var(--el-fill-color-lighter));
}

.think.is-live .think__head {
	cursor: default;
}

.think__pulse {
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background-color: var(--el-color-primary);
	animation: think-pulse 1.4s ease-out infinite;
}

@keyframes think-pulse {
	0% {
		box-shadow: 0 0 0 0 color-mix(in srgb, var(--el-color-primary) 45%, transparent);
	}

	100% {
		box-shadow: 0 0 0 8px transparent;
	}
}

.think--tools {
	border-color: color-mix(in srgb, var(--el-color-warning) 26%, var(--el-border-color-lighter));
	background-color: color-mix(in srgb, var(--el-color-warning) 5%, var(--el-fill-color-lighter));
}

.think--tools .think__icon {
	color: var(--el-color-warning);
}

.msg-files {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
}

/* 失败提示不拉满整行：收在 520px 内，用户消息靠右对齐，避免红块横亘整行、比气泡还宽 */
.msg-fail {
	width: 100%;
	max-width: 520px;
}

.msg-user .msg-fail {
	margin-left: auto;
}

.msg-meta {
	display: flex;
	align-items: center;
	gap: 14px;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}

/* 用户消息的元信息贴着气泡右边缘，否则整行左对齐、跟右侧气泡脱节 */
.msg-user .msg-meta {
	justify-content: flex-end;
}

.msg-user .msg-files {
	justify-content: flex-end;
}

/* AI 相关日志入口：做成小圆角 chip，比裸链接更像可点按钮，与推荐问题 chip 视觉同族 */
.msg-log-link {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	padding: 1px 8px;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-color-primary);
	cursor: pointer;
	background-color: color-mix(in srgb, var(--el-color-primary) 8%, transparent);
	border: none;
	border-radius: 10px;
	transition: background-color 0.18s ease;
}

.msg-log-link:hover {
	background-color: color-mix(in srgb, var(--el-color-primary) 16%, transparent);
}

/* 弹层：标题栏 + 卡片式日志列表（弹层内容挂 body 上，但元素在本组件模板内编译，作用域样式可命中） */
.msg-log {
	display: flex;
	flex-direction: column;
	gap: 10px;
}

.msg-log__head {
	display: flex;
	align-items: center;
	padding-bottom: 8px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.msg-log__title {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.msg-log__title i {
	font-size: 15px;
	color: var(--el-color-primary);
}

.msg-log__badge {
	margin-left: auto;
	padding: 0 8px;
	border-radius: 10px;
	background-color: var(--el-fill-color);
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	font-variant-numeric: tabular-nums;
}

.msg-log__list {
	display: flex;
	flex-direction: column;
	gap: 8px;
	max-height: 420px;
	padding-right: 2px;
	overflow: auto;
}

/* 左侧色条按日志类型着色：类型标签之外再给一层整卡可扫读的识别线 */
.msg-log-item {
	padding: 8px 10px 8px 12px;
	border-left: 3px solid var(--el-border-color);
	border-radius: 8px;
	background-color: var(--el-fill-color-lighter);
}

.msg-log-item.is-type-1 {
	border-left-color: var(--el-color-primary);
}

.msg-log-item.is-type-2 {
	border-left-color: var(--el-color-success);
}

.msg-log-item.is-type-3 {
	border-left-color: var(--el-color-info);
}

.msg-log-item.is-type-4 {
	border-left-color: var(--el-color-danger);
}

.msg-log-item.is-type-5 {
	border-left-color: var(--el-color-warning);
}

.msg-log-item__head {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 6px;
}

.msg-log-item__idx {
	margin-left: auto;
	font-size: 11px;
	color: var(--el-text-color-placeholder);
	font-variant-numeric: tabular-nums;
}

/* 单条日志不再自己开滚动条：整段完整展开，滚动交给外层列表，避免弹层里套着两层滚动 */
.msg-log-item__content {
	margin: 0;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	font-size: 12px;
	line-height: 18px;
	white-space: pre-wrap;
	word-break: break-all;
	color: var(--el-text-color-regular);
}

/* 推荐问题：做成浅底胶囊按钮，比 el-tag 更像"可点的建议"而不是"标签" */
.msg-recommend {
	width: 100%;
	padding-top: 10px;
	border-top: 1px dashed var(--el-border-color-lighter);
}

.msg-recommend-title {
	margin-bottom: 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.msg-recommend-list {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
}

.msg-recommend-chip {
	padding: 5px 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 14px;
	background-color: var(--el-bg-color);
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-regular);
	cursor: pointer;
	transition:
		color 0.18s ease,
		border-color 0.18s ease,
		background-color 0.18s ease;
}

.msg-recommend-chip:hover {
	border-color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
}

/* ============================== 输入区（TrSender） ============================== */
.chat-input {
	padding: 10px 16px 14px;
	border-top: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-bg-color);
}

/* 输入框整体收成圆角卡片；文本变量桥接 EP，深色主题下编辑器文字可读 */
.chat-input :deep(.tr-sender) {
	--tr-color-primary: var(--el-color-primary);
	--tr-color-primary-light: var(--el-color-primary-light-5);
	--tr-sender-button-size-submit: 26px;
	--tr-sender-text-color: var(--el-text-color-primary);
	--tr-sender-placeholder-color: var(--el-text-color-placeholder);
	--tr-text-primary: var(--el-text-color-primary);
	--tr-text-secondary: var(--el-text-color-secondary);
	--tr-text-tertiary: var(--el-text-color-placeholder);

	border: 1px solid var(--el-border-color-lighter);
	border-radius: 12px;
	background-color: var(--el-bg-color);
	box-shadow: var(--el-box-shadow-lighter);
	transition:
		border-color 0.25s ease,
		box-shadow 0.25s ease;
}

.chat-input :deep(.tr-sender:focus-within) {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

/* 附件按钮：圆形工具键，hover 铺主题色浅底 */
.chat-tool {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 30px;
	height: 30px;
	padding: 0;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	background: transparent;
	border: none;
	border-radius: 50%;
	transition:
		color 0.2s ease,
		background-color 0.2s ease;
}

.chat-tool:hover {
	color: var(--el-color-primary);
	background-color: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
}

/* 语音识别中：红色底 + 呼吸动画，一眼可辨「正在录音」 */
.chat-tool.is-asr-active {
	color: var(--el-color-white);
	background-color: var(--el-color-danger);
	animation: asr-breath 1.4s ease-in-out infinite;
}

@keyframes asr-breath {
	0%,
	100% {
		box-shadow: 0 0 0 0 color-mix(in srgb, var(--el-color-danger) 45%, transparent);
	}

	50% {
		box-shadow: 0 0 0 6px color-mix(in srgb, var(--el-color-danger) 0%, transparent);
	}
}

.chat-tool:disabled {
	cursor: not-allowed;
	opacity: 0.5;
}

/* 底部一行：已选附件 + 开关胶囊 */
.input-tags {
	display: flex;
	flex: 1;
	align-items: center;
	gap: 6px;
	min-width: 0;
}

.input-tags-spacer {
	flex: 1;
}

/* 开关 + 文字合成一枚小胶囊：两个开关并排时，文字与控件的关系一眼能对上 */
.toggle-chip {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 3px 8px 3px 10px;
	border-radius: 12px;
	background-color: var(--el-fill-color-light);
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	/* 外层用 div 而不是 label：label 包裹 el-switch 时点文字会触发原生 input 勾选，而可见状态由 v-model 驱动，两者不同步就会看着开了其实没开 */
	user-select: none;
}

/* 空草稿时右下角的快捷键提示：弱化到几乎只剩形状，不抢输入区的注意力 */
.send-hint {
	padding-right: 4px;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	user-select: none;
}

/* ============================== 新建对话：选择智能体 ============================== */
/* 弹窗标题不要在这里写任何覆盖：头部背景是主题色，标题白色由全局 .dialog-title（theme/page.scss）统一给，
   本文件的 scoped 规则带 [data-v] 优先级更高，一写就会把白色顶成正文黑、把图标顶成主题色（绿底绿字看不见） */
.agent-pick-tip {
	margin-bottom: 12px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.agent-pick-grid {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 10px;
	max-height: 52vh;
	overflow-y: auto;
	padding-right: 2px;
}

.agent-pick-card {
	position: relative;
	display: flex;
	gap: 10px;
	padding: 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	cursor: pointer;
	transition:
		border-color 0.18s ease,
		background-color 0.18s ease,
		box-shadow 0.18s ease;
}

.agent-pick-card:hover {
	border-color: var(--el-color-primary-light-5);
	box-shadow: var(--el-box-shadow-lighter);
}

.agent-pick-card.active {
	border-color: var(--el-color-primary);
	background-color: var(--el-color-primary-light-9);
}

.agent-pick-card.active .agent-pick-check {
	opacity: 1;
	transform: scale(1);
}

.agent-pick-icon {
	flex-shrink: 0;
	width: 34px;
	height: 34px;
	display: grid;
	place-items: center;
	border-radius: 8px;
	background-color: var(--el-color-primary-light-7);
	color: var(--el-color-primary);
	font-size: 17px;
}

.agent-pick-body {
	flex: 1;
	min-width: 0;
}

.agent-pick-name {
	font-size: 14px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.agent-pick-desc {
	margin-top: 2px;
	font-size: 12px;
	line-height: 18px;
	color: var(--el-text-color-secondary);
	display: -webkit-box;
	-webkit-box-orient: vertical;
	-webkit-line-clamp: 2;
	overflow: hidden;
}

.agent-pick-tags {
	margin-top: 6px;
}

.agent-pick-none {
	font-size: 12px;
	line-height: 22px;
	color: var(--el-text-color-placeholder);
}

/* 选中标记：默认缩到 0 隐藏，避免未选中的卡片上也挂着对勾造成误读 */
.agent-pick-check {
	position: absolute;
	top: 8px;
	right: 8px;
	color: var(--el-color-primary);
	font-size: 16px;
	opacity: 0;
	transform: scale(0.6);
	transition:
		opacity 0.18s ease,
		transform 0.18s ease;
}

/* 工具审批弹窗的样式已随组件抽到 ./component/toolApprovalDialog.vue */
</style>
