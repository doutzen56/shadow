import { pinyin } from 'pinyin-pro';

/**
 * 拼音匹配：支持全拼、首字母及二者混合（如"系统监控"可被 xtjk/xitong/xtjiankong 命中）。
 * 逐字生成 [全拼, 首字母] 序列后做贪心连续匹配，避免为每个菜单预存拼音串。
 */
export const matchPinyin = (title: string, query: string) => {
	if (!/[一-龥]/.test(title)) return false;
	const spellings: [string, string][] = title.split('').map((char) => {
		if (!/[一-龥]/.test(char)) return [char.toLowerCase(), char.toLowerCase()];		return [pinyin(char, { toneType: 'none', type: 'array' })[0], pinyin(char, { pattern: 'first', toneType: 'none', type: 'array' })[0]];
	});
	const match = (idx: number, qIdx: number): boolean => {
		if (qIdx >= query.length) return true;
		if (idx >= spellings.length) return false;
		const [full, first] = spellings[idx];
		if (query.startsWith(first, qIdx) && match(idx + 1, qIdx + first.length)) return true;
		if (query.startsWith(full, qIdx) && match(idx + 1, qIdx + full.length)) return true;
		return match(idx + 1, qIdx);
	};
	return match(0, 0);
};
