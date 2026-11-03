<template>
	<div class="sis-page" v-loading="state.isLoading">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero（右侧的指标面板与状态胶囊是本页特有内容，保留在页面内） -->
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
					<h2 class="page-hero__title">系统信息配置</h2>
					<p class="page-hero__desc">统一配置平台品牌标识、视觉主题、登录策略与备案信息，保存后对所有终端即时生效。</p>
				</div>
				<div class="page-hero__right">
					<div class="sis-metrics">
						<div class="sis-metric">
							<span class="sis-metric-k">主题色</span>
							<span class="sis-metric-v">
								<i class="sis-metric-dot" :style="{ background: state.sysInfo.themeColor || 'transparent' }"></i>
								{{ state.colorName || '未设置' }}
							</span>
						</div>
						<div class="sis-metric">
							<span class="sis-metric-k">布局模式</span>
							<span class="sis-metric-v">{{ currentLayoutLabel }}</span>
						</div>
						<div class="sis-metric">
							<span class="sis-metric-k">登录验证</span>
							<span class="sis-metric-v">{{ state.sysInfo.captcha === true ? '验证码已开' : '验证码已关' }}</span>
						</div>
					</div>
					<div class="sis-status" :class="{ 'is-dirty': isDirty }">
						<span class="sis-status-dot"></span>
						{{ isDirty ? '有未保存的修改' : '当前配置已同步' }}
					</div>
				</div>
			</div>
		</div>

		<div class="sis-body">
			<!-- 品牌标识 -->
			<section class="sis-card">
				<header class="sis-card-head">
					<div class="sis-card-icon" style="--c1: #3b82f6; --c2: #22d3ee">
						<el-icon><ele-School /></el-icon>
					</div>
					<div class="sis-card-titles">
						<h3>品牌标识</h3>
						<p>系统图标与主副标题，展示在登录页、浏览器标签与工作台</p>
					</div>
					<span class="sis-card-no">01</span>
				</header>
				<div class="sis-card-body">
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Picture /></el-icon><span>系统图标</span></div>
						<div class="sis-logo-row">
							<el-upload
								ref="uploadRef"
								class="avatar-uploader"
								:showFileList="false"
								:autoUpload="false"
								accept=".jpg,.png,.svg"
								action
								:limit="1"
								:onChange="handleUploadLogoChange"
							>
								<img v-if="state.sysInfo.logo" :src="state.sysInfo.logo" class="avatar" />
								<div v-else class="avatar-uploader-empty">
									<SvgIcon class="avatar-uploader-icon" name="ele-Plus" :size="28" />
									<span>上传图标</span>
								</div>
								<span v-if="state.sysInfo.logo" class="sis-logo-mask"><el-icon><ele-Refresh /></el-icon><i>更换</i></span>
							</el-upload>
							<div class="sis-logo-tip">
								<p>建议尺寸 200 × 200，透明底 PNG</p>
								<p>支持 jpg / png / svg，单张替换</p>
							</div>
						</div>
					</div>
					<div class="sis-field">
						<div class="sis-label"><el-icon><ele-Star /></el-icon><span>系统主标题</span></div>
						<el-input v-model="state.sysInfo.title" placeholder="请输入系统主标题" />
					</div>
					<div class="sis-field">
						<div class="sis-label"><el-icon><ele-StarFilled /></el-icon><span>系统副标题</span></div>
						<el-input v-model="state.sysInfo.viceTitle" placeholder="请输入系统副标题" />
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Guide /></el-icon><span>系统描述</span></div>
						<el-input v-model="state.sysInfo.viceDesc" type="textarea" :rows="2" placeholder="一句话描述平台定位" />
					</div>
					<div class="sis-field">
						<div class="sis-label"><el-icon><ele-Clock /></el-icon><span>版本号</span></div>
						<el-input v-model="state.sysInfo.version" placeholder="如 v2.1.0" />
					</div>
				</div>
			</section>

			<!-- 视觉与主题 -->
			<section class="sis-card">
				<header class="sis-card-head">
					<div class="sis-card-icon" style="--c1: #8b5cf6; --c2: #ec4899">
						<el-icon><ele-MagicStick /></el-icon>
					</div>
					<div class="sis-card-titles">
						<h3>视觉与主题</h3>
						<p>主题配色、界面布局与页面切换动画</p>
					</div>
					<span class="sis-card-no">02</span>
				</header>
				<div class="sis-card-body">
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Brush /></el-icon><span>主题颜色</span><em>影响按钮、菜单与高亮</em></div>
						<div class="sis-theme">
							<div class="sis-theme-chip" :style="{ background: state.sysInfo.themeColor || 'transparent' }">
								<el-icon><ele-Brush /></el-icon>
							</div>
							<div class="sis-theme-info">
								<div class="sis-theme-name">{{ state.colorName || '未设置' }}</div>
								<div class="sis-theme-hex">{{ state.sysInfo.themeColor || '—' }}</div>
							</div>
							<el-input v-model="state.sysInfo.themeColor" class="sis-theme-input" placeholder="#3B82F6" />
							<el-button type="primary" icon="ele-Grid" @click="state.dialogChineseColorVisible = true">传统色</el-button>
						</div>
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Menu /></el-icon><span>布局模式</span></div>
						<div class="sis-layout-picker">
							<div
								v-for="item in layoutOptions"
								:key="item.value"
								class="sis-layout-item"
								:class="{ 'is-active': state.sysInfo.layout === item.value }"
								@click="state.sysInfo.layout = item.value"
							>
								<div class="sis-layout-thumb" :class="`thumb-${item.value}`">
									<span class="t-side"></span>
									<span class="t-top"></span>
									<span class="t-main"></span>
									<el-icon class="t-check"><ele-Select /></el-icon>
								</div>
								<span class="sis-layout-name">{{ item.label }}</span>
							</div>
						</div>
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Promotion /></el-icon><span>页面动画</span></div>
						<el-input v-model="state.sysInfo.animation" placeholder="选择页面切换动画">
							<template #append>
								<el-button type="primary" icon="ele-Promotion" @click="handTransfromWindow">选择动画</el-button>
							</template>
						</el-input>
					</div>
				</div>
			</section>

			<!-- 安全与登录 -->
			<section class="sis-card">
				<header class="sis-card-head">
					<div class="sis-card-icon" style="--c1: #f59e0b; --c2: #ef4444">
						<el-icon><ele-Unlock /></el-icon>
					</div>
					<div class="sis-card-titles">
						<h3>安全与登录</h3>
						<p>登录校验策略与全局水印</p>
					</div>
					<span class="sis-card-no">03</span>
				</header>
				<div class="sis-card-body">
					<div class="sis-field sis-field-full">
						<div class="sis-switch-row" :class="{ 'is-on': state.sysInfo.captcha === true }">
							<div class="sis-switch-text">
								<div class="sis-label"><el-icon><ele-Picture /></el-icon><span>图形验证码</span></div>
								<p>登录时要求输入图形验证码，可抵御自动化爆破</p>
							</div>
							<el-switch v-model="state.sysInfo.captcha" active-text="启用" inactive-text="禁用" inline-prompt />
						</div>
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-switch-row" :class="{ 'is-on': state.sysInfo.secondVer === true }">
							<div class="sis-switch-text">
								<div class="sis-label"><el-icon><ele-Lock /></el-icon><span>登录二次验证</span></div>
								<p>登录成功后追加一次身份确认，适用于高安全场景</p>
							</div>
							<el-switch v-model="state.sysInfo.secondVer" active-text="启用" inactive-text="禁用" inline-prompt />
						</div>
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-Football /></el-icon><span>水印内容</span><em>留空则不显示水印</em></div>
						<el-input v-model="state.sysInfo.watermark" placeholder="若此处留空，则不显示水印" />
					</div>
				</div>
			</section>

			<!-- 备案与版权 -->
			<section class="sis-card">
				<header class="sis-card-head">
					<div class="sis-card-icon" style="--c1: #10b981; --c2: #84cc16">
						<el-icon><ele-Stamp /></el-icon>
					</div>
					<div class="sis-card-titles">
						<h3>备案与版权</h3>
						<p>租户入口地址、ICP 备案与版权声明</p>
					</div>
					<span class="sis-card-no">04</span>
				</header>
				<div class="sis-card-body">
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-User /></el-icon><span>租户标识</span></div>
						<div class="sis-tenant">
							<div class="sis-tenant-id">
								<span class="sis-tenant-k">Tenant ID</span>
								<span class="sis-tenant-v">{{ state.sysInfo.tenantId }}</span>
							</div>
							<div class="sis-link" :title="`点击复制：${tenantLoginUrl}`" @click="copyTenantUrl">
								<el-icon class="sis-link-ico"><ele-Link /></el-icon>
								<span class="sis-link-text">{{ tenantLoginUrl }}</span>
								<el-icon class="sis-link-copy"><ele-CopyDocument /></el-icon>
							</div>
						</div>
					</div>
					<div class="sis-field">
						<div class="sis-label"><el-icon><ele-Share /></el-icon><span>ICP备案号</span></div>
						<el-input v-model="state.sysInfo.icp" placeholder="如 晋ICP备00000000号" />
					</div>
					<div class="sis-field">
						<div class="sis-label"><el-icon><ele-Link /></el-icon><span>ICP地址</span></div>
						<el-input v-model="state.sysInfo.icpUrl" placeholder="https://beian.miit.gov.cn" />
					</div>
					<div class="sis-field sis-field-full">
						<div class="sis-label"><el-icon><ele-OfficeBuilding /></el-icon><span>版权说明</span></div>
						<el-input v-model="state.sysInfo.copyright" placeholder="如 © 2026 某某科技有限公司" />
					</div>
				</div>
			</section>

			<!-- 首页轮播图（当前版本不开放，整块置灰只读） -->
			<section class="sis-card sis-card-wide is-disabled">
				<header class="sis-card-head">
					<div class="sis-card-icon" style="--c1: #6366f1; --c2: #0ea5e9">
						<el-icon><ele-PictureFilled /></el-icon>
					</div>
					<div class="sis-card-titles">
						<h3>首页轮播图</h3>
						<p>建议 1920 × 600 横幅，支持 jpg / png / svg</p>
					</div>
					<span class="sis-card-no">05</span>
					<el-tag class="sis-card-tag" type="info" size="small" effect="plain">
						<el-icon><ele-Lock /></el-icon>暂不开放
					</el-tag>
				</header>
				<div class="sis-card-body">
					<div class="sis-disabled-note">
						<el-icon><ele-InfoFilled /></el-icon>
						<span>首页轮播图配置在当前版本中暂不开放，如需调整请联系平台管理员。</span>
					</div>
					<div class="sis-field sis-field-full">
						<el-upload
							disabled
							:file-list="state.carouselFileList"
							list-type="picture-card"
							:autoUpload="false"
							accept=".jpg,.png,.svg"
							:onChange="handleCarouselChange"
							:on-preview="handleCarouselPreview"
							:before-remove="handleCarouselRemove"
						>
							<div class="sis-upload-inner">
								<el-icon><ele-Plus /></el-icon>
								<span>添加图片</span>
							</div>
						</el-upload>
					</div>
				</div>
			</section>
		</div>

		<!-- 底部操作条：全页唯一的保存入口 -->
		<div class="sis-footer" :class="{ 'is-dirty': isDirty }">
			<div class="sis-footer-tip">
				<el-icon><ele-InfoFilled /></el-icon>
				<span>{{ isDirty ? '配置已修改，保存后立即生效' : '暂无修改，配置已与服务端同步' }}</span>
			</div>
			<div class="sis-footer-actions">
				<el-button size="large" icon="ele-RefreshLeft" @click="loadSysInfoData">重新加载</el-button>
				<el-button
					class="sis-save-btn"
					:class="{ 'is-idle': !isDirty }"
					:type="isDirty ? 'primary' : 'default'"
					size="large"
					icon="ele-SuccessFilled"
					:loading="state.isLoading"
					@click="saveSysInfo"
					>保存配置</el-button
				>
			</div>
		</div>

		<ChineseColorPicker v-model="state.sysInfo.themeColor" v-model:dialogVisible="state.dialogChineseColorVisible" @change="setThemeColor" />
		<Transform ref="transformRef" @getAnimation="getAnimation" />
	</div>
</template>

<!-- 系统设置 -->
<script setup lang="ts" name="sysInfoSetting">
import { nextTick, onMounted, reactive, ref, computed } from 'vue';
import { ElMessage, UploadFile, UploadFiles, UploadInstance } from 'element-plus';
import { loadSysInfo } from '/@/utils/sysInfo';
import chineseColorArr from '/@/layout/navBars/topBar/chineseColor.json';

import ChineseColorPicker from '/@/layout/navBars/topBar/chineseColorPicker.vue';
import Transform from './component/transform.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi, SysTenantApi } from '/@/api-services/system/api';

const transformRef = ref<InstanceType<typeof Transform>>();
const host = window.location.host;
const uploadRef = ref<UploadInstance>();
const state = reactive({
	isLoading: false,
	sysInfo: {} as any, // 系统信息数据
	logoFile: undefined as any, // logo上传文件
	dialogChineseColorVisible: false, // 中国传统颜色弹窗
	colorName: '飞燕草蓝', // 主题颜色名称
	carouselFileList: [] as any, // 轮播图文件列表
});

// 布局模式：卡片化选择，value 与后端约定保持一致
const layoutOptions = [
	{ label: '默认布局', value: 'defaults' },
	{ label: '经典布局', value: 'classic' },
	{ label: '横向布局', value: 'transverse' },
	{ label: '分栏布局', value: 'columns' },
];

// 租户登录入口地址
const tenantLoginUrl = computed(() => `${host}/#/login?tid=${state.sysInfo.tenantId}`);

// 顶部概览：当前布局模式的中文名
const currentLayoutLabel = computed(() => layoutOptions.find((i) => i.value === state.sysInfo.layout)?.label ?? '未设置');

// 变更检测：与「上次保存/加载」时的快照对比，仅用于顶部与底部状态提示
const snapshot = ref('');
const currentSnapshot = () => JSON.stringify({ info: state.sysInfo, carousel: state.carouselFileList?.map((f: any) => f.id ?? f.uid ?? f.name) ?? [] });
const isDirty = computed(() => snapshot.value !== '' && snapshot.value !== currentSnapshot());

// 页面初始化
onMounted(async () => {
	await loadSysInfoData();
});

// 加载系统信息（底部「重新加载」复用同一逻辑）
const loadSysInfoData = async () => {
	state.isLoading = true;
	try {
		state.sysInfo = await getAPI(SysTenantApi)
			.apiSysTenantSysInfoTenantIdGet(0)
			.then((res) => res.data.result ?? []);

		// 根据颜色Hex值获取名称
		state.colorName = getColorName(state.sysInfo.themeColor);

		if (state.sysInfo.carouselFiles) state.carouselFileList = state.sysInfo.carouselFiles;
		snapshot.value = currentSnapshot();
	} finally {
		nextTick(() => {
			state.isLoading = false;
		});
	}
};

const getColorName = (hex: string) => {
	if (!hex) return '';
	const target = hex.trim().toLowerCase();
	return chineseColorArr.find((i: any) => i.hex.toLowerCase() === target)?.name ?? hex;
};

// 复制租户登录地址
const copyTenantUrl = async () => {
	try {
		await navigator.clipboard.writeText(tenantLoginUrl.value);
		ElMessage.success('访问地址已复制');
	} catch {
		ElMessage.warning('复制失败，请手动复制');
	}
};

// 打开选择动画
const handTransfromWindow = () => {
	transformRef.value?.openDialog({ name: state.sysInfo.animation });
};

// 动画返回
const getAnimation = (e: any) => {
	if (e.name) state.sysInfo.animation = e.value;
};

// 保存
const saveSysInfo = async () => {
	try {
		state.isLoading = true;
		if (state.logoFile != undefined && state.logoFile.raw != undefined) {
			var res = await getAPI(SysFileApi).apiSysFileUploadLogoTenantIdPostForm(state.sysInfo.tenantId, state.logoFile.raw);
			state.sysInfo.logo = res.data.result?.url;
		}
		if (state.carouselFileList != undefined && state.carouselFileList.length > 0) {
			// 已有的轮播图
			state.sysInfo.carouselFileIds = state.carouselFileList.filter((file: any) => file.raw == undefined && file.id != undefined).map((file: any) => file.id);

			// 新增的轮播图
			var carouselFiles = state.carouselFileList.filter((file: any) => file.raw != undefined).map((file: any) => file.raw);
			if (carouselFiles.length > 0) {
				var sysFileIds = await getAPI(SysFileApi)
					.apiSysFileUploadCarouselTenantIdPostForm(state.sysInfo.tenantId, carouselFiles)
					.then((res) => res.data.result?.map((file: any) => file.id) ?? []);
				state.sysInfo.carouselFileIds.push(...sysFileIds);
			}
		}
		await getAPI(SysTenantApi).apiSysTenantSaveSysInfoPost(state.sysInfo);
		ElMessage.success('保存成功');

		// 置空上传文件
		state.logoFile = undefined;

		// 更新系统信息
		await loadSysInfo(state.sysInfo.tenantId);

		// 保存成功即视为已同步，重置变更快照
		snapshot.value = currentSnapshot();
	} finally {
		nextTick(() => {
			state.isLoading = false;
		});
	}
};

// 上传系统logo
const handleUploadLogoChange = (file: any) => {
	uploadRef.value!.clearFiles();
	// 释放上一次本地预览占用的 blob URL，避免反复更换图标时残留内存
	const prev = state.sysInfo.logo;
	if (typeof prev === 'string' && prev.startsWith('blob:')) URL.revokeObjectURL(prev);
	state.logoFile = file;
	state.sysInfo.logo = URL.createObjectURL(state.logoFile.raw); // 显示预览logo
};

// 上传轮播图文件
const handleCarouselChange = async (file: UploadFile, files: UploadFiles) => {
	state.carouselFileList = files;
};

// 删除轮播图文件
const handleCarouselRemove = (file: UploadFile) => {
	let index = state.carouselFileList.indexOf(file);
	state.carouselFileList.splice(index, 1);
};

// 预览轮播图
const handleCarouselPreview = (file: UploadFile) => {
	window.open(file.url);
};

// 选择主题色
const setThemeColor = (e: any) => {
	navigator.clipboard.writeText(e.hex);
	state.sysInfo.themeColor = e.hex;
	state.colorName = e.name;
	state.dialogChineseColorVisible = false;
};
</script>

<style lang="scss" scoped>
.sis-page {
	position: relative;
	padding-bottom: 4px;
}

/* 顶部头图统一走 theme/hero.scss 的 .page-hero，本页不再复写
 * 但 __right 本身是块级容器：里面两个块级子项会各占一行，
 * 「当前配置已同步」胶囊会被挤到指标面板下面。这里改成 flex 行，让两者同排右对齐 */
.page-hero__right {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 10px;
	flex-wrap: wrap;
}

.sis-status {
	flex: 0 0 auto;
	display: inline-flex;
	align-items: center;
	gap: 7px;
	font-size: 12px;
	padding: 5px 12px;
	border-radius: 999px;
	background: color-mix(in srgb, var(--el-color-white) 16%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-white) 28%, transparent);
	white-space: nowrap;
}

.sis-status-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: var(--el-color-white);
	box-shadow: 0 0 0 3px color-mix(in srgb, var(--el-color-white) 25%, transparent);
}

.sis-status.is-dirty {
	background: color-mix(in srgb, var(--el-color-warning) 32%, transparent);
	border-color: color-mix(in srgb, var(--el-color-white) 45%, transparent);

	.sis-status-dot {
		background: var(--el-color-warning-light-3);
		animation: sisPulse 1.6s ease-in-out infinite;
	}
}

.sis-metrics {
	display: flex;
	align-items: stretch;
	gap: 10px;
	padding: 10px 12px;
	border-radius: 12px;
	background: color-mix(in srgb, var(--el-color-black) 14%, transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-white) 16%, transparent);
	backdrop-filter: blur(2px);
}

.sis-metric {
	display: flex;
	flex-direction: column;
	gap: 3px;
	padding: 0 10px;
	min-width: 92px;

	+ .sis-metric {
		border-left: 1px solid color-mix(in srgb, var(--el-color-white) 18%, transparent);
	}
}

.sis-metric-k {
	font-size: 11px;
	opacity: 0.7;
}

.sis-metric-v {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	font-size: 13px;
	font-weight: 600;
	white-space: nowrap;
}

.sis-metric-dot {
	width: 10px;
	height: 10px;
	border-radius: 50%;
	border: 1px solid color-mix(in srgb, var(--el-color-white) 60%, transparent);
	box-shadow: 0 0 6px color-mix(in srgb, var(--el-color-white) 45%, transparent);
}

/* ===================== 主体卡片 ===================== */
.sis-body {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(430px, 1fr));
	gap: 16px;
	margin-top: 16px;
}

.sis-card {
	position: relative;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 14px;
	padding: 18px 20px 20px;
	transition: transform 0.28s ease, box-shadow 0.28s ease, border-color 0.28s ease;
	animation: sisRise 0.5s ease both;
	overflow: hidden;

	&::before {
		content: '';
		position: absolute;
		inset: 0 0 auto;
		height: 2px;
		background: linear-gradient(90deg, var(--c1, var(--el-color-primary)), var(--c2, var(--el-color-primary)));
		opacity: 0;
		transition: opacity 0.28s ease;
	}

	&:hover {
		transform: translateY(-2px);
		border-color: color-mix(in srgb, var(--el-color-primary) 35%, var(--el-border-color-lighter));
		box-shadow: 0 12px 28px -18px color-mix(in srgb, var(--el-color-primary) 70%, transparent);

		&::before {
			opacity: 1;
		}
	}
}

.sis-body .sis-card:nth-child(1) {
	animation-delay: 0.04s;
}
.sis-body .sis-card:nth-child(2) {
	animation-delay: 0.1s;
}
.sis-body .sis-card:nth-child(3) {
	animation-delay: 0.16s;
}
.sis-body .sis-card:nth-child(4) {
	animation-delay: 0.22s;
}
.sis-body .sis-card:nth-child(5) {
	animation-delay: 0.28s;
}

.sis-card-wide {
	grid-column: 1 / -1;
}

.sis-card-head {
	display: flex;
	align-items: center;
	gap: 12px;
	padding-bottom: 14px;
	margin-bottom: 16px;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}

.sis-card-icon {
	width: 40px;
	height: 40px;
	border-radius: 11px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	font-size: 20px;
	/* 装饰规范：浅色同色系底 + 深色图标（不再用渐变实底 + 白图标） */
	color: var(--c1, var(--el-color-primary));
	background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 13%, transparent);
	transition: transform 0.3s ease;

	.sis-card:hover & {
		transform: rotate(-8deg) scale(1.06);
	}
}

.sis-card-titles {
	flex: 1;
	min-width: 0;

	h3 {
		margin: 0;
		font-size: 15px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	p {
		margin: 3px 0 0;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

.sis-card-body {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
	gap: 16px;
}

.sis-field-full {
	grid-column: 1 / -1;
}

.sis-label {
	display: flex;
	align-items: center;
	gap: 5px;
	font-size: 13px;
	color: var(--el-text-color-regular);
	margin-bottom: 7px;

	.el-icon {
		font-size: 14px;
		color: var(--el-color-primary);
	}

	em {
		font-style: normal;
		font-size: 11px;
		color: var(--el-text-color-placeholder);
		margin-left: 4px;
	}
}

/* 字段获得焦点时标签联动高亮 */
.sis-field:focus-within .sis-label {
	color: var(--el-color-primary);

	em {
		color: var(--el-color-primary-light-3);
	}
}

/* 表单控件统一动效 */
.sis-card-body :deep(.el-input__wrapper),
.sis-card-body :deep(.el-textarea__inner) {
	transition: box-shadow 0.25s ease, background 0.25s ease;
}

.sis-field:hover :deep(.el-input__wrapper) {
	box-shadow: 0 0 0 1px var(--el-color-primary-light-5) inset;
}

.sis-card-body :deep(.el-input__wrapper.is-focus) {
	box-shadow: 0 0 0 1px var(--el-color-primary) inset, 0 0 0 3px color-mix(in srgb, var(--el-color-primary) 14%, transparent);
}

/* 卡片右上角的序号与状态标签 */
.sis-card-no {
	margin-left: auto;
	align-self: flex-start;
	font-size: 20px;
	font-weight: 800;
	font-style: italic;
	letter-spacing: -1px;
	color: var(--el-text-color-placeholder);
	opacity: 0.45;
	transition: color 0.28s ease, opacity 0.28s ease;

	.sis-card:hover & {
		color: var(--el-color-primary);
		opacity: 0.75;
	}
}

.sis-card-tag {
	margin-left: 10px;
	align-self: flex-start;
	display: inline-flex;
	align-items: center;
	gap: 4px;
	border-radius: 999px;
}

/* ---------- 禁用态整块（当前版本不开放的分区） ---------- */
.sis-card.is-disabled {
	background: var(--el-fill-color-lighter);
	border-style: dashed;

	&::before {
		display: none;
	}

	&:hover {
		transform: none;
		box-shadow: none;
		border-color: var(--el-border-color-lighter);
	}

	.sis-card-icon {
		background: var(--el-text-color-placeholder);
		box-shadow: none;

		.sis-card:hover & {
			transform: none;
		}
	}

	.sis-card-titles h3,
	.sis-card-titles p {
		color: var(--el-text-color-secondary);
	}

	.sis-card-no {
		opacity: 0.3;

		.sis-card:hover & {
			color: var(--el-text-color-placeholder);
			opacity: 0.3;
		}
	}

	.sis-card-body {
		opacity: 0.5;
		filter: grayscale(1);
	}

	// 彻底断掉交互：预览、删除按钮一并失效
	:deep(.el-upload),
	:deep(.el-upload-list__item) {
		pointer-events: none;
	}
}

.sis-disabled-note {
	grid-column: 1 / -1;
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 9px 12px;
	border-radius: 10px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color);
	border: 1px dashed var(--el-border-color);

	.el-icon {
		font-size: 14px;
		color: var(--el-color-info);
	}
}

/* ---------- 系统图标 ---------- */
.sis-logo-row {
	display: flex;
	align-items: center;
	gap: 18px;
}

.avatar-uploader {
	:deep(.el-upload) {
		position: relative;
		width: 100px;
		height: 100px;
		border: 1px dashed var(--el-border-color);
		border-radius: 12px;
		cursor: pointer;
		overflow: hidden;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		background: var(--el-fill-color-lighter);
		transition: border-color 0.25s ease, transform 0.25s ease;
	}

	:deep(.el-upload:hover) {
		border-color: var(--el-color-primary);
		transform: translateY(-1px);
	}

	:deep(.el-upload:hover) .sis-logo-mask {
		opacity: 1;
	}
}

.avatar-uploader .avatar {
	width: 100px;
	height: 100px;
	display: block;
	object-fit: contain;
}

.avatar-uploader-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 4px;
	color: var(--el-text-color-placeholder);
	font-size: 12px;
}

.avatar-uploader-icon {
	color: var(--el-text-color-placeholder);
}

.sis-logo-mask {
	position: absolute;
	inset: 0;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 3px;
	color: #fff;
	font-size: 12px;
	background: rgba(0, 0, 0, 0.45);
	opacity: 0;
	transition: opacity 0.25s ease;

	i {
		font-style: normal;
	}
}

.sis-logo-tip {
	p {
		margin: 0;
		font-size: 12px;
		line-height: 1.8;
		color: var(--el-text-color-secondary);
	}
}

/* ---------- 主题色 ---------- */
.sis-theme {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;
	padding: 12px;
	border-radius: 12px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
}

.sis-theme-chip {
	position: relative;
	width: 46px;
	height: 46px;
	border-radius: 12px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	color: #fff;
	font-size: 18px;
	overflow: hidden;
	box-shadow: 0 6px 14px -8px rgba(0, 0, 0, 0.5);

	&::after {
		content: '';
		position: absolute;
		inset: 0;
		background: linear-gradient(135deg, rgba(255, 255, 255, 0.35), transparent 60%);
	}
}

.sis-theme-info {
	min-width: 92px;
}

.sis-theme-name {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.sis-theme-hex {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	font-family: var(--el-font-family-code, monospace);
}

.sis-theme-input {
	width: 140px;
}

/* ---------- 布局模式 ---------- */
.sis-layout-picker {
	display: grid;
	// 固定两列均分：4 个布局正好排成 2 × 2
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 12px;
}

.sis-layout-item {
	cursor: pointer;
	padding: 10px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
	text-align: center;
	transition: transform 0.25s ease, border-color 0.25s ease, box-shadow 0.25s ease;

	&:hover {
		transform: translateY(-2px);
		border-color: var(--el-color-primary-light-5);
	}

	&.is-active {
		border-color: var(--el-color-primary);
		background: color-mix(in srgb, var(--el-color-primary) 8%, var(--el-bg-color));
		box-shadow: 0 8px 18px -14px var(--el-color-primary);
	}
}

.sis-layout-thumb {
	position: relative;
	width: 100%;
	height: 44px;
	border-radius: 7px;
	overflow: hidden;
	background: var(--el-bg-color-overlay, var(--el-bg-color));
	border: 1px solid var(--el-border-color-light);

	span {
		position: absolute;
		background: color-mix(in srgb, var(--el-text-color-primary) 20%, transparent);
		border-radius: 2px;
		transition: background 0.25s ease;
	}

	.t-check {
		display: none;
		position: absolute;
		right: 3px;
		bottom: 2px;
		font-size: 13px;
		color: var(--el-color-primary);
		background: transparent;
	}
}

/* 四种布局的缩略示意：仅用色块位置表达结构差异 */
.sis-layout-thumb.thumb-defaults {
	.t-side {
		left: 3px;
		top: 3px;
		bottom: 3px;
		width: 26%;
	}
	.t-top {
		left: 31%;
		right: 3px;
		top: 3px;
		height: 24%;
	}
	.t-main {
		left: 31%;
		right: 3px;
		top: 31%;
		bottom: 3px;
	}
}

.sis-layout-thumb.thumb-classic {
	.t-side {
		left: 3px;
		top: 31%;
		bottom: 3px;
		width: 26%;
	}
	.t-top {
		left: 3px;
		right: 3px;
		top: 3px;
		height: 24%;
	}
	.t-main {
		left: 31%;
		right: 3px;
		top: 31%;
		bottom: 3px;
	}
}

.sis-layout-thumb.thumb-transverse {
	.t-side {
		display: none;
	}
	.t-top {
		left: 3px;
		right: 3px;
		top: 3px;
		height: 30%;
	}
	.t-main {
		left: 3px;
		right: 3px;
		top: 37%;
		bottom: 3px;
	}
}

.sis-layout-thumb.thumb-columns {
	.t-side {
		left: 3px;
		top: 3px;
		bottom: 3px;
		width: 16%;
	}
	.t-top {
		left: 21%;
		right: 3px;
		top: 3px;
		height: 20%;
	}
	.t-main {
		left: 21%;
		right: 3px;
		top: 26%;
		bottom: 3px;
	}

	// 分栏布局在主区内再补一条窄栏，区别于默认布局
	.t-main::after {
		content: '';
		position: absolute;
		left: 6%;
		top: 0;
		bottom: 0;
		width: 24%;
		border-radius: 2px;
		background: color-mix(in srgb, var(--el-text-color-primary) 12%, transparent);
	}
}

.sis-layout-item:hover .sis-layout-thumb span {
	background: color-mix(in srgb, var(--el-color-primary) 35%, transparent);
}

.sis-layout-item.is-active .sis-layout-thumb span {
	background: color-mix(in srgb, var(--el-color-primary) 55%, transparent);
}

.sis-layout-item.is-active .sis-layout-thumb .t-check {
	display: block;
}

.sis-layout-name {
	display: block;
	margin-top: 8px;
	font-size: 12px;
	color: var(--el-text-color-regular);
}

.sis-layout-item.is-active .sis-layout-name {
	color: var(--el-color-primary);
	font-weight: 600;
}

/* ---------- 开关行 ---------- */
.sis-switch-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	padding: 12px 14px;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-lighter);
	transition: border-color 0.25s ease, background 0.25s ease;

	&.is-on {
		border-color: color-mix(in srgb, var(--el-color-success) 45%, var(--el-border-color-lighter));
		background: color-mix(in srgb, var(--el-color-success) 7%, var(--el-fill-color-lighter));
	}

	.sis-label {
		margin-bottom: 0;
	}

	p {
		margin: 4px 0 0;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

/* ---------- 租户与地址 ---------- */
.sis-tenant {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;
}

.sis-tenant-id {
	display: inline-flex;
	align-items: baseline;
	gap: 8px;
	padding: 8px 14px;
	border-radius: 10px;
	background: linear-gradient(135deg, color-mix(in srgb, var(--el-color-primary) 16%, transparent), transparent);
	border: 1px solid color-mix(in srgb, var(--el-color-primary) 28%, transparent);
}

.sis-tenant-k {
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

.sis-tenant-v {
	font-size: 18px;
	font-weight: 700;
	color: var(--el-color-primary);
}

.sis-link {
	display: inline-flex;
	align-items: center;
	gap: 7px;
	max-width: 100%;
	padding: 8px 12px;
	border-radius: 10px;
	border: 1px dashed var(--el-border-color);
	color: var(--el-text-color-regular);
	font-size: 12px;
	cursor: pointer;
	transition: color 0.25s ease, border-color 0.25s ease, background 0.25s ease;

	&:hover {
		color: var(--el-color-primary);
		border-color: var(--el-color-primary);
		background: color-mix(in srgb, var(--el-color-primary) 6%, transparent);

		.sis-link-copy {
			transform: scale(1.12);
		}
	}
}

.sis-link-ico {
	font-size: 14px;
}

.sis-link-text {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.sis-link-copy {
	font-size: 14px;
	transition: transform 0.25s ease;
}

/* ---------- 轮播图上传 ---------- */
.sis-upload-inner {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 4px;
	color: var(--el-text-color-placeholder);
	font-size: 12px;

	.el-icon {
		font-size: 20px;
	}
}

.sis-card :deep(.el-upload--picture-card) {
	width: 104px;
	height: 104px;
	border-radius: 12px;
	transition: border-color 0.25s ease, transform 0.25s ease;

	&:hover {
		border-color: var(--el-color-primary);
		transform: translateY(-2px);
	}
}

.sis-card :deep(.el-upload-list--picture-card .el-upload-list__item) {
	width: 104px;
	height: 104px;
	border-radius: 12px;
}

/* ===================== 底部操作条 ===================== */
.sis-footer {
	position: sticky;
	bottom: 0;
	z-index: 5;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 16px;
	flex-wrap: wrap;
	margin-top: 16px;
	padding: 12px 18px;
	border-radius: 14px;
	background: color-mix(in srgb, var(--el-bg-color) 88%, transparent);
	backdrop-filter: blur(8px);
	border: 1px solid var(--el-border-color-lighter);
	// 阴影默认不显示，鼠标移入时才浮起，避免常驻压在内容上
	box-shadow: none;
	transition: box-shadow 0.3s ease, border-color 0.3s ease;

	&:hover {
		box-shadow: 0 -6px 24px -18px rgba(0, 0, 0, 0.5);
	}
}

.sis-footer.is-dirty {
	border-color: color-mix(in srgb, var(--el-color-primary) 42%, var(--el-border-color-lighter));

	&:hover {
		box-shadow: 0 -8px 26px -18px color-mix(in srgb, var(--el-color-primary) 85%, transparent);
	}

	.sis-footer-tip,
	.sis-footer-tip .el-icon {
		color: var(--el-color-primary);
	}
}

.sis-footer-tip {
	display: inline-flex;
	align-items: center;
	gap: 7px;
	font-size: 12px;
	color: var(--el-text-color-secondary);

	.el-icon {
		color: var(--el-color-info);
	}
}

.sis-footer-actions {
	display: flex;
	gap: 10px;
}

.sis-save-btn {
	position: relative;
	overflow: hidden;
	transition: transform 0.25s ease, box-shadow 0.25s ease, opacity 0.25s ease;

	&:hover {
		transform: translateY(-1px);
	}

	// 无改动时弱化保存入口（仍可点击，避免误以为按钮失效）
	&.is-idle {
		opacity: 0.7;

		&::after {
			display: none;
		}
	}

	&::after {
		content: '';
		position: absolute;
		top: 0;
		left: -60%;
		width: 40%;
		height: 100%;
		background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.4), transparent);
		transform: skewX(-20deg);
		transition: left 0.6s ease;
	}

	&:hover::after {
		left: 120%;
	}
}

/* ===================== 动画 ===================== */
@keyframes sisRise {
	from {
		opacity: 0;
		transform: translateY(10px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@keyframes sisPulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
	}
	50% {
		opacity: 0.45;
		transform: scale(0.8);
	}
}

@media (max-width: 768px) {
	.sis-body {
		grid-template-columns: 1fr;
	}
}
</style>
