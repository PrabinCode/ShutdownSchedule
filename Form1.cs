using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShutdownSchedule
{
    public partial class Form1 : Form
    {
        // Persistent storage paths and synchronization primitives.
        private readonly string _appDataDirectory;
        private readonly string _settingsFilePath;
        private readonly string _logFilePath;
        private readonly SemaphoreSlim _logLock = new(1, 1);

        // Runtime state for password protection, theming, and scheduling.
        private string? _passwordHash;
        private string? _passwordSalt;
        private bool _isDarkMode;
        private bool _allowClose;
        private DateTime? _scheduledShutdownTime;

        // Native constants for theming tweaks.
        private const int DwmaUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmaUseImmersiveDarkMode = 20;
        private const int WmCtlColorStatic = 0x0018;
        private const int WmCtlColorEdit = 0x0133;
        private const int DtmGetMonthCal = 0x1000 + 30;
        private const int McmSetColor = 0x1000 + 10;
        private const int McscBackground = 0;
        private const int McscText = 1;
        private const int McscTitleBack = 2;
        private const int McscTitleText = 3;
        private const int McscMonthBack = 4;
        private const int McscTrailingText = 5;

        private IntPtr _pickerBrush = IntPtr.Zero;
        private PickerColorProxy? _pickerProxy;

        public Form1()
        {
            InitializeComponent();

            notifyIcon.Icon = SystemIcons.Shield;
            notifyIcon.Visible = true;

            toolTipMain.AutoPopDelay = 6000;
            toolTipMain.InitialDelay = 400;
            toolTipMain.ReshowDelay = 100;
            toolTipMain.ShowAlways = true;
            ConfigureTooltips();

            _appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShutdownSchedule");
            _settingsFilePath = Path.Combine(_appDataDirectory, "settings.json");
            _logFilePath = Path.Combine(_appDataDirectory, "activity.log");
            Directory.CreateDirectory(_appDataDirectory);

            var initialTarget = DateTime.Now.AddMinutes(10);
            datePickerSchedule.MinDate = DateTime.Today;
            datePickerSchedule.Value = initialTarget.Date;
            timePickerSchedule.Format = DateTimePickerFormat.Time;
            timePickerSchedule.Value = DateTime.Today.Add(initialTarget.TimeOfDay);

            datePickerSchedule.DropDown += datePickerSchedule_DropDown;

            LoadSettings();
            ApplyTheme(_isDarkMode);
            UpdateScheduledStatusLabel();
            timerStatus.Start();

            _pickerProxy = new PickerColorProxy(panelSchedule, this);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplySystemTitleBarTheme();
            ApplyPickerTheme(datePickerSchedule);
            ApplyPickerTheme(timePickerSchedule);
            UpdatePickerBrush();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await UpdateLogListAsync().ConfigureAwait(true);
        }

        private async void btnSchedule_Click(object sender, EventArgs e)
        {
            var scheduledDate = datePickerSchedule.Value.Date;
            var scheduledTime = timePickerSchedule.Value.TimeOfDay;
            var target = scheduledDate.Add(scheduledTime);
            if (target <= DateTime.Now)
            {
                MessageBox.Show(this, "Please select a future time for shutdown.", "Invalid Time", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var seconds = (int)Math.Ceiling((target - DateTime.Now).TotalSeconds);
            seconds = Math.Clamp(seconds, 0, 315360000);

            var result = await ExecuteShutdownCommandAsync($"/s /t {seconds}").ConfigureAwait(true);
            if (!result.Success)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            _scheduledShutdownTime = target;
            await LogEventAsync($"Scheduled shutdown for {target:yyyy-MM-dd HH:mm:ss} ({seconds} seconds)").ConfigureAwait(true);
            await UpdateLogListAsync().ConfigureAwait(true);
            UpdateScheduledStatusLabel();
            ShowTrayNotification("Shutdown Scheduled", $"System will shut down at {target:HH:mm:ss}.", ToolTipIcon.Info);
        }

        private async void btnCancel_Click(object sender, EventArgs e)
        {
            await CancelScheduledShutdownAsync().ConfigureAwait(true);
        }

        private async void btnImmediateShutdown_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/s /t 0", "Shutdown initiated immediately.", "Shutdown Now", ToolTipIcon.Warning, clearSchedule: true).ConfigureAwait(true);
        }

        private async void btnRestart_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/r /t 0", "Restart command executed.", "Restarting", ToolTipIcon.Warning, clearSchedule: true).ConfigureAwait(true);
        }

        private async void btnLogOff_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/l", "Log off command executed.", "Logging Off", ToolTipIcon.Info, clearSchedule: true).ConfigureAwait(true);
        }

        private async void btnHibernate_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/h", "Hibernate command executed.", "Hibernating", ToolTipIcon.Info, clearSchedule: true).ConfigureAwait(true);
        }

        private void btnToggleTheme_Click(object sender, EventArgs e)
        {
            ApplyTheme(!_isDarkMode);
            SaveSettings();
        }

        private void btnSetPassword_Click(object sender, EventArgs e)
        {
            SetPassword();
        }

        private async void menuTrayShutdown_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/s /t 0", "Shutdown command executed from tray.", "Shutdown Now", ToolTipIcon.Warning, clearSchedule: true).ConfigureAwait(true);
        }

        private async void menuTrayRestart_Click(object sender, EventArgs e)
        {
            await HandleCommandAsync("/r /t 0", "Restart command executed from tray.", "Restarting", ToolTipIcon.Warning, clearSchedule: true).ConfigureAwait(true);
        }

        private async void menuTrayCancel_Click(object sender, EventArgs e)
        {
            await CancelScheduledShutdownAsync().ConfigureAwait(true);
        }

        private void menuTrayExit_Click(object sender, EventArgs e)
        {
            _allowClose = true;
            notifyIcon.Visible = false;
            Close();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                ShowTrayNotification("Shutdown Scheduler", "Application continues running in the system tray.", ToolTipIcon.Info);
            }
            else
            {
                notifyIcon.Visible = false;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowTrayNotification("Shutdown Scheduler", "Application minimized to system tray.", ToolTipIcon.Info);
            }
        }

        private void timerStatus_Tick(object sender, EventArgs e)
        {
            if (datePickerSchedule.MinDate < DateTime.Today)
            {
                datePickerSchedule.MinDate = DateTime.Today;
            }

            UpdateScheduledStatusLabel();
        }

        private async Task CancelScheduledShutdownAsync()
        {
            if (string.IsNullOrEmpty(_passwordHash) || string.IsNullOrEmpty(_passwordSalt))
            {
                MessageBox.Show(this, "Please set a password before attempting to cancel.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var password = PromptForPassword("Enter Password", requireConfirmation: false, isNewPassword: false);
            if (password is null)
            {
                return;
            }

            if (!VerifyPassword(password))
            {
                MessageBox.Show(this, "Incorrect password. Cancellation aborted.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = await ExecuteShutdownCommandAsync("/a").ConfigureAwait(true);
            if (!result.Success)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            _scheduledShutdownTime = null;
            await LogEventAsync("Scheduled shutdown canceled.").ConfigureAwait(true);
            await UpdateLogListAsync().ConfigureAwait(true);
            UpdateScheduledStatusLabel();
            ShowTrayNotification("Shutdown Canceled", "The pending shutdown has been canceled.", ToolTipIcon.Info);
        }

        private async Task HandleCommandAsync(string arguments, string logMessage, string balloonTitle, ToolTipIcon icon, bool clearSchedule)
        {
            var result = await ExecuteShutdownCommandAsync(arguments).ConfigureAwait(true);
            if (!result.Success)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            if (clearSchedule)
            {
                _scheduledShutdownTime = null;
                UpdateScheduledStatusLabel();
            }

            await LogEventAsync(logMessage).ConfigureAwait(true);
            await UpdateLogListAsync().ConfigureAwait(true);
            ShowTrayNotification(balloonTitle, logMessage, icon);
        }

        private async Task<(bool Success, string ErrorMessage)> ExecuteShutdownCommandAsync(string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                var errorTask = process.StandardError.ReadToEndAsync();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync().ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    return (true, string.Empty);
                }

                var error = await errorTask.ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(error))
                {
                    error = await outputTask.ConfigureAwait(false);
                }

                return (false, string.IsNullOrWhiteSpace(error) ? $"Command exited with code {process.ExitCode}." : error.Trim());
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task LogEventAsync(string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            await _logLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(_appDataDirectory);
                await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine).ConfigureAwait(false);
            }
            finally
            {
                _logLock.Release();
            }
        }

        private async Task UpdateLogListAsync()
        {
            string[] lines;
            await _logLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    lstLogs.Items.Clear();
                    return;
                }

                lines = await File.ReadAllLinesAsync(_logFilePath).ConfigureAwait(false);
            }
            finally
            {
                _logLock.Release();
            }

            var recentEntries = lines.Reverse().Take(5).Reverse().ToArray();
            lstLogs.BeginUpdate();
            try
            {
                lstLogs.Items.Clear();
                foreach (var entry in recentEntries)
                {
                    lstLogs.Items.Add(entry);
                }
            }
            finally
            {
                lstLogs.EndUpdate();
            }
        }

        private void UpdateScheduledStatusLabel()
        {
            if (_scheduledShutdownTime.HasValue)
            {
                var remaining = _scheduledShutdownTime.Value - DateTime.Now;
                var remainingText = remaining > TimeSpan.Zero ? $" ({remaining:hh\\:mm\\:ss} remaining)" : " (pending)";
                lblScheduled.Text = $"Shutdown scheduled for {_scheduledShutdownTime:yyyy-MM-dd HH:mm:ss}{remainingText}";
                UpdateNotifyIconText("Shutdown scheduled");
            }
            else
            {
                lblScheduled.Text = "No shutdown scheduled.";
                UpdateNotifyIconText("No shutdown scheduled");
            }
        }

        private void UpdateNotifyIconText(string status)
        {
            var text = $"Shutdown Scheduler - {status}";
            notifyIcon.Text = text.Length <= 63 ? text : text.Substring(0, 63);
        }

        private void ShowTrayNotification(string title, string content, ToolTipIcon icon)
        {
            notifyIcon.BalloonTipIcon = icon;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = content;
            notifyIcon.ShowBalloonTip(3000);
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void SetPassword()
        {
            while (true)
            {
                var password = PromptForPassword("Set Password", requireConfirmation: true, isNewPassword: true);
                if (password is null)
                {
                    return;
                }

                try
                {
                    var salt = RandomNumberGenerator.GetBytes(16);
                    using var derive = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                    var hash = derive.GetBytes(32);
                    _passwordSalt = Convert.ToBase64String(salt);
                    _passwordHash = Convert.ToBase64String(hash);
                    SaveSettings();
                    MessageBox.Show(this, "Password saved successfully.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to set password: {ex.Message}");
                }
            }
        }

        private string? PromptForPassword(string title, bool requireConfirmation, bool isNewPassword)
        {
            while (true)
            {
                var baseHeight = requireConfirmation ? 200 : 160;
                using var prompt = new Form
                {
                    Text = title,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowInTaskbar = false,
                    ClientSize = new Size(340, baseHeight)
                };

                var lblPassword = new Label
                {
                    Text = isNewPassword ? "Enter new password:" : "Enter password:",
                    AutoSize = true,
                    Location = new Point(12, 15)
                };

                var txtPassword = new TextBox
                {
                    UseSystemPasswordChar = true,
                    Location = new Point(15, 40),
                    Width = prompt.ClientSize.Width - 30,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                TextBox? txtConfirm = null;
                Label? lblConfirm = null;

                if (requireConfirmation)
                {
                    lblConfirm = new Label
                    {
                        Text = "Confirm password:",
                        AutoSize = true,
                        Location = new Point(12, 80)
                    };

                    txtConfirm = new TextBox
                    {
                        UseSystemPasswordChar = true,
                        Location = new Point(15, 100),
                        Width = prompt.ClientSize.Width - 30,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                    };
                }

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 70,
                    Padding = new Padding(0, 0, 15, 15)
                };
                const int buttonWidth = 100;
                const int buttonHeight = 34;
                var buttonFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
                var btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Width = buttonWidth,
                    Height = buttonHeight,
                    Font = buttonFont,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Width = buttonWidth,
                    Height = buttonHeight,
                    Font = buttonFont,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };

                void PositionButtons()
                {
                    var bottom = buttonPanel.ClientSize.Height - btnOk.Height - 15;
                    btnCancel.Location = new Point(buttonPanel.ClientSize.Width - btnCancel.Width - 15, bottom);
                    btnOk.Location = new Point(btnCancel.Left - btnOk.Width - 10, bottom);
                }

                buttonPanel.SizeChanged += (_, _) => PositionButtons();
                buttonPanel.Controls.Add(btnOk);
                buttonPanel.Controls.Add(btnCancel);
                PositionButtons();

                prompt.Controls.Add(lblPassword);
                prompt.Controls.Add(txtPassword);
                if (requireConfirmation && txtConfirm is not null && lblConfirm is not null)
                {
                    prompt.Controls.Add(lblConfirm);
                    prompt.Controls.Add(txtConfirm);
                }

                prompt.Controls.Add(buttonPanel);
                prompt.AcceptButton = btnOk;
                prompt.CancelButton = btnCancel;

                ApplyThemeToDialog(prompt, txtPassword, txtConfirm, btnOk, btnCancel, lblPassword, lblConfirm);

                var dialogResult = prompt.ShowDialog(this);
                var password = txtPassword.Text;
                var confirm = txtConfirm?.Text;

                if (dialogResult != DialogResult.OK)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(this, "Password cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                if (requireConfirmation && password != confirm)
                {
                    MessageBox.Show(this, "Passwords do not match.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                return password;
            }
        }

        private void ApplyTheme(bool isDark)
        {
            _isDarkMode = isDark;
            var background = isDark ? Color.FromArgb(24, 24, 24) : Color.FromArgb(244, 244, 248);
            var foreground = isDark ? Color.Gainsboro : Color.FromArgb(32, 32, 32);
            var surface = isDark ? Color.FromArgb(37, 37, 38) : Color.White;
            var surfaceAlt = isDark ? Color.FromArgb(32, 32, 33) : Color.FromArgb(236, 236, 239);
            var accent = isDark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(224, 224, 228);
            var accentText = isDark ? Color.White : Color.FromArgb(30, 30, 30);

            BackColor = background;
            ForeColor = foreground;
            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, background, foreground, isDark);
            }

            panelHeader.BackColor = accent;
            panelHeader.ForeColor = accentText;
            lblTitle.ForeColor = accentText;
            lblSubtitle.ForeColor = accentText;

            panelSchedule.BackColor = surface;
            panelSchedule.ForeColor = foreground;

            flowActions.BackColor = surfaceAlt;
            flowActions.ForeColor = foreground;

            contextMenuTray.BackColor = isDark ? Color.FromArgb(45, 45, 48) : SystemColors.ControlLightLight;
            contextMenuTray.ForeColor = foreground;
            btnToggleTheme.Text = isDark ? "Switch to Light" : "Switch to Dark";

            ApplyPickerTheme(datePickerSchedule);
            ApplyPickerTheme(timePickerSchedule);
            ApplySystemTitleBarTheme();
            UpdatePickerBrush();
            datePickerSchedule.Invalidate();
            timePickerSchedule.Invalidate();
        }

        private void ApplyThemeToDialog(Form dialog, params Control?[] controls)
        {
            var background = _isDarkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            var foreground = _isDarkMode ? Color.Gainsboro : SystemColors.ControlText;
            dialog.BackColor = background;
            dialog.ForeColor = foreground;
            foreach (var control in controls)
            {
                if (control is null)
                {
                    continue;
                }

                ApplyThemeToControl(control, background, foreground, _isDarkMode);
            }
        }

        private void ApplyThemeToControl(Control control, Color background, Color foreground, bool isDark)
        {
            switch (control)
            {
                case Button button:
                    button.BackColor = isDark ? Color.FromArgb(63, 63, 70) : SystemColors.Control;
                    button.ForeColor = foreground;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.BorderColor = isDark ? Color.FromArgb(90, 90, 90) : SystemColors.ControlDark;
                    button.FlatAppearance.MouseOverBackColor = isDark ? Color.FromArgb(72, 72, 80) : Color.FromArgb(224, 224, 224);
                    button.FlatAppearance.MouseDownBackColor = isDark ? Color.FromArgb(82, 82, 90) : Color.FromArgb(210, 210, 210);
                    button.Cursor = Cursors.Hand;
                    button.Padding = new Padding(6, 4, 6, 4);
                    button.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
                    button.UseVisualStyleBackColor = false;
                    break;
                case ListBox listBox:
                    listBox.BackColor = isDark ? Color.FromArgb(24, 24, 24) : SystemColors.Window;
                    listBox.ForeColor = foreground;
                    break;
                case Label label:
                    label.ForeColor = foreground;
                    break;
                case DateTimePicker picker:
                    picker.CalendarForeColor = foreground;
                    picker.CalendarMonthBackground = isDark ? Color.FromArgb(37, 37, 38) : SystemColors.Window;
                    picker.CalendarTitleBackColor = isDark ? Color.FromArgb(63, 63, 70) : SystemColors.Highlight;
                    picker.CalendarTitleForeColor = isDark ? Color.White : SystemColors.HighlightText;
                    picker.CalendarTrailingForeColor = isDark ? Color.DimGray : SystemColors.GrayText;
                    picker.BackColor = isDark ? Color.FromArgb(37, 37, 38) : SystemColors.Window;
                    picker.ForeColor = foreground;
                    break;
                default:
                    control.BackColor = background;
                    control.ForeColor = foreground;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, background, foreground, isDark);
            }
        }

        private void ConfigureTooltips()
        {
            toolTipMain.SetToolTip(datePickerSchedule, "Pick the day for the scheduled shutdown.");
            toolTipMain.SetToolTip(timePickerSchedule, "Pick the time to shut down the system.");
            toolTipMain.SetToolTip(btnSchedule, "Queue a shutdown at the selected date and time.");
            toolTipMain.SetToolTip(btnCancel, "Cancel the pending shutdown (requires password).");
            toolTipMain.SetToolTip(btnSetPassword, "Set or update the password required to cancel shutdowns.");
            toolTipMain.SetToolTip(btnToggleTheme, "Switch between light and dark appearances.");
            toolTipMain.SetToolTip(btnImmediateShutdown, "Shut down the computer immediately.");
            toolTipMain.SetToolTip(btnRestart, "Restart the computer right away.");
            toolTipMain.SetToolTip(btnLogOff, "Log off the current Windows session.");
            toolTipMain.SetToolTip(btnHibernate, "Hibernate the system immediately.");
            toolTipMain.SetToolTip(lstLogs, "View recent shutdown and power events.");
        }

        private bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(_passwordHash) || string.IsNullOrEmpty(_passwordSalt))
            {
                return false;
            }

            try
            {
                var saltBytes = Convert.FromBase64String(_passwordSalt);
                var expectedHash = Convert.FromBase64String(_passwordHash);
                using var derive = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                var actualHash = derive.GetBytes(expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch
            {
                return false;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings is null)
                {
                    return;
                }

                _passwordHash = settings.PasswordHash;
                _passwordSalt = settings.PasswordSalt;
                _isDarkMode = settings.IsDarkMode;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new UserSettings
                {
                    PasswordHash = _passwordHash,
                    PasswordSalt = _passwordSalt,
                    IsDarkMode = _isDarkMode
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        private void ShowError(string error)
        {
            MessageBox.Show(this, error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void datePickerSchedule_DropDown(object? sender, EventArgs e)
        {
            ApplyMonthCalendarTheme(datePickerSchedule);
        }

        private void ApplyMonthCalendarTheme(DateTimePicker picker)
        {
            if (!picker.IsHandleCreated)
            {
                return;
            }

            var monthHandle = SendMessage(picker.Handle, DtmGetMonthCal, IntPtr.Zero, IntPtr.Zero);
            if (monthHandle == IntPtr.Zero)
            {
                return;
            }

            var background = ColorTranslator.ToWin32(_isDarkMode ? Color.FromArgb(45, 45, 48) : Color.White);
            var textColor = ColorTranslator.ToWin32(_isDarkMode ? Color.Gainsboro : Color.FromArgb(30, 30, 30));
            var titleBack = ColorTranslator.ToWin32(_isDarkMode ? Color.FromArgb(63, 63, 70) : SystemColors.Highlight);
            var titleText = ColorTranslator.ToWin32(_isDarkMode ? Color.White : SystemColors.HighlightText);
            var monthBack = ColorTranslator.ToWin32(_isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White);
            var trailingText = ColorTranslator.ToWin32(_isDarkMode ? Color.DimGray : SystemColors.GrayText);

            SendMessage(monthHandle, McmSetColor, (IntPtr)McscBackground, (IntPtr)background);
            SendMessage(monthHandle, McmSetColor, (IntPtr)McscText, (IntPtr)textColor);
            SendMessage(monthHandle, McmSetColor, (IntPtr)McscTitleBack, (IntPtr)titleBack);
            SendMessage(monthHandle, McmSetColor, (IntPtr)McscTitleText, (IntPtr)titleText);
            SendMessage(monthHandle, McmSetColor, (IntPtr)McscMonthBack, (IntPtr)monthBack);
            SendMessage(monthHandle, McmSetColor, (IntPtr)McscTrailingText, (IntPtr)trailingText);
            _ = SetWindowTheme(monthHandle, _isDarkMode ? "DarkMode_Explorer" : "Explorer", null);
        }

        private void UpdatePickerBrush()
        {
            var targetColor = ColorTranslator.ToWin32(_isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White);
            if (_pickerBrush != IntPtr.Zero)
            {
                DeleteObject(_pickerBrush);
            }

            _pickerBrush = CreateSolidBrush(targetColor);
        }

        private void ReleasePickerBrush()
        {
            if (_pickerBrush != IntPtr.Zero)
            {
                DeleteObject(_pickerBrush);
                _pickerBrush = IntPtr.Zero;
            }
        }

        private void ApplyPickerTheme(DateTimePicker picker)
        {
            if (picker is null)
            {
                return;
            }

            if (picker.IsHandleCreated)
            {
                var themeName = _isDarkMode ? "DarkMode_Explorer" : "Explorer";
                _ = SetWindowTheme(picker.Handle, themeName, null);
            }

            picker.CalendarForeColor = _isDarkMode ? Color.Gainsboro : Color.FromArgb(30, 30, 30);
            picker.CalendarFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            picker.CalendarMonthBackground = _isDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
            picker.CalendarTitleBackColor = _isDarkMode ? Color.FromArgb(63, 63, 70) : SystemColors.Highlight;
            picker.CalendarTitleForeColor = _isDarkMode ? Color.White : SystemColors.HighlightText;
            picker.CalendarTrailingForeColor = _isDarkMode ? Color.DimGray : SystemColors.GrayText;
            picker.ForeColor = _isDarkMode ? Color.Gainsboro : Color.FromArgb(30, 30, 30);
            picker.BackColor = _isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White;
        }

        private void ApplySystemTitleBarTheme()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            var useDark = _isDarkMode ? 1 : 0;
            var attribute = Environment.OSVersion.Version >= new Version(10, 0, 18985)
                ? DwmaUseImmersiveDarkMode
                : DwmaUseImmersiveDarkModeBefore20H1;

            _ = DwmSetWindowAttribute(Handle, attribute, ref useDark, sizeof(int));
        }

        protected override void WndProc(ref Message m)
        {
            if ((m.Msg == WmCtlColorStatic || m.Msg == WmCtlColorEdit) && _pickerBrush != IntPtr.Zero)
            {
                var target = m.LParam;
                if (IsHandleForPicker(target, datePickerSchedule) || IsHandleForPicker(target, timePickerSchedule))
                {
                    var hdc = m.WParam;
                    var textColor = ColorTranslator.ToWin32(_isDarkMode ? Color.Gainsboro : Color.FromArgb(30, 30, 30));
                    var backColor = ColorTranslator.ToWin32(_isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White);
                    SetTextColor(hdc, textColor);
                    SetBkColor(hdc, backColor);
                    m.Result = _pickerBrush;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private static bool IsHandleForPicker(IntPtr candidateHandle, DateTimePicker picker)
        {
            if (picker is null || candidateHandle == IntPtr.Zero)
            {
                return false;
            }

            var pickerHandle = picker.Handle;
            var current = candidateHandle;
            while (current != IntPtr.Zero)
            {
                if (current == pickerHandle)
                {
                    return true;
                }

                current = GetParent(current);
            }

            return false;
        }

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern int SetBkColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private sealed class UserSettings
        {
            public string? PasswordHash { get; set; }
            public string? PasswordSalt { get; set; }
            public bool IsDarkMode { get; set; }
        }

        private sealed class PickerColorProxy : NativeWindow, IDisposable
        {
            private readonly Control _parent;
            private readonly Form1 _owner;

            public PickerColorProxy(Control parent, Form1 owner)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));

                _parent.HandleCreated += OnParentHandleCreated;
                _parent.HandleDestroyed += OnParentHandleDestroyed;

                if (_parent.IsHandleCreated)
                {
                    AssignHandle(_parent.Handle);
                }
            }

            protected override void WndProc(ref Message m)
            {
                if ((_owner._pickerBrush != IntPtr.Zero) && (m.Msg == WmCtlColorStatic || m.Msg == WmCtlColorEdit))
                {
                    if (IsHandleForPicker(m.LParam, _owner.datePickerSchedule) || IsHandleForPicker(m.LParam, _owner.timePickerSchedule))
                    {
                        var textColor = ColorTranslator.ToWin32(_owner._isDarkMode ? Color.Gainsboro : Color.FromArgb(30, 30, 30));
                        var backColor = ColorTranslator.ToWin32(_owner._isDarkMode ? Color.FromArgb(37, 37, 38) : Color.White);
                        SetTextColor(m.WParam, textColor);
                        SetBkColor(m.WParam, backColor);
                        m.Result = _owner._pickerBrush;
                        return;
                    }
                }

                base.WndProc(ref m);
            }

            private void OnParentHandleCreated(object? sender, EventArgs e)
            {
                AssignHandle(_parent.Handle);
            }

            private void OnParentHandleDestroyed(object? sender, EventArgs e)
            {
                ReleaseHandle();
            }

            public void Dispose()
            {
                ReleaseHandle();
                _parent.HandleCreated -= OnParentHandleCreated;
                _parent.HandleDestroyed -= OnParentHandleDestroyed;
            }
        }
    }
}
