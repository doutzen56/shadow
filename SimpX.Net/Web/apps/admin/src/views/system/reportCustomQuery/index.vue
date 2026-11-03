<template>
	<div class="rcq-page">
		<!-- 顶部头图：统一使用 theme/hero.scss 的 .page-hero -->
		<div class="page-hero">
			<div class="page-hero__deco">
				<span class="page-hero__blob is-1"></span>
				<span class="page-hero__blob is-2"></span>
				<span class="page-hero__blob is-3"></span>
				<span class="page-hero__mesh"></span>
				<span class="page-hero__shine"></span>
			</div>
			<div class="page-hero__main">
				<div class="page-hero__left">
					<h2 class="page-hero__title">自定义查询</h2>
					<p class="page-hero__desc">可视化拼装 SELECT 语句：选表选列、配置连接关系与筛选条件，实时预览 SQL 并直接执行查看结果，方案可导出复用。</p>
				</div>
				<div class="page-hero__right">
					<div class="page-hero__stats">
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rcqStats.tables }}</span>
							<span class="page-hero__stat-k">数据来源表</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v is-on">{{ rcqStats.columns }}</span>
							<span class="page-hero__stat-k">显示列</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rcqStats.joins }}</span>
							<span class="page-hero__stat-k">连接关系</span>
						</div>
						<div class="page-hero__stat">
							<span class="page-hero__stat-v">{{ rcqStats.wheres }}</span>
							<span class="page-hero__stat-k">查询条件</span>
						</div>
					</div>
				</div>
			</div>
		</div>

		<div class="rcq-body" :class="{ 'is-stack': isStack }">
			<splitpanes class="default-theme" horizontal>
				<!-- ===== 上：四项配置 ===== -->
				<pane size="52">
					<div class="rcq-cfg">
						<div class="rcq-cfg-grid" :class="{ 'has-folded': anyFold }">
							<!-- 显示列表配置 -->
							<el-card shadow="hover" class="rcq-card" :class="{ 'is-folded': cfgFold.select }">
								<template #header>
									<div class="rcq-head">
										<span class="rcq-head-ico"><el-icon><ele-Grid /></el-icon></span>
										<span class="rcq-head-title">显示列表配置</span>
										<span class="rcq-head-chip">{{ state.dbConfigs.length }} 项</span>
										<div class="rcq-head-acts">
											<el-tooltip content="添加一项" placement="top">
												<el-button type="primary" icon="ele-Plus" size="small" plain circle @click="addDbConfig" />
											</el-tooltip>
											<el-tooltip content="清空全部" placement="top">
												<el-button type="danger" icon="ele-Delete" size="small" plain circle @click="clearDbConfig" />
											</el-tooltip>
											<el-tooltip :content="cfgFold.select ? '展开' : '折叠'" placement="top">
												<el-button size="small" plain circle :icon="cfgFold.select ? 'ele-ArrowDown' : 'ele-ArrowUp'" @click="cfgFold.select = !cfgFold.select" />
											</el-tooltip>
										</div>
									</div>
								</template>
								<div class="rcq-rows" v-show="!cfgFold.select">
							<div v-for="(selectConfig, index) in state.dbConfigs" :key="index" class="rcq-sel">
								<div class="rcq-row">
									<div class="rcq-field">
										<el-select v-model="selectConfig.selectedTables" filterable placeholder="选择表" @change="(val: string) => onTableChange(val, selectConfig)">
											<el-option v-for="item in state.dbTables" :key="item.name" :label="`${item.description ? item.description : ''}[${item.name}]`" :value="item.name" />
										</el-select>
									</div>
									<el-button type="danger" icon="ele-CloseBold" plain circle size="small" @click="removeDbConfig(index)" />
								</div>
								<div class="rcq-row is-sub">
									<div class="rcq-field">
										<el-select
											v-model="selectConfig.selectedColumns"
											multiple
											filterable
											collapse-tags
											collapse-tags-tooltip
											placeholder="选择列"
											@change="onSelectColumnsChange(selectConfig)"
										>
											<el-option
												v-for="column in selectConfig.dbColumns"
												:key="`${column.tableName}.${column.dbColumnName}`"
												:label="`${column.tableDescription ? column.tableDescription : ''}.${column.columnDescription ? column.columnDescription : ''}[${column.tableName}.${column.dbColumnName}]`"
													:value="`${column.tableName}.${column.dbColumnName}`"
												/>
											</el-select>
										</div>
										<el-tooltip :content="isAllColumns(selectConfig) ? '清空已选列' : '一次勾选本表全部列'" placement="top">
											<el-button
												size="small"
												plain
												:icon="isAllColumns(selectConfig) ? 'ele-Close' : 'ele-Check'"
												@click="toggleAllColumns(selectConfig)"
											/>
										</el-tooltip>
									</div>
								<!-- 每列单独指定聚合方式：同一张表可以「一列求和、一列原值」 -->
								<div v-if="selectConfig.selectedColumns.length" class="rcq-cols">
									<div v-for="full in selectConfig.selectedColumns" :key="full" class="rcq-col">
										<span class="rcq-col-name" :title="full">{{ columnTitle(selectConfig, full) }}</span>
										<el-select v-model="selectConfig.aggregates[full]" size="small" class="rcq-col-agg" @change="markSqlDirty">
											<el-option label="原值" value="None" />
											<el-option label="计数" value="COUNT" />
											<el-option label="求和" value="SUM" />
											<el-option label="平均" value="AVG" />
											<el-option label="最小" value="MIN" />
											<el-option label="最大" value="MAX" />
										</el-select>
									</div>
								</div>
							</div>
							<div v-if="state.dbConfigs.length === 0" class="rcq-blank">还没有选择要显示的表与列</div>
							<template v-else>
								<div v-if="hasAggregate" class="rcq-row is-group">
									<span class="rcq-eq">分组</span>
									<div class="rcq-field">
										<el-select
											v-model="state.groupBy"
											multiple
											filterable
											clearable
											collapse-tags
											collapse-tags-tooltip
											placeholder="留空时自动把所有非聚合列作为分组字段"
										>
											<el-option v-for="opt in allColumnOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
										</el-select>
									</div>
								</div>
								<div v-if="hasAggregate" class="rcq-tip">聚合统计已开启{{ state.groupBy.length ? '，按所选字段分组' : '，未指定分组字段时自动按所有非聚合列分组' }}</div>
							</template>
								</div>
							</el-card>

							<!-- 连接关系配置 -->
							<el-card shadow="hover" class="rcq-card" :class="{ 'is-folded': cfgFold.join }">
								<template #header>
									<div class="rcq-head">
										<span class="rcq-head-ico is-violet"><el-icon><ele-Connection /></el-icon></span>
										<span class="rcq-head-title">连接关系配置</span>
										<span class="rcq-head-chip">{{ state.joinConfigs.length }} 项</span>
										<div class="rcq-head-acts">
											<el-tooltip content="添加一项" placement="top">
												<el-button type="primary" icon="ele-Plus" size="small" plain circle @click="addJoinConfig" />
											</el-tooltip>
											<el-tooltip content="清空全部" placement="top">
												<el-button type="danger" icon="ele-Delete" size="small" plain circle @click="clearJoinConfig" />
											</el-tooltip>
											<el-tooltip :content="cfgFold.join ? '展开' : '折叠'" placement="top">
												<el-button size="small" plain circle :icon="cfgFold.join ? 'ele-ArrowDown' : 'ele-ArrowUp'" @click="cfgFold.join = !cfgFold.join" />
											</el-tooltip>
										</div>
									</div>
								</template>
								<div class="rcq-rows" v-show="!cfgFold.join">
									<div v-for="(joinConfig, index) in state.joinConfigs" :key="index" class="rcq-join">
										<div class="rcq-row">
											<div class="rcq-field is-narrow">
												<el-select v-model="joinConfig.joinType" placeholder="连接方式">
													<el-option label="左连接" value="LEFT JOIN" />
													<el-option label="右连接" value="RIGHT JOIN" />
													<el-option label="内连接" value="INNER JOIN" />
												</el-select>
											</div>
											<div class="rcq-field">
												<el-select v-model="joinConfig.leftTable" filterable placeholder="左表" @visible-change="(visible: boolean) => visible && getJoinTableColumns(joinConfig.leftTable, true, index)">
													<el-option v-for="item in state.dbTables" :key="item.name" :label="`${item.description ? item.description : ''}[${item.name}]`" :value="item.name" />
												</el-select>
											</div>
											<div class="rcq-field">
												<el-select v-model="joinConfig.rightTable" filterable placeholder="右表" @visible-change="(visible: boolean) => visible && getJoinTableColumns(joinConfig.rightTable, false, index)">
													<el-option v-for="item in state.dbTables" :key="item.name" :label="`${item.description ? item.description : ''}[${item.name}]`" :value="item.name" />
												</el-select>
											</div>
											<el-button-group>
												<el-tooltip content="增加字段配对" placement="top">
													<el-button type="primary" icon="ele-Plus" size="small" @click="addJoinTable(index)" />
												</el-tooltip>
												<el-tooltip content="删除该连接" placement="top">
													<el-button type="danger" icon="ele-Close" size="small" @click="delJoinTable(index)" />
												</el-tooltip>
											</el-button-group>
										</div>
										<div v-for="(item, kindex) in joinConfig.tableColumns" :key="kindex" class="rcq-row is-sub">
											<span class="rcq-eq">ON</span>
											<div class="rcq-field">
												<el-select v-model="item.leftTableColumn" filterable placeholder="左表字段" @visible-change="(visible: boolean) => visible && getJoinTableColumns(joinConfig.leftTable, true, index)">
													<el-option
														v-for="column in joinConfig.leftTableColumns"
														:key="column.dbColumnName"
														:label="`${column.columnDescription ? column.columnDescription : ''}[${column.tableName}.${column.dbColumnName}]`"
														:value="`${column.dbColumnName}`"
													/>
												</el-select>
											</div>
											<div class="rcq-field">
												<el-select v-model="item.rightTableColumn" filterable placeholder="右表字段" @visible-change="(visible: boolean) => visible && getJoinTableColumns(joinConfig.rightTable, false, index)">
													<el-option
														v-for="column in joinConfig.rightTableColumns"
														:key="column.dbColumnName"
														:label="`${column.columnDescription ? column.columnDescription : ''}[${column.tableName}.${column.dbColumnName}]`"
														:value="`${column.dbColumnName}`"
													/>
												</el-select>
											</div>
											<el-button type="danger" icon="ele-CloseBold" plain circle size="small" @click="removeJoinColumn(index, kindex)" />
										</div>
									</div>
									<div v-if="state.joinConfigs.length === 0" class="rcq-blank">单表查询无需配置连接关系</div>
								</div>
							</el-card>

							<!-- 查询条件配置 -->
							<el-card shadow="hover" class="rcq-card" :class="{ 'is-folded': cfgFold.where }">
								<template #header>
									<div class="rcq-head">
										<span class="rcq-head-ico is-amber"><el-icon><ele-Filter /></el-icon></span>
										<span class="rcq-head-title">查询条件配置</span>
										<span class="rcq-head-chip">{{ state.whereConfigs.length }} 项</span>
										<div class="rcq-head-acts">
											<el-tooltip content="添加一项" placement="top">
												<el-button type="primary" icon="ele-Plus" size="small" plain circle @click="addWhereConfig" />
											</el-tooltip>
											<el-tooltip content="清空全部" placement="top">
												<el-button type="danger" icon="ele-Delete" size="small" plain circle @click="clearWhereConfig" />
											</el-tooltip>
											<el-tooltip :content="cfgFold.where ? '展开' : '折叠'" placement="top">
												<el-button size="small" plain circle :icon="cfgFold.where ? 'ele-ArrowDown' : 'ele-ArrowUp'" @click="cfgFold.where = !cfgFold.where" />
											</el-tooltip>
										</div>
									</div>
								</template>
								<div class="rcq-rows" v-show="!cfgFold.where">
									<div v-for="(condition, index) in state.whereConfigs" :key="index" class="rcq-row">
										<!-- 第一条条件没有前导逻辑符，用 WHERE 占位让几列对齐 -->
										<div class="rcq-field is-mini">
											<span v-if="index === 0" class="rcq-tag">WHERE</span>
											<el-select v-else v-model="condition.joinType" placeholder="逻辑">
												<el-option label="且" value="AND" />
												<el-option label="或" value="OR" />
											</el-select>
										</div>
										<div class="rcq-field">
											<el-select v-model="condition.table" filterable placeholder="表" @change="changeWhereTable(condition)">
												<el-option v-for="item in state.dbTables" :key="item.name" :label="`${item.description ? item.description : ''}[${item.name}]`" :value="item.name" />
											</el-select>
										</div>
										<div class="rcq-field">
											<el-select v-model="condition.column" filterable placeholder="列" @change="changeWhereColumn(condition)">
												<el-option
													v-for="column in condition.columns"
													:key="column.dbColumnName"
													:label="`${column.columnDescription ? column.columnDescription : ''}[${column.dbColumnName}]`"
													:value="`${column.tableName}.${column.dbColumnName}`"
												/>
											</el-select>
										</div>
										<div class="rcq-field is-narrow">
											<el-select v-model="condition.operator" placeholder="操作符" @change="onOperatorChange(condition)">
												<el-option label="等于" value="=" />
												<el-option label="不等于" value="!=" />
												<el-option label="大于" value=">" />
												<el-option label="小于" value="<" />
												<el-option label="大于等于" value=">=" />
												<el-option label="小于等于" value="<=" />
												<el-option label="包含" value="CONTAINS" />
												<el-option label="不包含" value="NOT CONTAINS" />
												<el-option label="开头是" value="STARTSWITH" />
												<el-option label="结尾是" value="ENDSWITH" />
												<el-option label="属于" value="IN" />
												<el-option label="不属于" value="NOT IN" />
												<el-option label="为空" value="IS NULL" />
												<el-option label="不为空" value="IS NOT NULL" />
												<el-option label="介于" value="BETWEEN" />
												<el-option label="不介于" value="NOT BETWEEN" />
											</el-select>
										</div>

										<!-- 区间：起始值 + 结束值 -->
										<div v-if="isRangeOp(condition.operator)" class="rcq-field">
											<div class="rcq-range">
												<template v-for="(key, ki) in ['value', 'value2']" :key="key">
													<el-input-number v-if="isNumType(condition.dataType)" v-model="condition[key]" controls-position="right" class="rcq-num" />
													<el-date-picker
														v-else-if="isDateType(condition.dataType)"
														v-model="condition[key]"
														type="datetime"
														format="YYYY-MM-DD HH:mm:ss"
														value-format="YYYY-MM-DD HH:mm:ss"
														class="rcq-date"
													/>
													<el-input v-else v-model="condition[key]" :placeholder="ki === 0 ? '起始值' : '结束值'" />
												</template>
											</div>
										</div>

										<!-- 单值 / 多值 -->
										<div v-else-if="needsValue(condition.operator)" class="rcq-field">
											<el-input-number v-if="isNumType(condition.dataType)" v-model="condition.value" controls-position="right" class="rcq-num" />
											<el-date-picker
												v-else-if="isDateType(condition.dataType)"
												v-model="condition.value"
												type="datetime"
												format="YYYY-MM-DD HH:mm:ss"
												value-format="YYYY-MM-DD HH:mm:ss"
												class="rcq-date"
											/>
											<el-input v-else v-model="condition.value" :placeholder="isMultiOp(condition.operator) ? '多个值用英文逗号分隔' : '值'" />
										</div>

										<!-- 判空类不需要值 -->
										<div v-else class="rcq-none">无需填值</div>

										<div class="rcq-field is-mini">
											<el-select v-model="condition.group" placeholder="括号">
												<el-option label="—" :value="0" />
												<el-option label="①" :value="1" />
												<el-option label="②" :value="2" />
											</el-select>
										</div>
										<el-button type="danger" icon="ele-CloseBold" plain circle size="small" @click="removeWhereConfig(index)" />
									</div>
									<div v-if="state.whereConfigs.length === 0" class="rcq-blank">不填条件即查询全量数据</div>
								</div>
							</el-card>

							<!-- 数据排序配置 -->
							<el-card shadow="hover" class="rcq-card" :class="{ 'is-folded': cfgFold.order }">
								<template #header>
									<div class="rcq-head">
									<span class="rcq-head-ico is-green"><el-icon><ele-Sort /></el-icon></span>
									<span class="rcq-head-title">排序与输出配置</span>
										<span class="rcq-head-chip">{{ state.orderConfigs.length }} 项</span>
										<div class="rcq-head-acts">
											<el-tooltip content="添加一项" placement="top">
												<el-button type="primary" icon="ele-Plus" size="small" plain circle @click="addSortConfig" />
											</el-tooltip>
											<el-tooltip content="清空全部" placement="top">
												<el-button type="danger" icon="ele-Delete" size="small" plain circle @click="clearSortConfig" />
											</el-tooltip>
											<el-tooltip :content="cfgFold.order ? '展开' : '折叠'" placement="top">
												<el-button size="small" plain circle :icon="cfgFold.order ? 'ele-ArrowDown' : 'ele-ArrowUp'" @click="cfgFold.order = !cfgFold.order" />
											</el-tooltip>
										</div>
									</div>
								</template>
								<div class="rcq-rows" v-show="!cfgFold.order">
									<div v-for="(sortCondition, index) in state.orderConfigs" :key="index" class="rcq-row">
										<div class="rcq-field">
											<el-select v-model="sortCondition.table" filterable placeholder="表" @change="changeWhereTable(sortCondition)">
												<el-option v-for="item in state.dbTables" :key="item.name" :label="`${item.description ? item.description : ''}[${item.name}]`" :value="item.name" />
											</el-select>
										</div>
										<div class="rcq-field">
											<el-select v-model="sortCondition.column" filterable placeholder="列">
												<el-option
													v-for="column in sortCondition.columns"
													:key="column.dbColumnName"
													:label="`${column.columnDescription ? column.columnDescription : ''}[${column.dbColumnName}]`"
													:value="`${column.tableName}.${column.dbColumnName}`"
												/>
											</el-select>
										</div>
										<div class="rcq-field is-mini">
											<el-select v-model="sortCondition.direction" placeholder="方向">
												<el-option label="升序" value="ASC" />
												<el-option label="降序" value="DESC" />
											</el-select>
										</div>
										<div class="rcq-field is-mini">
											<el-input-number v-model="sortCondition.sort" controls-position="right" class="rcq-num" />
										</div>
										<el-button type="danger" icon="ele-CloseBold" plain circle size="small" @click="removeSortConfig(index)" />
									</div>
								<div v-if="state.orderConfigs.length === 0" class="rcq-blank">序号越小越优先</div>
								<!-- 输出选项：去重、分页、行数上限，统一收在这张卡的底部 -->
								<div class="rcq-opts">
									<el-checkbox v-model="state.distinct">去重 DISTINCT</el-checkbox>
									<el-tooltip content="填 0 表示不分页，直接按行数上限取" placement="top">
										<span class="rcq-opt">
											<span class="rcq-opt-k">每页</span>
											<el-input-number v-model="state.pageSize" :min="0" :max="1000" :step="10" controls-position="right" class="rcq-num is-mini" />
										</span>
									</el-tooltip>
									<span class="rcq-opt">
										<span class="rcq-opt-k">页码</span>
										<el-input-number v-model="state.pageIndex" :min="1" :max="500" controls-position="right" class="rcq-num is-mini" />
									</span>
									<el-tooltip content="系统硬上限 10000 行，填更大也会被收敛" placement="top">
										<span class="rcq-opt">
											<span class="rcq-opt-k">上限</span>
											<el-input-number v-model="state.maxRows" :min="1" :max="10000" :step="500" controls-position="right" class="rcq-num is-mini" />
										</span>
									</el-tooltip>
								</div>
							</div>
							</el-card>
						</div>
					</div>
				</pane>

				<!-- ===== 下：SQL 预览 + 查询结果 ===== -->
				<pane size="48">
					<div class="rcq-bottom">
						<el-card shadow="hover" class="rcq-card rcq-sql-card">
							<template #header>
								<div class="rcq-head">
									<span class="rcq-head-ico is-primary"><el-icon><ele-Document /></el-icon></span>
									<span class="rcq-head-title">SQL 语句</span>
									<span class="rcq-head-chip">{{ state.sqlString ? '已生成' : '待生成' }}</span>
									<div class="rcq-head-acts">
								<el-button icon="ele-View" size="small" plain :loading="state.previewing" @click="generateSQL"> 预览SQL </el-button>
								<el-button icon="ele-Position" size="small" type="primary" :loading="state.executing" @click="executeSQL()"> 执行查询 </el-button>
										<el-dropdown trigger="click" @command="(cmd: string) => handleSqlMore(cmd)">
											<el-button size="small" plain>
												更多<el-icon class="rcq-more-ico"><ele-ArrowDown /></el-icon>
											</el-button>
											<template #dropdown>
												<el-dropdown-menu>
													<el-dropdown-item command="copy" icon="ele-DocumentCopy" :disabled="!state.sqlString">复制 SQL</el-dropdown-item>
													<el-dropdown-item command="open" icon="ele-FolderOpened">打开方案</el-dropdown-item>
													<el-dropdown-item command="save" icon="ele-Download">保存方案</el-dropdown-item>
												</el-dropdown-menu>
											</template>
										</el-dropdown>
									</div>
								</div>
							</template>
							<pre v-if="state.sqlString" class="rcq-sql">{{ state.sqlString }}</pre>
							<div v-else class="rcq-blank is-sql">点击「预览SQL」生成语句，或「执行查询」直接取数</div>
						</el-card>

						<el-card shadow="hover" class="rcq-card rcq-res-card">
							<template #header>
								<div class="rcq-head">
							<span class="rcq-head-ico is-cyan"><el-icon><ele-DataAnalysis /></el-icon></span>
							<span class="rcq-head-title">查询结果</span>
							<span class="rcq-head-chip">{{ state.queryResult.length }} 行</span>
							<span v-if="state.execInfo.total !== null" class="rcq-head-chip is-plain">共 {{ state.execInfo.total }} 条</span>
							<span v-if="state.execInfo.pageSize > 0" class="rcq-head-chip is-plain">第 {{ state.execInfo.pageIndex }} 页</span>
							<span v-if="state.execInfo.elapsedMs" class="rcq-head-chip is-plain">{{ state.execInfo.elapsedMs }} ms</span>
							<span v-if="state.execInfo.truncated" class="rcq-head-chip is-warn">已达上限 {{ state.execInfo.maxRows }}</span>
							<div class="rcq-head-acts">
								<el-tooltip content="导出为 CSV（Excel 可直接打开）" placement="top">
									<el-button size="small" plain circle icon="ele-Download" :disabled="!state.queryResult.length" @click="exportCsv" />
								</el-tooltip>
								<el-button-group v-if="state.execInfo.pageSize > 0">
									<el-button size="small" :disabled="state.execInfo.pageIndex <= 1" icon="ele-ArrowLeft" @click="gotoPage(state.execInfo.pageIndex - 1)">上一页</el-button>
									<el-button size="small" :disabled="!state.execInfo.hasNext" @click="gotoPage(state.execInfo.pageIndex + 1)">
										下一页<el-icon class="rcq-more-ico"><ele-ArrowRight /></el-icon>
									</el-button>
								</el-button-group>
							</div>
						</div>
							</template>
							<div class="rcq-grid-wrap">
								<vxe-grid ref="xGrid" class="xGrid-style rcq-grid" v-bind="options">
									<template #empty>
										<div class="rcq-blank is-grid">暂无数据，点「执行查询」取数</div>
									</template>
								</vxe-grid>
							</div>
						</el-card>
					</div>
				</pane>
			</splitpanes>
		</div>
	</div>
</template>

<!-- 报表自定义查询 -->
<script lang="ts" setup name="sysReportCustomQuery">
import { reactive, onMounted, onUnmounted, computed, ref, watch, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
// import { format } from 'sql-formatter';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VxeGridInstance } from 'vxe-table';

import { getAPI } from '/@/utils/axios-utils';
import { SysDatabaseApi } from '/@/api-services/system/api';
import { DbColumnOutput, DbTableInfo } from '/@/api-services/system/models';

const state = reactive({
	// 数据库表和列集合
	dbTables: [] as Array<DbTableInfo>,
	// 数据显示列配置：aggregates 以「表名.列名」为键，逐列指定聚合方式
	dbConfigs: [] as Array<{
		selectedTables: string;
		selectedColumns: any[];
		dbColumns: any[];
		aggregates: Record<string, string>;
	}>,

	selectConfigs: [] as any,
	// 连接关系配置
	joinConfigs: [] as Array<{
		joinType: 'LEFT JOIN'; // 左连接LEFTJOIN、右连接RIGHTJOIN、内连接INNERJOIN
		leftTable: ''; // 左表名称
		rightTable: ''; // 右表名称
		tableColumns: Array<{ leftTableColumn: string; rightTableColumn: string }>; // 左右表列集合（一一对应）
		leftTableColumns: any[];
		rightTableColumns: any[];
	}>,
	// 查询条件配置
	whereConfigs: [] as Array<{
		table: string;
		column: string;
		operator: string; // 操作符
		value: any; // 值（区间/多值时为第一项）
		value2: any; // 区间结束值
		columns: any;
		dataType: string; // 列数据类型
		joinType: string; // 与上一条件的连接方式
		group: number; // 括号分组序号，0 表示不分组
	}>,
	// 数据排序配置
	orderConfigs: [] as Array<{
		table: string;
		column: string;
		sort: number; // 排序（从小到大）
		direction: string; // ASC / DESC
		columns: any;
	}>,
	// SQL语句
	sqlString: '',
	// 查询结果
	queryResult: [] as any,
	// 主库标识
	mainConfigId: '1300000000001',

	// ===== 输出选项：去重 / 分页 / 行数上限 =====
	distinct: false,
	pageIndex: 1,
	pageSize: 20,
	maxRows: 1000,
	// 分组字段（元素为「表名.列名」，留空时由服务端按非聚合列自动推导）
	groupBy: [] as string[],
	// 执行元信息：耗时、是否截断、是否还有下一页、符合条件的总行数
	execInfo: { rowCount: 0, elapsedMs: 0, truncated: false, hasNext: false, pageIndex: 1, pageSize: 0, maxRows: 0, total: null as number | null },
	previewing: false,
	executing: false,
});

/** 后端查询结果的实际列名：执行后由服务端回传，表格按它渲染 */
const resultColumns = ref<Array<{ name: string; label: string }>>([]);

/** 区间类操作符：需要起始值 + 结束值 */
const isRangeOp = (op: string) => ['BETWEEN', 'NOT BETWEEN'].includes(op);
/** 多值类操作符：值用英文逗号分隔 */
const isMultiOp = (op: string) => ['IN', 'NOT IN'].includes(op);
/** 除判空类外都需要填值 */
const needsValue = (op: string) => !['IS NULL', 'IS NOT NULL'].includes(op);

/** 按数据库列类型判断是否走数字控件（与后端 ConvertValue 的判定保持一致） */
const isNumType = (dataType: string) => {
	const t = (dataType ?? '').toLowerCase();
	return ['int', 'decimal', 'numeric', 'number', 'float', 'double', 'real', 'money', 'byte', 'short', 'long'].some((k) => t.includes(k));
};
/** 按数据库列类型判断是否走日期控件 */
const isDateType = (dataType: string) => {
	const t = (dataType ?? '').toLowerCase();
	return t.includes('date') || t.includes('time');
};

/** 切换操作符时清掉旧值：数字列填的字符串在换操作符后会变成非法参数 */
const onOperatorChange = (condition: any) => {
	condition.value = undefined;
	condition.value2 = undefined;
	state.sqlString = '';
};

/** 是否启用了聚合：任一显示列指定了聚合函数 */
const hasAggregate = computed(() =>
	state.dbConfigs.some((cfg: any) => Object.values(cfg.aggregates ?? {}).some((v: any) => v && v !== 'None'))
);

/** 已选列的下拉选项，供分组字段选择 */
const allColumnOptions = computed(() => {
	const opts: Array<{ value: string; label: string }> = [];
	state.dbConfigs.forEach((cfg: any) => {
		(cfg.selectedColumns ?? []).forEach((full: string) => opts.push({ value: full, label: columnTitle(cfg, full) }));
	});
	return opts;
});

/** 取列的中文名：表描述.列描述，取不到就退回列名 */
const columnTitle = (cfg: any, full: string) => {
	const [table, col] = String(full ?? '').split('.');
	const matched = (cfg?.dbColumns ?? []).find((c: any) => c.tableName === table && c.dbColumnName === col);
	if (!matched) return col ?? full;
	const prefix = matched.tableDescription ? `${matched.tableDescription}.` : '';
	return `${prefix}${matched.columnDescription || matched.dbColumnName}`;
};

/** 配置有变动时作废已生成的 SQL，避免展示与配置不一致 */
const markSqlDirty = () => {
	state.sqlString = '';
};

// 头图统计：把四块配置的条数直接搬到头图，省得用户来回数
const rcqStats = computed(() => ({
	tables: state.dbConfigs.filter((x) => !!x.selectedTables).length,
	columns: state.selectConfigs?.length ?? 0,
	joins: state.joinConfigs.length,
	wheres: state.whereConfigs.length,
}));

/**
 * 小屏适配：分栏布局要求上/下各拿到一半高度，窗口一矮（或一窄）四张配置卡就被压没。
 * 低于阈值时改为「整块竖向堆叠 + 外层滚动」，每张卡高度封顶、条目再多只在卡内竖向滚动，
 * 再配一个手动折叠，条件多时可以把不用的配置区收成一条标题。
 */
const isStack = ref(false);
const syncStack = () => {
	isStack.value = window.innerWidth < 1280 || window.innerHeight < 760;
};

/** 四张配置卡的折叠状态 */
const cfgFold = reactive({ select: false, join: false, where: false, order: false });
/** 是否有卡被折叠：折叠后网格改按内容排布，不再强行等分两行 */
const anyFold = computed(() => cfgFold.select || cfgFold.join || cfgFold.where || cfgFold.order);

// 获取xGrid实例引用
const xGrid = ref<VxeGridInstance>();

// 创建响应式的列配置：执行过查询就以服务端回传的列名为准，未执行时按已选列预展示
const dynamicColumns = computed(() => {
	const columns: any = [
		{
			field: 'seq',
			type: 'seq',
			title: '序号',
			width: 60,
			fixed: 'left',
		},
	];

	if (resultColumns.value.length > 0) {
		resultColumns.value.forEach((c) => {
			columns.push({ field: c.name, title: c.label || c.name, minWidth: 120, showOverflow: 'tooltip' });
		});
		return columns;
	}

	// 未执行时用已选列做预展示（聚合列在服务端返回前没有确定列名，这里只做占位）
	(state.selectConfigs ?? []).forEach((config: any) => {
		if (!config?.dbColumnName) return;
		columns.push({
			field: config.dbColumnName.substring(0, 1).toLowerCase() + (config.dbColumnName?.substring(1) ?? ''), //首字母小写,可根据后端返回的JSON格式修改
			title: config.tableDescription ? `${config.tableDescription}.${config.columnDescription}` : config.dbColumnName,
			showOverflow: 'tooltip',
		});
	});

	return columns;
});

// 列配置变化时刷新表格列（结果列以服务端回传为准，聚合列别名也在这里生效）
watch(
	() => dynamicColumns.value,
	() => {
		xGrid.value?.loadColumn(dynamicColumns.value);
	},
	{ deep: true }
);

// 表格参数配置
const options = useVxeTable<DbColumnOutput>(
	{
		id: 'sysCustomQuery',
		name: '自定义查询',
		columns: dynamicColumns.value as any,
	},
	{
		// 分页配置
		pagerConfig: { enabled: false },
		// 工具栏配置
		toolbarConfig: { export: true },
	}
);

// 页面初始化
onMounted(async () => {
	syncStack();
	window.addEventListener('resize', syncStack);

	// 查询表列表
	const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseTableListConfigIdGet(state.mainConfigId);
	res.data.result.forEach((element: any) => {
		// 排除zero_开头的表
		if (!element.name.startsWith('zero_')) {
			state.dbTables.push(element);
		}
	});
	updateSelectConfigs();
});

onUnmounted(() => {
	window.removeEventListener('resize', syncStack);
});

// 当选择表变化时，加载对应的列
const onTableChange = async (value: string, selectConfig: any) => {
	selectConfig.dbColumns = [];
	// 换表后旧列与聚合方式都不再适用
	selectConfig.selectedColumns = [];
	selectConfig.aggregates = {};
	if (!selectConfig.selectedTables) return;

	// 获取当前选择表的详细项
	const tableInfo = state.dbTables.find((table) => table.name === value);
	const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseColumnListTableNameConfigIdGet(value, state.mainConfigId);
	if (res.data.result && Array.isArray(res.data.result)) {
		res.data.result.forEach((newColumn: any) => {
			const columnWithTableDesc = {
				...newColumn,
				tableDescription: tableInfo ? tableInfo.description : '', // 添加表描述
			};
			selectConfig.dbColumns.push(columnWithTableDesc);
		});
	}
};

// 当列选择变化时：同步聚合字典、清理已移除列的残留配置
const onSelectColumnsChange = (selectConfig: any) => {
	const kept = new Set<string>(selectConfig.selectedColumns ?? []);
	Object.keys(selectConfig.aggregates ?? {}).forEach((key) => {
		if (!kept.has(key)) delete selectConfig.aggregates[key];
	});
	// 分组字段只能引用仍在显示的列
	state.groupBy = state.groupBy.filter((g) => allColumnOptions.value.some((o) => o.value === g));
	// 新勾上的列默认「原值」，避免下拉框空白看着像没配置
	kept.forEach((key) => {
		if (!selectConfig.aggregates[key]) selectConfig.aggregates[key] = 'None';
	});
	state.sqlString = '';
	updateSelectConfigs();
};

const updateSelectConfigs = () => {
	// 先清空现有数据，避免重复添加
	state.selectConfigs = [];

	// 遍历所有数据库配置
	state.dbConfigs.forEach((config) => {
		config.selectedColumns.forEach((colFullName: string) => {
			// 解析列名，格式为"表名.列名"
			const [tableName, dbColumnName] = colFullName.split('.');
			// 如果解析成功，在当前config的dbColumns中查找匹配的项
			if (tableName && dbColumnName) {
				const matchedColumn = config.dbColumns.find((column: any) => column.tableName === tableName && column.dbColumnName === dbColumnName);
				if (matchedColumn) {
					state.selectConfigs.push(matchedColumn);
				}
			}
		});
	});
};

/** 该表的列是否已全部勾选 */
const isAllColumns = (selectConfig: any): boolean =>
	!!selectConfig?.dbColumns?.length && (selectConfig.selectedColumns?.length ?? 0) >= selectConfig.dbColumns.length;

/** 一键全选 / 清空本表列：列多时逐条勾选太累，同时挡住后端 200 列的上限 */
const toggleAllColumns = (selectConfig: any) => {
	const all: string[] = (selectConfig?.dbColumns ?? []).map((c: any) => `${c.tableName}.${c.dbColumnName}`);
	if (!all.length) {
		ElMessage.warning('请先选择表');
		return;
	}

	if (isAllColumns(selectConfig)) {
		selectConfig.selectedColumns = [];
	} else {
		if (all.length > 200) {
			ElMessage.warning(`该表有 ${all.length} 列，最多只能选 200 列`);
			selectConfig.selectedColumns = all.slice(0, 200);
		} else {
			selectConfig.selectedColumns = all;
		}
	}

	onSelectColumnsChange(selectConfig);
};

// 添加数据显示列配置
const addDbConfig = () => {
	state.dbConfigs.push({
		selectedTables: '',
		selectedColumns: [],
		dbColumns: [],
		aggregates: {},
	});
	updateSelectConfigs();
};

// 删除数据显示列配置
const removeDbConfig = (index: number) => {
	state.dbConfigs.splice(index, 1);
	updateSelectConfigs();
};

// 清除数据显示列配置
const clearDbConfig = () => {
	state.dbConfigs = [];
	state.selectConfigs = [];
	updateSelectConfigs();
};

// 添加连接关系配置
const addJoinConfig = () => {
	state.joinConfigs.push({
		joinType: 'LEFT JOIN',
		leftTable: '',
		rightTable: '',
		tableColumns: [
			{
				leftTableColumn: '',
				rightTableColumn: '',
			},
		],
		leftTableColumns: [],
		rightTableColumns: [],
	});
};

// 清除连接关系配置
const clearJoinConfig = () => {
	state.joinConfigs = [];
};

// 添加连接关系配置对应表
const addJoinTable = (index: number) => {
	state.joinConfigs[index].tableColumns.push({
		leftTableColumn: '',
		rightTableColumn: '',
	});
};

// 删除连接关系配置对应表
const delJoinTable = (index: number) => {
	state.joinConfigs.splice(index, 1);
};

// 删除连接关系配置对应列
const removeJoinColumn = (index: number, keyIndex: number) => {
	state.joinConfigs[index].tableColumns.splice(keyIndex, 1);
};

// 获取连接关系配置表列数据
const getJoinTableColumns = async (tableName: string, isLeftTable: boolean = true, joinIndex: number) => {
	if (tableName) {
		try {
			const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseColumnListTableNameConfigIdGet(tableName, state.mainConfigId);
			const columns = res?.data?.result || [];

			// 根据isLeftTable参数决定更新哪个表的字段
			if (isLeftTable) {
				state.joinConfigs[joinIndex].leftTableColumns = columns;
			} else {
				state.joinConfigs[joinIndex].rightTableColumns = columns;
			}

			return columns;
		} catch (error) {
			console.error('获取表字段失败:', error);
			return [];
		}
	}
	return [];
};

// 改变查询条件表时
const changeWhereTable = async (condition: any) => {
	const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseColumnListTableNameConfigIdGet(condition.table, state.mainConfigId);
	condition.columns = res.data.result;
	condition.column = undefined;
	condition.value = undefined;
	condition.value2 = undefined;
};

// 改变查询条件列时
const changeWhereColumn = async (condition: any) => {
	// 获取当前列数据类型
	var column = condition.columns.find((column: any) => column.tableName + '.' + column.dbColumnName == condition.column);
	condition.dataType = column?.dataType ?? '';
	// 换列后旧值多半类型不匹配，一并清掉
	condition.value = undefined;
	condition.value2 = undefined;
};

// 添加查询条件配置
const addWhereConfig = () => {
	state.whereConfigs.push({
		table: '',
		column: '',
		operator: '=',
		value: '',
		value2: '',
		columns: [],
		dataType: '',
		joinType: 'AND',
		group: 0,
	});
};

// 移除查询条件配置
const removeWhereConfig = (index: number) => {
	state.whereConfigs.splice(index, 1);
};

// 清除所有查询条件配置
const clearWhereConfig = () => {
	state.whereConfigs = [];
};

// 添加排序配置
const addSortConfig = () => {
	state.orderConfigs.push({
		table: '',
		column: '',
		sort: 10,
		direction: 'ASC',
		columns: [],
	});
};

// 移除排序配置
const removeSortConfig = (index: number) => {
	state.orderConfigs.splice(index, 1);
};

// 清除所有排序配置
const clearSortConfig = () => {
	state.orderConfigs = [];
};

/** 统一转字符串：后端 Value 声明为 string，数字/日期控件的值必须显式转换后再提交 */
const toStr = (v: any): string => (v === null || v === undefined ? '' : String(v).trim());

/** 从「表名.列名」里取列名部分 */
const colName = (full: string): string => (String(full ?? '').includes('.') ? String(full).split('.').slice(1).join('.') : String(full ?? ''));

/** 组装提交给后端的查询配置（值一律转成字符串，交由服务端按列类型转换与参数化） */
const buildPayload = (): any => {
	const selectConfigs: any[] = [];
	state.dbConfigs.forEach((cfg: any) => {
		if (!cfg.selectedTables) return;
		(cfg.selectedColumns ?? []).forEach((full: string) => {
			const [table, column] = String(full).split('.');
			if (!table || !column) return;
			selectConfigs.push({ table, column, aggregate: cfg.aggregates?.[full] || 'None', alias: '' });
		});
	});

	return {
		selectConfigs,
		joinConfigs: state.joinConfigs
			.filter((c: any) => c.leftTable && c.rightTable)
			.map((c: any) => ({
				joinType: c.joinType,
				leftTable: c.leftTable,
				rightTable: c.rightTable,
				tableColumns: (c.tableColumns ?? []).filter((t: any) => t.leftTableColumn && t.rightTableColumn),
			})),
		whereConfigs: state.whereConfigs
			.filter((c: any) => c.table && c.column)
			.map((c: any) => ({
				table: c.table,
				column: colName(c.column),
				operator: c.operator,
				value: toStr(c.value),
				// 区间走 values，多值由服务端按逗号拆分
				values: isRangeOp(c.operator) ? [toStr(c.value), toStr(c.value2)] : [],
				dataType: c.dataType ?? '',
				joinType: c.joinType || 'AND',
				group: Number(c.group) || 0,
			})),
		orderConfigs: state.orderConfigs
			.filter((c: any) => c.table && c.column)
			.map((c: any) => ({
				table: c.table,
				column: colName(c.column),
				sort: Number(c.sort) || 10,
				direction: c.direction || 'ASC',
			})),
		groupByConfigs: state.groupBy
			.map((full: string) => ({ table: String(full).split('.')[0], column: colName(full) }))
			.filter((g: any) => g.table && g.column),
		distinct: state.distinct,
		pageIndex: Number(state.pageIndex) || 1,
		pageSize: Number(state.pageSize) || 0,
		maxRows: Number(state.maxRows) || 1000,
		configId: state.mainConfigId,
		// 只在分页时统计总数：不分页时行数就是上限，多跑一次计数没有意义
		withTotal: (Number(state.pageSize) || 0) > 0,
	};
};

/** 提交前本地校验：能在本端拦住的就不必打扰后端 */
const validatePayload = (payload: any): string => {
	if (!payload.selectConfigs?.length) return '请至少选择一个显示列';

	for (const w of payload.whereConfigs ?? []) {
		if (!needsValue(w.operator)) continue;
		if (isRangeOp(w.operator)) {
			if (!w.values[0] || !w.values[1]) return `「${w.column}」的区间条件需要填写起始值与结束值`;
		} else if (isMultiOp(w.operator)) {
			if (!w.value) return `「${w.column}」的条件需要填写值（多个值用英文逗号分隔）`;
		} else if (!w.value) {
			return `「${w.column}」的条件需要填写值`;
		}
	}

	for (const j of payload.joinConfigs ?? []) {
		if (!j.tableColumns?.length) return `连接 ${j.leftTable} → ${j.rightTable} 至少需要一个字段配对`;
	}

	return '';
};

// 预览SQL语句
const generateSQL = async () => {
	const payload = buildPayload();
	const err = validatePayload(payload);
	if (err) {
		ElMessage.warning(err);
		return;
	}

	state.previewing = true;
	try {
		const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseGenerateSQLPost(payload);
		state.sqlString = res?.data?.result ?? '';
	} finally {
		state.previewing = false;
	}
};

// 执行SQL：服务端同时回传列名与行数据，表格列按回传结果渲染
const executeSQL = async (pageIndex?: number) => {
	const payload = buildPayload();
	const err = validatePayload(payload);
	if (err) {
		ElMessage.warning(err);
		return;
	}
	if (pageIndex) {
		payload.pageIndex = pageIndex;
		state.pageIndex = pageIndex;
	}

	state.executing = true;
	try {
		const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseExecuteSQLPost(payload);
		const r = res?.data?.result ?? {};

		resultColumns.value = (r.columns ?? []).map((c: any) => ({ name: c.name, label: c.label || c.name }));
		state.queryResult = r.rows ?? [];
		state.execInfo = {
			rowCount: r.rowCount ?? state.queryResult.length,
			elapsedMs: r.elapsedMs ?? 0,
			truncated: !!r.truncated,
			hasNext: !!r.hasNext,
			pageIndex: r.pageIndex ?? 1,
			pageSize: r.pageSize ?? 0,
			maxRows: r.maxRows ?? 0,
			total: r.total ?? null,
		};
		// 回显服务端实际执行的语句，避免预览与执行两份配置不一致
		if (r.sql) state.sqlString = r.sql;

		await nextTick();
		xGrid.value?.loadColumn(dynamicColumns.value);
		xGrid.value?.loadData(state.queryResult);
	} finally {
		state.executing = false;
	}
};

/** 翻页：换页码后重新执行 */
const gotoPage = (pageIndex: number) => executeSQL(Math.max(Number(pageIndex) || 1, 1));

/** 导出当前结果为 CSV：带 BOM，Excel 打开中文不乱码；值里的引号/逗号按 RFC4180 转义 */
const exportCsv = () => {
	const cols = dynamicColumns.value.filter((c: any) => c.field !== 'seq');
	if (!cols.length) {
		ElMessage.warning('请先执行一次查询');
		return;
	}
	if (!state.queryResult?.length) {
		ElMessage.warning('当前没有可导出的结果');
		return;
	}

	const esc = (v: any) => {
		const s = v === null || v === undefined ? '' : String(v);
		return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
	};

	const lines = [cols.map((c: any) => esc(c.title)).join(',')];
	state.queryResult.forEach((row: any) => {
		lines.push(cols.map((c: any) => esc(row[c.field])).join(','));
	});

	const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = `查询结果_${new Date().toLocaleString('zh-CN').replace(/[\/\s:]/g, '-')}.csv`;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
	ElMessage.success(`已导出 ${state.queryResult.length} 行`);
};

// 复制SQL语句
const copySql = async () => {
	if (!state.sqlString) return;
	try {
		await navigator.clipboard.writeText(state.sqlString);
		ElMessage.success('SQL 已复制');
	} catch {
		ElMessage.warning('当前浏览器不支持自动复制，请手动选中');
	}
};

/** SQL 卡头「更多」：低频操作收进下拉，保证窄栏下操作区不换行 */
const handleSqlMore = (cmd: string) => {
	if (cmd === 'copy') copySql();
	else if (cmd === 'open') openSchema();
	else if (cmd === 'save') saveSchema();
};

/** 只刷新下拉用的列数据，不清空已选值（导入方案时需要保留用户原来的选择） */
const refreshColumns = async (item: any) => {
	if (!item?.table) return;
	try {
		const res: any = await getAPI(SysDatabaseApi).apiSysDatabaseColumnListTableNameConfigIdGet(item.table, state.mainConfigId);
		item.columns = res?.data?.result ?? [];
	} catch {
		item.columns = item.columns ?? [];
	}
};

/**
 * 导入方案后按当前库表重建元数据：文件里的列信息可能是旧版本或被手改过，
 * 直接用会导致「列名已不存在」的错误，这里统一重拉并剔除失效的列
 */
const refreshImportedMetadata = async () => {
	for (const cfg of state.dbConfigs) {
		if (!cfg.selectedTables) continue;
		const kept: string[] = [...(cfg.selectedColumns ?? [])];
		try {
			// onTableChange 会清空已选列，先存再还原
			await onTableChange(cfg.selectedTables, cfg);
		} catch {
			continue;
		}
		cfg.selectedColumns = kept.filter((full) => cfg.dbColumns.some((c: any) => `${c.tableName}.${c.dbColumnName}` === full));
	}

	for (const c of state.whereConfigs ?? []) {
		await refreshColumns(c);
		const exists = (c.columns ?? []).some((col: any) => `${col.tableName}.${col.dbColumnName}` === c.column);
		if (c.column && !exists) {
			c.column = '';
			c.value = '';
			c.value2 = '';
		}
	}
	for (const c of state.orderConfigs ?? []) {
		await refreshColumns(c);
		const exists = (c.columns ?? []).some((col: any) => `${col.tableName}.${col.dbColumnName}` === c.column);
		if (c.column && !exists) c.column = '';
	}

	updateSelectConfigs();
};

// 打开方案
const openSchema = () => {
	// 创建文件选择器
	const input = document.createElement('input');
	input.type = 'file';
	input.accept = '.json';

	input.onchange = (event) => {
		const file = (event.target as HTMLInputElement).files?.[0];
		if (!file) return;

		const reader = new FileReader();
		reader.onload = async (e) => {
			try {
				const schemaData = JSON.parse(e.target?.result as string);
				// 逐段做结构校验并补默认值：外部文件可能是旧版本或被手改过，不能直接赋值
				if (!schemaData || typeof schemaData !== 'object' || !Array.isArray(schemaData.dbConfigs)) throw new Error('缺少 dbConfigs');

				state.dbConfigs = schemaData.dbConfigs.map((c: any) => ({
					selectedTables: c?.selectedTables ?? '',
					selectedColumns: Array.isArray(c?.selectedColumns) ? c.selectedColumns : [],
					dbColumns: Array.isArray(c?.dbColumns) ? c.dbColumns : [],
					aggregates: c?.aggregates && typeof c.aggregates === 'object' ? c.aggregates : {},
				}));
				state.whereConfigs = (Array.isArray(schemaData.whereConfigs) ? schemaData.whereConfigs : []).map((c: any) => ({
					table: c?.table ?? '',
					column: c?.column ?? '',
					operator: c?.operator ?? '=',
					value: c?.value ?? '',
					value2: c?.value2 ?? '',
					columns: Array.isArray(c?.columns) ? c.columns : [],
					dataType: c?.dataType ?? '',
					joinType: c?.joinType ?? 'AND',
					group: Number(c?.group) || 0,
				}));
				state.joinConfigs = (Array.isArray(schemaData.joinConfigs) ? schemaData.joinConfigs : []).map((c: any) => ({
					joinType: c?.joinType ?? 'LEFT JOIN',
					leftTable: c?.leftTable ?? '',
					rightTable: c?.rightTable ?? '',
					tableColumns: Array.isArray(c?.tableColumns) && c.tableColumns.length ? c.tableColumns : [{ leftTableColumn: '', rightTableColumn: '' }],
					leftTableColumns: [],
					rightTableColumns: [],
				}));
				state.orderConfigs = (Array.isArray(schemaData.orderConfigs) ? schemaData.orderConfigs : []).map((c: any) => ({
					table: c?.table ?? '',
					column: c?.column ?? '',
					sort: Number(c?.sort) || 10,
					direction: c?.direction ?? 'ASC',
					columns: Array.isArray(c?.columns) ? c.columns : [],
				}));
				state.groupBy = Array.isArray(schemaData.groupBy) ? schemaData.groupBy : [];
				state.distinct = !!schemaData.distinct;
				state.pageIndex = Number(schemaData.pageIndex) || 1;
				state.pageSize = Number(schemaData.pageSize) || 0;
				state.maxRows = Number(schemaData.maxRows) || 1000;

				// 导入方案的库表元数据不可信，清空缓存后按已选表重新拉取
				resultColumns.value = [];
				state.queryResult = [];
				state.execInfo = { rowCount: 0, elapsedMs: 0, truncated: false, hasNext: false, pageIndex: 1, pageSize: 0, maxRows: 0, total: null };
				state.sqlString = '';
				updateSelectConfigs();

				// 方案里的列元数据按当前库表重建一次，避免沿用过期结构
				await refreshImportedMetadata();
				ElMessage.success('自定义查询方案加载成功');
			} catch (error) {
				ElMessage.error('自定义查询方案文件格式错误');
			}
		};

		reader.readAsText(file);
	};

	// 触发文件选择对话框
	input.click();
};

// 保存方案
const saveSchema = () => {
	if (!state.dbConfigs.some((c: any) => c.selectedColumns?.length)) {
		ElMessage.warning('请先选择要显示的表与列');
		return;
	}
	// 创建要保存的查询配置对象
	const schemaData = {
		version: 2,
		dbConfigs: state.dbConfigs, // 数据库配置（含逐列聚合方式）
		selectConfigs: state.selectConfigs, // 选中的列的详细信息
		whereConfigs: state.whereConfigs, // 查询条件
		orderConfigs: state.orderConfigs, // 排序条件
		joinConfigs: state.joinConfigs, // 关联键配置
		groupBy: state.groupBy, // 分组字段
		distinct: state.distinct, // 是否去重
		pageIndex: state.pageIndex, // 页码
		pageSize: state.pageSize, // 每页条数
		maxRows: state.maxRows, // 行数上限
	};

	// 转换为JSON字符串
	const jsonString = JSON.stringify(schemaData, null, 2);

	// 创建Blob对象
	const blob = new Blob([jsonString], { type: 'application/json' });

	// 创建下载链接
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	// 设置文件名，使用表名和当前时间作为标识
	const timestamp = new Date().toLocaleString('zh-CN').replace(/[\/\s:]/g, '-');
	a.download = `自定义查询方案_${timestamp}.json`;

	// 触发下载
	document.body.appendChild(a);
	a.click();

	// 清理
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
};
</script>

<style lang="scss" scoped>
.rcq-page {
	/* 页边距由 .layout-parent 的 padding:5px 提供，这里只补底部；
	   flex 列 + min-height:0 才能把剩余高度交给下面的 splitpanes */
	position: relative;
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	min-height: 0;
	padding-bottom: 4px;
	overflow: hidden;
}

.rcq-body {
	flex: 1 1 auto;
	min-height: 0;
	margin-top: 14px;

	:deep(.splitpanes) {
		width: 100%;
		height: 100%;
	}
}

/* 上半区不再整体滚动：四张卡等分高度，各自内部滚动 */
.rcq-cfg {
	height: 100%;
	min-height: 0;
	padding: 2px;
	overflow: auto;
}

.rcq-cfg-grid {
	display: grid;
	/* 网格必须有确定高度，否则 minmax(0,1fr) 的行高会退化成内容高度 */
	height: 100%;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	/* 两行等分上栏高度：每卡约 170px，正好露出 2 行配置项；
	   条目再多也只在卡片 body 内部滚动，不会把整栏顶出滚动条 */
	grid-template-rows: repeat(2, minmax(0, 1fr));
	gap: 12px;
}

/* 有卡被折叠：行高改按内容走，折叠卡只留卡头，腾出的高度留给展开的卡 */
.rcq-cfg-grid.has-folded {
	height: auto;
	grid-template-rows: auto auto;
}

.rcq-bottom {
	height: 100%;
	/* SQL 预览与查询结果左右均分，两张卡都拿到完整高度 */
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	grid-template-rows: minmax(0, 1fr);
	gap: 14px;
	padding: 2px;
	min-height: 0;
}

/* ===================== 卡片与卡头 ===================== */
.rcq-card {
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	height: 100%;
	min-height: 0;
	display: flex;
	flex-direction: column;

	:deep(.el-card__header) {
		flex: 0 0 auto;
		padding: 10px 14px;
		border-bottom: 1px solid var(--el-border-color-lighter);
	}

	:deep(.el-card__body) {
		flex: 1 1 auto;
		min-height: 0;
		overflow: auto;
		padding: 10px 12px 12px;
	}
}

/* 折叠态：只留卡头（条数仍在标题后），高度退回内容高度 */
.rcq-card.is-folded {
	height: auto;
	align-self: start;

	:deep(.el-card__body) {
		display: none;
	}
}

.rcq-head {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

/* 装饰规范：图标深色 + 外围浅色同色系，--tint 切换语义色 */
.rcq-head-ico {
	flex: 0 0 auto;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-primary {
		--tint: var(--el-color-primary);
	}

	&.is-violet {
		--tint: #8b5cf6;
	}

	&.is-amber {
		--tint: #f59e0b;
	}

	&.is-green {
		--tint: #10b981;
	}

	&.is-cyan {
		--tint: #0ea5e9;
	}
}

.rcq-head-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.rcq-head-chip {
	flex: 0 0 auto;
	padding: 0 8px;
	height: 18px;
	line-height: 18px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);

	/* 耗时等中性信息 */
	&.is-plain {
		color: var(--el-text-color-secondary);
		background: var(--el-fill-color);
	}

	/* 被行数上限截断的告警 */
	&.is-warn {
		color: var(--el-color-warning);
		background: color-mix(in srgb, var(--el-color-warning) 14%, transparent);
	}
}

.rcq-head-acts {
	margin-left: auto;
	display: flex;
	align-items: center;
	gap: 6px;
	flex-wrap: wrap;
}

/* ===================== 配置行 ===================== */
.rcq-rows {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

/* 紧凑行：不再给每个字段加上方小标签（改用 placeholder），单行只放控件 */
.rcq-row {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 6px 8px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px solid transparent;
	transition: border-color 0.2s ease;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 26%, transparent);
	}
}

/* 连接关系的字段配对行：缩进 + 虚线，视觉上从属于上面的连接 */
.rcq-row.is-sub {
	background: transparent;
	border: 1px dashed var(--el-border-color-lighter);
	padding: 6px 8px;
	margin-left: 6px;
}

.rcq-field {
	flex: 1 1 0;
	min-width: 0;

	:deep(.el-select),
	:deep(.el-input),
	:deep(.el-date-editor) {
		width: 100%;
	}
}

.rcq-field.is-narrow {
	flex: 0 0 118px;
}

.rcq-field.is-mini {
	flex: 0 0 76px;
}

.rcq-num {
	width: 100%;
}

.rcq-date {
	width: 100% !important;
}

/* 首条条件的 WHERE 占位，和其它行的下拉框等高 */
.rcq-tag {
	flex: 1 1 auto;
	min-width: 0;
	height: 32px;
	line-height: 32px;
	padding: 0 6px;
	border-radius: 6px;
	font-size: 12px;
	text-align: center;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 12%, transparent);
}

.rcq-eq {
	flex: 0 0 auto;
	height: 32px;
	line-height: 32px;
	font-size: 12px;
	font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
	color: var(--el-text-color-secondary);
}

.rcq-join {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding-bottom: 6px;
	border-bottom: 1px dashed var(--el-border-color-lighter);

	&:last-child {
		border-bottom: none;
		padding-bottom: 0;
	}
}

/* 显示列：每张表一段，段内挂「逐列聚合」选择 */
.rcq-sel {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding-bottom: 6px;
	border-bottom: 1px dashed var(--el-border-color-lighter);

	&:last-child {
		border-bottom: none;
		padding-bottom: 0;
	}
}

.rcq-cols {
	display: flex;
	flex-direction: column;
	gap: 4px;
	margin-left: 6px;
}

.rcq-col {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.rcq-col-name {
	flex: 1 1 auto;
	min-width: 0;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	font-size: 12.5px;
	color: var(--el-text-color-regular);
}

.rcq-col-agg {
	flex: 0 0 96px;
	width: 96px;
}

/* 区间：起始值 + 结束值等分一行 */
.rcq-range {
	display: flex;
	align-items: center;
	gap: 6px;

	> * {
		flex: 1 1 0;
		min-width: 0;
	}
}

/* 判空类条件的值占位，保持与其它行的输入框等高 */
.rcq-none {
	flex: 1 1 0;
	min-width: 0;
	font-size: 12px;
	text-align: center;
	color: var(--el-text-color-placeholder);
}

/* 输出选项：去重 / 分页 / 行数上限 */
.rcq-opts {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px 14px;
	margin-top: 8px;
	padding: 8px 10px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px dashed var(--el-border-color-lighter);
}

.rcq-opt {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}

.rcq-opt-k {
	font-size: 12.5px;
	color: var(--el-text-color-secondary);
}

.rcq-num.is-mini {
	width: 108px;
}

/* 聚合提示条 */
.rcq-tip {
	padding: 6px 9px;
	border-radius: 8px;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-color-primary);
	background: color-mix(in srgb, var(--el-color-primary) 8%, transparent);
}

.rcq-blank {
	padding: 14px 0;
	text-align: center;
	font-size: 12.5px;
	color: var(--el-text-color-placeholder);
}

/* 两张下栏卡的空态都在卡片正中间，不做顶部留白 */
.rcq-blank.is-sql {
	flex: 1 1 auto;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 0;
}

.rcq-blank.is-grid {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	padding: 0;
}

/* ===================== SQL 预览 ===================== */
.rcq-sql-card {
	/* 卡体改成 flex 列，空态才能垂直居中、SQL 块才能 flex:1 填满剩余高度 */
	:deep(.el-card__body) {
		display: flex;
		flex-direction: column;
	}
}

.rcq-sql {
	margin: 0;
	flex: 1 1 auto;
	min-height: 0;
	overflow: auto;
	padding: 12px 14px;
	border-radius: 10px;
	background: #1e1e1e;
	color: #d4d4d4;
	font-family: Consolas, Monaco, 'Courier New', monospace;
	font-size: 13px;
	line-height: 1.7;
	white-space: pre-wrap;
	word-break: break-all;

	&::-webkit-scrollbar-track {
		background-color: transparent;
	}
	&::-webkit-scrollbar-thumb {
		background-color: rgba(144, 147, 153, 0.55);
		border-radius: 5px;
	}
}

/* ===================== 查询结果 ===================== */
.rcq-res-card {
	min-height: 0;
	display: flex;
	flex-direction: column;

	/* 卡片本身已是 flex 列，body 直接 flex:1 吃掉剩余高度，
	   不要再叠 height:calc(100% - 62px)，两种约束同时给容易算出 0 高度 */
	:deep(.el-card__body) {
		flex: 1 1 auto;
		min-height: 0;
		padding: 0;
	}
}

.rcq-grid-wrap {
	height: 100%;
	box-sizing: border-box;
	overflow: hidden;
	padding: 6px 8px 8px;

	:deep(.xGrid-style) {
		height: 100%;
	}
}

.rcq-more-ico {
	margin-left: 2px;
	font-size: 12px;
}

/* ===================== 小屏适配 =====================
   分栏布局把上下各压到 50%，窗口一矮（或一窄）四张配置卡就没高度、没法操作。
   isStack 时整个 body 改成竖向堆叠 + 自身滚动，各卡按内容自然撑开。 */
.rcq-body.is-stack {
	overflow: auto;

	/* 拆掉 splitpanes 的分栏：pane 带了内联百分比高度，必须用 !important 覆盖 */
	:deep(.splitpanes) {
		display: block;
		height: auto !important;
	}

	:deep(.splitpanes__pane) {
		height: auto !important;
	}

	:deep(.splitpanes__splitter) {
		display: none;
	}

	.rcq-cfg {
		height: auto;
	}

	.rcq-cfg-grid {
		height: auto;
		grid-template-columns: minmax(0, 1fr);
		grid-template-rows: none;
		grid-auto-rows: auto;
	}

	.rcq-bottom {
		height: auto;
		margin-top: 12px;
		grid-template-columns: minmax(0, 1fr);
		grid-template-rows: none;
		grid-auto-rows: auto;
		overflow: visible;
	}

	/* 配置卡：高度跟内容走，但设上限——小屏条件多时不再把整页撑长，
	   超出部分改成卡内竖向滚动；想彻底收起就用卡头的折叠按钮 */
	.rcq-card {
		height: auto;
		min-height: 140px;
		max-height: 264px;
	}

	.rcq-card.is-folded {
		max-height: none;
	}

	.rcq-card :deep(.el-card__body) {
		overflow: auto;
	}

	.rcq-sql-card {
		height: 250px;
	}

	.rcq-res-card {
		/* 结果表格给固定高度，太矮看不了数据 */
		height: 420px;
	}
}

/* 窄到一行放不下多个控件时，让字段换行铺开而不是被压变形 */
@media (max-width: 900px) {
	.rcq-row {
		flex-wrap: wrap;
	}

	.rcq-field {
		flex: 1 1 160px;
	}

	.rcq-field.is-narrow {
		flex: 0 0 130px;
	}

	.rcq-field.is-mini {
		flex: 0 0 90px;
	}
}
</style>
