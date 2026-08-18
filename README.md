<p align="center">
  <img src="src/FocusPace/Assets/FocusPace.ico" width="104" alt="Focus Pace brand icon" />
</p>

<h1 align="center">Focus Pace</h1>

<p align="center"><strong>Focus Pace measures progress, not pressure.</strong></p>

Focus Pace 是一个极简、低打扰的 Windows 专注节奏工具。它用正向累计展示已经完成的专注或休息时间，不使用制造压力的倒计时。代码、可执行文件和配置目录继续使用兼容名称 `FocusPace`。

<p align="center">
  <img src="screenshots/focus%20pace.png" width="460" alt="Focus Pace main window" />
</p>

## v0.1.0 功能

- Focus / Rest 正向累计与自动状态流转
- 半透明、始终置顶且不抢焦点的悬浮进度控件
- Bar、Ring、Fluid 三种 Widget 样式，每种支持动态或静态显示
- Brand、Ocean、Violet、Mint、Amber、Rose、Silver 全局配色
- 可调 Widget 不透明度、置顶状态和开机启动
- Focus 达标后使用当前桌面壁纸进入全屏 Rest 提醒
- Rest 全屏计时、延长 Focus 3 分钟和退出确认
- Ready 检测到鼠标或键盘活动后自动开始 Focus
- Focus 无操作达到 Rest Goal 后自动回到 Ready
- 暂停、恢复、重启以及提前切换 Focus / Rest
- 锁屏、断开会话和休眠时自动暂停
- 关闭主窗口后继续驻留托盘
- 拖动悬浮控件并按显示器保存相对位置
- Windows 深浅色主题跟随、单实例运行、可选开机启动
- 同一次 Windows 启动内的异常退出会话恢复；重启系统后回到 Ready

## 下载

从 [GitHub Releases](https://github.com/yuzhounh/focus-pace/releases) 下载 Windows x64 版本：

- `FocusPace-Setup.exe`：传统安装版，提供开始菜单、可选桌面快捷方式和标准卸载入口。
- `FocusPace.exe`：便携单文件版，无需安装即可运行。

## 运行与开发

需要 Windows 10/11 和 .NET 8 SDK：

```powershell
dotnet build .\FocusPace.sln
dotnet run --project .\src\FocusPace\FocusPace.csproj
```

运行无第三方测试框架的核心状态机测试：

```powershell
dotnet run --project .\tests\FocusPace.Core.Tests\FocusPace.Core.Tests.csproj
```

生成单文件、自包含的 Windows x64 发布包：

```powershell
.\scripts\publish.ps1
```

输出位于 `artifacts/publish/win-x64/`。

安装 Inno Setup 6 后生成传统安装包：

```powershell
.\scripts\build-installer.ps1
```

输出位于 `artifacts/installer/FocusPace-Setup.exe`。

## 数据

FocusPace 不需要账户或网络服务。设置和当前会话保存在：

```text
%LOCALAPPDATA%\FocusPace\settings.json
```

第一版不记录历史统计、任务、项目或标签。
