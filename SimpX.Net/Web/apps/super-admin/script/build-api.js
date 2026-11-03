import {spawn} from 'child_process';
import { resolve } from 'path';
import { fileURLToPath } from 'url';
import os from 'os';

// 获取当前文件的目录（兼容 ES 模块）
const __filename = fileURLToPath(import.meta.url);
const apiBuildDir = resolve(resolve(__filename, '..'), '..', 'api_build');

const isWin = os.platform() === 'win32';
const moduleName = process.argv.slice(2)?.[0] ?? '';
const shell = isWin ? 'cmd.exe' : '/bin/bash';
const script = isWin ? `build${moduleName}.bat` : `./build${moduleName}.sh`;

// 构建完整命令
const child = spawn(shell, [script], {
    cwd: apiBuildDir,
    stdio: 'pipe', // 捕获输出流
    windowsVerbatimArguments: isWin, // Windows 下正确处理引号和空格
});

// 实时输出 stdout
child.stdout.on('data', (data) => {
    console.log(data.toString().trimEnd());
});

// 实时输出 stderr
child.stderr.on('data', (data) => {
    console.error(data.toString().trimEnd());
});

// 监听进程结束
child.on('close', (code) => {
    if (code === 0) {
        console.log(`✅ ${script} 执行成功\n`);
    } else {
        console.error(new Error(`${script} 执行失败，退出码: ${code}`));
    }
});

// 处理进程异常
child.on('error', (err) => {
    console.error(new Error(`启动进程失败: ${err.message}`));
});