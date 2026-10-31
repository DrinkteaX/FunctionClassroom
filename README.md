# 函数课堂

面向 Windows 10 希沃大屏的触控函数绘图工具。软件只使用 Windows 自带的
.NET Framework/WPF，不需要 Electron、WebView2、Python、Node.js 或第三方 DLL。

目标系统为 Windows 10 64 位，使用系统内置的 .NET Framework 4.8。

## 已实现

- 同一坐标系同时绘制最多 6 条显函数。
- 触控公式键盘，也可使用实体键盘输入。
- 平移、缩放、复位、自动适配视野和触摸查值。
- 参数 `a` 至 `k` 自动生成滑块。
- 指定两条函数求交点，或求函数与 x 轴的交点。
- 常见高中函数模板与简短性质说明。
- 课堂演示模式、自动保存、PNG 导出和复制到剪贴板。

## 支持输入

```text
x^2 - 4
2x + 1
sqrt(x + 3)
abs(x - 2)
2^x
sin(2x)
log(x, 10)
a*(x-h)^2+k
```

函数名包括 `sin`、`cos`、`tan`、`asin`、`acos`、`atan`、`sqrt`、`abs`、
`ln`、`log`、`log10`、`exp`。

## 构建

在 Windows PowerShell 中运行：

```powershell
.\build.ps1
```

构建脚本会先用 Windows 自带 C# 编译器运行数学测试和触控坐标测试，再生成：

```text
dist\FunctionClassroom.exe
dist\FunctionClassroom.exe.config
dist\README.md
```

`dist` 目录可以整体复制到希沃电脑，直接运行 `FunctionClassroom.exe`。
不需要联网，不需要管理员权限，也不会安装系统服务。

## 快捷键

- `Ctrl+S`：保存当前工作区。
- `F11`：进入或退出课堂演示。
- `Esc`：退出演示模式。

## 数据

最近的工作区自动保存在：

```text
%LOCALAPPDATA%\FunctionClassroom\workspace.xml
```

如果该文件损坏，软件会忽略损坏内容并回到默认工作区。

## 已知边界

- 交点只搜索当前视野，结果是数值近似值。
- 首版只支持显函数 `y=f(x)`，不支持完整分段函数、隐函数和符号推导。
- 当前构建已完成 Windows 桌面自动化验证；部署到具体希沃机型前仍需做一次真机触控验收。
