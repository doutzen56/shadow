import LogicFlow from '@logicflow/core';
// 引入自定义节点
import nodeStart from './Nodes/NodeStart';
import nodeEnd from './Nodes/NodeEnd';
import nodeUser from './Nodes/NodeUser';
import nodeGateway from './Nodes/NodeGateway';
import nodeParallel from './Nodes/NodeParallel';
// 注册节点
const Register = (lf: LogicFlow) => {
	lf.register(nodeStart);
	lf.register(nodeEnd);
	lf.register(nodeUser);
	lf.register(nodeGateway);
	lf.register(nodeParallel);
};

export default { Register };
