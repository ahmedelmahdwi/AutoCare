using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C__project_Auto_Care.Theme
{
    public enum WMsgIcon { Info, Warning, Error, Success }

    public static class WMessageBox
    {
        public static void Show(string text, string title, WMsgIcon icon = WMsgIcon.Info, Window owner = null)
        {
            Create(text, title, icon, true, owner).ShowDialog();
        }

        public static bool Confirm(string text, string title, Window owner = null)
        {
            return Create(text, title, WMsgIcon.Warning, false, owner).ShowDialog() == true;
        }

private static Window Create(string text, string title, WMsgIcon icon, bool singleButton, Window owner)
        {
            bool showable = owner != null && owner.IsVisible;
            var win = new Window
            {
                Title = title,
                Width = 460,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = showable ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = AppTheme.ContentBgB,
                FontFamily = AppTheme.Font,
                FlowDirection = FlowDirection.RightToLeft,
                ShowInTaskbar = false,
                Owner = showable ? owner : null
            };

            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var main = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 16) };
            string glyph = "";
            Brush color = AppTheme.PrimaryB;
            switch (icon)
            {
                case WMsgIcon.Info: glyph = "\u2139"; color = AppTheme.PrimaryB; break;
                case WMsgIcon.Warning: glyph = "\u26A0"; color = AppTheme.Solid(AppTheme.AccentAmber); break;
                case WMsgIcon.Error: glyph = "\u2716"; color = AppTheme.Solid(AppTheme.AccentRed); break;
                case WMsgIcon.Success: glyph = "\u2714"; color = AppTheme.Solid(AppTheme.AccentGreen); break;
            }
            var iconBlock = new TextBlock
            {
                Text = glyph,
                Foreground = color,
                FontSize = 38,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            var msg = new TextBlock
            {
                Text = text,
                Foreground = AppTheme.BlackB,
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 360
            };
            main.Children.Add(iconBlock);
            main.Children.Add(msg);
            Grid.SetRow(main, 0);
            root.Children.Add(main);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            if (singleButton)
            {
                var ok = CreateButton("حسناً", "PrimaryGradientBrush", "PrimaryDarkBrush", 110, 38);
                ok.Click += (s, e) => win.DialogResult = true;
                btns.Children.Add(ok);
            }
            else
            {
                var yes = CreateButton("نعم", "AccentGreenBrush", "AccentGreenDarkBrush", 110, 38);
                yes.Click += (s, e) => win.DialogResult = true;
                var no = CreateButton("لا", "AccentRedBrush", "AccentRedDarkBrush", 110, 38);
                no.Click += (s, e) => win.DialogResult = false;
                yes.Margin = new Thickness(0, 0, 12, 0);
                btns.Children.Add(yes);
                btns.Children.Add(no);
            }
            Grid.SetRow(btns, 1);
            root.Children.Add(btns);

win.Content = root;
            win.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    win.DialogResult = true;
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    win.DialogResult = false;
                    e.Handled = true;
                }
            };
            return win;
        }

        private static Button CreateButton(string text, string bgBrushKey, string hoverBrushKey, double width = 110, double height = 38)
        {
            var btn = new Button
            {
                Content = text,
                Width = width,
                Height = height,
                FontFamily = AppTheme.Font,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0),
                FocusVisualStyle = null
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border), "border");
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetResourceReference(Border.BackgroundProperty, bgBrushKey);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)));

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
    }
}
