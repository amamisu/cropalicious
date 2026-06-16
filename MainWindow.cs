using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class MainWindow : Form
    {
        private readonly AppSettings settings;
        private OverlayWindow? overlayWindow;
        private TableLayoutPanel? presetsTable;
        private TableLayoutPanel? customTable;
        private Panel? customScrollPanel;
        private Label? outputLabel;
        private Label? hotkeyLabel;
        private ToolTip? toolTip;
        private bool boundsRestored;

        private const int DpiBase = 96;
        private const int CaptureGridColumns = 3;
        private const int CardLogicalWidth = 180;
        private const int CardLogicalHeight = 80;
        private const int DeleteButtonLogicalSize = 22;
        private const int DeleteButtonLogicalInset = 10;
        private const int CustomGridMaxLogicalHeight = 260;

        private static readonly Color PresetCardColor = Color.FromArgb(50, 180, 200);
        private static readonly Color AddCardColor = Color.FromArgb(160, 200, 50);
        private static readonly Color CustomCardColor = Color.FromArgb(200, 60, 120);
        private static readonly Color DeleteButtonColor = Color.FromArgb(180, 50, 50);

        private readonly CapturePreset[] presets =
        {
            new(1024, 1024, "1024×1024\nSquare"),
            new(1216, 832, "1216×832\nWide"),
            new(832, 1216, "832×1216\nTall"),
            new(1344, 768, "1344×768\nUltrawide"),
            new(768, 1344, "768×1344\nUltra Tall")
        };

        public event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;
        public event EventHandler? SettingsDialogOpening;
        public event EventHandler? SettingsDialogClosed;
        public event EventHandler? SettingsChanged;

        public MainWindow(AppSettings settings)
        {
            this.settings = settings;
            InitializeComponent();

            FormClosing += OnFormClosing;
            SizeChanged += OnSizeChanged;
            LocationChanged += OnLocationChanged;

            TopMost = settings.StayOnTop;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (overlayWindow != null)
                {
                    overlayWindow.ScreenshotTaken -= OnOverlayScreenshotTaken;
                    overlayWindow.Dispose();
                    overlayWindow = null;
                }

                toolTip?.Dispose();
                Icon?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyMinimumWindowSize();
            RestoreWindowBounds();
            UpdateCaptureCardLayout();
            boundsRestored = true;
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplyMinimumWindowSize();
            UpdateCaptureCardLayout();
        }

        private void InitializeComponent()
        {
            Text = "Cropalicious";
            AutoScaleDimensions = new SizeF(DpiBase, DpiBase);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true;
            Icon = CropaliciousApp.CreateAppIcon(32);
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.Sizable;
            Padding = new Padding(20);

            var mainLayout = CreateMainLayout();
            var hintLabel = CreateHintLabel();

            presetsTable = CreateCaptureGrid(new Padding(0, 0, 0, 20));
            CreatePresetCards();

            customTable = CreateCaptureGrid(Padding.Empty);
            customScrollPanel = CreateCustomScrollPanel();
            customScrollPanel.Controls.Add(customTable);
            RefreshCustomCards();

            mainLayout.Controls.Add(hintLabel, 0, 0);
            mainLayout.Controls.Add(presetsTable, 0, 1);
            mainLayout.Controls.Add(customScrollPanel, 0, 2);
            mainLayout.Controls.Add(CreateBottomPanel(), 0, 3);

            Controls.Add(mainLayout);
            ApplyTheme();
            ApplyMinimumWindowSize();
            UpdateCaptureCardLayout();
        }

        private TableLayoutPanel CreateMainLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            return layout;
        }

        private Label CreateHintLabel()
        {
            return new Label
            {
                Text = "Left click to capture, right click or ESC to cancel",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 15),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        private TableLayoutPanel CreateCaptureGrid(Padding margin)
        {
            var grid = new TableLayoutPanel
            {
                ColumnCount = CaptureGridColumns,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = margin
            };

            ConfigureCaptureGridColumns(grid);
            return grid;
        }

        private Panel CreateCustomScrollPanel()
        {
            var panel = new Panel
            {
                AutoScroll = true,
                AutoSize = false,
                MaximumSize = new Size(0, ScaleLogical(CustomGridMaxLogicalHeight)),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0, 0, 20, 0)
            };

            panel.HorizontalScroll.Enabled = false;
            panel.HorizontalScroll.Visible = false;
            return panel;
        }

        private void CreatePresetCards()
        {
            ConfigureCaptureGridRows(presetsTable!, 2);
            bool isDark = settings.Theme == AppTheme.Dark;

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var button = CreateCaptureCard(preset.Name, PresetCardColor, (preset.Width, preset.Height), OnPresetButtonClick, isDark);
                presetsTable!.Controls.Add(button, i % CaptureGridColumns, i / CaptureGridColumns);
            }

            var addButton = CreateCaptureCard("+\nAdd Custom", AddCardColor, null, OnAddCustomClick, isDark);
            presetsTable!.Controls.Add(addButton, 2, 1);
        }

        private void RefreshCustomCards()
        {
            customTable!.SuspendLayout();

            try
            {
                ClearControls(customTable.Controls);

                int rows = (settings.CustomSizes.Count + CaptureGridColumns - 1) / CaptureGridColumns;
                ConfigureCaptureGridRows(customTable, rows);
                customTable.Visible = settings.CustomSizes.Count > 0;

                if (settings.CustomSizes.Count > 0)
                {
                    bool isDark = settings.Theme == AppTheme.Dark;

                    for (int i = 0; i < settings.CustomSizes.Count; i++)
                    {
                        var custom = settings.CustomSizes[i];
                        var text = $"{custom.Width}×{custom.Height}\n{custom.Name}";
                        var button = CreateCaptureCard(text, CustomCardColor, (custom.Width, custom.Height), OnPresetButtonClick, isDark);
                        button.Controls.Add(CreateDeleteButton(i));
                        customTable.Controls.Add(button, i % CaptureGridColumns, i / CaptureGridColumns);
                    }
                }
            }
            finally
            {
                customTable.ResumeLayout(true);
            }

            UpdateCaptureCardLayout();
        }

        private GlowButton CreateCaptureCard(string text, Color color, object? tag, EventHandler click, bool isDark)
        {
            var size = GetCurrentCardSize();
            var button = new GlowButton
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                AutoSize = false,
                Size = size,
                MinimumSize = size,
                MaximumSize = size,
                Margin = Padding.Empty,
                Tag = tag,
                UseVisualStyleBackColor = false
            };

            button.SetGlow(isDark);
            button.Click += click;
            return button;
        }

        private Button CreateDeleteButton(int index)
        {
            var size = ScaleLogical(DeleteButtonLogicalSize);
            var button = new Button
            {
                Text = "×",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = DeleteButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(size, size),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = index,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += OnDeleteCustomClick;
            return button;
        }

        private void ConfigureCaptureGridRows(TableLayoutPanel grid, int rows)
        {
            grid.RowStyles.Clear();
            grid.RowCount = rows;

            for (int i = 0; i < rows; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        private static void ConfigureCaptureGridColumns(TableLayoutPanel grid)
        {
            grid.ColumnStyles.Clear();

            for (int i = 0; i < CaptureGridColumns; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / CaptureGridColumns));
        }

        private void UpdateCaptureCardLayout()
        {
            var cardSize = GetCurrentCardSize();
            bool isDark = settings.Theme == AppTheme.Dark;

            foreach (var card in CaptureCards())
            {
                card.AutoSize = false;
                card.Size = cardSize;
                card.MinimumSize = cardSize;
                card.MaximumSize = cardSize;
                card.SetGlow(isDark);
                PositionDeleteButtons(card);
                card.Invalidate();
            }

            UpdateCustomGridHost();
        }

        private IEnumerable<GlowButton> CaptureCards()
        {
            if (presetsTable != null)
            {
                foreach (var card in presetsTable.Controls.OfType<GlowButton>())
                    yield return card;
            }

            if (customTable != null)
            {
                foreach (var card in customTable.Controls.OfType<GlowButton>())
                    yield return card;
            }
        }

        private void PositionDeleteButtons(GlowButton card)
        {
            var size = ScaleLogical(DeleteButtonLogicalSize);
            var inset = ScaleLogical(DeleteButtonLogicalInset);

            foreach (var button in card.Controls.OfType<Button>())
            {
                button.Size = new Size(size, size);
                button.Location = new Point(card.Width - size - inset, 0);
            }
        }

        private void UpdateCustomGridHost()
        {
            if (customScrollPanel == null || customTable == null)
                return;

            bool hasCustomSizes = settings.CustomSizes.Count > 0;
            customTable.Visible = hasCustomSizes;
            customScrollPanel.Visible = hasCustomSizes;

            if (!hasCustomSizes)
            {
                customTable.MinimumSize = Size.Empty;
                customScrollPanel.MinimumSize = Size.Empty;
                customScrollPanel.Size = Size.Empty;
                return;
            }

            customTable.PerformLayout();
            var tableSize = customTable.GetPreferredSize(Size.Empty);
            var maxHeight = ScaleLogical(CustomGridMaxLogicalHeight);
            var panelHeight = Math.Min(tableSize.Height, maxHeight);
            var needsVerticalScroll = tableSize.Height > maxHeight;
            var panelWidth = tableSize.Width + customScrollPanel.Padding.Horizontal;

            if (needsVerticalScroll)
                panelWidth += SystemInformation.VerticalScrollBarWidth;

            customScrollPanel.MaximumSize = new Size(0, maxHeight);
            customTable.MinimumSize = tableSize;
            customScrollPanel.Size = new Size(panelWidth, panelHeight);
            customScrollPanel.MinimumSize = new Size(panelWidth, panelHeight);
            customScrollPanel.PerformLayout();
            customScrollPanel.Invalidate(true);
        }

        private Size GetCurrentCardSize()
        {
            if (!IsHandleCreated)
                return new Size(CardLogicalWidth, CardLogicalHeight);

            return new Size(
                ScaleLogical(CardLogicalWidth),
                ScaleLogical(CardLogicalHeight)
            );
        }

        private int ScaleLogical(int value)
        {
            var dpi = IsHandleCreated ? DeviceDpi : DpiBase;
            return Math.Max(1, (int)Math.Round(value * dpi / (float)DpiBase));
        }

        private Size ScaleLogicalSize(Size size)
        {
            return new Size(ScaleLogical(size.Width), ScaleLogical(size.Height));
        }

        private void ApplyMinimumWindowSize()
        {
            MinimumSize = ScaleLogicalSize(new Size(560, 360));
        }

        private void OnDeleteCustomClick(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var custom = settings.CustomSizes[index];
                var result = ConfirmDialog.Show($"Delete '{custom.Name}'?", "Delete Preset", settings.Theme, this);

                if (result == DialogResult.Yes)
                {
                    settings.CustomSizes.RemoveAt(index);
                    settings.Save();
                    RefreshCustomCards();
                }
            }
        }

        private Panel CreateBottomPanel()
        {
            var panel = new Panel
            {
                AutoSize = true,
                Dock = DockStyle.Top
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = true
            };

            for (int i = 0; i < 6; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            outputLabel = new Label
            {
                Text = $"Output: {TruncatePath(settings.OutputFolder, 50)}",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            toolTip = new ToolTip();
            toolTip.SetToolTip(outputLabel, settings.OutputFolder);

            var outputButtonsPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 10)
            };
            outputButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            outputButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var openFolderButton = CreateSmallButton("Open Folder");
            openFolderButton.Margin = new Padding(0, 0, 5, 0);
            openFolderButton.Click += OnOpenFolderClick;

            var changeFolderButton = CreateSmallButton("Change Folder");
            changeFolderButton.Margin = Padding.Empty;
            changeFolderButton.Click += OnChangeFolderClick;

            outputButtonsPanel.Controls.Add(openFolderButton, 0, 0);
            outputButtonsPanel.Controls.Add(changeFolderButton, 1, 0);

            hotkeyLabel = new Label
            {
                Text = $"Hotkey: {GetHotkeyLabelText()}",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            var settingsButton = CreateSmallButton("Settings");
            settingsButton.Margin = new Padding(0, 0, 0, 5);
            settingsButton.Click += OnSettingsClick;

            var stayOnTopCheckbox = new CheckBox
            {
                Text = "Stay on Top",
                AutoSize = true,
                Checked = settings.StayOnTop
            };
            stayOnTopCheckbox.CheckedChanged += OnStayOnTopChanged;

            layout.Controls.Add(outputLabel, 0, 0);
            layout.Controls.Add(outputButtonsPanel, 0, 1);
            layout.Controls.Add(hotkeyLabel, 0, 2);
            layout.Controls.Add(settingsButton, 0, 3);
            layout.Controls.Add(stayOnTopCheckbox, 0, 4);

            panel.Controls.Add(layout);
            return panel;
        }

        private void ApplyTheme() => Theme.Apply(this, settings.Theme);

        private void RefreshCardGlow()
        {
            bool isDark = settings.Theme == AppTheme.Dark;

            foreach (var card in CaptureCards())
            {
                card.SetGlow(isDark);
                card.Invalidate();
            }
        }

        private Button CreateSmallButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(100, 25),
                Padding = new Padding(8, 0, 8, 0)
            };

            Theme.StyleButton(button, settings.Theme);
            return button;
        }

        private void RestoreWindowBounds()
        {
            var hasSavedLocation = settings.WindowX != -1 || settings.WindowY != -1;
            var savedLocation = new Point(settings.WindowX, settings.WindowY);
            var targetScreen = hasSavedLocation ? FindScreenContaining(savedLocation) : null;
            var screen = targetScreen ?? Screen.PrimaryScreen ?? Screen.AllScreens[0];

            if (targetScreen != null)
                Location = savedLocation;

            var size = ScaleSize(new Size(settings.WindowWidth, settings.WindowHeight), DpiBase, DeviceDpi);
            size = new Size(
                Math.Max(MinimumSize.Width, Math.Min(size.Width, screen.WorkingArea.Width)),
                Math.Max(MinimumSize.Height, Math.Min(size.Height, screen.WorkingArea.Height))
            );

            var bounds = targetScreen != null
                ? new Rectangle(savedLocation, size)
                : CenterIn(screen.WorkingArea, size);

            Bounds = ClampToWorkingArea(bounds, screen.WorkingArea);
        }

        private static Screen? FindScreenContaining(Point point)
        {
            return Screen.AllScreens.FirstOrDefault(screen => screen.WorkingArea.Contains(point));
        }

        private static Rectangle CenterIn(Rectangle area, Size size)
        {
            return new Rectangle(
                area.Left + Math.Max(0, (area.Width - size.Width) / 2),
                area.Top + Math.Max(0, (area.Height - size.Height) / 2),
                size.Width,
                size.Height
            );
        }

        private static Rectangle ClampToWorkingArea(Rectangle bounds, Rectangle workingArea)
        {
            var width = Math.Min(bounds.Width, workingArea.Width);
            var height = Math.Min(bounds.Height, workingArea.Height);
            var x = Math.Max(workingArea.Left, Math.Min(bounds.X, workingArea.Right - width));
            var y = Math.Max(workingArea.Top, Math.Min(bounds.Y, workingArea.Bottom - height));

            return new Rectangle(x, y, width, height);
        }

        private static Size ScaleSize(Size size, int sourceDpi, int targetDpi)
        {
            return new Size(
                Math.Max(1, (int)Math.Round(size.Width * targetDpi / (float)sourceDpi)),
                Math.Max(1, (int)Math.Round(size.Height * targetDpi / (float)sourceDpi))
            );
        }

        private static void ClearControls(Control.ControlCollection controls)
        {
            var existingControls = controls.Cast<Control>().ToArray();
            controls.Clear();

            foreach (var control in existingControls)
                control.Dispose();
        }

        private void OnStayOnTopChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkbox)
            {
                settings.StayOnTop = checkbox.Checked;
                TopMost = checkbox.Checked;
                settings.Save();
            }
        }

        private void OnPresetButtonClick(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is (int width, int height))
            {
                settings.CaptureWidth = width;
                settings.CaptureHeight = height;
                UpdateHotkeyLabel();
                StartCapture();
            }
        }

        private void OnAddCustomClick(object? sender, EventArgs e)
        {
            using var dialog = new CustomSizeDialog(settings.CustomSizes.Count, settings.Theme);
            dialog.TopMost = TopMost;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                settings.CustomSizes.Add(new CustomSize
                {
                    Width = dialog.CustomWidth,
                    Height = dialog.CustomHeight,
                    Name = dialog.CustomName
                });

                settings.Save();
                RefreshCustomCards();
            }
        }

        private void StartCapture()
        {
            if (overlayWindow == null || overlayWindow.IsDisposed)
            {
                overlayWindow = new OverlayWindow(settings);
                overlayWindow.ScreenshotTaken += OnOverlayScreenshotTaken;
                overlayWindow.Show();

                if (!settings.StayOnTop)
                    WindowState = FormWindowState.Minimized;
            }
        }

        private void OnOverlayScreenshotTaken(object? sender, ScreenshotEventArgs e)
        {
            ScreenshotTaken?.Invoke(this, e);
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        private void OnOpenFolderClick(object? sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(settings.OutputFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", settings.OutputFolder);
                }
                else
                {
                    MessageBox.Show("Output folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnChangeFolderClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = settings.OutputFolder,
                Description = "Select output folder for screenshots"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                settings.OutputFolder = dialog.SelectedPath;
                settings.Save();
                UpdateOutputLabel();
            }
        }

        private void OnSettingsClick(object? sender, EventArgs e) => OpenSettings();

        public void OpenSettings()
        {
            SettingsDialogOpening?.Invoke(this, EventArgs.Empty);

            try
            {
                using var settingsForm = new SettingsForm(settings);
                settingsForm.TopMost = TopMost;

                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    settings.HotkeyKey = settingsForm.Settings.HotkeyKey;
                    settings.HotkeyModifiers = settingsForm.Settings.HotkeyModifiers;
                    settings.HotkeyMode = settingsForm.Settings.HotkeyMode;
                    settings.FixedCaptureWidth = settingsForm.Settings.FixedCaptureWidth;
                    settings.FixedCaptureHeight = settingsForm.Settings.FixedCaptureHeight;
                    settings.SnapMode = settingsForm.Settings.SnapMode;
                    settings.MinimizeToTray = settingsForm.Settings.MinimizeToTray;
                    settings.ContinuousCaptureMode = settingsForm.Settings.ContinuousCaptureMode;
                    settings.ShowNotifications = settingsForm.Settings.ShowNotifications;
                    settings.OutputFolder = settingsForm.Settings.OutputFolder;
                    settings.Theme = settingsForm.Settings.Theme;
                    settings.Save();
                    ApplyTheme();
                    RefreshCardGlow();
                    UpdateOutputLabel();
                    UpdateHotkeyLabel();
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                SettingsDialogClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateHotkeyLabel()
        {
            if (hotkeyLabel != null)
                hotkeyLabel.Text = $"Hotkey: {GetHotkeyLabelText()}";
        }

        private string GetHotkeyLabelText()
        {
            if (settings.HotkeyMode == HotkeyMode.FixedSize)
                return $"{GetHotkeyText()} (fixed: {settings.FixedCaptureWidth}×{settings.FixedCaptureHeight})";

            return $"{GetHotkeyText()} (last-used: {settings.CaptureWidth}×{settings.CaptureHeight})";
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            SaveWindowState();
            Application.Exit();
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            SaveWindowState();
        }

        private void OnLocationChanged(object? sender, EventArgs e)
        {
            SaveWindowState();
        }

        private void SaveWindowState()
        {
            if (!boundsRestored || WindowState != FormWindowState.Normal)
                return;

            var logical = ScaleSize(Size, DeviceDpi, DpiBase);
            settings.WindowWidth = logical.Width;
            settings.WindowHeight = logical.Height;
            settings.WindowX = Location.X;
            settings.WindowY = Location.Y;
            settings.Save();
        }

        private string GetHotkeyText()
        {
            return HotkeyFormatter.Format(settings.HotkeyModifiers, settings.HotkeyKey);
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (parts.Length <= 2)
                return path.Substring(0, maxLength - 3) + "...";

            var drive = parts[0] + Path.DirectorySeparatorChar;
            var lastPart = parts[^1];

            if (drive.Length + 4 + lastPart.Length >= maxLength)
                return path.Substring(0, maxLength - 3) + "...";

            return $"{drive}...{Path.DirectorySeparatorChar}{lastPart}";
        }

        private void UpdateOutputLabel()
        {
            if (outputLabel != null)
            {
                outputLabel.Text = $"Output: {TruncatePath(settings.OutputFolder, 50)}";
                toolTip?.SetToolTip(outputLabel, settings.OutputFolder);
            }
        }

        private readonly record struct CapturePreset(int Width, int Height, string Name);
    }
}
