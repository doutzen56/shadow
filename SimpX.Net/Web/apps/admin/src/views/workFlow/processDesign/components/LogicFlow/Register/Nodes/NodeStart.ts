import { h } from '@logicflow/core';
import { centerGlyph, createTerminalNode } from './nodeFactory';

/**
 * 开始节点：青绿色圆圈 + 白色播放三角（与结束节点的方框图标形成呼应）
 * 只能作为连线起点，不能作为连线的终点
 */
export default createTerminalNode({
	type: 'start-node',
	label: '开始',
	fill: '#14b8a6',
	shadow: 'rgba(18, 161, 130, 0.32)',
	forbidAsTarget: true,
	icon: (x, y) =>
		centerGlyph(
			x,
			y,
			22,
			h('path', {
				d: 'M8 5.5a1 1 0 0 1 1.53-.85l9 6.5a1 1 0 0 1 0 1.7l-9 6.5A1 1 0 0 1 8 18.5v-13Z',
				fill: '#ffffff',
			})
		),
});
