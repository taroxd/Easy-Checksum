using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using System.Text;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace CheckSumIt
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _isBusy = false;

        private bool _canStart
        {
            get => HashNamesStackPanel.Children.Any(child => ((CheckBox)child).IsChecked.GetValueOrDefault()) && !_isBusy;
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void FilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            openPicker.FileTypeFilter.Add("*");
            var files = await openPicker.PickMultipleFilesAsync();
            await ReadFileAndUpdate(files);
        }

        private async Task ReadFileAndUpdate(IEnumerable<IStorageFile> files)
        {
            resultTextBox.Text = "";
            if (files.Count() > 0 && !_isBusy)
            {
                SetBusy(true);
                var hashNames = (from child in HashNamesStackPanel.Children
                                 where ((CheckBox)child).IsChecked.GetValueOrDefault()
                                 select ((CheckBox)child).Content as string);
                var hasOnlyOneHash = hashNames.Count() == 1;

                foreach (IStorageFile file in files)
                {
                    var hashes = await Hasher.GetHashAsync(file, hashNames);
                    var lines = hashNames.Zip(hashes, (hashName, hash) => $"{hash} - {file.Name} ({hashName})");
                    resultTextBox.Text +=
                        String.Join(Environment.NewLine, lines)
                        + Environment.NewLine
                        + (hasOnlyOneHash ? "" : Environment.NewLine);
                }
                resultTextBox.Text = resultTextBox.Text.TrimEnd();
                SetBusy(false);
            }
        }

        private void HashCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateFilePickerButtonEnabled();
        }

        private void UpdateFilePickerButtonEnabled()
        {
            FilePickerButton.IsEnabled = _canStart;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = from item in items where item is IStorageFile select item as IStorageFile;
                await ReadFileAndUpdate(files);
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (_canStart)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.IsCaptionVisible = false;
                e.DragUIOverride.IsGlyphVisible = false;
            }
        }

        private void SetBusy(bool isBusy)
        {
            _isBusy = isBusy;
            UpdateFilePickerButtonEnabled();
        }
    }
}
