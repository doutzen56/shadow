import { h } from '@logicflow/core';
import { centerGlyph, createTerminalNode } from './nodeFactory';

/**
 * 结束节点：红色圆圈 + 白色圆角方框（停止标志）
 * 只能作为连线终点，不能作为连线的起点
 */
export default createTerminalNode({
	type: 'end-node',
	label: '结束',
	fill: '#f04438',
	shadow: 'rgba(240, 68, 56, 0.32)',
	forbidAsSource: true,
	icon: (x, y) =>
		centerGlyph(
			x,
			y,
			22,
			h('rect', { x: 7, y: 7, width: 10, height: 10, rx: 2.5, fill: '#ffffff' })
		),
});
