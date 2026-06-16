using System.Drawing;
using System.Windows.Forms;

namespace Cropalicious
{
    public class ConfirmDialog : Form
    {
        public ConfirmDialog(string message, string title, AppTheme theme)
        {
            Text = title;
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
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(ScaleByFont(18))
            };

            var label = new Label
            {
                Text = message,
                AutoSize = true,
                MaximumSize = new Size(ScaleByFont(280), 0),
                Margin = new Padding(0, 0, 0, ScaleByFont(14)),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var yesButton = new Button
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(28)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };
            Theme.StyleButton(yesButton, theme);

            var noButton = new Button
            {
                Text = "No",
                DialogResult = DialogResult.No,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(ScaleByFont(75), ScaleByFont(28)),
                Padding = new Padding(ScaleByFont(8), 0, ScaleByFont(8), 0)
            };
            Theme.StyleButton(noButton, theme);

            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                FlowDirection = FlowDirection.LeftToRight
            };
            buttonPanel.Controls.AddRange(new Control[] { yesButton, noButton });

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(buttonPanel, 0, 1);

            Controls.Add(layout);
            Theme.Apply(this, theme);
            AcceptButton = yesButton;
            CancelButton = noButton;
        }

        private int ScaleByFont(int value)
        {
            const float baseFontHeight = 15f;
            return System.Math.Max(1, (int)System.Math.Round(value * Font.Height / baseFontHeight));
        }

        public static DialogResult Show(string message, string title, AppTheme theme, Form? owner = null)
        {
            using var dialog = new ConfirmDialog(message, title, theme);
            if (owner != null) dialog.TopMost = owner.TopMost;
            return dialog.ShowDialog(owner);
        }
    }
}
