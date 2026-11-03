<template>
	<div class="agents-skill-tool-container">
		<!-- 顶部头图：同一组件被工具 / 技能 / MCP 三个菜单复用，标题与说明随 fixedType 切换 -->
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
					<h2 class="page-hero__title">{{ heroMeta.title }}</h2>
					<p class="page-hero__desc">{{ heroMeta.desc }}</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ state.total }}</span>
							<span class="page-hero__stat-k">总数</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-off">{{ heroStats.builtin }}</span>
							<span class="page-hero__stat-k">本页内置</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ heroStats.active }}</span>
							<span class="page-hero__stat-k">本页启用</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 查询区：查询条件与操作按钮同一行（新增按钮放这里，不进卡片区，避免随卡片滚动） -->
		<div class="query-toolbar">
			<el-form :model="state.queryParams" :inline="true" :show-message="false" label-width="auto" class="query-form" @submit.prevent>
				<!-- 固定类型模式下不显示类型筛选 -->
				<el-form-item v-if="!props.fixedType" label="类型">
					<el-select v-model="state.queryParams.parameter" class="query-select" placeholder="全部类型" clearable>
						<el-option label="工具 Tool" :value="1" />
						<el-option label="技能 Skill" :value="2" />
						<el-option label="MCP" :value="3" />
					</el-select>
				</el-form-item>
				<el-form-item label="关键字">
					<el-input v-model="state.queryParams.searchKey" class="query-input" placeholder="名称 / 描述" clearable @keyup.enter="handleQuery" />
				</el-form-item>
			</el-form>
			<div class="query-actions">
				<el-button-group>
					<el-button type="primary" icon="ele-Search" @click="handleQuery" :loading="state.loading"> 查询 </el-button>
					<el-button icon="ele-Refresh" @click="resetQuery"> 重置 </el-button>
				</el-button-group>
				<el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'agent/skillTool/addEdit'"> {{ addButtonText }} </el-button>
			</div>
		</div>

		<el-card class="full-table" shadow="hover">
			<div v-loading="state.loading" class="card-grid">
				<el-empty :image-size="160" :description="emptyText" v-if="!state.tableData.length" />
				<el-row v-else :gutter="12" class="card-grid-row">
					<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="row in state.tableData" :key="row.id">
						<el-card shadow="never" class="entity-card entity-card--accent" :class="`is-${stateAccent(row)}`">
							<i class="entity-card__watermark" :class="watermarkIcon(row.skillToolType)" />

							<div class="entity-card__head">
								<div class="entity-card__icon">
									<i :class="watermarkIcon(row.skillToolType)" />
								</div>
								<div class="entity-card__title-wrap">
									<div class="entity-card__title">{{ row.name }}</div>
									<div class="entity-card__sub">
										{{ typeText(row.skillToolType) }}<template v-if="!props.fixedType"> · {{ row.activeStatus === 1 ? '已启用' : '已停用' }}</template>
									</div>
								</div>
								<!-- 标签配色是整张卡色彩的来源：左边框 / hover 阴影都跟着它走 -->
								<el-tag size="small" type="warning" effect="light" v-if="row.isSystem">内置</el-tag>
								<el-tag size="small" v-else :type="row.activeStatus === 1 ? 'success' : 'info'" effect="light">{{ row.activeStatus === 1 ? '启用' : '停用' }}</el-tag>
							</div>

							<div class="entity-card__body">
								<div class="card-desc">{{ row.description || '暂无描述' }}</div>

								<!-- 方法映射是"这个工具实际调用哪个后端能力"，对工具类最关键，单独给一块等宽展示 -->
								<div class="code-preview tool-method" v-if="row.skillToolType === 1 && row.classMethod">{{ row.classMethod }}</div>

								<!-- MCP：地址与"勾了多少工具"是运行时真正生效的两项，比描述更需要露出来 -->
								<template v-if="row.skillToolType === 3">
									<div class="code-preview tool-method" v-if="row.mcpUrl">{{ row.mcpType }} · {{ row.mcpUrl }}</div>
									<div class="metric-group">
										<div class="metric-item">
											<span>已启用工具</span><b>{{ parseJsonArray(row.mcpSelectedTools).length }}</b>
										</div>
										<div class="metric-item">
											<span>服务提供</span><b>{{ parseJsonTools(row.mcpTools).length }}</b>
										</div>
									</div>
								</template>
							</div>

							<div class="entity-card__foot">
								<span class="cell-sub">{{ row.isSystem ? '内置能力，不可修改' : '可自定义配置' }}</span>
								<div class="entity-card__actions">
									<!-- 内置 MCP：名称/地址不可改，但必须能测试连接与挑工具，所以单独给一个「配置工具」入口 -->
									<el-button v-if="row.skillToolType === 3 && row.isSystem" icon="ele-Connection" text type="primary" @click="handleConfigMcp(row)" v-auth="'agent/skillTool/addEdit'">
										配置工具
									</el-button>
									<el-button v-else icon="ele-Edit" text type="primary" :disabled="row.isSystem" @click="handleEdit(row)" v-auth="'agent/skillTool/addEdit'"> 编辑 </el-button>
									<el-button icon="ele-Delete" text type="danger" :disabled="row.isSystem" @click="handleDelete(row)" v-auth="'agent/skillTool/delete'"> 删除 </el-button>
								</div>
							</div>
						</el-card>
					</el-col>
				</el-row>
			</div>

			<el-pagination
				class="table-pager"
				background
				v-model:current-page="state.queryParams.page"
				v-model:page-size="state.queryParams.pageSize"
				:page-sizes="[10, 20, 50, 100]"
				layout="total, sizes, prev, pager, next, jumper"
				:total="state.total"
				@change="handleQuery"
			/>
		</el-card>

		<!-- 新增/编辑 -->
		<el-dialog v-model="state.editVisible" draggable :close-on-click-modal="false" width="800px">
			<template #header>
				<div class="dialog-title">
					<el-icon size="16"><ele-Edit /></el-icon>
					<span>{{ state.editTitle }}</span>
				</div>
			</template>
			<el-form :model="state.form" ref="editFormRef" :rules="rules" label-width="auto">
				<!-- 固定类型模式下不显示类型选择 -->
				<el-form-item v-if="!props.fixedType" label="技能工具类型" prop="skillToolType">
					<el-select v-model="state.form.skillToolType" style="width: 100%" @change="onTypeChange">
						<el-option label="工具 Tool（调用系统内置能力）" :value="1" />
						<el-option label="技能 Skill（上传技能包，由脚本实现）" :value="2" />
						<el-option label="MCP（对接外部 MCP 服务）" :value="3" />
					</el-select>
				</el-form-item>
				<el-form-item label="名称" prop="name">
					<el-input v-model="state.form.name" placeholder="工具/技能名称，同名同类型不可重复" maxlength="100" clearable :disabled="state.form.isSystem" />
				</el-form-item>
				<el-form-item label="描述">
					<el-input v-model="state.form.description" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="描述会作为工具说明提供给模型" :disabled="!!state.form.isSystem" />
				</el-form-item>
				<!-- 工具/技能的启用开关；MCP 的启用开关在下方与 MCP 类型并排 -->
				<el-form-item label="是否启用" v-if="state.form.skillToolType !== 3">
					<el-switch v-model="state.form.activeStatus" :active-value="1" :inactive-value="0" :disabled="!!state.form.isSystem" />
				</el-form-item>

				<!-- 工具 -->
				<template v-if="state.form.skillToolType === 1">
					<el-form-item label="方法映射" prop="classMethod">
						<el-select v-model="state.form.classMethod" placeholder="请选择要暴露给模型的方法" filterable style="width: 100%">
							<el-option v-for="item in toolMethodOptions" :key="item.value" :label="item.label" :value="item.value" />
						</el-select>
					</el-form-item>
				</template>

				<!-- 技能 -->
				<template v-if="state.form.skillToolType === 2">
					<el-form-item label="技能包" :required="!state.form.id">
						<el-upload :auto-upload="false" :limit="1" :show-file-list="true" :on-change="onZipChange" :file-list="state.zipFileList" accept=".zip">
							<el-button icon="ele-Upload"> 选择技能包 </el-button>
						</el-upload>
						<div class="tip">技能包需为 zip 格式，解压后目录结构为 {技能名}/{技能名}/SKILL.md；保存时会完成安全校验并部署到 Skills 目录。</div>
						<div class="tip" v-if="state.form.skillFile?.fileName">当前已上传：{{ state.form.skillFile.fileName }}</div>
					</el-form-item>
				</template>

				<!-- MCP -->
				<template v-if="state.form.skillToolType === 3">
					<el-row :gutter="10">
						<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item label="MCP类型" prop="mcpType">
								<el-select v-model="state.form.mcpType" style="width: 100%" :disabled="!!state.form.isSystem">
									<el-option label="https（StreamableHttp）" value="https" />
									<el-option label="http（StreamableHttp）" value="http" />
									<el-option label="sse" value="sse" />
									<el-option label="stdio（本地进程）" value="stdio" />
								</el-select>
							</el-form-item>
						</el-col>
						<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
							<el-form-item label="是否启用">
								<el-switch v-model="state.form.activeStatus" :active-value="1" :inactive-value="0" />
							</el-form-item>
						</el-col>
					</el-row>
					<el-form-item label="MCP地址" v-if="state.form.mcpType !== 'stdio'">
						<el-input v-model="state.form.mcpUrl" placeholder="如：https://example.com/mcp" clearable :disabled="!!state.form.isSystem" />
					</el-form-item>
					<el-form-item label="请求头" v-if="state.form.mcpType !== 'stdio'">
						<el-input v-model="state.form.mcpHeaders" type="textarea" :rows="8" placeholder='如：{"X-Api-Key":"xxx"}；Authorization 会自动透传当前登录凭证' :disabled="!!state.form.isSystem" />
					</el-form-item>
					<el-form-item label="命令" v-if="state.form.mcpType === 'stdio'">
						<el-input v-model="state.form.mcpCommand" placeholder="如：npx" clearable />
					</el-form-item>
					<el-form-item label="参数" v-if="state.form.mcpType === 'stdio'">
						<el-input v-model="state.form.mcpArguments" placeholder="逗号分隔，如：-y,@modelcontextprotocol/server-filesystem,/tmp" clearable />
					</el-form-item>
					<el-form-item label="环境变量" v-if="state.form.mcpType === 'stdio'">
						<el-input v-model="state.form.mcpEnvironment" type="textarea" :rows="2" placeholder='如：{"KEY":"VALUE"}' />
					</el-form-item>
					<el-form-item label="MCP工具">
						<el-button :loading="state.testingMcp" @click="handleTestMcp">测试连接</el-button>
						<div class="tip">先测试连接获取工具清单，再勾选需要加载的工具（AI 运行时仅加载勾选的工具；内置 MCP 的名称与地址不可修改，仅可配置工具与启用状态）</div>
						<!-- 旧数据只存了工具名，打开弹窗时自动补一次清单，这里给个进度提示，避免用户以为卡住了 -->
						<div class="tip mcp-sync-tip" v-if="state.mcpAutoSyncing">
							<el-icon class="is-loading"><ele-Loading /></el-icon>
							正在同步工具清单与中文名称…
						</div>
						<!-- 平台内置 MCP 会返回数百个工具，没有搜索根本挑不出来，所以清单上方加过滤与批量操作 -->
						<div v-if="state.mcpToolList.length" class="mcp-tool-panel">
							<div class="mcp-tool-bar">
								<el-input v-model="state.mcpToolKeyword" class="mcp-tool-search" placeholder="搜索工具名 / 中文名，如 sysUser / 用户" clearable size="small">
									<template #prefix
										><el-icon><ele-Search /></el-icon
									></template>
								</el-input>
								<span class="mcp-tool-count">
									已选 <b>{{ state.mcpSelected.length }}</b> / 共 {{ state.mcpToolList.length }}
									<template v-if="state.mcpToolKeyword">（筛出 {{ filteredMcpTools.length }}）</template>
								</span>
								<div class="mcp-tool-actions">
									<el-button link type="primary" size="small" @click="selectFilteredTools(true)">全选当前</el-button>
									<el-button link type="info" size="small" @click="selectFilteredTools(false)">取消当前</el-button>
								</div>
							</div>
							<div v-if="filteredMcpTools.length" class="mcp-tool-list">
								<div v-for="tool in filteredMcpTools" :key="tool" class="mcp-tool-item" :class="{ 'is-checked': isToolChecked(tool) }" @click="toggleTool(tool)">
									<el-checkbox class="mcp-tool-check" :model-value="isToolChecked(tool)" @click.stop @change="(v) => onToolCheck(tool, v)" />
									<span class="mcp-tool-body">
										<span class="mcp-tool-name">
											<i class="mcp-tool-icon ri-tools-fill" />
											<span class="mcp-tool-name-text" :title="tool === toolSummary(tool) ? tool : `${tool}　${toolSummary(tool)}`">{{ tool }}</span>
										</span>
										<span class="mcp-tool-sub">
											<span class="mcp-tool-method" :class="`m-${toolMethod(tool)}`">[{{ toolMethod(tool).toUpperCase() }}]</span>
											<span v-if="toolPath(tool)" class="mcp-tool-path" :title="toolPath(tool)">{{ toolPath(tool) }}</span>
											<span v-if="toolSummary(tool) !== tool" class="mcp-tool-desc" :title="toolSummary(tool)">{{ toolSummary(tool) }}</span>
										</span>
									</span>
								</div>
							</div>
							<div v-if="!filteredMcpTools.length" class="mcp-tool-empty">
								<el-icon><ele-Search /></el-icon>
								<span>没有匹配「{{ state.mcpToolKeyword }}」的工具</span>
							</div>
						</div>
					</el-form-item>
				</template>
			</el-form>
			<template #footer>
				<el-button icon="ele-CircleCloseFilled" @click="state.editVisible = false">取消</el-button>
				<el-button type="primary" icon="ele-CircleCheckFilled" :loading="state.saving" @click="handleSubmit">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="agentSkillTool">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, FormInstance, UploadFile, UploadUserFile } from 'element-plus';

import { CardAccent } from '/@/views/agent/cardAccent';
import { getAPI } from '/@/utils/axios-utils';
import { AgentSkillToolApi, AgentFileApi } from '/@/api-services/agent/api';
import { AgentSkillToolDto, AgentPageInput } from '/@/api-services/agent/models';

/** fixedType：1=工具 2=技能 3=MCP；传入后列表只展示该类型、新增时直接创建该类型，也不再显示类型筛选与选择；不传则维持三合一行为 */
const props = defineProps<{ fixedType?: number }>();

const editFormRef = ref<FormInstance>();

/** 方法映射候选：与插件 AgentToolSkillService.GetAITools 暴露的 ClassMethod 一一对应，改动需两边同步 */
const toolMethodOptions = [
	{ label: 'AgentHttpClientTools.GetAsync（HTTP GET 请求）', value: 'AgentHttpClientTools.GetAsync' },
	{ label: 'AgentHttpClientTools.PostAsync（HTTP POST 请求）', value: 'AgentHttpClientTools.PostAsync' },
	{ label: 'AgentHttpClientTools.PutAsync（HTTP PUT 请求）', value: 'AgentHttpClientTools.PutAsync' },
	{ label: 'AgentHttpClientTools.DeleteAsync（HTTP DELETE 请求）', value: 'AgentHttpClientTools.DeleteAsync' },
	{ label: 'ShellTools.RunShell（执行 Shell 命令）', value: 'ShellTools.RunShell' },
	{ label: 'PythonTools.RunPythonCode（执行 Python 代码）', value: 'PythonTools.RunPythonCode' },
	{ label: 'CommonTools.GetRuntimePlatform（获取运行平台）', value: 'CommonTools.GetRuntimePlatform' },
	{ label: 'CommonTools.GetDesktopPath（获取桌面路径）', value: 'CommonTools.GetDesktopPath' },
	{ label: 'CommonTools.WriteTextToDesktop（输出文件到桌面）', value: 'CommonTools.WriteTextToDesktop' },
	{ label: 'iKevinAITasksService.AddOrUpdateCronTask（创建/更新周期任务）', value: 'iKevinAITasksService.AddOrUpdateCronTask' },
	{ label: 'iKevinAITasksService.AddOnceTask（创建一次性任务）', value: 'iKevinAITasksService.AddOnceTask' },
	{ label: 'iKevinAITasksService.RemoveCronTask（移除任务）', value: 'iKevinAITasksService.RemoveCronTask' },
	{ label: 'iKevinAITasksService.TriggerCronTask（立即触发任务）', value: 'iKevinAITasksService.TriggerCronTask' },
	{ label: 'iKevinAITasksService.GetTaskList（获取任务列表）', value: 'iKevinAITasksService.GetTaskList' },
	{ label: 'AIMsgService.SendDDToUserMsg（发送钉钉消息）', value: 'AIMsgService.SendDDToUserMsg' },
	{ label: 'WebSearchEngine.DoubaoSearchGlobalAsync（豆包联网搜索 Global）', value: 'WebSearchEngine.DoubaoSearchGlobalAsync' },
	{ label: 'WebSearchEngine.DoubaoSearchCustomAsync（豆包联网搜索 Custom）', value: 'WebSearchEngine.DoubaoSearchCustomAsync' },
];

const rules = {
	skillToolType: [{ required: true, message: '请选择技能工具类型', trigger: 'change' }],
	name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
	classMethod: [{ required: true, message: '请选择方法映射', trigger: 'change' }],
};

const state = reactive({
	loading: false,
	saving: false,
	total: 0,
	tableData: [] as AgentSkillToolDto[],
	queryParams: { page: 1, pageSize: 20, searchKey: '', parameter: props.fixedType ?? (undefined as number | undefined) } as AgentPageInput,
	editVisible: false,
	editTitle: '',
	form: {} as AgentSkillToolDto,
	zipFile: undefined as File | undefined,
	// el-upload 的 file-list 用官方 UploadUserFile 类型，不自己造结构类型
	zipFileList: [] as UploadUserFile[],
	testingMcp: false,
	/** 打开弹窗时的静默同步（旧数据没有中文名才会触发），与「测试连接」按钮的 loading 分开，避免按钮无故转圈 */
	mcpAutoSyncing: false,
	mcpToolList: [] as string[],
	mcpSelected: [] as string[],
	/** 工具名 → 服务端原始描述（来自测试连接；重新打开未测时为空，由名称中文映射兜底） */
	mcpToolDesc: {} as Record<string, string>,
	/** 工具清单搜索词：平台内置 MCP 会返回几百个工具，靠滚动勾选不可用 */
	mcpToolKeyword: '',
});

/** 解析后端以 Json 字符串数组存储的工具清单（非法或空时返回空数组） */
const parseJsonArray = (json?: string | null): string[] => {
	if (!json) return [];
	try {
		const arr = JSON.parse(json);
		return Array.isArray(arr) ? arr.map((t) => String(t)) : [];
	} catch {
		return [];
	}
};

/**
 * 解析存储的工具清单：兼容新格式「[{name,description}]」与旧版纯名称数组「["name"]」，并兼容 PascalCase（Name/Description）。
 * 测试连接后工具清单列持久化完整 DTO（含 swagger 中文描述），历史数据仍是名称数组，两者都要能解析。
 */
const parseJsonTools = (json?: string | null): Array<{ name: string; description: string }> => {
	if (!json) return [];
	try {
		const raw = JSON.parse(json);
		if (!Array.isArray(raw)) return [];
		return raw
			.map((t) => (typeof t === 'string' ? { name: t, description: '' } : { name: String(t?.name ?? t?.Name ?? ''), description: String(t?.description ?? t?.Description ?? '') }))
			.filter((t) => t.name);
	} catch {
		return [];
	}
};

/**
 * 平台内置 MCP 返回的描述形如：[GET] /api/sysUser/getPage  获取用户分页列表
 * 前端只想要后半段的中文摘要，所以剥离开头的「[方法] /路由」前缀
 */
const cleanServerDesc = (desc?: string | null): string => {
	if (!desc) return '';
	const m = desc.match(/^\s*\[[A-Z]+\]\s+\S+\s+([\s\S]*)$/);
	const text = (m ? m[1] : desc).replace(/\s{2,}/g, ' ').trim();
	return text;
};

/**
 * 平台内置 MCP 返回的描述形如：[GET] /api/sysUser/getPage  获取用户分页列表
 * 拆成「方法 / 路径 / 中文摘要」三段，列表项分别展示；与名称推断互补（重新打开未测时只有名称，无三段）
 */
const parseServerDesc = (desc?: string | null): { method: string; path: string; summary: string } => {
	const m = (desc || '').match(/^\s*\[([A-Z]+)\]\s+(\S+)\s+([\s\S]*)$/);
	if (!m) return { method: '', path: '', summary: cleanServerDesc(desc) };
	return { method: m[1], path: m[2], summary: m[3].replace(/\s{2,}/g, ' ').trim() };
};

/** 列表项：方法标签（GET/POST/PUT/DELETE/api），优先取服务端描述的 [方法]，否则从工具名末段推断 */
const toolMethod = (name: string): string => {
	const sd = parseServerDesc(state.mcpToolDesc[name]);
	return sd.method ? sd.method.toLowerCase() : methodFromName(name);
};

/** 列表项：API 路径（仅当服务端描述带 [方法] 路径时才展示，如 /api/sysUser/getPage） */
const toolPath = (name: string): string => parseServerDesc(state.mcpToolDesc[name]).path || guessPathFromName(name);

/**
 * 服务端描述缺失（如直接打开已配置的弹窗、未重新测试连接）时，按平台 RESTful 命名约定
 * 从工具名反推接口路由，保证路由始终可见；若服务端描述已含精确路径则优先用精确值。
 * 约定：模块_动作…_[末段 camelCase 参数]_方法，如 sysOAuthUser_list_userId_get → /api/sysOAuthUser/list/{userId}
 */
const guessPathFromName = (name: string): string => {
	const parts = name.split('_');
	if (parts.length < 3) return '';
	const method = parts[parts.length - 1].toLowerCase();
	if (!['get', 'post', 'put', 'delete'].includes(method)) return ''; // 非 REST 工具无路由可推
	const segments = parts.slice(0, parts.length - 1); // 去掉方法段
	const lastIdx = segments.length - 1;
	return (
		'/api/' +
		segments
			.map((seg, idx) => {
				// 仅把「末段动作」里 camelCase 的名词当作路径参数 {xxx}，动词动作原样保留，避免误判（getPage 不是参数）
				if (
					idx === lastIdx &&
					/[a-z]/.test(seg) &&
					/[A-Z]/.test(seg) &&
					!/^(page|list|info|detail|tree|datalist|userlist|unreadlist|search|query|count|stats|submit|save|add|insert|update|delete|set|run|start|stop|send|bind|unbind|enable|disable|create|generate|build|preview|copy|move|clear|login|logout|sync|grant|revoke|upload|download|check|reset|refresh|approve|reject|export|import|lock|unlock|publish|deploy|test|tag)$/i.test(
						seg
					)
				) {
					return `{${seg.charAt(0).toLowerCase() + seg.slice(1)}}`;
				}
				return seg;
			})
			.join('/')
	);
};

/** 工具中文名：取 swagger 注释里的摘要（描述尾部），列表里跟在方法/路径之后；无描述时回退原始工具名，供调用方判断是否展示 */
const toolSummary = (name: string): string => {
	const sd = parseServerDesc(state.mcpToolDesc[name]);
	return sd.summary || name;
};

/** 从工具名末段推断 HTTP 方法，用于第二行的小标签（get/post/put/delete，否则 api） */
const methodFromName = (name: string): string => {
	const last = name.split('_').pop()?.toLowerCase() ?? '';
	return ['get', 'post', 'put', 'delete'].includes(last) ? last : 'api';
};

const typeText = (value?: number) => (value === 2 ? 'Skill' : value === 3 ? 'MCP' : 'Tool');

/** 卡片图标底色按类型区分：工具蓝、技能绿、MCP 橙，与菜单图标语义保持一致 */
/** 图标字形按类型走：工具扳手、技能拼图、MCP 插头，缩到 36px 也能分辨；底色统一用主题色 */
const watermarkIcon = (value?: number) => (value === 2 ? 'ri-puzzle-2-fill' : value === 3 ? 'ri-plug-fill' : 'ri-tools-fill');

/** 整卡统一主题色：类型由图标形状区分、内置与启用状态由右上角文字标签表达，配色不再承担这两重职责 */
const stateAccent = (_row: AgentSkillToolDto): CardAccent => 'primary';

/** 空状态文案按菜单类型区分 —— 固定的「暂无数据」不会告诉用户下一步该做什么 */
const emptyText = computed(() => {
	switch (props.fixedType) {
		case 1:
			return '暂无工具，点击上方按钮即可绑定系统内置能力';
		case 2:
			return '暂无技能，上传技能包后可在智能体里勾选使用';
		case 3:
			return '暂无 MCP，填写服务地址并测试连接后即可加载工具';
		default:
			return '暂无技能工具，点击上方按钮新增';
	}
});

// 新增按钮文案：固定类型模式下按类型区分，混合模式下使用通用文案
const addButtonText = computed(() => {
	switch (props.fixedType) {
		case 1:
			return '新增工具';
		case 2:
			return '新增技能';
		case 3:
			return '新增 MCP';
		default:
			return '新增技能工具';
	}
});

/** 头图标题/说明：同一组件挂三个菜单，文案必须跟着 fixedType 走，否则技能页会顶一个「工具管理」 */
const heroMeta = computed(() => {
	switch (props.fixedType) {
		case 1:
			return {
				title: '工具管理',
				desc: '把系统内置能力（HTTP、Shell、Python、联网搜索、定时任务等）注册成工具，勾选后模型即可在对话中调用。',
			};
		case 2:
			return {
				title: '技能管理',
				desc: '上传技能包（zip）即可部署为可复用能力；保存时会做安全扫描，非授权域名地址的脚本会被拦下。',
			};
		case 3:
			return {
				title: 'MCP 服务',
				desc: '对接外部 MCP 服务：先测试连接拉取工具清单，再挑选需要加载的工具，运行时只加载勾选项。',
			};
		default:
			return {
				title: '技能工具',
				desc: '统一维护工具、技能与 MCP 三类能力，智能体在装配时按勾选结果挂载。',
			};
	}
});

/** 头图统计：总数取接口返回值，内置与启用数量按当前页汇总 */
const heroStats = computed(() => ({
	builtin: state.tableData.filter((t) => t.isSystem).length,
	active: state.tableData.filter((t) => t.activeStatus === 1).length,
}));

const handleQuery = async () => {
	state.loading = true;
	try {
		// 固定类型模式下始终把 parameter 钉死为 fixedType，避免外部清空导致跨类型查询
		if (props.fixedType) state.queryParams.parameter = props.fixedType;
		const res = await getAPI(AgentSkillToolApi).apiAgentSkillToolPagePost(state.queryParams);
		state.tableData = res.data.result?.items ?? [];
		state.total = res.data.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

const resetQuery = () => {
	state.queryParams.searchKey = '';
	// 固定类型模式下 parameter 不能被清空
	state.queryParams.parameter = props.fixedType ?? undefined;
	state.queryParams.page = 1;
	handleQuery();
};

const handleAdd = () => {
	state.editTitle = `新增${addButtonText.value.replace('新增', '')}`;
	// 固定类型模式下默认类型为 fixedType，否则默认工具
	state.form = { skillToolType: props.fixedType ?? 1, activeStatus: 1, name: '', description: '', classMethod: '' } as AgentSkillToolDto;
	state.zipFile = undefined;
	state.zipFileList = [];
	state.mcpToolList = [];
	state.mcpSelected = [];
	state.mcpToolDesc = {};
	state.mcpToolKeyword = '';
	state.mcpAutoSyncing = false;
	state.editVisible = true;
};

/** 按搜索词过滤后的工具清单（名称、中文名、描述任一命中即可，工具多时按中文名更好找） */
const filteredMcpTools = computed(() => {
	const kw = state.mcpToolKeyword.trim().toLowerCase();
	if (!kw) return state.mcpToolList;
	return state.mcpToolList.filter((t) => t.toLowerCase().includes(kw) || toolSummary(t).toLowerCase().includes(kw) || (state.mcpToolDesc[t] ?? '').toLowerCase().includes(kw));
});

/** 对当前筛出来的工具批量勾选/取消：只影响可见范围，避免误改搜索外的选择 */
const selectFilteredTools = (checked: boolean) => {
	const scope = filteredMcpTools.value;
	const rest = state.mcpSelected.filter((t) => !scope.includes(t));
	state.mcpSelected = checked ? [...rest, ...scope] : rest;
};

/** 工具是否已勾选（受控勾选项的判定，替代 el-checkbox-group 的 v-model） */
const isToolChecked = (tool: string): boolean => state.mcpSelected.includes(tool);

/** 整行点击切换勾选：复选框自身的点击已 .stop 拦截，不会与这里重复触发 */
const toggleTool = (tool: string) => {
	const idx = state.mcpSelected.indexOf(tool);
	if (idx === -1) state.mcpSelected.push(tool);
	else state.mcpSelected.splice(idx, 1);
};

/** 复选框 change 回调：直接用控件结果同步数组，避免与整行点击双重切换 */
const onToolCheck = (tool: string, val: boolean) => {
	const idx = state.mcpSelected.indexOf(tool);
	if (val && idx === -1) state.mcpSelected.push(tool);
	else if (!val && idx !== -1) state.mcpSelected.splice(idx, 1);
};

/**
 * 回填已落库的工具清单：返回清单里是否带描述（带描述才能还原中文名）。
 * 历史数据只存了名称数组，解析得出来但没有描述，需要调用方再同步一次。
 */
const fillMcpTools = (json?: string | null): boolean => {
	const tools = parseJsonTools(json);
	state.mcpToolList = tools.map((t) => t.name);
	state.mcpSelected = parseJsonArray(state.form.mcpSelectedTools);
	state.mcpToolDesc = {};
	tools.forEach((t) => {
		if (t.description) state.mcpToolDesc[t.name] = t.description;
	});
	return tools.some((t) => t.description);
};

/** 打开内置 MCP 的配置弹窗：行数据已含完整字段，不必再拉详情；名称/地址只读，只放开「测试连接 + 勾选工具 + 启用」 */
const handleConfigMcp = (row: AgentSkillToolDto) => {
	state.editTitle = '配置内置 MCP 工具';
	state.form = { ...row };
	state.mcpToolKeyword = '';
	state.zipFile = undefined;
	state.zipFileList = [];
	// 工具清单列持久化的是完整 DTO（含 swagger 中文描述），解析后回填名称列表与描述映射，重新打开弹窗也能显示中文名
	const hasDesc = fillMcpTools(state.form.mcpTools);
	state.editVisible = true;
	// 旧数据只有英文工具名，本地无法还原中文名 —— 打开时静默同步一次，后端会把带描述的清单落库，之后再打开就是秒开
	if (!hasDesc) void syncMcpTools(true);
};

const handleEdit = async (row: AgentSkillToolDto) => {
	state.editTitle = '编辑技能工具';
	// 拉详情失败时不要留下一个空表单弹窗：先把异常抛给全局提示，再退出
	try {
		const res = await getAPI(AgentSkillToolApi).apiAgentSkillToolDetailGet(row.id!);
		state.form = res.data.result ?? ({} as AgentSkillToolDto);
	} catch {
		return;
	}
	state.zipFile = undefined;
	state.zipFileList = [];
	const hasDesc = fillMcpTools(state.form.mcpTools);
	state.mcpToolKeyword = '';
	state.editVisible = true;
	if (state.form.skillToolType === 3 && !hasDesc) void syncMcpTools(true);
};

/**
 * 拉取 MCP 工具清单并回填中文名（「测试连接」按钮与打开弹窗的自动补全都走这里）。
 * auto=true 为静默补全：失败不弹提示，避免历史数据里已失效的 MCP 一打开就报错。
 */
const syncMcpTools = async (auto = false) => {
	if (auto && state.mcpAutoSyncing) return;
	if (state.form.mcpType !== 'stdio' && !state.form.mcpUrl) {
		if (!auto) ElMessage.warning('请先填写 MCP 地址');
		return;
	}
	if (state.form.mcpType === 'stdio' && !state.form.mcpCommand) {
		if (!auto) ElMessage.warning('请先填写命令');
		return;
	}
	if (auto) state.mcpAutoSyncing = true;
	else state.testingMcp = true;
	try {
		// 走 codegen 生成的接口：手写路径极易与后端动态 API 路由（/api/agentSkillTool/...）对不上而 404
		const res = await getAPI(AgentSkillToolApi).apiAgentSkillToolTestMcpConnectionPost(
			{
				// 编辑既有条目时带上 Id：后端顺带把工具清单落库，下次打开配置弹窗不用再测一遍
				id: state.form.id ?? 0,
				mcpUrl: state.form.mcpUrl,
				mcpType: state.form.mcpType,
				mcpHeaders: state.form.mcpHeaders,
				mcpCommand: state.form.mcpCommand,
				mcpArguments: state.form.mcpArguments,
				mcpEnvironment: state.form.mcpEnvironment,
			},
			// 自动补全不能拖住弹窗：全局 10 分钟超时对失效的 MCP 等于无限转圈，这里单独收紧到 15 秒
			auto ? { timeout: 15000 } : undefined
		);
		const tools: Array<{ name?: string; description?: string }> = (res.data.result ?? [])
			.map((t: { name?: string; description?: string }) => ({ name: t.name, description: t.description }))
			.filter((t) => t.name);
		if (!tools.length) {
			if (!auto) ElMessage.warning('测试连接成功，但未返回任何工具');
			return;
		}
		state.mcpToolList = tools.map((t) => t.name);
		// 缓存每个工具的服务端描述（中文摘要），列表项展示用
		state.mcpToolDesc = {};
		tools.forEach((t) => {
			if (t.description) state.mcpToolDesc[t.name!] = t.description;
		});
		// 保留仍然存在的已勾选工具
		state.mcpSelected = state.mcpSelected.filter((t) => tools.some((x) => x.name === t));
		// 持久化完整 DTO（含描述）：重新打开弹窗时前端解析即可回显中文名，不必再测一次
		state.form.mcpTools = JSON.stringify(tools);
		if (!auto) ElMessage.success(`测试连接成功，共 ${tools.length} 个工具`);
	} catch (e: unknown) {
		if (auto) return;
		// MCP 服务端把鉴权失败放在 200 的 body 里（code=401），全局拦截器只会显示成普通失败，这里补一句可操作的排查建议
		const err = e as { message?: string };
		const tip = /401|未授权|登录已过期|Unauthorized/i.test(err?.message ?? '')
			? `连接失败：${err?.message ?? ''}（MCP 服务要求登录鉴权，请确认当前账号有效且地址正确）`
			: `连接失败：${err?.message ?? '未知错误'}`;
		ElMessage.error(tip);
	} finally {
		if (auto) state.mcpAutoSyncing = false;
		else state.testingMcp = false;
	}
};

/** 「测试连接」按钮：手动触发，成功/失败都给明确反馈 */
const handleTestMcp = () => syncMcpTools(false);

const onTypeChange = () => {
	state.form.classMethod = '';
};

const onZipChange = (file: UploadFile) => {
	state.zipFile = file.raw as File;
	state.zipFileList = [file];
};

const handleSubmit = async () => {
	await editFormRef.value?.validate();
	if (state.form.skillToolType === 2 && !state.form.id && !state.zipFile) {
		ElMessage.warning('请上传 skill 技能附件包');
		return;
	}

	state.saving = true;
	try {
		// MCP：勾选结果与工具清单落库；工具清单持久化完整 DTO（含 swagger 中文描述），后端 Check 会校验“先测试、至少勾一个、勾选⊆测试结果”
		if (state.form.skillToolType === 3) {
			state.form.mcpTools = JSON.stringify(state.mcpToolList.map((n) => ({ name: n, description: state.mcpToolDesc[n] ?? '' })));
			state.form.mcpSelectedTools = JSON.stringify(state.mcpSelected);
		}
		// 内置 MCP 走专用接口：AddEdit 会直接拒绝内置条目（“系统内置工具不允许修改”）。
		// 只回传勾选结果 —— 工具清单由后端从「最近一次测试连接」的缓存里取，避免几十 KB 的清单往返
		if (state.form.skillToolType === 3 && state.form.isSystem) {
			await getAPI(AgentSkillToolApi).apiAgentSkillToolConfigMcpToolsPost({
				id: state.form.id!,
				mcpSelectedTools: state.form.mcpSelectedTools,
				activeStatus: state.form.activeStatus ?? 1,
			});
			ElMessage.success('配置已保存');
			state.editVisible = false;
			handleQuery();
			return;
		}
		// 技能包：先落库拿到 Id，再上传附件（后端保存文本时会校验并部署技能包）
		const addRes = await getAPI(AgentSkillToolApi).apiAgentSkillToolAddEditPost(state.form);
		const id = addRes.data.result;

		if (state.form.skillToolType === 2 && state.zipFile && id) {
			await getAPI(AgentFileApi).apiAgentFileUploadPostForm(state.zipFile, 'AgentSkillTool', 'SkillZip', id, '.zip');
			// 附件上传后再触发一次保存，让后端完成技能包部署
			await getAPI(AgentSkillToolApi).apiAgentSkillToolAddEditPost({ ...state.form, id });
		}

		ElMessage.success('保存成功');
		state.editVisible = false;
		handleQuery();
	} finally {
		state.saving = false;
	}
};

const handleDelete = async (row: AgentSkillToolDto) => {
	try {
		await ElMessageBox.confirm(`确定要删除「${row.name}」吗？`, '提示', { type: 'warning' });
	} catch {
		return;
	}
	await getAPI(AgentSkillToolApi).apiAgentSkillToolDeleteIdPost(row.id!);
	ElMessage.success('删除成功');
	handleQuery();
};

onMounted(() => {
	handleQuery();
});
</script>

<style scoped>
/* 头图与下方内容块的间距 */
.query-toolbar {
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

/* MCP 工具清单面板：无边框，纯白容器；搜索栏与列表靠留白分组，不靠框线 */
.mcp-tool-panel {
	margin-top: 8px;
	border: none;
	background: transparent;
	width: 100%;
}

.mcp-tool-bar {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 0 0 8px;
	background: transparent;
}

.mcp-tool-bar .mcp-tool-search {
	width: 240px;
}

.mcp-tool-count {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}

.mcp-tool-count b {
	color: var(--el-color-primary);
	font-size: 13px;
}

.mcp-tool-actions {
	margin-left: auto;
	display: flex;
	gap: 2px;
}

/* 单列紧凑列表：上方图标+名称（中文注释名），下方方法/路径；无边框、无分隔线、整行可点选 */
.mcp-tool-list {
	display: block;
	padding: 2px 0;
	max-height: 360px;
	overflow-y: auto;
}

/* 每个工具 = 整行：复选框 +（图标+名称 首行 / 方法+路径 次行）；整行可点选、紧凑但留出行距 */
.mcp-tool-item {
	display: flex;
	align-items: flex-start;
	width: 100%;
	padding: 4px 6px;
	border-radius: 6px;
	background: transparent;
	cursor: pointer;
	transition: background-color 0.14s ease;
}

/* 复选框：贴近首行名称，不与图标争高度 */
.mcp-tool-check {
	flex-shrink: 0;
	margin: 0 6px 0 0;
	height: auto;
}

/* 右侧主信息列：首行（图标+名称）+ 次行（方法/路径），行距适中 */
.mcp-tool-body {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 3px;
}

/* 首行：图标 + 工具名 同行且垂直居中（保证图标与名称水平对齐） */
.mcp-tool-name {
	display: flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
}

/* 工具图标：灰色小扳手，与名称同处首行 */
.mcp-tool-icon {
	flex-shrink: 0;
	font-size: 13px;
	line-height: 1;
	color: var(--el-text-color-secondary);
}

/* 工具名：原始工具名（对模型可见的标识），加粗为主视觉，单行裁切；中文名在次行 */
.mcp-tool-name-text {
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 13px;
	font-weight: 600;
	line-height: 1.4;
	color: var(--el-text-color-primary);
}

/* 次行：方法 [GET] + 路径，缩进到名称文字下方对齐，超长整体裁切（仿参考图） */
.mcp-tool-sub {
	display: flex;
	align-items: baseline;
	gap: 6px;
	min-width: 0;
	overflow: hidden;
	padding-left: 19px; /* 图标13 + 间距6，与首行名称左缘对齐 */
	line-height: 1.3;
}

/* 方法类型：方括号包裹、按方法着色（仿参考图的 [GET] 样式） */
.mcp-tool-method {
	flex-shrink: 0;
	font-size: 11px;
	font-weight: 700;
	letter-spacing: 0.3px;
	color: var(--el-text-color-secondary);
}
.mcp-tool-method.m-get {
	color: var(--el-color-success);
}
.mcp-tool-method.m-post {
	color: var(--el-color-primary);
}
.mcp-tool-method.m-put {
	color: var(--el-color-warning);
}
.mcp-tool-method.m-delete {
	color: var(--el-color-danger);
}

/* 路径：等宽小字、浅灰；过长时自行截断，给后面的中文名让位 */
.mcp-tool-path {
	flex-shrink: 1;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 11px;
	font-family: var(--el-font-family-mono, ui-monospace, monospace);
	color: var(--el-text-color-placeholder);
}

/* 中文名：接在方法与路径之后，是这一行的主要信息，同样单行裁切 */
.mcp-tool-desc {
	flex-shrink: 1;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

/* hover：整行浅底 */
.mcp-tool-item:hover {
	background: var(--el-fill-color-light);
}
.mcp-tool-item:hover .mcp-tool-name-text {
	color: var(--el-color-primary);
}

/* 选中：整行浅灰底（仿参考图高亮样式），无边框、无竖条；名称变主色 */
.mcp-tool-item.is-checked {
	background: var(--el-fill-color);
}
.mcp-tool-item.is-checked .mcp-tool-name-text {
	color: var(--el-color-primary);
}

.mcp-tool-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	padding: 24px 0;
	color: var(--el-text-color-placeholder);
	font-size: 13px;
}

/* 方法映射：等宽 + 单行裁切，长方法名不换行（换行会让卡片高度不可控） */
.tool-method {
	margin-top: 6px;
	-webkit-line-clamp: 1;
	white-space: nowrap;
	text-overflow: ellipsis;
}

/* 自动同步提示：与「测试连接」按钮同一行高度，避免弹窗内容跳动 */
.mcp-sync-tip {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	margin-top: 4px;
}
</style>
