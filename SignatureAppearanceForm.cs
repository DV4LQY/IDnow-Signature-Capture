using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDNowWacomBridge
{
    internal sealed class SignatureAppearanceForm : Form
    {
        private readonly NumericUpDown strokeWidth;
        private readonly Panel colorPreview;
        private Color strokeColor;

        internal SignatureAppearanceForm(SignatureAppearance appearance)
        {
            Text = "Signature appearance";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(430, 180);

            Label widthLabel = new Label
            {
                AutoSize = true,
                Location = new Point(18, 22),
                Text = "Stroke width:"
            };
            Controls.Add(widthLabel);

            strokeWidth = new NumericUpDown
            {
                DecimalPlaces = 2,
                Increment = 0.05M,
                Minimum = 0.10M,
                Maximum = 2.50M,
                Location = new Point(125, 18),
                Size = new Size(90, 23),
                Value = ClampWidth(appearance.StrokeWidthMillimeters)
            };
            Controls.Add(strokeWidth);

            Label millimetersLabel = new Label
            {
                AutoSize = true,
                Location = new Point(222, 22),
                Text = "mm"
            };
            Controls.Add(millimetersLabel);

            Label colorLabel = new Label
            {
                AutoSize = true,
                Location = new Point(18, 66),
                Text = "Preview color:"
            };
            Controls.Add(colorLabel);

            colorPreview = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(125, 60),
                Size = new Size(90, 28)
            };
            Controls.Add(colorPreview);

            Button chooseColor = new Button
            {
                Location = new Point(225, 59),
                Size = new Size(118, 30),
                Text = "Choose color..."
            };
            chooseColor.Click += ChooseColor_Click;
            Controls.Add(chooseColor);

            Label compatibilityNote = new Label
            {
                AutoSize = false,
                Location = new Point(18, 104),
                Size = new Size(394, 32),
                Text = "The generated BMP and clipboard image preserve this color. A legacy IDNow template may still convert it during import."
            };
            Controls.Add(compatibilityNote);

            Button ok = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(256, 145),
                Size = new Size(75, 25),
                Text = "OK"
            };
            Controls.Add(ok);

            Button cancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(337, 145),
                Size = new Size(75, 25),
                Text = "Cancel"
            };
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
            strokeColor = appearance.StrokeColor;
            UpdateColorPreview();
        }

        internal float StrokeWidthMillimeters
        {
            get { return Decimal.ToSingle(strokeWidth.Value); }
        }

        internal Color StrokeColor
        {
            get { return strokeColor; }
        }

        private void ChooseColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = strokeColor;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    strokeColor = dialog.Color;
                    UpdateColorPreview();
                }
            }
        }

        private void UpdateColorPreview()
        {
            colorPreview.BackColor = strokeColor;
        }

        private static decimal ClampWidth(float value)
        {
            if (value < 0.10F) return 0.10M;
            if (value > 2.50F) return 2.50M;
            return (decimal)value;
        }
    }
}
