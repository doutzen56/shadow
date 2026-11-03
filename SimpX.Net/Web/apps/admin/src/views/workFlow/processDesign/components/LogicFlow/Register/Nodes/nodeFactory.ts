import { CircleNode, CircleNodeModel, PolygonNode, PolygonNodeModel, h } from '@logicflow/core';

/** 选中态外框全站统一：主题蓝虚线（开始/结束/网关/并行/审批共用） */
export const applySelectedOutline = (style: any) => {
	style.stroke = '#4B6BF7';
	style.strokeWidth = 1.5;
	style.strokeDasharray = '4 2';
	return style;
};

/** 圆形节点图标：把 24x24 viewBox 的图元居中到节点坐标 (x, y) */
export const centerGlyph = (x: number, y: number, size: number, children: any) =>
	h('svg', { x: x - size / 2, y: y - size / 2, width: size, height: size, viewBox: '0 0 24 24' }, children);

export interface TerminalNodeConfig {
	type: string;
	/** 默认文案，同时用于连线规则提示（如「开始」「结束」） */
	label: string;
	fill: string;
	/** 节点下方投影色（rgba），与主题色同系 */
	shadow: string;
	/** 居中白色图标（24x24 viewBox） */
	icon: (x: number, y: number) => any;
	/** 禁止作为连线终点（开始节点） */
	forbidAsTarget?: boolean;
	/** 禁止作为连线起点（结束节点） */
	forbidAsSource?: boolean;
}

/**
 * 圆形开始/结束节点工厂
 * 视觉：同色 18% 光晕外圈 + 白色内环 + 居中图标，直径 40px，文本固定显示在下方 35px
 */
export const createTerminalNode = (config: TerminalNodeConfig) => {
	class TerminalNode extends CircleNode {
		getShape() {
			const { x, y, r } = this.props.model;
			const { fill } = this.props.model.getNodeStyle();

			return h('g', { style: `filter: drop-shadow(0 2px 6px ${config.shadow})` }, [
				h('circle', { cx: x, cy: y, r: r + 6, fill, opacity: 0.18 }),
				h('circle', { cx: x, cy: y, r, fill, stroke: '#ffffff', strokeWidth: 2 }),
				config.icon(x, y),
			]);
		}
	}

	class TerminalNodeModel extends CircleNodeModel {
		initNodeData(data) {
			// 文本显示在节点下方（LogicFlow 默认文本位置），不可拖拽、可双击编辑
			data.text = {
				value: (data.text && data.text.value) || config.label,
				x: data.x,
				y: data.y + 35,
				dragable: false,
				editable: true,
			};

			super.initNodeData(data);
			this.r = 20;
		}

		getNodeStyle() {
			const style = super.getNodeStyle();
			style.fill = config.fill;
			style.stroke = '#ffffff'; // 白色内环，节点边缘更干净
			style.strokeWidth = 2;
			return style;
		}

		getAnchorStyle() {
			const style = super.getAnchorStyle();
			style.hover.r = 8;
			style.hover.fill = 'rgb(24, 125, 255)';
			style.hover.stroke = 'rgb(24, 125, 255)';
			return style;
		}

		getOutlineStyle() {
			return applySelectedOutline(super.getOutlineStyle());
		}

		getConnectedTargetRules() {
			const rules = super.getConnectedTargetRules();
			if (config.forbidAsTarget) {
				rules.push({ message: `${config.label}节点不能作为连线的终点`, validate: () => false });
			}
			return rules;
		}

		getConnectedSourceRules() {
			const rules = super.getConnectedSourceRules();
			if (config.forbidAsSource) {
				rules.push({ message: `${config.label}节点不能作为连线的起点`, validate: () => false });
			}
			return rules;
		}
	}

	return { type: config.type, view: TerminalNode, model: TerminalNodeModel };
};

export interface DiamondNodeConfig {
	type: string;
	/** 默认文案（如「条件」「并行」） */
	label: string;
	fill: string;
	/** 略深的描边色，强化菱形轮廓 */
	stroke: string;
	shadow: string;
	/** 图标占宽（24x24 viewBox 内居中于菱形） */
	iconSize: number;
	iconStrokeWidth: number;
	/** 白色描边式图标的各路径 */
	iconPaths: string[];
}

/**
 * 菱形网关/并行节点工厂
 * 视觉：70x70 圆角描边菱形 + 居中描边图标，文本显示在下方 60px；boundSize 存入 properties 供视图取尺寸
 */
export const createDiamondNode = (config: DiamondNodeConfig) => {
	class DiamondNode extends PolygonNode {
		getIconShape() {
			const size = Number(this.props.model.properties?.boundSize ?? 80);
			const offset = size / 2 - config.iconSize / 2;

			return h(
				'svg',
				{ x: offset, y: offset, width: config.iconSize, height: config.iconSize, viewBox: '0 0 24 24', pointerEvents: 'none' },
				h(
					'g',
					{ stroke: '#ffffff', strokeWidth: config.iconStrokeWidth, fill: 'none', strokeLinecap: 'round', strokeLinejoin: 'round' },
					config.iconPaths.map((d) => h('path', { d }))
				)
			);
		}

		getShape() {
			const { x = 0, y = 0, points = [] } = this.props.model;
			const size = Number(this.props.model.properties?.boundSize ?? 80);
			const style = this.props.model.getNodeStyle();
			const pathPoints = points.map((point) => point.join(',')).join(' ');
			const transform = `matrix(1 0 0 1 ${x - size / 2} ${y - size / 2})`;

			return h('g', { transform, style: `filter: drop-shadow(0 2px 6px ${config.shadow})` }, [
				h('polygon', { points: pathPoints, fill: style.fill, stroke: style.stroke, strokeWidth: style.strokeWidth, strokeLinejoin: 'round' }),
				this.getIconShape(),
			]);
		}
	}

	class DiamondNodeModel extends PolygonNodeModel {
		initNodeData(data) {
			super.initNodeData(data);

			const x = this.x ?? data.x ?? 0;
			const y = this.y ?? data.y ?? 0;
			this.text = {
				value: data.text?.value || config.label,
				x: x,
				y: y + 60,
				draggable: false,
				editable: false,
			};

			const size = 70;
			const half = size / 2;

			if (!this.properties) {
				this.properties = {};
			}
			this.properties.boundSize = size;

			// 菱形四个顶点：上 -> 右 -> 下 -> 左
			this.points = [
				[half, 0],
				[size, half],
				[half, size],
				[0, half],
			];
		}

		getNodeStyle() {
			const style = super.getNodeStyle();
			style.fill = config.fill;
			style.stroke = config.stroke;
			style.strokeWidth = 2; // 配合 strokeLinejoin:round 柔化尖角
			return style;
		}

		getOutlineStyle() {
			return applySelectedOutline(super.getOutlineStyle());
		}
	}

	return { type: config.type, view: DiamondNode, model: DiamondNodeModel };
};
