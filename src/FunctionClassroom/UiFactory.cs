using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FunctionClassroom
{
    internal static class UiFactory
    {
        public static readonly Brush PageBackground = Brush("#EEF2F6");
        public static readonly Brush PanelBackground = Brush("#FFFFFF");
        public static readonly Brush SoftBackground = Brush("#F4F7FA");
        public static readonly Brush BorderBrush = Brush("#D7E0EA");
        public static readonly Brush TextBrush = Brush("#1D2938");
        public static readonly Brush MutedTextBrush = Brush("#667587");
        public static readonly Brush AccentBrush = Brush("#0F5EA8");
        public static readonly Brush AccentSoftBrush = Brush("#E3F0FC");
        public static readonly Brush DangerBrush = Brush("#B42318");
        public static readonly Brush SuccessBrush = Brush("#16794A");

        public static SolidColorBrush Brush(string color)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }

        public static Button Button(string text, Action action, bool primary)
        {
            Button button = new Button();
            button.Content = text;
            button.MinHeight = 48.0;
            button.MinWidth = 64.0;
            button.Padding = new Thickness(14.0, 8.0, 14.0, 8.0);
            button.Margin = new Thickness(4.0);
            button.FontSize = 17.0;
            button.FontWeight = FontWeights.SemiBold;
            button.Foreground = primary ? Brushes.White : TextBrush;
            button.Background = primary ? AccentBrush : SoftBackground;
            button.BorderBrush = primary ? AccentBrush : BorderBrush;
            button.BorderThickness = new Thickness(1.0);
            button.Cursor = System.Windows.Input.Cursors.Hand;
            button.Focusable = true;
            button.Click += delegate
            {
                if (action != null)
                    action();
            };
            return button;
        }

        public static Button SmallButton(string text, Action action)
        {
            Button button = Button(text, action, false);
            button.FontSize = 15.0;
            button.Padding = new Thickness(10.0, 5.0, 10.0, 5.0);
            button.MinWidth = 56.0;
            button.MinHeight = 44.0;
            return button;
        }

        public static TextBlock Text(string text, double size, Brush brush)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = size;
            block.Foreground = brush;
            block.TextWrapping = TextWrapping.Wrap;
            return block;
        }

        public static Border Card(UIElement child)
        {
            Border border = new Border();
            border.Background = PanelBackground;
            border.BorderBrush = BorderBrush;
            border.BorderThickness = new Thickness(1.0);
            border.CornerRadius = new CornerRadius(8.0);
            border.Padding = new Thickness(14.0);
            border.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
            border.Child = child;
            return border;
        }
    }
}
