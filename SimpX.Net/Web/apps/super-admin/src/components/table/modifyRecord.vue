<template>
	<el-popover placement="bottom" width="360" trigger="hover">
		<template #reference>
			<el-text type="primary" class="cursor-default">
				<el-icon><ele-InfoFilled /></el-icon>{{ '详情' }}
			</el-text>
		</template>
		<el-descriptions direction="vertical" :column="2" border>
			<el-descriptions-item v-if="'createUserName' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-UserFilled /></el-icon>{{ '创建者' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.createUserName ?? '无' }}</el-tag>
			</el-descriptions-item>
			<el-descriptions-item v-if="'createTime' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-Calendar /></el-icon>{{ '创建时间' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.createTime ?? '无' }}</el-tag>
			</el-descriptions-item>
			<el-descriptions-item v-if="'updateUserName' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-UserFilled /></el-icon>{{ '修改者' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.updateUserName ?? '无' }}</el-tag>
			</el-descriptions-item>
			<el-descriptions-item v-if="'updateTime' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-Calendar /></el-icon>{{ '修改时间' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.updateTime ?? '无' }}</el-tag>
			</el-descriptions-item>

			<el-descriptions-item v-if="'createOrgName' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-OfficeBuilding /></el-icon>{{ '机构名称' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.createOrgName ?? '无' }}</el-tag>
			</el-descriptions-item>
			<el-descriptions-item v-if="'tenantId' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-School /></el-icon>{{ '租户名称' }}
					</el-text>
				</template>
				<el-tag>{{ props.data.tenantId ?? '无' }}</el-tag>
			</el-descriptions-item>

			<el-descriptions-item v-if="'remark' in props.data">
				<template #label>
					<el-text truncated>
						<el-icon><ele-Tickets /></el-icon>{{ '备注' }}
					</el-text>
				</template>
				{{ props.data.remark ?? '无' }}
			</el-descriptions-item>
		</el-descriptions>
	</el-popover>
</template>

<script lang="ts" setup>
const props = defineProps<{
	data: ModifyRecord;
}>();
</script>

<style scoped lang="scss">
.el-descriptions {
	// 弹层宽度固定，表格改用 fixed 布局并让内容省略，避免长文本把弹层撑破
	:deep(.el-descriptions__table) {
		table-layout: fixed;
	}
	:deep(.el-tag) {
		max-width: 100%;
	}
	:deep(.el-tag__content) {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
	}
}
</style>
