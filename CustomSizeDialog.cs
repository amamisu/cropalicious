using System;
using System.Drawing;
using System.Windows.Forms;

namespace Cropalicious
{
    public partial class CustomSizeDialog : Form
    {
        public int CustomWidth { get; private set; }
        public int CustomHeight { get; private set; }
        public string CustomName { get; private set; } = string.Empty;

        private NumericUpDown widthUpDown = null!;
        private NumericUpDown heightUpDown = null!;
        private TextBox nameTextBox = null!;
        private readonly int existingCount;
        private readonly AppTheme theme;

        public CustomSizeDialog(int existingCount, AppTheme theme)
        {
            this.existingCount = existingCount;
            this.theme = theme;
            InitializeComponent();
            Theme.Apply(this, theme);
        }

        private void InitializeComponent()
        {
            Text = "Add Custom Size";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(ScaleByFont(18))
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            widthUpDown = new NumericUpDown
            {
                MinimumSize = new Size(ScaleByFont(90), ScaleByFont(23)),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            heightUpDown = new NumericUpDown
            {
                MinimumSize = new Size(ScaleByFont(90), ScaleByFont(23)),
                Minimum = 100,
                Maximum = 4000,
                Value = 1024
            };

            nameTextBox = new TextBox
            {
                MinimumSize = new Size(ScaleByFont(230), ScaleByFont(23)),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            var okButton = new Button
            {
                Text = "OK",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(28)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };
            okButton.Click += OnOK;

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(28)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };

            var sizePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight
            };
            sizePanel.Controls.AddRange(new Control[]
            {
                CreateLabel("Width:"), widthUpDown,
                CreateLabel("Height:"), heightUpDown
            });

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

            AddRow(layout, CreateLabel("Size:"), sizePanel);
            AddRow(layout, CreateLabel("Name:"), nameTextBox);
            AddRow(layout, new Label(), buttonPanel);

            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private int ScaleByFont(int value)
        {
            const float baseFontHeight = 15f;
            return Math.Max(1, (int)Math.Round(value * Font.Height / baseFontHeight));
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, ScaleByFont(8), ScaleByFont(10))
            };
        }

        private void AddRow(TableLayoutPanel layout, Control label, Control control)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            label.Margin = new Padding(0, 0, ScaleByFont(12), ScaleByFont(10));
            control.Margin = new Padding(0, 0, 0, ScaleByFont(10));

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void OnOK(object? sender, EventArgs e)
        {
            CustomName = string.IsNullOrWhiteSpace(nameTextBox.Text)
                ? $"Custom{existingCount + 1:D2}"
                : nameTextBox.Text.Trim();
            CustomWidth = (int)widthUpDown.Value;
            CustomHeight = (int)heightUpDown.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
