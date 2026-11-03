/**
 * 工作流画布元素注册：14 类业务节点 + 统一连线
 *
 * 节点采用「卡片 + 左侧色带 + 图标块」的视觉，与后台卡片墙保持同一套语言；
 * 连线统一走 wf-edge（贝塞尔 + 流动虚线 + 箭头），条件/并行/循环体的路由标签由映射层生成。
 */
import LogicFlow, { BezierEdge, BezierEdgeModel, RectNode, RectNodeModel, h } from '@logicflow/core';
// LogicFlow 的布局全靠这份基础样式（.lf-graph 高度、.lf-grid 的绝对定位等），
// 不引入时网格会掉进普通文档流，把画布撑出一截可滚动的空白
import '@logicflow/core/dist/index.css';
import { WORKFLOW_NODE_META_LIST, WORKFLOW_NODE_TYPE, WorkflowNodeType, getNodeMeta } from '../types';

const CARD_WIDTH = 186;
const CARD_HEIGHT = 56;
const PILL_WIDTH = 116;
const PILL_HEIGHT = 34;

const withAlpha = (hex: string, alpha: number) => {
	const value = hex.replace('#', '');
	const r = parseInt(value.substring(0, 2), 16);
	const g = parseInt(value.substring(2, 4), 16);
	const b = parseInt(value.substring(4, 6), 16);
	return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/** 往白（amount > 0）或黑（amount < 0）方向混合，用于做同色系的渐变端点 */
const shade = (hex: string, amount: number) => {
	const value = hex.replace('#', '');
	const num = parseInt(value, 16);
	const target = amount < 0 ? 0 : 255;
	const ratio = Math.abs(amount);
	const mix = (channel: number) => Math.round(channel + (target - channel) * ratio);
	return `rgb(${mix((num >> 16) & 255)}, ${mix((num >> 8) & 255)}, ${mix(num & 255)})`;
};

/** 节点类型解析：properties.type 优先，其次按 LogicFlow 注册类型反查 */
const resolveNodeType = (model: any): WorkflowNodeType => {
	const fromProperties = Number(model?.properties?.type);
	if (fromProperties) return fromProperties as WorkflowNodeType;
	const matched = WORKFLOW_NODE_META_LIST.find((item) => item.lfType === model?.type);
	return matched?.type ?? WORKFLOW_NODE_TYPE.Agent;
};

/** 粗略估算单字宽度：空格更窄、半角字符约半字宽、中文整字宽（SVG 没有 text-overflow，只能估） */
const charWidth = (ch: string, fontSize: number) => (ch === ' ' ? fontSize * 0.35 : /[\x00-\xff]/.test(ch) ? fontSize * 0.55 : fontSize);

/** 粗略测量文本宽度 */
const measureText = (text: string, fontSize: number) => Array.from(text).reduce((sum, ch) => sum + charWidth(ch, fontSize), 0);

/** 按可用宽度截断文本，超出补省略号（省略号本身按一个字宽预留，避免顶出卡片） */
const truncateToWidth = (text: string, maxWidth: number, fontSize: number) => {
	let used = 0;
	let result = '';
	for (const ch of Array.from(text)) {
		const width = charWidth(ch, fontSize);
		if (used + width > maxWidth - fontSize) return `${result}…`;
		used += width;
		result += ch;
	}
	return result;
};

/** 节点图标（以节点中心为原点绘制，纯 SVG 图元，不依赖字体） */
const renderGlyph = (type: WorkflowNodeType, accent: string) => {
	switch (type) {
		case WORKFLOW_NODE_TYPE.Start:
			return h('path', { d: 'M-4,-7 L8,0 L-4,7 Z', fill: accent, stroke: 'none' });
		case WORKFLOW_NODE_TYPE.End:
			// 圆环里一颗实心方块：比单颗方块更像标准「停止」图元
			return h('g', {}, [
				h('circle', { cx: 0, cy: 0, r: 6.5, fill: 'none', stroke: accent, strokeWidth: 1.6 }),
				h('rect', { x: -3, y: -3, width: 6, height: 6, rx: 1.2, fill: accent, stroke: 'none' }),
			]);
		case WORKFLOW_NODE_TYPE.Agent:
			return h('path', {
				d: 'M0,-9 L2.5,-2.5 L9,0 L2.5,2.5 L0,9 L-2.5,2.5 L-9,0 L-2.5,-2.5 Z',
				fill: accent,
				stroke: 'none',
			});
		case WORKFLOW_NODE_TYPE.Condition:
			return h('path', { d: 'M0,-8 L8,0 L0,8 L-8,0 Z', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinejoin: 'round' });
		case WORKFLOW_NODE_TYPE.Parallel:
			return h('path', {
				d: 'M0,-9 L0,-2 M0,-2 L-7,5 M0,-2 L7,5 M-7,5 L-7,9 M7,5 L7,9',
				fill: 'none',
				stroke: accent,
				strokeWidth: 1.6,
				strokeLinecap: 'round',
				strokeLinejoin: 'round',
			});
		case WORKFLOW_NODE_TYPE.Approval:
			return h('path', { d: 'M-8,0 L-2,6 L8,-6', fill: 'none', stroke: accent, strokeWidth: 2, strokeLinecap: 'round', strokeLinejoin: 'round' });
		case WORKFLOW_NODE_TYPE.Knowledge:
			// 摊开的书本：检索类节点的通用语义
			return h('path', {
				d: 'M0,6.5 L0,-5 M0,-5 C-2.2,-7 -5.2,-7 -8,-6.2 L-8,5.2 C-5.2,4.4 -2.2,4.6 0,6.5 C2.2,4.6 5.2,4.4 8,5.2 L8,-6.2 C5.2,-7 2.2,-7 0,-5',
				fill: 'none',
				stroke: accent,
				strokeWidth: 1.5,
				strokeLinecap: 'round',
				strokeLinejoin: 'round',
			});
		case WORKFLOW_NODE_TYPE.Tool:
			// 插头：代表调用外部工具/插件能力
			return h('g', {}, [
				h('path', { d: 'M-4.5,-9 L-4.5,-4 M4.5,-9 L4.5,-4', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinecap: 'round' }),
				h('path', { d: 'M-6.5,-4 L6.5,-4 L6.5,0.5 A6.5,6.5 0 0 1 -6.5,0.5 Z', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinejoin: 'round' }),
				h('path', { d: 'M0,7 L0,4', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinecap: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.Http:
			return h('g', {}, [
				h('circle', { cx: 0, cy: 0, r: 8, fill: 'none', stroke: accent, strokeWidth: 1.6 }),
				h('path', { d: 'M-8,0 L8,0 M0,-8 C3.5,-4 3.5,4 0,8 M0,-8 C-3.5,-4 -3.5,4 0,8', fill: 'none', stroke: accent, strokeWidth: 1.4, strokeLinecap: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.Variable:
			return h('g', {}, [
				h('path', { d: 'M-3.5,-8 C-6.5,-8 -5.5,-1.4 -8,0 C-5.5,1.4 -6.5,8 -3.5,8', fill: 'none', stroke: accent, strokeWidth: 1.5, strokeLinecap: 'round' }),
				h('path', { d: 'M3.5,-8 C6.5,-8 5.5,-1.4 8,0 C5.5,1.4 6.5,8 3.5,8', fill: 'none', stroke: accent, strokeWidth: 1.5, strokeLinecap: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.Output:
			return h('g', {}, [
				h('path', { d: 'M-7,-8 L4,-8 L7,-5 L7,8 L-7,8 Z', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinejoin: 'round' }),
				h('path', { d: 'M4,-8 L4,-5 L7,-5', fill: 'none', stroke: accent, strokeWidth: 1.4, strokeLinejoin: 'round' }),
				h('path', { d: 'M0,-2 L0,4 M-3,1 L0,4 L3,1', fill: 'none', stroke: accent, strokeWidth: 1.5, strokeLinecap: 'round', strokeLinejoin: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.Loop:
			return h('g', {}, [
				h('path', { d: 'M6.5,-2 A7,7 0 1 0 6.5,2', fill: 'none', stroke: accent, strokeWidth: 1.7, strokeLinecap: 'round' }),
				h('path', { d: 'M6.5,-2 L7,-6.5 M6.5,-2 L2.5,-0.5', fill: 'none', stroke: accent, strokeWidth: 1.7, strokeLinecap: 'round', strokeLinejoin: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.Delay:
			return h('g', {}, [
				h('circle', { cx: 0, cy: 0, r: 8, fill: 'none', stroke: accent, strokeWidth: 1.6 }),
				h('path', { d: 'M0,-4 L0,0 L4,2', fill: 'none', stroke: accent, strokeWidth: 1.6, strokeLinecap: 'round', strokeLinejoin: 'round' }),
			]);
		case WORKFLOW_NODE_TYPE.SubWorkflow:
			return h('g', {}, [
				h('rect', { x: -9, y: -8, width: 9, height: 9, rx: 2.2, fill: 'none', stroke: accent, strokeWidth: 1.5 }),
				h('rect', { x: 1, y: -1, width: 8, height: 8, rx: 2, fill: 'none', stroke: accent, strokeWidth: 1.5 }),
				h('path', { d: 'M-5,1 L-5,3.5 L1,3.5', fill: 'none', stroke: accent, strokeWidth: 1.4, strokeLinecap: 'round', strokeLinejoin: 'round' }),
			]);
		default:
			return h('circle', { r: 4, fill: accent });
	}
};

class WorkflowNodeView extends RectNode {
	getShape() {
		const { model } = this.props as any;
		const { x, y, width, height, properties, isSelected, isHovered } = model;
		const type = resolveNodeType(model);
		const meta = getNodeMeta(type);
		const accent = meta.accent;
		const left = x - width / 2;
		const top = y - height / 2;

		if (meta.pill) {
			// 开始/结束是流程的「起止符」：实心主色胶囊 + 白色图元，和中间的白色卡片形成明确层级
			const fontSize = 12;
			const glyphWidth = 13;
			const gap = 7;
			const rawLabel = String(properties?.name ?? meta.defaultName);
			const label = truncateToWidth(rawLabel, width - 30 - glyphWidth - gap, fontSize);
			const contentWidth = glyphWidth + gap + measureText(label, fontSize);
			const startX = left + (width - contentWidth) / 2;
			const gradientId = `wf-pill-${String(model.id ?? type).replace(/[^\w-]/g, '')}`;

			return h('g', {}, [
				// 竖向渐变：顶部提亮、底部压暗，实心胶囊有体积但不靠「贴一层高光」
				h('defs', {}, [
					h('linearGradient', { id: gradientId, x1: 0, y1: 0, x2: 0, y2: 1 }, [
						h('stop', { offset: '0%', 'stop-color': shade(accent, 0.16) }),
						h('stop', { offset: '100%', 'stop-color': shade(accent, -0.12) }),
					]),
				]),
				// 落地投影带主色调，实心胶囊不会看起来像贴图
				h('rect', { x: left + 1, y: top + 3, width: width - 2, height, rx: height / 2, fill: withAlpha(accent, isHovered ? 0.3 : 0.2), stroke: 'none' }),
				isSelected ? h('rect', { x: left - 3, y: top - 3, width: width + 6, height: height + 6, rx: (height + 6) / 2, fill: 'none', stroke: withAlpha(accent, 0.22), strokeWidth: 3 }) : null,
				h('rect', { x: left, y: top, width, height, rx: height / 2, fill: `url(#${gradientId})`, stroke: 'none' }),
				h('g', { transform: `translate(${startX + glyphWidth / 2}, ${y}) scale(0.72)` }, [renderGlyph(type, '#FFFFFF')]),
				h(
					'text',
					{
						x: startX + glyphWidth + gap,
						y: y + 4,
						fill: '#FFFFFF',
						'font-size': `${fontSize}px`,
						'font-weight': '600',
						'letter-spacing': '0.5px',
						'text-anchor': 'start',
						style: 'pointer-events: none; user-select: none;',
					},
					label
				),
			]);
		}

		const title = String(properties?.name ?? meta.defaultName);
		const subtitle = String(properties?.summary ?? '');
		// 文本从 left+49 起排，右侧留 8px 内边距，可用宽度就是卡片宽度减去这两段
		const clipWidth = width - 57;
		// 色带比圆角半径窄，手工描边会在圆角处露头，直接拿卡片形状裁剪
		const bandClipId = `wf-card-${String(model.id ?? type).replace(/[^\w-]/g, '')}`;
		const bandWidth = 4;

		return h('g', {}, [
			h('defs', {}, [h('clipPath', { id: bandClipId }, [h('rect', { x: left, y: top, width, height, rx: 10 })])]),
			// 投影分两层：近处一层收边，远处一层做氛围，抬升但不脏
			h('rect', { x: left + 1.5, y: top + 2, width, height, rx: 10, fill: 'rgba(15, 23, 42, 0.05)', stroke: 'none' }),
			h('rect', { x: left, y: top + (isHovered ? 5 : 4), width, height, rx: 10, fill: `rgba(15, 23, 42, ${isHovered ? 0.04 : 0.028})`, stroke: 'none' }),
			// 选中时不加粗描边，而是补一圈同色柔光，避免卡片在不同缩放级别下「忽胖忽瘦」
			isSelected ? h('rect', { x: left - 2, y: top - 2, width: width + 4, height: height + 4, rx: 12, fill: 'none', stroke: withAlpha(accent, 0.15), strokeWidth: 3 }) : null,
			h('rect', {
				x: left,
				y: top,
				width,
				height,
				rx: 10,
				fill: 'var(--el-bg-color-overlay, #ffffff)',
				stroke: isSelected ? withAlpha(accent, 0.85) : isHovered ? withAlpha(accent, 0.5) : 'var(--el-border-color-lighter, #ebeef5)',
				strokeWidth: 1,
			}),
			// 左侧整条边框染成节点主色：满高、跟卡片圆角贴合，比原来的短色条更完整
			h('rect', { x: left, y: top, width: bandWidth, height, fill: accent, stroke: 'none', 'clip-path': `url(#${bandClipId})` }),
			// 图标块：实心主色圆角块 + 白色图元，和左侧色带同色，一眼能认出节点类型
			h('rect', { x: left + 14, y: y - 14, width: 28, height: 28, rx: 9, fill: accent, stroke: 'none' }),
			h('g', { transform: `translate(${left + 28}, ${y}) scale(0.78)` }, [renderGlyph(type, '#FFFFFF')]),
			h(
				'text',
				{
					x: left + 49,
					y: y - 4,
					fill: 'var(--el-text-color-primary, #303133)',
					'font-size': '13px',
					'font-weight': '600',
					'text-anchor': 'start',
					style: 'pointer-events: none; user-select: none;',
				},
				truncateToWidth(title, clipWidth, 13)
			),
			h(
				'text',
				{
					x: left + 49,
					y: y + 12.5,
					fill: 'var(--el-text-color-secondary, #909399)',
					'font-size': '11px',
					'text-anchor': 'start',
					style: 'pointer-events: none; user-select: none;',
				},
				truncateToWidth(subtitle, clipWidth, 11)
			),
		]);
	}
}

class WorkflowNodeModel extends RectNodeModel {
	initNodeData(data: any) {
		super.initNodeData(data);
		const type = resolveNodeType(data);
		const meta = getNodeMeta(type);
		this.width = meta.pill ? PILL_WIDTH : CARD_WIDTH;
		this.height = meta.pill ? PILL_HEIGHT : CARD_HEIGHT;
		this.radius = meta.pill ? PILL_HEIGHT / 2 : 11;
		this.text.editable = false;
		this.text.draggable = false;
	}

	getNodeStyle() {
		const style = super.getNodeStyle();
		style.fill = 'none';
		style.stroke = 'none';
		return style;
	}

	getAnchorStyle() {
		const style = super.getAnchorStyle();
		const accent = getNodeMeta(resolveNodeType(this)).accent;
		style.r = 3.5;
		style.fill = 'var(--el-bg-color-overlay, #ffffff)';
		// 锚点跟随节点主色：拖线时能一眼看出是从哪类节点出发的
		style.stroke = accent;
		style.strokeWidth = 1;
		style.hover.r = 6;
		style.hover.fill = accent;
		style.hover.stroke = accent;
		return style;
	}

	getOutlineStyle() {
		const style = super.getOutlineStyle();
		style.stroke = 'transparent';
		style.hoverStroke = 'transparent';
		return style;
	}

	private get nodeType(): WorkflowNodeType {
		return resolveNodeType(this);
	}

	getConnectedSourceRules() {
		const rules = super.getConnectedSourceRules();
		const type = this.nodeType;

		if (type === WORKFLOW_NODE_TYPE.End) {
			rules.push({ message: '结束节点不能作为连线的起点', validate: () => false } as any);
		}
		if (type === WORKFLOW_NODE_TYPE.Condition) {
			rules.push({ message: '条件分支请在右侧属性面板配置分支路由', validate: () => false } as any);
		}
		// 除条件（分支配置）与结束（无出边）外，所有节点都只允许一条出边
		if (type !== WORKFLOW_NODE_TYPE.Condition && type !== WORKFLOW_NODE_TYPE.End) {
			rules.push({
				message: '该节点最多只能有一条出边，多路路由请使用条件/并行节点',
				validate: () => (this.outgoing?.edges?.length ?? 0) === 0,
			} as any);
		}
		rules.push({
			message: '不能连接到节点自身',
			validate: (source: any, target: any) => !source || !target || source.id !== target.id,
		} as any);
		rules.push({
			message: '两个节点之间已存在连线',
			validate: (_source: any, target: any) => !this.outgoing?.edges?.some((edge: any) => edge.targetNodeId === target?.id),
		} as any);
		return rules;
	}

	getConnectedTargetRules() {
		const rules = super.getConnectedTargetRules();
		const type = this.nodeType;

		if (type === WORKFLOW_NODE_TYPE.Start) {
			rules.push({ message: '开始节点不能作为连线的终点', validate: () => false } as any);
		}
		// 并行分支与循环体都会把入口节点交给执行器直接跑，开始/人工审批节点不能作为入口
		if (type === WORKFLOW_NODE_TYPE.Approval) {
			rules.push({
				message: '人工审批节点不能作为并行分支或循环体的入口',
				validate: (source: any) => {
					const sourceType = Number(source?.properties?.type);
					return sourceType !== WORKFLOW_NODE_TYPE.Parallel && sourceType !== WORKFLOW_NODE_TYPE.Loop;
				},
			} as any);
		}
		return rules;
	}
}

class WorkflowEdgeModel extends BezierEdgeModel {
	initEdgeData(data: any) {
		super.initEdgeData(data);
		this.text.editable = false;
	}

	updatePoints() {
		// LogicFlow 默认把贝塞尔控制点外扩 100px：节点一近，曲线就会折返、覆盖到自己身上，
		// 虚线的间隙被后半段填掉，看起来就成了实线。这里按两端距离收敛。
		const start = this.startPoint;
		const end = this.endPoint;
		if (start && end) {
			this.offset = Math.min(40, Math.max(6, Math.hypot(end.x - start.x, end.y - start.y) / 3));
		}
		super.updatePoints();
	}

	getEdgeStyle() {
		const style = super.getEdgeStyle() as any;
		const projected = this.properties?.projected === true;
		const accent = String(this.properties?.accent ?? '');

		if (projected && accent) {
			// 分支/并行/循环体路由本身就是静态虚线（配置的可视化），不属于「主流程实线」，保持原样
			style.stroke = withAlpha(accent, 0.72);
			style.strokeWidth = 1.2;
			style.strokeDasharray = '5 4';
		} else {
			// 主流程不再画实线：这层路径只留作点击热区（透明 + 加宽，细线也好点中），
			// 可见的线由 getEdge() 里那条流动虚线承担
			style.stroke = 'transparent';
			style.strokeWidth = 10;
			style.strokeDasharray = '';
		}
		style.strokeLinecap = 'round';
		return style;
	}

	getTextStyle() {
		const style = super.getTextStyle() as any;
		style.fontSize = 11;
		style.fill = 'var(--el-text-color-regular, #606266)';
		return style;
	}

	getArrowStyle() {
		const style = super.getArrowStyle() as any;
		style.offset = 7;
		style.verticalLength = 3.5;
		style.strokeWidth = 1;
		// 主线描边是透明的（只做热区），箭头颜色必须显式给，否则会跟着一起消失
		const projected = this.properties?.projected === true;
		const accent = String(this.properties?.accent ?? '');
		const color = projected && accent ? withAlpha(accent, 0.72) : 'var(--el-color-primary, #409eff)';
		style.stroke = color;
		style.fill = color;
		return style;
	}
}

class WorkflowEdgeView extends BezierEdge {
	getEndArrow() {
		const { model } = this.props;
		const arrow = model.getArrowStyle() as any;
		const offset = arrow.offset ?? 8;
		const verticalLength = arrow.verticalLength ?? 4;
		const stroke = arrow.stroke || 'var(--el-border-color-darker, #c0c4cc)';
		return h('path', {
			d: `M${-offset / 4},0 L${(3 * offset) / 4},${verticalLength} L${(3 * offset) / 4},${-verticalLength} Z`,
			fill: stroke,
			stroke,
			strokeWidth: arrow.strokeWidth ?? 1,
			strokeLinecap: 'round',
			strokeLinejoin: 'round',
		});
	}

	getEdge() {
		const base = super.getEdge();
		const model = this.props.model as any;
		if (!model?.path) return base;

		const projected = model.properties?.projected === true;
		const accent = String(model.properties?.accent ?? '');
		const color = projected && accent ? withAlpha(accent, 0.72) : 'var(--el-color-primary, #409eff)';
		const path = String(model.path);

		// 投影边：静态虚线 + 淡柔光，不做流动
		if (projected) {
			return h('g', {}, [
				h('path', { d: path, fill: 'none', stroke: color, 'stroke-width': 5, 'stroke-linecap': 'round', opacity: 0.07, 'pointer-events': 'none' }),
				base,
			]);
		}

		const selected = !!model.isSelected;
		const hovered = !!model.isHovered;

		// 主流程只用一条流动细虚线表达（没有实线垫底），柔光只做极淡的衬底，不能把虚线糊成实线
		return h('g', {}, [
			h('path', { d: path, fill: 'none', stroke: color, 'stroke-width': selected ? 4 : 2.5, 'stroke-linecap': 'round', opacity: selected ? 0.16 : hovered ? 0.1 : 0.07, 'pointer-events': 'none' }),
			base,
			h('path', {
				d: path,
				class: 'wf-edge-flow',
				fill: 'none',
				stroke: color,
				// 细虚线：主线要保持「轻」，只做方向提示，不抢节点视觉
				'stroke-width': selected ? 1.8 : 1.2,
				'stroke-linecap': 'round',
				// 虚线周期 6+8=14，与 CSS 里 -14 的位移一致，循环时不会跳帧
				'stroke-dasharray': '6 8',
				'pointer-events': 'none',
			}),
		]);
	}
}

/** 注册全部业务节点与统一连线（重复注册同名类型会覆盖，热更新场景安全） */
export const registerWorkflowElements = (lf: LogicFlow) => {
	WORKFLOW_NODE_META_LIST.forEach((meta) => {
		lf.register({ type: meta.lfType, view: WorkflowNodeView, model: WorkflowNodeModel });
	});
	lf.register({ type: 'wf-edge', view: WorkflowEdgeView, model: WorkflowEdgeModel });
	lf.setDefaultEdgeType('wf-edge' as any);
};
