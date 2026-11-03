/**
 * 阿里云实时语音识别（NLS）前端模块
 *
 * 麦克风 PCM 音频流经 WebSocket 直传阿里云做实时转写，识别文本回填聊天输入框；
 * 服务端只签发临时 Token（/api/agentAsr/getToken），不中转音频流，避免带宽与延迟成本。
 * 协议参考：https://help.aliyun.com/document_detail/457202.html（实时语音识别 WebSocket）
 */

// 阿里云实时语音识别 WebSocket 网关（上海）
const NLS_GATEWAY = 'wss://nls-gateway.cn-shanghai.aliyuncs.com/ws/v1';

// 音频参数：NLS 实时识别要求 16kHz 单声道 PCM
const SAMPLE_RATE = 16000;
// 每 200ms 合并队列里攒到的音频块发送一次，兼顾实时性与帧数开销
const CHUNK_INTERVAL_MS = 200;

export type AsrState = 'idle' | 'connecting' | 'listening' | 'error' | 'stopped';

export interface AliAsrOptions {
	/** NLS AppKey（服务端签发） */
	appKey: string;
	/** NLS 临时 Token（服务端签发，24h 有效） */
	token: string;
	/** 中间识别结果（尚未停顿），isFinal=false */
	onResult: (text: string, isFinal: boolean) => void;
	onError?: (message: string) => void;
	onStateChange?: (state: AsrState) => void;
}

/**
 * 创建阿里云实时 ASR 实例
 */
export function createAliAsr(options: AliAsrOptions) {
	const { onResult, onError, onStateChange, appKey, token } = options;

	let ws: WebSocket | null = null;
	let audioContext: AudioContext | null = null;
	let mediaStream: MediaStream | null = null;
	let mediaSource: MediaStreamAudioSourceNode | null = null;
	// ScriptProcessor 虽被标记 deprecated，但兼容性最好（含老旧内核），AI 场景音频处理逻辑极简，风险可控
	let processor: ScriptProcessorNode | null = null;
	let chunkTimer: ReturnType<typeof setInterval> | null = null;
	let audioQueue: ArrayBuffer[] = [];
	let taskId = '';
	let isListening = false;
	let isConnected = false;
	// 累积的最终文本：SentenceEnd 逐句产出，中间结果只覆盖当前句
	let finalText = '';

	/** NLS 要求 UUID 去掉横线（32 位十六进制字符） */
	function genUuid(): string {
		return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
			const r = (Math.random() * 16) | 0;
			const v = c === 'x' ? r : (r & 0x3) | 0x8;
			return v.toString(16);
		});
	}

	function setState(state: AsrState) {
		isListening = state === 'listening';
		onStateChange?.(state);
	}

	function connect() {
		const url = `${NLS_GATEWAY}?appkey=${appKey}&token=${token}`;
		ws = new WebSocket(url);
		ws.binaryType = 'arraybuffer';

		ws.onopen = () => {
			isConnected = true;
			ws?.send(
				JSON.stringify({
					header: {
						message_id: genUuid(),
						task_id: taskId,
						namespace: 'SpeechTranscriber',
						name: 'StartTranscription',
						appkey: appKey,
					},
					payload: {
						format: 'pcm',
						sample_rate: SAMPLE_RATE,
						enable_intermediate_result: true,
						enable_punctuation_prediction: true,
						enable_inverse_text_normalization: true,
					},
				})
			);
		};

		ws.onmessage = (event) => {
			try {
				handleServerMessage(JSON.parse(event.data));
			} catch {
				// 非 JSON 帧忽略（心跳等）
			}
		};

		ws.onerror = () => {
			onError?.('语音识别连接异常');
			setState('error');
			cleanup();
		};

		ws.onclose = () => {
			isConnected = false;
			setState('stopped');
		};
	}

	function handleServerMessage(msg: any) {
		const header = msg?.header;
		if (!header) return;

		const name = header.name as string;
		// NLS 状态码：20000000 表示成功
		const statusCode = header.status || header.status_code;

		// 服务端确认启动 → 开始采集音频
		if (name === 'TranscriptionStarted') {
			if (statusCode !== 20000000) {
				onError?.(header.status_text || '语音识别启动失败');
				setState('error');
				cleanup();
				return;
			}
			setState('listening');
			startAudioCapture();
			return;
		}

		// 中间结果：当前句尚未说完，只做预览展示
		if (name === 'TranscriptionResultChanged') {
			if (msg.payload?.result) onResult(finalText + msg.payload.result, false);
			return;
		}

		// 单句结束：固化到累积文本
		if (name === 'SentenceEnd') {
			if (msg.payload?.result) {
				finalText += msg.payload.result;
				onResult(finalText, false);
			}
			return;
		}

		// 任务完成：给出最终文本
		if (name === 'TranscriptionCompleted') {
			if (msg.payload?.result) onResult(finalText + msg.payload.result, true);
			else onResult(finalText, true);
			return;
		}

		// 任务失败
		if (name === 'TaskFailed') {
			onError?.(header.status_text || '语音识别任务失败');
			setState('error');
			cleanup();
		}
	}

	async function startAudioCapture() {
		try {
			mediaStream = await navigator.mediaDevices.getUserMedia({
				audio: {
					sampleRate: SAMPLE_RATE,
					channelCount: 1,
					echoCancellation: true,
					noiseSuppression: true,
				},
			});

			audioContext = new AudioContext({ sampleRate: SAMPLE_RATE });
			// 非用户手势创建的 AudioContext 处于 suspended 状态，必须 resume 后才会触发采集
			if (audioContext.state === 'suspended') {
				await audioContext.resume();
			}
			mediaSource = audioContext.createMediaStreamSource(mediaStream);
			processor = audioContext.createScriptProcessor(4096, 1, 1);

			processor.onaudioprocess = (event) => {
				if (!isConnected) return;
				const inputData = event.inputBuffer.getChannelData(0);
				audioQueue.push(float32ToInt16(inputData));
			};

			mediaSource.connect(processor);
			processor.connect(audioContext.destination);

			chunkTimer = setInterval(sendAudioChunk, CHUNK_INTERVAL_MS);
		} catch {
			onError?.('无法访问麦克风，请检查浏览器权限');
			setState('error');
			cleanup();
		}
	}

	function sendAudioChunk() {
		if (!ws || !isConnected || audioQueue.length === 0) return;

		const totalLen = audioQueue.reduce((sum, buf) => sum + buf.byteLength, 0);
		const merged = new Uint8Array(totalLen);
		let offset = 0;
		for (const buf of audioQueue) {
			merged.set(new Uint8Array(buf), offset);
			offset += buf.byteLength;
		}
		audioQueue = [];

		// NLS 二进制帧直接承载裸 PCM 数据
		ws.send(merged.buffer);
	}

	function float32ToInt16(float32Array: Float32Array): ArrayBuffer {
		const len = float32Array.length;
		const int16Array = new Int16Array(len);
		for (let i = 0; i < len; i++) {
			const s = Math.max(-1, Math.min(1, float32Array[i]));
			int16Array[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
		}
		return int16Array.buffer;
	}

	function cleanup() {
		if (chunkTimer) {
			clearInterval(chunkTimer);
			chunkTimer = null;
		}
		audioQueue = [];

		processor?.disconnect();
		processor = null;
		mediaSource?.disconnect();
		mediaSource = null;
		if (audioContext) {
			audioContext.close().catch(() => {});
			audioContext = null;
		}
		mediaStream?.getTracks().forEach((t) => t.stop());
		mediaStream = null;

		if (ws) {
			ws.onopen = null;
			ws.onmessage = null;
			ws.onerror = null;
			ws.onclose = null;
			if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
				ws.close();
			}
			ws = null;
		}
	}

	async function start() {
		if (isListening) return;
		taskId = genUuid();
		finalText = '';
		setState('connecting');
		connect();
	}

	function stop() {
		if (ws && isConnected) {
			try {
				ws.send(
					JSON.stringify({
						header: {
							message_id: genUuid(),
							task_id: taskId,
							namespace: 'SpeechTranscriber',
							name: 'StopTranscription',
							appkey: appKey,
						},
					})
				);
			} catch {
				// 已断开时忽略
			}
		}
		cleanup();
		setState('stopped');
	}

	return { start, stop, get isListening() { return isListening; } };
}
