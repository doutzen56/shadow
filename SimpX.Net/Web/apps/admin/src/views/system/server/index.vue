<template>
	<div class="mc-page" v-loading="loading">
		<!-- 顶部头图：主机身份 + 健康状态 + 实时负载 -->
		<div class="mc-hero">
			<div class="mc-hero-deco">
				<span class="mc-blob mc-blob-1"></span>
				<span class="mc-blob mc-blob-2"></span>
				<span class="mc-blob mc-blob-3"></span>
				<span class="mc-mesh"></span>
				<span class="mc-shine"></span>
			</div>
			<div class="mc-hero-main">
				<div class="mc-hero-left">
					<h2 class="mc-hero-title">{{ deviceInfo.systemInfo.machineName || '服务器' }}</h2>
					<p class="mc-hero-desc">
						{{ deviceInfo.systemInfo.osDescription || '—' }}
						<i class="mc-dot-split"></i>
						{{ deviceInfo.systemInfo.frameworkDescription || '—' }}
						<i class="mc-dot-split"></i>
						已运行 {{ deviceInfo.systemUptime || '—' }}
					</p>
					<div class="mc-hero-status">
						<el-tag class="mc-status-tag" :type="healthLevel" effect="dark" size="small" round>
							<span class="mc-status-dot"></span>
							{{ healthText }}
						</el-tag>
						<span class="mc-hero-meta">数据库：{{ deviceInfo.systemInfo.databaseType || '—' }}</span>
					</div>
				</div>
				<div class="mc-hero-right">
					<div class="mc-gauges">
						<div class="mc-gauge">
							<div class="mc-gauge-head">
								<span class="mc-gauge-k">CPU</span>
								<span class="mc-gauge-v">{{ cpuPercent }}%</span>
							</div>
							<div class="mc-bar"><i :style="{ width: cpuPercent + '%', background: getCpuColor(deviceInfo.cpuInfo.usagePercentage) }"></i></div>
						</div>
						<div class="mc-gauge">
							<div class="mc-gauge-head">
								<span class="mc-gauge-k">内存</span>
								<span class="mc-gauge-v">{{ ramPercent }}%</span>
							</div>
							<div class="mc-bar"><i :style="{ width: ramPercent + '%', background: getRamColor(deviceInfo.ramInfo.usagePercentage) }"></i></div>
						</div>
						<div class="mc-gauge">
							<div class="mc-gauge-head">
								<span class="mc-gauge-k">磁盘峰值</span>
								<span class="mc-gauge-v">{{ maxDiskPercent }}%</span>
							</div>
							<div class="mc-bar"><i :style="{ width: maxDiskPercent + '%', background: getDiskColor(100 - maxDiskPercent) }"></i></div>
						</div>
					</div>
					<el-button class="mc-refresh" size="large" circle :loading="loading" @click="getHardwareInfoData">
						<el-icon><ele-Refresh /></el-icon>
					</el-button>
				</div>
			</div>
		</div>

		<!-- 硬件概览磁贴 -->
		<div class="mc-tiles">
			<div class="mc-tile" style="--c1: #3b82f6; --c2: #22d3ee">
				<div class="mc-tile-icon"><el-icon><ele-Cpu /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">处理器</span>
					<span class="mc-tile-v" :title="deviceInfo.cpuInfo.processorName">{{ deviceInfo.cpuInfo.processorName || '—' }}</span>
					<span class="mc-tile-s">{{ deviceInfo.cpuInfo.physicalCoreCount }} 核 / {{ deviceInfo.cpuInfo.logicalCoreCount }} 线程</span>
				</div>
			</div>
			<div class="mc-tile" style="--c1: #8b5cf6; --c2: #ec4899">
				<div class="mc-tile-icon"><el-icon><ele-Stopwatch /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">内存</span>
					<span class="mc-tile-v">{{ deviceInfo.ramInfo.totalSpace || '—' }}</span>
					<span class="mc-tile-s">已用 {{ deviceInfo.ramInfo.usedSpace || '—' }} · 可用 {{ deviceInfo.ramInfo.freeSpace || '—' }}</span>
				</div>
			</div>
			<div class="mc-tile" style="--c1: #10b981; --c2: #84cc16">
				<div class="mc-tile-icon"><el-icon><ele-Monitor /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">操作系统</span>
					<span class="mc-tile-v" :title="deviceInfo.systemInfo.osDescription">{{ deviceInfo.systemInfo.osDescription || '—' }}</span>
					<span class="mc-tile-s">{{ deviceInfo.systemInfo.osArchitecture || '—' }} 架构</span>
				</div>
			</div>
			<div class="mc-tile" style="--c1: #f59e0b; --c2: #ef4444">
				<div class="mc-tile-icon"><el-icon><ele-Guide /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">运行框架</span>
					<span class="mc-tile-v" :title="deviceInfo.systemInfo.frameworkDescription">{{ deviceInfo.systemInfo.frameworkDescription || '—' }}</span>
					<span class="mc-tile-s">{{ deviceInfo.systemInfo.osVersion || '' }}</span>
				</div>
			</div>
			<div class="mc-tile" style="--c1: #6366f1; --c2: #0ea5e9">
				<div class="mc-tile-icon"><el-icon><ele-Timer /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">运行时间</span>
					<span class="mc-tile-v">{{ deviceInfo.systemUptime || '—' }}</span>
					<span class="mc-tile-s">进程启动 {{ deviceInfo.systemInfo.processStartTime || '—' }}</span>
				</div>
			</div>
			<div class="mc-tile mc-tile-serial" style="--c1: #0ea5e9; --c2: #8b5cf6">
				<div class="mc-tile-icon"><el-icon><ele-Key /></el-icon></div>
				<div class="mc-tile-text">
					<span class="mc-tile-k">
						机器序列号
						<el-tooltip content="序列号的最后一位是校验位，抄写错误可被立即发现" placement="top">
							<el-icon class="mc-serial-tip"><ele-InfoFilled /></el-icon>
						</el-tooltip>
					</span>
					<span class="mc-tile-v mc-serial" :title="deviceInfo.machineSerialKey || '—'">
						<template v-if="serialGroups.length">
							<template v-for="(group, index) in serialGroups" :key="index">
								<em class="mc-serial-g">{{ group }}</em>
								<i v-if="index < serialGroups.length - 1" class="mc-serial-dash">-</i>
							</template>
						</template>
						<template v-else>{{ deviceInfo.machineSerialKey || '—' }}</template>
					</span>
					<span class="mc-tile-s">主板 / 整机 UUID / BIOS / 处理器 指纹摘要</span>
				</div>
				<el-tooltip content="复制机器序列号" placement="top">
					<el-button class="mc-serial-copy" text circle size="small" :disabled="!deviceInfo.machineSerialKey" @click="copySerialKey">
						<el-icon><ele-CopyDocument /></el-icon>
					</el-button>
				</el-tooltip>
			</div>
		</div>

		<div class="mc-body">
			<!-- CPU -->
			<section class="mc-card mc-card-quarter">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #3b82f6; --c2: #22d3ee"><el-icon><ele-Cpu /></el-icon></div>
					<div class="mc-card-titles">
						<h3>CPU 性能</h3>
						<p>当前负载与处理器规格</p>
					</div>
					<span class="mc-card-no">01</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-dial">
						<el-progress type="dashboard" :percentage="deviceInfo.cpuInfo.usagePercentage" :color="getCpuColor(deviceInfo.cpuInfo.usagePercentage)" :width="140" :stroke-width="11">
							<template #default>
								<div class="mc-dial-text">
									<div class="mc-dial-value">{{ cpuPercent }}%</div>
									<div class="mc-dial-label">CPU 使用率</div>
								</div>
							</template>
						</el-progress>
					</div>
					<div class="mc-kv">
						<div class="mc-kv-item">
							<span class="mc-kv-k">处理器架构</span>
							<span class="mc-kv-v">{{ deviceInfo.cpuInfo.processorArchitecture || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">基础频率</span>
							<span class="mc-kv-v">{{ deviceInfo.cpuInfo.baseClockSpeed }} GHz</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">缓存大小</span>
							<span class="mc-kv-v">{{ deviceInfo.cpuInfo.cacheSize || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">核心 / 线程</span>
							<span class="mc-kv-v">{{ deviceInfo.cpuInfo.physicalCoreCount }} / {{ deviceInfo.cpuInfo.logicalCoreCount }}</span>
						</div>
					</div>
				</div>
			</section>

			<!-- 内存 -->
			<section class="mc-card mc-card-quarter">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #8b5cf6; --c2: #ec4899"><el-icon><ele-Stopwatch /></el-icon></div>
					<div class="mc-card-titles">
						<h3>内存使用</h3>
						<p>物理内存占用明细</p>
					</div>
					<span class="mc-card-no">02</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-dial">
						<el-progress type="dashboard" :percentage="deviceInfo.ramInfo.usagePercentage" :color="getRamColor(deviceInfo.ramInfo.usagePercentage)" :width="140" :stroke-width="11">
							<template #default>
								<div class="mc-dial-text">
									<div class="mc-dial-value">{{ ramPercent }}%</div>
									<div class="mc-dial-label">内存使用率</div>
								</div>
							</template>
						</el-progress>
					</div>
					<div class="mc-kv">
						<div class="mc-kv-item">
							<span class="mc-kv-k">总内存</span>
							<span class="mc-kv-v">{{ deviceInfo.ramInfo.totalSpace || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">已使用</span>
							<span class="mc-kv-v">{{ deviceInfo.ramInfo.usedSpace || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">可用</span>
							<span class="mc-kv-v">{{ deviceInfo.ramInfo.freeSpace || '—' }}</span>
						</div>
					</div>
				</div>
			</section>

			<!-- 主板信息 -->
			<section class="mc-card mc-card-quarter">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #14b8a6; --c2: #22c55e"><el-icon><ele-Film /></el-icon></div>
					<div class="mc-card-titles">
						<h3>主板信息</h3>
						<p>硬件厂商与序列号</p>
					</div>
					<span class="mc-card-no">03</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-kv">
						<div class="mc-kv-item">
							<span class="mc-kv-k">制造商</span>
							<span class="mc-kv-v">{{ deviceInfo.boardInfo.manufacturer || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">产品型号</span>
							<span class="mc-kv-v">{{ deviceInfo.boardInfo.product || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">版本</span>
							<span class="mc-kv-v">{{ deviceInfo.boardInfo.version || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">序列号</span>
							<span class="mc-kv-v">{{ deviceInfo.boardInfo.serialNumber || '—' }}</span>
						</div>
					</div>
				</div>
			</section>

			<!-- 系统信息 -->
			<section class="mc-card mc-card-quarter">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #64748b; --c2: #3b82f6"><el-icon><ele-InfoFilled /></el-icon></div>
					<div class="mc-card-titles">
						<h3>系统信息</h3>
						<p>运行时环境与进程</p>
					</div>
					<span class="mc-card-no">04</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-kv mc-kv-dense">
						<div class="mc-kv-item">
							<span class="mc-kv-k">操作系统</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.osDescription || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">系统版本</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.osVersion || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">系统架构</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.osArchitecture || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">运行框架</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.frameworkDescription || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">数据库类型</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.databaseType || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">机器名称</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.machineName || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">当前用户</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.userName || '—' }}</span>
						</div>
						<div class="mc-kv-item">
							<span class="mc-kv-k">系统启动时间</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.systemStartTime || '—' }}</span>
						</div>
						<div class="mc-kv-item mc-kv-full">
							<span class="mc-kv-k">进程启动时间</span>
							<span class="mc-kv-v">{{ deviceInfo.systemInfo.processStartTime || '—' }}</span>
						</div>
					</div>
				</div>
			</section>

			<!-- 磁盘 -->
			<section class="mc-card mc-card-wide">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #10b981; --c2: #84cc16"><el-icon><ele-Files /></el-icon></div>
					<div class="mc-card-titles">
						<h3>磁盘使用情况</h3>
						<p>共 {{ deviceInfo.diskInfos.length }} 个逻辑驱动器</p>
					</div>
					<span class="mc-card-no">05</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-disk-grid">
						<div class="mc-disk" v-for="disk in deviceInfo.diskInfos" :key="disk.diskName">
							<div class="mc-disk-head">
								<span class="mc-disk-name">{{ disk.diskName }}</span>
								<el-tag size="small" effect="plain" round>{{ disk.typeName }}</el-tag>
							</div>
							<div class="mc-disk-main">
								<el-progress type="circle" :percentage="parseFloat(disk.usedPercentage)" :color="getDiskColor(disk.availableRate)" :width="108" :stroke-width="8">
									<template #default>
										<div class="mc-dial-text">
											<div class="mc-dial-value sm">{{ disk.usedPercentage }}%</div>
											<div class="mc-dial-label">已使用</div>
										</div>
									</template>
								</el-progress>
								<div class="mc-kv mc-kv-compact">
									<div class="mc-kv-item">
										<span class="mc-kv-k">总容量</span>
										<span class="mc-kv-v">{{ disk.totalSpace }}</span>
									</div>
									<div class="mc-kv-item">
										<span class="mc-kv-k">已使用</span>
										<span class="mc-kv-v">{{ disk.usedSpace }}</span>
									</div>
									<div class="mc-kv-item">
										<span class="mc-kv-k">可用空间</span>
										<span class="mc-kv-v">{{ disk.freeSpace }}</span>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			</section>

			<!-- 网络适配器（与显卡并排；无显卡数据时独占整行） -->
			<section :class="['mc-card', hasGpuCard ? 'mc-card-half' : 'mc-card-wide']">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #0ea5e9; --c2: #6366f1"><el-icon><ele-Connection /></el-icon></div>
					<div class="mc-card-titles">
						<h3>网络适配器</h3>
						<p>{{ activeNetworks.length }} 个活跃连接 · 收发流量与链路状态</p>
					</div>
					<div class="mc-net-toggle">
						<el-tooltip content="回环接口（127.0.0.1 / ::1）不经过物理链路，速率显示为 —；只看对外流量时可切到「仅物理」" placement="top">
							<el-icon class="mc-net-toggle-tip"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
						<el-switch v-model="showLoopback" size="small" active-text="含回环" inactive-text="仅物理" inline-prompt />
					</div>
					<span class="mc-card-no">06</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-net-grid" v-if="activeNetworks.length > 0">
						<div class="mc-net" v-for="network in activeNetworks" :key="network.name">
							<div class="mc-net-top">
								<span class="mc-net-pulse" :class="network.operationalStatus === 'Up' ? 'is-up' : 'is-down'"></span>
								<div class="mc-net-id">
									<div class="mc-net-name" :title="network.name">{{ network.name }}</div>
									<div class="mc-net-desc" :title="network.description">{{ network.description }}</div>
								</div>
								<el-tag size="small" :type="getNetworkTypeColor(network.type)" effect="light" round>{{ networkTypeName(network.type) }}</el-tag>
							</div>

							<div class="mc-net-chips">
								<span class="mc-net-chip"><el-icon><ele-Odometer /></el-icon>{{ network.speed }}</span>
								<span class="mc-net-chip"><el-icon><ele-Cpu /></el-icon>{{ network.physicalAddress || 'N/A' }}</span>
							</div>

							<div class="mc-net-ips">
								<span class="mc-ip" v-for="ip in network.iPv4Addresses ?? []" :key="ip.address">
									{{ ip.address }}<i>/{{ ip.prefixLength }}</i>
								</span>
								<span class="mc-ip is-empty" v-if="!(network.iPv4Addresses && network.iPv4Addresses.length)">未分配 IPv4 地址</span>
							</div>

							<template v-if="network.statistics">
								<div class="mc-net-flow">
									<div class="mc-flow">
										<span class="mc-flow-ico is-down"><el-icon><ele-Bottom /></el-icon></span>
										<div class="mc-flow-text">
											<span class="mc-flow-k">接收</span>
											<span class="mc-flow-v" :title="formatBytes(network.statistics.bytesReceived)">{{ formatBytes(network.statistics.bytesReceived) }}</span>
										</div>
									</div>
									<div class="mc-flow">
										<span class="mc-flow-ico is-up"><el-icon><ele-Top /></el-icon></span>
										<div class="mc-flow-text">
											<span class="mc-flow-k">发送</span>
											<span class="mc-flow-v" :title="formatBytes(network.statistics.bytesSent)">{{ formatBytes(network.statistics.bytesSent) }}</span>
										</div>
									</div>
								</div>
								<div class="mc-net-bar">
									<i class="is-down" :style="{ width: flowRatio(network.statistics).rx + '%' }"></i>
									<i class="is-up" :style="{ width: flowRatio(network.statistics).tx + '%' }"></i>
								</div>
								<div class="mc-net-packets">
									<div class="mc-pk">
										<span class="mc-pk-k">接收包</span>
										<span class="mc-pk-v">{{ (network.statistics.packetsReceived ?? 0).toLocaleString() }}</span>
									</div>
									<div class="mc-pk">
										<span class="mc-pk-k">发送包</span>
										<span class="mc-pk-v">{{ (network.statistics.packetsSent ?? 0).toLocaleString() }}</span>
									</div>
									<div class="mc-pk">
										<span class="mc-pk-k">丢弃</span>
										<span class="mc-pk-v">{{ ((network.statistics.incomingPacketsDiscarded ?? 0) + (network.statistics.outgoingPacketsDiscarded ?? 0)).toLocaleString() }}</span>
									</div>
									<div class="mc-pk">
										<span class="mc-pk-k">错误</span>
										<span class="mc-pk-v">{{ ((network.statistics.incomingPacketsWithErrors ?? 0) + (network.statistics.outgoingPacketsWithErrors ?? 0)).toLocaleString() }}</span>
									</div>
								</div>
							</template>
						</div>
					</div>
					<div class="mc-net-empty" v-else>
						<el-icon><ele-WarningFilled /></el-icon>
						<span>未检测到活跃的网络适配器</span>
					</div>
				</div>
			</section>

			<!-- GPU -->
			<section class="mc-card mc-card-half mc-card-gpu" v-if="hasGpuCard">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #f43f5e; --c2: #f97316"><el-icon><ele-View /></el-icon></div>
					<div class="mc-card-titles">
						<h3>显卡信息</h3>
						<p>共 {{ deviceInfo.gpuInfos.length }} 个显示适配器</p>
					</div>
					<span class="mc-card-no">07</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-disk-grid">
						<div class="mc-gpu" v-for="(gpu, index) in deviceInfo.gpuInfos" :key="index">
							<div class="mc-disk-head">
								<span class="mc-disk-name" :title="gpu.name">{{ gpu.name }}</span>
								<el-tag size="small" :type="gpu.status === 'OK' ? 'success' : 'danger'" effect="light" round>{{ gpu.status }}</el-tag>
							</div>
							<div class="mc-kv mc-kv-compact">
								<div class="mc-kv-item">
									<span class="mc-kv-k">显存</span>
									<span class="mc-kv-v">{{ gpu.memorySize }}</span>
								</div>
								<div class="mc-kv-item">
									<span class="mc-kv-k">驱动版本</span>
									<span class="mc-kv-v">{{ gpu.driverVersion || 'N/A' }}</span>
								</div>
								<div class="mc-kv-item">
									<span class="mc-kv-k">分辨率</span>
									<span class="mc-kv-v">{{ gpu.videoModeDescription }}</span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</section>

			<!-- NuGet 包 -->
			<section class="mc-card mc-card-wide">
				<header class="mc-card-head">
					<div class="mc-card-icon" style="--c1: #a855f7; --c2: #6366f1"><el-icon><ele-Collection /></el-icon></div>
					<div class="mc-card-titles">
						<h3>NuGet 包信息</h3>
						<p>共 {{ deviceInfo.nugetPackages.length }} 个依赖包</p>
					</div>
					<span class="mc-card-no">08</span>
				</header>
				<div class="mc-card-body">
					<div class="mc-pkgs">
						<div v-for="pkg in deviceInfo.nugetPackages" :key="pkg.packageName" class="mc-pkg">
							<span class="mc-pkg-name">{{ pkg.packageName }}</span>
							<span class="mc-pkg-ver">v{{ pkg.packageVersion }}</span>
						</div>
					</div>
				</div>
			</section>
		</div>
	</div>
</template>

<!-- 服务器信息 -->
<script lang="ts" setup name="sysServer">
import { ref, reactive, onMounted, computed } from 'vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysServerApi } from '/@/api-services/system/api';
import { formatBytes } from '/@/utils/format';
import commonFunction from '/@/utils/commonFunction';

// 网络接口类型定义
interface NetworkInfo {
	name: string;
	description: string;
	type: string;
	operationalStatus: string;
	speed: string;
	physicalAddress: string;
	supportsMulticast: boolean;
	isReceiveOnly: boolean;
	dnsAddresses: string[];
	gatewayAddresses: string[];
	dhcpServerAddresses: string[];
	iPv4Addresses: Array<{
		address: string;
		subnetMask: string;
		prefixLength: number;
	}>;
	iPv6Addresses: Array<{
		address: string;
		subnetMask: string;
		prefixLength: number;
	}>;
	statistics?: {
		bytesReceived: number;
		bytesSent: number;
		packetsReceived: number;
		packetsSent: number;
		incomingPacketsDiscarded: number;
		outgoingPacketsDiscarded: number;
		incomingPacketsWithErrors: number;
		outgoingPacketsWithErrors: number;
	};
}

// 设备信息数据
const deviceInfo = reactive<any>({
	cpuInfo: {
		processorName: '',
		processorArchitecture: '',
		physicalCoreCount: 0,
		logicalCoreCount: 0,
		baseClockSpeed: 0,
		cacheSize: '',
		usagePercentage: 0,
	},
	ramInfo: {
		totalSpace: '',
		usedSpace: '',
		freeSpace: '',
		usagePercentage: 0,
	},
	diskInfos: [],
	networkInfos: [],
	gpuInfos: [],
	boardInfo: {
		manufacturer: '',
		product: '',
		version: '',
		serialNumber: '',
	},
	systemUptime: '',
	machineSerialKey: '',
	systemInfo: {
		osDescription: '',
		osVersion: '',
		osArchitecture: '',
		frameworkDescription: '',
		machineName: '',
		userName: '',
		systemStartTime: '',
		processStartTime: '',
	},
	nugetPackages: [],
});

const loading = ref(false);

// 顶部负载指标（统一成 1 位小数，避免原始数据长尾）
const cpuPercent = computed(() => Number(deviceInfo.cpuInfo.usagePercentage ?? 0).toFixed(1));
const ramPercent = computed(() => Number(deviceInfo.ramInfo.usagePercentage ?? 0).toFixed(1));
const maxDiskPercent = computed(() => {
	const list = deviceInfo.diskInfos ?? [];
	if (list.length === 0) return '0.0';
	return Math.max(...list.map((d: any) => parseFloat(d.usedPercentage) || 0)).toFixed(1);
});

// 健康度：任一指标超过 90% 视为告警
const healthLevel = computed(() => {
	const vals = [parseFloat(cpuPercent.value), parseFloat(ramPercent.value), parseFloat(maxDiskPercent.value)];
	if (vals.some((v) => v >= 90)) return 'danger';
	if (vals.some((v) => v >= 75)) return 'warning';
	return 'success';
});
const healthText = computed(() => ({ success: '运行正常', warning: '负载偏高', danger: '资源告警' }[healthLevel.value] ?? '运行正常'));

// 机器序列号按 5 位一组分段展示（25 位标准商业格式：5 组 × 5 位，末位为校验位）
const serialGroups = computed(() => {
	const raw = (deviceInfo.machineSerialKey || '').replace(/[^0-9A-Za-z]/g, '').toUpperCase();
	return raw.length === 25 ? raw.match(/.{5}/g) ?? [] : [];
});

// 复制机器序列号（授权绑定时需要把它发给供应商）
const { copyText } = commonFunction();
const copySerialKey = () => {
	if (!deviceInfo.machineSerialKey) return;
	copyText(deviceInfo.machineSerialKey);
};

// 获取硬件信息
const getHardwareInfoData = async () => {
	loading.value = true;
	try {
		// 并行获取硬件信息、运行时信息和 NuGet 包信息
		var res1 = await getAPI(SysServerApi).apiSysServerHardwareInfoGet();
		const hardwareData = res1.data.result ?? ({} as any);

		var res2 = await getAPI(SysServerApi).apiSysServerRuntimeInfoGet();
		const runtimeData = res2.data.result ?? ({} as any);

		var res3 = await getAPI(SysServerApi).apiSysServerNuGetPackageInfoGet();
		const nugetPackages = res3.data.result ?? ([] as any);

		// 处理CPU信息
		Object.assign(deviceInfo.cpuInfo, {
			processorName: hardwareData.cpuInfo.processorName,
			processorArchitecture: hardwareData.cpuInfo.processorArchitecture,
			physicalCoreCount: hardwareData.cpuInfo.physicalCoreCount,
			logicalCoreCount: hardwareData.cpuInfo.logicalCoreCount,
			baseClockSpeed: hardwareData.cpuInfo.baseClockSpeed,
			cacheSize: formatBytes(hardwareData.cpuInfo.cacheBytes || 0),
			usagePercentage: hardwareData.cpuInfo.usagePercentage,
		});

		// 处理内存信息
		Object.assign(deviceInfo.ramInfo, {
			totalSpace: formatBytes(hardwareData.ramInfo.totalBytes),
			usedSpace: formatBytes(hardwareData.ramInfo.usedBytes),
			freeSpace: formatBytes(hardwareData.ramInfo.freeBytes),
			usagePercentage: hardwareData.ramInfo.usagePercentage,
		});

		// 处理磁盘信息
		deviceInfo.diskInfos = hardwareData.diskInfos.map((disk: any) => ({
			diskName: disk.diskName,
			typeName: disk.typeName,
			totalSpace: formatBytes(disk.totalSpace),
			usedSpace: formatBytes(disk.usedSpace),
			freeSpace: formatBytes(disk.freeSpace),
			availableRate: disk.availableRate,
			usedPercentage: ((disk.usedSpace / disk.totalSpace) * 100).toFixed(1),
		}));

		// 处理网络信息
		deviceInfo.networkInfos = hardwareData.networkInfos;

		// 处理GPU信息
		deviceInfo.gpuInfos = hardwareData.gpuInfos.map((gpu: any) => ({
			name: gpu.name,
			description: gpu.description,
			vendor: gpu.vendor,
			deviceId: gpu.deviceId,
			busInfo: gpu.busInfo,
			driverVersion: gpu.driverVersion,
			memorySize: gpu.memoryBytes ? formatBytes(gpu.memoryBytes) : 'N/A',
			videoModeDescription: gpu.videoModeDescription,
			status: gpu.status,
		}));

		// 处理主板信息
		Object.assign(deviceInfo.boardInfo, {
			manufacturer: hardwareData.boardInfo.manufacturer,
			product: hardwareData.boardInfo.product,
			version: hardwareData.boardInfo.version,
			serialNumber: hardwareData.boardInfo.serialNumber,
		});

		// 机器序列号（硬件指纹摘要）：接口取不到时保持空串，前端显示为「—」
		deviceInfo.machineSerialKey = hardwareData.machineSerialKey || '';

		// 处理系统运行时间和其他系统信息
		deviceInfo.systemUptime = runtimeData.runningTime || runtimeData.runtimeInfo.systemUptime;

		// 添加系统信息到deviceInfo（可选）
		deviceInfo.systemInfo = {
			osDescription: runtimeData.runtimeInfo.osDescription,
			osVersion: runtimeData.runtimeInfo.osVersion,
			osArchitecture: runtimeData.runtimeInfo.osArchitecture,
			frameworkDescription: runtimeData.runtimeInfo.frameworkDescription,
			databaseType: runtimeData.runtimeInfo.databaseType,
			machineName: runtimeData.runtimeInfo.machineName,
			userName: runtimeData.runtimeInfo.userName,
			systemStartTime: runtimeData.runtimeInfo.systemStartTime,
			processStartTime: runtimeData.runtimeInfo.processStartTime,
		};

		// 处理 NuGet 包信息
		deviceInfo.nugetPackages = nugetPackages.map((pkg: any) => ({
			packageName: pkg.packageName,
			packageVersion: pkg.packageVersion,
		}));
	} catch (error) {
		console.error('获取硬件信息失败:', error);
		// 设置默认值，避免页面崩溃
		deviceInfo.systemUptime = '获取失败';
	} finally {
		loading.value = false;
	}
};

// 是否显示回环接口（默认展示：它是真实存在的接口，排查本机监听的服务时需要看到）
const showLoopback = ref(true);

// 显卡卡片是否渲染：没有可用显卡数据时，网卡独自占满整行
const hasGpuCard = computed(() => deviceInfo.gpuInfos.length > 0 && deviceInfo.gpuInfos[0].name != 'Error');

// 获取活跃网络
const activeNetworks = computed(() =>
	deviceInfo.networkInfos.filter(
		(network: NetworkInfo) =>
			network.operationalStatus === 'Up' &&
			(showLoopback.value || !network.isLoopback) &&
			!network.name.includes('WFP') &&
			!network.name.includes('QoS') &&
			!network.name.includes('Filter') &&
			!network.name.includes('vSwitch')
	)
);

// 网络类型中文名
const networkTypeName = (type: string) => {
	switch (type) {
		case 'Ethernet':
			return '以太网';
		case 'Wireless80211':
			return '无线局域网';
		case 'Loopback':
			return '回环接口';
		case 'Tunnel':
			return '隧道接口';
		case 'Ppp':
			return 'PPP 拨号';
		default:
			return type || '未知类型';
	}
};

// 收发流量占比（用于比例条），无流量时各占一半
const flowRatio = (statistics: any) => {
	const rx = Number(statistics?.bytesReceived ?? 0);
	const tx = Number(statistics?.bytesSent ?? 0);
	const total = rx + tx;
	if (total <= 0) return { rx: 50, tx: 50 };
	return { rx: Number(((rx / total) * 100).toFixed(1)), tx: Number(((tx / total) * 100).toFixed(1)) };
};

// 获取网络类型颜色
const getNetworkTypeColor = (type: string) => {
	switch (type) {
		case 'Ethernet':
			return 'primary';
		case 'Wireless80211':
			return 'success';
		case 'Loopback':
			return 'info';
		default:
			return 'warning';
	}
};

// 获取CPU使用率颜色
const getCpuColor = (percentage: number) => {
	if (percentage < 50) return 'var(--el-color-success)';
	if (percentage < 80) return 'var(--el-color-warning)';
	return 'var(--el-color-error)';
};

// 获取内存使用率颜色
const getRamColor = (percentage: number) => {
	if (percentage < 60) return 'var(--el-color-primary)';
	if (percentage < 80) return 'var(--el-color-warning)';
	return 'var(--el-color-error)';
};

// 获取磁盘使用率颜色
const getDiskColor = (availablePercentage: number) => {
	const usedPercentage = 100 - availablePercentage;
	if (usedPercentage < 70) return 'var(--el-color-primary)';
	if (usedPercentage < 90) return 'var(--el-color-warning)';
	return 'var(--el-color-error)';
};

// 页面初始化
onMounted(async () => {
	await getHardwareInfoData();
});
</script>

<style lang="scss" scoped>
.mc-page {
	position: relative;
	padding-bottom: 4px;
}

/* ===================== 顶部头图 ===================== */
.mc-hero {
	position: relative;
	overflow: hidden;
	border-radius: 14px;
	padding: 26px 30px;
	background: linear-gradient(
		135deg,
		var(--el-color-primary-dark-2) 0%,
		var(--el-color-primary) 48%,
		var(--el-color-primary-light-3) 100%
	);
	color: var(--el-color-white);
	box-shadow: 0 10px 30px -12px var(--el-color-primary-light-5);
	animation: mcRise 0.5s ease both;
}

/* 暗色下主色实底会在深色页面里发亮：主色只取低比例压到页面底色上（旧内核用纯黑叠加兜底） */
[data-theme='dark'] .mc-hero {
	background:
		linear-gradient(135deg, rgb(0 0 0 / 58%) 0%, rgb(0 0 0 / 44%) 100%),
		linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary) 100%);
	background: linear-gradient(
		135deg,
		color-mix(in srgb, var(--el-color-primary) 34%, var(--el-bg-color)) 0%,
		color-mix(in srgb, var(--el-color-primary) 22%, var(--el-bg-color)) 100%
	);
	box-shadow: 0 10px 30px -14px color-mix(in srgb, var(--el-color-primary) 32%, transparent);
}

.mc-hero-deco {
	position: absolute;
	inset: 0;
	pointer-events: none;
}

.mc-blob {
	position: absolute;
	border-radius: 50%;
	background: rgb(255 255 255 / 12%);
}

.mc-blob-1 {
	width: 260px;
	height: 260px;
	right: -60px;
	top: -110px;
	animation: mcFloat 9s ease-in-out infinite;
}

.mc-blob-2 {
	width: 150px;
	height: 150px;
	right: 260px;
	bottom: -80px;
	background: rgb(255 255 255 / 8%);
	animation: mcFloat 11s ease-in-out infinite reverse;
}

.mc-blob-3 {
	width: 90px;
	height: 90px;
	right: 150px;
	top: 24px;
	background: rgb(255 255 255 / 10%);
	animation: mcFloat 7s ease-in-out infinite;
}

.mc-mesh {
	position: absolute;
	inset: 0;
	opacity: 0.16;
	background-image: linear-gradient(rgb(255 255 255 / 60%) 1px, transparent 1px),
		linear-gradient(90deg, rgb(255 255 255 / 60%) 1px, transparent 1px);
	background-size: 26px 26px;
	-webkit-mask-image: linear-gradient(90deg, transparent, #000 55%);
	mask-image: linear-gradient(90deg, transparent, #000 55%);
}

.mc-shine {
	position: absolute;
	top: -30%;
	left: -40%;
	width: 32%;
	height: 160%;
	background: linear-gradient(90deg, transparent, rgb(255 255 255 / 20%), transparent);
	transform: skewX(-18deg);
	animation: mcShine 7s ease-in-out infinite;
	pointer-events: none;
}

.mc-hero-main {
	position: relative;
	z-index: 1;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 24px;
	flex-wrap: wrap;
}

.mc-hero-title {
	margin: 0 0 6px;
	font-size: 24px;
	font-weight: 700;
	letter-spacing: 0.5px;
	line-height: 1.2;
	word-break: break-all;
}

.mc-hero-desc {
	margin: 0;
	max-width: 680px;
	font-size: 13px;
	line-height: 1.8;
	opacity: 0.85;
}

.mc-dot-split {
	display: inline-block;
	width: 3px;
	height: 3px;
	border-radius: 50%;
	background: currentColor;
	opacity: 0.6;
	margin: 0 8px;
	vertical-align: middle;
}

.mc-hero-status {
	display: flex;
	align-items: center;
	gap: 12px;
	margin-top: 12px;
	flex-wrap: wrap;
}

.mc-status-tag {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	border: none;
	font-weight: 600;
}

.mc-status-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: currentColor;
	animation: mcPulse 1.8s ease-in-out infinite;
}

.mc-hero-meta {
	font-size: 12px;
	opacity: 0.8;
}

.mc-hero-right {
	display: flex;
	align-items: center;
	gap: 14px;
}

.mc-gauges {
	display: flex;
	flex-direction: column;
	gap: 10px;
	padding: 12px 14px;
	border-radius: 12px;
	background: rgb(0 0 0 / 14%);
	border: 1px solid rgb(255 255 255 / 16%);
	min-width: 230px;
}

.mc-gauge-head {
	display: flex;
	align-items: baseline;
	justify-content: space-between;
	gap: 10px;
	margin-bottom: 5px;
}

.mc-gauge-k {
	font-size: 12px;
	opacity: 0.8;
}

.mc-gauge-v {
	font-size: 14px;
	font-weight: 700;
}

.mc-bar {
	height: 6px;
	border-radius: 999px;
	background: rgb(255 255 255 / 22%);
	overflow: hidden;

	i {
		display: block;
		height: 100%;
		border-radius: 999px;
		transition: width 0.6s ease;
	}
}

.mc-refresh {
	flex-shrink: 0;
	/* 暗色全局规则用 --next-border-color 顶掉所有按钮的 hover 边框，这里固定在白描边上 */
	--next-border-color: rgb(255 255 255 / 40%);
	background: rgb(255 255 255 / 18%);
	border: 1px solid rgb(255 255 255 / 40%);
	color: var(--el-color-white);
	transition: transform 0.3s ease, background 0.25s ease;

	&:hover {
		background: rgb(255 255 255 / 28%);
		transform: rotate(90deg);
	}
}

/* ===================== 硬件概览磁贴 ===================== */
.mc-tiles {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
	gap: 12px;
	margin-top: 14px;
}

.mc-tile {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 14px 16px;
	border-radius: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition: transform 0.28s ease, box-shadow 0.28s ease, border-color 0.28s ease;
	animation: mcRise 0.5s ease both;

	&:hover {
		transform: translateY(-2px);
		border-color: color-mix(in srgb, var(--c1) 40%, var(--el-border-color-lighter));
		box-shadow: 0 12px 26px -18px var(--c1);
	}
}

.mc-tiles .mc-tile:nth-child(2) {
	animation-delay: 0.05s;
}
.mc-tiles .mc-tile:nth-child(3) {
	animation-delay: 0.1s;
}
.mc-tiles .mc-tile:nth-child(4) {
	animation-delay: 0.15s;
}
.mc-tiles .mc-tile:nth-child(5) {
	animation-delay: 0.2s;
}
.mc-tiles .mc-tile:nth-child(6) {
	animation-delay: 0.25s;
}
.mc-tiles .mc-tile:nth-child(7) {
	animation-delay: 0.3s;
}

.mc-tile-icon {
	width: 38px;
	height: 38px;
	flex-shrink: 0;
	border-radius: 10px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	font-size: 19px;
	/* 装饰规范（2026-10-22 全站统一）：图标深色、外围圆角块浅色同色系，
	 * 不再用"渐变实底 + 白色图标"（实底块视觉重量太大、成片彩色方块很吵）。 */
	color: var(--c1, var(--el-color-primary));
	background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 13%, transparent);
	transition: transform 0.3s ease;

	.mc-tile:hover & {
		transform: scale(1.08);
	}
}

.mc-tile-text {
	display: flex;
	flex-direction: column;
	min-width: 0;
	gap: 2px;
}

.mc-tile-k {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.mc-tile-v {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.mc-tile-s {
	font-size: 11px;
	color: var(--el-text-color-placeholder);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* 机器序列号磁贴：等宽字体 + 分组高亮 + 一键复制 */
.mc-tile-serial {
	grid-column: span 2;
}

.mc-tile-serial .mc-tile-text {
	flex: 1;
}

.mc-serial-tip {
	margin-left: 3px;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	cursor: help;
}

.mc-serial {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 2px;
	font-family: Consolas, Monaco, 'Courier New', monospace;
	font-size: 13px !important;
	font-weight: 600;
	color: var(--c1, var(--el-color-primary));

	// 分段展示时不需要省略号：机器码必须完整可见，否则用户会抄错
	overflow: visible;
	text-overflow: clip;
	white-space: normal;
}

.mc-serial-g {
	padding: 1px 4px;
	border-radius: 4px;
	font-style: normal;
	letter-spacing: 0.5px;
	background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 10%, transparent);
	transition: background 0.2s ease;
}

.mc-tile-serial:hover .mc-serial-g {
	background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 18%, transparent);
}

.mc-serial-dash {
	font-style: normal;
	color: var(--el-text-color-placeholder);
}

.mc-serial-copy {
	flex-shrink: 0;
	color: var(--el-text-color-secondary);

	&:hover {
		color: var(--c1, var(--el-color-primary));
		background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 12%, transparent);
	}
}

/* ===================== 主体卡片 ===================== */
.mc-body {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 16px;
	margin-top: 16px;
}

.mc-card {
	position: relative;
	display: flex;
	flex-direction: column;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 14px;
	padding: 18px 20px 20px;
	overflow: hidden;
	transition: transform 0.28s ease, box-shadow 0.28s ease, border-color 0.28s ease;
	animation: mcRise 0.5s ease both;

	&::before {
		content: '';
		position: absolute;
		inset: 0 0 auto;
		height: 2px;
		background: linear-gradient(90deg, var(--c1, var(--el-color-primary)), var(--c2, var(--el-color-primary)));
		opacity: 0;
		transition: opacity 0.28s ease;
	}

	&:hover {
		transform: translateY(-2px);
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 12px 28px -18px var(--el-color-primary-light-5);

		&::before {
			opacity: 1;
		}
	}
}

.mc-body .mc-card:nth-child(2) {
	animation-delay: 0.05s;
}
.mc-body .mc-card:nth-child(3) {
	animation-delay: 0.1s;
}
.mc-body .mc-card:nth-child(4) {
	animation-delay: 0.15s;
}
.mc-body .mc-card:nth-child(5) {
	animation-delay: 0.2s;
}
.mc-body .mc-card:nth-child(6) {
	animation-delay: 0.25s;
}
.mc-body .mc-card:nth-child(7) {
	animation-delay: 0.3s;
}
.mc-body .mc-card:nth-child(8) {
	animation-delay: 0.35s;
}

.mc-card-wide {
	grid-column: 1 / -1;
}

/* 半行卡：与另一张卡并排展示（显卡 + 网卡） */
.mc-card-half {
	grid-column: span 2;

	/* 半行可用宽度约为整行的一半，网格最小列宽同步收窄，避免退化成单列显得又高又空 */
	.mc-disk-grid {
		grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
	}

	.mc-net-grid {
		grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
	}
}

/* 显卡条目通常比网卡少，并排时垂直居中，避免底部留出大片空白 */
.mc-card-gpu .mc-card-body {
	align-content: center;
}

/* 一行四张的小卡：内容垂直居中，保证四张卡视觉等高 */
.mc-card-quarter {
	.mc-card-body {
		flex: 1;
		align-content: center;
	}

	.mc-kv {
		min-width: 0;
		grid-template-columns: minmax(0, 1fr);
	}
}

/* 字段较多的卡片用双列压缩高度 */
.mc-card-quarter .mc-kv-dense {
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 8px;

	.mc-kv-item {
		padding: 8px 10px;
	}

	.mc-kv-v {
		font-size: 12px;
		line-height: 1.4;
	}
}

@media (max-width: 1500px) {
	.mc-body {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}

	/* 两列布局下 span 2 即整行，内部网格恢复常规列宽 */
	.mc-card-half {
		grid-column: 1 / -1;

		.mc-disk-grid {
			grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
		}

		.mc-net-grid {
			grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
		}
	}
}

@media (max-width: 900px) {
	.mc-body {
		grid-template-columns: minmax(0, 1fr);
	}

	/* 单列布局下 span 2 会撑出隐式列，必须收回 */
	.mc-card-half {
		grid-column: 1 / -1;
	}
}

.mc-card-head {
	display: flex;
	align-items: center;
	gap: 12px;
	padding-bottom: 14px;
	margin-bottom: 16px;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}

.mc-card-icon {
	width: 40px;
	height: 40px;
	border-radius: 11px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	font-size: 20px;
	/* 同 .mc-tile-icon：浅色底 + 深色图标 */
	color: var(--c1, var(--el-color-primary));
	background: color-mix(in srgb, var(--c1, var(--el-color-primary)) 13%, transparent);
	transition: transform 0.3s ease;

	.mc-card:hover & {
		transform: rotate(-8deg) scale(1.06);
	}
}

.mc-card-titles {
	flex: 1;
	min-width: 0;

	h3 {
		margin: 0;
		font-size: 15px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	p {
		margin: 3px 0 0;
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

.mc-card-no {
	margin-left: auto;
	align-self: flex-start;
	font-size: 20px;
	font-weight: 800;
	font-style: italic;
	letter-spacing: -1px;
	color: var(--el-text-color-placeholder);
	opacity: 0.45;
	transition: color 0.28s ease, opacity 0.28s ease;

	.mc-card:hover & {
		color: var(--el-color-primary);
		opacity: 0.75;
	}
}

/* ---------- 仪表盘 ---------- */
.mc-card-body {
	display: flex;
	align-items: center;
	gap: 22px;
	flex-wrap: wrap;
}

.mc-dial {
	flex-shrink: 0;
	display: flex;
	align-items: center;
	justify-content: center;
}

.mc-dial-text {
	text-align: center;
}

.mc-dial-value {
	font-size: 26px;
	font-weight: 700;
	line-height: 1.1;
	color: var(--el-text-color-primary);

	&.sm {
		font-size: 18px;
	}
}

.mc-dial-label {
	margin-top: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

/* ---------- 键值明细 ---------- */
.mc-kv {
	flex: 1;
	min-width: 220px;
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
	gap: 10px;
}

.mc-kv-item {
	display: flex;
	flex-direction: column;
	gap: 3px;
	padding: 10px 12px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px solid transparent;
	transition: border-color 0.25s ease, background 0.25s ease, transform 0.25s ease;

	&:hover {
		transform: translateY(-1px);
		border-color: var(--el-color-primary-light-7);
		background: var(--el-color-primary-light-9);
	}
}

.mc-kv-k {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.mc-kv-v {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	word-break: break-all;
}

.mc-kv-full {
	grid-column: 1 / -1;
}

.mc-kv-compact {
	grid-template-columns: 1fr;
	gap: 8px;
	min-width: 0;
}

/* ---------- 磁盘 / GPU ---------- */
.mc-disk-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
	gap: 14px;
	width: 100%;
}

.mc-disk,
.mc-gpu {
	padding: 14px;
	border-radius: 12px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
	transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;

	&:hover {
		transform: translateY(-2px);
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 10px 22px -16px var(--el-color-primary-light-5);
	}
}

.mc-disk-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	margin-bottom: 12px;
}

.mc-disk-name {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.mc-disk-main {
	display: flex;
	align-items: center;
	gap: 16px;
	flex-wrap: wrap;
}

/* ---------- 网络适配器 ---------- */
.mc-net-toggle {
	margin-left: auto;
	display: flex;
	align-items: center;
	gap: 6px;
}

/* 开关已吃掉剩余空间，序号紧随其后即可，不必再被 auto 推开 */
.mc-net-toggle + .mc-card-no {
	margin-left: 12px;
}

.mc-net-toggle-tip {
	font-size: 13px;
	color: var(--el-text-color-placeholder);
	cursor: help;
}

.mc-net-grid {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
	gap: 14px;
	width: 100%;
}

.mc-net {
	position: relative;
	display: flex;
	flex-direction: column;
	gap: 12px;
	padding: 14px 16px 16px;
	border-radius: 12px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
	overflow: hidden;
	transition: transform 0.28s ease, border-color 0.28s ease, box-shadow 0.28s ease;

	&::before {
		content: '';
		position: absolute;
		inset: 0 auto 0 0;
		width: 3px;
		background: linear-gradient(180deg, var(--el-color-primary), color-mix(in srgb, var(--el-color-primary) 20%, transparent));
		opacity: 0;
		transition: opacity 0.28s ease;
	}

	&:hover {
		transform: translateY(-2px);
		border-color: var(--el-color-primary-light-7);
		box-shadow: 0 14px 28px -20px var(--el-color-primary-light-5);

		&::before {
			opacity: 1;
		}
	}
}

.mc-net-top {
	display: flex;
	align-items: flex-start;
	gap: 10px;
}

.mc-net-pulse {
	flex-shrink: 0;
	width: 9px;
	height: 9px;
	margin-top: 5px;
	border-radius: 50%;
	position: relative;

	&.is-up {
		background: var(--el-color-success);
		box-shadow: 0 0 0 4px var(--el-color-success-light-7);
		animation: mcPulse 2s ease-in-out infinite;
	}

	&.is-down {
		background: var(--el-color-danger);
		box-shadow: 0 0 0 4px var(--el-color-danger-light-7);
	}
}

.mc-net-id {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.mc-net-name {
	font-size: 14px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.mc-net-desc {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.mc-net-chips {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.mc-net-chip {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 3px 9px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	font-family: var(--el-font-family-code, monospace);
}

.mc-net-ips {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
}

.mc-ip {
	display: inline-flex;
	align-items: baseline;
	gap: 1px;
	padding: 3px 9px;
	border-radius: 8px;
	font-size: 12px;
	font-weight: 600;
	font-family: var(--el-font-family-code, monospace);
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	border: 1px solid var(--el-color-primary-light-7);

	i {
		font-style: normal;
		font-size: 11px;
		font-weight: 400;
		opacity: 0.65;
	}

	&.is-empty {
		color: var(--el-text-color-placeholder);
		background: transparent;
		border-style: dashed;
		font-weight: 400;
	}
}

.mc-net-flow {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 10px;
}

.mc-flow {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 9px 12px;
	border-radius: 10px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	transition: transform 0.25s ease, border-color 0.25s ease;

	&:hover {
		transform: translateY(-1px);
	}
}

.mc-flow-ico {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 26px;
	height: 26px;
	border-radius: 8px;
	font-size: 14px;
	color: var(--tint, var(--el-color-primary));
	background: color-mix(in srgb, var(--tint, var(--el-color-primary)) 13%, transparent);

	&.is-down {
		--tint: #22c55e;
	}

	&.is-up {
		--tint: #f59e0b;
	}
}

/* 上文字、下数值：并排卡片变窄时数值不会被标题挤到换行 */
.mc-flow-text {
	display: flex;
	flex-direction: column;
	gap: 1px;
	flex: 1;
	min-width: 0;
}

.mc-flow-k {
	font-size: 11px;
	line-height: 1.2;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}

.mc-flow-v {
	font-size: 14px;
	line-height: 1.25;
	font-weight: 700;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	/* 数值等宽，轮询刷新时不会左右抖动 */
	font-variant-numeric: tabular-nums;
}

.mc-net-bar {
	display: flex;
	height: 7px;
	border-radius: 999px;
	overflow: hidden;
	background: var(--el-border-color-lighter);

	i {
		height: 100%;
		transition: width 0.6s ease;

		&.is-down {
			background: linear-gradient(90deg, #22c55e, #0ea5e9);
		}

		&.is-up {
			background: linear-gradient(90deg, #f59e0b, #f43f5e);
		}
	}
}

.mc-net-packets {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 8px;
	padding-top: 2px;
}

.mc-pk {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 2px;
	padding: 7px 4px;
	border-radius: 8px;
	background: var(--el-bg-color);
	border: 1px dashed var(--el-border-color-lighter);
}

.mc-pk-k {
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

.mc-pk-v {
	font-size: 13px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	font-family: var(--el-font-family-code, monospace);
}

.mc-net-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 8px;
	width: 100%;
	padding: 32px 0;
	font-size: 13px;
	color: var(--el-text-color-placeholder);

	.el-icon {
		font-size: 26px;
	}
}

@media (max-width: 560px) {
	.mc-net-packets {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

/* ---------- NuGet 包 ---------- */
.mc-pkgs {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
	gap: 10px;
	width: 100%;
	max-height: 260px;
	overflow-y: auto;
	padding-right: 4px;
}

.mc-pkg {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	padding: 9px 12px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
	transition: transform 0.25s ease, border-color 0.25s ease, box-shadow 0.25s ease;

	&:hover {
		transform: translateY(-2px);
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 8px 18px -14px var(--el-color-primary);
	}
}

.mc-pkg-name {
	font-size: 12px;
	color: var(--el-text-color-primary);
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.mc-pkg-ver {
	flex-shrink: 0;
	font-size: 11px;
	font-weight: 600;
	color: var(--el-color-primary);
	padding: 1px 7px;
	border-radius: 999px;
	background: var(--el-color-primary-light-9);
}

/* ===================== 动画 ===================== */
@keyframes mcRise {
	from {
		opacity: 0;
		transform: translateY(10px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

@keyframes mcFloat {
	0%,
	100% {
		transform: translate3d(0, 0, 0);
	}
	50% {
		transform: translate3d(0, -14px, 0);
	}
}

@keyframes mcShine {
	0% {
		left: -40%;
	}
	55%,
	100% {
		left: 130%;
	}
}

@keyframes mcPulse {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.4;
	}
}

@media (max-width: 768px) {
	.mc-hero {
		padding: 20px;
	}

	.mc-hero-main {
		flex-direction: column;
		align-items: flex-start;
	}

	.mc-body {
		grid-template-columns: 1fr;
	}
}
/* ===================== 老内核兜底（奇安信可信浏览器 / Chromium 102） =====================
 * 上面凡是可以换算的 color-mix 都已经换成 Element 色阶或 rgb(255 255 255 / X%)（两内核等价）。
 * 剩下这几种底色跟着 --c1 / --tint 动态走，静态色阶映射不出来，只能另想办法：
 * 102 里 color-mix 不是"解析期丢弃声明"，而是属性在计算值阶段失效、回落到初始值
 * （背景 = 透明、边框 = currentColor），表现为图标块只剩一个光秃秃的图标、hover 边框发黑。 */
@supports not (background: color-mix(in srgb, red, blue)) {
	/* 动态浅底统一用伪元素还原：父级建层叠上下文，::before 铺满并 z-index:-1，
	 * 色块压在父背景之上、文字与图标之下，不会盖住内容 */
	.mc-tile-icon,
	.mc-card-icon,
	.mc-serial-g,
	.mc-flow-ico {
		position: relative;
		z-index: 0;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			z-index: -1;
			border-radius: inherit;
			background: var(--c1, var(--el-color-primary));
			opacity: 0.13;
			pointer-events: none;
		}
	}

	.mc-serial-g::before {
		opacity: 0.1;
	}

	.mc-tile-serial:hover .mc-serial-g::before {
		opacity: 0.18;
	}

	.mc-flow-ico::before {
		background: var(--tint, var(--el-color-primary));
	}

	/* 磁贴 hover 的边框混的是 --c1：整条失效会回落到 currentColor（深色发黑），
	 * 退成中性灰保留"浮起"反馈，box-shadow 仍带着 --c1 的色 */
	.mc-tile:hover {
		border-color: var(--el-border-color);
	}

	/* 复制按钮 hover 底色失效 = 完全没有反馈，退回中性浅底 */
	.mc-serial-copy:hover {
		background: var(--el-fill-color-light);
	}

	/* 网卡左侧色带：渐变终点是 20% 透明主色，整条失效色带就出不来 */
	.mc-net::before {
		background: linear-gradient(180deg, var(--el-color-primary), var(--el-color-primary-light-7));
	}
}
</style>
