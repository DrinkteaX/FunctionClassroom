using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FunctionClassroom.Setup
{
    internal static class Program
    {
        private const string DisplayName = "函数课堂";
        private const string DisplayVersion = "0.1.0";

        [STAThread]
        private static int Main(string[] args)
        {
            bool silent = HasArgument(args, "/silent");
            bool verify = HasArgument(args, "/verify");
            bool uninstall = HasArgument(args, "/uninstall") ||
                string.Equals(Path.GetFileNameWithoutExtension(Application.ExecutablePath),
                    "Uninstall", StringComparison.OrdinalIgnoreCase);
            string target = ReadArgumentValue(args, "/target:");

            if (verify)
                return InstallerEngine.Install(target, false, false, false) ? 0 : 1;

            if (silent)
            {
                if (uninstall)
                    return InstallerEngine.Uninstall(target, true) ? 0 : 1;
                return InstallerEngine.Install(target, true, true, true) ? 0 : 1;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm(uninstall, target));
            return 0;
        }

        private static bool HasArgument(string[] args, string value)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string ReadArgumentValue(string[] args, string prefix)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(prefix.Length).Trim('"');
            }
            return null;
        }

        internal sealed class SetupForm : Form
        {
            private readonly bool _uninstallOnly;
            private readonly string _requestedTarget;
            private readonly Label _status;
            private readonly ProgressBar _progress;
            private readonly Button _actionButton;
            private readonly CheckBox _launchCheckBox;

            public SetupForm(bool uninstallOnly, string requestedTarget)
            {
                _uninstallOnly = uninstallOnly;
                _requestedTarget = requestedTarget;

                Text = DisplayName + " 安装程序";
                ClientSize = new Size(660, 390);
                MinimumSize = new Size(660, 390);
                MaximumSize = new Size(660, 390);
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                BackColor = Color.FromArgb(245, 248, 252);
                Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Regular);

                Panel header = new Panel();
                header.BackColor = Color.FromArgb(15, 94, 168);
                header.Dock = DockStyle.Top;
                header.Height = 116;
                Controls.Add(header);

                Label title = new Label();
                title.Text = DisplayName;
                title.ForeColor = Color.White;
                title.Font = new Font("Microsoft YaHei UI", 27.0f, FontStyle.Bold);
                title.AutoSize = true;
                title.Location = new Point(30, 18);
                header.Controls.Add(title);

                Label subtitle = new Label();
                subtitle.Text = "Windows 10 希沃大屏触控函数绘图软件";
                subtitle.ForeColor = Color.FromArgb(224, 239, 252);
                subtitle.AutoSize = true;
                subtitle.Location = new Point(33, 69);
                header.Controls.Add(subtitle);

                Label credit = new Label();
                credit.Text = "制作：雾雨魔理沙    ·    版本 " + DisplayVersion;
                credit.ForeColor = Color.FromArgb(90, 105, 122);
                credit.AutoSize = true;
                credit.Location = new Point(32, 140);
                Controls.Add(credit);

                Label summary = new Label();
                summary.Text = _uninstallOnly
                    ? "卸载函数课堂。\n用户保存的函数工作区将保留。"
                    : "安装到当前 Windows 用户，无需管理员权限。\n包含桌面快捷方式、开始菜单入口和系统卸载入口。";
                summary.ForeColor = Color.FromArgb(29, 41, 56);
                summary.Font = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Regular);
                summary.AutoSize = false;
                summary.SetBounds(32, 173, 590, 62);
                Controls.Add(summary);

                Label pathLabel = new Label();
                pathLabel.Text = "安装位置：" + InstallerEngine.GetTargetPath(_requestedTarget);
                pathLabel.ForeColor = Color.FromArgb(90, 105, 122);
                pathLabel.AutoSize = false;
                pathLabel.SetBounds(32, 237, 590, 24);
                Controls.Add(pathLabel);

                _progress = new ProgressBar();
                _progress.Style = ProgressBarStyle.Marquee;
                _progress.MarqueeAnimationSpeed = 24;
                _progress.Visible = false;
                _progress.SetBounds(32, 270, 250, 8);
                Controls.Add(_progress);

                _status = new Label();
                _status.Text = InstallerEngine.IsInstalled() ? "检测到已安装版本，可覆盖安装。" : "准备安装。";
                _status.ForeColor = Color.FromArgb(90, 105, 122);
                _status.AutoSize = false;
                _status.SetBounds(32, 284, 590, 28);
                Controls.Add(_status);

                _launchCheckBox = new CheckBox();
                _launchCheckBox.Text = "安装完成后启动函数课堂";
                _launchCheckBox.Checked = true;
                _launchCheckBox.Visible = !_uninstallOnly;
                _launchCheckBox.AutoSize = true;
                _launchCheckBox.Location = new Point(32, 320);
                Controls.Add(_launchCheckBox);

                _actionButton = CreateButton(
                    _uninstallOnly ? "卸载" : "开始安装",
                    Color.FromArgb(15, 94, 168),
                    Color.White);
                _actionButton.SetBounds(380, 320, 116, 44);
                _actionButton.Click += OnAction;
                Controls.Add(_actionButton);

                Button close = CreateButton("退出", Color.FromArgb(244, 247, 250), Color.FromArgb(29, 41, 56));
                close.SetBounds(506, 320, 116, 44);
                close.Click += delegate { Close(); };
                Controls.Add(close);
            }

            private static Button CreateButton(string text, Color back, Color fore)
            {
                Button button = new Button();
                button.Text = text;
                button.BackColor = back;
                button.ForeColor = fore;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(200, 211, 224);
                button.Font = new Font("Microsoft YaHei UI", 11.0f, FontStyle.Bold);
                button.Cursor = Cursors.Hand;
                return button;
            }

            private void OnAction(object sender, EventArgs e)
            {
                _actionButton.Enabled = false;
                _progress.Visible = true;
                _status.Text = _uninstallOnly ? "正在卸载……" : "正在安装……";
                Refresh();

                try
                {
                    bool success = _uninstallOnly
                        ? ConfirmAndUninstall()
                        : InstallerEngine.Install(_requestedTarget, true, true, true);
                    _status.Text = success
                        ? (_uninstallOnly ? "卸载已安排完成。" : "安装完成。")
                        : "操作未完成。";
                    if (success && !_uninstallOnly)
                    {
                        if (_launchCheckBox.Checked)
                            InstallerEngine.Launch(InstallerEngine.GetTargetPath(_requestedTarget));
                        if (!_uninstallOnly)
                        {
                            MessageBox.Show(this, "函数课堂已安装完成。", DisplayName,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    if (success && _uninstallOnly)
                        Close();
                }
                catch (Exception error)
                {
                    _status.Text = "操作失败。";
                    MessageBox.Show(this, error.Message, DisplayName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _progress.Visible = false;
                    _actionButton.Enabled = true;
                }
            }

            private bool ConfirmAndUninstall()
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "确定卸载函数课堂？\n\n用户保存的函数工作区会保留。",
                    "卸载函数课堂",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                return result == DialogResult.Yes &&
                    InstallerEngine.Uninstall(_requestedTarget, false);
            }
        }
    }

    internal static class InstallerEngine
    {
        private const string AppFileName = "FunctionClassroom.exe";
        private const string UninstallFileName = "Uninstall.exe";

        public static string DefaultTargetPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "FunctionClassroom");
            }
        }

        public static string GetTargetPath(string requested)
        {
            if (!string.IsNullOrWhiteSpace(requested))
                return Path.GetFullPath(requested);
            string installed = ReadInstalledPath();
            return string.IsNullOrWhiteSpace(installed) ? DefaultTargetPath : installed;
        }

        public static bool IsInstalled()
        {
            string path = ReadInstalledPath();
            return !string.IsNullOrWhiteSpace(path) &&
                File.Exists(Path.Combine(path, AppFileName));
        }

        public static bool Install(string requestedTarget, bool shortcuts, bool register, bool copyUninstaller)
        {
            if (IsAppRunning())
                throw new InvalidOperationException("函数课堂正在运行，请先关闭软件再安装。");

            string target = GetTargetPath(requestedTarget);
            Directory.CreateDirectory(target);
            ExtractResource("FunctionClassroom.exe", Path.Combine(target, AppFileName));
            ExtractResource("FunctionClassroom.exe.config", Path.Combine(target, "FunctionClassroom.exe.config"));
            ExtractResource("README.md", Path.Combine(target, "README.md"));

            if (copyUninstaller)
            {
                string current = Application.ExecutablePath;
                string uninstallPath = Path.Combine(target, UninstallFileName);
                if (!string.Equals(
                    Path.GetFullPath(current),
                    Path.GetFullPath(uninstallPath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(current, uninstallPath, true);
                }
            }

            if (shortcuts)
                CreateShortcuts(target);
            if (register)
                RegisterUninstall(target);
            return true;
        }

        public static bool Uninstall(string requestedTarget, bool quiet)
        {
            if (IsAppRunning())
            {
                if (quiet)
                    return false;
                MessageBox.Show("函数课堂正在运行，请先关闭软件再卸载。", "函数课堂",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string target = GetTargetPath(requestedTarget);
            if (!File.Exists(Path.Combine(target, AppFileName)))
                return false;
            DeleteShortcut(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "函数课堂.lnk"));
            DeleteShortcut(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), "函数课堂.lnk"));
            DeleteShortcut(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), "卸载函数课堂.lnk"));

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FunctionClassroom", true))
            {
                if (key != null)
                    key.DeleteSubKeyTree(string.Empty, false);
            }

            ScheduleDirectoryRemoval(target);
            return true;
        }

        public static void Launch(string target)
        {
            string executable = Path.Combine(target, AppFileName);
            if (!File.Exists(executable))
                return;
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = target,
                UseShellExecute = true
            });
        }

        private static void ExtractResource(string resourceName, string outputPath)
        {
            using (Stream source = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceName))
            {
                if (source == null)
                    throw new InvalidOperationException("安装包资源缺失：" + resourceName);

                string temporary = outputPath + ".tmp";
                using (FileStream destination = new FileStream(
                    temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    source.CopyTo(destination);
                }
                File.Copy(temporary, outputPath, true);
                File.Delete(temporary);
            }
        }

        private static void CreateShortcuts(string target)
        {
            string executable = Path.Combine(target, AppFileName);
            string uninstaller = Path.Combine(target, UninstallFileName);
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "函数课堂.lnk"),
                executable,
                target,
                "函数课堂 - 制作：雾雨魔理沙");
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "函数课堂.lnk"),
                executable,
                target,
                "函数课堂 - 制作：雾雨魔理沙");
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "卸载函数课堂.lnk"),
                uninstaller,
                target,
                "卸载函数课堂");
        }

        private static void CreateShortcut(
            string shortcutPath,
            string targetPath,
            string workingDirectory,
            string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                return;
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath + ",0" });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private static void DeleteShortcut(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static void RegisterUninstall(string target)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FunctionClassroom"))
            {
                key.SetValue("DisplayName", "函数课堂");
                key.SetValue("DisplayVersion", "0.1.0");
                key.SetValue("Publisher", "雾雨魔理沙");
                key.SetValue("InstallLocation", target);
                key.SetValue("DisplayIcon", Path.Combine(target, AppFileName) + ",0");
                key.SetValue("UninstallString", "\"" + Path.Combine(target, UninstallFileName) + "\" /uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", 128, RegistryValueKind.DWord);
            }
        }

        private static string ReadInstalledPath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FunctionClassroom"))
                {
                    if (key == null)
                        return null;
                    return key.GetValue("InstallLocation") as string;
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool IsAppRunning()
        {
            Process[] processes = Process.GetProcessesByName("FunctionClassroom");
            return processes.Length > 0;
        }

        private static void ScheduleDirectoryRemoval(string target)
        {
            if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target))
                return;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            startInfo.Arguments = "/c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \"" + target + "\"";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            Process.Start(startInfo);
        }
    }
}
