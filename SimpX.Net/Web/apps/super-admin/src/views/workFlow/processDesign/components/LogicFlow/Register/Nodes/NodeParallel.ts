import { createDiamondNode } from './nodeFactory';

/**
 * 并行网关节点：紫色菱形 + 白色加号图标（与条件网关的琥珀色区分）
 * 用于流程中的并发执行分支
 */
export default createDiamondNode({
	type: 'parallel-node',
	label: '并行',
	fill: '#7a5af8',
	stroke: '#6938ef',
	shadow: 'rgba(122, 90, 248, 0.35)',
	iconSize: 26,
	iconStrokeWidth: 2.4,
	iconPaths: ['M12 5v14', 'M5 12h14'],
});
