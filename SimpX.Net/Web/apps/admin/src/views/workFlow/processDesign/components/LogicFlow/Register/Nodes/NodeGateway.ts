import { createDiamondNode } from './nodeFactory';

/**
 * 条件网关节点：琥珀色菱形 + 白色分支图标
 * 条件表达式配置在出边上，而不是节点本身
 */
export default createDiamondNode({
	type: 'gateway-node',
	label: '条件',
	fill: '#f79009',
	stroke: '#dc6803',
	shadow: 'rgba(247, 144, 9, 0.35)',
	iconSize: 24,
	iconStrokeWidth: 2,
	iconPaths: ['M4 12h3', 'M7 12l6-5h4', 'M7 12l6 5h4'],
});
