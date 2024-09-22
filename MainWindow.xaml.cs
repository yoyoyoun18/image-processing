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

        /* grayIntensity = 0 -1 사이의 double 값
         * 해당 값을 grayIntensity에 동적으로 받아와 흑백의 정도를 조절할 수 있다.
         */
        private async void ApplyAdjustableGrayscale_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null) return;

            try
            {
                // 슬라이더에서 grayIntensity 값을 가져옵니다.
                double grayIntensity = GrayIntensitySlider.Value;

                var processedImage = await Task.Run(() => ApplyGrayscale(grayIntensity));

                DisplayImage.Source = processedImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying adjustable grayscale: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage ApplyGrayscale(double grayIntensity)
        {
            using var gray = new Mat();
            Cv2.CvtColor(_originalImage, gray, ColorConversionCodes.BGR2GRAY);

            using var colorMat = new Mat();
            Cv2.CvtColor(gray, colorMat, ColorConversionCodes.GRAY2BGR);

            using var result = new Mat();
            Cv2.AddWeighted(_originalImage, 1 - grayIntensity, colorMat, grayIntensity, 0, result);

            return MatToBitmapImage(result);
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

        // 추가: 슬라이더 값이 변경될 때마다 그레이스케일을 적용합니다.
        private async void GrayIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalImage == null) return;

            try
            {
                double grayIntensity = e.NewValue;
                var processedImage = await Task.Run(() => ApplyGrayscale(grayIntensity));
                DisplayImage.Source = processedImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying grayscale: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}