import { readAdminList, readAdminRecordCount, toVxeProxyPage, type AdminApiPageInfo } from '/@/utils/admin-api';
import { showCaughtError } from '/@/utils/axios-utils';

/**
 * 业务列表统一入口：拉 info → toVxeProxyPage；失败弹错，禁止静默空表。
 * 拦截器已弹过的错误（含 502 节流）不再二次 ElMessage，避免中英双提示。
 */
export async function runAdminVxeQuery<T = Record<string, unknown>>(
	loader: () => Promise<AdminApiPageInfo<T> | Record<string, unknown> | null | undefined>,
	emptyMessage = '查询失败'
) {
	try {
		const info = await loader();
		return toVxeProxyPage(readAdminList<T>(info as any), readAdminRecordCount(info as any));
	} catch (e: any) {
		showCaughtError(e, emptyMessage);
		return toVxeProxyPage<T>([], 0);
	}
}
