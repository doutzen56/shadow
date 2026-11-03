<template>
	<div class="property-panel">
		<div class="panel-header">
			<div class="panel-header-top">
				<div class="panel-header-info">
					<div class="panel-title">
						<el-icon><ele-InfoFilled /></el-icon>节点属性
					</div>
					<div class="panel-subtitle">{{ nodeDesc }}</div>
				</div>
				<el-button v-if="nodeData && !readonly" type="primary" size="small" @click="handleSave" :loading="saving" icon="ele-Check"> 保存属性 </el-button>
			</div>
		</div>
		<div v-if="!nodeData" class="panel-placeholder">
			<div class="placeholder-inner">
				<span class="placeholder-icon"><el-icon><ele-Pointer /></el-icon></span>
				<p class="placeholder-title">请选择节点或连线</p>
				<p class="placeholder-tip">点击画布中的节点或连线，即可在此配置属性</p>
			</div>
		</div>
		<template v-else>
			<el-form :model="form" label-width="auto" class="panel-form" :disabled="readonly">
				<el-divider border-style="dashed">基础信息</el-divider>
				<el-form-item label="ID">
					<el-input v-model="form.id" readonly />
				</el-form-item>
				<el-form-item label="类型">
					<el-tag size="small">{{ form.type || '-' }}</el-tag>
				</el-form-item>
				<el-form-item v-if="isEdge" label="连线名称" :error="errors.name">
					<el-input v-model="form.name" placeholder="显示在连线上的名称" />
					<div class="edge-name-actions">
						<el-button type="primary" text icon="ele-DocumentCopy" @click="handleFillEdgeNameByFirstCondition">根据连线条件生成</el-button>
					</div>
				</el-form-item>
				<template v-else>
					<el-form-item label="节点编码" :error="errors.code">
						<el-input v-model="form.code" placeholder="英文或数字，接口/审批取值使用" />
					</el-form-item>
					<el-form-item label="显示名称" :error="errors.name">
						<el-input v-model="form.name" placeholder="画布显示名称" />
					</el-form-item>
				</template>

				<template v-if="isEndNode">
					<el-divider border-style="dashed">结束配置</el-divider>
					<el-form-item label="结束策略">
						<el-select v-model="form.endStrategy">
							<el-option label="自动结束" value="auto" />
							<el-option label="等待回写" value="wait" />
						</el-select>
					</el-form-item>
					<el-form-item label="结束通知">
						<el-select v-model="form.endNotifyStrategy" multiple collapse-tags placeholder="选择通知方式">
							<el-option label="站内消息" value="message" />
							<el-option label="邮件" value="email" />
							<el-option label="短信" value="sms" />
						</el-select>
					</el-form-item>
					<el-form-item label="结束备注">
						<el-input v-model="form.remark" type="textarea" :autosize="{ minRows: 2, maxRows: 3 }" placeholder="结束节点可附加说明" />
					</el-form-item>
				</template>

				<template v-if="isParallelNode">
					<el-divider border-style="dashed">并行配置</el-divider>
					<el-form-item label="并行组编号">
						<el-input-number v-model="form.parallelGroup" :min="1" :step="1" class="w100" />
					</el-form-item>
					<el-form-item label="并行策略">
						<el-select v-model="form.parallelStrategy">
							<el-option label="全部完成后汇聚" value="all" />
							<el-option label="任意分支完成" value="any" />
						</el-select>
					</el-form-item>
				</template>

				<template v-if="isUserTask">
					<div class="approval-tabs-wrapper">
						<el-tabs v-model="activeTab" class="approval-tabs">
							<el-tab-pane label="基础信息" name="basic">
								<div class="tab-content">
									<el-form-item label="审批模式">
										<el-select v-model="form.approvalMode">
											<el-option label="单人审批" value="single" />
											<el-option label="会签（全部同意）" value="and" />
											<el-option label="或签（任意同意）" value="or" />
										</el-select>
									</el-form-item>
									<ScopePicker
										v-model="form.approverScope"
										v-model:scope-type="form.approverType"
										type-label="审批人类型"
										scope-label="审批范围"
										:type-options="approverTypeOptions"
										:preset-options="approverPresetOptions"
										:tree-data="orgTreeData"
										:tree-expanded-keys="orgTreeExpandedKeys"
										:loading="loadingData"
										:load-tree="fetchOrgTree"
										:org-alert-title="orgApproverAlertTitle"
										free-text-placeholder="user:1,2 或 role:1001，多值用逗号隔开"
										:error="errors.scope"
										@update:scope-type="fetchScopeData"
									>
										<template #extra>
											<el-form-item label="无审批人时">
												<el-select v-model="form.noApproverAction" placeholder="跟随全局配置">
													<el-option label="跟随全局配置（默认自动通过）" value="" />
													<el-option label="自动通过，流程继续流转" value="auto" />
													<el-option label="转交流程管理员" value="admin" />
													<el-option label="终止流程并提示发起人" value="reject" />
												</el-select>
												<div class="tip">
													审批人按范围动态解析，可能一个都解析不到（例如发起人没有上级部门）。未配置时按全局策略处理，默认自动通过，避免流程卡住。
												</div>
											</el-form-item>
										</template>
									</ScopePicker>
									<el-form-item label="驳回规则">
										<el-select v-model="form.rejectStrategy">
											<el-option label="终止流程" value="terminate" />
											<el-option label="回退到上一个节点" value="previous" />
											<el-option label="回退到发起节点" value="starter" />
										</el-select>
									</el-form-item>
									<el-form-item label="待办提醒">
										<el-checkbox-group v-model="form.todoNotifyChannels">
											<el-checkbox label="message" value="message">站内信</el-checkbox>
											<el-checkbox label="sms" value="sms">短信</el-checkbox>
											<el-checkbox label="email" value="email">邮件</el-checkbox>
										</el-checkbox-group>
									</el-form-item>
								</div>
							</el-tab-pane>
							<el-tab-pane label="抄送配置" name="cc">
								<div class="tab-content">
									<el-alert type="info" :closable="false" show-icon style="margin-bottom: 10px">
										<template #title>
											<div style="font-size: 12px">
												当<b>当前审批节点</b>审批通过后，根据下面配置为指定人员生成
												<b>抄送(CC)任务</b>，仅用于查看流程和表单详情，不影响流程流转。
											</div>
										</template>
									</el-alert>
									<ScopePicker
										v-model="form.ccScope"
										v-model:scope-type="form.ccType"
										type-label="抄送人类型"
										type-placeholder="不选择则不抄送"
										scope-label="抄送范围"
										:type-options="ccTypeOptions"
										:preset-options="ccPresetOptions"
										:tree-data="orgTreeData"
										:tree-expanded-keys="orgTreeExpandedKeys"
										:loading="loadingData"
										:load-tree="fetchOrgTree"
										:org-alert-title="orgCcAlertTitle"
										:free-text-placeholder="ccFreeTextPlaceholder"
										@update:scope-type="fetchScopeData"
									/>
								</div>
							</el-tab-pane>
							<el-tab-pane label="审批完成回调" name="callback">
								<div class="tab-content">
									<el-form-item label="请求方法">
										<el-select v-model="form.callbackHttpMethod" placeholder="选择请求方法">
											<el-option label="GET" value="GET" />
											<el-option label="POST" value="POST" />
											<el-option label="PUT" value="PUT" />
											<el-option label="DELETE" value="DELETE" />
											<el-option label="PATCH" value="PATCH" />
										</el-select>
									</el-form-item>
									<el-form-item label="接口地址">
										<el-input v-model="form.callbackUrl" placeholder="请输入HTTP接口地址，如：&#10;/api/workflow/callback&#10;http://example.com/api/callback" />
										<div style="font-size: 12px; color: #909399; margin-top: 4px">
											支持两种格式：<br />
											1. <strong>相对路径</strong>：以 / 开头的路径，如 /api/xxx（系统会自动拼接基础URL）<br />
											2. <strong>绝对路径</strong>：完整的URL，如 http://example.com/api/xxx（直接使用）
										</div>
									</el-form-item>
									<el-form-item label="请求头">
										<el-input
											v-model="form.callbackHeaders"
											type="textarea"
											:autosize="{ minRows: 2, maxRows: 5 }"
											placeholder="请输入请求头，格式：Key: Value，每行一个，如：&#10;Content-Type: application/json&#10;Authorization: Bearer token"
										/>
										<div style="font-size: 12px; color: #909399; margin-top: 4px">格式：Key: Value，每行一个（可选）</div>
									</el-form-item>
									<el-form-item label="请求体">
										<el-input
											v-model="form.callbackBody"
											type="textarea"
											:autosize="{ minRows: 3, maxRows: 8 }"
											placeholder='请输入请求体（JSON格式），支持变量替换，如：&#10;{&#10;    "id": "{{businessId}}"&#10;}'
										/>
										<div style="font-size: 12px; color: #909399; margin-top: 4px">
											支持变量替换：<br />
											<strong style="color: #409eff">业务信息：</strong><br />
											• <code v-pre>{{ businessId }}</code> - 业务流程表单ID（特殊变量）<br />
											• <code v-pre>{{ businessModule }}</code> - 业务模块名称<br />
											<strong style="color: #409eff">流程信息：</strong><br />
											• <code v-pre>{{ instanceCode }}</code> - 流程实例编码<br />
											• <code v-pre>{{ stepInstance.stepCode }}</code> - 步骤编码<br />
											• <code v-pre>{{ stepInstance.stepName }}</code> - 步骤名称<br />
											<strong style="color: #409eff">操作人信息：</strong><br />
											• <code v-pre>{{ operatorId }}</code> - 操作人ID<br />
											• <code v-pre>{{ operatorName }}</code> - 操作人姓名<br />
											• <code v-pre>{{ comment }}</code> - 审批意见<br />
											• <code v-pre>{{ approvalStatus }}</code> - 审批结果（approve=通过，reject=驳回）<br />
											<strong style="color: #409eff">表单数据：</strong><br />
											• <code v-pre>{{ formData }}</code> - 完整表单数据（JSON字符串）<br />
											• <code v-pre>{{ formData.fieldName }}</code> - 表单字段值（如：<code v-pre>{{ formData.Reason }}</code
											>）<br />
											<strong style="color: #409eff">注意：GET请求不支持Body，会自动忽略此配置,参数值需要使用双引号包裹</strong>
										</div>
									</el-form-item>
								</div>
							</el-tab-pane>
						</el-tabs>
					</div>
				</template>

				<template v-if="isConditionNode">
					<el-divider border-style="dashed">条件网关说明</el-divider>
					<el-alert type="info" :closable="false" show-icon>
						<template #title>
							<div style="font-size: 12px">
								条件网关的条件表达式应配置在<strong>出边上</strong>，而不是节点本身。<br />
								请点击从该节点出发的连线，在连线属性中配置条件表达式。
							</div>
						</template>
					</el-alert>
				</template>

				<template v-if="isEdge">
					<el-divider border-style="dashed">条件配置</el-divider>
					<el-form-item :label="isEdge ? '连线条件' : '条件表达式'">
						<ConditionBuilder
							ref="conditionBuilderRef"
							:key="`condition-${form.id || 'new'}-${props.formType}-${props.businessModule}`"
							v-model="form.conditionExpression"
							:form-json="props.formJson"
							:form-type="props.formType ?? 0"
							:business-module="props.businessModule"
						/>
					</el-form-item>
				</template>

				<el-divider border-style="dashed">备注</el-divider>
				<el-form-item label="说明">
					<el-input v-model="form.remark" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" placeholder="可填写节点说明或提示" />
				</el-form-item>
			</el-form>
		</template>
	</div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi, SysRoleApi, SysOrgApi } from '/@/api-services/system/api';
import ConditionBuilder from './ConditionBuilder.vue';
import ScopePicker from './scopePicker.vue';

interface PropertyForm {
	id: string;
	type: string;
	code: string;
	name: string;
	// 结束节点配置
	endStrategy: string;
	endNotifyStrategy: string[];
	// 审批配置
	approvalMode: string;
	approverType: string;
	approverScope: string;
	/** 未解析到审批人时的兜底动作：空=跟随全局配置，auto/admin/reject */
	noApproverAction: string;
	approverScopeName: string; // 审批范围名称
	autoPass: boolean;
	timeoutValue: number | null;
	timeoutUnit: string;
	rejectStrategy: string;
	todoNotifyChannels: string[];
	// 并行网关配置
	parallelGroup: number | null;
	parallelStrategy: string;
	// 条件/备注
	conditionExpression: string;
	remark: string;
	// 审批完成回调配置
	callbackHttpMethod: string;
	callbackUrl: string;
	callbackHeaders: string;
	callbackBody: string;
	// 抄送配置
	ccType: string;
	ccScope: string;
}

const props = defineProps<{
	lf: any;
	nodeData: any;
	formJson?: string; // 表单JSON，用于条件配置
	formType?: number; // 表单类型：0-自定义表单, 1-业务模块表单
	businessModule?: string; // 业务模块名称
	readonly?: boolean; // 只读模式：禁止编辑属性、隐藏保存按钮
}>();

const emit = defineEmits(['setPropertiesFinish']);
const saving = ref(false);
const errors = reactive<{ code?: string; name?: string; scope?: string }>({});
const conditionBuilderRef = ref<any>();
const form = reactive<PropertyForm>({
	id: '',
	type: '',
	code: '',
	name: '',
	// 结束节点配置
	endStrategy: 'auto',
	endNotifyStrategy: [],
	// 审批配置
	approvalMode: 'single',
	approverType: 'role',
	approverScope: '',
	noApproverAction: '', // 空=跟随全局配置
	approverScopeName: '',
	autoPass: false,
	timeoutValue: null,
	timeoutUnit: 'hour',
	rejectStrategy: 'terminate',
	todoNotifyChannels: ['message'],
	// 并行网关配置
	parallelGroup: null,
	parallelStrategy: 'all',
	// 条件/备注
	conditionExpression: '',
	remark: '',
	// 审批完成回调配置
	callbackHttpMethod: 'POST',
	callbackUrl: '',
	callbackHeaders: '',
	callbackBody: '',
	// 抄送配置
	ccType: '',
	ccScope: '',
});

const nodeData = computed(() => props.nodeData);
const isEdge = computed(() => nodeData.value?.elementType === 'edge' || nodeData.value?.type?.includes('edge'));
const isEndNode = computed(() => !isEdge.value && form.type === 'end-node');
const isParallelNode = computed(() => !isEdge.value && form.type === 'parallel-node');
const isUserTask = computed(() => !isEdge.value && form.type === 'user-node');
const isConditionNode = computed(() => !isEdge.value && form.type === 'gateway-node');
// 上级/下级部门都由发起人组织动态推导，无需配置审批范围
const approverTypeOptions = [
	{ label: '指定成员', value: 'user' },
	{ label: '指定角色', value: 'role' },
	{ label: '指定部门', value: 'dept' },
	{ label: '提交人上级部门审批', value: 'orgparent' },
	{ label: '提交人下级部门审批', value: 'orgchild' },
];
const ccTypeOptions = approverTypeOptions.slice(0, 4);

const orgApproverAlertTitle = computed(() => {
	const level = form.approverType === 'orgchild' ? '下级部门' : '上级部门';
	return `将根据<b>流程提交人</b>所在部门的<b>${level}</b>自动获取全部成员作为审批人，无需手动配置审批范围。`;
});
const orgCcAlertTitle = '当抄送人类型为<b>提交人上级部门</b>时，系统会根据<b>流程提交人</b>所在部门的<b>上级部门</b>自动获取全部成员作为抄送人，<b>无需手动配置抄送范围</b>。';
const ccFreeTextPlaceholder = `根据抄送人类型填写范围，多个用逗号分隔：
user 类型：user:1001,user:1002
role 类型：role:manager,role:leader
dept 类型：dept:100,dept:200
提交人上级部门类型可留空`;

/** 按类型生成下拉选项：部门走组织树，这里只给成员/角色 */
const buildScopeOptions = (type: string) => {
	if (type === 'user') {
		return userList.value.map((user) => ({
			label: `${user.realName || user.account} (user:${user.posName}/${user.orgName})`,
			value: `user:${user.id}`,
		}));
	}
	if (type === 'role') {
		return roleList.value.map((role) => ({
			label: `${role.name} (role:${role.code || role.id})`,
			value: `role:${role.code || role.id}`,
		}));
	}
	return [];
};
const approverPresetOptions = computed(() => buildScopeOptions(form.approverType));
const ccPresetOptions = computed(() => buildScopeOptions(form.ccType));

const handleFillEdgeNameByFirstCondition = () => {
	if (!isEdge.value) return;

	const edgeName = conditionBuilderRef.value?.getFirstConditionLabel?.()?.trim();
	if (!edgeName) {
		ElMessage.warning('请先配置完整的第一条连线条件');
		return;
	}
	form.name = edgeName;
};

const activeTab = ref('basic'); // Tab 选项卡：basic-基础信息, callback-审批完成回调

// 数据源
const userList = ref<any[]>([]);
const roleList = ref<any[]>([]);
const orgTreeData = ref<any[]>([]); // 部门树形数据
const orgTreeExpandedKeys = ref<number[]>([]);
const loadingData = ref(false);

/** 按类型加载下拉/树形数据源（抄送没有下级部门） */
const fetchScopeData = async (type: string) => {
	if (type === 'user') await fetchUserList();
	else if (type === 'role') await fetchRoleList();
	else if (type === 'dept') await fetchOrgTree();
};

const scopeItemsOf = (scope?: string) =>
	(scope || '')
		.split(',')
		.map((item) => item.trim())
		.filter((item) => !!item);

// 根据 approverScope 和 approverType 更新 approverScopeName
const updateApproverScopeName = () => {
	if (!form.approverScope) {
		form.approverScopeName = '';
		return;
	}

	const scopeItems = scopeItemsOf(form.approverScope);

	const names: string[] = [];

	if (form.approverType === 'user') {
		// 从 userList 中查找用户名
		scopeItems.forEach((item) => {
			const userId = item.replace(/^user:/, '');
			const user = userList.value.find((u) => u.id?.toString() === userId);
			if (user) {
				names.push(user.realName || user.account || userId);
			} else {
				names.push(item);
			}
		});
	} else if (form.approverType === 'role') {
		// 从 roleList 中查找角色名
		scopeItems.forEach((item) => {
			const roleCode = item.replace(/^role:/, '');
			const role = roleList.value.find((r) => (r.code || r.id)?.toString() === roleCode);
			if (role) {
				names.push(role.name || roleCode);
			} else {
				names.push(item);
			}
		});
	} else if (form.approverType === 'dept') {
		const findDept = (tree: any[], deptId: string): any => {
			for (const node of tree) {
				if (node.code?.toString() === deptId || node.id?.toString() === deptId) {
					return node;
				}
				if (node.children?.length) {
					const found = findDept(node.children, deptId);
					if (found) return found;
				}
			}
			return null;
		};
		// 从 orgTreeData 中查找部门名
		scopeItems.forEach((item) => {
			const dept = findDept(orgTreeData.value, item.replace(/^dept:/, ''));
			names.push(dept ? dept.name : item);
		});
	} else {
		// 其他类型直接使用原值
		form.approverScopeName = form.approverScope;
		return;
	}

	form.approverScopeName = names.join(',');
};

// 获取用户列表
const fetchUserList = async () => {
	if (userList.value.length > 0) return; // 已加载过，避免重复请求
	try {
		loadingData.value = true;
		// 使用分页接口获取所有用户（流程设计器需要能选择所有用户）
		const res = await getAPI(SysUserApi).apiSysUserPagePost({
			page: 1,
			pageSize: 10000, // 设置一个较大的值以获取所有用户
		});
		userList.value = res.data.result?.items ?? [];
	} catch (error) {
		console.error('获取用户列表失败:', error);
		ElMessage.error('获取用户列表失败');
	} finally {
		loadingData.value = false;
	}
};

// 获取角色列表
const fetchRoleList = async () => {
	if (roleList.value.length > 0) return; // 已加载过，避免重复请求
	try {
		loadingData.value = true;
		const res = await getAPI(SysRoleApi).apiSysRoleListGet();
		roleList.value = res.data.result ?? [];
	} catch (error) {
		console.error('获取角色列表失败:', error);
		ElMessage.error('获取角色列表失败');
	} finally {
		loadingData.value = false;
	}
};

// 获取部门树形数据（用于部门类型选择器）
const fetchOrgTree = async () => {
	if (orgTreeData.value.length > 0) return; // 已加载过，避免重复请求
	try {
		loadingData.value = true;
		// 使用树形接口获取所有部门，传入 id=0, level=100 获取完整的部门树
		const treeResult = (await getAPI(SysOrgApi).apiSysOrgChildTreeIdLevelGet(0, 100)).data.result ?? [];
		orgTreeData.value = treeResult;
		// 默认展开第一层
		if (treeResult.length > 0 && treeResult[0].id) {
			orgTreeExpandedKeys.value = [treeResult[0].id];
		}
	} catch (error) {
		console.error('获取部门树失败:', error);
		ElMessage.error('获取部门树失败');
	} finally {
		loadingData.value = false;
	}
};

// 审批人类型变化：加载数据源 → 动态推导类型清空范围，其余类型剔除与新类型不匹配的前缀
watch(
	() => form.approverType,
	async (newType) => {
		await fetchScopeData(newType);
		if (newType === 'orgparent' || newType === 'orgchild') {
			form.approverScope = '';
			form.approverScopeName = '';
		} else if (['user', 'role', 'dept'].includes(newType)) {
			const prefix = `${newType}:`;
			form.approverScope = scopeItemsOf(form.approverScope)
				.filter((item) => item.startsWith(prefix))
				.join(',');
			updateApproverScopeName();
		}
	}
);

// 抄送人类型变化：与审批人同理（抄送无下级部门类型）
watch(
	() => form.ccType,
	async (newType) => {
		await fetchScopeData(newType);
		if (newType === 'orgparent' || newType === 'orgchild') {
			form.ccScope = '';
		} else if (['user', 'role', 'dept'].includes(newType)) {
			const prefix = `${newType}:`;
			form.ccScope = scopeItemsOf(form.ccScope)
				.filter((item) => item.startsWith(prefix))
				.join(',');
		}
	}
);

const nodeDesc = computed(() => {
	if (!nodeData.value) return '未选择';
	return nodeData.value.text?.value || nodeData.value.properties?.name || nodeData.value.id || '未命名节点';
});

const fillForm = () => {
	const data = nodeData.value ?? {};

	// 如果是边或节点，都尝试从 LogicFlow 的图数据中获取最新的属性
	// 这样可以确保获取到已保存的属性，而不是事件回调中可能过时的数据
	let propsData = data.properties ?? {};
	if (props.lf && data.id) {
		try {
			const graphData = props.lf.getGraphData();
			if (isEdge.value) {
				// 对于边，从图数据中查找
				const edgeData = graphData.edges?.find((e: any) => e.id === data.id);
				if (edgeData && edgeData.properties) {
					// 优先使用图数据中的属性（包含最新的已保存属性）
					propsData = {
						...propsData,
						...edgeData.properties,
					};
				}
			} else {
				// 对于节点，也从图数据中查找（确保一致性）
				const nodeDataFromGraph = graphData.nodes?.find((n: any) => n.id === data.id);
				if (nodeDataFromGraph && nodeDataFromGraph.properties) {
					// 优先使用图数据中的属性
					propsData = {
						...propsData,
						...nodeDataFromGraph.properties,
					};
				}
			}
		} catch (error) {
			console.warn('[PropertyDialog] 获取图数据属性失败:', error);
		}
	}

	form.id = data.id || '';
	form.type = data.type || '';
	form.name = data.text?.value || propsData.name || '';

	// 重置 tab 为基础信息（如果是审批节点）
	if (form.type === 'user-node') {
		activeTab.value = 'basic';
	}
	form.code = propsData.code || propsData.nodeCode || data.id || '';
	form.endStrategy = propsData.endStrategy || 'auto';
	form.endNotifyStrategy = propsData.endNotifyStrategy ? [].concat(propsData.endNotifyStrategy) : [];
	form.parallelGroup = propsData.parallelGroup ?? null;
	form.parallelStrategy = propsData.parallelStrategy || 'all';
	form.approvalMode = propsData.approvalMode || 'single';
	form.approverType = propsData.approverType || 'role';
	form.approverScope = propsData.approverScope || '';
	form.noApproverAction = propsData.noApproverAction || ''; // 未解析到审批人时的兜底动作，空=跟随全局配置
	form.approverScopeName = propsData.approverScopeName || ''; // 加载已保存的名称
	form.autoPass = propsData.autoPass ?? false;
	form.timeoutValue = propsData.timeoutValue ?? null;
	form.timeoutUnit = propsData.timeoutUnit || 'hour';
	form.rejectStrategy = propsData.rejectStrategy || 'terminate';
	form.todoNotifyChannels = propsData.todoNotifyChannels ? [].concat(propsData.todoNotifyChannels) : ['message'];

	// 对于 conditionExpression 的处理：
	// 始终优先使用图数据中的值，确保已保存的值被正确加载
	// 这样可以避免在切换焦点时，值被错误重置
	const savedConditionExpression = propsData.conditionExpression || propsData.expression || '';

	// 无论是否切换了节点/边，都使用图数据中的值
	// 这样可以确保每次 fillForm 时，都能正确加载已保存的值
	// 注意：这可能会覆盖用户正在编辑的值，但这是必要的，因为我们需要确保从图数据中加载的是最新保存的值
	if (form.conditionExpression !== savedConditionExpression) {
		form.conditionExpression = savedConditionExpression;
	}

	form.remark = propsData.remark || '';
	// 审批完成回调配置
	form.callbackHttpMethod = propsData.callbackHttpMethod || 'POST';
	form.callbackUrl = propsData.callbackUrl || '';
	form.callbackHeaders = propsData.callbackHeaders || '';
	form.callbackBody = propsData.callbackBody || '';
	// 抄送配置
	form.ccType = propsData.ccType || '';
	form.ccScope = propsData.ccScope || '';
	errors.code = '';
	errors.name = '';
	errors.scope = '';

	// 数据源就绪后翻译审批范围名称（范围勾选态由 ScopePicker 内部按 modelValue 解析）
	fetchScopeData(form.approverType).then(updateApproverScopeName);
	if (form.ccType) fetchScopeData(form.ccType);
};

// 清空表单的辅助函数
const resetForm = () => {
	form.id = '';
	form.type = '';
	form.code = '';
	form.name = '';
	// 结束节点配置
	form.endStrategy = 'auto';
	form.endNotifyStrategy = [];
	// 审批配置
	form.approvalMode = 'single';
	form.approverType = 'role';
	form.approverScope = '';
	form.approverScopeName = '';
	form.autoPass = false;
	form.timeoutValue = null;
	form.timeoutUnit = 'hour';
	form.rejectStrategy = 'terminate';
	form.todoNotifyChannels = ['message'];
	// 并行网关配置
	form.parallelGroup = null;
	form.parallelStrategy = 'all';
	// 条件/备注
	form.conditionExpression = '';
	form.remark = '';
	form.callbackHttpMethod = 'POST';
	form.callbackUrl = '';
	form.callbackHeaders = '';
	form.callbackBody = '';
	// 抄送配置
	form.ccType = '';
	form.ccScope = '';
	activeTab.value = 'basic';
	errors.code = '';
	errors.name = '';
	errors.scope = '';
};

// 监听 nodeData 的变化，确保在切换节点/边时正确加载属性
// 使用两个 watch：
// 1. 监听 nodeData.value 本身，处理 null/undefined 的情况
// 2. 监听 nodeData.value?.id，处理 id 变化的情况
watch(
	() => nodeData.value,
	(newData, oldData) => {
		// 如果 nodeData 变为 null/undefined，完全清空表单
		if (!newData) {
			resetForm();
			return;
		}
		// 如果从 null/undefined 变为有值，填充表单
		if (!oldData && newData) {
			fillForm();
		}
	},
	{ immediate: true }
);

// 监听 nodeData 的 id 变化，确保在切换不同的节点/边时重新填充表单
// 这样可以避免在编辑过程中因为其他属性变化而刷新表单
watch(
	() => nodeData.value?.id,
	(newId, oldId) => {
		if (!nodeData.value) {
			resetForm();
			return;
		}
		// 只有当 id 变化时才重新填充表单（即选择了不同的节点/边）
		// 这样可以确保切换焦点后，能够正确加载已保存的属性
		if (newId !== oldId) {
			fillForm();
		}
	},
	{ immediate: true }
);

const handleSave = async () => {
	if (!props.lf || !form.id) {
		ElMessage.warning('请选择节点后再编辑属性');
		return;
	}
	errors.code = '';
	errors.name = '';
	errors.scope = '';
	if (!form.name || !form.name.trim()) {
		errors.name = '显示名称不能为空';
		ElMessage.error('请填写显示名称');
		return;
	}
	if (!isEdge.value && (!form.code || !/^[A-Za-z0-9_\-]+$/.test(form.code))) {
		errors.code = '节点编码仅限字母、数字、下划线或中划线';
		ElMessage.error('节点编码格式不正确');
		return;
	}
	// 提交人上级/下级部门由流程提交人动态推导，无需配置审批范围
	if (isUserTask.value && !['orgparent', 'orgchild'].includes(form.approverType) && !form.approverScope?.trim()) {
		errors.scope = '请配置审批范围';
		ElMessage.error('审批范围不能为空');
		return;
	}
	saving.value = true;
	try {
		// 更新文本（对于节点和边都适用）
		if (form.name) {
			props.lf.updateText(form.id, form.name);
		}

		const propertyPayload = isEdge.value
			? {
					conditionExpression: form.conditionExpression || '',
					name: form.name || '',
				}
			: {
					code: form.code,
					name: form.name,
					endStrategy: form.endStrategy,
					endNotifyStrategy: form.endNotifyStrategy,
					// 审批配置
					approvalMode: form.approvalMode,
					approverType: form.approverType,
					approverScope: form.approverScope,
					noApproverAction: form.noApproverAction,
					approverScopeName: form.approverScopeName,
					autoPass: form.autoPass,
					timeoutValue: form.timeoutValue,
					timeoutUnit: form.timeoutUnit,
					rejectStrategy: form.rejectStrategy,
					todoNotifyChannels: form.todoNotifyChannels,
					// 并行网关
					parallelGroup: form.parallelGroup,
					parallelStrategy: form.parallelStrategy,
					// 条件/备注
					conditionExpression: form.conditionExpression,
					remark: form.remark,
					// 审批完成回调配置
					callbackHttpMethod: form.callbackHttpMethod,
					callbackUrl: form.callbackUrl,
					callbackHeaders: form.callbackHeaders,
					callbackBody: form.callbackBody,
					// 抄送配置
					ccType: form.ccType,
					ccScope: form.ccScope,
				};

		// 合并属性，确保所有属性都被正确保存
		const mergedProperties = {
			...(nodeData.value?.properties || {}),
			...propertyPayload,
		};

		// 对于边，需要特别处理 conditionExpression，确保它被正确保存
		if (isEdge.value) {
			// 如果 conditionExpression 为空字符串，也要保存（表示清除条件）
			if (form.conditionExpression === '') {
				mergedProperties.conditionExpression = '';
				// 同时清除 expression（兼容旧版本）
				mergedProperties.expression = '';
			} else if (form.conditionExpression) {
				mergedProperties.conditionExpression = form.conditionExpression;
				// 同时设置 expression（兼容旧版本）
				mergedProperties.expression = form.conditionExpression;
			}
		}

		// 使用 setProperties 更新属性（对于节点和边都适用）
		// 注意：setProperties 会更新 LogicFlow 内部的图数据，但为了确保数据一致性，
		// 我们还需要手动更新图数据和 nodeData
		props.lf.setProperties(form.id, mergedProperties);

		// 获取最新的图数据，确保获取到 setProperties 更新后的数据
		const graphData = props.lf.getGraphData();

		// 对于边和节点，都需要确保属性被正确保存到图数据中
		if (isEdge.value) {
			// 直接更新图数据中的边属性，确保保存成功
			const edgeIndex = graphData.edges?.findIndex((e: any) => e.id === form.id);
			if (edgeIndex !== undefined && edgeIndex >= 0 && graphData.edges) {
				// 更新图数据中的边属性
				if (!graphData.edges[edgeIndex].properties) {
					graphData.edges[edgeIndex].properties = {};
				}
				// 使用 Object.assign 确保所有属性都被正确保存
				Object.assign(graphData.edges[edgeIndex].properties, mergedProperties);
			}
		} else {
			// 对于节点，也确保属性被正确保存到图数据中
			const nodeIndex = graphData.nodes?.findIndex((n: any) => n.id === form.id);
			if (nodeIndex !== undefined && nodeIndex >= 0 && graphData.nodes) {
				// 更新图数据中的节点属性
				if (!graphData.nodes[nodeIndex].properties) {
					graphData.nodes[nodeIndex].properties = {};
				}
				// 使用 Object.assign 确保所有属性都被正确保存
				Object.assign(graphData.nodes[nodeIndex].properties, mergedProperties);
			}
		}

		// 同时更新 nodeData 的 properties，确保属性面板能正确显示
		// 这样在切换焦点后再切换回来时，能够正确加载已保存的属性
		if (nodeData.value) {
			if (!nodeData.value.properties) {
				nodeData.value.properties = {};
			}
			// 使用 Object.assign 确保所有属性都被正确更新
			Object.assign(nodeData.value.properties, mergedProperties);
		}

		ElMessage.success('属性已保存');
		emit('setPropertiesFinish');
	} catch (error: any) {
		console.error('保存属性失败:', error);
		ElMessage.error(error?.message || '保存失败');
	} finally {
		saving.value = false;
	}
};
</script>

<style scoped>
.property-panel {
	height: 100%;
	display: flex;
	flex-direction: column;
	padding: 12px;
	/* 父级 .fd-pane 是 display:flex 的行容器，不撑开就会按内容收缩：
	   审批节点字段多看着是满的，开始/结束节点字段少右侧就空一大块 */
	flex: 1;
	width: 100%;
	min-width: 0;
}

.panel-header {
	position: sticky;
	top: -12px;
	/* 跟随主题色：与设计器头部同一套渐变，换主题色这里自动跟着变 */
	background: linear-gradient(118deg, var(--el-color-primary) 0%, var(--el-color-primary-dark-2) 96%);
	padding: 0 0 12px 0;
	margin: -12px -12px 10px -12px;
	padding-left: 12px;
	padding-right: 12px;
	padding-top: 12px;
	border-bottom: none;
	z-index: 100;
	box-shadow: 0 6px 16px -12px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
}

.panel-header-top {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 12px;
}

.panel-header-info {
	flex: 1;
	min-width: 0;
}

.panel-title {
	font-size: 15px;
	font-weight: 600;
	color: #fff;
}

.panel-subtitle {
	font-size: 12px;
	color: rgba(255, 255, 255, 0.76);
	margin-top: 4px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 头部是主题色实底，默认 primary 按钮会「蓝压蓝」看不见，改为白底主色字 */
.panel-header :deep(.el-button--primary:not(.is-text):not(.is-link)) {
	--el-button-bg-color: #fff;
	--el-button-border-color: #fff;
	--el-button-text-color: var(--el-color-primary);
	--el-button-hover-bg-color: color-mix(in srgb, #fff 90%, var(--el-color-primary));
	--el-button-hover-border-color: color-mix(in srgb, #fff 90%, var(--el-color-primary));
	--el-button-hover-text-color: var(--el-color-primary);
	--el-button-active-bg-color: color-mix(in srgb, #fff 82%, var(--el-color-primary));
	--el-button-active-border-color: color-mix(in srgb, #fff 82%, var(--el-color-primary));
	--el-button-active-text-color: var(--el-color-primary);
	box-shadow: 0 2px 8px -2px rgba(16, 24, 40, 0.22);
}

.panel-placeholder {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 0 8px;
}

.placeholder-inner {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	padding: 0 18px;
	text-align: center;
}

.placeholder-icon {
	width: 56px;
	height: 56px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 24px;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	margin-bottom: 4px;
}

.placeholder-title {
	margin: 0;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.placeholder-tip {
	margin: 0;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-text-color-placeholder);
}

.panel-form {
	flex: 1;
	overflow-y: auto;
	padding-right: 4px;
	padding-bottom: 10px;

	/* 让下拉、数字输入、文本域等一律铺满面板宽度，避免节点属性看起来「右半边空着」 */
	:deep(.el-select),
	:deep(.el-input-number),
	:deep(.el-date-editor) {
		width: 100%;
	}

	:deep(.el-form-item__content) {
		min-width: 0;
	}
}

.edge-name-actions {
	margin-top: 4px;
	line-height: 1;
}

.timeout-input-group {
	display: flex;
	align-items: center;
	width: 100%;
	flex-wrap: nowrap;
}

.approval-tabs-wrapper {
	margin-top: 10px;
	margin-bottom: 10px;
}

.approval-tabs {
	width: 100%;
}

:deep(.approval-tabs .el-tabs__header) {
	margin: 0 0 12px 0;
}

:deep(.approval-tabs .el-tabs__content) {
	padding: 0;
}

:deep(.approval-tabs .el-tab-pane) {
	padding: 0;
}

.tab-content {
	padding: 0;
}
</style>
