using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingByOpenCV
{
    public partial class MainWindow : System.Windows.Window
    {
        private Mat _originalImage;
        private Mat _processedImage;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    await LoadImageAsync(openFileDialog.FileName);
                    await ApplyFiltersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadImageAsync(string fileName)
        {
            await Task.Run(() =>
            {
                DisposeImages();
                _originalImage = Cv2.ImRead(fileName);
                _processedImage = _originalImage.Clone();
            });

            await UpdateDisplayImageAsync();
        }

        private async void GrayIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplyFiltersAsync();
        }

        private async void BlurIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                await Task.Run(() =>
                {
                    _processedImage = _originalImage.Clone();
                    ApplyGrayscale();
                    ApplyGaussianBlur();
                });

                await UpdateDisplayImageAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void ApplyGrayscale()
        {
            double grayIntensity = 0;
            Dispatcher.Invoke(() => grayIntensity = GrayIntensitySlider.Value);
            if (grayIntensity == 0) return;

            using var gray = new Mat();
            Cv2.CvtColor(_originalImage, gray, ColorConversionCodes.BGR2GRAY);
            using var colorMat = new Mat();
            Cv2.CvtColor(gray, colorMat, ColorConversionCodes.GRAY2BGR);
            Cv2.AddWeighted(_originalImage, 1 - grayIntensity, colorMat, grayIntensity, 0, _processedImage);
        }

        private void ApplyGaussianBlur()
        {
            int blurIntensity = 0;
            Dispatcher.Invoke(() => blurIntensity = (int)BlurIntensitySlider.Value);
            if (blurIntensity == 0) return;

            int kernelSize = blurIntensity * 2 + 1;
            Cv2.GaussianBlur(_processedImage, _processedImage, new OpenCvSharp.Size(kernelSize, kernelSize), 0);
        }

        private async Task UpdateDisplayImageAsync()
        {
            if (_processedImage == null) return;

            BitmapSource bitmap = null;
            await Task.Run(() =>
            {
                using var stream = _processedImage.ToMemoryStream(".png");
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                bitmap = bitmapImage;
            });

            await Dispatcher.InvokeAsync(() =>
            {
                DisplayImage.Source = bitmap;
            });
        }

        private void DisposeImages()
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            DisposeImages();
            base.OnClosed(e);
        }
    }
}