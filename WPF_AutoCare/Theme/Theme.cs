using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace C__project_Auto_Care.Theme
{
    public static class AppTheme
    {
        public const string HeaderFontName = "HasanAyaW23-Regular";
        public const string BodyFontName = "Segoe UI";
        public const string FallbackFontName = "Tahoma";

        public static FontFamily HeaderFont => new FontFamily("HasanAyaW23-Regular, Tahoma, Segoe UI");
        public static FontFamily BodyFont => new FontFamily("Segoe UI, Tahoma, Arial");

        public static string CustomFontName => BodyFontName;
        public static string FontName => BodyFontName;
        public static FontFamily Font => BodyFont;

        public static Color C(byte r, byte g, byte b)
        {
            return Color.FromRgb(r, g, b);
        }

        public static Brush Solid(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static Brush Solid(byte r, byte g, byte b)
        {
            return Solid(C(r, g, b));
        }

        public static Brush Linear(Color c1, Color c2, double angle)
        {
            var b = new LinearGradientBrush(c1, c2, angle);
            b.Freeze();
            return b;
        }

        // ── Light Crystal Liquid Glass Palette (Base: #0284C7 | RGB: 2, 132, 199) ──
        // Base primary liquid azure
        public static readonly Color PrimaryColor      = C(0x02, 0x84, 0xC7); // #0284c7 — base liquid azure

        // Light liquid variations (crystal tints)
        public static readonly Color PrimaryLight1     = C(0x38, 0xBD, 0xF8); // #38bdf8 — luminous liquid cyan glow
        public static readonly Color PrimaryLight2     = C(0x7D, 0xD3, 0xFC); // #7dd3fc — frosted ice tint
        public static readonly Color PrimaryLight3     = C(0xBA, 0xE6, 0xFD); // #bae6fd — translucent water highlight
        public static readonly Color PrimaryTintLight  = C(0xE0, 0xF2, 0xFE); // #e0f2fe — soft droplet sheen
        public static readonly Color PrimaryTintSoft   = C(0xF0, 0xF9, 0xFF); // #f0f9ff — pure water surface tint

        // Named aliases for task specifications
        public static readonly Color PrimaryLightColor = PrimaryLight1;
        public static readonly Color PrimaryDarkColor  = C(0x03, 0x69, 0xA1); // #0369a1

        // Dark variations (oceanic depth)
        public static readonly Color PrimaryDark1      = C(0x03, 0x69, 0xA1); // #0369a1 — deep water shading
        public static readonly Color PrimaryDark2      = C(0x07, 0x59, 0x85); // #075985 — ocean blue
        public static readonly Color PrimaryDark3      = C(0x0C, 0x4A, 0x6E); // #0c4a6e — midnight aquatic depth

        // Legacy aliases
        public static readonly Color CyanColor         = PrimaryLight1;
        public static readonly Color LightBlueColor    = PrimaryLight3;

        // Sidebar stops
        public static readonly Color SidebarDarkColor  = C(0x07, 0x17, 0x24);
        public static readonly Color SidebarLightColor = C(0x0F, 0x28, 0x3C);

        // Content / surface
        public static readonly Color ContentBgColor    = C(0xEA, 0xF0, 0xF6); // #eaf0f6 soothing canvas background
        public static readonly Color CardTopColor      = PrimaryColor;
        public static readonly Color SurfaceColor      = Colors.White;
        public static readonly Color TextColor         = C(0x0F, 0x17, 0x2A); // #0f172a slate-navy crisp readability
        public static readonly Color TextMutedColor    = C(0x47, 0x55, 0x69); // #475569 muted slate
        public static readonly Color BorderColor       = C(0xC8, 0xD7, 0xE6); // #c8d7e6 soft, distinct border
        public static readonly Color GridLineColor     = C(0xDE, 0xE8, 0xF2); // #dee8f2
        public static readonly Color RowOddColor       = C(0xF4, 0xF8, 0xFC); // #f4f8fc

        // ── Accent Colors (Luminous Liquid Gems) ──────────────────────────────────
        public static readonly Color AccentColor       = C(0xF5, 0x9E, 0x0B); // #f59e0b liquid amber
        public static readonly Color AccentAmber       = C(0xF5, 0x9E, 0x0B); // #f59e0b
        public static readonly Color AccentGreen       = C(0x10, 0xB9, 0x81); // #10b981 liquid emerald / mint
        public static readonly Color AccentRed         = C(0xF4, 0x3F, 0x5E); // #f43f5e liquid coral / rose
        public static readonly Color AccentPurple      = C(0x63, 0x66, 0xF1); // #6366f1 liquid iris / violet

        // Card Background Tints (Translucent Liquid Infusions)
        public static readonly Color CardBgAmber       = C(0xFF, 0xFB, 0xEB);
        public static readonly Color CardBgPrimary     = C(0xF0, 0xF9, 0xFF);
        public static readonly Color CardBgGreen       = C(0xEC, 0xFD, 0xF5);
        public static readonly Color CardBgPurple      = C(0xEE, 0xF2, 0xFF);
        public static readonly Color CardBgCyan        = C(0xEC, 0xFE, 0xFF);

        // ── Brushes ──────────────────────────────────────────────────────────────
        public static readonly Brush PrimaryB          = Solid(PrimaryColor);
        public static readonly Brush PrimaryLightB     = Solid(PrimaryLight1);
        public static readonly Brush PrimaryLight2B    = Solid(PrimaryLight2);
        public static readonly Brush PrimaryLight3B    = Solid(PrimaryLight3);
        public static readonly Brush PrimaryDarkB      = Solid(PrimaryDark1);
        public static readonly Brush PrimaryDark2B     = Solid(PrimaryDark2);
        public static readonly Brush PrimaryDark3B     = Solid(PrimaryDark3);

        public static readonly Brush CyanB             = Solid(CyanColor);
        public static readonly Brush LightBlueB        = Solid(LightBlueColor);
        public static readonly Brush ContentBgB        = Solid(ContentBgColor);
        public static readonly Brush WhiteB            = Brushes.White;
        public static readonly Brush BlackB            = Solid(TextColor);
        public static readonly Brush GrayB             = Solid(TextMutedColor);
        public static readonly Brush TextMutedB        = Solid(TextMutedColor);
        public static readonly Brush BorderB           = Solid(BorderColor);

        public static readonly Brush RowEvenBrush      = Brushes.White;
        public static readonly Brush RowOddBrush       = Solid(RowOddColor);

        // DataGrid header: crisp liquid azure gradient
        public static readonly Brush GridHeaderBrush   = Linear(PrimaryColor, PrimaryDark1, 0);

        // ── Named Gradient Brushes ─────────────────────────────────────────────────
        // Primary Liquid Glass Gradient: Luminous cyan highlight to azure body to oceanic base
        public static readonly Brush PrimaryGradientBrush = MakePrimaryGradient();

        private static Brush MakePrimaryGradient()
        {
            var g = new LinearGradientBrush();
            g.StartPoint = new System.Windows.Point(0, 0);
            g.EndPoint   = new System.Windows.Point(1, 1);
            g.GradientStops.Add(new GradientStop(PrimaryLight1, 0.0));  // #38bdf8
            g.GradientStops.Add(new GradientStop(PrimaryColor,  0.5));  // #0284c7
            g.GradientStops.Add(new GradientStop(PrimaryDark1,  1.0));  // #0369a1
            g.Freeze();
            return g;
        }

        // Sidebar gradient: deep oceanic crystal glass
        public static readonly Brush SidebarBrush       = MakeNavBarGradient();
        public static readonly Brush SidebarButtonBrush = Solid(C(0x0F, 0x28, 0x3C));
        public static readonly Brush GradientNavBar     = MakeNavBarGradient();

        private static Brush MakeNavBarGradient()
        {
            var g = new LinearGradientBrush();
            g.StartPoint = new System.Windows.Point(0, 0);
            g.EndPoint   = new System.Windows.Point(0, 1);
            g.GradientStops.Add(new GradientStop(C(0x0F, 0x28, 0x3C), 0.0)); // #0F283C
            g.GradientStops.Add(new GradientStop(C(0x0B, 0x20, 0x32), 0.5)); // #0B2032
            g.GradientStops.Add(new GradientStop(C(0x07, 0x17, 0x24), 1.0)); // #071724
            g.Freeze();
            return g;
        }

        // Button gradient family (Liquid gems)
        public static readonly Brush GradientAmber      = Linear(C(0xFB, 0xBF, 0x24), AccentAmber, 90);
        public static readonly Brush GradientGreen      = Linear(C(0x34, 0xD3, 0x99), AccentGreen, 90);
        public static readonly Brush GradientBlue       = MakePrimaryGradient();
        public static readonly Brush GradientRed        = Linear(C(0xFB, 0x71, 0x85), AccentRed, 90);
        public static readonly Brush GradientPurple     = Linear(C(0x81, 0x8C, 0xF8), AccentPurple, 90);

        // ── Liquid Glassmorphism Brushes ──────────────────────────────────────────
        public static readonly Brush GlassSurfaceBrush       = Solid(Color.FromArgb(230, 255, 255, 255));
        public static readonly Brush GlassCardBrush          = Solid(Color.FromArgb(244, 255, 255, 255));
        public static readonly Brush GlassBorderBrush        = Solid(Color.FromArgb(128, 186, 230, 253));
        public static readonly Brush GlassBorderSubtle       = Solid(Color.FromArgb(64, 186, 230, 253));
        public static readonly Brush GlassDarkCardBrush      = Solid(Color.FromArgb(204, 10, 34, 52));
        public static readonly Brush GlassDarkBorderBrush    = Solid(Color.FromArgb(64, 56, 189, 248));

        // ── Central Theme Initializer ─────────────────────────────────────────────
        public static void InitializeTheme(Application app)
        {
            if (app == null) return;

            try
            {
                // 1. Attempt to load Colors.xaml from file system if present
                string[] candidatePaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Colors.xaml"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Theme", "Colors.xaml"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Theme", "Colors.xaml")
                };

                ResourceDictionary dict = null;
                foreach (var path in candidatePaths)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            using (var fs = File.OpenRead(path))
                            {
                                dict = (ResourceDictionary)XamlReader.Load(fs);
                                break;
                            }
                        }
                    }
                    catch { }
                }

                if (dict != null)
                {
                    app.Resources.MergedDictionaries.Add(dict);
                }
            }
            catch { }

            // 2. Register all named resources in app.Resources to guarantee they are always available
            RegisterResource(app, "PrimaryColor", PrimaryColor);
            RegisterResource(app, "PrimaryLightColor", PrimaryLight1);
            RegisterResource(app, "PrimaryLight1Color", PrimaryLight1);
            RegisterResource(app, "PrimaryLight2Color", PrimaryLight2);
            RegisterResource(app, "PrimaryLight3Color", PrimaryLight3);
            RegisterResource(app, "PrimaryDarkColor", PrimaryDark1);
            RegisterResource(app, "PrimaryDark1Color", PrimaryDark1);
            RegisterResource(app, "PrimaryDark2Color", PrimaryDark2);
            RegisterResource(app, "PrimaryDark3Color", PrimaryDark3);
            RegisterResource(app, "AccentColor", AccentColor);
            RegisterResource(app, "AccentAmberColor", AccentAmber);
            RegisterResource(app, "AccentGreenColor", AccentGreen);
            RegisterResource(app, "AccentRedColor", AccentRed);
            RegisterResource(app, "AccentPurpleColor", AccentPurple);
            RegisterResource(app, "BackgroundColor", ContentBgColor);
            RegisterResource(app, "SurfaceColor", SurfaceColor);
            RegisterResource(app, "TextColor", TextColor);
            RegisterResource(app, "TextMutedColor", TextMutedColor);
            RegisterResource(app, "BorderColor", BorderColor);

            RegisterResource(app, "PrimaryBrush", PrimaryB);
            RegisterResource(app, "PrimaryColorBrush", PrimaryB);
            RegisterResource(app, "PrimaryLightBrush", PrimaryLightB);
            RegisterResource(app, "PrimaryLightColorBrush", PrimaryLightB);
            RegisterResource(app, "PrimaryLight2Brush", PrimaryLight2B);
            RegisterResource(app, "PrimaryLight3Brush", PrimaryLight3B);
            RegisterResource(app, "PrimaryDarkBrush", PrimaryDarkB);
            RegisterResource(app, "PrimaryDarkColorBrush", PrimaryDarkB);
            RegisterResource(app, "PrimaryDark2Brush", PrimaryDark2B);
            RegisterResource(app, "PrimaryDark3Brush", PrimaryDark3B);

            RegisterResource(app, "AccentBrush", Solid(AccentColor));
            RegisterResource(app, "AccentColorBrush", Solid(AccentColor));
            RegisterResource(app, "AccentAmberBrush", Solid(AccentAmber));
            RegisterResource(app, "AccentGreenBrush", Solid(AccentGreen));
            RegisterResource(app, "AccentRedBrush", Solid(AccentRed));
            RegisterResource(app, "AccentPurpleBrush", Solid(AccentPurple));

            RegisterResource(app, "BackgroundBrush", ContentBgB);
            RegisterResource(app, "BackgroundColorBrush", ContentBgB);
            RegisterResource(app, "SurfaceBrush", WhiteB);
            RegisterResource(app, "SurfaceColorBrush", WhiteB);
            RegisterResource(app, "TextBrush", BlackB);
            RegisterResource(app, "TextColorBrush", BlackB);
            RegisterResource(app, "TextMutedBrush", GrayB);
            RegisterResource(app, "BorderBrush", BorderB);

            RegisterResource(app, "PrimaryGradientBrush", PrimaryGradientBrush);
            RegisterResource(app, "NavBarGradientBrush", SidebarBrush);
            RegisterResource(app, "DataGridHeaderBrush", GridHeaderBrush);
            RegisterResource(app, "DataGridRowEvenBrush", RowEvenBrush);
            RegisterResource(app, "DataGridRowOddBrush", RowOddBrush);
            RegisterResource(app, "DataGridRowSelectedBrush", PrimaryLight3B);

            // ── Liquid Glassmorphism Design Tokens ──
            RegisterResource(app, "GlassSurfaceBrush", GlassSurfaceBrush);
            RegisterResource(app, "GlassCardBrush", GlassCardBrush);
            RegisterResource(app, "GlassBorderBrush", GlassBorderBrush);
            RegisterResource(app, "GlassBorderSubtleBrush", GlassBorderSubtle);
            RegisterResource(app, "GlassInputBgBrush", Solid(Color.FromArgb(0xF8, 0xF8, 0xFC, 0xFE)));
            RegisterResource(app, "GlassDarkCardBrush", GlassDarkCardBrush);
            RegisterResource(app, "GlassDarkBorderBrush", GlassDarkBorderBrush);
            RegisterResource(app, "GlassHeaderGradientBrush", Linear(Color.FromArgb(0xF8, 255, 255, 255), Color.FromArgb(0xEE, 0xE8, 0xF4, 0xF9), 0));

            // 3. System highlight colors in teal
            try
            {
                app.Resources[SystemColors.HighlightBrushKey] = PrimaryLight3B;
                app.Resources[SystemColors.HighlightTextBrushKey] = BlackB;
                app.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = Solid(PrimaryTintSoft);
                app.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = BlackB;
            }
            catch { }
        }

        private static void RegisterResource(Application app, string key, object value)
        {
            try
            {
                app.Resources[key] = value;
            }
            catch { }
        }
    }
}