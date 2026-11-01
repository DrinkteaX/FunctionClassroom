# 函数课堂

**制作：雾雨魔理沙**

面向 Windows 10 希沃大屏的触控函数绘图工具。界面为大屏触控设计，
同时支持鼠标和实体键盘。

## 功能

- 最多同时绘制 6 条函数。
- 触控公式键盘，可输入常见高中函数和参数 `a` 至 `k`。
- 平移、缩放、触摸查值和自动适配视野。
- 查看两条函数的交点，或查看函数与 x 轴的交点。
- 二次、指数、对数、三角函数等快捷模板。
- 参数滑块、课堂演示、自动保存、PNG 导出和剪贴板复制。

## 运行环境

- Windows 10 64 位。
- 使用系统自带的 .NET Framework 4.8 和 WPF。
- 不需要 Electron、WebView2、Python、Node.js、NuGet 包或管理员权限。

## 安装

运行 `FunctionClassroom_Setup_v0.1.0.exe`：

1. 点击“开始安装”。
2. 安装完成后从桌面“函数课堂”启动。
3. 卸载入口位于开始菜单或 Windows“应用和功能”。

安装位置：

```text
%LOCALAPPDATA%\Programs\FunctionClassroom
```

绿色版可解压 `FunctionClassroom_v0.1.0_portable.zip` 后直接运行
`FunctionClassroom.exe`。

## 快捷键

- `Ctrl+S`：保存。
- `F11`：进入或退出演示。
- `Esc`：退出演示。

## 已知边界

- 交点只搜索当前视野，结果为数值近似值。
- 首版只支持显函数 `y=f(x)`。

工作区保存在 `%LOCALAPPDATA%\FunctionClassroom\workspace.xml`。

## 开源许可证

本项目采用 [MIT License](LICENSE)，可以自由使用、修改和再发布。

---

制作：**雾雨魔理沙**
