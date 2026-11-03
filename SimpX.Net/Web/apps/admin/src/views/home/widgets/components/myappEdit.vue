<template>
	<el-drawer v-model="visible" title="添加应用" size="min(820px, 100%)" destroy-on-close @open="resetDraft">
		<template #header>
			<div>
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Menu /> </el-icon>
				<span>{{ '添加应用' }}</span>
			</div>
		</template>
		<div class="picker">
			<!-- 候选：搜索 + 按目录分组 -->
			<section class="picker__pane">
				<el-input v-model="keyword" class="picker__search" placeholder="搜索名称、目录、路由或拼音首字母" prefix-icon="ele-Search" clearable :validate-event="false" />
				<div class="picker__scroll">
					<div v-for="group in displayGroups" :key="group.id" class="app-group">
						<div class="app-group__title">{{ group.title }}</div>
						<button
							v-for="row in group.rows"
							:key="row.id"
							type="button"
							class="app-row"
							:class="{ 'is-picked': pickedIds.has(row.id) }"
							:aria-pressed="pickedIds.has(row.id)"
							:title="row.title"
							@click="toggle(row.item)"
						>
							<SvgIcon :name="row.icon" :size="16" class="app-row__icon" />
							<span class="app-row__title">
								<span v-for="(seg, i) in row.segments" :key="i" :class="{ hit: seg.hit }">{{ seg.text }}</span>
							</span>
							<span v-if="isSearching" class="app-row__group">{{ row.dir }}</span>
							<el-icon class="app-row__flag"><ele-Check v-if="pickedIds.has(row.id)" /><ele-Plus v-else /></el-icon>
						</button>
					</div>
					<el-empty v-if="isSearching && !displayGroups.length" description="没有匹配的应用" :image-size="60" />
				</div>
			</section>

			<!-- 已选：拖拽排序 + 上/下移 + 移除 -->
			<section class="picker__pane picker__pane--picked">
				<div class="picker__head">
					<span>已选 {{ draft.length }}</span>
					<el-button icon="ele-Delete" type="danger" plain :disabled="!draft.length" @click="draft = []">清空</el-button>
				</div>
				<VueDraggable v-model="draft" tag="ul" :animation="180" class="picked">
					<li v-for="(item, index) in draft" :key="item.id" class="picked__item">
						<el-icon class="picked__handle"><ele-Rank /></el-icon>
						<SvgIcon :name="iconOf(item)" :size="20" class="picked__icon" />
						<div class="picked__text" :title="`${titleOf(item)} · ${dirOf(item) || '未归类'} · ${item.path}`">
							<span class="picked__title">{{ titleOf(item) }}</span>
							<span class="picked__dir">{{ dirOf(item) || '未归类' }}</span>
						</div>
						<span class="picked__ops">
							<button type="button" class="picked__op" title="上移" :disabled="index === 0" @click="move(index, -1)">
								<el-icon><ele-Top /></el-icon>
							</button>
							<button type="button" class="picked__op" title="下移" :disabled="index === draft.length - 1" @click="move(index, 1)">
								<el-icon><ele-Bottom /></el-icon>
							</button>
							<button type="button" class="picked__op" title="移除" @click="removeAt(index)">
								<el-icon><ele-Close /></el-icon>
							</button>
						</span>
					</li>
				</VueDraggable>
				<div v-if="!draft.length" class="picked__empty">{{ '点左侧列表项加入快捷入口' }}</div>
			</section>
		</div>

		<template #footer>
			<div class="picker__footer">
				<el-button icon="ele-CircleCloseFilled" @click="visible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" @click="submit">确定</el-button>
			</div>
		</template>
	</el-drawer>
</template>

<!-- 快捷入口 - 添加应用 -->
<script lang="ts" setup name="myappEdit">
import { computed, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { VueDraggable } from 'vue-draggable-plus';

import { useQuickEntry } from '/@/stores/quickEntry';
import { MenuOutput } from '/@/api-services/system/models';
import { matchPinyin } from '/@/utils/pinyinMatch';

const visible = defineModel<boolean>();
const quickEntry = useQuickEntry();

const draft = ref<MenuOutput[]>([]); // 草稿态，点保存才提交
const keyword = ref('');

const isSearching = computed(() => !!keyword.value.trim());

const titleOf = (item: MenuOutput) => item.meta?.title || item.name || '';
const iconOf = (item: MenuOutput) => item.meta?.icon || 'ele-Menu';
// 已选项可能因菜单停用/失权限而不在候选池里，退回显示路由，避免第二行空着
const dirOf = (item: MenuOutput) => quickEntry.pool.dirById.get(item.id as number) || (item.path ?? '');

// 打开时按服务端最新状态重建草稿：卡片可能尚未加载，或配置在别处被改过；关闭即丢弃
const resetDraft = async () => {
	await quickEntry.load();
	draft.value = [...quickEntry.entries];
	keyword.value = '';
};

const pickedIds = computed(() => new Set(draft.value.map((v) => v.id)));

const toggle = (item: MenuOutput) => {
	if (pickedIds.value.has(item.id as number)) draft.value = draft.value.filter((v) => v.id !== item.id);
	else draft.value.push(item);
};

const move = (index: number, offset: number) => {
	const target = index + offset;
	if (target < 0 || target >= draft.value.length) return;
	const next = [...draft.value];
	[next[index], next[target]] = [next[target], next[index]];
	draft.value = next;
};

const removeAt = (index: number) => draft.value.splice(index, 1);

// 保存失败时不关抽屉，草稿留给用户重试
const submit = async () => {
	await quickEntry.save([...draft.value]);
	visible.value = false;
	ElMessage.success('设置常用成功');
};

// 命中片段高亮，避免用 v-html 拼接
const splitHit = (text: string, word: string) => {
	if (!word) return [{ text, hit: false }];
	const lower = text.toLowerCase();
	const segments: { text: string; hit: boolean }[] = [];
	let cursor = 0;
	for (;;) {
		const at = lower.indexOf(word, cursor);
		if (at < 0) {
			segments.push({ text: text.slice(cursor), hit: false });
			break;
		}
		if (at > cursor) segments.push({ text: text.slice(cursor, at), hit: false });
		segments.push({ text: text.slice(at, at + word.length), hit: true });
		cursor = at + word.length;
	}
	return segments.filter((v) => v.text);
};

type RowView = { id: number; item: MenuOutput; title: string; icon: string; dir: string; segments: { text: string; hit: boolean }[] };

const toRow = (item: MenuOutput, kw: string): RowView => ({
	id: item.id as number,
	item,
	title: titleOf(item),
	icon: iconOf(item),
	dir: dirOf(item),
	segments: splitHit(titleOf(item), kw),
});

const matchItem = (item: MenuOutput, groupTitle: string, kw: string) =>
	titleOf(item).toLowerCase().includes(kw) || (item.path ?? '').toLowerCase().includes(kw) || groupTitle.toLowerCase().includes(kw) || matchPinyin(titleOf(item), kw);

// 行视图只随关键词变化：勾选一条不会重算字符串切分，模板里只做 Set 查表
const displayGroups = computed(() => {
	const kw = keyword.value.trim().toLowerCase();
	if (!kw) return quickEntry.pool.groups.map((group) => ({ id: group.id, title: group.title, rows: group.items.map((item) => toRow(item, kw)) }));
	const items = quickEntry.pool.groups.flatMap((group) => group.items.filter((item) => matchItem(item, group.title, kw)));
	if (!items.length) return [];
	return [{ id: -1, title: `搜索结果（${items.length}）`, rows: items.map((item) => toRow(item, kw)) }];
});
</script>

<style scoped lang="scss">
.picker {
	display: grid;
	grid-template-columns: minmax(0, 1fr) 280px;
	gap: 12px;
	/* 撑满抽屉 body，否则 body 比 picker 高时会在底部留一条空白 */
	height: 100%;
}
.picker__pane {
	display: flex;
	flex-direction: column;
	min-width: 0;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background: var(--el-bg-color);
	overflow: hidden;
}
/* 已选栏贴内容高度，不再被拉成整高的空面板 */
.picker__pane--picked {
	align-self: start;
	max-height: 100%;
}
.picker__search {
	margin: 10px;
	/* .el-input 自带 width:100%，在 column 弹性盒里左右 margin 是「加在 100% 之外」的，
	   右侧会多出 10px 被面板裁掉（表现为搜索框过宽）。宽度交回弹性盒计算即可自动扣掉 margin */
	width: auto;
}
.picker__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 10px 12px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}
.picker__scroll {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	/* 不能有上内边距：sticky 的 top:0 落在内边距盒上沿，滚出去的行会从缝里露出来 */
	padding: 0 8px 10px;
}
.picker__footer {
	display: flex;
	justify-content: flex-end;
}
.app-group + .app-group {
	margin-top: 8px;
}
.app-group__title {
	position: sticky;
	top: 0;
	z-index: 1;
	margin: 0 -8px;
	padding: 8px 16px;
	background: var(--el-bg-color);
	border-bottom: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.app-row {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
	margin: 2px 0;
	padding: 7px 8px;
	border: none;
	border-radius: 6px;
	background: transparent;
	color: var(--el-text-color-primary);
	font-size: 13px;
	text-align: left;
	cursor: pointer;
	transition: background-color 0.15s;
	&:hover {
		background: var(--el-fill-color-light);
	}
	&:focus-visible {
		outline: 2px solid var(--el-color-primary);
		outline-offset: -2px;
	}
	&.is-picked {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
	}
}
.app-row__icon {
	flex: none;
}
.app-row__title {
	min-width: 0;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}
.app-row__group {
	flex: 1;
	min-width: 0;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.app-row__flag {
	flex: none;
	margin-left: auto;
	font-size: 14px;
	color: var(--el-text-color-secondary);
}
.app-row.is-picked .app-row__flag {
	color: var(--el-color-primary);
}
.hit {
	color: var(--el-color-warning);
	font-weight: 600;
}
.picked {
	flex: 1;
	min-height: 0;
	list-style: none;
	margin: 0;
	padding: 8px;
	overflow-y: auto;
}
.picked__item {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 6px 6px 6px 2px;
	margin-bottom: 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-fill-color-light);
	cursor: move;
}
.picked__handle {
	flex: none;
	font-size: 14px;
	color: var(--el-text-color-placeholder);
}
.picked__icon {
	flex: none;
	color: var(--el-color-primary);
}
.picked__text {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-width: 0;
}
.picked__title {
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 13px;
	line-height: 18px;
	color: var(--el-text-color-primary);
}
.picked__dir {
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 12px;
	line-height: 16px;
	color: var(--el-text-color-secondary);
}
.picked__ops {
	display: inline-flex;
	flex: none;
	align-items: center;
	/* 常驻占位、只切透明度：用 display 切换会让标题在 hover 时被挤动 */
	opacity: 0;
	pointer-events: none;
	transition: opacity 0.12s;
}
.picked__item:hover .picked__ops,
.picked__item:focus-within .picked__ops {
	opacity: 1;
	pointer-events: auto;
}
.picked__op {
	/* 用 flex 居中图标：button 默认按行盒基线排版，图标会比行中心偏高约 1.7px */
	display: flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border: none;
	border-radius: 4px;
	background: transparent;
	padding: 0;
	line-height: 1;
	cursor: pointer;
	color: var(--el-text-color-secondary);
	&:hover:not(:disabled) {
		color: var(--el-color-primary);
		background: var(--el-fill-color);
	}
	&:disabled {
		color: var(--el-text-color-disabled);
		cursor: not-allowed;
	}
}
.picked__empty {
	padding: 10px 12px 14px;
	font-size: 12px;
	text-align: center;
	color: var(--el-text-color-secondary);
}
.sortable-ghost {
	opacity: 0.35;
}
@media (max-width: 900px) {
	.picker {
		grid-template-columns: minmax(0, 1fr);
		/* 左栏吃掉剩余高度，右栏贴内容，否则底部会留一条空白 */
		grid-template-rows: minmax(0, 1fr) auto;
	}
	.picked {
		max-height: 30vh;
	}
}
</style>
