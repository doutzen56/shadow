import '../lang/index';
import { createApp } from 'vue';
import pinia from '/@/stores/index';
import App from '/@/App.vue';
import router from '/@/router';
import { directive } from '/@/directive/index';
import { i18n } from '/@/i18n/index';
import other from '/@/utils/other';
import ElementPlus from 'element-plus';
import '/@/theme/index.scss';
// 动画库
import 'animate.css';
// 栅格布局
import VueGridLayout from 'vue-grid-layout';
// 电子签名
import VueSignaturePad from 'vue-signature-pad';
// 组织架构图
import vue3TreeOrg from 'vue3-tree-org';
import 'vue3-tree-org/lib/vue3-tree-org.css';
// FormCreate 表单设计
import FcDesigner from '@form-create/designer';
// Vxe-Table
import { setupVXETable } from '/@/hooks/setupVXETableHook';
// 自定义字典组件
import sysDict from '/@/components/sysDict/sysDict.vue';
// 打印模板
import '@worm-vue3-print/canvas/style.css';
// AI组件
import '@opentiny/tiny-robot/dist/style.css';

const app = createApp(App);
directive(app);
other.elSvg(app);

// 注册全局字典组件
app.component('GSysDict', sysDict);

app.use(pinia).use(i18n).use(router).use(ElementPlus).use(setupVXETable).use(VueGridLayout).use(FcDesigner.formCreate).use(VueSignaturePad).use(vue3TreeOrg).mount('#app');
