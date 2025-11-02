using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ShutdownSchedule
{
    partial class Form1 : Form
    {
        private IContainer components = null!;
        private DateTimePicker datePickerSchedule = null!;
        private DateTimePicker timePickerSchedule = null!;
        private Panel panelHeader = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Panel panelSchedule = null!;
        private Label lblDateLabel = null!;
        private Label lblTimeLabel = null!;
        private Button btnSchedule = null!;
        private Button btnCancel = null!;
        private Button btnSetPassword = null!;
        private Button btnToggleTheme = null!;
        private FlowLayoutPanel flowActions = null!;
        private Button btnImmediateShutdown = null!;
        private Button btnRestart = null!;
        private Button btnLogOff = null!;
        private Button btnHibernate = null!;
        private Label lblScheduled = null!;
        private Label lblLogHeader = null!;
        private ListBox lstLogs = null!;
        private NotifyIcon notifyIcon = null!;
        private ContextMenuStrip contextMenuTray = null!;
        private ToolStripMenuItem menuTrayShutdown = null!;
        private ToolStripMenuItem menuTrayRestart = null!;
        private ToolStripMenuItem menuTrayCancel = null!;
        private ToolStripMenuItem menuTrayExit = null!;
        private ToolTip toolTipMain = null!;
        private System.Windows.Forms.Timer timerStatus = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pickerProxy?.Dispose();
                ReleasePickerBrush();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            panelHeader = new Panel();
            lblSubtitle = new Label();
            lblTitle = new Label();
            panelSchedule = new Panel();
            btnToggleTheme = new Button();
            btnSetPassword = new Button();
            btnCancel = new Button();
            btnSchedule = new Button();
            lblTimeLabel = new Label();
            lblDateLabel = new Label();
            timePickerSchedule = new DateTimePicker();
            datePickerSchedule = new DateTimePicker();
            flowActions = new FlowLayoutPanel();
            btnImmediateShutdown = new Button();
            btnRestart = new Button();
            btnLogOff = new Button();
            btnHibernate = new Button();
            lblScheduled = new Label();
            lblLogHeader = new Label();
            lstLogs = new ListBox();
            notifyIcon = new NotifyIcon(components);
            contextMenuTray = new ContextMenuStrip(components);
            menuTrayShutdown = new ToolStripMenuItem();
            menuTrayRestart = new ToolStripMenuItem();
            menuTrayCancel = new ToolStripMenuItem();
            menuTrayExit = new ToolStripMenuItem();
            toolTipMain = new ToolTip(components);
            timerStatus = new System.Windows.Forms.Timer(components);
            panelHeader.SuspendLayout();
            panelSchedule.SuspendLayout();
            flowActions.SuspendLayout();
            contextMenuTray.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.Controls.Add(lblSubtitle);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(24, 14, 24, 14);
            panelHeader.Size = new Size(720, 70);
            panelHeader.TabIndex = 0;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            lblSubtitle.Location = new Point(26, 40);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(258, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Schedule, monitor, and manage system power.";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Location = new Point(24, 8);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(214, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Shutdown Scheduler";
            // 
            // panelSchedule
            // 
            panelSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelSchedule.Controls.Add(btnToggleTheme);
            panelSchedule.Controls.Add(btnSetPassword);
            panelSchedule.Controls.Add(btnCancel);
            panelSchedule.Controls.Add(btnSchedule);
            panelSchedule.Controls.Add(lblTimeLabel);
            panelSchedule.Controls.Add(lblDateLabel);
            panelSchedule.Controls.Add(timePickerSchedule);
            panelSchedule.Controls.Add(datePickerSchedule);
            panelSchedule.Location = new Point(12, 80);
            panelSchedule.Name = "panelSchedule";
            panelSchedule.Padding = new Padding(18, 14, 18, 16);
            panelSchedule.Size = new Size(696, 110);
            panelSchedule.TabIndex = 1;
            // 
            // btnToggleTheme
            // 
            btnToggleTheme.Location = new System.Drawing.Point(530, 66);
            btnToggleTheme.Name = "btnToggleTheme";
            btnToggleTheme.Size = new Size(144, 34);
            btnToggleTheme.TabIndex = 7;
            btnToggleTheme.Text = "Switch Theme";
            btnToggleTheme.UseVisualStyleBackColor = true;
            btnToggleTheme.Click += btnToggleTheme_Click;
            // 
            // btnSetPassword
            // 
            btnSetPassword.Location = new System.Drawing.Point(380, 66);
            btnSetPassword.Name = "btnSetPassword";
            btnSetPassword.Size = new Size(144, 34);
            btnSetPassword.TabIndex = 6;
            btnSetPassword.Text = "Set Password";
            btnSetPassword.UseVisualStyleBackColor = true;
            btnSetPassword.Click += btnSetPassword_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(530, 26);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(144, 34);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSchedule
            // 
            btnSchedule.Location = new System.Drawing.Point(380, 26);
            btnSchedule.Name = "btnSchedule";
            btnSchedule.Size = new Size(144, 34);
            btnSchedule.TabIndex = 4;
            btnSchedule.Text = "Schedule";
            btnSchedule.UseVisualStyleBackColor = true;
            btnSchedule.Click += btnSchedule_Click;
            // 
            // lblTimeLabel
            // 
            lblTimeLabel.AutoSize = true;
            lblTimeLabel.Location = new System.Drawing.Point(196, 10);
            lblTimeLabel.Name = "lblTimeLabel";
            lblTimeLabel.Size = new Size(67, 15);
            lblTimeLabel.TabIndex = 3;
            lblTimeLabel.Text = "Select Time";
            // 
            // lblDateLabel
            // 
            lblDateLabel.AutoSize = true;
            lblDateLabel.Location = new System.Drawing.Point(18, 10);
            lblDateLabel.Name = "lblDateLabel";
            lblDateLabel.Size = new Size(68, 15);
            lblDateLabel.TabIndex = 2;
            lblDateLabel.Text = "Select Date";
            // 
            // timePickerSchedule
            // 
            timePickerSchedule.Format = DateTimePickerFormat.Time;
            timePickerSchedule.Location = new System.Drawing.Point(196, 28);
            timePickerSchedule.Name = "timePickerSchedule";
            timePickerSchedule.ShowUpDown = true;
            timePickerSchedule.Size = new Size(160, 23);
            timePickerSchedule.TabIndex = 1;
            // 
            // datePickerSchedule
            // 
            datePickerSchedule.Format = DateTimePickerFormat.Short;
            datePickerSchedule.Location = new System.Drawing.Point(18, 28);
            datePickerSchedule.Name = "datePickerSchedule";
            datePickerSchedule.Size = new Size(160, 23);
            datePickerSchedule.TabIndex = 0;
            // 
            // flowActions
            // 
            flowActions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            flowActions.Controls.Add(btnImmediateShutdown);
            flowActions.Controls.Add(btnRestart);
            flowActions.Controls.Add(btnLogOff);
            flowActions.Controls.Add(btnHibernate);
            flowActions.FlowDirection = FlowDirection.LeftToRight;
            flowActions.Location = new Point(12, 200);
            flowActions.Name = "flowActions";
            flowActions.Padding = new Padding(10, 8, 10, 8);
            flowActions.Size = new Size(696, 78);
            flowActions.TabIndex = 2;
            // 
            // btnImmediateShutdown
            // 
            btnImmediateShutdown.Location = new System.Drawing.Point(13, 11);
            btnImmediateShutdown.Margin = new Padding(3);
            btnImmediateShutdown.Name = "btnImmediateShutdown";
            btnImmediateShutdown.Size = new Size(160, 40);
            btnImmediateShutdown.TabIndex = 0;
            btnImmediateShutdown.Text = "Shutdown Now";
            btnImmediateShutdown.UseVisualStyleBackColor = true;
            btnImmediateShutdown.Click += btnImmediateShutdown_Click;
            // 
            // btnRestart
            // 
            btnRestart.Location = new System.Drawing.Point(179, 11);
            btnRestart.Margin = new Padding(3);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(160, 40);
            btnRestart.TabIndex = 1;
            btnRestart.Text = "Restart";
            btnRestart.UseVisualStyleBackColor = true;
            btnRestart.Click += btnRestart_Click;
            // 
            // btnLogOff
            // 
            btnLogOff.Location = new System.Drawing.Point(345, 11);
            btnLogOff.Margin = new Padding(3);
            btnLogOff.Name = "btnLogOff";
            btnLogOff.Size = new Size(160, 40);
            btnLogOff.TabIndex = 2;
            btnLogOff.Text = "Log Off";
            btnLogOff.UseVisualStyleBackColor = true;
            btnLogOff.Click += btnLogOff_Click;
            // 
            // btnHibernate
            // 
            btnHibernate.Location = new System.Drawing.Point(511, 11);
            btnHibernate.Margin = new Padding(3);
            btnHibernate.Name = "btnHibernate";
            btnHibernate.Size = new Size(160, 40);
            btnHibernate.TabIndex = 3;
            btnHibernate.Text = "Hibernate";
            btnHibernate.UseVisualStyleBackColor = true;
            btnHibernate.Click += btnHibernate_Click;
            // 
            // lblScheduled
            // 
            lblScheduled.AutoSize = true;
            lblScheduled.Location = new System.Drawing.Point(12, 288);
            lblScheduled.Name = "lblScheduled";
            lblScheduled.Size = new Size(133, 15);
            lblScheduled.TabIndex = 3;
            lblScheduled.Text = "No shutdown scheduled.";
            // 
            // lblLogHeader
            // 
            lblLogHeader.AutoSize = true;
            lblLogHeader.Location = new System.Drawing.Point(12, 318);
            lblLogHeader.Name = "lblLogHeader";
            lblLogHeader.Size = new Size(99, 15);
            lblLogHeader.TabIndex = 4;
            lblLogHeader.Text = "Recent Activity";
            // 
            // lstLogs
            // 
            lstLogs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstLogs.BorderStyle = BorderStyle.FixedSingle;
            lstLogs.FormattingEnabled = true;
            lstLogs.HorizontalScrollbar = true;
            lstLogs.ItemHeight = 15;
            lstLogs.Location = new System.Drawing.Point(12, 336);
            lstLogs.Name = "lstLogs";
            lstLogs.Size = new Size(696, 92);
            lstLogs.TabIndex = 5;
            lstLogs.TabStop = false;
            // 
            // notifyIcon
            // 
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipTitle = "Shutdown Scheduler";
            notifyIcon.ContextMenuStrip = contextMenuTray;
            notifyIcon.Text = "Shutdown Scheduler";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            // 
            // contextMenuTray
            // 
            contextMenuTray.Items.AddRange(new ToolStripItem[] { menuTrayShutdown, menuTrayRestart, menuTrayCancel, menuTrayExit });
            contextMenuTray.Name = "contextMenuTray";
            contextMenuTray.Size = new Size(128, 92);
            // 
            // menuTrayShutdown
            // 
            menuTrayShutdown.Name = "menuTrayShutdown";
            menuTrayShutdown.Size = new Size(127, 22);
            menuTrayShutdown.Text = "Shutdown";
            menuTrayShutdown.Click += menuTrayShutdown_Click;
            // 
            // menuTrayRestart
            // 
            menuTrayRestart.Name = "menuTrayRestart";
            menuTrayRestart.Size = new Size(127, 22);
            menuTrayRestart.Text = "Restart";
            menuTrayRestart.Click += menuTrayRestart_Click;
            // 
            // menuTrayCancel
            // 
            menuTrayCancel.Name = "menuTrayCancel";
            menuTrayCancel.Size = new Size(127, 22);
            menuTrayCancel.Text = "Cancel";
            menuTrayCancel.Click += menuTrayCancel_Click;
            // 
            // menuTrayExit
            // 
            menuTrayExit.Name = "menuTrayExit";
            menuTrayExit.Size = new Size(127, 22);
            menuTrayExit.Text = "Exit";
            menuTrayExit.Click += menuTrayExit_Click;
            // 
            // timerStatus
            // 
            timerStatus.Interval = 1000;
            timerStatus.Tick += timerStatus_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 450);
            Controls.Add(lstLogs);
            Controls.Add(lblLogHeader);
            Controls.Add(lblScheduled);
            Controls.Add(flowActions);
            Controls.Add(panelSchedule);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Shutdown Scheduler";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Resize += Form1_Resize;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelSchedule.ResumeLayout(false);
            panelSchedule.PerformLayout();
            flowActions.ResumeLayout(false);
            contextMenuTray.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
