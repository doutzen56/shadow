/**
 * 管理端导入文件解析（Excel / CSV → 行数组）。
 */
import ExcelJS from 'exceljs';

/** 单元格纯文本 */
export function cellText(v: unknown): string {
	if (v == null) return '';
	if (typeof v === 'object' && v && 'text' in (v as any)) return String((v as any).text ?? '').trim();
	return String(v).trim();
}

/**
 * 解析本地导入文件为二维字符串表。
 * @param file 文件
 * @param cols Excel 取前 N 列（默认 16）
 */
export async function parseAdminImportTable(file: File, cols = 16): Promise<string[][]> {
	const name = file.name.toLowerCase();
	const rows: string[][] = [];
	if (name.endsWith('.xlsx') || name.endsWith('.xls')) {
		const wb = new ExcelJS.Workbook();
		await wb.xlsx.load(await file.arrayBuffer());
		const sheet = wb.worksheets[0];
		if (!sheet) return rows;
		const indexes = Array.from({ length: cols }, (_, i) => i + 1);
		sheet.eachRow((row) => {
			rows.push(indexes.map((i) => cellText(row.getCell(i).value)));
		});
		return rows;
	}
	const text = await file.text();
	for (const line of text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean)) {
		rows.push(line.split(/[,，\t]/).map((p) => p.trim()));
	}
	return rows;
}

/** 是否表头行（用户名列） */
export function isUserNameHeader(first?: string) {
	return /^user(name)?$/i.test(String(first || '').trim());
}
