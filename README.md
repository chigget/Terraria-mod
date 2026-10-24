# MythosOfOlympus

以古希腊神话为主题的 Terraria 内容 Mod，当前为第一个小版本的开发原型。

## 环境

- Terraria / tModLoader 1.4.4.9
- C#、.NET 8
- 将整个仓库放在 tModLoader 的 `ModSources/MythosOfOlympus` 目录中。
- 在游戏的 Mod Sources 页面执行 **Build + Reload**。项目引用上级目录中由 tModLoader 提供的 `tModLoader.targets`，不能作为普通独立 .NET 项目直接运行。

## 当前内容

- 史莱姆王掉落迷宫钥匙，消耗一把可永久解锁当前世界的迷宫。
- 创建世界时自动尝试生成地下迷宫，包含阶梯分支、宝箱、罐子和压力板飞镖陷阱。
- Minotaur 守卫：靠近唤醒、冲刺、连续锤击、跳跃追击，以及低于 40% 生命后的狂暴。
- 战斗期间封闭大厅入口，脱战或击败后解除。
- 必掉米诺陶洛斯战斧，每次挥动有 25% 几率朝鼠标方向发出冲击波。
- 中英文文本。美术目前使用原版占位贴图和简单绘制效果。

## 单人测试

聊天栏命令：

- `/labyrinth locate`：查看入口位置与状态。
- `/labyrinth tp`：传送到迷宫门外。
- `/labyrinth key`：领取测试钥匙。
- `/labyrinth generate`：为尚无迷宫的世界尝试补生成。

已有迷宫不会自动更新布局。测试召唤物和临时配置仅用于开发，发布前需要整理。

完整测试说明见 [docs/LabyrinthTesting.md](docs/LabyrinthTesting.md)。客户端手感和双客户端联机仍需实际游玩验证。
