using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT.Interop;
using WinUIEx;
using Color = Windows.UI.Color;

namespace MiSTerNESPaletteEditor
{
    /// <summary>
    /// It's the main app window.
    /// </summary>
    public sealed partial class MainWindow : WindowEx, INotifyPropertyChanged
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            //this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 1024));
            //this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(1920, 1080, 1280, 1024));            
        }

        private List<PaletteColor> colors = new();
        private Boolean progressRingState = false;
        public string inputFilename = string.Empty;
        public string outputFilename = string.Empty;

        private string AssemblyVersion => "v" + typeof(MainWindow).Assembly?.GetName()?.Version?.ToString() ?? "?.?.?";

        public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = cancel,
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void PaletteLoad_Click(object sender, RoutedEventArgs e)
        {
            byte[] fileBytes = File.ReadAllBytes(inputFilename);

            colors = new List<PaletteColor>();            

            for (var i = 0; i < fileBytes.Length / 3; i++)
            {
                try
                {
                    var r = fileBytes[i * 3];
                    var g = fileBytes[(i * 3) + 1];
                    var b = fileBytes[(i * 3) + 2];
                    colors.Add(new PaletteColor { R = r, G = g, B = b, Position = i });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await ShowAlertAsync("Alert Title", $"Error loading palette: {ex.Message}");
                }
            }

            try
            {
                colorGrid.ItemsSource = colors;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await ShowAlertAsync("Alert Title", $"Error displaying palette: {ex.Message}");
            }
        }

        private void PaletteSave_Click(object sender, RoutedEventArgs e)
        {
            byte[] fileBytes = new byte[colors.Count * 3];
            for (var i = 0; i < colors.Count; i++)
            {
                fileBytes[i * 3] = colors[i].R;
                fileBytes[(i * 3) + 1] = colors[i].G;
                fileBytes[(i * 3) + 2] = colors[i].B;
            }

            // TODO: not this. add a textbox
            outputFilename = inputFilename;

            File.WriteAllBytes(outputFilename, fileBytes);
        }

        private void textBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            //RaisePropertyChanged(string.Empty);
        }

        private async void paletteBrowse_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hWnd);

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".pal");

            SetProgressRing(true);

            // Open the picker for the user to choose a palette input file
            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                inputFilename = file.Path;
                paletteLoad.IsEnabled = true;
            }
            else
            {
                paletteLoad.IsEnabled = false;
            }

            SetProgressRing(false);
        }

        private void PaletteRNG_Click(object sender, RoutedEventArgs e)
        {
            SetProgressRing(true);

            var rand = new Random();
            for (var i = 0; i < colors.Count; i++)
            {
                colors[i].R = (byte)rand.Next(0, 256);
                colors[i].G = (byte)rand.Next(0, 256);
                colors[i].B = (byte)rand.Next(0, 256);
            }

            SetProgressRing(false);
        }

        private void SetProgressRing(bool state)
        {
            progressRingState = state;
            RaisePropertyChanged(string.Empty);
        }

        private void PaletteWebSafe_Click(object sender, RoutedEventArgs e)
        {
            SetProgressRing(true);

            for (var i = 0; i < colors.Count; i++)
            {
                colors[i].R = (byte)GetNearestWebSafe(colors[i].R);
                colors[i].G = (byte)GetNearestWebSafe(colors[i].G);
                colors[i].B = (byte)GetNearestWebSafe(colors[i].B);
            }

            SetProgressRing(false);
        }

        private void RGBToHSL(int red, int green, int blue)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(red, green, blue);
            float hue = color.GetHue();
            float saturation = color.GetSaturation();
            float lightness = color.GetBrightness();
        }

        private int GetNearestWebSafe(int c)
        {
            return (int)Math.Round((c / 255.0) * 5) * 51;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

            foreach (var color in colors)
            {
                color.RaisePropertyChanged(string.Empty);
            }
        }
    }

    public class PaletteColor : INotifyPropertyChanged
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public int Position { get; set; }

        public int Index => Position + 1;

        public Color Color => Color.FromArgb(255, R, G, B);

        public Brush BrushColor => new SolidColorBrush(Color);

        public string RGB => $"({R},{G},{B})";

        public string Hex
        {
            get => $"#{R:X2}{G:X2}{B:X2}";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                var s = value.Trim();
                if (s.StartsWith("#"))
                    s = s[1..];

                if (s.Length != 6)
                    return;

                if (byte.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
                    byte.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
                    byte.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                {
                    R = r;
                    G = g;
                    B = b;

                    RaisePropertyChanged(nameof(R));
                    RaisePropertyChanged(nameof(G));
                    RaisePropertyChanged(nameof(B));
                    RaisePropertyChanged(nameof(Color));
                    RaisePropertyChanged(nameof(BrushColor));
                    RaisePropertyChanged(nameof(Hex));
                    RaisePropertyChanged(nameof(RGB));
                    RaisePropertyChanged(nameof(IndexTextColor));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public Brush IndexTextColor
        {
            get
            {
                var r = CalculateLuminanceForColor(R);
                var g = CalculateLuminanceForColor(G);
                var b = CalculateLuminanceForColor(B);

                double l = 0.2126 * r + 0.7152 * g + 0.0722 * b;

                return l > 0.179
                    ? new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                    : new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
        }

        private double CalculateLuminanceForColor(int color)
        {
            double c = color / 255.0;
            return c <= 0.04045
                ? c / 12.92
                : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
