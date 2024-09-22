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
                DisposeCurrentImage();
                _originalImage = Cv2.ImRead(fileName);
            });

            DisplayImage.Source = MatToBitmapImage(_originalImage);
        }

        private async void ApplyGrayscale_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null) return;

            try
            {
                var grayImage = await Task.Run(() =>
                {
                    using var gray = new Mat();
                    Cv2.CvtColor(_originalImage, gray, ColorConversionCodes.BGR2GRAY);
                    return MatToBitmapImage(gray);
                });

                DisplayImage.Source = grayImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying grayscale: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage MatToBitmapImage(Mat mat)
        {
            byte[] imageData;
            Cv2.ImEncode(".png", mat, out imageData);

            using var memoryStream = new MemoryStream(imageData);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // 다른 스레드에서 사용할 수 있게 합니다.

            return bitmapImage;
        }

        private void DisposeCurrentImage()
        {
            _originalImage?.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            DisposeCurrentImage();
            base.OnClosed(e);
        }
    }
}