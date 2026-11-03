/**
 * 知识库图谱接口
 *
 * 后端新增的图谱接口还没进 swagger 生成流程（生成需要后端运行并重新执行 build.bat），
 * 这里先用统一的 axios 实例手写一份；等前端接口重新生成后可以直接替换成生成的 AgentKmsApi 调用。
 */
import { axiosInstance, serveConfig } from '/@/utils/axios-utils';

/** 图谱节点类型 */
export type KmsGraphNodeType = 'kms' | 'doc' | 'chunk';

/** 图谱边类型：归属 / 语义相似 */
export type KmsGraphEdgeType = 'contains' | 'semantic';

/** 图谱节点 */
export interface KmsGraphNode {
	id: string;
	type: KmsGraphNodeType;
	label: string;
	size: number;
	cluster: number;
	docId?: string | number | null;
	docName?: string;
	chunkIndex?: number | null;
	content?: string;
	degree: number;
}

/** 图谱边 */
export interface KmsGraphEdge {
	id: string;
	source: string;
	target: string;
	type: KmsGraphEdgeType;
	weight: number;
}

/** 图谱中的文档项 */
export interface KmsGraphDoc {
	id: string | number;
	name: string;
	fileType: string;
	chunkCount: number;
	sampledCount: number;
	cluster: number;
	status: number;
}

/** 图谱数据 */
export interface KmsGraphData {
	kmsId: string | number;
	name: string;
	docCount: number;
	chunkCount: number;
	totalChunkCount: number;
	truncated: boolean;
	minScore: number;
	semanticEdgeCount: number;
	warning: string;
	generatedAt: string;
	nodes: KmsGraphNode[];
	edges: KmsGraphEdge[];
	docs: KmsGraphDoc[];
}

/** 图谱查询参数 */
export interface KmsGraphQuery {
	/** 知识库 Id（长整型超过 JS 安全范围，按字符串传递） */
	kmsId: string | number;
	includeChunks: boolean;
	minScore: number;
	maxChunks: number;
}

const BASE_PATH = (serveConfig as unknown as { basePath?: string })?.basePath ?? '';

/**
 * 拉取知识库图谱
 * @param query 查询参数
 * @returns 图谱数据
 */
export const fetchKmsGraph = async (query: KmsGraphQuery): Promise<KmsGraphData> => {
	const response = await axiosInstance.post(`${BASE_PATH}/api/agentKms/graph`, query);
	const body = response?.data ?? {};
	if (body.code !== undefined && body.code !== 200) {
		throw new Error(body.message || '获取知识图谱失败');
	}
	if (!body.result) throw new Error('知识图谱数据为空，请稍后重试');
	return body.result as KmsGraphData;
};
