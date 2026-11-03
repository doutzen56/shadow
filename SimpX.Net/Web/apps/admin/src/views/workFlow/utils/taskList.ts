/**
 * 工作流任务列表页（待办 / 抄送 / 已办 / 我发起的）共用工具。
 * 后端响应统一为 { code, result } 包装（result = 分页对象或业务对象），
 * 因此这里不再保留 list / records / PascalCase 等多重兼容分支。
 */
import { ref } from 'vue';
import { ElMessage } from 'element-plus';
import { getWorkflowInstance, getAllFlowNodes } from '/@/api/workflow';

/** 解析分页响应，始终返回 { items, total } */
export const parsePagedResult = (res: any) => {
	const data = res?.data?.result;
	return { items: data?.items ?? [], total: data?.total ?? 0 };
};

/** 解析单对象响应（工作流实例、步骤、日志等） */
export const parseData = (res: any) => res?.data?.result ?? null;

/** 按实例编码获取表单记录与业务模块信息（详情弹窗共用） */
export const fetchInstanceFormRecord = async (instanceCode: string) => {
	try {
		const formRecordData = parseData(await getWorkflowInstance(instanceCode));
		return {
			formRecord: formRecordData?.formRecord ?? null,
			businessModule: formRecordData?.businessModule as string | undefined,
			businessId: formRecordData?.businessId as string | number | undefined,
		};
	} catch (error: any) {
		ElMessage.error(error?.message || '获取表单信息失败');
		return { formRecord: null, businessModule: undefined as string | undefined, businessId: undefined as string | number | undefined };
	}
};

/** 「全部节点」页签：状态 + 切页签时懒加载，多个任务详情页共用一份实现 */
export const useAllFlowNodes = () => {
	const loadingAllNodes = ref(false);
	const allNodesData = ref<any>(null);

	const loadAllFlowNodes = async (instanceCode: string) => {
		loadingAllNodes.value = true;
		try {
			allNodesData.value = parseData(await getAllFlowNodes(instanceCode));
		} catch (error: any) {
			ElMessage.error(error?.message || '获取全部节点信息失败');
		} finally {
			loadingAllNodes.value = false;
		}
	};

	/** 打开新实例详情前清空，避免复用上一实例的数据 */
	const resetFlowNodes = () => {
		allNodesData.value = null;
	};

	/** el-tabs @tab-click：首次切到「全部节点」才请求 */
	const handleNodesTabClick = async (tab: any, instanceCode?: string) => {
		if (tab.paneName === 'allNodes' && !allNodesData.value && instanceCode) {
			await loadAllFlowNodes(instanceCode);
		}
	};

	return { loadingAllNodes, allNodesData, loadAllFlowNodes, resetFlowNodes, handleNodesTabClick };
};
