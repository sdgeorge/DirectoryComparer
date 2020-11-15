using System;
using System.Drawing;
using System.Windows.Forms;

namespace DirectoryComparer.Components
{
    public class InfoProgressBar : ProgressBar
    {
        // Property to set to decide whether to print a % or Text
        // public enum ProgressBarDisplayStyle { Percentage, Text }
        // public ProgressBarDisplayStyle DisplayStyle { get; set; }

        // Property to hold the custom text
        public override string Text { get; set; }

        public InfoProgressBar()
        {
            // Modify the ControlStyles flags
            //http://msdn.microsoft.com/en-us/library/system.windows.forms.controlstyles.aspx
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = ClientRectangle;
            Graphics graphics = e.Graphics;

            ProgressBarRenderer.DrawHorizontalBar(graphics, rect);
            rect.Inflate(-3, -3);
            // Draw the chunks of the progress bar
            if (Value > 0)
            {
                Rectangle clip = new Rectangle(rect.X, rect.Y, (int)Math.Round(((float)Value / Maximum) * rect.Width), rect.Height);
                ProgressBarRenderer.DrawHorizontalChunks(graphics, clip);
            }

            // Set the Display text (Either a % amount or our custom text
            double progress = ((double)Value / (double)Maximum) * 100.0d;
            string text = Text ?? progress.ToString("0.##") + "%";

            using (Font font = new Font(FontFamily.GenericSerif, 10))
            {
                SizeF len = graphics.MeasureString(text, font);
                // Calculate the location of the text (the middle of progress bar)
                // Point location = new Point(Convert.ToInt32((rect.Width / 2) - (len.Width / 2)), Convert.ToInt32((rect.Height / 2) - (len.Height / 2)));
                Point location = new Point(Convert.ToInt32((Width / 2) - len.Width / 2), Convert.ToInt32((Height / 2) - len.Height / 2));
                // The commented-out code will centre the text into the highlighted area only. This will centre the text regardless of the highlighted area.
                // Draw the custom text
                graphics.DrawString(text, font, Brushes.Black, location);
            }
        }
    }
}
