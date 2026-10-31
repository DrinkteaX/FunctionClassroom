# 函数课堂 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个只依赖 Windows 10 自带运行库的触控函数绘图应用。

**Architecture:** 使用无 XAML 的 WPF 单窗口界面，把表达式、数学求值、交点搜索、绘图和持久化拆成独立类。通过 Windows 自带的 `csc.exe` 直接编译，避免依赖 .NET SDK 和 NuGet。

**Tech Stack:** C# 5、WPF、.NET Framework 4.x、PowerShell 构建脚本。

**Spec:** `docs/superpowers/specs/2026-10-31-function-classroom-design.md`

## Global Constraints

- Windows 10 64 位。
- 不使用 NuGet、Electron、WebView2、Python、Node.js。
- 最多同时显示 6 条函数。
- 交点只搜索当前视窗，返回近似值。
- 主操作按钮适合手指触控。
- 构建结果可在目标机器上直接运行。

---

### Task 1: 表达式核心

**Files:**
- Create: `tests/MathCoreTests.cs`
- Create: `src/FunctionClassroom/ExpressionParser.cs`
- Create: `src/FunctionClassroom/FunctionExpression.cs`

**Interfaces:**
- Produces: `ExpressionParser.Parse(string) -> FunctionExpression`
- Produces: `FunctionExpression.Evaluate(double x, Func<string,double> parameterLookup) -> double`

- [ ] **Step 1: Write failing parser tests**

```csharp
AssertNear(4, Eval("2+2", 0));
AssertNear(9, Eval("x^2", 3));
AssertNear(5, Eval("x+1", 4));
AssertNear(1, Eval("sin(pi/2)", 0));
```

- [ ] **Step 2: Run the test runner and verify a missing parser compile failure.**

- [ ] **Step 3: Implement tokenization, recursive-descent precedence, unary operators, functions and parse errors.**

- [ ] **Step 4: Run all parser tests until they pass.**

- [ ] **Step 5: Commit the expression core.**

### Task 2: 曲线采样与断线

**Files:**
- Create: `src/FunctionClassroom/PlotSampler.cs`
- Modify: `tests/MathCoreTests.cs`

**Interfaces:**
- Produces: `PlotSampler.Sample(FunctionExpression expression, double xMin, double xMax, int width, double parameter) -> List<PolylineSegmentData>`

- [ ] **Step 1: Add a failing `1/x` sampling test that asserts no segment crosses x=0.**
- [ ] **Step 2: Add a failing `sqrt(x)` test that asserts no samples exist for x<0.**
- [ ] **Step 3: Implement per-pixel sampling, non-finite filtering and discontinuity splitting.**
- [ ] **Step 4: Run math tests until green.**
- [ ] **Step 5: Commit the sampler.**

### Task 3: 交点搜索

**Files:**
- Create: `src/FunctionClassroom/IntersectionFinder.cs`
- Modify: `tests/MathCoreTests.cs`

**Interfaces:**
- Produces: `IntersectionFinder.Find(Func<double,double> first, Func<double,double> second, double xMin, double xMax, int sampleCount) -> List<PointData>`

- [ ] **Step 1: Add failing tests for line/parabola crossings and `x^2` tangency at zero.**
- [ ] **Step 2: Add failing tests for no intersection and duplicate suppression.**
- [ ] **Step 3: Implement sign-change bisection, local-minimum tangency refinement and result limits.**
- [ ] **Step 4: Run all math tests until green.**
- [ ] **Step 5: Commit the intersection finder.**

### Task 4: 绘图控件与触控

**Files:**
- Create: `src/FunctionClassroom/GraphView.cs`
- Create: `src/FunctionClassroom/PlotModel.cs`

**Interfaces:**
- Consumes: `PlotSampler` and `FunctionExpression`.
- Produces: `GraphView.Functions`, `GraphView.SetViewport`, `GraphView.FindIntersections`.

- [ ] **Step 1: Build a custom `FrameworkElement` that draws grid, axes, curves and empty-state text.**
- [ ] **Step 2: Add DPI-aware coordinate transforms.**
- [ ] **Step 3: Add touch/mouse drag pan, touch pinch scale and toolbar-compatible zoom methods.**
- [ ] **Step 4: Add coordinate tracing and intersection marker hit testing.**
- [ ] **Step 5: Manually launch and verify pan, zoom and redraw at 1920×1080.**

### Task 5: 单窗口课堂界面

**Files:**
- Create: `src/FunctionClassroom/MainWindow.cs`
- Create: `src/FunctionClassroom/Program.cs`

**Interfaces:**
- Consumes: `GraphView`.
- Produces: executable entry point and the complete touch-first classroom UI.

- [ ] **Step 1: Build window shell with function list, graph, toolbar and collapsible formula keyboard.**
- [ ] **Step 2: Wire add/delete/show/edit function actions and automatic parameter sliders.**
- [ ] **Step 3: Add intersection selection, x-axis mode and marker list.**
- [ ] **Step 4: Add presentation mode and large-touch styling.**
- [ ] **Step 5: Manually launch and exercise the complete classroom flow.**

### Task 6: 模板、保存和导出

**Files:**
- Create: `src/FunctionClassroom/TemplateLibrary.cs`
- Create: `src/FunctionClassroom/ProjectStore.cs`
- Create: `src/FunctionClassroom/ImageExporter.cs`

**Interfaces:**
- Produces: template definitions, XML workspace persistence, PNG and clipboard export.

- [ ] **Step 1: Add five function templates with expressions and concise properties.**
- [ ] **Step 2: Add XML serialization and safe fallback on corrupt input.**
- [ ] **Step 3: Add PNG export and clipboard image copy.**
- [ ] **Step 4: Manually verify save/reload/export.**
- [ ] **Step 5: Commit the completed application.**

### Task 7: 构建和交付

**Files:**
- Create: `build.ps1`
- Create: `README.md`
- Create: `.gitignore`

**Interfaces:**
- Produces: `dist/FunctionClassroom.exe`

- [ ] **Step 1: Compile with the Windows framework compiler and WPF runtime assemblies.**
- [ ] **Step 2: Fix all compile errors and warnings.**
- [ ] **Step 3: Run the math regression test executable.**
- [ ] **Step 4: Launch the application and inspect the initial window.**
- [ ] **Step 5: Package `dist` and document deployment.**

## Self-Review

- Every requirement in the spec maps to one of Tasks 1–7.
- No placeholder API names are referenced outside their defining task.
- The executable remains framework-dependent only, matching the Windows 10 built-in runtime constraint.
