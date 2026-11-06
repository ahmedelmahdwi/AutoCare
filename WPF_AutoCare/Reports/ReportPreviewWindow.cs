using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using C__project_Auto_Care.Reports;
using C__project_Auto_Care.Theme;

namespace C__project_Auto_Care.Reports
{
    public class ReportPreviewWindow : DialogWindow
    {
        private readonly Bitmap[] _pages;
        private readonly ScrollViewer _scroll;
        private readonly Grid _surface;
        private readonly Border _pageFrame;
        private readonly Viewbox _pageViewbox;
        private readonly System.Windows.Controls.Image _image;
        private TextBlock _lblPage;
        private ComboBox _cmbZoom;
        private int _index;
        private int _zoomPercent = 100;
        private double _scale = 1.0;
        private bool _fitEnabled = true;
        private bool _updatingZoom;

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private sealed class OwnerHandle : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;
            public OwnerHandle(IntPtr handle) { _handle = handle; }
            public IntPtr Handle { get { return _handle; } }
        }

        private System.Windows.Forms.IWin32Window WinFormsOwner()
        {
            return new OwnerHandle(new WindowInteropHelper(this).Handle);
        }

        private static BitmapSource ToSource(Bitmap bmp)
        {
            IntPtr hbmp = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero,
                    System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hbmp);
            }
        }

        private ReportPreviewWindow(Bitmap[] pages, string title = "معاينة التقرير - AutoCare") : base(title ?? "معاينة التقرير - AutoCare", 1120, 800)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Title = title ?? "معاينة التقرير - AutoCare";
            ResizeMode = ResizeMode.CanResize;

            _pages = pages;
            if (_pages == null || _pages.Length == 0)
                _pages = new[] { new Bitmap(1, 1) };

            var toolbar = CreateToolbar();

            _scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = AppTheme.Solid(System.Windows.Media.Color.FromRgb(210, 210, 210))
            };
            _surface = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                FlowDirection = FlowDirection.LeftToRight
            };
            _pageFrame = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = AppTheme.Solid(System.Windows.Media.Color.FromRgb(150, 150, 150)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _pageViewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _image = new System.Windows.Controls.Image
            {
                Stretch = Stretch.Fill
            };
            _pageViewbox.Child = _image;
            _pageFrame.Child = _pageViewbox;
            _surface.Children.Add(_pageFrame);
            _scroll.Content = _surface;

            // Structure root layout so toolbar is fixed at top and never scrolls off
            var rootLayout = new System.Windows.Controls.Grid();
            rootLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleBar = CreateTitleBar(this, Title);
            Grid.SetRow(titleBar, 0);
            rootLayout.Children.Add(titleBar);

            var toolbarBorder = new Border
            {
                Background = (System.Windows.Media.Brush)Application.Current?.Resources["CardBackgroundBrush"] ?? System.Windows.Media.Brushes.White,
                BorderBrush = (System.Windows.Media.Brush)Application.Current?.Resources["BorderBrush"] ?? AppTheme.Solid(System.Windows.Media.Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(6, 4, 6, 4)
            };
            toolbarBorder.Child = toolbar;
            Grid.SetRow(toolbarBorder, 1);
            rootLayout.Children.Add(toolbarBorder);

            Grid.SetRow(_scroll, 2);
            rootLayout.Children.Add(_scroll);

            Content = rootLayout;

            // Ctrl + MouseWheel Smooth Zooming (Zoom in / Zoom out, stops at Fit to Page)
            _scroll.PreviewMouseWheel += (s, e) =>
            {
                if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                {
                    e.Handled = true;
                    if (_pages == null || _pages.Length == 0 || _index >= _pages.Length) return;

                    Bitmap page = _pages[_index];
                    const double previewMargin = 22;
                    double kw = Math.Max(1, _scroll.ViewportWidth - previewMargin) / Math.Max(1, (double)page.Width);
                    double kh = Math.Max(1, _scroll.ViewportHeight - previewMargin) / Math.Max(1, (double)page.Height);
                    double fitScale = Math.Min(kw, kh);
                    int fitPercent = Math.Max(10, (int)Math.Round(fitScale * 100.0));

                    int step = 10;
                    if (e.Delta > 0)
                    {
                        int targetPercent = Math.Min(300, _zoomPercent + step);
                        _fitEnabled = false;
                        SetZoom(targetPercent);
                    }
                    else if (e.Delta < 0)
                    {
                        int targetPercent = _zoomPercent - step;
                        if (targetPercent <= fitPercent)
                        {
                            FitToWindow();
                        }
                        else
                        {
                            _fitEnabled = false;
                            SetZoom(targetPercent);
                        }
                    }
                }
            };

            Loaded += (s, e) =>
            {
                FitToWindow();
            };

            SizeChanged += (s, e) =>
            {
                if (_fitEnabled) FitToWindow();
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape) Close();
            };
        }

        private DockPanel CreateToolbar()
        {
            var bar = new DockPanel { Margin = new Thickness(10, 8, 10, 8) };

            var print = CreateBtn("طباعة", "PrimaryLightBrush", "PrimaryBrush", 100, 36, 14);
            print.Margin = new Thickness(6, 0, 0, 0);
            print.Click += (s, e) => PrintReport();
            DockPanel.SetDock(print, Dock.Left);
            bar.Children.Add(print);

            var exportPdf = CreateBtn("📄 تصدير PDF", "SuccessLightBrush", "SuccessBrush", 125, 36, 13.5);
            exportPdf.Margin = new Thickness(6, 0, 0, 0);
            exportPdf.Background = AppTheme.Solid(System.Windows.Media.Color.FromRgb(16, 185, 129));
            exportPdf.Click += (s, e) => ExportPdf();
            DockPanel.SetDock(exportPdf, Dock.Left);
            bar.Children.Add(exportPdf);

            var exportWord = CreateBtn("📝 تصدير Word", "PrimaryLightBrush", "PrimaryBrush", 125, 36, 13.5);
            exportWord.Margin = new Thickness(6, 0, 0, 0);
            exportWord.Background = AppTheme.Solid(System.Windows.Media.Color.FromRgb(37, 99, 235));
            exportWord.Click += (s, e) => ExportWord();
            DockPanel.SetDock(exportWord, Dock.Left);
            bar.Children.Add(exportWord);

            var setup = CreateGhostBtn("إعداد الصفحة", 110, 36);
            setup.Margin = new Thickness(6, 0, 0, 0);
            setup.Click += (s, e) => PageSetup();
            DockPanel.SetDock(setup, Dock.Left);
            bar.Children.Add(setup);

            var close = CreateBtn("إغلاق", "AccentRedBrush", "AccentRedDarkBrush", 90, 36, 14);
            close.Margin = new Thickness(6, 0, 0, 0);
            close.Click += (s, e) => Close();
            DockPanel.SetDock(close, Dock.Left);
            bar.Children.Add(close);

            var fit = CreateBtn("مناسب للصفحة", "PrimaryGradientBrush", "PrimaryDarkBrush", 130, 36, 14);
            fit.Margin = new Thickness(6, 0, 0, 0);
            fit.Click += (s, e) => FitToWindow();
            DockPanel.SetDock(fit, Dock.Right);
            bar.Children.Add(fit);

            _lblPage = CreateLabel("الصفحة 1", 14, true);
            _lblPage.Margin = new Thickness(14, 0, 14, 0);
            DockPanel.SetDock(_lblPage, Dock.Right);
            bar.Children.Add(_lblPage);

            var next = CreateBtn("التالي", "PrimaryGradientBrush", "PrimaryDarkBrush", 90, 36, 14);
            next.Click += (s, e) => GoTo(_index + 1);
            DockPanel.SetDock(next, Dock.Right);
            bar.Children.Add(next);

            var prev = CreateBtn("السابق", "PrimaryGradientBrush", "PrimaryDarkBrush", 90, 36, 14);
            prev.Margin = new Thickness(6, 0, 6, 0);
            prev.Click += (s, e) => GoTo(_index - 1);
            DockPanel.SetDock(prev, Dock.Right);
            bar.Children.Add(prev);

            var zoomLabel = CreateLabel("التكبير:", 14, true);
            zoomLabel.Margin = new Thickness(14, 0, 6, 0);
            DockPanel.SetDock(zoomLabel, Dock.Right);
            bar.Children.Add(zoomLabel);

            _cmbZoom = new ComboBox
            {
                Width = 90,
                Height = 36,
                FontFamily = AppTheme.Font,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
            _cmbZoom.Items.Clear();
            _cmbZoom.Items.Add("25%");
            _cmbZoom.Items.Add("50%");
            _cmbZoom.Items.Add("75%");
            _cmbZoom.Items.Add("100%");
            _cmbZoom.Items.Add("125%");
            _cmbZoom.Items.Add("150%");
            _cmbZoom.Items.Add("200%");
            _cmbZoom.Items.Add("250%");
            _cmbZoom.Items.Add("300%");
            _cmbZoom.SelectedIndex = 3;
            _cmbZoom.VerticalAlignment = VerticalAlignment.Center;
            _cmbZoom.SelectionChanged += (s, e) =>
            {
                if (_updatingZoom) return;
                string sel = _cmbZoom.SelectedItem?.ToString() ?? "100%";
                int v;
                if (int.TryParse(sel.Replace("%", ""), out v) && v > 0)
                {
                    _fitEnabled = false;
                    _zoomPercent = v;
                    _scale = v / 100.0;
                    ShowPage();
                }
            };
            DockPanel.SetDock(_cmbZoom, Dock.Right);
            bar.Children.Add(_cmbZoom);

            return bar;
        }

        public static void Show(ReportDocument document, Window owner = null, string title = null)
        {
            Bitmap[] pages = null;
            try
            {
                pages = ReportRenderer.Render(document);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("تعذر إنشاء التقرير:\n" + ex.Message, "خطأ", WMsgIcon.Error, owner);
                return;
            }

            if (pages == null || pages.Length == 0)
            {
                WMessageBox.Show("تعذر إنشاء التقرير.", "خطأ", WMsgIcon.Error, owner);
                return;
            }

            string docTitle = !string.IsNullOrWhiteSpace(title)
                ? title
                : "معاينة التقرير - AutoCare";

            var win = new ReportPreviewWindow(pages, docTitle) { Owner = owner };
            win.ShowDialog();

            foreach (Bitmap bmp in pages)
                bmp.Dispose();
        }

        private void ExportPdf()
        {
            try
            {
                ReportExporter.SaveWithDialog(_pages, Title ?? "تقرير", this, ReportExportFormat.Pdf);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("فشل تصدير ملف PDF:\n" + ex.Message, "خطأ", WMsgIcon.Error, this);
            }
        }

        private void ExportWord()
        {
            try
            {
                ReportExporter.SaveWithDialog(_pages, Title ?? "تقرير", this, ReportExportFormat.WordDocx);
            }
            catch (Exception ex)
            {
                WMessageBox.Show("فشل تصدير مستند Word:\n" + ex.Message, "خطأ", WMsgIcon.Error, this);
            }
        }

        private void GoTo(int newIndex)
        {
            if (_pages.Length == 0) return;
            if (newIndex < 0) newIndex = 0;
            if (newIndex >= _pages.Length) newIndex = _pages.Length - 1;
            _index = newIndex;
            ShowPage();
        }

        private void ShowPage()
        {
            if (_image == null || _index >= _pages.Length) return;

            Bitmap page = _pages[_index];
            double width = page.Width * _scale;
            double height = page.Height * _scale;
            const double framePadding = 22;

            _surface.Width = Math.Max(_scroll.ViewportWidth, width + framePadding);
            _surface.Height = Math.Max(_scroll.ViewportHeight, height + framePadding);
            _pageFrame.Width = width + 2;
            _pageFrame.Height = height + 2;
            _image.Width = page.Width;
            _image.Height = page.Height;
            _image.Source = ToSource(page);
            _lblPage.Text = "الصفحة " + (_index + 1) + " من " + _pages.Length;
        }

        private void SetZoom(int pct)
        {
            _updatingZoom = true;
            _zoomPercent = pct;
            _scale = pct / 100.0;
            try
            {
                string label = pct + "%";
                int idx = _cmbZoom.Items.IndexOf(label);
                if (idx >= 0)
                {
                    _cmbZoom.SelectedIndex = idx;
                }
                else
                {
                    _cmbZoom.Items.Add(label);
                    _cmbZoom.SelectedItem = label;
                }
            }
            finally
            {
                _updatingZoom = false;
            }
            ShowPage();
        }

        private void FitToWindow()
        {
            if (_pages.Length == 0 || _scroll.ViewportWidth < 50 || _scroll.ViewportHeight < 50) return;

            Bitmap page = _pages[_index];
            const double previewMargin = 22;
            double kw = Math.Max(1, _scroll.ViewportWidth - previewMargin) / Math.Max(1, (double)page.Width);
            double kh = Math.Max(1, _scroll.ViewportHeight - previewMargin) / Math.Max(1, (double)page.Height);
            double k = Math.Min(kw, kh);
            _fitEnabled = true;
            _scale = k;
            _zoomPercent = Math.Max(1, (int)Math.Round(k * 100.0));
            _updatingZoom = true;
            try
            {
                string label = _zoomPercent + "%";
                int idx = _cmbZoom.Items.IndexOf(label);
                if (idx >= 0)
                {
                    _cmbZoom.SelectedIndex = idx;
                }
                else
                {
                    _cmbZoom.Items.Add(label);
                    _cmbZoom.SelectedItem = label;
                }
            }
            finally
            {
                _updatingZoom = false;
            }
            ShowPage();
        }

        // ---------------- Print support (GDI+ PrintDocument, identical output to WinForms app) ----------------
        private System.Drawing.Printing.Margins _margins = new System.Drawing.Printing.Margins(40, 40, 40, 40);

        private void PrintReport()
        {
            if (_pages == null || _pages.Length == 0) return;

            using (System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument())
            {
                pd.DocumentName = Title;
                pd.DefaultPageSettings.Margins = _margins;

                using (System.Windows.Forms.PrintDialog dialog = new System.Windows.Forms.PrintDialog())
                {
                    dialog.Document = pd;
                    dialog.AllowSomePages = false;
                    dialog.AllowSelection = false;
                    dialog.AllowCurrentPage = false;
                    if (dialog.ShowDialog(WinFormsOwner()) != System.Windows.Forms.DialogResult.OK)
                        return;
                }

                int current = 0;
                pd.PrintPage += delegate(object s, System.Drawing.Printing.PrintPageEventArgs e)
                {
                    if (current >= _pages.Length)
                    {
                        e.HasMorePages = false;
                        return;
                    }

                    Bitmap bmp = _pages[current];
                    RectangleF target = e.MarginBounds;
                    if (target.Width < 20 || target.Height < 20)
                        target = e.PageBounds;

                    float scale = Math.Min(target.Width / bmp.Width, target.Height / bmp.Height);
                    int dw = (int)(bmp.Width * scale);
                    int dh = (int)(bmp.Height * scale);
                    int dx = (int)(target.X + (target.Width - dw) / 2f);
                    int dy = (int)(target.Y + (target.Height - dh) / 2f);

                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(bmp, new Rectangle(dx, dy, dw, dh),
                        0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel);

                    current++;
                    e.HasMorePages = current < _pages.Length;
                };

                try
                {
                    pd.Print();
                }
                catch (Exception ex)
                {
                    WMessageBox.Show("حدث خطأ أثناء الطباعة:\n" + ex.Message, "خطأ", WMsgIcon.Error, this);
                }
            }
        }

        private void PageSetup()
        {
            using (System.Windows.Forms.PageSetupDialog dialog = new System.Windows.Forms.PageSetupDialog())
            using (System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument())
            {
                pd.DefaultPageSettings.Margins = _margins;
                dialog.Document = pd;
                if (dialog.ShowDialog(WinFormsOwner()) == System.Windows.Forms.DialogResult.OK)
                {
                    _margins = pd.DefaultPageSettings.Margins;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_image != null) _image.Source = null;
            base.OnClosed(e);
        }

        private static Button CreateBtn(string text, string bgBrushKey, string hoverBrushKey, double width, double height, double fontSize = 14)
        {
            var btn = new Button
            {
                Content = text,
                Width = width,
                Height = height,
                FontFamily = AppTheme.Font,
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0),
                FocusVisualStyle = null
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "border");
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetResourceReference(Border.BackgroundProperty, bgBrushKey);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.BorderBrushProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255)));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new DynamicResourceExtension(hoverBrushKey)
            });
            template.Triggers.Add(hoverTrigger);

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.OpacityProperty,
                Value = 0.85
            });
            template.Triggers.Add(pressedTrigger);

            btn.Template = template;
            return btn;
        }

        private static Button CreateGhostBtn(string text, double width, double height)
        {
            var btn = new Button
            {
                Content = text,
                Width = width,
                Height = height,
                FontFamily = AppTheme.Font,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                FocusVisualStyle = null
            };
            btn.SetResourceReference(Button.ForegroundProperty, "PrimaryBrush");
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "border");
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetResourceReference(Border.BackgroundProperty, "GlassSurfaceBrush");
            borderFactory.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1.2));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;
            btn.Template = template;
            return btn;
        }

        private static TextBlock CreateLabel(string text, double fontSize = 14, bool bold = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontFamily = AppTheme.Font,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (System.Windows.Media.Brush)Application.Current?.Resources["TextBrush"] ?? System.Windows.Media.Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            };
        }
    }
}