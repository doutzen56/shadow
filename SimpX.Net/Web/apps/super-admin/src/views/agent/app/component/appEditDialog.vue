<template>
	<el-form :model="form" ref="formRef" :rules="rules" label-width="120px" class="app-edit-form">
		<el-tabs v-model="state.activeTab" class="app-edit-tabs">
			<!-- 基本信息 -->
			<el-tab-pane label="智能体信息" name="base" ref="basePaneRef" class="app-edit-pane" :style="paneStyle">
				<div class="form-section-title">基础信息</div>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="名称" prop="name">
							<el-input v-model="form.name" placeholder="请输入智能体名称" maxlength="128" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="类型">
							<el-input v-model="form.type" placeholder="如：通用助手 / 运维助手" maxlength="64" clearable />
						</el-form-item>
					</el-col>
				</el-row>
				<el-form-item label="描述">
					<el-input v-model="form.describe" type="textarea" :rows="2" maxlength="512" show-word-limit placeholder="智能体描述，会作为模型 Description 提供给子智能体调用" />
				</el-form-item>
				<div class="form-section-title">关联配置</div>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="提示词" prop="aiPromptID">
							<el-select v-model="form.aiPromptID" placeholder="请选择提示词" filterable style="width: 100%">
								<el-option v-for="item in prompts" :key="item.id" :label="item.name" :value="item.id!" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="知识库">
							<el-select v-model="form.kmsId" placeholder="可不选" filterable clearable style="width: 100%">
								<el-option v-for="item in kmsList" :key="item.id" :label="item.name" :value="item.id!" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="会话模型" prop="chatModelID">
							<el-select v-model="form.chatModelID" placeholder="请选择会话模型" filterable style="width: 100%">
								<el-option label="auto（随机选择并自动切换备用模型）" value="auto" />
								<el-option v-for="item in chatModels" :key="item.id" :label="item.modelName" :value="String(item.id)" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="消息输出类型">
							<el-select v-model="form.msgType" style="width: 100%">
								<el-option label="流式文本（推荐）" :value="2" />
								<el-option label="非流式文本" :value="1" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<div class="form-section-title">模型参数</div>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="温度(0-100)">
							<el-slider v-model="form.temperature" :min="0" :max="100" show-input />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="相似度(0-100)">
							<el-slider v-model="form.relevance" :min="0" :max="100" show-input />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="向量匹配数">
							<el-input-number v-model="form.maxMatchesCount" :min="1" :max="50" controls-position="right" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="最大重试次数">
							<el-input-number v-model="form.maxRetries" :min="1" :max="10" controls-position="right" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="请求超时(分钟)">
							<el-input-number v-model="form.networkTimeout" :min="1" :max="600" controls-position="right" style="width: 100%" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="对话消息上限">
							<el-input-number v-model="form.chatMessageLimit" :min="10" :max="10000" controls-position="right" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="内容长度限制">
							<el-input-number v-model="form.contentLengthLimit" :min="1000" :step="1000" controls-position="right" style="width: 100%" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-form-item label="请求白名单">
					<el-input v-model="form.authorizedDomains" placeholder="* 为不限制；多个域名用英文逗号分隔，如：api.example.com,example.org" clearable />
					<div class="tip">命中白名单之外域名时，Shell / Python / HTTP 工具会被拦截</div>
				</el-form-item>
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="返回格式">
							<el-select v-model="form.responseFormat" clearable placeholder="默认（不限制）" style="width: 100%">
								<el-option label="Json" value="Json" />
								<el-option label="Text" value="Text" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="思考能力">
							<el-select v-model="form.reasoningEffort" clearable placeholder="默认（不配置）" style="width: 100%">
								<el-option label="0 None" :value="0" />
								<el-option label="1 Low" :value="1" />
								<el-option label="2 Medium" :value="2" />
								<el-option label="3 High" :value="3" />
								<el-option label="4 ExtraHigh" :value="4" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
						<el-form-item label="思考输出">
							<el-select v-model="form.reasoningOutput" clearable placeholder="默认（不配置）" style="width: 100%">
								<el-option label="0 None" :value="0" />
								<el-option label="1 Summary" :value="1" />
								<el-option label="2 Full" :value="2" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-form-item label="开关配置">
					<div class="switch-grid">
						<div class="switch-item"><el-checkbox v-model="form.isAITools">启用AI工具</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isSkill">启用Skill技能</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isMcp">启用MCP工具</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isMemory">启用智能体记忆</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isThinkingLog">展示思考过程</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isToolLog">展示工具调用</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isSecurityIntercept">启用安全拦截</el-checkbox></div>
						<div class="switch-item"><el-checkbox v-model="form.isHttpLog">记录AI请求日志</el-checkbox></div>
					</div>
				</el-form-item>
			</el-tab-pane>

			<!-- 技能工具 -->
			<el-tab-pane label="技能工具" name="skill" class="app-edit-pane" :style="paneStyle">
				<el-form-item label="能力绑定">
					<div style="width: 100%">
						<div class="tip">为智能体各项能力指定所用模型；关闭开关即停用该能力（保留模型配置）。</div>
						<el-row class="binding-row" :gutter="10" v-for="row in bindingRows" :key="row.capability" style="margin-top: 6px">
							<el-col :xs="24" :sm="8" :md="7" :lg="6" :xl="6">
								<el-switch v-model="row.enabled" />
								<span style="margin-left: 8px">{{ row.label }}</span>
							</el-col>
							<el-col :xs="24" :sm="16" :md="17" :lg="18" :xl="18">
								<el-select v-model="row.modelId" :placeholder="row.placeholder" filterable clearable style="width: 100%" :disabled="!row.enabled">
									<el-option v-for="m in row.options" :key="m.id" :label="m.modelName" :value="String(m.id)" />
								</el-select>
							</el-col>
						</el-row>
					</div>
				</el-form-item>
				<el-tabs v-model="state.skillTab" type="card">
					<el-tab-pane label="工具 Tools" name="tools">
						<el-checkbox-group v-model="state.selectedTools">
							<el-row :gutter="10">
								<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.toolList" :key="item.id">
									<el-checkbox :value="item.id!">
										{{ item.name }}
										<el-tooltip v-if="item.description" :content="item.description" placement="top">
											<el-icon style="margin-left: 4px"><ele-InfoFilled /></el-icon>
										</el-tooltip>
									</el-checkbox>
								</el-col>
							</el-row>
						</el-checkbox-group>
					</el-tab-pane>
					<el-tab-pane label="技能 Skills" name="skills">
						<el-checkbox-group v-model="state.selectedSkills">
							<el-row :gutter="10">
								<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.skillList" :key="item.id">
									<el-checkbox :value="item.id!">
										{{ item.name }}
										<el-tooltip v-if="item.description" :content="item.description" placement="top">
											<el-icon style="margin-left: 4px"><ele-InfoFilled /></el-icon>
										</el-tooltip>
									</el-checkbox>
								</el-col>
							</el-row>
						</el-checkbox-group>
					</el-tab-pane>
					<el-tab-pane label="MCP 工具" name="mcps">
						<el-checkbox-group v-model="state.selectedMcps">
							<el-row :gutter="10">
								<el-col :xs="24" :sm="12" :md="8" :lg="8" :xl="6" v-for="item in state.mcpList" :key="item.id">
									<el-checkbox :value="item.id!">{{ item.name }}</el-checkbox>
								</el-col>
							</el-row>
						</el-checkbox-group>
					</el-tab-pane>
				</el-tabs>
			</el-tab-pane>

			<!-- 授权绑定 -->
			<el-tab-pane label="授权绑定" name="bind" class="app-edit-pane" :style="paneStyle">
				<el-form-item label="授权用户">
					<el-select v-model="state.bindUserIds" multiple filterable clearable style="width: 100%" placeholder="授权后该用户可以使用此智能体">
						<el-option v-for="item in state.userList" :key="item.id" :label="`${item.realName ?? item.account}`" :value="item.id!" />
					</el-select>
				</el-form-item>
				<el-form-item label="授权角色">
					<el-select v-model="state.bindRoleIds" multiple filterable clearable style="width: 100%" placeholder="授权后该角色下的用户可以使用此智能体">
						<el-option v-for="item in state.roleList" :key="item.id" :label="item.name ?? ''" :value="item.id!" />
					</el-select>
				</el-form-item>
				<el-form-item label="子智能体">
					<el-select v-model="state.bindAgentIds" multiple filterable clearable style="width: 100%" placeholder="挂载为工具，供当前智能体按需调用（最多三级）">
						<el-option v-for="item in state.otherApps" :key="item.id" :label="item.name" :value="item.id!" />
					</el-select>
				</el-form-item>
				<div class="tip">超级管理员默认可使用全部智能体；未做任何授权时，普通用户将看不到该智能体。</div>
			</el-tab-pane>

			<!-- 压缩策略 -->
			<el-tab-pane label="压缩策略" name="compaction" class="app-edit-pane" :style="paneStyle">
				<el-form-item label="对话自动压缩">
					<el-switch v-model="form.isAIMessageCompaction" />
					<div class="tip">开启后历史对话、思考过程、工具结果会在后台自动压缩为摘要</div>
				</el-form-item>
				<el-form-item label="自动获取压缩" v-if="form.isAIMessageCompaction">
					<el-switch v-model="form.isAutoGetAIMessageCompaction" />
					<div class="tip">开启后压缩摘要直接注入系统提示词；关闭则作为工具由模型按需获取</div>
				</el-form-item>
				<el-form-item label="用户轮次阈值" v-if="form.isAIMessageCompaction">
					<el-input-number v-model="form.conversationTurnsExceed" :min="1" :max="200" controls-position="right" />
					<div class="tip">超出该轮次的历史对话会被压缩</div>
				</el-form-item>
				<el-form-item label="压缩提示词" v-if="form.isAIMessageCompaction">
					<el-input v-model="form.aiMessageCompactionPrompt" type="textarea" :rows="10" placeholder="压缩策略提示词" />
				</el-form-item>
			</el-tab-pane>
		</el-tabs>

		<div class="dialog-footer is-inline">
			<el-button icon="ele-CircleCloseFilled" @click="emit('close')">取消</el-button>
			<el-button type="primary" icon="ele-CircleCheckFilled" :loading="saving" @click="handleSubmit">确定</el-button>
		</div>
	</el-form>
</template>

<script lang="ts" setup name="appEditDialog">
import { computed, nextTick, onMounted, reactive, ref } from 'vue';
import { ElMessage, FormInstance } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi } from '/@/api-services/system/apis/sys-user-api';
import { SysRoleApi } from '/@/api-services/system/apis/sys-role-api';
import { AgentApi, AgentKmsApi, AgentModelApi, AgentPromptApi, AgentSkillToolApi } from '/@/api-services/agent/api';
import { AgentAppDto, AgentPromptDto, AgentModelDto, AgentKmsDto, AgentSkillToolDto } from '/@/api-services/agent/models';
// 用户/角色下拉直接用接口返回的真实 DTO 类型：它们的可空字段（如 name?: string | null）与自造结构类型不兼容
import { RoleOutput, UserOutput } from '/@/api-services/system/models';

const props = defineProps<{ appId?: number }>();
const emit = defineEmits(['close', 'saved']);

const formRef = ref<FormInstance>();
const saving = ref(false);
const prompts = ref<AgentPromptDto[]>([]);
const chatModels = ref<AgentModelDto[]>([]);

/** 是否具备图片输入能力（与后端 agents_model.Capabilities 里的 vision 标签对应） */
const hasVisionCapability = (capabilities?: string | null) =>
	(capabilities ?? '')
		.split(',')
		.map((item) => item.trim().toLowerCase())
		.includes('vision');

/** 「识图模型」下拉的候选：只列出声明了图片输入能力的聊天模型 */
const visionModels = computed(() => chatModels.value.filter((item) => hasVisionCapability(item.capabilities)));
/** 「文生图模型」下拉的候选：模型类型为文生图（TextToImage=3）的模型 */
const imageGenModels = ref<AgentModelDto[]>([]);
const kmsList = ref<AgentKmsDto[]>([]);

/** 能力绑定项（与后端 AgentModelBindingDto 对应，存于 agents_app.model_bindings 的 JSON 数组） */
type AgentModelBinding = { capability: string; modelId?: string | null; enabled?: boolean };

/** 能力绑定编辑行（能力键固定，模型下拉候选按能力给定） */
const bindingRows = ref<{ capability: string; label: string; placeholder: string; enabled: boolean; modelId: string; options: AgentModelDto[] }[]>([]);

/** 解析后端存储的绑定 JSON 数组（非法或空时返回空数组） */
const parseBindings = (json?: string | null): AgentModelBinding[] => {
	if (!json) return [];
	try {
		const arr = JSON.parse(json);
		return Array.isArray(arr) ? arr.filter((t) => t && t.capability) : [];
	} catch {
		return [];
	}
};

/** 按当前 modelBindings 重建编辑行（vision 默认开、image_gen 默认关，与后端默认值一致） */
const buildBindingRows = () => {
	const parsed = parseBindings(form.modelBindings);
	const get = (cap: string) => parsed.find((b) => String(b.capability).toLowerCase() === cap);
	const vision = get('vision');
	const imageGen = get('image_gen');
	bindingRows.value = [
		{ capability: 'vision', label: '识图', placeholder: '请选择具备图片输入能力的模型', enabled: vision?.enabled ?? true, modelId: vision?.modelId ?? '', options: visionModels.value },
		{ capability: 'image_gen', label: '文生图', placeholder: '请选择文生图模型', enabled: imageGen?.enabled ?? false, modelId: imageGen?.modelId ?? '', options: imageGenModels.value },
	];
};

/** 表单即实体：字段与 agents_app 表一致，未显式声明的字段由后端默认值兜底 */
const form = reactive({
	name: '',
	describe: '',
	type: '',
	icon: 'windows',
	chatModelID: '',
	msgType: 2,
	temperature: 50,
	relevance: 50,
	maxMatchesCount: 3,
	rerankCount: 20,
	maxRetries: 2,
	networkTimeout: 30,
	chatMessageLimit: 100,
	contentLengthLimit: 50000,
	authorizedDomains: '*',
	isAITools: true,
	isSkill: true,
	isMcp: true,
	isMemory: false,
	modelBindings: '[]',
	isThinkingLog: true,
	isToolLog: true,
	isSecurityIntercept: true,
	isHttpLog: false,
	isAIMessageCompaction: false,
	isAutoGetAIMessageCompaction: false,
	conversationTurnsExceed: 10,
	aiMessageCompactionPrompt: '',
}) as AgentAppDto;

const rules = {
	name: [{ required: true, message: '请输入智能体名称', trigger: 'blur' }],
	chatModelID: [{ required: true, message: '请选择会话模型', trigger: 'change' }],
	aiPromptID: [{ required: true, message: '请选择提示词', trigger: 'change' }],
};

const state = reactive({
	activeTab: 'base',
	skillTab: 'tools',
	toolList: [] as AgentSkillToolDto[],
	skillList: [] as AgentSkillToolDto[],
	mcpList: [] as AgentSkillToolDto[],
	selectedTools: [] as number[],
	selectedSkills: [] as number[],
	selectedMcps: [] as number[],
	userList: [] as UserOutput[],
	roleList: [] as RoleOutput[],
	otherApps: [] as AgentAppDto[],
	bindUserIds: [] as string[],
	bindRoleIds: [] as string[],
	bindAgentIds: [] as string[],
	/** 智能体信息这一屏实测出的内容高度：四个 tab 内容量差很多，按当前 tab 自适应会让整窗每次切 tab 都跳一下，所以首屏量一次把其余 tab 撑到同一高度 */
	paneHeight: 0,
});

const basePaneRef = ref<{ $el?: HTMLElement }>();

/** 各 tab 的最小高度 = 信息页实测高度；只有比它还高的 tab 才会让弹窗变高（此时内部滚动） */
const paneStyle = computed(() =>
	state.paneHeight
		? // padding-right 给滚动条留位，避免它压在输入框右边缘上
			{ height: `${state.paneHeight}px`, overflowY: 'auto' as const, paddingRight: '6px' }
		: undefined
);

/** 量取基准高度：必须在信息页可见且未被限制高度时进行，因此放在数据加载完成后的 nextTick */
const measurePane = async () => {
	await nextTick();
	const el = basePaneRef.value?.$el;
	if (!el) return;
	// 弹窗头部、tab 条、页脚按钮行合计约占 300px，剩下的才是内容区可用高度。
	// 超出就夹到上限并交给 pane 内部滚动 —— 否则内容把弹窗顶出视口，底部的确定/取消会被截在屏幕外。
	const max = Math.max(320, window.innerHeight - 300);
	// +2 兜住子元素 margin 折叠带来的 1~2px 误差，避免最后一格被裁出滚动条
	state.paneHeight = Math.min(el.offsetHeight + 2, max);
};

const handleSubmit = async () => {
	await formRef.value?.validate();
	if (state.selectedSkills.length === 0 && form.isSkill) {
		// 未绑定技能时仅提示，不阻断保存
		ElMessage.warning('当前未绑定任何 Skill 技能，Skill 能力将为空');
	}

	form.tools = state.toolList.map((item) => ({
		aiSkillToolManagementId: item.id!,
		isSelect: state.selectedTools.includes(item.id!),
	}));
	form.skills = state.skillList.map((item) => ({
		aiSkillToolManagementId: item.id!,
		isSelect: state.selectedSkills.includes(item.id!),
	}));
	form.mcps = state.mcpList.map((item) => ({
		aiSkillToolManagementId: item.id!,
		isSelect: state.selectedMcps.includes(item.id!),
	}));
	form.bindIds = [...state.bindUserIds.map((id) => `user_${id}`), ...state.bindRoleIds.map((id) => `role_${id}`), ...state.bindAgentIds.map((id) => `agent_${id}`)];

	// 能力绑定：把编辑行序列化为 JSON 数组（能力键 + 模型 Id + 是否启用）
	form.modelBindings = JSON.stringify(bindingRows.value.map((row) => ({ capability: row.capability, modelId: row.modelId || null, enabled: !!row.enabled })));

	saving.value = true;
	try {
		await getAPI(AgentApi).apiAgentAddEditPost({ ...form });
		ElMessage.success('保存成功');
		emit('saved');
	} finally {
		saving.value = false;
	}
};

const fillForm = (data: AgentAppDto) => {
	Object.assign(form, data);
	state.selectedSkills = (data.skills ?? [])
		.filter((t) => t.isSelect)
		.map((t) => t.aiSkillToolManagementId)
		.filter((id): id is number => id !== undefined);
	state.selectedTools = (data.tools ?? [])
		.filter((t) => t.isSelect)
		.map((t) => t.aiSkillToolManagementId)
		.filter((id): id is number => id !== undefined);
	state.selectedMcps = (data.mcps ?? [])
		.filter((t) => t.isSelect)
		.map((t) => t.aiSkillToolManagementId)
		.filter((id): id is number => id !== undefined);
	state.bindUserIds = (data.bindIds ?? []).filter((t) => t.startsWith('user_')).map((t) => t.replace('user_', ''));
	state.bindRoleIds = (data.bindIds ?? []).filter((t) => t.startsWith('role_')).map((t) => t.replace('role_', ''));
	state.bindAgentIds = (data.bindIds ?? []).filter((t) => t.startsWith('agent_')).map((t) => t.replace('agent_', ''));
	// 能力绑定：按后端返回的 modelBindings 重建编辑行
	buildBindingRows();
};

onMounted(async () => {
	const promptRes = await getAPI(AgentPromptApi).apiAgentPromptListGet();
	prompts.value = promptRes.data.result ?? [];
	const modelRes = await getAPI(AgentModelApi).apiAgentModelListGet(1);
	chatModels.value = modelRes.data.result ?? [];
	const imageGenRes = await getAPI(AgentModelApi).apiAgentModelListGet(3);
	imageGenModels.value = imageGenRes.data.result ?? [];
	const kmsRes = await getAPI(AgentKmsApi).apiAgentKmsListGet();
	kmsList.value = kmsRes.data.result ?? [];
	const toolRes = await getAPI(AgentSkillToolApi).apiAgentSkillToolToolListGet();
	state.toolList = toolRes.data.result ?? [];
	const skillRes = await getAPI(AgentSkillToolApi).apiAgentSkillToolSkillListGet();
	state.skillList = skillRes.data.result ?? [];
	const mcpRes = await getAPI(AgentSkillToolApi).apiAgentSkillToolMcpListGet();
	state.mcpList = mcpRes.data.result ?? [];

	const users = await getAPI(SysUserApi).apiSysUserPagePost({ page: 1, pageSize: 1000 });
	state.userList = users.data.result?.items ?? [];
	const roles = await getAPI(SysRoleApi).apiSysRoleListGet();
	state.roleList = roles.data.result ?? [];

	// 子智能体候选：排除自身
	const appsRes = await getAPI(AgentApi).apiAgentListGet();
	state.otherApps = (appsRes.data.result ?? []).filter((item) => item.id !== props.appId);

	if (props.appId) {
		const detailRes = await getAPI(AgentApi).apiAgentDetailGet(props.appId);
		if (detailRes.data.result) fillForm(detailRes.data.result);
	} else {
		// 新建时由后端返回默认配置（含可绑定技能工具）
		const initRes = await getAPI(AgentApi).apiAgentInitializationPost();
		form.type = initRes.data.result?.type ?? '通用助手';
		state.selectedSkills = [];
	}
	// 能力绑定：模型候选已加载完毕，重建编辑行（新建时 modelBindings 为 '[]'，走默认值）
	buildBindingRows();

	// 数据回填后再量高度：名称/描述等字段会改变行高
	await measurePane();
});
</script>

<style scoped>
/* 分区标题：主色竖条 + 小标题 + 向右渐隐的细分隔线，让长表单有明确的视觉分组 */
.form-section-title {
	display: flex;
	align-items: center;
	margin: 24px 0 16px;
	font-size: 13px;
	font-weight: 600;
	line-height: 20px;
	color: var(--el-text-color-primary);
}

/* 首个分区上方就是 tab 内容区的起始位置，不需要额外的大留白 */
.form-section-title:first-child {
	margin-top: 4px;
}

.form-section-title::before {
	content: '';
	width: 3px;
	height: 14px;
	margin-right: 8px;
	border-radius: 2px;
	background-color: var(--el-color-primary);
}

.form-section-title::after {
	content: '';
	flex: 1;
	height: 1px;
	margin-left: 6px;
	background: linear-gradient(to right, var(--el-border-color), transparent);
}

/* 表单项：统一纵向呼吸感（Element 默认 18px 在密集配置表单里偏挤） */
.app-edit-form :deep(.el-form-item) {
	margin-bottom: 20px;
}

/*
 * 关键：theme/element.scss 里有
 *   .el-form .el-form-item:last-of-type { margin-bottom: 0 !important }
 * 它本意是「弹窗底部不留多余空白」，但用的是后代选择器 ——
 * 而栅格里每个 el-col 只放一个表单项，于是全部命中 :last-of-type，
 * 两列表单的整列行距被清零，输入框上下贴死。
 * 这里用更高一级的特异性 + !important 反压，只在栅格列内把行距还回来。
 */
.app-edit-form :deep(.el-col > .el-form-item:last-of-type) {
	margin-bottom: 20px !important;
}

/* 页脚按钮行与最后一组控件拉开距离：默认 12px 在内容可滚动的弹窗里看着像贴着内容。
 * 表单写在子组件里用不了 #footer 插槽，这里补一条分隔线，让页脚与原生弹窗页脚观感一致 */
.dialog-footer.is-inline {
	margin-top: 18px;
	padding-top: 14px;
	border-top: 1px solid var(--el-border-color-lighter);
}

/* 开关配置：8 项平铺成自适应网格，每格一个可整格点击的圆角小块，比裸 checkbox 更像配置面板 */
.switch-grid {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
	gap: 8px;
	width: 100%;
}

.switch-item {
	display: flex;
	align-items: center;
	padding: 5px 10px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background-color: var(--el-bg-color);
	transition:
		border-color 0.2s ease,
		background-color 0.2s ease;
}

.switch-item:hover {
	border-color: var(--el-color-primary-light-5);
	background-color: var(--el-color-primary-light-9);
}

/* checkbox 撑满整格：点到格子空白处也能切换，不用精准命中文字 */
.switch-item :deep(.el-checkbox) {
	width: 100%;
	height: 22px;
	margin-right: 0;
	margin-bottom: 0;
}

/* 开关 / 工具勾选项：平铺多行时给出固定行高与行距，避免上下两排贴在一起 */
.app-edit-form :deep(.el-checkbox) {
	height: 28px;
	margin-right: 16px;
	margin-bottom: 8px;
}

.app-edit-form :deep(.el-form-item__label) {
	color: var(--el-text-color-regular);
	font-weight: 500;
}

/* 能力绑定行：浅底圆角条 + hover 高亮，与上方的开关区拉开层次 */
.binding-row {
	padding: 8px 12px;
	margin-bottom: 8px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background-color: var(--el-fill-color-light);
	transition:
		border-color 0.2s ease,
		background-color 0.2s ease;
}

.binding-row:hover {
	border-color: var(--el-color-primary-light-5);
	background-color: var(--el-color-primary-light-9);
}
</style>
