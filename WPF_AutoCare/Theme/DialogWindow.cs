using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace C__project_Auto_Care.Theme
{
    /// <summary>
    /// Base modal window providing a modern glass header, drag-to-move, and escape-to-close behavior.
    /// Fully independent of legacy procedural UI generation.
    /// </summary>
    public class DialogWindow : Window
    {
        public DockPanel Body { get; private set; }

        public DialogWindow(string title, double width, double height)
        {
            Title = title;
            Width = width;
            Height = height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            FontFamily = AppTheme.Font;
            FlowDirection = FlowDirection.RightToLeft;
            WindowStyle = WindowStyle.None;
            Background = AppTheme.ContentBgB;
            SetResourceReference(Window.BackgroundProperty, "BackgroundBrush");

            var root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var bar = CreateTitleBar(this, title);
            root.Children.Add(bar);

            Body = new DockPanel { Margin = new Thickness(0) };
            var scroll = new ScrollViewer
            {
                Content = Body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Focusable = false,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };
            System.Windows.Controls.Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            Content = root;

            KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape && !e.Handled)
                {
                    Close();
                    e.Handled = true;
                }
            };

            Loaded += (s, e) => CenterAndFitToScreen(this);
        }

        public static Border CreateTitleBar(Window win, string title)
        {
            var bar = new Border
            {
                Height = 52,
                Padding = new Thickness(12, 0, 12, 0)
            };
            bar.SetResourceReference(Border.BackgroundProperty, "PrimaryGradientBrush");

            var dock = new DockPanel();

            var close = new Button
            {
                Width = 34,
                Height = 34,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                VerticalAlignment = VerticalAlignment.Center
            };

            var closeTemplate = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "closeBd";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetValue(TextBlock.TextProperty, "\u2715");
            textFactory.SetValue(TextBlock.FontFamilyProperty, AppTheme.Font);
            textFactory.SetValue(TextBlock.FontSizeProperty, 15.0);
            textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            textFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(textFactory);
            closeTemplate.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, AppTheme.Solid(Color.FromRgb(239, 68, 68)), "closeBd"));
            closeTemplate.Triggers.Add(hoverTrigger);
            close.Template = closeTemplate;

            close.Click += (s, e) =>
            {
                if (win != null) win.Close();
            };
            DockPanel.SetDock(close, Dock.Left);
            dock.Children.Add(close);

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = AppTheme.Font,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleBlock, Dock.Right);
            dock.Children.Add(titleBlock);

            bar.Child = dock;
            bar.MouseLeftButtonDown += (s, e) =>
            {
                try { if (win != null && e.ButtonState == MouseButtonState.Pressed) win.DragMove(); }
                catch { }
            };
            return bar;
        }

        public static void CenterAndFitToScreen(Window win)
        {
            try
            {
                var wa = SystemParameters.WorkArea;
                if (wa.Width < 200 || wa.Height < 200)
                    return;

                if (win.Width > wa.Width * 0.98)
                    win.Width = wa.Width * 0.98;
                if (win.Height > wa.Height * 0.98)
                    win.Height = wa.Height * 0.98;

                win.WindowStartupLocation = WindowStartupLocation.Manual;
                win.UpdateLayout();
                win.Left = wa.Left + Math.Max(0, (wa.Width - win.Width) / 2);
                win.Top = wa.Top + Math.Max(0, (wa.Height - win.Height) / 2);
            }
            catch
            {
            }
        }
    }
}
