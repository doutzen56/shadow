<template>
	<div class="chat-user-list">
		<!-- 顶部操作区：刷新与搜索同一行，刷新不再挂在抽屉标题上 -->
		<div class="chat-user-list__top">
			<el-input v-model="keyword" class="chat-user-list__search" placeholder="搜索姓名 / 账号" clearable size="default">
				<template #prefix>
					<el-icon><ele-Search /></el-icon>
				</template>
			</el-input>
			<el-tooltip content="刷新列表" placement="bottom">
				<el-button class="chat-user-list__refresh" :icon="Refresh" :loading="props.loading" @click="emit('refresh')" />
			</el-tooltip>
		</div>

		<div class="chat-user-list__tabs">
			<span v-for="t in TABS" :key="t.key" :class="['is-' + t.key, { active: tab === t.key }]" @click="tab = t.key">
				<el-icon :size="13"><component :is="t.icon" /></el-icon>
				<b>{{ t.label }}</b>
				<i>{{ counts[t.key] }}</i>
			</span>
		</div>

		<el-scrollbar ref="barRef" class="chat-user-list__body" always @scroll="onScroll">
			<div class="chat-user-list__inner">
				<!-- key 必须用行键（连接Id）：同一账号多端登录是多行，用 userId 会撞键导致渲染错乱 -->
				<div
					v-for="u in visibleUsers"
					:key="keyOf(u)"
					class="chat-user-item"
					:class="{ 'is-active': keyOf(u) === props.activeKey, 'is-unread': u.unread }"
					@click="emit('select', u)"
				>
					<div class="chat-user-item__avatar">
						<el-avatar :size="42" :src="u.avatar || ''" class="chat-avatar">{{ avatarText(u) }}</el-avatar>
						<i class="chat-dot" :class="{ 'is-online': u.online }"></i>
					</div>

					<div class="chat-user-item__main">
						<div class="chat-user-item__line">
							<b class="chat-user-item__name">{{ displayName(u) }}</b>
							<!-- 自己也在列表里：标一个"我"，免得以为是别人 -->
							<span v-if="u.isMe" class="chat-user-item__me">我</span>
							<!-- 同一账号多端/多标签登录：一行一个连接，标出"第几端/共几端"，否则"在线 4 只显示 3 个人"会被当成漏人 -->
						<span
							v-if="(u.deviceCount ?? 1) > 1"
							class="chat-user-item__dev"
							:title="`该账号共 ${u.deviceCount} 个连接（多标签页 / 多端登录），这是第 ${u.deviceIndex} 个`"
						>
							{{ u.deviceIndex }}/{{ u.deviceCount }} 端
						</span>
						<span class="chat-user-item__time">{{ formatChatTime(u.lastTime) }}</span>
						</div>
						<div class="chat-user-item__line">
							<span class="chat-user-item__msg">
								<!-- 自己最后发的那条加"我："前缀，一眼分清是谁收尾 -->
								<i v-if="u.lastMessage && u.lastSendUserId === props.myId" class="chat-user-item__mine">我：</i>
								{{ toMessageSummary(u.lastMessage) || (u.online ? `${u.ip ?? ''} ${u.browser ?? ''}`.trim() : '离线') }}
							</span>
							<i v-if="u.unread" class="chat-user-item__badge"></i>
						</div>
					</div>

					<!-- 操作区：绝对定位在行右上角，悬停/聚焦时才出现。
					     放进流里会常年占掉近 90px，姓名和时间被硬挤到左边，为三个平时用不到的图标不值当 -->
					<div class="chat-user-item__acts">
						<el-tooltip v-if="u.online" :content="u.isMe ? '断开我这条连接' : '强制下线（只断开这条连接）'" placement="top">
							<el-button
								v-auth="'sysOnlineUser/forceOffline'"
								icon="ele-CircleCloseFilled"
								text
								:type="u.isMe ? 'warning' : 'danger'"
								@click.stop="emit('forceOffline', u)"
							/>
						</el-tooltip>
						<el-tooltip
							v-if="u.online && (u.deviceCount ?? 1) > 1"
							:content="u.isMe ? `断开我的其它 ${(u.deviceCount ?? 1) - 1} 个连接（保留这一端）` : `断开该账号的其它 ${(u.deviceCount ?? 1) - 1} 个连接（保留这一条）`"
							placement="top"
						>
							<el-button
								v-auth="'sysOnlineUser/forceOffline'"
								icon="ele-SwitchButton"
								text
								type="danger"
								@click.stop="emit('forceOfflineAll', u)"
							/>
						</el-tooltip>
						<el-tooltip v-if="!u.isMe" content="清空与该用户的聊天记录（仅自己参与的）" placement="top">
							<el-button
								icon="ele-Delete"
								text
								type="danger"
								@click.stop="emit('deleteConversation', u.userId)"
							/>
						</el-tooltip>
					</div>
				</div>

				<!-- 懒加载提示：滚到底自动再加载一页 -->
				<div v-if="filteredUsers.length" class="chat-user-list__more">
					<template v-if="hasMore">向下滚动加载更多（{{ visibleCount }} / {{ filteredUsers.length }}）</template>
					<template v-else>— 已显示全部 {{ filteredUsers.length }} 条 —</template>
				</div>

				<el-empty v-if="!filteredUsers.length" description="暂无可聊天的用户" :image-size="90" />
			</div>
		</el-scrollbar>
	</div>
</template>

<script lang="ts" setup name="chatUserList">
import { computed, ref, watch } from 'vue';
import { Connection, Moon, Refresh, User } from '@element-plus/icons-vue';
import { formatChatTime } from './chatTime';
import { toMessageSummary } from './chatContent';
import { avatarText, displayName, rowKey } from './chatUser';
import type { ChatUserItem } from './chatTypes';

/** 顶部筛选：在线 / 离线 / 全部，各配一个图标点缀 */
const TABS = [
	{ key: 'online', label: '在线', icon: Connection },
	{ key: 'offline', label: '离线', icon: Moon },
	{ key: 'all', label: '全部', icon: User },
] as const;

type TabKey = (typeof TABS)[number]['key'];

const props = defineProps({
	users: { type: Array as () => ChatUserItem[], default: () => [] },
	/** 当前选中行的行键（不是用户Id：同一账号多端登录有多行，只有行键能唯一定位） */
	activeKey: { type: String, default: '' },
	loading: { type: Boolean, default: false },
	/** 当前登录用户Id：用于给"我发的最后一条"加前缀 */
	myId: { type: Number, default: 0 },
});

const emit = defineEmits(['select', 'forceOffline', 'forceOfflineAll', 'refresh', 'deleteConversation']);

/** 行键：在线行 = 连接Id，离线行 = 用户Id（见 chatUser.ts 的 rowKey） */
const keyOf = (u: ChatUserItem) => rowKey(u);

/** 每页条数：首屏只渲染 50 条，其余滚到底再补，避免上百人时一次性铺开卡顿 */
const PAGE_SIZE = 50;
/** 距底部多少像素即认为"滚到底了" */
const LOAD_MORE_THRESHOLD = 80;

const keyword = ref('');
const tab = ref<TabKey>('all');
const visibleCount = ref(PAGE_SIZE);
const barRef = ref<any>(null);

/** 各筛选项的数量，供顶部标签显示 */
const counts = computed<Record<TabKey, number>>(() => {
	const online = props.users.filter((u) => u.online).length;
	return { online, offline: props.users.length - online, all: props.users.length };
});

const filteredUsers = computed(() => {
	const kw = keyword.value.trim().toLowerCase();
	return props.users.filter((u) => {
		if (tab.value === 'online' && !u.online) return false;
		if (tab.value === 'offline' && u.online) return false;
		if (!kw) return true;
		return `${u.realName ?? ''}${u.userName ?? ''}`.toLowerCase().includes(kw);
	});
});

/** 当前实际渲染的条目 */
const visibleUsers = computed(() => filteredUsers.value.slice(0, visibleCount.value));
const hasMore = computed(() => visibleCount.value < filteredUsers.value.length);

/** 再加载一页 */
const loadMore = () => {
	if (!hasMore.value) return;
	visibleCount.value = Math.min(visibleCount.value + PAGE_SIZE, filteredUsers.value.length);
};

/** 滚动到底自动续加载：ElScrollbar 的滚动容器在 wrapRef 上 */
const onScroll = () => {
	const wrap = barRef.value?.wrapRef as HTMLElement | undefined;
	if (!wrap) return;
	if (wrap.scrollTop + wrap.clientHeight >= wrap.scrollHeight - LOAD_MORE_THRESHOLD) loadMore();
};

// 换了筛选条件就从头开始分页，否则"搜索出 3 条"却沿用上次的 100 条计数会一直显示已到底
watch([keyword, tab], () => (visibleCount.value = PAGE_SIZE));
// 总人数变少时把计数收回来，避免 slice 越界后底部提示与实际条数对不上
watch(
	() => filteredUsers.value.length,
	(len) => (visibleCount.value = Math.max(PAGE_SIZE, Math.min(visibleCount.value, len)))
);
</script>

<style scoped lang="scss">
.chat-user-list {
	display: flex;
	flex-direction: column;
	// 宽度由父级拖动分隔条内联控制（默认 320px）：276 太挤，
	// "姓名 + 1/3 端标签 + 时间"一行放不下，时间常被挤掉
	width: 320px;
	flex: none;
	height: 100%;
	background-color: var(--el-bg-color);

	&__top {
		display: flex;
		align-items: center;
		gap: 6px;
		padding: 12px 12px 4px;
	}

	&__search {
		flex: 1;
		min-width: 0;
	}

	// 刷新按钮：与搜索框平齐的圆形图标键
	&__refresh {
		flex: none;
		width: 32px;
		height: 32px;
		padding: 0;
		border-radius: 8px;
	}

	// 在线 / 离线 / 全部：三等分的胶囊分段器，选中项浅主色底 + 主色描边
	&__tabs {
		display: flex;
		gap: 6px;
		padding: 8px 12px 10px;

		span {
			display: flex;
			flex: 1;
			align-items: center;
			justify-content: center;
			gap: 4px;
			padding: 5px 0;
			font-size: 12.5px;
			color: var(--el-text-color-secondary);
			cursor: pointer;
			border: 1px solid transparent;
			border-radius: 8px;
			transition: all 0.2s;

			b {
				font-weight: 500;
			}

			// 数量：弱化显示，避免抢了名称的视觉重心
			i {
				font-style: normal;
				font-size: 11px;
				opacity: 0.75;
			}

			// 图标配色：在线绿、离线灰、全部主色
			&.is-online .el-icon {
				color: var(--el-color-success);
			}

			&.is-offline .el-icon {
				color: var(--el-text-color-placeholder);
			}

			&.is-all .el-icon {
				color: var(--el-color-info);
			}

			&:hover {
				color: var(--el-color-primary);
				background-color: var(--el-fill-color-light);
			}

			&.active {
				font-weight: 600;
				color: var(--el-color-primary);
				background-color: var(--el-color-primary-light-9);
				border-color: color-mix(in srgb, var(--el-color-primary) 35%, transparent);

				b {
					font-weight: 600;
				}
			}
		}
	}

	&__body {
		flex: 1;
		min-height: 0;
	}

	&__inner {
		padding-bottom: 8px;
	}

	&__more {
		padding: 12px 0 4px;
		font-size: 12px;
		text-align: center;
		color: var(--el-text-color-placeholder);
	}
}

.chat-user-item {
	position: relative;
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px 14px;
	cursor: pointer;
	transition: background-color 0.2s;

	// 选中态：左侧竖条 + 浅色底，比单纯换底色更像主流聊天软件
	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 12px;
		bottom: 12px;
		width: 3px;
		border-radius: 0 3px 3px 0;
		background-color: transparent;
		transition: background-color 0.2s;
	}

	&:hover {
		background-color: var(--el-fill-color-light);
	}

	&.is-active {
		background-color: var(--el-color-primary-light-9);

		&::before {
			background-color: var(--el-color-primary);
		}

		.chat-user-item__name {
			color: var(--el-color-primary);
		}
	}

	// 头像与状态点：兜底底色与在线点样式见 chatBase.scss（列表与面板共用）
	&__avatar {
		position: relative;
		flex: none;
	}

	&__main {
		flex: 1;
		min-width: 0;
	}

	&__line {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 8px;
	}

	&__name {
		overflow: hidden;
		white-space: nowrap;
		text-overflow: ellipsis;
		font-size: 14px;
		font-weight: 500;
		color: var(--el-text-color-primary);
	}

	// "我"标记：橙色小签，一眼把自己的条目和别人分开
	&__me {
		flex: none;
		padding: 0 5px;
		margin-right: auto;
		font-size: 10.5px;
		line-height: 15px;
		color: var(--el-color-warning);
		background-color: var(--el-color-warning-light-9);
		border-radius: 4px;
	}

	// 多端登录标记：淡蓝底小签，明确但不抢名称的注意力
	&__dev {
		flex: none;
		padding: 0 5px;
		margin-right: auto;
		font-size: 10.5px;
		line-height: 15px;
		color: var(--el-color-primary);
		background-color: var(--el-color-primary-light-9);
		border-radius: 4px;
	}

	&__time {
		flex: none;
		font-size: 11px;
		color: var(--el-text-color-secondary);
		transition: opacity 0.16s ease;
	}

	&__msg {
		overflow: hidden;
		white-space: nowrap;
		text-overflow: ellipsis;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	// "我："前缀：弱化到比消息正文更淡，只起点睛作用
	&__mine {
		font-style: normal;
		color: var(--el-text-color-placeholder);
	}

	// 未读标记：红色圆点（不用文字"新"，更克制）
	&__badge {
		flex: none;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background-color: var(--el-color-danger);
	}

	&.is-unread &__name {
		font-weight: 700;
	}

	&.is-unread &__msg {
		color: var(--el-text-color-primary);
	}

	// 操作区：脱离文档流浮在行右上角，悬停/聚焦才出现。
	// 三个图标键常驻会白占近 90px，而这行真正要读的是姓名和时间
	&__acts {
		position: absolute;
		top: 5px;
		right: 6px;
		z-index: 2;
		display: flex;
		align-items: center;
		gap: 2px;
		// 不设上下内边距：整颗药丸要收在第一行的高度内，否则下沿会压到第二行的消息摘要
		padding: 0 4px;
		border-radius: 8px;
		// 名字很长时会从按钮下面穿过去，铺一层半透明底 + 轻模糊保证图标始终认得清
		background-color: color-mix(in srgb, var(--el-bg-color) 92%, transparent);
		backdrop-filter: blur(3px);
		opacity: 0;
		pointer-events: none;
		transition: opacity 0.16s ease;

		// 图标键之间 Element 默认有 12px 外边距，在这里纯属浪费
		.el-button {
			margin-left: 0;
			padding: 4px;
		}
	}

	&:hover &__acts,
	&:focus-within &__acts {
		opacity: 1;
		pointer-events: auto;
	}

	// 时间与操作区都在第一行右侧，同时出现会糊成一团：悬停时让时间退场
	&:hover &__time,
	&:focus-within &__time {
		opacity: 0;
	}
}

/* 小屏：列表整屏铺开（不再受拖动宽度约束） */
@media (max-width: 768px) {
	.chat-user-list {
		width: 100% !important;
	}

	.chat-user-item {
		padding: 12px 14px; // 触控目标放大一点
	}

	// 触屏没有 hover：操作区回到文档流常显，否则永远点不到，绝对定位还会压住时间
	.chat-user-item__acts {
		position: static;
		padding: 0;
		background: none;
		backdrop-filter: none;
		opacity: 0.7;
		pointer-events: auto;
	}
}
</style>
