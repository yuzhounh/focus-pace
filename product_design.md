好的，产品名称就定为 **FocusPace**。我建议把它定义成一个“极简、低打扰、以积极进度感为核心的 Windows 专注节奏工具”，而不是番茄钟或休息提醒器。

下面可以直接作为后续开发时的产品设计蓝图。

# FocusPace

> **Stay focused. Keep your pace.**  
> 在专注与休息之间维持舒适、可持续的节奏。

核心思想不是：

> 距离强制休息还有 12 分钟。

而是：

> **这一轮已经专注 33 分钟，目标 45 分钟。**

所以 FocusPace 的关键词应该是：

**Progress · Pace · Focus · Rest · Lightweight · Ambient**

---

# 一、产品定位

FocusPace 是一个 Windows 常驻桌面工具。

它主要解决三个问题：

1. 工作时容易忘记时间，连续坐太久。
2. 传统休息提醒器只在“该休息了”时出现，平时无法感知工作节奏。
3. 番茄钟往往带有倒计时压力，而且功能越来越复杂。

FocusPace 不要求用户管理：

- Todo
- Project
- Task
- 标签
- 番茄数量
- 日程
- 白噪声
- 工作记录

第一阶段甚至完全不需要账户和云同步。

它只做：

> **专注一段时间 → 达成本轮目标 → 提醒休息 → 休息 → 开始下一轮。**

---

# 二、最重要的设计原则

## 1. Goals, not deadlines

这是整个产品最重要的原则。

45 分钟不是：

> 45 分钟之后必须停下来。

而是：

> **这一轮希望至少专注 45 分钟。**

因此不能使用传统倒计时：

`44:59 → 44:58 → …… → 00:00`

而应该使用累计：

`00:01 → 00:02 → …… → 45:00`

也就是告诉用户：

> **我已经完成了多少。**

而不是：

> **我还欠多少。**

---

# 三、核心桌面悬浮控件

这是 FocusPace 最重要、甚至可以说是最具辨识度的部分。

参考你那个 Tampermonkey `Copy` 按钮的设计语言。

右上角出现一个半透明 pill：

```text
╭────────────────────╮
│  ● Focus    32:18  │
╰────────────────────╯
```

或者第一版可以更加简单：

```text
╭────────────────╮
│ Focus   32:18  │
╰────────────────╯
```

尺寸大概：

- 高度：34–40 px
- 宽度：110–150 px
- 圆角：10–14 px
- 距右边：20–30 px
- 距顶部：20–30 px

整体风格：

- 无窗口标题栏
- 无边框
- 半透明
- Acrylic / Mica / blur 效果
- 柔和阴影
- 始终置顶
- 不抢焦点
- 不影响正常输入
- 默认弱存在感
- Hover 后提高不透明度

也就是说，它应该更像：

> 浏览器页面上的一个轻量 floating widget

而不是：

> Windows 软件窗口。

---

# 四、悬浮按钮同时承担“进度条”功能

这是我很建议做的设计。

不要再单独加一个传统 progress bar。

整个按钮本身就是 progress indicator。

例如目标 45 分钟：

### 刚开始

```text
▏ Focus   03:12
```

### 工作一半

```text
██████░░░░░░  Focus 22:30
```

### 接近目标

```text
███████████░  Focus 41:20
```

但实际上不必真正画这些块。

可以让按钮内部有一层半透明背景，从左到右逐渐填充。

因此用户即使不看数字：

> 扫一眼颜色填充程度，就知道这一轮已经进行多少。

---

# 五、时间展示方式

我建议默认显示：

> **32:18**

表示已经专注 32 分 18 秒。

而不是：

> 12:42 remaining

Hover 时可以出现详细 Tooltip：

> Focused 32:18 of 45 min

这样数字的心理含义始终是：

> **累计成果。**

---

# 六、专注阶段

例如用户设置：

> Focus：45 min

开始后：

```text
Focus · 00:00
```

然后：

```text
Focus · 08:34
Focus · 21:17
Focus · 37:42
Focus · 44:58
```

达到：

```text
✓ Focus · 45:00
```

此时不是：

> Time's up!

而应该：

> **Focus goal reached**

甚至可以使用更柔和的：

> **Nice work. Time for a break.**

---

# 七、达到目标以后不要立即“失败式超时”

这是我觉得 FocusPace 可以明显区别于普通番茄钟的地方。

45 分钟之后，如果用户正在：

- 写一句话
- 编译程序
- 回复消息
- 看一段资料
- 处理一个操作

不应该突然强制进入休息。

达到目标以后继续累计：

```text
✓ Focus · 45:00
✓ Focus · 46:12
✓ Focus · 49:37
```

也就是说：

> **45 分钟以上全部是已经达标后的额外专注时间。**

这会产生非常不同的心理感受。

传统番茄钟：

> 超时了。

FocusPace：

> **目标已经完成，现在自然收尾即可。**

---

# 八、达到专注目标时的提醒

可以出现 Windows 右上角附近的轻量弹窗：

```text
╭─────────────────────────╮
│ ✓ Focus goal reached    │
│                         │
│ 45 min focused          │
│                         │
│ [ Start break ]         │
╰─────────────────────────╯
```

同时播放一个非常轻的提示音。

这里建议：

**Start break** 是主要操作。

而不是强制自动切换。

如果用户暂时不操作：

> Focus 时间继续累计。

甚至不一定需要 `Skip`。

因为：

> 不点击 Start break 本身就是继续工作。

这样界面反而非常简单。

---

# 九、休息阶段也建议采用“正向累计”

这是从这个设计理念自然推出来的。

如果 Focus：

> 32 / 45 min

那么 Rest 也可以：

> 3 / 5 min

而不是：

> Rest in 02:00

例如：

```text
Rest · 00:00
Rest · 01:24
Rest · 03:48
✓ Rest · 05:00
```

这样 FocusPace 整个软件都没有“倒计时”。

这会成为一个非常鲜明的产品特征：

> **FocusPace doesn't count down. It builds up.**

我觉得甚至可以把这写进 README。

---

# 十、休息阶段的颜色

休息阶段应该明显区别于 Focus，但不要强刺激。

例如：

Focus：

> 蓝灰 → 蓝 → 淡紫

Rest：

> 青绿 / 柔和绿色

完成：

> 稍亮的绿色 + ✓

核心还是：

> 状态变化，而不是红黄绿报警系统。

因此我不建议出现红色。

---

# 十一、进度颜色逻辑

可以按照完成度渐变：

### 0–30%

冷灰蓝。

表达：

> 刚开始。

### 30–70%

柔和蓝色。

表达：

> 已经进入状态。

### 70–99%

蓝紫 / 柔和紫。

表达：

> 正在接近目标。

### ≥100%

绿色。

表达：

> ✓ Goal reached

这样用户看到颜色就可以大致理解当前状态。

---

# 十二、休息结束以后

达到休息目标：

```text
✓ Rest · 05:00
```

弹出：

> **Break complete**
>
> Ready for another focus session.

然后进入：

> Ready

状态。

例如悬浮按钮：

```text
╭────────────────╮
│  Ready · Focus │
╰────────────────╯
```

点击后：

> 新一轮 Focus。

---

# 十三、以后可以加入一个很聪明的机制

休息完成以后，不需要要求用户点击。

检测到用户重新开始：

- 键盘输入
- 鼠标移动
- 鼠标点击

就认为：

> 人已经回到电脑前。

于是自动开启下一轮 Focus。

整个过程就是：

**Focus → 达标 → 用户开始休息 → Rest → 达标 → 用户重新使用电脑 → Focus**

这比传统番茄钟自然得多。

---

# 十四、主界面必须保持极简

你前面提出：

> 软件主界面里仅设置工作时长和休息时长。

我赞成保持这个原则。

主界面甚至可以只有：

```text
FocusPace

Focus
[ 45 ] min

Rest
[  5 ] min
```

下面：

```text
Start Focus
```

就这些。

不要第一版就放十几个设置项。

---

# 十五、高级设置不要污染主界面

以后确实会需要一些设置，但可以隐藏在：

> Settings → Advanced

里面。

例如：

- 开机启动
- 提示声音
- 悬浮窗透明度
- 悬浮窗位置
- 是否显示秒
- 空闲检测
- Windows 锁屏自动暂停
- 全屏应用行为
- 通知方式

主界面仍然只显示：

**Focus duration + Rest duration**

---

# 十六、悬浮窗交互

我建议：

### 左键

根据状态执行主要动作。

例如：

Focus 中：

> 打开小型状态菜单。

Ready：

> Start Focus

Goal reached：

> Start Rest

Rest complete：

> Start Focus

---

### 右键

打开 context menu：

```text
Pause
Restart session
Start break
Settings
Hide FocusPace
Exit
```

类似很多 Windows tray 工具。

---

### Hover

控件从：

> 70–80% 可见

提高到：

> 100%

同时可以显示更多信息：

```text
Focused 32:18 / 45:00
```

平时则保持：

```text
Focus 32:18
```

---

# 十七、暂停状态

如果用户需要暂时离开：

```text
Paused · 32:18
```

颜色变成中性灰。

恢复：

```text
Resume
```

暂停期间：

> 时间不累计。

---

# 十八、Windows 锁屏必须自动暂停

这是一个很重要的逻辑。

如果：

> 用户工作 20 分钟 → 锁屏 → 离开 30 分钟

回来以后不能显示：

> Focus 50 min

应该：

> Focus 20 min

Windows：

- Lock
- Sleep
- Hibernate
- User session disconnected

都应该停止 Focus 计时。

---

# 十九、Idle detection

以后建议支持：

例如：

> 5 分钟没有键盘/鼠标操作

FocusPace 自动认为用户离开电脑。

这段时间不计入 Focus。

回来后：

> Resume Focus

不过这是 **v0.2 / v0.3** 再做比较合适。

第一版只处理 Windows Lock/Sleep 就够。

---

# 二十、全屏应用处理

需要考虑：

- PowerPoint 演示
- 视频
- 游戏
- 全屏远程桌面

因为 Always-on-top 按钮可能不应该出现在这些场景。

建议未来支持：

> Hide while another app is fullscreen

默认：

**开启。**

---

# 二十一、系统托盘

FocusPace 应该是典型的：

> 常驻 Tray App。

启动以后：

系统托盘：

> FocusPace 图标

主窗口可以关闭。

但程序继续运行。

Tray 右键：

```text
FocusPace

Focus 32:18

Pause
Start break
Settings
Exit
```

---

# 二十二、关闭主窗口 ≠ 退出软件

点击主窗口右上角：

`×`

建议：

> 隐藏到系统托盘。

只有：

> Tray → Exit

才真正退出。

这符合这种工具的使用方式。

---

# 二十三、悬浮窗位置

默认：

> 屏幕右上角。

例如：

`right: 30px`
`top: 30px`

和你的 Tampermonkey Copy 按钮基本是一套视觉思想。

但位置最好允许用户：

> 直接拖动。

拖动以后保存。

下次启动恢复原位置。

---

# 二十四、多显示器

建议至少从一开始考虑：

- 当前主屏幕
- 记住显示器
- 显示器拔掉后的 fallback
- DPI scaling

否则后面比较容易出现窗口跑到屏幕外。

第一版默认：

> 主显示器右上角。

拖动后记录：

> monitor + relative position。

---

# 二十五、Always on Top

FocusPace 的核心特点之一：

> 永远悬浮在普通 Windows 窗口之上。

但它不应该：

- 获得焦点
- 抢走键盘输入
- 出现在任务栏
- 出现在 Alt+Tab

它应该感觉像：

> Windows 桌面 HUD。

---

# 二十六、视觉风格直接延续你的 Copy 脚本

可以基本确定一套 FocusPace Design Language：

### 圆角

10–14 px。

### 背景

半透明：

> 10–30% opacity

配合 blur。

### 边框

非常淡：

> 1 px

### 阴影

非常轻。

### 字体

Windows 默认优先：

> Segoe UI Variable / Segoe UI

无需额外字体。

### Hover

- 提高透明度
- 增强文字
- 增加阴影
- 轻微向上移动 1 px

和你的 Copy button 完全可以保持一致。

---

# 二十七、Dark / Light Mode

自动跟随 Windows。

Light：

> 白灰半透明。

Dark：

> 深灰半透明。

无需让用户手动设置 Theme。

至少 v0.1 不需要。

---

# 二十八、减少动态效果

支持：

> Windows Reduce Motion

如果系统关闭动画：

FocusPace 也关闭：

- Hover transition
- popup animation
- progress animation

---

# 二十九、时间精度

计时逻辑不要简单依赖：

> 每秒 `timer++`

因为：

- Windows sleep
- 程序阻塞
- 系统 timer drift

可能导致累计误差。

应该记录：

> Session Start Timestamp

然后：

`elapsed = now - start - pausedDuration`

UI 定时刷新只是：

> 展示。

而不是：

> 真正的时间来源。

---

# 三十、Focus session 数据模型

即使 v0.1 不做历史统计，也建议内部设计成 Session。

例如：

```text
Session

type: Focus
target: 45 min
startedAt
pausedDuration
elapsed
completedAt
status
```

Rest 同理。

这样以后扩展统计很容易。

---

# 三十一、第一版可以不保存历史数据

这一点我反而建议克制。

v0.1：

只存：

- Focus 时长
- Rest 时长
- 窗口位置
- 是否开机启动
- 当前 session 必要状态

不需要：

> 过去 365 天 Focus chart。

FocusPace 第一阶段就是一个 utility。

---

# 三十二、异常退出恢复

如果 Windows 重启或者程序崩溃，需要决定：

> 是否恢复上一轮 Focus？

我建议第一版：

如果是程序短暂重启：

> 可以恢复。

如果 Windows 已经 sleep / reboot：

> 不继续累计。

启动后进入：

> Ready

这是最不容易产生错误结果的行为。

---

# 三十三、通知设计

不要使用警报式：

> ⛔ TIME'S UP!

应该：

> ✓ **Focus goal reached**
>
> 45 minutes focused.
>
> Take a break when you're ready.

休息完成：

> ✓ **Break complete**
>
> Ready when you are.

整个语气都应该：

> calm / encouraging / neutral

---

# 三十四、声音设计

Focus goal：

> 很轻的一声 chime。

Rest complete：

> 另一种柔和声音。

不要：

- 闹钟
- 连续响
- 高频 beep
- 强制确认

目标是：

> 提醒，而不是打断。

---

# 三十五、产品 Logo / 图标方向

我不会直接使用：

- 番茄
- 咖啡杯
- 眼睛
- 闹钟

因为这些都会把 FocusPace 限定成某一种产品。

更适合的是：

### 圆环 / 弧线

代表：

> progress + rhythm

例如一个不完整的圆：

```text
◔
```

逐渐形成：

```text
◔ → ◑ → ◕ → ●
```

非常符合 FocusPace。

也可以用：

> FP + progress arc

做 tray icon。

---

# 三十六、产品术语建议统一

我建议以后代码、README、UI 都统一：

### 专注阶段

`Focus`

不要：

`Work`

因为 Focus 同时覆盖：

- 学习
- 阅读
- 写作
- 编程
- 工作

---

### 休息

`Rest`

而不是：

`Break`

因为 Break 稍显强制。

因此整个软件：

> **Focus ↔ Rest**

非常干净。

---

# 三十七、目标时间的术语

不要叫：

> Deadline

甚至尽量少叫：

> Timer duration

可以叫：

> Focus Goal

例如：

```text
Focus Goal
45 min
```

休息：

```text
Rest Goal
5 min
```

这样和产品理念完全一致。

---

# 三十八、核心状态机

FocusPace 实际上只需要几个状态：

```text
READY
  ↓
FOCUS
  ↓
FOCUS_GOAL_REACHED
  ↓
REST
  ↓
REST_GOAL_REACHED
  ↓
READY / FOCUS
```

加一个：

```text
PAUSED
```

就够了。

这意味着整个程序逻辑其实很干净。

---

# 三十九、v0.1.0 我建议严格控制范围

第一版只做：

### 核心

- Focus 时长设置
- Rest 时长设置
- Focus 正向计时
- Rest 正向计时
- 进度计算
- Focus goal reached
- Rest goal reached

### Floating widget

- Always on top
- 半透明
- 进度填充
- Dark / Light
- Hover
- 拖动
- 保存位置

### Windows

- Tray
- Lock 暂停
- Sleep 暂停
- 开机启动
- 单实例运行

### 基础控制

- Start
- Pause
- Resume
- Start Rest
- Restart
- Exit

这已经足够成为：

> **FocusPace v0.1.0**

---

# 四十、v0.2.0 可以加入

- Idle detection
- Fullscreen detection
- 自定义提示音
- 显示秒开关
- Widget opacity
- 自动开始下一轮
- 延迟休息
- 多显示器优化

---

# 四十一、v0.3.0 再考虑统计

例如：

```text
Today

Focus
3 h 42 min

Sessions
5

Rest
28 min
```

甚至可以加入：

> 今日 Focus progress ring

但我不建议一开始就做。

因为它很容易让 FocusPace 从：

> calm utility

变成：

> productivity tracker。

---

# 四十二、不要做的东西

至少早期明确 **不做**：

- Todo
- 项目管理
- 日程管理
- 番茄任务
- 云同步
- 登录
- 社交
- AI
- 游戏化等级
- 连续签到
- 排行榜

这些都会破坏 FocusPace 原本最有价值的地方：

> **它几乎没有存在感。**

---

# 四十三、Windows 技术路线

如果明确主要面向 Windows，我仍然建议：

> **C# + .NET + WPF**

或者使用较新的 Windows UI 技术实现视觉层。

核心要求不是技术“新”，而是：

- 常驻稳定
- 内存低
- Always on Top 稳定
- Tray 成熟
- DPI 支持好
- 多显示器稳定
- 系统 Session Events 好处理
- 安装简单

对于这种 utility：

> 原生 Windows 技术明显比 Electron 更合适。

---

# 四十四、项目结构可以提前保持简单

例如：

```text
focuspace/
│
├─ src/
│  └─ FocusPace/
│
├─ assets/
│  ├─ icons/
│  └─ screenshots/
│
├─ docs/
│
├─ LICENSE
└─ README.md
```

程序集名称：

`FocusPace`

exe：

`FocusPace.exe`

GitHub：

`focuspace`

---

# 四十五、README 定位

我建议以后 README 开头不要写：

> A Pomodoro timer.

而是：

> **FocusPace is a lightweight Windows app that helps you maintain a healthy focus-and-rest rhythm.**

然后突出最核心的设计：

> Instead of counting down to a deadline, FocusPace shows how much focused time you've already built up toward your current goal.

这句话已经基本解释了产品区别。

---

# 四十六、最核心的一句话

我认为整个 FocusPace 最值得坚持的是：

> **FocusPace measures progress, not pressure.**

中文可以理解为：

> **记录进展，而不是制造压力。**

或者产品设计原则：

> **Build up, don't count down.**

这句话更加直接。

---

# 四十七、最终产品形态

我脑中最终的 FocusPace 应该是这样的：

电脑启动以后，它安静地待在右上角：

```text
╭──────────────────╮
│  Focus    27:42  │
╰──────────────────╯
```

背景大概已经填满 60%。

你继续工作。

变成：

```text
╭──────────────────╮
│  Focus    41:23  │
╰──────────────────╯
```

然后：

```text
╭──────────────────╮
│ ✓ Focus   45:00  │
╰──────────────────╯
```

轻轻响一下：

> **Focus goal reached.**

你把手头这一段做完：

```text
✓ Focus 48:37
```

点击：

> Start Rest

然后：

```text
Rest 02:18
```

五分钟后：

```text
✓ Rest 05:00
```

回来继续下一轮。

整个过程中，它没有催你，没有追着你跑，也没有要求你维护一套生产力系统。

**它只是安静地告诉你：今天这一段专注已经走了多远。**

这就是我认为 **FocusPace 最应该坚持的产品核心**。

