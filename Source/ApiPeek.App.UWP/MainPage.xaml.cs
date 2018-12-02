using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiPeek.Service;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace ApiPeek.App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            StorageFile peekFile;
            try
            {
                peekFile = await Task.Run(() => ApiPeekService.PeekAndLog().AsTask());
            }
            catch (Exception ex)
            {
                await new MessageDialog("Error in ApiPeekService: " + ex).ShowAsync();
                return;
            }
            finally
            {
                StartButton.IsEnabled = true;
            }

            if (peekFile == null)
            {
                await new MessageDialog("Error when gathering Api data!").ShowAsync();
                return;
            }

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("API Peek result", new List<string> { ".zip" });
            savePicker.SuggestedFileName = peekFile.Name;

#if WINDOWS_APP || WINDOWS_UWP
            StorageFile saveFile = await savePicker.PickSaveFileAsync();
            if (saveFile != null)
            {
                await peekFile.CopyAndReplaceAsync(saveFile);
                await new MessageDialog("Done!").ShowAsync();
            }
#elif WINDOWS_PHONE_APP
            App.SavedFile = peekFile;
            savePicker.PickSaveFileAndContinue();
#endif
        }
    }
}