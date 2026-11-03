/** 卡片主色档位：与 page.scss 的 .entity-card.is-* 一一对应，只用 Element 语义色，不引入新色值 */
export type CardAccent = 'primary' | 'warning' | 'success' | 'danger' | 'info';

/**
 * 卡片主色：全站统一走主题色。
 * 早前按名称 hash 散成五档，同一屏会出现绿 / 橙 / 红 / 灰混排 —— 卡片颜色并不承载任何信息，
 * 反而让列表显得杂。边框、图标、图标底色统一取主题色后，层级只由布局与留白表达；
 * 类型、状态这些真正需要区分的信息，一律交给卡片右上角的文字标签承载。
 * 入参仅为兼容既有调用点保留，不再参与取色。
 */
export const cardAccent = (_seed?: string | null): CardAccent => 'primary';

/** 取可直接写进 :class 的类名（即 is-primary） */
export const cardAccentClass = (seed?: string | null) => `is-${cardAccent(seed)}`;
