using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ImageProcessingByOpenCV
{
    public partial class MainWindow : Window
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
                _originalImage = new Mat(fileName);
            });

            DisplayImage.Source = _originalImage.ToBitmapSource();
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
                    return gray.ToBitmapSource();
                });

                DisplayImage.Source = grayImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying grayscale: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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