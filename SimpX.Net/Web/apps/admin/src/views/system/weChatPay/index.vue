<template>
	<div class="weChatPay-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="订单号">
							<el-input v-model="state.queryParams.keyword" clearable="" placeholder="请输入订单号" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item label="创建时间">
							<el-date-picker placeholder="请选择创建时间" value-format="YYYY/MM/DD" type="daterange" v-model="state.queryParams.createTimeRange" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="align-self: stretch; height: auto; margin: 0 10px 5px 10px" direction="vertical" />

			<el-row>
				<el-col>
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery(true)" :loading="options.loading"> 查询 </el-button>
						<el-button icon="ele-Refresh" @click="resetQuery" :loading="options.loading"> 重置 </el-button>
					</el-button-group>
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Plus" @click="openAddDialog"> 添加模拟数据 </el-button>
				</template>
				<template #toolbar_tools> </template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_total="{ row }">
					{{ (row.total / 100).toFixed(2) }}
				</template>
				<template #row_tradeState="{ row }">
					<el-tag v-if="row.tradeState == 'SUCCESS'" type="success"> 完成 </el-tag>
					<el-tag v-else-if="row.tradeState == 'REFUND'" type="danger"> 退款 </el-tag>
					<el-tag v-else-if="row.tradeState == 'NOTPAY'" type="warning"> 未支付 </el-tag>
					<el-tag v-else type="info"> 未完成 </el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button
						size="small"
						text
						type="primary"
						v-if="row.qrcodeContent != null && row.qrcodeContent != '' && (row.tradeState === '' || !row.tradeState || row.tradeState == 'NOTPAY')"
						@click="openQrDialog(row.qrcodeContent)"
					>
						付款二维码
					</el-button>
					<el-button size="small" text type="primary" v-if="row.tradeState === 'REFUND'" @click="openRefundDialog(row.transactionId)">查看退款</el-button>
					<el-button size="small" text type="primary" v-if="row.tradeState === 'SUCCESS'" @click="doRefund(row)">全额退款</el-button>
					<el-button size="small" text type="primary" @click="doRefreshRecord(row)">刷新</el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="showAddDialog" draggable overflow destroy-on-close width="700px">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> 添加模拟数据 </span>
				</div>
			</template>
			<el-form ref="ruleFormRef" :model="addData" label-width="auto">
				<el-form-item label="商品名称" prop="description" :rules="[{ required: true, message: '商品名称不能为空', trigger: 'blur' }]">
					<el-input v-model="addData.description" placeholder="商品名称" clearable />
				</el-form-item>
				<el-form-item label="金额(分)" prop="total" :rules="[{ required: true, message: '金额(分)不能为空', trigger: 'blur' }]">
					<el-input v-model="addData.total" placeholder="填数字,单位是分" clearable />
				</el-form-item>
				<el-form-item label="附加信息">
					<el-input v-model="addData.attachment" clearable />
				</el-form-item>
				<el-form-item label="GoodsTag">
					<el-input v-model="addData.goodsTag" clearable />
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button icon="ele-CircleCloseFilled" @click="closeAddDialog">取 消</el-button>
					<el-button type="primary" icon="ele-CircleCheckFilled" @click="saveData">确 定</el-button>
				</span>
			</template>
		</el-dialog>

		<el-dialog v-model="showQrDialog">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> 付款二维码 </span>
				</div>
			</template>
			<div ref="qrDiv"></div>
		</el-dialog>

		<el-dialog v-model="showRefundDialog">
			<template #header>
				<div>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> 退款信息 </span>
				</div>
			</template>
			<vxe-grid v-bind="refundGridOptions" :data="subTableData">
				<template #refund_status="{ row }">
					<el-tag v-if="row.refundStatus == 'SUCCESS'" type="success"> 完成 </el-tag>
					<el-tag v-else-if="row.refundStatus == 'REFUND'" type="danger"> 退款 </el-tag>
					<el-tag v-else type="info"> 未完成 </el-tag>
				</template>
			</vxe-grid>
		</el-dialog>
	</div>
</template>

<!-- 微信支付 -->
<script setup lang="ts" name="sysWechatPay">
import { ref, nextTick, reactive } from 'vue';
import { ElMessageBox, ElMessage, ElForm } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridProps, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import QRCode from 'qrcodejs2-fixes';

import { getAPI } from '/@/utils/axios-utils';
import { SysWechatPayApi } from '/@/api-services/system/api';
import { SysWechatPay } from '/@/api-services/system/models';

const qrDiv = ref<HTMLElement | null>(null);
const showAddDialog = ref(false);
const showQrDialog = ref(false);
const showRefundDialog = ref(false);

const xGrid = ref<VxeGridInstance>();
const subTableData = ref<any>([]);
const addData = ref<any>({});
const ruleFormRef = ref<InstanceType<typeof ElForm>>();

// 退款信息弹窗表格
const refundGridOptions = reactive<VxeGridProps>({
	autoResize: true,
	align: 'center',
	columns: [
		{ field: 'seq', type: 'seq', title: '序号', width: 65 },
		{ field: 'outRefundNumber', title: '商户退款号', width: 210, showOverflow: 'tooltip' },
		{ field: 'transactionId', title: '支付订单号', width: 220, showOverflow: 'tooltip' },
		{ field: 'refund', title: '金额(分)', width: 80 },
		{ field: 'reason', title: '退款原因', width: 180, showOverflow: 'tooltip' },
		{ field: 'refundStatus', title: '状态', width: 80, slots: { default: 'refund_status' } },
		{ field: 'remark', title: '备注', width: 180, showOverflow: 'tooltip' },
		{ field: 'createTime', title: '创建时间', width: 150, showOverflow: 'tooltip' },
		{ field: 'successTime', title: '完成时间', width: 150, showOverflow: 'tooltip' },
	],
});

const state = reactive({
	queryParams: {
		keyword: undefined,
		createTimeRange: undefined,
	},
});

// 表格参数配置
const options = useVxeTable<SysWechatPay>(
	{
		id: 'sysWechatPay',
		name: '微信支付',
		columns: [
			{ field: 'seq', type: 'seq', title: '序号', width: 65, fixed: 'left' },
			{ field: 'outTradeNumber', title: '商户订单号', width: 180, showOverflow: 'tooltip' },
			{ field: 'transactionId', title: '支付订单号', width: 220, showOverflow: 'tooltip' },
			{ field: 'description', title: '描述', width: 180, showOverflow: 'tooltip' },
			{ field: 'total', title: '金额', width: 80, slots: { default: 'row_total' } },
			{ field: 'tradeState', title: '状态', width: 80, slots: { default: 'row_tradeState' } },
			{ field: 'attachment', title: '附加信息', width: 180, showOverflow: 'tooltip' },
			{ field: 'goodsTag', title: 'GoodsTag', width: 90, showOverflow: 'tooltip' },
			{ field: 'tags', title: '业务类型', width: 90, showOverflow: 'tooltip' },
			{ field: 'createTime', title: '创建时间', width: 150, showOverflow: 'tooltip' },
			{ field: 'successTime', title: '完成时间', width: 150, showOverflow: 'tooltip' },
			{ field: 'businessId', title: '业务ID', width: 130, showOverflow: 'tooltip' },
			{ field: 'buttons', title: '操作', width: 'auto', fixed: 'right', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid配置参数(此处可覆写任何参数)，参考vxe-table官方文档
	{
		rowConfig: { keyField: 'id' },
		proxyConfig: { autoLoad: true, ajax: { query: ({ page }) => handleQueryApi(page) } },
		pagerConfig: { pageSize: 50 },
		toolbarConfig: { export: true },
	}
);

// 表格事件
const gridEvents: VxeGridListeners<SysWechatPay> = {};

// 查询api
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
	let params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize });
	return getAPI(SysWechatPayApi).apiSysWechatPayPagePost(params);
};

// 查询操作
const handleQuery = async (reset = false) => {
	options.loading = true;
	reset ? await xGrid.value?.commitProxy('reload') : await xGrid.value?.commitProxy('query');
	options.loading = false;
};

// 重置操作
const resetQuery = async () => {
	state.queryParams.keyword = undefined;
	state.queryParams.createTimeRange = undefined;
	await handleQuery(true);
};

// 打开新增页面
const openAddDialog = () => {
	addData.value = {
		description: null,
		total: null,
		attachment: null,
		goodsTag: null,
	};
	showAddDialog.value = true;
};

// 关闭新增页面
const closeAddDialog = () => {
	showAddDialog.value = false;
};

// 打开扫码页面
const openQrDialog = (code: string) => {
	showQrDialog.value = true;
	nextTick(() => {
		(<HTMLElement>qrDiv.value).innerHTML = '';
		new QRCode(qrDiv.value, {
			text: code,
			width: 260,
			height: 260,
			colorDark: '#000000',
			colorLight: '#ffffff',
		});
	});
};

// 打开退款页面
const openRefundDialog = async (transactionId: string) => {
	var res = await getAPI(SysWechatPayApi).apiSysWechatPayRefundListGet(transactionId);
	let tmpRows = res.data.result ?? [];
	subTableData.value = tmpRows;
	showRefundDialog.value = true;
};

// 保存数据
const saveData = async () => {
	ruleFormRef.value?.validate(async (valid) => {
		if (valid) {
			var res = await getAPI(SysWechatPayApi).apiSysWechatPayPayTransactionNativePost(addData.value);
			closeAddDialog();
			let code = res.data.result?.qrcodeUrl;
			openQrDialog(code == undefined ? '' : code);
			await handleQuery();
		}
	});
};

// 退款
const doRefund = async (orderInfo: any) => {
	ElMessageBox.prompt(`确定进行退款：${orderInfo.total / 100}元？请输入退款理由`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
	})
		.then(async ({ value }) => {
			await getAPI(SysWechatPayApi).apiSysWechatPayRefundPost({
				outTradeNumber: orderInfo.outTradeNumber,
				reason: value,
				refund: orderInfo.total,
				total: orderInfo.total,
			});
			ElMessage.success(`退款申请成功：${value}`);
		})
		.catch(() => {
			ElMessage.error('取消操作');
		});
};

// 刷新支付信息
const doRefreshRecord = async (orderInfo: any) => {
	await getAPI(SysWechatPayApi).apiSysWechatPayPayInfoFromWechatTradeIdGet(orderInfo.outTradeNumber);
	await handleQuery();
};
</script>
