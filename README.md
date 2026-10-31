# deltarune-local-trainer
A local single-player trainer for DELTARUNE

一个面向 Windows Steam 版 DELTARUNE 的本地单人游戏修改器示例。它通过游戏目录中的章节补丁和本地控制文件工作，不连接网络，不读取或上传存档。

## 功能

- 全队锁血与常规伤害无敌
- 当前生命、生命上限、防御修改
- 暗世界金钱修改
- TP 百分比设置与锁定
- 中文桌面控制台
- 安装前自动备份和哈希校验，可恢复原始章节文件

## 使用前须知

本项目是粉丝制作的单人游戏修改工具，与 Toby Fox、DELTARUNE 或 Steam 无关。请仅用于你拥有的游戏副本，并在更新游戏前关闭修改器、恢复原文件或重新制作适配补丁。

项目不包含 DELTARUNE 的原始游戏资源。用户需要自行安装 Steam 正式版。游戏资源及汉化补丁仍受其各自版权和许可约束。

修改器不会主动编辑 `filech*`、`dr.ini` 或 `keyconfig*` 存档文件；但如果玩家在游戏中主动保存，游戏本身可能保存已经修改的数值。使用前请自行备份存档。

## 快速开始

1. 下载 Release 中的 `DELTARUNE修改器.exe` 和对应补丁包。
2. 确认游戏目录为 `D:\Steam\steamapps\common\DELTARUNE`，或按说明修改安装脚本中的路径。
3. 运行安装脚本。脚本会先备份五个章节的 `data.win`，再进行哈希校验。
4. 启动 Steam 游戏并进入章节，再打开修改器。
5. 修改器显示“已连接”后使用功能。

详细说明见 [使用说明.txt](使用说明.txt)。

## 从源码构建

### 修改器界面

`Trainer.cs` 使用 .NET Framework Windows Forms 编译。Windows 上可用 Visual Studio Developer Command Prompt 执行：

```powershell
csc /target:winexe /out:DELTARUNE修改器.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll Trainer.cs
```

### 游戏补丁

补丁由 UndertaleModTool CLI 编译。请从官方仓库获取工具，并对你本机对应版本的五个章节 `data.win` 运行 `patch.csx`。不要把原游戏 `data.win` 或完整游戏资源提交到本仓库。

## 许可证

本项目代码采用 MIT License。DELTARUNE 相关名称、图像、音频和游戏资源不属于本项目许可范围。
