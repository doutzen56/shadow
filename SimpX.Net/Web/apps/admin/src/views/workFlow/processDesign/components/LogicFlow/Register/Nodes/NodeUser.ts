import { RectNode, RectNodeModel, h } from '@logicflow/core';
import { applySelectedOutline } from './nodeFactory';

/**
 * 审批模式 → 视觉主题
 * main 主色（图标盒）/ soft 卡片底色 / line 描边 / label 标题文案
 */
const MODE_THEME: Record<string, { main: string; soft: string; line: string; label: string }> = {
	single: { main: '#4B6BF7', soft: '#F3F6FF', line: '#C7D2FE', label: '单人审批' },
	and: { main: '#F79009', soft: '#FFF8EF', line: '#FED7AA', label: '会签 · 全部同意' },
	or: { main: '#12B76A', soft: '#EDFDF4', line: '#A6F4C5', label: '或签 · 任意同意' },
};

/** 审批人类型 → 展示文案 */
const APPROVER_TEXT: Record<string, string> = {
	user: '指定成员',
	role: '指定角色',
	dept: '指定部门',
	orgparent: '上级部门审批',
};

/** 卡片内文本区左边距（左侧色带宽度 + 10px 呼吸） */
const PADDING = 16;
/**
 * 左侧色带宽度
 * 色带窄于圆角半径时无法靠圆弧自然贴合，统一用 clipPath 裁到卡片轮廓内，
 * 因此这里可以自由调整粗细，不必与 CARD_RADIUS 保持同值
 */
const BAND = 6;
/** 卡片圆角半径 */
const CARD_RADIUS = 10;
/** 图标盒尺寸 */
const ICON_BOX = 28;

/**
 * 按字符预算截断文本，超出部分以省略号结尾
 * SVG 无法自动换行，只能按可用宽度估算可容纳的字符数
 */
const ellipsis = (text: string, maxChars: number) => (text.length > maxChars ? `${text.slice(0, maxChars)}…` : text);

/**
 * 自定义审批节点 - 视图层
 * 卡片式呈现：主色图标盒 + 审批模式标题 + 审批人配置摘要 + 抄送胶囊
 */
class UserNode extends RectNode {
	/**
	 * 取审批模式对应的视觉主题，未知模式回落「单人审批」
	 * @param mode - 审批模式：single / and / or
	 */
	getTheme(mode: string) {
		return MODE_THEME[mode] || MODE_THEME.single;
	}

	/**
	 * 主色圆角图标盒 + 白色人形图标（左侧垂直居中）
	 * @param left - 卡片左边界 x
	 * @param centerY - 卡片垂直中心 y
	 */
	getIconShape(left: number, centerY: number) {
		const { properties } = this.props.model;
		const { main } = this.getTheme(properties?.approvalMode || 'single');

		const x = left + PADDING;
		const y = centerY - ICON_BOX / 2;

		return h('g', {}, [
			h('rect', { x, y, width: ICON_BOX, height: ICON_BOX, rx: 8, fill: main }),
			h(
				'svg',
				{ x: x + 6, y: y + 6, width: ICON_BOX - 12, height: ICON_BOX - 12, viewBox: '0 0 24 24' },
				h('path', {
					fill: '#ffffff',
					d: 'M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z',
				})
			),
		]);
	}

	/**
	 * 抄送胶囊（右上角），仅在配置了抄送人时渲染
	 * @param right - 卡片右边界 x
	 * @param top - 胶囊顶部 y
	 */
	getCcBadgeShape(right: number, top: number) {
		const { properties } = this.props.model;
		if (!(properties?.ccType && properties?.ccScope)) return null;

		const w = 38;
		const hgt = 18;
		const x = right - w;

		return h('g', {}, [
			h('rect', { x, y: top, width: w, height: hgt, rx: hgt / 2, fill: '#ECFDF3', stroke: '#A6F4C5', strokeWidth: 1 }),
			h(
				'text',
				{ x: x + w / 2, y: top + hgt / 2 + 3.5, fill: '#027A48', fontSize: 10, fontWeight: 600, textAnchor: 'middle' },
				'抄送'
			),
		]);
	}

	/**
	 * 获取节点整体形状：卡片 + 图标盒 + 两行文本 + 抄送胶囊
	 */
	getShape() {
		const { model } = this.props;
		const { width, height, x, y, properties, id } = model;

		const theme = this.getTheme(properties?.approvalMode || 'single');

		const left = x - width / 2;
		const top = y - height / 2;
		const textX = left + PADDING + ICON_BOX + 10;

		// 抄送胶囊占据右上角，标题需为其预留空间
		const hasCc = !!(properties?.ccType && properties?.ccScope);
		const titleBudget = Math.max(4, Math.floor((left + width - PADDING - (hasCc ? 46 : 0) - textX) / 12));
		const subBudget = Math.max(4, Math.floor((left + width - PADDING - textX) / 11));

		const scopeText = properties?.approverScopeName || '未配置';
		const approverText = APPROVER_TEXT[properties?.approverType || 'role'] || '指定角色';

		// 色带裁切路径：色带窄于圆角，直接画矩形会从左上/左下圆角处溢出卡片，
		// 用与卡片同轮廓的 clipPath 裁掉溢出部分（id 需随节点唯一，避免多节点互相干扰）
		const clipId = `fd-user-band-${String(id ?? '').replace(/\W/g, '')}`;

		const elements = [
			// 节点 ID 转作悬浮提示，不再占用卡片版面
			h('title', {}, String(id ?? '')),
			h('defs', {}, h('clipPath', { id: clipId }, h('rect', { x: left, y: top, width, height, rx: CARD_RADIUS }))),
			// 卡片主体
			h('rect', { x: left, y: top, width, height, rx: CARD_RADIUS, fill: theme.soft, stroke: theme.line, strokeWidth: 1 }),
			// 左侧实心色带：随审批模式换色，裁切后与卡片圆角自然重合
			h('rect', { x: left, y: top, width: BAND, height, fill: theme.main, 'clip-path': `url(#${clipId})` }),
			// 主色图标盒
			this.getIconShape(left, y),
			// 标题：审批模式
			h(
				'text',
				{ x: textX, y: y - 3, fill: '#1D2939', fontSize: 13, fontWeight: 600, textAnchor: 'start' },
				ellipsis(theme.label, titleBudget)
			),
			// 副标题：审批人类型 · 审批范围
			h(
				'text',
				{ x: textX, y: y + 15, fill: '#667085', fontSize: 11, textAnchor: 'start' },
				ellipsis(`${approverText} · ${scopeText}`, subBudget)
			),
		];

		const ccBadge = this.getCcBadgeShape(left + width - PADDING, top + 8);
		if (ccBadge) elements.push(ccBadge);

		return h('g', { style: 'filter: drop-shadow(0 1px 3px rgba(16, 24, 40, 0.1))' }, elements);
	}
}

/**
 * 自定义审批节点 - 数据模型层
 * 管理节点的数据、尺寸、样式等
 */
class UserNodeModel extends RectNodeModel {
	/**
	 * 构造函数
	 */
	constructor(data, graphModel) {
		// 从 properties 中获取节点内容，默认为'审批节点'
		const nodeName = (data.properties && data.properties.name) || '审批节点';

		// 设置节点下方文本（LogicFlow 默认文本位置）
		data.text = {
			value: nodeName,
			x: data.x,
			y: data.y + 60,
		};

		super(data, graphModel);
	}

	/**
	 * 初始化节点数据
	 */
	initNodeData(data) {
		super.initNodeData(data);

		// 节点尺寸配置（宽度加宽以显示更多内容）
		this.width = 220; // 宽度增加到220px，显示完整的审批配置
		this.height = 80;
		this.radius = 10;

		// 初始化 properties
		if (!this.properties) {
			this.properties = {};
		}

		// 设置默认审批配置
		if (!this.properties.approvalMode) {
			this.properties.approvalMode = 'single';
		}
		if (!this.properties.approverType) {
			this.properties.approverType = 'role';
		}
		if (!this.properties.approverScope) {
			this.properties.approverScope = '';
		}
		if (!this.properties.approverScopeName) {
			this.properties.approverScopeName = '';
		}
	}

	/**
	 * 获取节点样式
	 */
	getNodeStyle() {
		const style = super.getNodeStyle();
		style.fill = '#ffffff';
		style.stroke = '#e4e7ec';
		style.strokeWidth = 1;
		return style;
	}

	/**
	 * 自定义锚点样式
	 */
	getAnchorStyle() {
		const style = super.getAnchorStyle();
		style.hover.r = 8;
		style.hover.fill = '#4B6BF7';
		style.hover.stroke = '#4B6BF7';
		return style;
	}

	/**
	 * 节点选中时的外框样式（与开始/结束/网关/并行统一）
	 */
	getOutlineStyle() {
		return applySelectedOutline(super.getOutlineStyle());
	}

	/**
	 * 更新节点显示内容（根据审批配置自动生成）
	 * 当审批配置发生变化时，调用此方法更新节点显示
	 */
	updateBusinessContent() {
		if (!this.graphModel || !this.id) return;

		// 触发节点重新渲染
		this.graphModel.eventCenter.emit('node:properties:change', {
			id: this.id,
			properties: this.properties,
		});

		// 更新节点数据
		this.graphModel.updateNode(this.id, {
			properties: this.properties,
		});
	}

	/**
	 * 设置审批模式
	 * @param {string} mode - 审批模式: single, and, or
	 */
	setApprovalMode(mode) {
		this.properties.approvalMode = mode;
		this.updateBusinessContent();
	}

	/**
	 * 设置审批人类型
	 * @param {string} type - 审批人类型: user, role, dept, orgparent
	 */
	setApproverType(type) {
		this.properties.approverType = type;
		this.updateBusinessContent();
	}

	/**
	 * 设置审批范围
	 * @param {string} scope - 审批范围字符串
	 */
	setApproverScope(scope) {
		this.properties.approverScope = scope;
		this.updateBusinessContent();
	}

	/**
	 * 设置审批范围名称
	 * @param {string} scopeName - 审批范围名称字符串
	 */
	setApproverScopeName(scopeName) {
		this.properties.approverScopeName = scopeName;
		this.updateBusinessContent();
	}

	/**
	 * 设置抄送类型
	 * @param {string} type - 抄送类型: user, role, dept
	 */
	setCcType(type) {
		this.properties.ccType = type;
		this.updateBusinessContent();
	}

	/**
	 * 设置抄送范围
	 * @param {string} scope - 抄送范围字符串
	 */
	setCcScope(scope) {
		this.properties.ccScope = scope;
		this.updateBusinessContent();
	}

	/**
	 * 批量设置审批配置
	 * @param {Object} config - 审批配置对象
	 */
	setApprovalConfig(config) {
		let hasChange = false;

		if (config.approvalMode !== undefined && this.properties.approvalMode !== config.approvalMode) {
			this.properties.approvalMode = config.approvalMode;
			hasChange = true;
		}
		if (config.approverType !== undefined && this.properties.approverType !== config.approverType) {
			this.properties.approverType = config.approverType;
			hasChange = true;
		}
		if (config.approverScope !== undefined && this.properties.approverScope !== config.approverScope) {
			this.properties.approverScope = config.approverScope;
			hasChange = true;
		}
		if (config.approverScopeName !== undefined && this.properties.approverScopeName !== config.approverScopeName) {
			this.properties.approverScopeName = config.approverScopeName;
			hasChange = true;
		}
		if (config.ccType !== undefined && this.properties.ccType !== config.ccType) {
			this.properties.ccType = config.ccType;
			hasChange = true;
		}
		if (config.ccScope !== undefined && this.properties.ccScope !== config.ccScope) {
			this.properties.ccScope = config.ccScope;
			hasChange = true;
		}

		if (hasChange) {
			this.updateBusinessContent();
		}
	}

	/**
	 * 获取审批配置
	 * @returns {Object} 审批配置对象
	 */
	getApprovalConfig() {
		return {
			approvalMode: this.properties?.approvalMode || 'single',
			approverType: this.properties?.approverType || 'role',
			approverScope: this.properties?.approverScope || '',
			approverScopeName: this.properties?.approverScopeName || '',
			ccType: this.properties?.ccType,
			ccScope: this.properties?.ccScope,
		};
	}
}

// 导出节点配置
export default {
	type: 'user-node',
	view: UserNode,
	model: UserNodeModel,
};
