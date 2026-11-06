using System.Collections.Generic;

namespace C__project_Auto_Care.Reports
{
    public enum ReportAlign
    {
        Right,
        Center,
        Left
    }

    public abstract class ReportBlock
    {
    }

    public class ReportTitleBlock : ReportBlock
    {
        public string Text;

        public ReportTitleBlock(string text)
        {
            Text = text;
        }
    }

    public class ReportSubtitleBlock : ReportBlock
    {
        public string Text;

        public ReportSubtitleBlock(string text)
        {
            Text = text;
        }
    }

    public class ReportDividerBlock : ReportBlock
    {
        public ReportDividerBlock()
        {
        }
    }

    public class ReportSpacerBlock : ReportBlock
    {
        public int Pixels;

        public ReportSpacerBlock(int pixels)
        {
            Pixels = pixels;
        }
    }

    public class ReportInfoBlock : ReportBlock
    {
        public readonly List<string[]> Items = new List<string[]>();

        public ReportInfoBlock Add(string label, string value)
        {
            Items.Add(new[] { label ?? "", value ?? "" });
            return this;
        }
    }

    public class ReportTotalsLineBlock : ReportBlock
    {
        public string Text;

        public ReportTotalsLineBlock()
        {
        }

        public ReportTotalsLineBlock(string text)
        {
            Text = text;
        }
    }

    public class ReportColumn
    {
        public string Header;
        public ReportAlign Align = ReportAlign.Right;
        public double Weight = 1.0;
    }

    public class ReportTableBlock : ReportBlock
    {
        public string Title;
        public readonly List<ReportColumn> Columns = new List<ReportColumn>();
        public readonly List<string[]> Rows = new List<string[]>();
        public string[] TotalsRow;

        public ReportTableBlock AddColumn(string header, ReportAlign align = ReportAlign.Right, double weight = 1.0)
        {
            Columns.Add(new ReportColumn { Header = header, Align = align, Weight = weight });
            return this;
        }

        public ReportTableBlock AddRow(params string[] cells)
        {
            Rows.Add(cells);
            return this;
        }
    }

    public class ReportQrCodeBlock : ReportBlock
    {
        public System.Drawing.Bitmap QrImage;
        public int Size;
        public string Caption;

        public ReportQrCodeBlock(System.Drawing.Bitmap qrImage, int size = 120, string caption = "رمز الفاتورة الإلكترونية المشفر")
        {
            QrImage = qrImage;
            Size = size;
            Caption = caption;
        }
    }

    public class ReportDocument
    {
        public readonly List<ReportBlock> Blocks = new List<ReportBlock>();

        public ReportDocument Add(ReportBlock block)
        {
            Blocks.Add(block);
            return this;
        }
    }
}