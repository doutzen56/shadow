<template>
	<div class="mqx-page">
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
					<h2 class="page-hero__title">MQTT 客户端</h2>
					<p class="page-hero__desc">在线调试 MQTT 服务：配置连接参数、订阅主题、发布消息，并实时查看服务端回包。</p>
				</div>
				<div class="page-hero__right">
					<div class="mqx-state">
						<span class="mqx-state-pill" :class="{ 'is-on': client.connected }">
							<i class="mqx-state-dot"></i>
							{{ client.connected ? '已连接' : '未连接' }}
						</span>
						<span class="mqx-state-url" :title="connectUrl">{{ connectUrl }}</span>
					</div>
				</div>
			</div>
		</div>

		<el-card shadow="hover" class="mqx-panel">
			<template #header>
				<div class="mqx-head">
					<span class="mqx-head-ico is-blue"><el-icon><Connection /></el-icon></span>
					<span class="mqx-head-title">连接参数</span>
					<div class="mqx-head-acts">
						<el-button :icon="RefreshLeft" plain @click="useCurrentSite" :disabled="connSuccess">当前站点</el-button>
						<el-button :icon="Link" plain @click="useApiSite" :disabled="connSuccess">API地址</el-button>
					</div>
				</div>
			</template>

			<el-form label-position="top" :model="connection" class="mqx-form">
				<el-row :gutter="12">
					<el-col :xs="12" :sm="6" :lg="3">
						<el-form-item label="协议">
							<el-select v-model="connection.protocol" :disabled="connSuccess">
								<el-option label="ws://" value="ws" />
								<el-option label="wss://" value="wss" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :lg="7">
						<el-form-item label="主机">
							<el-input v-model.trim="connection.host" :disabled="connSuccess" clearable placeholder="aiapijima.c" @blur="applyHostInput" />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="6" :lg="3">
						<el-form-item label="端口">
							<el-input v-model.trim="connection.port" :disabled="connSuccess" clearable placeholder="默认" />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="6" :lg="3">
						<el-form-item label="WS路径">
							<el-input v-model.trim="connection.path" :disabled="connSuccess" clearable placeholder="/mqtt" />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="6" :lg="4">
						<el-form-item label="账号">
							<el-input v-model.trim="connection.username" :disabled="connSuccess" clearable placeholder="superadmin" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :lg="4">
						<el-form-item label="密码">
							<el-input v-model="connection.password" :disabled="connSuccess" type="password" show-password clearable placeholder="请输入 MQTT 密码" />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="12" :lg="6">
						<el-form-item label="ClientId">
							<el-input v-model.trim="connection.clientId" :disabled="connSuccess" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="6" :lg="3">
						<el-form-item label="KeepAlive">
							<el-input-number v-model="connection.keepalive" :disabled="connSuccess" :min="0" :max="65535" controls-position="right" />
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="6" :lg="3">
						<el-form-item label="Clean">
							<el-switch v-model="connection.clean" :disabled="connSuccess" inline-prompt active-text="是" inactive-text="否" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :lg="12" class="mqx-actions">
						<el-button type="primary" :icon="Setting" :disabled="client.connected" :loading="btnLoadingType === 'connect'" @click="createConnection"> 连接 </el-button>
						<el-button v-if="client.connected" type="warning" :icon="Discount" :loading="btnLoadingType === 'disconnect'" @click="destroyConnection">断开</el-button>
					</el-col>
				</el-row>
			</el-form>
		</el-card>

		<div class="mqx-grid">
			<el-card shadow="hover" class="mqx-panel">
				<template #header>
					<div class="mqx-head">
						<span class="mqx-head-ico is-green"><el-icon><Connection /></el-icon></span>
						<span class="mqx-head-title">订阅</span>
						<span v-if="subscribedSuccess" class="mqx-head-tag">{{ subscribedTopic }}</span>
					</div>
				</template>

				<el-form label-position="top" :model="subscription" class="mqx-form">
					<el-row :gutter="12">
						<el-col :xs="24" :sm="16">
							<el-form-item label="订阅主题">
								<el-input v-model.trim="connection.subTopic" :disabled="subscribedSuccess" clearable placeholder="demo/test" />
							</el-form-item>
						</el-col>
						<el-col :xs="12" :sm="4">
							<el-form-item label="QoS">
								<el-select v-model="subscription.qos" :disabled="subscribedSuccess">
									<el-option v-for="qos in qosList" :key="qos" :label="getQosLabel(qos)" :value="qos" />
								</el-select>
							</el-form-item>
						</el-col>
						<el-col :xs="12" :sm="4" class="mqx-actions">
							<el-button v-if="!subscribedSuccess" type="primary" :icon="Connection" :loading="btnLoadingType === 'subscribe'" :disabled="!client.connected" @click="doSubscribe"> 订阅 </el-button>
							<el-button v-else type="warning" :icon="Discount" :loading="btnLoadingType === 'unsubscribe'" :disabled="!client.connected" @click="doUnSubscribe"> 取消 </el-button>
						</el-col>
					</el-row>
				</el-form>
			</el-card>

			<el-card shadow="hover" class="mqx-panel">
				<template #header>
					<div class="mqx-head">
						<span class="mqx-head-ico is-orange"><el-icon><Position /></el-icon></span>
						<span class="mqx-head-title">发布</span>
					</div>
				</template>

				<el-form label-position="top" :model="publish" class="mqx-form">
					<el-row :gutter="12">
						<el-col :xs="24" :sm="16">
							<el-form-item label="发布主题">
								<el-input v-model.trim="connection.pubTopic" clearable placeholder="demo/test" />
							</el-form-item>
						</el-col>
						<el-col :xs="12" :sm="4">
							<el-form-item label="QoS">
								<el-select v-model="publish.qos">
									<el-option v-for="qos in qosList" :key="qos" :label="getQosLabel(qos)" :value="qos" />
								</el-select>
							</el-form-item>
						</el-col>
						<el-col :xs="12" :sm="4">
							<el-form-item label="Retain">
								<el-select v-model="publish.retain">
									<el-option :value="false" label="false 不保留" />
									<el-option :value="true" label="true 保留" />
								</el-select>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="20">
							<el-form-item label="消息内容">
								<el-input v-model="publish.payload" type="textarea" :rows="3" resize="none" clearable />
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="4" class="mqx-actions">
							<el-button type="success" :icon="Position" :loading="btnLoadingType === 'publish'" :disabled="!client.connected" @click="doPublish"> 发布 </el-button>
						</el-col>
					</el-row>
				</el-form>
			</el-card>
		</div>

		<el-card shadow="hover" class="mqx-panel">
			<template #header>
				<div class="mqx-head">
					<span class="mqx-head-ico is-purple"><el-icon><ChatDotRound /></el-icon></span>
					<span class="mqx-head-title">接收消息</span>
					<span class="mqx-head-count">{{ logCount }}</span>
					<div class="mqx-head-acts">
						<el-button @click="clearMessages" plain :icon="Delete">清空</el-button>
					</div>
				</div>
			</template>

			<el-alert v-if="!client.connected" title="MQTT 服务未连接" type="warning" effect="light" :closable="false" class="mqx-alert" />
			<el-input id="recv" v-model="receivedMessages" type="textarea" :rows="12" resize="vertical" readonly class="mqx-log" />
		</el-card>
	</div>
</template>

<script setup lang="ts" name="mqttx">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { ChatDotRound, Connection, Delete, Discount, Link, Position, RefreshLeft, Setting } from '@element-plus/icons-vue';
import * as MQTT from 'mqtt';

type MqttProtocol = 'ws' | 'wss';

interface EndpointSettings {
	protocol: MqttProtocol;
	host: string;
	port: string;
	path: string;
}

interface StoredSettings extends Partial<EndpointSettings> {
	username?: string;
	keepalive?: number;
	clean?: boolean;
	reconnectPeriod?: number;
}

interface ConnectionSettings extends EndpointSettings {
	clientId: string;
	username: string;
	password: string;
	keepalive: number;
	clean: boolean;
	connectTimeout: number;
	reconnectPeriod: number;
	resubscribe: boolean;
	subTopic: string;
	pubTopic: string;
}

const STORAGE_KEY = 'admin-net-mqttx-settings';
const DEFAULT_WS_PATH = '/mqtt';
const TEST_TOPIC = 'demo/test';
const qosList = [0, 1, 2] as MQTT.QoS[];

const client = ref({ connected: false } as MQTT.MqttClient);
const receivedMessages = ref('');
const subscribedSuccess = ref(false);
const subscribedTopic = ref('');
const connSuccess = ref(false);
const btnLoadingType = ref('');
const retryTimes = ref(0);

type MqttModule = typeof MQTT & {
	default?:
		| {
				connect?: typeof MQTT.connect;
		  }
		| typeof MQTT.connect;
};

let cachedMqttConnect: typeof MQTT.connect | null = null;

const getMqttConnect = () => {
	if (cachedMqttConnect) return cachedMqttConnect;

	const mqttModule = MQTT as MqttModule;
	const defaultExport = mqttModule.default;
	const candidate = mqttModule.connect ?? (typeof defaultExport === 'function' ? defaultExport : defaultExport?.connect);

	if (typeof candidate !== 'function') throw new Error('mqtt.connect is not available');

	cachedMqttConnect = candidate;
	return cachedMqttConnect;
};

const formatNow = () => new Date().toLocaleString();

const normalizePath = (path?: string) => {
	const value = (path || DEFAULT_WS_PATH).trim();
	return value.startsWith('/') ? value : `/${value}`;
};

const getEndpointFromUrl = (url?: string): EndpointSettings => {
	const fallback = typeof window === 'undefined' ? 'http://localhost' : window.location.origin;
	const raw = (url || '').trim() || fallback;

	try {
		const parsed = new URL(raw, fallback);
		return {
			protocol: parsed.protocol === 'https:' || parsed.protocol === 'wss:' ? 'wss' : 'ws',
			host: parsed.hostname || (typeof window === 'undefined' ? 'localhost' : window.location.hostname),
			port: parsed.port,
			path: parsed.pathname && parsed.pathname !== '/' ? normalizePath(parsed.pathname) : DEFAULT_WS_PATH,
		};
	} catch {
		return {
			protocol: typeof window !== 'undefined' && window.location.protocol === 'https:' ? 'wss' : 'ws',
			host: typeof window === 'undefined' ? 'localhost' : window.location.hostname,
			port: typeof window === 'undefined' ? '' : window.location.port,
			path: DEFAULT_WS_PATH,
		};
	}
};

const getDefaultEndpoint = () => {
	const apiUrl = typeof window === 'undefined' ? '' : ((window as any).__env__?.VITE_API_URL as string | undefined);
	return getEndpointFromUrl(apiUrl);
};

const readStoredSettings = (): StoredSettings => {
	if (typeof window === 'undefined') return {};
	try {
		return JSON.parse(window.localStorage.getItem(STORAGE_KEY) || '{}') as StoredSettings;
	} catch {
		return {};
	}
};

const defaultEndpoint = getDefaultEndpoint();
const storedSettings = readStoredSettings();

const connection = reactive<ConnectionSettings>({
	protocol: storedSettings.protocol || defaultEndpoint.protocol,
	host: storedSettings.host || defaultEndpoint.host,
	port: storedSettings.port ?? defaultEndpoint.port,
	path: storedSettings.path || defaultEndpoint.path,
	clientId: `admin_net_mqttx_${Math.random().toString(16).slice(2, 8)}`,
	username: storedSettings.username || 'superadmin',
	password: '',
	keepalive: storedSettings.keepalive ?? 30,
	clean: storedSettings.clean ?? true,
	connectTimeout: 30 * 1000,
	reconnectPeriod: storedSettings.reconnectPeriod ?? 5000,
	resubscribe: true,
	subTopic: TEST_TOPIC,
	pubTopic: TEST_TOPIC,
});

const subscription = reactive({
	qos: 0 as MQTT.QoS,
});

const publish = reactive({
	qos: 0 as MQTT.QoS,
	retain: false,
	payload: 'hello mqtt',
});

const connectUrl = computed(() => {
	const host = connection.host.trim();
	const port = connection.port.trim();
	const portPart = port ? `:${port.replace(/^:/, '')}` : '';
	return `${connection.protocol}://${host}${portPart}${normalizePath(connection.path)}`;
});

// 接收区按行统计消息条数，空行不计
const logCount = computed(() => receivedMessages.value.split('\n').filter((u) => u.trim()).length);

const getQosLabel = (qos: MQTT.QoS) => {
	if (qos === 0) return '0 至多一次';
	if (qos === 1) return '1 至少一次';
	return '2 仅仅一次';
};

const persistConnectionSettings = () => {
	if (typeof window === 'undefined') return;
	const safeSettings: StoredSettings = {
		protocol: connection.protocol,
		host: connection.host,
		port: connection.port,
		path: connection.path,
		username: connection.username,
		keepalive: connection.keepalive,
		clean: connection.clean,
		reconnectPeriod: connection.reconnectPeriod,
	};
	window.localStorage.setItem(STORAGE_KEY, JSON.stringify(safeSettings));
};

const applyEndpoint = (endpoint: EndpointSettings) => {
	connection.protocol = endpoint.protocol;
	connection.host = endpoint.host;
	connection.port = endpoint.port;
	connection.path = endpoint.path || DEFAULT_WS_PATH;
};

const useCurrentSite = () => {
	applyEndpoint(getEndpointFromUrl(typeof window === 'undefined' ? '' : window.location.origin));
};

const useApiSite = () => {
	const apiUrl = typeof window === 'undefined' ? '' : ((window as any).__env__?.VITE_API_URL as string | undefined);
	if (!apiUrl) {
		useCurrentSite();
		ElMessage.info('未读取到 API 地址，已使用当前站点');
		return;
	}
	applyEndpoint(getEndpointFromUrl(apiUrl));
};

const applyHostInput = () => {
	const value = connection.host.trim();
	if (!value) return;
	const candidate = value.includes('://') ? value : `${connection.protocol}://${value}`;
	try {
		const parsed = new URL(candidate);
		const protocol = parsed.protocol.replace(':', '');
		if (protocol === 'wss' || protocol === 'https' || protocol === 'mqtts') connection.protocol = 'wss';
		if (protocol === 'ws' || protocol === 'http' || protocol === 'mqtt') connection.protocol = 'ws';
		connection.host = parsed.hostname;
		if (parsed.port) connection.port = parsed.port;
		if (parsed.pathname && parsed.pathname !== '/') connection.path = normalizePath(parsed.pathname);
	} catch {
		connection.host = value.replace(/^\/+|\/+$/g, '');
	}
};

const resetClientState = () => {
	client.value = {
		connected: false,
	} as MQTT.MqttClient;
	retryTimes.value = 0;
	btnLoadingType.value = '';
	subscribedSuccess.value = false;
	subscribedTopic.value = '';
	connSuccess.value = false;
};

const syncBottom = () => {
	const textarea = document.querySelector<HTMLTextAreaElement>('#recv textarea');
	if (textarea) textarea.scrollTop = textarea.scrollHeight;
};

const appendMessage = (msg: string) => {
	receivedMessages.value = receivedMessages.value.concat(`${msg}\r\n`);
	nextTick(syncBottom);
};

const handleOnReConnect = () => {
	retryTimes.value++;
	appendMessage(`${formatNow()}|正在重连：${retryTimes.value}`);
	if (retryTimes.value > 5) {
		try {
			client.value.end();
			resetClientState();
			appendMessage(`${formatNow()}|超出重连次数，停止重试`);
		} catch (error) {
		}
	}
};

const createConnection = () => {
	applyHostInput();
	if (!connection.host) {
		ElMessage.warning('请填写 MQTT WebSocket 主机');
		return;
	}
	if (!connection.username || !connection.password) {
		ElMessage.warning('请填写 MQTT 账号和密码');
		return;
	}

	try {
		btnLoadingType.value = 'connect';
		persistConnectionSettings();
		const mqttConnect = getMqttConnect();
		const options: MQTT.IClientOptions = {
			clientId: connection.clientId,
			username: connection.username,
			password: connection.password,
			keepalive: Number(connection.keepalive) || 0,
			clean: connection.clean,
			connectTimeout: Number(connection.connectTimeout) || 30 * 1000,
			reconnectPeriod: Number(connection.reconnectPeriod) || 5000,
			resubscribe: connection.resubscribe,
		};
		client.value = mqttConnect(connectUrl.value, options);

		client.value.on('connect', () => {
			btnLoadingType.value = '';
			connSuccess.value = true;
			retryTimes.value = 0;
			appendMessage(`${formatNow()}|连接服务成功`);
			ElMessage.success('MQTT 服务连接成功');
		});

		client.value.on('reconnect', handleOnReConnect);

		client.value.on('close', () => {
			if (btnLoadingType.value !== 'disconnect') {
				connSuccess.value = false;
				subscribedSuccess.value = false;
				subscribedTopic.value = '';
			}
		});

		client.value.on('offline', () => {
			connSuccess.value = false;
			subscribedSuccess.value = false;
			subscribedTopic.value = '';
			appendMessage(`${formatNow()}|连接已离线`);
		});

		client.value.on('error', (error) => {
			btnLoadingType.value = '';
			appendMessage(`${formatNow()}|连接错误：${error.message || error}`);
			ElMessage.error('连接失败，请检查 WebSocket 地址和账号密码');
		});

		client.value.on('message', (topic: string, message) => {
			appendMessage(`${formatNow()}|收到消息：${topic}\r\n${message.toString()}`);
		});
	} catch (error) {
		btnLoadingType.value = '';
		ElMessage.error('MQTT 连接初始化失败');
	}
};

const doSubscribe = () => {
	if (!client.value.connected) {
		ElMessage.warning('请先连接 MQTT 服务');
		return;
	}
	const topic = connection.subTopic.trim();
	if (!topic) {
		ElMessage.warning('请填写订阅主题');
		return;
	}
	btnLoadingType.value = 'subscribe';
	client.value.subscribe(topic, { qos: subscription.qos }, (error: Error, granted: MQTT.ISubscriptionGrant[]) => {
		btnLoadingType.value = '';
		if (error) {
			ElMessage.error('订阅失败');
			return;
		}
		subscribedSuccess.value = true;
		subscribedTopic.value = topic;
		appendMessage(`${formatNow()}|订阅成功：${granted.map((item) => item.topic).join(',')}`);
	});
};

const doUnSubscribe = () => {
	const topic = subscribedTopic.value || connection.subTopic.trim();
	if (!topic) {
		ElMessage.warning('请填写订阅主题');
		return;
	}
	btnLoadingType.value = 'unsubscribe';
	client.value.unsubscribe(topic, (error) => {
		btnLoadingType.value = '';
		subscribedSuccess.value = false;
		subscribedTopic.value = '';
		if (error) {
			ElMessage.error('取消订阅失败');
			return;
		}
		appendMessage(`${formatNow()}|已取消订阅：${topic}`);
	});
};

const doPublish = () => {
	if (!client.value.connected) {
		ElMessage.warning('请先连接 MQTT 服务');
		return;
	}
	const topic = connection.pubTopic.trim();
	if (!topic) {
		ElMessage.warning('请填写发布主题');
		return;
	}
	btnLoadingType.value = 'publish';
	client.value.publish(topic, publish.payload, { qos: publish.qos, retain: publish.retain }, (error) => {
		btnLoadingType.value = '';
		if (error) {
			appendMessage(`${formatNow()}|发布消息错误：${error}`);
			ElMessage.error('发布失败');
			return;
		}
		appendMessage(`${formatNow()}|发布成功：${topic}\r\n${publish.payload}`);
	});
};

const destroyConnection = () => {
	if (!client.value.connected && !connSuccess.value) return;
	btnLoadingType.value = 'disconnect';
	try {
		client.value.end(false, () => {
			resetClientState();
			appendMessage(`${formatNow()}|连接已断开`);
		});
	} catch (error) {
		btnLoadingType.value = '';
	}
};

const clearMessages = () => {
	receivedMessages.value = '';
};

onMounted(() => {
	nextTick(syncBottom);
});

onBeforeUnmount(() => {
	if (client.value.connected) client.value.end(true);
});
</script>

<style lang="scss" scoped>
.mqx-page {
	/* 页边距统一由 .layout-parent 的 padding:5px 提供（与打印模板页一致），这里不再叠加 */
	position: relative;
	padding-bottom: 4px;
	background: var(--el-bg-color);
	min-height: 100%;

	/* 页面内各块统一 14px 上下间距。之前靠面板自己的 margin-bottom 撑开，
	   结果「订阅/发布网格 → 接收消息面板」这一段是 0（网格没有 margin），必须显式给每个块 margin-top */
	> .mqx-panel,
	> .mqx-grid {
		margin-top: 14px;
	}
}

/* ===================== 头图右侧的连接状态 ===================== */
.mqx-state {
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	gap: 6px;
	min-width: 0;
}

.mqx-state-pill {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 4px 12px;
	border-radius: 999px;
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
	background: color-mix(in srgb, var(--el-color-white) 18%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-white) 34%, transparent);

	&.is-on {
		background: color-mix(in srgb, #22c55e 34%, transparent);
		border-color: color-mix(in srgb, var(--el-color-white) 45%, transparent);
	}
}

.mqx-state-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: var(--el-color-white);
	box-shadow: 0 0 0 3px color-mix(in srgb, var(--el-color-white) 25%, transparent);
}

.mqx-state-pill.is-on .mqx-state-dot {
	animation: mqxPulse 1.6s ease-in-out infinite;
}

.mqx-state-url {
	max-width: 320px;
	font-family: Consolas, Monaco, monospace;
	font-size: 11px;
	line-height: 1.4;
	opacity: 0.85;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* ===================== 面板 ===================== */
.mqx-grid {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 14px;
}

.mqx-panel {
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

		/* 卡片内相邻组件统一 14px 垂直间距（提示条 → 输入区），
		   靠各自的 margin-bottom 会在条件渲染时丢失 */
		> * + * {
			margin-top: 14px;
		}
	}
}

.mqx-head {
	display: flex;
	align-items: center;
	gap: 9px;
	min-width: 0;
}

.mqx-head-ico {
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

	&.is-purple {
		--tint: #8b5cf6;
	}
}

.mqx-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.mqx-head-count {
	padding: 0 8px;
	line-height: 18px;
	border-radius: 999px;
	font-size: 11px;
	font-weight: 700;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.mqx-head-tag {
	max-width: 220px;
	padding: 0 8px;
	line-height: 18px;
	border-radius: 5px;
	font-size: 11px;
	font-family: Consolas, Monaco, monospace;
	color: var(--el-color-success);
	background: color-mix(in srgb, var(--el-color-success) 14%, transparent);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.mqx-head-acts {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-left: auto;
	flex-shrink: 0;
}

/* ===================== 表单 ===================== */
.mqx-form {
	:deep(.el-form-item) {
		margin-bottom: 10px;
	}

	:deep(.el-form-item__label) {
		padding-bottom: 4px;
		font-size: 12px;
		line-height: 20px;
		color: var(--el-text-color-secondary);
	}
}

.mqx-actions {
	display: flex;
	align-items: flex-end;
	justify-content: flex-end;
	gap: 8px;
	padding-bottom: 10px;
}

/* 与下方输入区的间距由卡片体「相邻组件 14px」规则统一给 */
.mqx-alert {
	margin-bottom: 0;
}

.mqx-log {
	:deep(.el-textarea__inner) {
		min-height: 260px;
		font-family: Consolas, Monaco, monospace;
		font-size: 13px;
		line-height: 20px;
		color: #d8dee9;
		background: #1f2933;
		border-color: #334155;
	}
}

/* ===================== 老内核兜底（Chromium 102 / 奇安信可信浏览器） =====================
 * color-mix() 在这种内核里不是「解析期丢掉声明」，而是让属性在计算值阶段失效、回落到初始值：
 *   · background / background-image → 透明、整条渐变消失；
 *   · border-color → currentColor（深色文字色），不是变浅 —— 实测面板 hover 时边框直接发黑。
 * 所以「先写静态值再写 color-mix」的前置 fallback 不会生效，必须整块 @supports not 隔离。
-------------------------------------------------------------------------------------- */
@supports not (background: color-mix(in srgb, red, blue)) {
	/* 面板卡片：30% 主色描边 ≈ light-7（实测新内核计算值与 light-7 重合）。
	 * 阴影里的主色没法折算成透明度，退回 light-5 实色 —— 视觉上仍是「带主色的浮起」 */
	.mqx-panel:hover {
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 12px 26px -18px var(--el-color-primary-light-5);
	}

	/* 头图里的连接状态胶囊：白 18% 底 / 白 34% 描边，直接写成带透明度的白 */
	.mqx-state-pill {
		background: rgb(255 255 255 / 18%);
		border-color: rgb(255 255 255 / 34%);
	}

	.mqx-state-pill.is-on {
		background: rgb(34 197 94 / 34%);
		border-color: rgb(255 255 255 / 45%);
	}

	.mqx-state-dot {
		box-shadow: 0 0 0 3px rgb(255 255 255 / 25%);
	}

	/* 卡头小图标块：底色跟着 --tint 走（蓝 / 绿 / 橙 / 紫），静态色阶映射不出来。
	 * 用伪元素铺一层 13% 的同色底 —— 父级建层叠上下文，z-index: -1 把色块压在背景之上、图标之下 */
	.mqx-head-ico {
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

	/* 数量 chip（12% 主色）与状态标签（14% 成功色）≈ light-9 */
	.mqx-head-count {
		background: var(--el-color-primary-light-9);
	}

	.mqx-head-tag {
		background: var(--el-color-success-light-9);
	}
}

@keyframes mqxPulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.45;
		transform: scale(1.35);
	}
}

@media (max-width: 1200px) {
	.mqx-grid {
		grid-template-columns: minmax(0, 1fr);
	}
}

@media (max-width: 768px) {
	.mqx-head,
	.mqx-head-acts {
		width: 100%;
		margin-left: 0;
	}

	.mqx-actions {
		justify-content: flex-start;
		padding-bottom: 0;
	}

	.mqx-state {
		align-items: flex-start;
	}
}
</style>
