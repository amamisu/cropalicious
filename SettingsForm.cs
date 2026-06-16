using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class SettingsForm : Form
    {
        public AppSettings Settings { get; private set; }
        
        private TextBox hotkeyTextBox = null!;
        private ComboBox hotkeyModeCombo = null!;
        private NumericUpDown widthUpDown = null!;
        private NumericUpDown heightUpDown = null!;
        private Label? dimensionsLabel;
        private Label? xLabel;
        private Label? pixelsLabel;
        private TextBox outputFolderTextBox = null!;
        private Button browseButton = null!;
        private ComboBox snapModeCombo = null!;
        private CheckBox minimizeToTrayCheckBox = null!;
        private CheckBox continuousCaptureCheckBox = null!;
        private CheckBox showNotificationsCheckBox = null!;
        private ComboBox themeCombo = null!;
        private Keys selectedHotkeyKey;
        private Keys selectedHotkeyModifiers;

        public SettingsForm(AppSettings currentSettings)
        {
            Settings = new AppSettings
            {
                HotkeyKey = currentSettings.HotkeyKey,
                HotkeyModifiers = currentSettings.HotkeyModifiers,
                CaptureWidth = currentSettings.CaptureWidth,
                CaptureHeight = currentSettings.CaptureHeight,
                OutputFolder = currentSettings.OutputFolder,
                SnapMode = currentSettings.SnapMode,
                MinimizeToTray = currentSettings.MinimizeToTray,
                ContinuousCaptureMode = currentSettings.ContinuousCaptureMode,
                ShowNotifications = currentSettings.ShowNotifications,
                HotkeyMode = currentSettings.HotkeyMode,
                FixedCaptureWidth = currentSettings.FixedCaptureWidth,
                FixedCaptureHeight = currentSettings.FixedCaptureHeight,
                Theme = currentSettings.Theme
            };

            InitializeComponent();
            Theme.Apply(this, Settings.Theme);
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Cropalicious Settings";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(ScaleByFont(15))
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            hotkeyTextBox = new TextBox
            {
                MinimumSize = new Size(ScaleByFont(230), ScaleByFont(23)),
                ReadOnly = true,
                TabStop = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            hotkeyTextBox.KeyDown += OnHotkeyKeyDown;

            hotkeyModeCombo = new ComboBox
            {
                MinimumSize = new Size(ScaleByFont(150), ScaleByFont(23)),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            hotkeyModeCombo.Items.AddRange(new[] { "Last preset", "Fixed" });
            hotkeyModeCombo.SelectedIndexChanged += OnHotkeyModeChanged;

            dimensionsLabel = CreateSettingsLabel("Fixed Size:");

            widthUpDown = new NumericUpDown
            {
                MinimumSize = new Size(ScaleByFont(80), ScaleByFont(23)),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            xLabel = CreateSettingsLabel("×");

            heightUpDown = new NumericUpDown
            {
                MinimumSize = new Size(ScaleByFont(80), ScaleByFont(23)),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            pixelsLabel = CreateSettingsLabel("pixels");

            outputFolderTextBox = new TextBox
            {
                MinimumSize = new Size(ScaleByFont(250), ScaleByFont(23)),
                ReadOnly = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            browseButton = new Button
            {
                Text = "...",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(32), ScaleByFont(23)),
                Padding = new Padding(ScaleByFont(6), 0, ScaleByFont(6), 0)
            };
            browseButton.Click += OnBrowseFolder;

            snapModeCombo = new ComboBox
            {
                MinimumSize = new Size(ScaleByFont(150), ScaleByFont(23)),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            snapModeCombo.Items.AddRange(new[] { "Snap", "Span", "Off (black fill)" });

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to system tray on close",
                AutoSize = true
            };

            continuousCaptureCheckBox = new CheckBox
            {
                Text = "Continuous capture mode",
                AutoSize = true
            };

            showNotificationsCheckBox = new CheckBox
            {
                Text = "Show notifications",
                AutoSize = true
            };

            themeCombo = new ComboBox
            {
                MinimumSize = new Size(ScaleByFont(150), ScaleByFont(23)),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeCombo.Items.AddRange(new[] { "Light", "Dark" });

            var versionLabel = new Label
            {
                Text = "Version 1.2.0.0",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = SystemColors.GrayText
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(25)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };
            okButton.Click += OnOK;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(25)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };

            var dimensionsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = CreateRowMargin()
            };
            dimensionsPanel.Controls.AddRange(new Control[] { widthUpDown, xLabel, heightUpDown, pixelsLabel });

            var folderPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Margin = CreateRowMargin()
            };
            folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderPanel.Controls.Add(outputFolderTextBox, 0, 0);
            folderPanel.Controls.Add(browseButton, 1, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, ScaleByFont(12), 0, 0)
            };
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            AddRow(layout, "Hotkey:", hotkeyTextBox);
            AddRow(layout, "Hotkey Size:", hotkeyModeCombo);
            AddRow(layout, dimensionsLabel, dimensionsPanel);
            AddRow(layout, "Output Folder:", folderPanel);
            AddRow(layout, "Snap Mode:", snapModeCombo);
            AddFullWidthRow(layout, minimizeToTrayCheckBox);
            AddFullWidthRow(layout, continuousCaptureCheckBox);
            AddFullWidthRow(layout, showNotificationsCheckBox);
            AddRow(layout, "Theme:", themeCombo);
            AddRow(layout, versionLabel, buttonPanel);

            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private int ScaleByFont(int value)
        {
            const float baseFontHeight = 15f;
            return Math.Max(1, (int)Math.Round(value * Font.Height / baseFontHeight));
        }

        private Padding CreateRowMargin()
        {
            return new Padding(0, 0, 0, ScaleByFont(10));
        }

        private Label CreateSettingsLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, ScaleByFont(12), ScaleByFont(10))
            };
        }

        private void AddRow(TableLayoutPanel layout, string labelText, Control control)
        {
            AddRow(layout, CreateSettingsLabel(labelText), control);
        }

        private void AddRow(TableLayoutPanel layout, Control label, Control control)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            label.Margin = new Padding(0, 0, ScaleByFont(12), ScaleByFont(10));
            control.Margin = CreateRowMargin();

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void AddFullWidthRow(TableLayoutPanel layout, Control control)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            control.Margin = CreateRowMargin();
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (hotkeyTextBox != null && hotkeyTextBox.Focused && TryApplyHotkey(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnHotkeyModeChanged(object? sender, EventArgs e)
        {
            bool isFixed = hotkeyModeCombo.SelectedIndex == 1;
            dimensionsLabel!.Enabled = isFixed;
            widthUpDown.Enabled = isFixed;
            xLabel!.Enabled = isFixed;
            heightUpDown.Enabled = isFixed;
            pixelsLabel!.Enabled = isFixed;
        }

        private void LoadSettings()
        {
            selectedHotkeyKey = Settings.HotkeyKey;
            selectedHotkeyModifiers = Settings.HotkeyModifiers;
            UpdateHotkeyTextBox();

            hotkeyModeCombo.SelectedIndex = (int)Settings.HotkeyMode;
            widthUpDown.Value = Settings.FixedCaptureWidth;
            heightUpDown.Value = Settings.FixedCaptureHeight;
            OnHotkeyModeChanged(null, EventArgs.Empty);

            outputFolderTextBox.Text = Settings.OutputFolder;

            snapModeCombo.SelectedIndex = (int)Settings.SnapMode;
            minimizeToTrayCheckBox.Checked = Settings.MinimizeToTray;
            continuousCaptureCheckBox.Checked = Settings.ContinuousCaptureMode;
            showNotificationsCheckBox.Checked = Settings.ShowNotifications;
            themeCombo.SelectedIndex = (int)Settings.Theme;
        }

        private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = TryApplyHotkey(e.KeyData);
        }

        private bool TryApplyHotkey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key == Keys.None || HotkeyFormatter.IsModifierKey(key))
                return true;

            selectedHotkeyKey = key;
            selectedHotkeyModifiers = keyData & Keys.Modifiers;
            UpdateHotkeyTextBox();
            return true;
        }

        private void UpdateHotkeyTextBox()
        {
            hotkeyTextBox.Text = HotkeyFormatter.Format(selectedHotkeyModifiers, selectedHotkeyKey);
        }

        private void OnBrowseFolder(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Settings.OutputFolder,
                Description = "Select output folder for screenshots"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnOK(object? sender, EventArgs e)
        {
            try
            {
                Settings.HotkeyModifiers = selectedHotkeyModifiers;
                Settings.HotkeyKey = selectedHotkeyKey;

                Settings.HotkeyMode = (HotkeyMode)hotkeyModeCombo.SelectedIndex;
                Settings.FixedCaptureWidth = (int)widthUpDown.Value;
                Settings.FixedCaptureHeight = (int)heightUpDown.Value;
                Settings.OutputFolder = outputFolderTextBox.Text;
                Settings.SnapMode = (SnapMode)snapModeCombo.SelectedIndex;
                Settings.MinimizeToTray = minimizeToTrayCheckBox.Checked;
                Settings.ContinuousCaptureMode = continuousCaptureCheckBox.Checked;
                Settings.ShowNotifications = showNotificationsCheckBox.Checked;
                Settings.Theme = (AppTheme)themeCombo.SelectedIndex;

                if (!Directory.Exists(Settings.OutputFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(Settings.OutputFolder);
                    }
                    catch
                    {
                        MessageBox.Show("Invalid output folder path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
