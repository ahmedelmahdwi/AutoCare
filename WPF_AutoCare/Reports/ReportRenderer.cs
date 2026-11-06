using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;

namespace C__project_Auto_Care.Reports
{
    /// <summary>
    /// Renders a ReportDocument into full resolution A4 page bitmaps using pure
    /// GDI+ (no third party report engine). Pages are the exact images that get
    /// printed, so the preview is always WYSIWYG. Arabic text is rendered with
    /// full shaping through DirectionRightToLeft drawing.
    /// </summary>
    public static class ReportRenderer
    {
        public const float Dpi = 200.0f;
        private const float BaseDpi = 150.0f;

        public static Size PageSize
        {
            get { return new Size((int)(8.27f * Dpi), (int)(11.69f * Dpi)); }
        }

        private static float S { get { return Dpi / BaseDpi; } }

        private static int Margin { get { return (int)Math.Round(45f * S); } }
        private static int BottomMargin { get { return (int)Math.Round(70f * S); } }

        private static float Px(float designPixels) { return designPixels * S; }

        private static readonly Font TitleFont = new Font("Tahoma", 30f, FontStyle.Bold);
        private static readonly Font SubtitleFont = new Font("Tahoma", 15f, FontStyle.Regular);
        private static readonly Font SectionFont = new Font("Tahoma", 16f, FontStyle.Bold);
        private static readonly Font HeaderFont = new Font("Tahoma", 14f, FontStyle.Bold);
        private static readonly Font BodyFont = new Font("Tahoma", 13f, FontStyle.Regular);
        private static readonly Font InfoFont = new Font("Tahoma", 14f, FontStyle.Regular);
        private static readonly Font TotalsFont = new Font("Tahoma", 14f, FontStyle.Bold);
        private static readonly Font FooterFont = new Font("Tahoma", 12f, FontStyle.Regular);

        private static readonly Color HeaderBack = Color.FromArgb(235, 235, 235);
        private static readonly Color GridPen = Color.FromArgb(180, 180, 180);
        private static readonly Color TopRule = Color.FromArgb(120, 120, 120);
        private static readonly Color Muted = Color.FromArgb(85, 85, 85);
        private static readonly Color TotalsBack = Color.FromArgb(250, 248, 230);

        public static string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.GetCultureInfo("en-US")) + " د.ل";
        }

        public static Bitmap[] Render(ReportDocument doc)
        {
            if (doc == null)
                doc = new ReportDocument();

            Size pageSize = PageSize;
            int w = pageSize.Width;
            int h = pageSize.Height;
            int contentBottom = h - BottomMargin;

            List<Bitmap> pages = new List<Bitmap>();
            Bitmap page = NewPage(w, h);
            Graphics g = Graphics.FromImage(page);
            SetTextMode(g);
            int y = Margin;

            Action<float> ensureSpace = need =>
            {
                need += Px(8f);
                if (y + need > contentBottom)
                {
                    pages.Add(page);
                    page = NewPage(w, h);
                    g.Dispose();
                    g = Graphics.FromImage(page);
                    SetTextMode(g);
                    y = Margin;
                }
            };

            foreach (ReportBlock block in doc.Blocks)
            {
                if (block is ReportTitleBlock title)
                {
                    float ht = MeasureWrapped(g, title.Text, TitleFont, w - 2 * Margin) + Px(6f);
                    ensureSpace(ht);
                    DrawCentered(g, title.Text, TitleFont, new RectangleF(Margin, y, w - 2 * Margin, ht), Color.Black);
                    y += (int)Math.Ceiling(ht);
                }
                else if (block is ReportSubtitleBlock subtitle)
                {
                    float ht = MeasureWrapped(g, subtitle.Text, SubtitleFont, w - 2 * Margin) + Px(4f);
                    ensureSpace(ht);
                    DrawCentered(g, subtitle.Text, SubtitleFont, new RectangleF(Margin, y, w - 2 * Margin, ht), Muted);
                    y += (int)Math.Ceiling(ht);
                }
                else if (block is ReportDividerBlock)
                {
                    ensureSpace(Px(4f));
                    using (Pen pen = new Pen(TopRule, Px(1.6f)))
                        g.DrawLine(pen, Margin, y + Px(6f), w - Margin, y + Px(6f));
                    y += (int)Math.Round(Px(16f));
                }
                else if (block is ReportSpacerBlock spacer)
                {
                    y += (int)Math.Round(Px(spacer.Pixels));
                }
                else if (block is ReportInfoBlock info)
                {
                    foreach (string[] pair in info.Items)
                    {
                        string line = pair[0] + ":  " + pair[1];
                        float ht = MeasureWrapped(g, line, InfoFont, w - 2 * Margin) + Px(3f);
                        ensureSpace(ht);
                        DrawRightAligned(g, line, InfoFont, new RectangleF(Margin, y, w - 2 * Margin, ht), Color.Black);
                        y += (int)Math.Ceiling(ht);
                    }
                }
                else if (block is ReportTotalsLineBlock totals)
                {
                    if (string.IsNullOrEmpty(totals.Text))
                        continue;
                    float ht = MeasureWrapped(g, totals.Text, TotalsFont, w - 2 * Margin) + Px(6f);
                    ensureSpace(ht);
                    DrawRightAligned(g, totals.Text, TotalsFont, new RectangleF(Margin, y, w - 2 * Margin, ht), Color.Black);
                    y += (int)Math.Ceiling(ht);
                }
                else if (block is ReportTableBlock table)
                {
                    float[] widths = ColumnWidths(table, w - 2 * Margin);

                    if (!string.IsNullOrEmpty(table.Title))
                    {
                        float secH = SectionFont.GetHeight(g) + Px(8f);
                        ensureSpace(secH);
                        DrawCentered(g, table.Title, SectionFont,
                            new RectangleF(Margin, y, w - 2 * Margin, secH), Color.Black);
                        y += (int)Math.Ceiling(secH);
                    }

                    float headerH = HeaderFont.GetHeight(g) + Px(14f);
                    Action drawHeader = () =>
                    {
                        DrawHeaderRow(g, table, widths, Margin, y, headerH, w);
                        y += (int)Math.Ceiling(headerH);
                    };

                    if (y + headerH > contentBottom)
                        NewPageAt(ref pages, ref page, ref g, w, h, ref y);

                    drawHeader();

                    foreach (string[] row in table.Rows)
                    {
                        float rowH = MeasureRowHeight(g, table, row, widths);
                        if (y + rowH > contentBottom)
                        {
                            NewPageAt(ref pages, ref page, ref g, w, h, ref y);
                            drawHeader();
                        }
                        DrawRow(g, table, row, widths, Margin, y, rowH, false);
                        y += (int)Math.Ceiling(rowH);
                    }

                    if (table.TotalsRow != null)
                    {
                        float totalsH = MeasureRowHeight(g, table, table.TotalsRow, widths);
                        if (y + totalsH > contentBottom)
                        {
                            NewPageAt(ref pages, ref page, ref g, w, h, ref y);
                            DrawHeaderRow(g, table, widths, Margin, y, headerH, w);
                            y += (int)Math.Ceiling(headerH);
                        }
                        DrawRow(g, table, table.TotalsRow, widths, Margin, y, totalsH, true);
                        y += (int)Math.Ceiling(totalsH);
                    }
                }
                else if (block is ReportQrCodeBlock qr && qr.QrImage != null)
                {
                    float qrPx = Px(qr.Size);
                    float ht = qrPx + Px(24f);
                    ensureSpace(ht);
                    float x = (w - qrPx) / 2f;
                    g.DrawImage(qr.QrImage, x, y, qrPx, qrPx);
                    if (!string.IsNullOrEmpty(qr.Caption))
                    {
                        RectangleF capRect = new RectangleF(Margin, y + qrPx + Px(4f), w - 2 * Margin, Px(18f));
                        DrawCentered(g, qr.Caption, FooterFont, capRect, Muted);
                    }
                    y += (int)Math.Ceiling(ht);
                }
            }

            pages.Add(page);
            g.Dispose();

            DrawFooters(pages);

            return pages.ToArray();
        }

        private static void NewPageAt(ref List<Bitmap> pages, ref Bitmap page, ref Graphics g, int w, int h, ref int y)
        {
            pages.Add(page);
            page = NewPage(w, h);
            g.Dispose();
            g = Graphics.FromImage(page);
            SetTextMode(g);
            y = Margin;
        }

        private static Bitmap NewPage(int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g0 = Graphics.FromImage(bmp))
            {
                g0.Clear(Color.White);
            }
            return bmp;
        }

        private static void SetTextMode(Graphics g)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PageUnit = GraphicsUnit.Pixel;
        }

        private static StringFormat RtlRight()
        {
            StringFormat sf = new StringFormat();
            sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Near;
            return sf;
        }

        private static StringFormat RtlCenter()
        {
            StringFormat sf = new StringFormat();
            sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Near;
            return sf;
        }

        private static float MeasureWrapped(Graphics g, string text, Font font, float maxWidth)
        {
            using (StringFormat sf = RtlRight())
            {
                SizeF size = g.MeasureString(text, font, new SizeF(maxWidth, 10000f), sf);
                return size.Height;
            }
        }

        private static void DrawRightAligned(Graphics g, string text, Font font, RectangleF rect, Color color)
        {
            using (StringFormat sf = RtlRight())
            using (SolidBrush brush = new SolidBrush(color))
                g.DrawString(text, font, brush, rect, sf);
        }

        private static void DrawCentered(Graphics g, string text, Font font, RectangleF rect, Color color)
        {
            using (StringFormat sf = RtlCenter())
            using (SolidBrush brush = new SolidBrush(color))
                g.DrawString(text, font, brush, rect, sf);
        }

        private static float[] ColumnWidths(ReportTableBlock table, int totalWidth)
        {
            float[] widths = new float[table.Columns.Count];
            double sum = 0.0;
            foreach (ReportColumn c in table.Columns)
                sum += c.Weight;
            if (sum <= 0.0)
                sum = table.Columns.Count;
            float allocated = 0f;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                widths[i] = (float)Math.Round(totalWidth * table.Columns[i].Weight / sum);
                allocated += widths[i];
            }
            if (table.Columns.Count > 0)
            {
                float diff = totalWidth - allocated;
                widths[table.Columns.Count - 1] += diff;
            }
            return widths;
        }

        private static void DrawHeaderRow(Graphics g, ReportTableBlock table, float[] widths, int x, int y, float rowH, int pageW)
        {
            int total = pageW - 2 * Margin;
            RectangleF rect = new RectangleF(x, y, total, rowH);
            using (SolidBrush brush = new SolidBrush(HeaderBack))
                g.FillRectangle(brush, rect);

            float currentRight = x + total;
            using (Pen pen = new Pen(GridPen, 1f))
            {
                for (int i = 0; i < widths.Length; i++)
                {
                    float cellX = currentRight - widths[i];
                    RectangleF cell = new RectangleF(cellX, y, widths[i], rowH);
                    g.DrawRectangle(pen, cell.X, cell.Y, cell.Width, cell.Height);
                    string header = table.Columns[i].Header ?? "";
                    using (StringFormat sf = MakeAlignFormat(table.Columns[i].Align))
                    {
                        RectangleF t = new RectangleF(cell.X + Px(6f), cell.Y + Px(4f), cell.Width - Px(12f), cell.Height - Px(8f));
                        g.DrawString(header, HeaderFont, Brushes.Black, t, sf);
                    }
                    currentRight -= widths[i];
                }
            }
        }

        private static float MeasureRowHeight(Graphics g, ReportTableBlock table, string[] cells, float[] widths)
        {
            float max = 0f;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                string text = i < cells.Length ? (cells[i] ?? "") : "";
                using (StringFormat sf = MakeAlignFormat(table.Columns[i].Align))
                {
                    SizeF size = g.MeasureString(text, BodyFont, new SizeF(widths[i] - Px(14f), 10000f), sf);
                    if (size.Height > max)
                        max = size.Height;
                }
            }
            return max + Px(14f);
        }

        private static void DrawRow(Graphics g, ReportTableBlock table, string[] cells, float[] widths, int x, int y, float rowH, bool isTotalsRow)
        {
            Font font = isTotalsRow ? TotalsFont : BodyFont;

            using (Pen pen = new Pen(GridPen, 1f))
            {
                float total = 0f;
                for (int i = 0; i < widths.Length; i++)
                    total += widths[i];

                if (isTotalsRow)
                {
                    RectangleF band = new RectangleF(x, y, total, rowH);
                    using (SolidBrush brush = new SolidBrush(TotalsBack))
                        g.FillRectangle(brush, band);
                }

                float currentRight = x + total;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    float cellX = currentRight - widths[i];
                    RectangleF cell = new RectangleF(cellX, y, widths[i], rowH);
                    g.DrawRectangle(pen, cell.X, cell.Y, cell.Width, cell.Height);
                    string text = i < cells.Length ? (cells[i] ?? "") : "";
                    using (StringFormat sf = MakeAlignFormat(table.Columns[i].Align))
                    {
                        RectangleF t = new RectangleF(cell.X + Px(5f), cell.Y + Px(3f), cell.Width - Px(10f), cell.Height - Px(6f));
                        using (SolidBrush brush = new SolidBrush(Color.Black))
                            g.DrawString(text, font, brush, t, sf);
                    }
                    currentRight -= widths[i];
                }
            }
        }

        private static StringFormat MakeAlignFormat(ReportAlign align)
        {
            StringFormat sf = new StringFormat();
            sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;
            sf.LineAlignment = StringAlignment.Center;
            switch (align)
            {
                case ReportAlign.Center:
                    sf.Alignment = StringAlignment.Center;
                    break;
                case ReportAlign.Left:
                    sf.Alignment = StringAlignment.Far; // Far in RTL = Left aligned
                    break;
                default:
                    sf.Alignment = StringAlignment.Near; // Near in RTL = Right aligned
                    break;
            }
            return sf;
        }

        private static void DrawFooters(List<Bitmap> pages)
        {
            Size ps = PageSize;
            int w = ps.Width;
            int h = ps.Height;

            for (int i = 0; i < pages.Count; i++)
            {
                Bitmap page = pages[i];
                using (Graphics g = Graphics.FromImage(page))
                {
                    SetTextMode(g);
                    int y = h - (int)Math.Round(Px(52f));
                    using (Pen pen = new Pen(GridPen, Px(1.2f)))
                        g.DrawLine(pen, Margin, y, w - Margin, y);

                    string footer = "AutoCare - نظام إدارة مراكز صيانة السيارات   •   تاريخ الإنشاء: " +
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
                        "   •   الصفحة " + (i + 1) + " من " + pages.Count;

                    using (StringFormat sf = RtlCenter())
                    using (SolidBrush brush = new SolidBrush(Muted))
                    {
                        RectangleF rect = new RectangleF(Margin, y + Px(8f), w - 2 * Margin, Px(36f));
                        g.DrawString(footer, FooterFont, brush, rect, sf);
                    }
                }
            }
        }
    }
}