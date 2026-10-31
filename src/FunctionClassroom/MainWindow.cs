using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FunctionClassroom
{
    public sealed class MainWindow : Window
    {
        private const int MaximumFunctions = 6;
        private static readonly string[] FunctionColors =
        {
            "#1565C0", "#E76F00", "#14804A", "#C62828", "#6A3DB8", "#00838F"
        };

        private readonly List<FunctionPlotItem> _functions = new List<FunctionPlotItem>();
        private readonly DispatcherTimer _intersectionTimer;
        private readonly DispatcherTimer _saveTimer;
        private Grid _root;
        private Grid _contentGrid;
        private ColumnDefinition _leftColumn;
        private ColumnDefinition _splitterColumn;
        private Border _topBar;
        private Border _leftPanel;
        private Border _keypadPanel;
        private Grid _graphHost;
        private GraphView _graphView;
        private StackPanel _functionListPanel;
        private StackPanel _templateListPanel;
        private TextBox _expressionBox;
        private TextBlock _editorTitle;
        private TextBlock _statusText;
        private TextBlock _intersectionStatus;
        private ComboBox _firstFunctionCombo;
        private ComboBox _secondFunctionCombo;
        private Button _presentationExitButton;
        private TabControl _leftTabs;
        private int _nextFunctionId = 1;
        private int _activeFunctionId = -1;
        private bool _intersectionActive;
        private bool _intersectionWithXAxis;
        private int _intersectionA = -1;
        private int _intersectionB = -1;
        private bool _freshWorkspace;
        private bool _presentationMode;
        private WindowStyle _savedWindowStyle;
        private ResizeMode _savedResizeMode;
        private WindowState _savedWindowState;

        public MainWindow()
        {
            Title = "函数课堂";
            Width = 1500.0;
            Height = 900.0;
            MinWidth = 1100.0;
            MinHeight = 700.0;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;
            Background = UiFactory.PageBackground;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            Foreground = UiFactory.TextBrush;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            _intersectionTimer = new DispatcherTimer();
            _intersectionTimer.Interval = TimeSpan.FromMilliseconds(240.0);
            _intersectionTimer.Tick += delegate
            {
                _intersectionTimer.Stop();
                RefreshIntersections();
            };

            _saveTimer = new DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(900.0);
            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                SaveWorkspace(false);
            };

            BuildInterface();
            LoadWorkspace();

            Loaded += delegate
            {
                if (_freshWorkspace)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _graphView.FitVisibleFunctions();
                        ShowEditor(_activeFunctionId);
                    }), DispatcherPriority.Loaded);
                }
            };
            Closing += OnWindowClosing;
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private void BuildInterface()
        {
            _root = new Grid();
            _root.Background = UiFactory.PageBackground;
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = _root;

            _topBar = BuildTopBar();
            Grid.SetRow(_topBar, 0);
            _root.Children.Add(_topBar);

            _contentGrid = BuildContent();
            Grid.SetRow(_contentGrid, 1);
            _root.Children.Add(_contentGrid);

            _keypadPanel = BuildKeypad();
            Grid.SetRow(_keypadPanel, 2);
            _root.Children.Add(_keypadPanel);
        }

        private Border BuildTopBar()
        {
            Grid layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285.0) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            StackPanel titlePanel = new StackPanel();
            titlePanel.Margin = new Thickness(22.0, 10.0, 10.0, 8.0);
            TextBlock title = UiFactory.Text("函数课堂", 28.0, UiFactory.TextBrush);
            title.FontWeight = FontWeights.Bold;
            titlePanel.Children.Add(title);
            _statusText = UiFactory.Text("触控绘图 · Windows 10", 13.0, UiFactory.MutedTextBrush);
            titlePanel.Children.Add(_statusText);
            TextBlock credit = UiFactory.Text("制作：雾雨魔理沙", 12.0, UiFactory.MutedTextBrush);
            credit.Margin = new Thickness(0.0, 1.0, 0.0, 0.0);
            titlePanel.Children.Add(credit);
            layout.Children.Add(titlePanel);

            WrapPanel actions = new WrapPanel();
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.VerticalAlignment = VerticalAlignment.Center;
            actions.Margin = new Thickness(4.0, 7.0, 12.0, 7.0);
            actions.Children.Add(UiFactory.Button("新增函数", AddFunction, true));
            actions.Children.Add(UiFactory.Button("适配视野", delegate { _graphView.FitVisibleFunctions(); }, false));
            actions.Children.Add(UiFactory.Button("演示", EnterPresentationMode, false));
            actions.Children.Add(UiFactory.Button("保存", delegate { SaveWorkspace(true); }, false));
            actions.Children.Add(UiFactory.Button("导出 PNG", ExportPng, false));
            actions.Children.Add(UiFactory.Button("复制图像", CopyGraphImage, false));
            Grid.SetColumn(actions, 1);
            layout.Children.Add(actions);

            Border border = new Border();
            border.Background = UiFactory.PanelBackground;
            border.BorderBrush = UiFactory.BorderBrush;
            border.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
            border.Child = layout;
            return border;
        }

        private Grid BuildContent()
        {
            Grid grid = new Grid();
            _leftColumn = new ColumnDefinition { Width = new GridLength(405.0) };
            _splitterColumn = new ColumnDefinition { Width = new GridLength(7.0) };
            grid.ColumnDefinitions.Add(_leftColumn);
            grid.ColumnDefinitions.Add(_splitterColumn);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            _leftPanel = BuildLeftPanel();
            Grid.SetColumn(_leftPanel, 0);
            grid.Children.Add(_leftPanel);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 7.0;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.Background = UiFactory.PageBackground;
            splitter.ShowsPreview = false;
            Grid.SetColumn(splitter, 1);
            grid.Children.Add(splitter);

            _graphHost = BuildGraphHost();
            Grid.SetColumn(_graphHost, 2);
            grid.Children.Add(_graphHost);
            return grid;
        }

        private Border BuildLeftPanel()
        {
            _leftTabs = new TabControl();
            _leftTabs.FontSize = 18.0;
            _leftTabs.Padding = new Thickness(0.0);
            _leftTabs.Background = UiFactory.PanelBackground;
            _leftTabs.BorderThickness = new Thickness(0.0);

            TabItem functionsTab = new TabItem();
            functionsTab.Header = "函数";
            functionsTab.Padding = new Thickness(24.0, 10.0, 24.0, 10.0);
            functionsTab.Content = BuildFunctionsTab();
            _leftTabs.Items.Add(functionsTab);

            TabItem templatesTab = new TabItem();
            templatesTab.Header = "公式模板";
            templatesTab.Padding = new Thickness(20.0, 10.0, 20.0, 10.0);
            templatesTab.Content = BuildTemplatesTab();
            _leftTabs.Items.Add(templatesTab);

            Border border = new Border();
            border.Background = UiFactory.PanelBackground;
            border.BorderBrush = UiFactory.BorderBrush;
            border.BorderThickness = new Thickness(0.0, 0.0, 1.0, 0.0);
            border.Child = _leftTabs;
            return border;
        }

        private UIElement BuildFunctionsTab()
        {
            DockPanel dock = new DockPanel();
            dock.LastChildFill = true;

            Grid heading = new Grid();
            heading.Margin = new Thickness(14.0, 12.0, 14.0, 8.0);
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock headingText = UiFactory.Text("函数列表", 20.0, UiFactory.TextBrush);
            headingText.FontWeight = FontWeights.SemiBold;
            heading.Children.Add(headingText);
            Button addButton = UiFactory.Button("＋ 新增", AddFunction, true);
            addButton.MinWidth = 92.0;
            Grid.SetColumn(addButton, 1);
            heading.Children.Add(addButton);
            DockPanel.SetDock(heading, Dock.Top);
            dock.Children.Add(heading);

            Border intersectionPanel = BuildIntersectionPanel();
            DockPanel.SetDock(intersectionPanel, Dock.Bottom);
            dock.Children.Add(intersectionPanel);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll.Padding = new Thickness(12.0, 0.0, 12.0, 4.0);
            _functionListPanel = new StackPanel();
            scroll.Content = _functionListPanel;
            dock.Children.Add(scroll);
            return dock;
        }

        private Border BuildIntersectionPanel()
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(14.0, 8.0, 14.0, 14.0);
            TextBlock heading = UiFactory.Text("交点分析", 19.0, UiFactory.TextBrush);
            heading.FontWeight = FontWeights.SemiBold;
            panel.Children.Add(heading);

            Grid choices = new Grid();
            choices.Margin = new Thickness(0.0, 8.0, 0.0, 4.0);
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            _firstFunctionCombo = CreateFunctionCombo();
            _secondFunctionCombo = CreateFunctionCombo();
            choices.Children.Add(_firstFunctionCombo);
            TextBlock versus = UiFactory.Text("与", 17.0, UiFactory.MutedTextBrush);
            versus.Margin = new Thickness(8.0, 8.0, 8.0, 8.0);
            Grid.SetColumn(versus, 1);
            choices.Children.Add(versus);
            Grid.SetColumn(_secondFunctionCombo, 2);
            choices.Children.Add(_secondFunctionCombo);
            panel.Children.Add(choices);

            WrapPanel buttons = new WrapPanel();
            buttons.Children.Add(UiFactory.Button("查找两线交点", delegate { FindIntersections(false); }, false));
            buttons.Children.Add(UiFactory.Button("与 x 轴交点", delegate { FindIntersections(true); }, false));
            panel.Children.Add(buttons);

            _intersectionStatus = UiFactory.Text("选择两条可见函数后查找。", 14.0, UiFactory.MutedTextBrush);
            _intersectionStatus.Margin = new Thickness(2.0, 3.0, 2.0, 0.0);
            panel.Children.Add(_intersectionStatus);

            Border border = new Border();
            border.Background = UiFactory.SoftBackground;
            border.BorderBrush = UiFactory.BorderBrush;
            border.BorderThickness = new Thickness(1.0, 1.0, 1.0, 0.0);
            border.Child = panel;
            return border;
        }

        private ComboBox CreateFunctionCombo()
        {
            ComboBox combo = new ComboBox();
            combo.MinHeight = 48.0;
            combo.FontSize = 17.0;
            combo.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
            return combo;
        }

        private UIElement BuildTemplatesTab()
        {
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(12.0, 12.0, 12.0, 12.0);
            _templateListPanel = new StackPanel();
            scroll.Content = _templateListPanel;

            IList<FunctionTemplate> templates = TemplateLibrary.All();
            for (int i = 0; i < templates.Count; i++)
            {
                FunctionTemplate template = templates[i];
                StackPanel content = new StackPanel();
                TextBlock title = UiFactory.Text(template.Name, 19.0, UiFactory.TextBrush);
                title.FontWeight = FontWeights.SemiBold;
                content.Children.Add(title);
                TextBlock expression = UiFactory.Text("y = " + template.Expression, 18.0, UiFactory.AccentBrush);
                expression.Margin = new Thickness(0.0, 5.0, 0.0, 4.0);
                content.Children.Add(expression);
                TextBlock description = UiFactory.Text(template.Description, 14.0, UiFactory.MutedTextBrush);
                content.Children.Add(description);

                Button button = new Button();
                button.Content = content;
                button.Background = UiFactory.PanelBackground;
                button.BorderBrush = UiFactory.BorderBrush;
                button.BorderThickness = new Thickness(1.0);
                button.Padding = new Thickness(14.0);
                button.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
                button.HorizontalContentAlignment = HorizontalAlignment.Left;
                button.Click += delegate(object sender, RoutedEventArgs e)
                {
                    AddTemplateFunction(template);
                };
                _templateListPanel.Children.Add(button);
            }
            return scroll;
        }

        private Grid BuildGraphHost()
        {
            Grid host = new Grid();
            host.Margin = new Thickness(10.0, 10.0, 12.0, 10.0);
            host.ClipToBounds = true;

            Border graphBorder = new Border();
            graphBorder.Background = Brushes.White;
            graphBorder.BorderBrush = UiFactory.BorderBrush;
            graphBorder.BorderThickness = new Thickness(1.0);
            graphBorder.CornerRadius = new CornerRadius(10.0);
            graphBorder.ClipToBounds = true;
            _graphView = new GraphView();
            _graphView.ViewportChanged += delegate
            {
                ScheduleSave();
                if (_intersectionActive)
                    ScheduleIntersectionRefresh();
            };
            graphBorder.Child = _graphView;
            host.Children.Add(graphBorder);

            Border toolsBorder = new Border();
            toolsBorder.HorizontalAlignment = HorizontalAlignment.Right;
            toolsBorder.VerticalAlignment = VerticalAlignment.Top;
            toolsBorder.Margin = new Thickness(0.0, 12.0, 14.0, 0.0);
            toolsBorder.Background = UiFactory.Brush("#F2FFFFFF");
            toolsBorder.BorderBrush = UiFactory.BorderBrush;
            toolsBorder.BorderThickness = new Thickness(1.0);
            toolsBorder.CornerRadius = new CornerRadius(8.0);
            toolsBorder.Padding = new Thickness(4.0);
            WrapPanel tools = new WrapPanel();
            tools.Children.Add(UiFactory.SmallButton("＋", delegate { _graphView.ZoomIn(); }));
            tools.Children.Add(UiFactory.SmallButton("－", delegate { _graphView.ZoomOut(); }));
            tools.Children.Add(UiFactory.SmallButton("适配", delegate { _graphView.FitVisibleFunctions(); }));
            tools.Children.Add(UiFactory.SmallButton("复位", delegate { _graphView.SetViewport(-10.0, 10.0, -7.0, 7.0); }));
            toolsBorder.Child = tools;
            host.Children.Add(toolsBorder);

            _presentationExitButton = UiFactory.Button("退出演示", ExitPresentationMode, true);
            _presentationExitButton.HorizontalAlignment = HorizontalAlignment.Right;
            _presentationExitButton.VerticalAlignment = VerticalAlignment.Bottom;
            _presentationExitButton.Margin = new Thickness(0.0, 0.0, 22.0, 20.0);
            _presentationExitButton.MinWidth = 130.0;
            _presentationExitButton.Visibility = Visibility.Collapsed;
            host.Children.Add(_presentationExitButton);
            return host;
        }

        private Border BuildKeypad()
        {
            StackPanel outer = new StackPanel();
            outer.Margin = new Thickness(18.0, 8.0, 18.0, 10.0);

            Grid editorRow = new Grid();
            editorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185.0) });
            editorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            editorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _editorTitle = UiFactory.Text("编辑函数", 19.0, UiFactory.TextBrush);
            _editorTitle.FontWeight = FontWeights.SemiBold;
            _editorTitle.VerticalAlignment = VerticalAlignment.Center;
            editorRow.Children.Add(_editorTitle);

            _expressionBox = new TextBox();
            _expressionBox.FontSize = 25.0;
            _expressionBox.MinHeight = 58.0;
            _expressionBox.Padding = new Thickness(12.0, 7.0, 12.0, 7.0);
            _expressionBox.VerticalContentAlignment = VerticalAlignment.Center;
            _expressionBox.Background = Brushes.White;
            _expressionBox.BorderBrush = UiFactory.AccentBrush;
            _expressionBox.BorderThickness = new Thickness(2.0);
            _expressionBox.Margin = new Thickness(4.0, 0.0, 10.0, 0.0);
            _expressionBox.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    ApplyCurrentExpression();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    HideKeypad();
                    e.Handled = true;
                }
            };
            InputMethod.SetIsInputMethodEnabled(_expressionBox, false);
            Grid.SetColumn(_expressionBox, 1);
            editorRow.Children.Add(_expressionBox);

            WrapPanel editorActions = new WrapPanel();
            editorActions.Children.Add(UiFactory.Button("绘制", ApplyCurrentExpression, true));
            editorActions.Children.Add(UiFactory.Button("收起", HideKeypad, false));
            Grid.SetColumn(editorActions, 2);
            editorRow.Children.Add(editorActions);
            outer.Children.Add(editorRow);

            UniformGrid keys = new UniformGrid();
            keys.Columns = 8;
            keys.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
            string[] labels =
            {
                "7", "8", "9", "÷", "(", ")", "π", "⌫",
                "4", "5", "6", "×", "x", "^", "e", "清空",
                "1", "2", "3", "-", "+", ".", "a", "b",
                "0", "sin(", "cos(", "tan(", "ln(", "log(", "sqrt(", "abs(",
                "x^2", "c", "h", "k", ",", "←", "→", "收起"
            };
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                Button key = UiFactory.Button(label, delegate { HandleKey(label); }, false);
                key.Focusable = false;
                key.MinHeight = 48.0;
                key.Margin = new Thickness(3.0);
                key.FontSize = 18.0;
                keys.Children.Add(key);
            }
            outer.Children.Add(keys);

            Border border = new Border();
            border.Background = UiFactory.Brush("#F9FBFD");
            border.BorderBrush = UiFactory.BorderBrush;
            border.BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0);
            border.Child = outer;
            border.Visibility = Visibility.Collapsed;
            return border;
        }

        private void HandleKey(string label)
        {
            if (_expressionBox == null)
                return;
            if (label == "收起")
            {
                HideKeypad();
                return;
            }
            if (label == "清空")
            {
                _expressionBox.Clear();
                return;
            }
            if (label == "⌫")
            {
                int start = _expressionBox.SelectionStart;
                int length = _expressionBox.SelectionLength;
                if (length > 0)
                {
                    _expressionBox.Text = _expressionBox.Text.Remove(start, length);
                    _expressionBox.SelectionStart = start;
                }
                else if (start > 0)
                {
                    _expressionBox.Text = _expressionBox.Text.Remove(start - 1, 1);
                    _expressionBox.SelectionStart = start - 1;
                }
                return;
            }
            if (label == "←")
            {
                _expressionBox.SelectionStart = Math.Max(0, _expressionBox.SelectionStart - 1);
                _expressionBox.SelectionLength = 0;
                return;
            }
            if (label == "→")
            {
                _expressionBox.SelectionStart = Math.Min(
                    _expressionBox.Text.Length, _expressionBox.SelectionStart + 1);
                _expressionBox.SelectionLength = 0;
                return;
            }

            string insertion = label;
            if (label == "÷") insertion = "/";
            else if (label == "×") insertion = "*";
            else if (label == "x^2") insertion = "^2";

            int selectionStart = _expressionBox.SelectionStart;
            int selectionLength = _expressionBox.SelectionLength;
            string text = _expressionBox.Text;
            _expressionBox.Text = text.Remove(selectionStart, selectionLength)
                .Insert(selectionStart, insertion);
            _expressionBox.SelectionStart = selectionStart + insertion.Length;
            _expressionBox.SelectionLength = 0;
        }

        private void LoadWorkspace()
        {
            string path = ProjectStore.DefaultPath;
            _freshWorkspace = !File.Exists(path);
            SavedWorkspace workspace = ProjectStore.Load(path);

            _functions.Clear();
            for (int i = 0; i < workspace.Functions.Count && i < MaximumFunctions; i++)
            {
                SavedFunction saved = workspace.Functions[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.Expression))
                    continue;

                FunctionPlotItem function = new FunctionPlotItem();
                function.Id = saved.Id > 0 ? saved.Id : i + 1;
                function.Name = string.IsNullOrWhiteSpace(saved.Name) ? "f" + function.Id : saved.Name;
                function.ExpressionText = saved.Expression;
                function.Color = ParseColor(saved.Color, FunctionColors[i % FunctionColors.Length]);
                function.IsVisible = saved.IsVisible;
                try
                {
                    function.Expression = ExpressionParser.Parse(saved.Expression);
                    for (int parameterIndex = 0;
                         parameterIndex < function.Expression.ParameterNames.Count;
                         parameterIndex++)
                    {
                        string name = function.Expression.ParameterNames[parameterIndex];
                        double value = DefaultParameterValue(name);
                        if (saved.ParameterNames != null &&
                            saved.ParameterValues != null)
                        {
                            int savedIndex = saved.ParameterNames.IndexOf(name);
                            if (savedIndex >= 0 && savedIndex < saved.ParameterValues.Count)
                                value = saved.ParameterValues[savedIndex];
                        }
                        function.Parameters[name] = value;
                    }
                }
                catch (Exception error)
                {
                    function.Error = "表达式错误：" + error.Message;
                }
                _functions.Add(function);
            }

            if (_functions.Count == 0)
            {
                FunctionPlotItem initial = CreateFunction("x^2");
                _functions.Add(initial);
            }

            _nextFunctionId = _functions.Max(function => function.Id) + 1;
            _activeFunctionId = _functions[0].Id;
            _graphView.SetViewport(workspace.XMin, workspace.XMax, workspace.YMin, workspace.YMax);
            RefreshAll();
        }

        private FunctionPlotItem CreateFunction(string expressionText)
        {
            int id = _nextFunctionId++;
            FunctionPlotItem function = new FunctionPlotItem();
            function.Id = id;
            function.Name = "f" + id;
            function.ExpressionText = expressionText;
            function.Color = ParseColor(FunctionColors[(id - 1) % FunctionColors.Length],
                FunctionColors[(id - 1) % FunctionColors.Length]);
            function.IsVisible = true;
            try
            {
                function.Expression = ExpressionParser.Parse(expressionText);
                for (int i = 0; i < function.Expression.ParameterNames.Count; i++)
                {
                    string name = function.Expression.ParameterNames[i];
                    function.Parameters[name] = DefaultParameterValue(name);
                }
            }
            catch (Exception error)
            {
                function.Error = "表达式错误：" + error.Message;
            }
            return function;
        }

        private void AddFunction()
        {
            if (_functions.Count >= MaximumFunctions)
            {
                SetStatus("最多同时显示六条函数。", UiFactory.DangerBrush);
                return;
            }
            FunctionPlotItem function = CreateFunction("x");
            _functions.Add(function);
            _activeFunctionId = function.Id;
            RefreshAll();
            if (_functions.Count > 1)
            {
                SelectChoice(_firstFunctionCombo, _functions[0].Id, 0);
                SelectChoice(_secondFunctionCombo, function.Id, 1);
            }
            ShowEditor(function.Id);
            ScheduleSave();
        }

        private void AddTemplateFunction(FunctionTemplate template)
        {
            if (_functions.Count >= MaximumFunctions)
            {
                SetStatus("最多同时显示六条函数。", UiFactory.DangerBrush);
                return;
            }
            FunctionPlotItem function = CreateFunction(template.Expression);
            _functions.Add(function);
            _activeFunctionId = function.Id;
            RefreshAll();
            if (_functions.Count > 1)
            {
                SelectChoice(_firstFunctionCombo, _functions[0].Id, 0);
                SelectChoice(_secondFunctionCombo, function.Id, 1);
            }
            ShowEditor(function.Id);
            ScheduleSave();
            SetStatus("已加入：" + template.Name, UiFactory.SuccessBrush);
        }

        private void RefreshAll()
        {
            RefreshFunctionList();
            RefreshFunctionChoices();
            RefreshGraph();
            if (_intersectionActive)
                ScheduleIntersectionRefresh();
        }

        private void RefreshFunctionList()
        {
            _functionListPanel.Children.Clear();
            for (int i = 0; i < _functions.Count; i++)
                _functionListPanel.Children.Add(BuildFunctionCard(_functions[i]));
        }

        private Border BuildFunctionCard(FunctionPlotItem function)
        {
            StackPanel content = new StackPanel();
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10.0) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border swatch = new Border();
            swatch.Background = new SolidColorBrush(function.Color);
            swatch.CornerRadius = new CornerRadius(4.0);
            swatch.Margin = new Thickness(0.0, 2.0, 10.0, 2.0);
            header.Children.Add(swatch);

            TextBlock name = UiFactory.Text(function.Name + "(x)", 19.0, UiFactory.TextBrush);
            name.FontWeight = FontWeights.SemiBold;
            name.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(name, 1);
            header.Children.Add(name);

            Button visibility = UiFactory.SmallButton(function.IsVisible ? "显示" : "隐藏", delegate
            {
                function.IsVisible = !function.IsVisible;
                RefreshFunctionList();
                RefreshGraph();
                ScheduleSave();
            });
            Grid.SetColumn(visibility, 2);
            header.Children.Add(visibility);

            Button delete = UiFactory.SmallButton("删除", delegate { DeleteFunction(function); });
            delete.Foreground = UiFactory.DangerBrush;
            Grid.SetColumn(delete, 3);
            header.Children.Add(delete);
            content.Children.Add(header);

            Button expression = new Button();
            expression.Content = "y = " + MathDisplay.Pretty(function.ExpressionText);
            expression.HorizontalContentAlignment = HorizontalAlignment.Left;
            expression.MinHeight = 54.0;
            expression.FontSize = 20.0;
            expression.Padding = new Thickness(12.0, 7.0, 12.0, 7.0);
            expression.Margin = new Thickness(0.0, 8.0, 0.0, 3.0);
            expression.Background = function.Id == _activeFunctionId ? UiFactory.AccentSoftBrush : Brushes.White;
            expression.BorderBrush = function.Id == _activeFunctionId ? UiFactory.AccentBrush : UiFactory.BorderBrush;
            expression.Click += delegate { ShowEditor(function.Id); };
            content.Children.Add(expression);

            if (!string.IsNullOrWhiteSpace(function.Error))
            {
                TextBlock error = UiFactory.Text(function.Error, 13.0, UiFactory.DangerBrush);
                error.Margin = new Thickness(2.0, 3.0, 2.0, 3.0);
                content.Children.Add(error);
            }

            if (function.Parameters.Count > 0)
            {
                TextBlock parameterTitle = UiFactory.Text("参数", 14.0, UiFactory.MutedTextBrush);
                parameterTitle.Margin = new Thickness(2.0, 8.0, 0.0, 2.0);
                content.Children.Add(parameterTitle);
                foreach (KeyValuePair<string, double> parameter in function.Parameters.OrderBy(pair => pair.Key))
                    content.Children.Add(BuildParameterRow(function, parameter.Key));
            }

            Border card = UiFactory.Card(content);
            if (function.Id == _activeFunctionId)
            {
                card.BorderBrush = UiFactory.AccentBrush;
                card.BorderThickness = new Thickness(2.0);
                card.Background = UiFactory.Brush("#FBFDFF");
            }
            return card;
        }

        private Grid BuildParameterRow(FunctionPlotItem function, string name)
        {
            Grid row = new Grid();
            row.MinHeight = 44.0;
            row.Margin = new Thickness(0.0, 2.0, 0.0, 2.0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30.0) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60.0) });

            TextBlock label = UiFactory.Text(name, 18.0, UiFactory.TextBrush);
            label.FontWeight = FontWeights.SemiBold;
            label.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(label);

            Slider slider = new Slider();
            slider.Minimum = -10.0;
            slider.Maximum = 10.0;
            slider.TickFrequency = 0.1;
            slider.IsMoveToPointEnabled = true;
            slider.MinHeight = 42.0;
            slider.Margin = new Thickness(4.0, 0.0, 8.0, 0.0);
            slider.Value = function.GetParameter(name);
            slider.Tag = name;
            Grid.SetColumn(slider, 1);
            row.Children.Add(slider);

            TextBlock valueText = UiFactory.Text(
                function.GetParameter(name).ToString("0.##", CultureInfo.CurrentCulture),
                16.0,
                UiFactory.AccentBrush);
            valueText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(valueText, 2);
            row.Children.Add(valueText);

            slider.ValueChanged += delegate(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                function.Parameters[name] = Math.Round(e.NewValue, 3);
                valueText.Text = e.NewValue.ToString("0.##", CultureInfo.CurrentCulture);
                RefreshGraph();
                if (_intersectionActive)
                    ScheduleIntersectionRefresh();
                ScheduleSave();
            };
            return row;
        }

        private void DeleteFunction(FunctionPlotItem function)
        {
            if (_functions.Count <= 1)
            {
                SetStatus("至少保留一个函数。", UiFactory.DangerBrush);
                return;
            }
            MessageBoxResult answer = MessageBox.Show(
                "删除 " + function.Name + "(x)？",
                "删除函数",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            _functions.Remove(function);
            if (_activeFunctionId == function.Id)
                _activeFunctionId = _functions[0].Id;
            _intersectionActive = false;
            _graphView.ClearIntersections();
            RefreshAll();
            ShowEditor(_activeFunctionId);
            ScheduleSave();
        }

        private void RefreshFunctionChoices()
        {
            if (_firstFunctionCombo == null || _secondFunctionCombo == null)
                return;
            int previousFirst = GetSelectedFunctionId(_firstFunctionCombo);
            int previousSecond = GetSelectedFunctionId(_secondFunctionCombo);
            _firstFunctionCombo.Items.Clear();
            _secondFunctionCombo.Items.Clear();
            for (int i = 0; i < _functions.Count; i++)
            {
                FunctionChoice choice = new FunctionChoice(_functions[i].Id, _functions[i].Name + "(x)");
                _firstFunctionCombo.Items.Add(choice);
                _secondFunctionCombo.Items.Add(new FunctionChoice(choice.Id, choice.Text));
            }

            SelectChoice(_firstFunctionCombo, previousFirst, 0);
            SelectChoice(_secondFunctionCombo, previousSecond, _functions.Count > 1 ? 1 : 0);
        }

        private static void SelectChoice(ComboBox combo, int id, int fallbackIndex)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FunctionChoice choice = combo.Items[i] as FunctionChoice;
                if (choice != null && choice.Id == id)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = Math.Min(fallbackIndex, combo.Items.Count - 1);
        }

        private static int GetSelectedFunctionId(ComboBox combo)
        {
            FunctionChoice choice = combo.SelectedItem as FunctionChoice;
            return choice == null ? -1 : choice.Id;
        }

        private void RefreshGraph()
        {
            _graphView.SetFunctions(_functions);
        }

        private void ShowEditor(int functionId)
        {
            FunctionPlotItem function = FindFunction(functionId);
            if (function == null)
                return;
            _activeFunctionId = functionId;
            _editorTitle.Text = "编辑 " + function.Name + "(x)";
            _expressionBox.Text = function.ExpressionText;
            _expressionBox.SelectionStart = _expressionBox.Text.Length;
            _keypadPanel.Visibility = Visibility.Visible;
            RefreshFunctionList();
        }

        private void HideKeypad()
        {
            _keypadPanel.Visibility = Visibility.Collapsed;
        }

        private void ApplyCurrentExpression()
        {
            FunctionPlotItem function = FindFunction(_activeFunctionId);
            if (function == null)
            {
                SetStatus("请先选择一个函数。", UiFactory.DangerBrush);
                return;
            }

            string expressionText = _expressionBox.Text.Trim();
            function.ExpressionText = expressionText;
            try
            {
                FunctionExpression parsed = ExpressionParser.Parse(expressionText);
                Dictionary<string, double> oldValues = new Dictionary<string, double>(function.Parameters);
                function.Parameters.Clear();
                for (int i = 0; i < parsed.ParameterNames.Count; i++)
                {
                    string name = parsed.ParameterNames[i];
                    double value;
                    if (!oldValues.TryGetValue(name, out value))
                        value = DefaultParameterValue(name);
                    function.Parameters[name] = value;
                }
                function.Expression = parsed;
                function.Error = null;
                RefreshFunctionList();
                RefreshGraph();
                if (_intersectionActive)
                    ScheduleIntersectionRefresh();
                ScheduleSave();
                SetStatus("已绘制 " + function.Name + "(x)", UiFactory.SuccessBrush);
            }
            catch (Exception error)
            {
                function.Expression = null;
                function.Error = "表达式错误：" + error.Message;
                RefreshFunctionList();
                RefreshGraph();
                SetStatus("表达式有误，请检查后重试。", UiFactory.DangerBrush);
            }
        }

        private void FindIntersections(bool withXAxis)
        {
            _intersectionActive = true;
            _intersectionWithXAxis = withXAxis;
            _intersectionA = GetSelectedFunctionId(_firstFunctionCombo);
            _intersectionB = withXAxis ? -1 : GetSelectedFunctionId(_secondFunctionCombo);
            RefreshIntersections();
        }

        private void ScheduleIntersectionRefresh()
        {
            if (!_intersectionActive)
                return;
            _intersectionTimer.Stop();
            _intersectionTimer.Start();
        }

        private void RefreshIntersections()
        {
            if (!_intersectionActive)
                return;

            FunctionPlotItem first = FindFunction(_intersectionA);
            FunctionPlotItem second = _intersectionWithXAxis ? null : FindFunction(_intersectionB);
            if (first == null || first.Expression == null)
            {
                _graphView.ClearIntersections();
                _intersectionStatus.Text = "请先选择有效函数。";
                return;
            }
            if (!_intersectionWithXAxis && (second == null || second.Expression == null))
            {
                _graphView.ClearIntersections();
                _intersectionStatus.Text = "请选择两条有效函数。";
                return;
            }
            if (!_intersectionWithXAxis && first.Id == second.Id)
            {
                _graphView.ClearIntersections();
                _intersectionStatus.Text = "请选择两条不同的函数。";
                return;
            }

            Func<double, double> firstFunction = delegate(double x) { return first.Evaluate(x); };
            Func<double, double> secondFunction;
            if (_intersectionWithXAxis)
                secondFunction = delegate(double x) { return 0.0; };
            else
                secondFunction = delegate(double x) { return second.Evaluate(x); };
            int samples = (int)Math.Max(500.0, Math.Min(3000.0, _graphView.ActualWidth * 1.2));
            IntersectionResult result = IntersectionFinder.Find(
                firstFunction, secondFunction, _graphView.XMin, _graphView.XMax, samples);
            _graphView.SetIntersections(result.Points);

            if (result.OverlapSuspected)
            {
                _intersectionStatus.Text = "两条函数在当前视野内可能重合。";
            }
            else if (result.Points.Count == 0)
            {
                _intersectionStatus.Text = "当前视野内没有交点。";
            }
            else
            {
                string status = "当前视野内找到 " + result.Points.Count + " 个交点";
                if (result.LimitReached)
                    status += "（仅显示前 120 个）";
                status += "。";
                _intersectionStatus.Text = status;
            }
        }

        private void EnterPresentationMode()
        {
            if (_presentationMode)
                return;
            _savedWindowStyle = WindowStyle;
            _savedResizeMode = ResizeMode;
            _savedWindowState = WindowState;
            _presentationMode = true;
            _topBar.Visibility = Visibility.Collapsed;
            _leftPanel.Visibility = Visibility.Collapsed;
            _leftColumn.Width = new GridLength(0.0);
            _splitterColumn.Width = new GridLength(0.0);
            _keypadPanel.Visibility = Visibility.Collapsed;
            _presentationExitButton.Visibility = Visibility.Visible;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        private void ExitPresentationMode()
        {
            if (!_presentationMode)
                return;
            _presentationMode = false;
            WindowStyle = _savedWindowStyle;
            ResizeMode = _savedResizeMode;
            WindowState = _savedWindowState;
            _topBar.Visibility = Visibility.Visible;
            _leftPanel.Visibility = Visibility.Visible;
            _leftColumn.Width = new GridLength(405.0);
            _splitterColumn.Width = new GridLength(7.0);
            _presentationExitButton.Visibility = Visibility.Collapsed;
        }

        private void ExportPng()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "导出函数图像";
            dialog.Filter = "PNG 图片|*.png";
            dialog.FileName = "函数图像_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            if (dialog.ShowDialog(this) != true)
                return;
            try
            {
                ImageExporter.SavePng(dialog.FileName, _graphView);
                SetStatus("已导出 PNG。", UiFactory.SuccessBrush);
            }
            catch (Exception error)
            {
                SetStatus("导出失败：" + error.Message, UiFactory.DangerBrush);
            }
        }

        private void CopyGraphImage()
        {
            try
            {
                ImageExporter.CopyToClipboard(_graphView);
                SetStatus("图像已复制，可粘贴到希沃白板。", UiFactory.SuccessBrush);
            }
            catch (Exception error)
            {
                SetStatus("复制失败：" + error.Message, UiFactory.DangerBrush);
            }
        }

        private void ScheduleSave()
        {
            if (_saveTimer == null)
                return;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveWorkspace(bool report)
        {
            try
            {
                SavedWorkspace workspace = new SavedWorkspace();
                workspace.XMin = _graphView.XMin;
                workspace.XMax = _graphView.XMax;
                workspace.YMin = _graphView.YMin;
                workspace.YMax = _graphView.YMax;
                for (int i = 0; i < _functions.Count; i++)
                {
                    FunctionPlotItem function = _functions[i];
                    SavedFunction saved = new SavedFunction();
                    saved.Id = function.Id;
                    saved.Name = function.Name;
                    saved.Expression = function.ExpressionText;
                    saved.Color = new SolidColorBrush(function.Color).Color.ToString();
                    saved.IsVisible = function.IsVisible;
                    foreach (KeyValuePair<string, double> parameter in function.Parameters.OrderBy(pair => pair.Key))
                    {
                        saved.ParameterNames.Add(parameter.Key);
                        saved.ParameterValues.Add(parameter.Value);
                    }
                    workspace.Functions.Add(saved);
                }
                ProjectStore.Save(ProjectStore.DefaultPath, workspace);
                if (report)
                    SetStatus("已保存到本地。", UiFactory.SuccessBrush);
            }
            catch (Exception error)
            {
                SetStatus("保存失败：" + error.Message, UiFactory.DangerBrush);
            }
        }

        private void SetStatus(string text, Brush brush)
        {
            if (_statusText == null)
                return;
            _statusText.Text = text;
            _statusText.Foreground = brush;
        }

        private FunctionPlotItem FindFunction(int id)
        {
            for (int i = 0; i < _functions.Count; i++)
            {
                if (_functions[i].Id == id)
                    return _functions[i];
            }
            return null;
        }

        private static double DefaultParameterValue(string name)
        {
            if (name == "c" || name == "h" || name == "k")
                return 0.0;
            return 1.0;
        }

        private static Color ParseColor(string value, string fallback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return (Color)ColorConverter.ConvertFromString(value);
            }
            catch
            {
            }
            return (Color)ColorConverter.ConvertFromString(fallback);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _saveTimer.Stop();
            SaveWorkspace(false);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _presentationMode)
            {
                ExitPresentationMode();
                e.Handled = true;
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveWorkspace(true);
                e.Handled = true;
            }
            else if (e.Key == Key.F11)
            {
                if (_presentationMode)
                    ExitPresentationMode();
                else
                    EnterPresentationMode();
                e.Handled = true;
            }
        }

        private sealed class FunctionChoice
        {
            public FunctionChoice(int id, string text)
            {
                Id = id;
                Text = text;
            }

            public int Id { get; private set; }
            public string Text { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
