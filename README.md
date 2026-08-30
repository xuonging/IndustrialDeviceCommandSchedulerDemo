# IndustrialDeviceCommandSchedulerDemo

基于 WPF 的设备指令调度 Demo，演示为什么同一个设备连接不能同时发送多条 Request/Response 命令。

## 实现内容

- `IDeviceCommandScheduler`：统一命令入口。
- `DeviceCommandScheduler`：单设备优先级队列 + 唯一 Worker。
- `IDeviceCommand`：命令抽象，包含优先级、超时、重试、响应匹配、丢弃策略。
- `IDeviceConnection`：设备连接抽象。
- `MockDeviceConnection`：模拟 PLC / 仪表请求响应。
- `EmergencyStopCommand`：最高优先级紧急停止。
- `StartMachineCommand` / `StopMachineCommand`：人工控制命令，不自动重试、不丢弃。
- `ReadTemperatureCommand` / `ReadPressureCommand`：后台采集命令，低优先级、可重试、可合并丢弃。
- `SlowCommand`：超时和重试演示。
- WPF 界面实时显示当前命令、队列长度、成功/失败、丢弃、重试、超时、调度日志。

## 运行

```powershell
dotnet run --project .\IndustrialDeviceCommandSchedulerDemo\IndustrialDeviceCommandSchedulerDemo.csproj
```

## 核心原则

```text
不同设备之间可以并行
同一个设备连接内部必须受控串行
业务层不直接拥有连接，Scheduler 才拥有连接使用权
```

## 调度流程

```text
UI / 后台采集 / 自动流程 / 人工控制
        ↓
IDeviceCommandScheduler.ExecuteAsync
        ↓
PriorityQueue
        ↓
Emergency / ManualControl / Normal / Background
        ↓
Single Worker
        ↓
Send Request
        ↓
Receive Response
        ↓
Response Match
        ↓
DeviceCommandResult
```

## 命令策略

| 命令类型 | 优先级 | 重试 | 丢弃 | 合并 |
| --- | --- | --- | --- | --- |
| 急停 | Emergency | 谨慎允许 | 禁止 | 可按 `ESTOP` 合并 |
| 人工启动/停止 | ManualControl | 默认禁止 | 禁止 | 禁止 |
| 普通业务 | Normal | 按业务决定 | 通常禁止 | 按业务决定 |
| 后台采集 | Background | 允许 | 允许 | 推荐使用 `CoalesceKey` |

软件急停只是软件层面的最高优先级，不能替代安全继电器、安全 PLC、硬件急停回路。
