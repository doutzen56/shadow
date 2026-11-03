import request from '/@/utils/request';

export interface MqttStatusOutput {
	enabled: boolean;
	started: boolean;
	port: number;
	webSocketPath: string;
	mqttServerId: string;
	logging: boolean;
	clientCount: number;
	messageCount: number;
	startedTime?: string;
}

export interface MqttClientStatus {
	id?: string;
	endpoint?: string;
	remoteEndPoint?: string;
	connectedTimestamp?: string;
	lastNonKeepAlivePacketReceivedTimestamp?: string;
	lastPacketReceivedTimestamp?: string;
	lastPacketSentTimestamp?: string;
	bytesReceived?: number;
	bytesSent?: number;
	receivedApplicationMessagesCount?: number;
	sentApplicationMessagesCount?: number;
	receivedPacketsCount?: number;
	sentPacketsCount?: number;
	protocolVersion?: string | number;
	session?: {
		id?: string;
		createdTimestamp?: string;
		disconnectedTimestamp?: string;
		pendingApplicationMessagesCount?: number;
		expiryInterval?: number;
	};
}

export interface MqttMessageRecord {
	id: number;
	time: string;
	eventType: string;
	clientId?: string;
	userName?: string;
	remoteEndPoint?: string;
	topic?: string;
	payload?: string;
	payloadLength: number;
	qualityOfServiceLevel?: string;
	retain: boolean;
	remark?: string;
}

export interface MqttMessagePageInput {
	page: number;
	pageSize: number;
	clientId?: string;
	userName?: string;
	topic?: string;
	eventType?: string;
	startTime?: string;
	endTime?: string;
}

export interface MqttPageResult<T> {
	page: number;
	pageSize: number;
	total: number;
	totalPages: number;
	items: T[];
	hasPrevPage: boolean;
	hasNextPage: boolean;
}

enum Api {
	Status = '/api/sysMqtt/status',
	Clients = '/api/sysMqtt/clients',
	PublicMessage = '/api/sysMqtt/publicMessage',
	MessagePage = '/api/sysMqtt/messagePage',
	ClearMessages = '/api/sysMqtt/clearMessages',
}

export const getMqttStatus = () =>
	request({
		url: Api.Status,
		method: 'get',
	});

export const getMqttClients = () =>
	request({
		url: Api.Clients,
		method: 'get',
	});

export const publishMqttMessage = (data: { topic: string; message: string }) =>
	request({
		url: Api.PublicMessage,
		method: 'post',
		data,
	});

export const getMqttMessagePage = (data: MqttMessagePageInput) =>
	request({
		url: Api.MessagePage,
		method: 'post',
		data,
	});

export const clearMqttMessages = () =>
	request({
		url: Api.ClearMessages,
		method: 'post',
	});
