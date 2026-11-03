export interface DocumentPermissionRow {
	objectType: number;
	objectId: number;
	objectName: string;
	permissionTypes: number[];
}

export interface DocumentPermissionPayloadItem {
	permissionType: number;
	objectType: number;
	objectIds: number[];
}

export const DOCUMENT_PERMISSION_OPTIONS = [
	{ label: '查看', value: 1, tagType: 'info' },
	{ label: '下载', value: 2, tagType: 'success' },
	{ label: '编辑', value: 3, tagType: 'warning' },
	{ label: '删除', value: 4, tagType: 'danger' },
	{ label: '分享', value: 5, tagType: 'primary' },
];

export const DOCUMENT_OBJECT_TYPE_OPTIONS = [
	{ label: '用户', value: 1, tagType: 'primary' },
	{ label: '部门', value: 2, tagType: 'success' },
	{ label: '角色', value: 3, tagType: 'warning' },
];

const uniqNumbers = (values: any[]) => Array.from(new Set(values.map((value) => Number(value)).filter((value) => Number.isFinite(value))));

export const clonePermissionRows = (rows: DocumentPermissionRow[] = []): DocumentPermissionRow[] =>
	rows.map((row) => ({
		objectType: Number(row.objectType),
		objectId: Number(row.objectId),
		objectName: row.objectName || '',
		permissionTypes: uniqNumbers(row.permissionTypes || []).sort((a, b) => a - b),
	}));

export const normalizePermissionRows = (permissions: any[] = []): DocumentPermissionRow[] => {
	const rowMap = new Map<string, DocumentPermissionRow>();

	permissions.forEach((permission) => {
		const objectType = Number(permission.objectType);
		const objectId = Number(permission.objectId);
		const permissionType = Number(permission.permissionType);
		if (!objectType || !objectId || !permissionType) return;

		const key = `${objectType}_${objectId}`;
		if (!rowMap.has(key)) {
			rowMap.set(key, {
				objectType,
				objectId,
				objectName: permission.objectName || '',
				permissionTypes: [],
			});
		}

		const row = rowMap.get(key)!;
		if (!row.permissionTypes.includes(permissionType)) {
			row.permissionTypes.push(permissionType);
		}
	});

	return Array.from(rowMap.values()).map((row) => ({
		...row,
		permissionTypes: row.permissionTypes.sort((a, b) => a - b),
	}));
};

export const buildPermissionPayload = (rows: DocumentPermissionRow[] = []): DocumentPermissionPayloadItem[] => {
	const permissionMap = new Map<string, number[]>();

	rows.forEach((row) => {
		const permissionTypes = uniqNumbers(row.permissionTypes || []);
		permissionTypes.forEach((permissionType) => {
			const key = `${permissionType}_${row.objectType}`;
			if (!permissionMap.has(key)) {
				permissionMap.set(key, []);
			}

			const objectIds = permissionMap.get(key)!;
			if (!objectIds.includes(row.objectId)) {
				objectIds.push(row.objectId);
			}
		});
	});

	return Array.from(permissionMap.entries()).map(([key, objectIds]) => {
		const [permissionType, objectType] = key.split('_').map(Number);
		return {
			permissionType,
			objectType,
			objectIds,
		};
	});
};

export const appendPermissionPayloadToFormData = (formData: FormData, permissions: DocumentPermissionPayloadItem[]) => {
	permissions.forEach((permission, permissionIndex) => {
		formData.append(`Permissions[${permissionIndex}].PermissionType`, permission.permissionType.toString());
		formData.append(`Permissions[${permissionIndex}].ObjectType`, permission.objectType.toString());
		permission.objectIds.forEach((objectId, objectIndex) => {
			formData.append(`Permissions[${permissionIndex}].ObjectIds[${objectIndex}]`, objectId.toString());
		});
	});
};
