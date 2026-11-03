<template>
	<div class="chat-panel">
		<!-- 未选择会话时的引导态 -->
		<div v-if="!props.user" class="chat-panel__blank">
			<!-- 小屏没有列表栏可点，必须给个返回出口，否则进来就出不去 -->
			<div v-if="props.mobile" class="chat-panel__backbar">
				<el-button text :icon="ArrowLeft" @click="emit('back')">返回</el-button>
			</div>
			<el-empty description="从左侧选择一位用户，开始聊天" :image-size="120" />
		</div>

		<template v-else>
			<div class="chat-panel__header">
				<!-- 小屏返回键：回到用户列表 -->
				<el-button v-if="props.mobile" class="chat-panel__back" text :icon="ArrowLeft" @click="emit('back')" />
				<div class="chat-panel__avatar">
					<el-avatar :size="40" :src="props.user.avatar || ''" class="chat-avatar">{{ avatarText(props.user) }}</el-avatar>
					<i class="chat-dot" :class="{ 'is-online': props.user.online }"></i>
				</div>
				<div class="chat-panel__who">
					<div class="chat-panel__name">
						<b>{{ displayName(props.user) }}</b>
						<el-tag v-if="props.user.isMe" size="small" type="warning" effect="light" round>我</el-tag>
						<el-tag size="small" :type="props.user.online ? 'success' : 'info'" effect="light" round>
							{{ props.user.online ? '在线' : '离线' }}
						</el-tag>
					</div>
					<div class="chat-panel__sub">
						<span>{{ props.user.userName || '—' }}</span>
						<span v-if="props.user.ip">{{ props.user.ip }}</span>
						<span v-if="props.user.browser">{{ props.user.browser }}</span>
					</div>
				</div>
				<div class="chat-panel__acts">
					<el-tooltip v-if="!props.user.isMe" content="发送正式站内信（带标题与类型）" placement="bottom">
						<!-- 小屏只留图标：带文字会把姓名挤出可视区 -->
						<el-button text type="primary" icon="ele-Edit" @click="emit('openNotice')">
							<span v-if="!props.mobile">站内信</span>
						</el-button>
					</el-tooltip>
					<el-tooltip v-if="!props.user.isMe" content="清空与该用户的聊天记录（仅自己参与的）" placement="bottom">
						<el-button text type="danger" icon="ele-Delete" @click="emit('clearConversation')">
							<span v-if="!props.mobile">清空</span>
						</el-button>
					</el-tooltip>
				</div>
			</div>

			<div class="chat-panel__body">
				<!-- 滚动容器自己持有：不再依赖组件内部结构，贴底判断与跟随都直接量这个 div -->
				<div ref="scrollRef" class="chat-panel__scroll" @scroll.passive="onListScroll">
					<div v-if="!bubbleList.length" class="chat-panel__hint">
						<template v-if="props.loadFailed">
							聊天记录加载失败，<a class="chat-panel__retry" @click="emit('retry')">点击重试</a>
						</template>
						<template v-else>还没有聊天记录，打个招呼吧 👋</template>
					</div>
					<template v-else>
						<template v-for="item in bubbleList" :key="item.key">
							<!-- 换天时插一条居中的日期分隔，商业聊天软件的标准分隔方式 -->
							<div v-if="item.dayText" class="chat-panel__day"><span>{{ item.dayText }}</span></div>
							<!-- 名字/时间单独成行：TinyRobot 的 prefix 插槽是气泡左侧的 flex 项，放在这里会把气泡挤走 -->
							<div class="chat-panel__meta" :class="{ 'is-mine': item.placement === 'end' }">
								<span>{{ item.senderName }}</span>
								<span>{{ item.timeText }}</span>
							</div>
							<TrBubble
								:id="item.key"
								:class="{ 'is-media': item.media }"
								:content="item.content"
								:avatar="item.avatar"
								:placement="item.placement"
								shape="corner"
								:fallback-content-renderer="ChatContentRenderer"
							/>
						</template>
					</template>
				</div>

				<!-- 上滑看历史时给一个回到底部的出口，不用手动拖滚动条 -->
				<transition name="chat-jump">
					<button v-if="showJump" type="button" class="chat-panel__jump" title="回到最新消息" @click="pinToBottom(true)">
						<el-icon :size="16"><ArrowDown /></el-icon>
						<span>回到最新</span>
					</button>
				</transition>
			</div>

			<!-- 自己不给自己发消息：直接给一句说明，比留一个发不出去的输入框清楚 -->
			<div v-if="props.user.isMe" class="chat-panel__selftip">
				<el-icon :size="14"><InfoFilled /></el-icon>
				<span>这是你自己，这里可以看到你的在线状态与连接信息</span>
			</div>

			<!-- paste 监听绑在自己这层容器上：粘贴事件会从编辑器冒泡上来，不用追踪组件内部 DOM -->
			<div v-else class="chat-panel__footer" @paste="onPasteNative">
				<TrSender
					ref="senderRef"
					v-model="draft"
					mode="multiple"
					submit-type="enter"
					clearable
					:loading="props.sending"
					:auto-size="{ minRows: 1, maxRows: 4 }"
					placeholder="输入消息，回车发送，Shift + 回车换行（可粘贴图片或文件）"
					@submit="onSubmit"
				>
					<!-- 底部一行（footer 插槽）：左侧工具栏，右侧是组件自带的发送按钮 -->
					<template #footer>
						<div class="chat-panel__tools">
							<el-popover v-model:visible="emojiVisible" placement="top-start" :width="props.mobile ? 250 : 286" trigger="click" popper-class="chat-emoji-popper">
								<template #reference>
									<!-- 开关交给 popover 自己的 click 触发，按钮上再绑一次会 toggle 两次相互抵消；
									     这里也不再套 el-tooltip：popover 的 reference 取不到内层组件的真实 DOM -->
									<button type="button" class="chat-panel__tool" title="表情">😊</button>
								</template>
								<div class="chat-emoji">
									<span v-for="e in EMOJIS" :key="e" class="chat-emoji__item" @click="insertEmoji(e)">{{ e }}</span>
								</div>
							</el-popover>

							<el-tooltip content="发送图片" placement="top">
								<button type="button" class="chat-panel__tool" :disabled="uploading" @click="imageRef?.click()">
									<el-icon :size="17"><PictureFilled /></el-icon>
								</button>
							</el-tooltip>

							<el-tooltip content="发送文件" placement="top">
								<button type="button" class="chat-panel__tool" :disabled="uploading" @click="fileRef?.click()">
									<el-icon :size="17"><Files /></el-icon>
								</button>
							</el-tooltip>

							<span v-if="uploading" class="chat-panel__uploading">
								<el-icon class="is-loading"><Loading /></el-icon>
								上传中
							</span>

							<!-- 可选类型与后端 AllowSuffix 保持一致，避免选完才被后端拒 -->
							<input ref="imageRef" type="file" accept="image/jpeg,image/png,image/bmp,image/gif" hidden @change="sendPickedFile(imageRef)" />
							<input ref="fileRef" type="file" :accept="FILE_ACCEPT" hidden @change="sendPickedFile(fileRef)" />
						</div>
					</template>

					<!-- 空草稿时右侧给一句快捷键提示：否则工具条左下、右下全空，整块看着像没做完 -->
					<template #footer-right>
						<span v-if="!draft.trim()" class="chat-panel__sendhint">回车发送</span>
					</template>
				</TrSender>
			</div>
		</template>

		<!-- 图片走内置大图预览；其余类型统一交给框架的在线预览组件（pdf / office / 文本 / 音视频等） -->
		<el-image-viewer v-if="imageUrl" :url-list="[imageUrl]" :hide-on-click-modal="true" @close="imageUrl = ''" />

		<el-drawer
			:model-value="!!doc"
			:title="doc?.name ?? '文件预览'"
			:size="props.mobile ? '100%' : '60%'"
			destroy-on-close
			append-to-body
			@close="doc = null"
		>
			<OnlineFilePreview :url="doc?.url ?? ''" class="chat-panel__preview" />
		</el-drawer>
	</div>
</template>

<script lang="ts" setup name="chatPanel">
import { computed, defineComponent, h, nextTick, ref, watch, withModifiers } from 'vue';
import { ElAvatar, ElIcon, ElImage, ElMessage, ElTooltip } from 'element-plus';
import { ArrowDown, ArrowLeft, Download, Files, InfoFilled, Loading, PictureFilled, View } from '@element-plus/icons-vue';
import { TrBubble, TrSender, useMessageContent } from '@opentiny/tiny-robot';
import { chatDayKey, chatDayLabel, formatChatTime } from './chatTime';
import { encodeFileMessage, encodeImageMessage, fileColorGroup, fileExt, formatFileSize, isImageName, parseChatContent } from './chatContent';
import { avatarText, displayName } from './chatUser';
import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system/api';
import { resolveDocumentBrowserFileUrl } from '/@/utils/documentFileUrl';
import OnlineFilePreview from '/@/components/file/OnlineFilePreview.vue';

import type { ChatMessageItem, ChatUserItem } from './chatTypes';

/** 气泡数据：content 直接用 TinyRobot 的内容协议 —— 文本传字符串，图片/文件传自定义类型交给 fallback 渲染器 */
type ChatBubbleItem = {
	key: string;
	placement: 'start' | 'end';
	content: string | Array<Record<string, any>>;
	senderName: string;
	avatar: string;
	timeText: string;
	/** 日期分割标签（换天的第一条消息才有值，其余为空） */
	dayText: string;
	/** 图片/文件消息：不铺渐变底，改白底细描边 */
	media: boolean;
};

/** 表情面板：常用表情，分组无关，够了就别堆太多 */
const EMOJIS = [
	'😀', '😄', '😁', '😂', '🤣', '😊', '😍', '😘',
	'😉', '😎', '🤔', '😐', '😴', '😭', '😡', '🥺',
	'👍', '👎', '👏', '🙏', '💪', '🤝', '✌️', '👌',
	'❤️', '🔥', '🎉', '⭐', '💯', '✅', '❌', '❓',
	'🚀', '💡', '☕', '🌹', '🍺', '🎁', '📌', '📎',
];

/** 单张图片大小上限（字节）：5MB */
const MAX_IMAGE_SIZE = 5 * 1024 * 1024;
/** 单个文件大小上限（字节）：50MB（后端 Upload.json 的 MaxSize 是 1GB，这里前端先卡一道体验线） */
const MAX_FILE_SIZE = 50 * 1024 * 1024;
/** 允许的图片后缀（传给后端 AllowSuffix 做二次校验） */
const IMAGE_SUFFIX = '.jpg.jpeg.png.bmp.gif';
/** 允许的文件后缀（空格分隔，直接传给后端 AllowSuffix；与 Upload.json 的 ContentType 白名单对齐） */
const FILE_SUFFIX = '.doc .docx .xls .xlsx .ppt .pptx .pdf .txt .md .csv .zip .rar .7z .mp4 .mkv .mp3 .wav .jpg .jpeg .png .bmp .gif';
/** 文件选择器的 accept：用后缀而非 MIME，各浏览器拿到的 MIME 差异太大 */
const FILE_ACCEPT = FILE_SUFFIX.split(' ').join(',');

const props = defineProps({
	user: { type: Object as () => ChatUserItem | null, default: null },
	messages: { type: Array as () => ChatMessageItem[], default: () => [] },
	sending: { type: Boolean, default: false },
	/** 历史记录是否拉取失败（失败要给出可见出口，不能静默显示空白） */
	loadFailed: { type: Boolean, default: false },
	myId: { type: Number, default: 0 },
	myName: { type: String, default: '' },
	myAvatar: { type: String, default: '' },
	/** 小屏单栏模式：显示返回键、头部与输入区按触屏尺寸收紧 */
	mobile: { type: Boolean, default: false },
});

const emit = defineEmits(['send', 'openNotice', 'retry', 'back', 'clearConversation']);

/** TrSender 暴露的实例：只需要根 DOM（$el），表情插入要用它找 contenteditable 编辑器 */
const senderRef = ref<{ $el?: HTMLElement } | null>(null);
const scrollRef = ref<HTMLElement>();
const imageRef = ref<HTMLInputElement>();
const fileRef = ref<HTMLInputElement>();
const emojiVisible = ref(false);
const uploading = ref(false);
/** 输入框内容（v-model 直控，提交后自行清空） */
const draft = ref('');
/** 大图预览地址（图片气泡与图片文件卡片共用，空表示关闭） */
const imageUrl = ref('');
/** 非图片文件的在线预览：pdf / office / 文本 / 音视频等都交给框架的 OnlineFilePreview */
const doc = ref<{ url: string; name: string } | null>(null);

/** 距底部多少像素内算"贴底"：留一点余量，否则图片加载撑开后就判定成上滑了 */
const BOTTOM_TOLERANCE = 60;

/** 当前是否贴在底部：贴底才自动跟随新消息，用户上滑翻历史时不打扰 */
const stickBottom = ref(true);
/** 切会话后强制落一次底：此时历史刚铺完，必须对齐最新一条 */
const forcePin = ref(false);

const isAtBottom = () => {
	const el = scrollRef.value;
	return el ? el.scrollHeight - el.scrollTop - el.clientHeight <= BOTTOM_TOLERANCE : true;
};

const pinToBottom = (smooth = false) => {
	scrollRef.value?.scrollTo({ top: scrollRef.value.scrollHeight, behavior: smooth ? 'smooth' : 'auto' });
	stickBottom.value = true;
};

const onListScroll = () => {
	stickBottom.value = isAtBottom();
};

/**
 * 切会话：强制落底 + 收起表情面板（避免它悬在上一个会话的输入框上）。
 */
watch(
	() => props.user?.userId ?? 0,
	() => {
		forcePin.value = true;
		stickBottom.value = true;
		emojiVisible.value = false;
	},
	{ immediate: true }
);

/**
 * 新消息进来时落到最新一条。
 * watcher 默认 pre 刷新，这里的 stickBottom 还是"追加前"的状态 —— 正是判断该不该跟随的依据；
 * 若等 DOM 更新完再读，内容已经变高，永远算不上贴底，就永远不滚了。
 */
watch(
	() => props.messages.length,
	() => {
		if (!forcePin.value && !stickBottom.value) return;
		forcePin.value = false;
		nextTick(() => pinToBottom());
	}
);

/** 图片是异步加载的，加载完高度才撑开；此时若仍贴底要再落一次，否则最新一条被顶出可视区 */
function onMediaLoad() {
	nextTick(() => stickBottom.value && pinToBottom());
}

/** 卡片上的提示语：直接告诉用户点下去会发生什么 */
const openTipOf = (file: { name?: string; url?: string }) => (!file.url ? '文件地址无效' : isImageName(file.name) ? '点击预览图片' : '点击在线预览');

/** 卡片里那行短提示：放不下长句，只用 4 个字说明动作（完整意思交给 title 与悬浮气泡） */
const shortTipOf = (file: { name?: string; url?: string }) => (!file.url ? '地址无效' : isImageName(file.name) ? '预览图片' : '在线预览');

/** 点击文件卡片：图片走大图预览，其余交给框架的在线预览组件 */
const openFileOf = (file: { name?: string; url?: string }) => {
	if (!file.url) {
		ElMessage.warning('文件地址无效，无法打开');
		return;
	}
	if (isImageName(file.name)) imageUrl.value = file.url;
	else doc.value = { url: file.url, name: file.name ?? '文件预览' };
};

/** 下载到本地：动态 a 标签触发，target=_blank 避免同源文件被当成路由跳转 */
const downloadFileOf = (file: { name?: string; url?: string }) => {
	if (!file.url) return;
	const a = document.createElement('a');
	a.href = file.url;
	a.download = file.name ?? '';
	a.target = '_blank';
	a.rel = 'noopener';
	document.body.appendChild(a);
	a.click();
	a.remove();
};

/** fallback 渲染器收到的自定义内容项：chat-image / chat-file 由我们编码产生，其余字段按文本兜底 */
type ChatContentItem = {
	type: string;
	url?: string;
	name?: string;
	size?: number;
	text?: string;
};

/**
 * 图片/文件内容渲染器：TinyRobot 默认只认 text / image_url 等协议类型，
 * 自定义类型（chat-image / chat-file）会落到 fallbackContentRenderer，在这里分发渲染。
 * 组件在 setup 作用域里定义，闭包直接拿到 onMediaLoad / openFileOf / downloadFileOf。
 */
const ChatContentRenderer = defineComponent({
	name: 'ChatContentRenderer',
	props: {
		message: { type: Object, required: true },
		contentIndex: { type: Number, default: 0 },
	},
	setup(rprops) {
		const { content } = useMessageContent(rprops as any);
		return () => {
			const item = content.value as ChatContentItem | null;
			if (!item) return null;

			if (item.type === 'chat-image') {
				const url = resolveDocumentBrowserFileUrl(item.url);
				return h('div', { class: 'chat-panel__image' }, [
					h(
						ElImage,
						{
							src: url,
							'preview-src-list': [url],
							'preview-teleported': true,
							fit: 'cover',
							loading: 'lazy',
							onLoad: () => onMediaLoad(),
						},
						{
							error: () =>
								h('div', { class: 'chat-panel__image-error' }, [
									h(ElIcon, null, () => h(PictureFilled)),
									h('span', '图片加载失败'),
								]),
						}
					),
				]);
			}

			if (item.type === 'chat-file') {
				const file = { name: item.name, url: resolveDocumentBrowserFileUrl(item.url) };
				return h(
					'div',
					{
						class: ['chat-panel__file', `is-${fileColorGroup(item.name)}`],
						// hover 时露出完整文件名：卡片宽度有限，长名字一定会被省略号截断
						title: item.name || openTipOf(file),
						onClick: () => openFileOf(file),
					},
					[
						// 扩展名角标：按文件类型分组配色（文档蓝 / 表格绿 / PDF 红 …）
						h('div', { class: 'chat-panel__file-icon' }, h('span', { class: 'chat-panel__file-ext' }, fileExt(item.name))),
						h('div', { class: 'chat-panel__file-info' }, [
							h('span', { class: 'chat-panel__file-name' }, item.name),
							// 第二行：大小 · 动作。没有大小时不渲染多余的点，避免出现「· 在线预览」
							h('div', { class: 'chat-panel__file-meta' }, [
								item.size ? h('span', { class: 'chat-panel__file-size' }, formatFileSize(item.size)) : null,
								item.size ? h('span', { class: 'chat-panel__file-dot' }, '·') : null,
								h('span', { class: 'chat-panel__file-hint' }, shortTipOf(file)),
							]),
						]),
						h('div', { class: 'chat-panel__file-acts' }, [
							h(ElTooltip, { content: openTipOf(file), placement: 'top' }, {
								default: () => h(ElIcon, { class: 'chat-panel__file-open', size: 14, onClick: withModifiers(() => openFileOf(file), ['stop']) }, () => h(View)),
							}),
							h(ElTooltip, { content: '下载到本地', placement: 'top' }, {
								default: () =>
									h(ElIcon, { class: 'chat-panel__file-dl', size: 14, onClick: withModifiers(() => downloadFileOf(file), ['stop']) }, () => h(Download)),
							}),
						]),
					]
				);
			}

			return h('div', String(item.text ?? ''));
		};
	},
});

// 气泡数据：自己发的一律靠右（placement=end），对方靠左
// key 带上下标：实时推送的消息在落库前 Id 为 0，只用 Id 会撞成重复 key
const bubbleList = computed<ChatBubbleItem[]>(() => {
	let prevDay = ''; // 上一条消息所属日期，用于判断要不要插日期分隔
	return (props.messages ?? []).map((m, i): ChatBubbleItem => {
		const mine = m.sendUserId === props.myId;
		const content = parseChatContent(m.message);

		const day = chatDayKey(m.sendTime);
		const dayText = day && day !== prevDay ? chatDayLabel(m.sendTime) : '';
		if (day) prevDay = day;

		const file = content.file;
		// 文本走 TinyRobot 默认渲染；图片/文件用自定义类型（默认渲染器不认识 → 落到 fallback 渲染器）
		const bubbleContent =
			content.kind === 'image'
				? [{ type: 'chat-image', url: content.image?.url }]
				: content.kind === 'file'
					? [{ type: 'chat-file', name: file?.name, size: file?.size, url: file?.url }]
					: (content.text ?? '');

		const senderName = mine ? props.myName || '我' : m.sendUserName || displayName(props.user) || '对方';
		const avatarSrc = mine ? props.myAvatar : props.user?.avatar || '';

		return {
			key: `${m.id ?? 0}_${i}`,
			placement: mine ? 'end' : 'start',
			content: bubbleContent,
			senderName,
			avatar: h(ElAvatar, { size: 34, src: avatarSrc || undefined, class: 'chat-avatar' }, { default: () => senderName.trim().slice(0, 1) }),
			timeText: formatChatTime(m.sendTime),
			dayText,
			media: content.kind !== 'text',
		};
	});
});

// 上滑翻历史时显示"回到最新"，不用手动拖滚动条
const showJump = computed(() => !stickBottom.value && bubbleList.value.length > 0);

// 提交：事件直接带回输入内容，提交后清空草稿
const onSubmit = (value?: string) => {
	const text = (value ?? draft.value).trim();
	if (!text) return;
	stickBottom.value = true; // 自己发言一律回到底部，即便刚才在上翻历史
	emit('send', text);
	draft.value = '';
};

/** 粘贴文件（从编辑器冒泡上来）：图片走图片消息、其余走文件消息 */
const onPasteNative = (e: ClipboardEvent) => {
	const files = Array.from(e.clipboardData?.files ?? []);
	if (!files.length) return;
	e.preventDefault();
	files.forEach((f) => uploadAndSend(f));
};

/** 从选择器取文件后发送：取完立刻清空 value，否则连续选同一个文件不会再触发 change */
const sendPickedFile = (input?: HTMLInputElement) => {
	const file = input?.files?.[0];
	if (input) input.value = '';
	if (file) uploadAndSend(file);
};

/** 上传附件并以对应消息类型发送（图片走图片气泡，其它走文件卡片） */
const uploadAndSend = async (file: File) => {
	const asImage = file.type?.startsWith('image/');
	const maxSize = asImage ? MAX_IMAGE_SIZE : MAX_FILE_SIZE;
	const limitText = asImage ? '图片不能超过 5MB' : '文件不能超过 50MB';
	if (file.size > maxSize) {
		ElMessage.warning(limitText);
		return;
	}

	uploading.value = true;
	try {
		const res = await getAPI(SysFileApi).apiSysFileUploadFilePostForm(file, 'chat', file.name, asImage ? IMAGE_SUFFIX : FILE_SUFFIX, undefined, undefined, true);
		const saved = res.data.result;
		const url = saved?.url;
		if (!url) throw new Error('上传未返回文件地址');

		stickBottom.value = true;
		emit('send', asImage ? encodeImageMessage({ url, name: file.name }) : encodeFileMessage({ url, name: file.name, size: file.size, id: Number(saved?.id ?? 0) || undefined }));
	} catch (err: any) {
		ElMessage.error(err?.message ?? '文件上传失败');
	} finally {
		uploading.value = false;
	}
};

/**
 * 插入表情：聚焦编辑器后用 execCommand 落在光标处（TinyRobot 编辑器是 Tiptap contenteditable，
 * insertText 会触发 input → v-model 同步）；编辑器还没渲染出来时退回直接拼到草稿末尾。
 */
const insertEmoji = (emoji: string) => {
	const editable = senderRef.value?.$el?.querySelector<HTMLElement>('[contenteditable="true"]');
	if (editable) {
		editable.focus();
		document.execCommand('insertText', false, emoji);
	} else {
		draft.value += emoji;
	}
};
</script>

<style scoped lang="scss">
.chat-panel {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-width: 0;
	height: 100%;
	background-color: var(--el-bg-color);

	&__blank {
		display: flex;
		flex: 1;
		align-items: center;
		justify-content: center;
	}

	&__header {
		display: flex;
		align-items: center;
		gap: 10px;
		flex: none;
		padding: 10px 16px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		background: linear-gradient(180deg, var(--el-bg-color) 0%, var(--el-fill-color-lighter) 100%);
	}

	// 头像与状态点：兜底底色与在线点样式见 chatBase.scss（列表与面板共用）
	&__avatar {
		position: relative;
		flex: none;
	}

	&__who {
		flex: 1;
		min-width: 0;
	}

	&__name {
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 15px;
		color: var(--el-text-color-primary);
	}

	&__sub {
		display: flex;
		gap: 12px;
		margin-top: 2px;
		font-size: 12px;
		color: var(--el-text-color-secondary);

		span {
			overflow: hidden;
			white-space: nowrap;
			text-overflow: ellipsis;
		}
	}

	&__acts {
		flex: none;
	}

	&__body {
		position: relative; // "回到最新"按钮相对会话区定位
		flex: 1;
		min-height: 0;
		padding: 12px 16px 0;
		// 会话区铺一层极淡的底色，与左右两侧纯白面板区分开
		background-image: linear-gradient(180deg, var(--el-fill-color-lighter) 0%, var(--el-bg-color) 120px);
	}

	&__scroll {
		height: 100%;
		overflow-y: auto;
		overflow-x: hidden;
		padding-bottom: 12px;
		// 同输入框：tr 自带一套亮色主题文本变量，项目切深色时不跟随，
		// 会让对方气泡的正文变成深灰字压深灰底，这里统一桥接到 Element Plus
		--tr-text-primary: var(--el-text-color-primary);
		--tr-text-secondary: var(--el-text-color-secondary);
		--tr-text-tertiary: var(--el-text-color-placeholder);

		// 气泡上下留白收紧，一屏能多看几条；宽度与旧版一致（72%）
		:deep(.tr-bubble) {
			margin: 6px 0;
			--tr-bubble-max-width: 72%;
		}

		// 自己发的气泡走主色渐变白字，对方维持浅底描边，视觉上一眼分清主客
		:deep(.tr-bubble[data-placement='end']) {
			--tr-bubble-text-color: #fff;
			--tr-bubble-box-shadow: 0 2px 8px color-mix(in srgb, var(--el-color-primary) 28%, transparent);

			.tr-bubble__box {
				background-image: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
				border: none;
			}
		}

		// 对方气泡：浅灰底 + 极淡描边，纯白背景下也能看出气泡边界
		:deep(.tr-bubble[data-placement='start']) {
			--tr-bubble-box-bg: var(--el-fill-color-light);
			--tr-bubble-box-border: 1px solid var(--el-border-color-lighter);
			--tr-bubble-box-shadow: none;
		}

		// 默认文本渲染器不处理换行，这里补回 pre-wrap；内容区恢复可选中
		:deep(.tr-bubble__text) {
			white-space: pre-wrap;
			word-break: break-word;
			font-size: 14px;
			line-height: 1.6;
		}

		:deep(.tr-bubble__content) {
			user-select: text;
			-webkit-user-select: text;
		}
	}

	// 图片/文件消息不铺渐变底：绿底包图片会露边，改成白底细描边、图片满铺
	&__scroll :deep(.tr-bubble.is-media .tr-bubble__box) {
		background-image: none;
		background-color: var(--el-bg-color);
		border: 1px solid var(--el-border-color-lighter);
		box-shadow: 0 1px 4px rgb(0 0 0 / 4%);
		padding: 0;
	}

	// 媒体气泡放宽到 84%：文件卡片要放下文件名，72% 会把名字截得太早
	&__scroll :deep(.tr-bubble.is-media) {
		--tr-bubble-max-width: 84%;
	}

	// 日期分隔：居中胶囊，与两侧气泡拉开距离
	&__day {
		display: flex;
		justify-content: center;
		margin: 10px 0 6px;

		span {
			padding: 2px 10px;
			font-size: 11.5px;
			color: var(--el-text-color-secondary);
			background-color: color-mix(in srgb, var(--el-text-color-secondary) 12%, transparent);
			border-radius: 10px;
		}
	}

	// 名字/时间一行：用头像宽度（34 + 10）缩进，对齐气泡列；自己的靠右
	&__meta {
		display: flex;
		gap: 8px;
		padding: 4px 0 0 44px;
		font-size: 11.5px;
		line-height: 1.5;
		color: var(--el-text-color-secondary);

		&.is-mine {
			justify-content: flex-end;
			padding: 4px 44px 0 0;
		}
	}

	// 回到最新：会话区右下角悬浮胶囊
	&__jump {
		position: absolute;
		right: 20px;
		bottom: 16px;
		display: inline-flex;
		align-items: center;
		gap: 4px;
		padding: 6px 12px 6px 10px;
		font-size: 12px;
		color: var(--el-color-primary);
		cursor: pointer;
		background-color: var(--el-bg-color-overlay);
		border: 1px solid var(--el-color-primary-light-7);
		border-radius: 999px;
		box-shadow: 0 4px 14px rgb(0 0 0 / 10%);
		transition: all 0.2s;

		&:hover {
			color: #fff;
			background-color: var(--el-color-primary);
			border-color: var(--el-color-primary);
			transform: translateY(-1px);
		}
	}

	// 在线预览抽屉：内容铺满抽屉高度（组件根节点接收父级 scoped 属性，append-to-body 后同样生效）
	&__preview {
		height: 100%;
	}

	// 上传中提示：贴在工具栏右侧，避免大文件上传时界面像卡住
	&__uploading {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		padding-left: 4px;
		font-size: 12px;
		color: var(--el-color-primary);
	}

	// 空态右侧的快捷键提示：弱化到几乎只剩形状，不抢输入区的注意力
	&__sendhint {
		padding-right: 4px;
		font-size: 12px;
		color: var(--el-text-color-placeholder);
		user-select: none;
	}

	&__hint {
		padding-top: 40px;
		text-align: center;
		font-size: 13px;
		color: var(--el-text-color-secondary);
	}

	&__retry {
		color: var(--el-color-primary);
		cursor: pointer;

		&:hover {
			text-decoration: underline;
		}
	}

	// 看自己时的说明条：替代输入区，避免留一个发不出去的输入框
	&__selftip {
		display: flex;
		flex: none;
		align-items: center;
		justify-content: center;
		gap: 6px;
		padding: 14px 16px calc(14px + env(safe-area-inset-bottom, 0px));
		font-size: 12.5px;
		color: var(--el-text-color-secondary);
		border-top: 1px solid var(--el-border-color-lighter);
		background-color: var(--el-bg-color);
	}

	&__footer {
		flex: none;
		padding: 8px 16px 12px;
		border-top: 1px solid var(--el-border-color-lighter);
		background-color: var(--el-bg-color);

		// 输入框收成圆角卡片；主题色整体接管（发送按钮/聚焦光环都跟 --el-color-primary 走）
		:deep(.tr-sender) {
			--tr-color-primary: var(--el-color-primary);
			--tr-color-primary-light: var(--el-color-primary-light-5);
			// 发送键默认 36px，收窄到 26px 与底部工具栏同一视觉重量
			--tr-sender-button-size-submit: 26px;
			// 组件自带一套亮/暗主题变量，但项目切到深色时它不跟着切，
			// 仍用自己的浅色主题值（#191919），于是深色底上文字几乎看不见。
			// 这里把编辑器文字与占位符直接绑到 Element Plus 的文本色上，亮暗自动跟随。
			--tr-sender-text-color: var(--el-text-color-primary);
			--tr-sender-placeholder-color: var(--el-text-color-placeholder);
			--tr-text-primary: var(--el-text-color-primary);
			--tr-text-secondary: var(--el-text-color-secondary);
			--tr-text-tertiary: var(--el-text-color-placeholder);

			background-color: var(--el-bg-color);
			border: 1px solid var(--el-border-color-lighter);
			border-radius: 12px;
			box-shadow: 0 1px 4px rgb(0 0 0 / 5%);
			transition: border-color 0.25s, box-shadow 0.25s;
		}

		// 聚焦时亮一圈主题色光环，和 Element 输入框的聚焦反馈保持同一语言
		:deep(.tr-sender:focus-within) {
			border-color: var(--el-color-primary-light-5);
			box-shadow:
				0 0 0 3px color-mix(in srgb, var(--el-color-primary) 12%, transparent),
				0 2px 8px rgb(0 0 0 / 6%);
		}

		// 底部一行（工具 + 发送按钮）加一点内边距，与编辑区呼吸开
		:deep(.tr-sender-footer) {
			padding: 2px 8px 6px 8px;
		}

		// 清除草稿的 ×（组件默认是裸图标且颜色偏深）：收成浅灰圆形小按钮，与左侧工具同一套质感
		:deep(.tr-action-button) {
			display: inline-flex;
			align-items: center;
			justify-content: center;
			width: 24px;
			height: 24px;
			padding: 0;
			font-size: 13px;
			cursor: pointer;
			color: var(--el-text-color-placeholder);
			background: transparent;
			border: none;
			border-radius: 50%;
			transition: all 0.2s;

			&:hover {
				color: var(--el-text-color-regular);
				background-color: var(--el-fill-color-light);
			}
		}

		// 组件把 hover 色写死成蓝色，这里跟着主题走
		:deep(.tr-sender-submit-button:hover .tr-sender-submit-button__icon) {
			color: var(--el-color-primary-dark-2);
		}

		// 可发送时放大并加一点按压力度（图标本身是圆弧图形，再叠底色会变成"甜甜圈"，所以只做尺寸与位移）
		:deep(.tr-sender-submit-button:not(.is-disabled):not(.is-loading)) {
			transition: transform 0.2s;
		}

		:deep(.tr-sender-submit-button:not(.is-disabled):not(.is-loading):hover) {
			transform: translateY(-1px) scale(1.06);
		}
	}

	// 底部工具栏（表情/图片/文件）：圆形按钮，hover 铺一层主题色浅底
	&__tools {
		display: flex;
		align-items: center;
		gap: 4px;
	}

	// 小屏返回键：贴在最左，圆形图标按钮，留出足够触控面积
	&__back {
		flex: none;
		width: 32px;
		height: 32px;
		margin-left: -8px;
		padding: 0;
	}

	// 空白态（未选会话）时的返回条
	&__backbar {
		display: flex;
		align-items: center;
		width: 100%;
		padding: 6px 8px;
		border-bottom: 1px solid var(--el-border-color-lighter);
	}

	&__tool {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 30px;
		height: 30px;
		padding: 0;
		font-size: 16px;
		line-height: 1;
		color: var(--el-text-color-secondary);
		cursor: pointer;
		background: transparent;
		border: none;
		border-radius: 50%;
		transition: all 0.2s;

		&:hover {
			color: var(--el-color-primary);
			background-color: color-mix(in srgb, var(--el-color-primary) 10%, transparent);
		}

		&:active {
			transform: scale(0.92);
		}

		&:disabled {
			cursor: not-allowed;
			opacity: 0.5;
		}
	}
}

/* ===================== 小屏（手机） ===================== */
@media (max-width: 768px) {
	.chat-panel {
		&__header {
			gap: 8px;
			padding: 8px 12px;
		}

		// 副信息（账号 / IP / 浏览器）只留第一项，否则在 360px 宽下会把姓名挤没
		&__sub span + span {
			display: none;
		}

		&__body {
			padding: 10px 12px 0;
		}

		// 底部输入区：贴边留白收窄，并给 iPhone 底部安全区留出高度，避免发送按钮落在手势条底下
		&__footer {
			padding: 6px 10px calc(8px + env(safe-area-inset-bottom, 0px));

			// 关键：底部工具栏允许收缩并裁掉，发送按钮绝不让位（flex: none + margin-left:auto）
			:deep(.tr-sender-footer-left) {
				flex: 0 1 auto;
				min-width: 0;
				overflow: hidden;
			}

			:deep(.tr-sender-footer-right) {
				flex: none;
				margin-left: auto;
			}
		}

		&__jump {
			right: 12px;
			bottom: 12px;
		}
	}
}

// "回到最新"按钮的进出动画
.chat-jump-enter-active,
.chat-jump-leave-active {
	transition: opacity 0.2s, transform 0.2s;
}

.chat-jump-enter-from,
.chat-jump-leave-to {
	opacity: 0;
	transform: translateY(8px);
}
</style>

<!--
	图片 / 文件消息的样式放非 scoped 块。
	这两类内容由 fallbackContentRenderer 在子组件里动态创建，节点上没有本组件的 data-v 标记，
	scoped 选择器匹配不到；类名前缀固定（chat-panel__image / chat-panel__file），不会误伤别处。
-->
<style lang="scss">
.chat-panel__image {
	.el-image {
		display: block;
		max-width: 200px;
		border-radius: 8px;
		overflow: hidden;
	}

	.el-image__inner {
		display: block;
		max-height: 240px;
		cursor: zoom-in;
	}
}

.chat-panel__image-error {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 4px;
	width: 120px;
	height: 90px;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	background-color: var(--el-fill-color);
}

// 文件卡片：扩展名角标（按类型配色）+ 文件名 + 大小/提示 + 打开/下载
// 每类文件给一个主题色变量 --file-c，角标底色用 color-mix 调出浅色版，深浅主题都成立
.chat-panel__file {
	--file-c: var(--el-text-color-secondary);

	display: flex;
	align-items: center;
	gap: 10px;
	// 宽度交给父级气泡（maxWidth 84%），窄屏上才不会被卡片撑破；整卡可点打开预览
	width: 100%;
	max-width: 320px;
	padding: 10px 10px 10px 12px;
	cursor: pointer;
	background-color: var(--el-fill-color-light);
	border: 1px solid transparent;
	border-radius: 10px;
	transition: background-color 0.25s, border-color 0.25s, box-shadow 0.25s, transform 0.25s;

	&.is-doc {
		--file-c: #3b82f6;
	}
	&.is-sheet {
		--file-c: #22c55e;
	}
	&.is-slide {
		--file-c: #f97316;
	}
	&.is-pdf {
		--file-c: #ef4444;
	}
	&.is-archive {
		--file-c: #8b5cf6;
	}
	&.is-media {
		--file-c: #06b6d4;
	}
	&.is-image {
		--file-c: #ec4899;
	}
}

.chat-panel__file-icon {
	display: flex;
	flex: none;
	align-items: center;
	justify-content: center;
	width: 42px;
	height: 42px;
	color: var(--file-c);
	background: linear-gradient(135deg, color-mix(in srgb, var(--file-c) 22%, transparent) 0%, color-mix(in srgb, var(--file-c) 10%, transparent) 100%);
	border-radius: 10px;
}

.chat-panel__file-ext {
	font-size: 10px;
	font-weight: 800;
	letter-spacing: -0.3px;
}

.chat-panel__file-info {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-width: 0;
	gap: 3px;
}

.chat-panel__file-name {
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

// 第二行：大小 · 动作，弱化显示；不换行，宁可省略也不折成两行
.chat-panel__file-meta {
	display: flex;
	align-items: center;
	gap: 4px;
	min-width: 0;
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
}

.chat-panel__file-hint {
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

// 打开与下载按钮：默认弱化，hover 卡片时点亮；圆形白底，比裸图标更有点击暗示
.chat-panel__file-acts {
	display: flex;
	flex: none;
	align-items: center;
	gap: 3px;
	opacity: 0.55;
	transition: opacity 0.2s;

	.chat-panel__file:hover & {
		opacity: 1;
	}
}

.chat-panel__file-open,
.chat-panel__file-dl {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	cursor: pointer;
	color: var(--el-text-color-secondary);
	background-color: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 50%;
	transition: all 0.2s;

	&:hover {
		color: var(--file-c, var(--el-color-primary));
		background-color: color-mix(in srgb, var(--file-c, var(--el-color-primary)) 10%, transparent);
		border-color: color-mix(in srgb, var(--file-c, var(--el-color-primary)) 40%, transparent);
		transform: scale(1.08);
	}
}

// 表情面板挂到 body 上（teleport），同样只能走非 scoped 块
.chat-emoji-popper {
	padding: 8px !important;
}

.chat-emoji {
	display: grid;
	grid-template-columns: repeat(8, 1fr);
	gap: 2px;
	max-height: 220px;
	overflow: auto;

	&__item {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 30px;
		font-size: 19px;
		cursor: pointer;
		border-radius: 6px;
		transition: all 0.15s;

		&:hover {
			background-color: var(--el-fill-color-light);
			transform: scale(1.15);
		}
	}
}
</style>
