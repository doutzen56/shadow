<template>
	<div class="mqs-page">
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
					<h2 class="page-hero__title">MQTT 服务端</h2>
					<p class="page-hero__desc">内置 Broker 的运行状态与消息流水：查看在线客户端、下发测试消息、回溯每一条收发记录。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats" title="服务状态与客户端数量每 5 秒自动刷新一次">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v" :class="state.status.started ? 'is-on' : 'is-off'">{{ state.status.started ? '运行中' : '未启动' }}</span>
							<span class="page-hero__stat-k">服务状态</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.status.clientCount || 0 }}</span>
							<span class="page-hero__stat-k">在线客户端</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.status.messageCount || 0 }}</span>
							<span class="page-hero__stat-k">消息记录</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.status.port || '-' }}</span>
							<span class="page-hero__stat-k">TCP 端口</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 服务端信息：Broker 的连接端点与运行开关（四张信息卡等宽铺满，操作区已移入「客户端列表」卡头） -->
		<div class="mqs-facts">
			<div class="mqs-fact">
				<span class="mqs-fact-ico is-blue"><el-icon><ele-Link /></el-icon></span>
				<div class="mqs-fact-meta">
					<span class="mqs-fact-k">WebSocket 路径</span>
					<span class="mqs-fact-v" :title="state.status.webSocketPath || '/mqtt'">{{ state.status.webSocketPath || '/mqtt' }}</span>
				</div>
			</div>
			<div class="mqs-fact">
				<span class="mqs-fact-ico is-violet"><el-icon><ele-Cpu /></el-icon></span>
				<div class="mqs-fact-meta">
					<span class="mqs-fact-k">服务端 ClientId</span>
					<span class="mqs-fact-v" :title="state.status.mqttServerId || '-'">{{ state.status.mqttServerId || '-' }}</span>
				</div>
			</div>
			<div class="mqs-fact">
				<span class="mqs-fact-ico" :class="state.status.enabled ? 'is-green' : 'is-gray'"><el-icon><ele-SwitchButton /></el-icon></span>
				<div class="mqs-fact-meta">
					<span class="mqs-fact-k">配置状态</span>
					<span class="mqs-fact-v" :class="state.status.enabled ? 'is-on' : 'is-off'">{{ state.status.enabled ? '已启用' : '未启用' }}</span>
				</div>
			</div>
			<div class="mqs-fact">
				<span class="mqs-fact-ico is-amber"><el-icon><ele-FolderOpened /></el-icon></span>
				<div class="mqs-fact-meta">
					<span class="mqs-fact-k">内存保留上限</span>
					<span class="mqs-fact-v">1000 条</span>
				</div>
			</div>
		</div>

		<div class="mqs-grid">
			<el-card shadow="hover" class="mqs-panel">
				<template #header>
					<div class="mqs-head">
						<span class="mqs-head-ico is-blue"><el-icon><ele-Monitor /></el-icon></span>
						<span class="mqs-head-title">客户端列表</span>
						<span class="mqs-head-count">{{ state.clients.length }}</span>
						<div class="mqs-head-acts">
							<span
								class="mqs-auto"
								:class="{ 'is-on': state.autoRefresh }"
								:title="state.autoRefresh ? '每 5 秒自动刷新服务状态与客户端列表' : '已暂停：关闭自动刷新后需手动点击刷新'"
							>
								<el-switch v-model="state.autoRefresh" size="small" />
								<span class="mqs-auto-text">自动刷新</span>
							</span>
							<el-button
								icon="ele-Refresh"
								plain
								@click="refreshAll"
								:loading="state.loadingStatus || state.loadingClients"
							>
								刷新
							</el-button>
						</div>
					</div>
				</template>
				<vxe-grid v-bind="clientGridOptions" :data="state.clients" :loading="state.loadingClients">
					<template #client_endpoint="{ row }">{{ row.remoteEndPoint || row.endpoint || '-' }}</template>
					<template #client_connected="{ row }">{{ formatDateTime(row.connectedTimestamp) }}</template>
					<template #client_lastPacket="{ row }">{{ formatDateTime(row.lastPacketReceivedTimestamp) }}</template>
					<template #client_bytes="{ row }">{{ formatBytes(row.bytesReceived) }} / {{ formatBytes(row.bytesSent) }}</template>
					<template #client_appMessages="{ row }">{{ row.receivedApplicationMessagesCount || 0 }} / {{ row.sentApplicationMessagesCount || 0 }}</template>
					<template #client_pending="{ row }">{{ row.session?.pendingApplicationMessagesCount || 0 }}</template>
					<template #empty>
						<el-empty description="当前没有在线客户端" :image-size="64" />
					</template>
				</vxe-grid>
			</el-card>

			<el-card shadow="hover" class="mqs-panel">
				<template #header>
					<div class="mqs-head">
						<span class="mqs-head-ico is-green"><el-icon><ele-Promotion /></el-icon></span>
						<span class="mqs-head-title">发布消息</span>
					</div>
				</template>
				<el-form ref="publishFormRef" :model="state.publishForm" :rules="publishRules" label-width="auto" class="mqs-publish-form">
					<el-form-item label="主题" prop="topic">
						<el-input v-model="state.publishForm.topic" clearable placeholder="例如：demo/test" />
					</el-form-item>
					<el-form-item label="内容" prop="message" class="is-grow">
						<el-input v-model="state.publishForm.message" type="textarea" :rows="9" resize="none" placeholder="请输入要发布的消息内容" />
					</el-form-item>
					<el-form-item class="mqs-publish-ops">
						<el-button type="primary" icon="ele-Promotion" :loading="state.publishing" @click="handlePublish" v-auth="'sysMqtt/publicMessage'">发布</el-button>
						<el-button icon="ele-RefreshLeft" @click="resetPublishForm">重置</el-button>
					</el-form-item>
				</el-form>
			</el-card>
		</div>

		<el-card shadow="hover" class="mqs-panel">
			<template #header>
				<div class="mqs-head">
					<span class="mqs-head-ico is-orange"><el-icon><ele-Document /></el-icon></span>
					<span class="mqs-head-title">消息记录</span>
					<div class="mqs-head-acts">
						<el-button icon="ele-Refresh" plain @click="loadMessages" :loading="state.loadingMessages" v-auth="'sysMqtt/messagePage'">刷新</el-button>
						<el-button icon="ele-Delete" type="danger" plain @click="handleClearMessages" v-auth="'sysMqtt/clearMessages'">清空</el-button>
					</div>
				</div>
			</template>
			<el-form :model="state.query" label-width="auto" class="mqs-query">
				<el-form-item label="ClientId">
					<el-input v-model="state.query.clientId" class="mqs-input" clearable placeholder="客户端" />
				</el-form-item>
				<el-form-item label="主题">
					<el-input v-model="state.query.topic" class="mqs-input" clearable placeholder="Topic" />
				</el-form-item>
				<el-form-item label="事件">
					<el-select v-model="state.query.eventType" class="mqs-select" clearable placeholder="全部">
						<el-option v-for="item in eventTypes" :key="item.value" :label="item.label" :value="item.value" />
					</el-select>
				</el-form-item>
				<div class="mqs-query-acts">
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleSearch"> 查询 </el-button>
						<el-button icon="ele-RefreshLeft" @click="resetSearch"> 重置 </el-button>
					</el-button-group>
				</div>
			</el-form>
			<vxe-grid v-bind="messageGridOptions" :data="state.messages" :loading="state.loadingMessages">
				<template #msg_time="{ row }">{{ formatDateTime(row.time) }}</template>
				<template #msg_event="{ row }">
					<el-tag :type="getEventTagType(row.eventType)" effect="light">{{ getEventLabel(row.eventType) }}</el-tag>
				</template>
				<template #msg_length="{ row }">{{ row.payloadLength || 0 }}</template>
				<template #msg_retain="{ row }">
					<el-tag :type="row.retain ? 'warning' : 'info'" effect="plain">{{ row.retain ? '是' : '否' }}</el-tag>
				</template>
				<template #empty>
					<el-empty description="暂无消息记录" :image-size="64" />
				</template>
			</vxe-grid>
			<div class="mqs-pager">
				<el-pagination
					v-model:current-page="state.query.page"
					v-model:page-size="state.query.pageSize"
					:page-sizes="[20, 50, 100, 200]"
					layout="total, sizes, prev, pager, next, jumper"
					:total="state.total"
					@size-change="loadMessages"
					@current-change="loadMessages"
				/>
			</div>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="sysMqtt">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance, FormRules } from 'element-plus';
import { VxeGridProps } from 'vxe-table';
import { formatDateTime } from '/@/utils/format';
import { clearMqttMessages, getMqttClients, getMqttMessagePage, getMqttStatus, MqttClientStatus, MqttMessageRecord, MqttStatusOutput, publishMqttMessage } from '/@/api/system/mqtt';

const publishFormRef = ref<FormInstance>();
let refreshTimer: ReturnType<typeof window.setInterval> | undefined;

// 在线客户端列表（每 5 秒整表刷新，静态数据绑定）
const clientGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	height: 360,
	columns: [
		{ field: 'id', title: 'ClientId', minWidth: 180, showOverflow: 'tooltip' },
		{ field: 'endpoint', title: '连接端点', minWidth: 150, showOverflow: 'tooltip', slots: { default: 'client_endpoint' } },
		{ field: 'connectedTimestamp', title: '连接时间', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'client_connected' } },
		{ field: 'lastPacketReceivedTimestamp', title: '最后收包', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'client_lastPacket' } },
		{ field: 'bytes', title: '收/发字节', minWidth: 160, showOverflow: 'tooltip', slots: { default: 'client_bytes' } },
		{ field: 'appMessages', title: '应用消息', width: 110, slots: { default: 'client_appMessages' } },
		{ field: 'pending', title: '待发', width: 90, slots: { default: 'client_pending' } },
	],
});

// 消息记录列表（条件分页由外部 el-pagination 驱动）
const messageGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	height: 420,
	columns: [
		{ field: 'time', title: '时间', width: 170, showOverflow: 'tooltip', slots: { default: 'msg_time' } },
		{ field: 'eventType', title: '事件', width: 120, slots: { default: 'msg_event' } },
		{ field: 'clientId', title: 'ClientId', minWidth: 170, showOverflow: 'tooltip' },
		{ field: 'topic', title: '主题', minWidth: 180, showOverflow: 'tooltip' },
		{ field: 'payload', title: '内容', minWidth: 260, showOverflow: 'tooltip' },
		{ field: 'qualityOfServiceLevel', title: 'QoS', width: 90 },
		{ field: 'payloadLength', title: '长度', width: 90, slots: { default: 'msg_length' } },
		{ field: 'retain', title: 'Retain', width: 90, slots: { default: 'msg_retain' } },
		{ field: 'remark', title: '说明', minWidth: 120, showOverflow: 'tooltip' },
	],
});

const state = reactive({
	loadingStatus: false,
	loadingClients: false,
	loadingMessages: false,
	publishing: false,
	autoRefresh: true,
	status: {
		enabled: false,
		started: false,
		port: 0,
		webSocketPath: '/mqtt',
		mqttServerId: '',
		logging: false,
		clientCount: 0,
		messageCount: 0,
	} as MqttStatusOutput,
	clients: [] as MqttClientStatus[],
	messages: [] as MqttMessageRecord[],
	total: 0,
	query: {
		page: 1,
		pageSize: 20,
		clientId: '',
		topic: '',
		eventType: '',
	},
	publishForm: {
		topic: '',
		message: '',
	},
});

const eventTypes = [
	{ label: '客户端连接', value: 'ClientConnected' },
	{ label: '客户端断开', value: 'ClientDisconnected' },
	{ label: '订阅主题', value: 'Subscribed' },
	{ label: '取消订阅', value: 'Unsubscribed' },
	{ label: '客户端发布', value: 'ClientPublish' },
	{ label: '服务端发布', value: 'ServerPublish' },
	{ label: '未被消费', value: 'NotConsumed' },
];

const publishRules = computed<FormRules>(() => ({
	topic: [{ required: true, message: '请输入主题', trigger: 'blur' }],
	message: [{ required: true, message: '请输入消息内容', trigger: 'blur' }],
}));

onMounted(async () => {
	await refreshAll();
	await loadMessages();
	refreshTimer = window.setInterval(() => {
		if (state.autoRefresh) refreshAll();
	}, 5000);
});

onBeforeUnmount(() => {
	if (refreshTimer) window.clearInterval(refreshTimer);
});

const refreshAll = async () => {
	await Promise.all([loadStatus(), loadClients()]);
};

const loadStatus = async () => {
	state.loadingStatus = true;
	try {
		const res = await getMqttStatus();
		state.status = res.data.result ?? state.status;
	} finally {
		state.loadingStatus = false;
	}
};

const loadClients = async () => {
	state.loadingClients = true;
	try {
		const res = await getMqttClients();
		state.clients = res.data.result ?? [];
	} catch {
		state.clients = [];
	} finally {
		state.loadingClients = false;
	}
};

const loadMessages = async () => {
	state.loadingMessages = true;
	try {
		const res = await getMqttMessagePage({
			page: state.query.page,
			pageSize: state.query.pageSize,
			clientId: state.query.clientId || undefined,
			topic: state.query.topic || undefined,
			eventType: state.query.eventType || undefined,
		});
		const result = res.data.result;
		state.messages = result?.items ?? [];
		state.total = result?.total ?? 0;
		await loadStatus();
	} finally {
		state.loadingMessages = false;
	}
};

const handleSearch = async () => {
	state.query.page = 1;
	await loadMessages();
};

const resetSearch = async () => {
	state.query.clientId = '';
	state.query.topic = '';
	state.query.eventType = '';
	state.query.page = 1;
	await loadMessages();
};

const handlePublish = async () => {
	if (!publishFormRef.value) return;
	await publishFormRef.value.validate(async (valid) => {
		if (!valid) return;
		state.publishing = true;
		try {
			await publishMqttMessage({ ...state.publishForm });
			ElMessage.success('发布成功');
			await Promise.all([loadMessages(), loadStatus()]);
		} finally {
			state.publishing = false;
		}
	});
};

const resetPublishForm = () => {
	state.publishForm.topic = '';
	state.publishForm.message = '';
	publishFormRef.value?.clearValidate();
};

const handleClearMessages = async () => {
	await ElMessageBox.confirm('确认清空当前内存中的 MQTT 消息记录？', '提示', {
		type: 'warning',
		confirmButtonText: '确认',
		cancelButtonText: '取消',
	});
	await clearMqttMessages();
	ElMessage.success('已清空');
	await loadMessages();
};

// MQTT 侧按「整数位 + 1 位小数」展示（B 不显示小数），与通用 formatBytes 的 2 位小数口径不同，保留本地实现
const formatBytes = (value?: number) => {
	const bytes = Number(value || 0);
	if (bytes <= 0) return '0 B';
	const units = ['B', 'KB', 'MB', 'GB', 'TB'];
	const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
	return `${(bytes / Math.pow(1024, index)).toFixed(index === 0 ? 0 : 1)} ${units[index]}`;
};

const getEventLabel = (eventType: string) => eventTypes.find((u) => u.value === eventType)?.label ?? eventType;

const getEventTagType = (eventType: string) => {
	switch (eventType) {
		case 'ClientConnected':
			return 'success';
		case 'ClientDisconnected':
			return 'info';
		case 'ClientPublish':
			return 'primary';
		case 'ServerPublish':
			return 'warning';
		case 'NotConsumed':
			return 'danger';
		default:
			return '';
	}
};
</script>

<style lang="scss" scoped>
.mqs-page {
	/* 页边距统一由 .layout-parent 的 padding:5px 提供（与打印模板页一致），这里不再叠加 */
	position: relative;
	padding-bottom: 4px;
	background: var(--el-bg-color);
	min-height: 100%;

	/* 页面内各块（信息卡行 / 面板网格 / 独立面板）统一 14px 上下间距：
	   不再各自写 margin-top + margin-bottom，否则相邻处要么叠成双倍、要么（网格→下一个面板）干脆为 0 */
	> .mqs-facts,
	> .mqs-grid,
	> .mqs-panel {
		margin-top: 14px;
	}
}

/* ===================== 服务端信息卡 ===================== */
/* 四张等宽信息卡铺满整行（操作区已移入「客户端列表」卡头，不再单独占一个色块） */
.mqs-facts {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 14px;
}

.mqs-fact {
	position: relative;
	overflow: hidden;
	display: flex;
	align-items: center;
	gap: 12px;
	min-width: 0;
	min-height: 84px;
	padding: 14px 16px;
	border-radius: 14px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		transform 0.28s ease,
		border-color 0.28s ease,
		box-shadow 0.28s ease;

	/* 顶部细彩条：只做装饰，pointer-events 由父级统一关掉 */
	&::before {
		content: '';
		position: absolute;
		inset: 0 0 auto 0;
		height: 3px;
		background: linear-gradient(90deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary), #ffffff 65%));
		opacity: 0.85;
	}

	&:hover {
		transform: translateY(-3px);
		border-color: color-mix(in srgb, var(--el-color-primary) 32%, transparent);
		box-shadow: 0 16px 30px -20px color-mix(in srgb, var(--el-color-primary) 95%, transparent);
	}
}

.mqs-fact-ico {
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 44px;
	height: 44px;
	border-radius: 13px;
	font-size: 20px;
	/* 装饰规范：浅色同色系底 + 深色图标 */
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-blue {
		--tint: #3b82f6;
	}

	&.is-violet {
		--tint: #8b5cf6;
	}

	&.is-green {
		--tint: #10b981;
	}

	&.is-amber {
		--tint: #f59e0b;
	}

	&.is-gray {
		--tint: #94a3b8;
	}
}

.mqs-fact-meta {
	display: flex;
	flex-direction: column;
	gap: 5px;
	min-width: 0;
}

.mqs-fact-k {
	font-size: 11.5px;
	line-height: 1.3;
	letter-spacing: 0.2px;
	color: var(--el-text-color-secondary);
}

.mqs-fact-v {
	font-size: 15px;
	font-weight: 700;
	line-height: 1.3;
	font-family: Consolas, Monaco, monospace;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;

	&.is-on {
		color: var(--el-color-success);
	}

	&.is-off {
		color: var(--el-text-color-secondary);
	}
}

/* 自动刷新开关：卡头里的小胶囊，开启时主色描边 + 浅底，与左侧数量 chip 同一套语言 */
.mqs-auto {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	height: 24px;
	padding: 0 10px 0 6px;
	border-radius: 999px;
	font-size: 12px;
	line-height: 1;
	white-space: nowrap;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);
	transition:
		color 0.2s ease,
		background-color 0.2s ease,
		border-color 0.2s ease;

	:deep(.el-switch) {
		--el-switch-on-color: var(--el-color-primary);
		height: 16px;
	}

	&.is-on {
		color: var(--el-color-primary);
		background: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
		border-color: color-mix(in srgb, var(--el-color-primary) 28%, transparent);
	}
}

.mqs-auto-text {
	font-weight: 600;
}

/* ===================== 面板 ===================== */
.mqs-grid {
	display: grid;
	grid-template-columns: minmax(0, 1.7fr) minmax(0, 1fr);
	gap: 14px;
}

.mqs-panel {
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	transition:
		box-shadow 0.25s ease,
		border-color 0.25s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 30%, transparent);
		box-shadow: 0 12px 26px -18px color-mix(in srgb, var(--el-color-primary) 90%, transparent);
	}

	:deep(.el-card__header) {
		padding: 12px 16px;
		border-bottom: 1px solid var(--el-border-color-lighter);
	}

	:deep(.el-card__body) {
		padding: 14px 16px;

		/* 卡片内相邻组件统一 14px 垂直间距（查询区 → 表格 → 分页），
		   不再靠某个组件自己写 margin-bottom，否则顺序一变间距就没了 */
		> * + * {
			margin-top: 14px;
		}
	}
}

.mqs-head {
	display: flex;
	align-items: center;
	gap: 9px;
	min-width: 0;
}

.mqs-head-ico {
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	font-size: 14px;
	/* 装饰规范：浅色同色系底 + 深色图标 */
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-blue {
		--tint: #3b82f6;
	}

	&.is-green {
		--tint: #10b981;
	}

	&.is-orange {
		--tint: #f59e0b;
	}
}

.mqs-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.mqs-head-count {
	padding: 0 8px;
	line-height: 18px;
	border-radius: 999px;
	font-size: 11px;
	font-weight: 700;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.mqs-head-acts {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-left: auto;
	flex-shrink: 0;
}

/* 表格：圆角收口，和卡片风格保持一致 */
.mqs-panel {
	:deep(.vxe-grid) {
		border-radius: 10px;
		overflow: hidden;
	}
}

/* ===================== 查询 / 发布 / 分页 ===================== */
/* 消息记录查询区：条件与按钮同一行水平对齐，控件给固定宽度（flex 行里 el-input 会被压窄） */
.mqs-query {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 0 12px;

	:deep(.el-form-item) {
		margin-bottom: 0;
		margin-right: 0;
	}
}

.mqs-input {
	width: 200px;
}

.mqs-select {
	width: 160px;
}

/* 查询 / 重置 固定为按钮组，紧跟查询条件 */
.mqs-query-acts {
	display: flex;
	align-items: center;
	gap: 8px;

	:deep(.el-button-group) {
		display: inline-flex;
	}
}

.mqs-publish-form {
	:deep(.el-form-item) {
		margin-bottom: 10px;
	}
}

.mqs-publish-form {
	display: flex;
	flex-direction: column;
	height: 100%;

	:deep(.el-textarea__inner) {
		font-family: Consolas, Monaco, monospace;
	}
}

.mqs-publish-ops {
	margin-top: auto;
	margin-bottom: 0 !important;
}

/* 分页与上方表格的间距由卡片体的「相邻组件 14px」规则统一给，这里不要再叠 padding-top */
.mqs-pager {
	display: flex;
	justify-content: flex-end;
}

/* 操作区移入卡头后信息卡独占整行，4 列可以撑到 1200px 再降为 2 列
   （原 1400 断点是为「4 卡 + 操作块」留空间，已不再需要） */
/* ===================== 老内核兜底（Chromium 102 / 奇安信可信浏览器） =====================
 * color-mix() 在这种内核里不是「解析期丢掉声明」，而是让属性在计算值阶段失效、回落到初始值：
 *   · background / background-image → 透明、整条渐变消失（信息卡顶部彩条会整条不见）；
 *   · border-color → currentColor（深色文字色），不是变浅 —— 实测卡片 hover 时边框直接发黑。
 * 所以「先写静态值再写 color-mix」的前置 fallback 不会生效，必须整块 @supports not 隔离。
-------------------------------------------------------------------------------------- */
@supports not (background: color-mix(in srgb, red, blue)) {
	/* 信息卡顶部彩条：渐变里带 color-mix 会让整条 background-image 失效，退回同色系两档色阶 */
	.mqs-fact::before {
		background: linear-gradient(90deg, var(--el-color-primary), var(--el-color-primary-light-3));
	}

	/* 32% / 30% 主色描边 ≈ light-7；阴影里的主色没法折算透明度，退回 light-5 实色 */
	.mqs-fact:hover {
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 16px 30px -20px var(--el-color-primary-light-5);
	}

	.mqs-panel:hover {
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 12px 26px -18px var(--el-color-primary-light-5);
	}

	/* 自动刷新胶囊：10% 底 / 28% 描边 */
	.mqs-auto.is-on {
		background: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-7);
	}

	/* 图标块：底色跟着 --tint 走（蓝 / 绿 / 橙 / 紫 / 灰），静态色阶映射不出来。
	 * 用伪元素铺一层 13% 的同色底 —— 父级建层叠上下文，z-index: -1 把色块压在背景之上、图标之下 */
	.mqs-fact-ico,
	.mqs-head-ico {
		position: relative;
		z-index: 0;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			z-index: -1;
			border-radius: inherit;
			background: var(--tint, var(--el-color-primary));
			opacity: 0.13;
		}
	}

	/* 数量 chip：12% 主色 ≈ light-9 */
	.mqs-head-count {
		background: var(--el-color-primary-light-9);
	}
}

@media (max-width: 1200px) {
	.mqs-facts {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}

	.mqs-grid {
		grid-template-columns: minmax(0, 1fr);
	}
}

@media (max-width: 768px) {
	.mqs-facts {
		grid-template-columns: minmax(0, 1fr);
	}

	.mqs-pager {
		justify-content: center;
	}
}
</style>
