using Avalonia.Interactivity;
using YoutubeDownloader.Framework;

namespace YoutubeDownloader.Features.Dashboard;

public partial class DownloadSingleSetupView : UserControl<DownloadSingleSetupViewModel>
{
    public DownloadSingleSetupView() => InitializeComponent();

    private void UserControl_OnLoaded(object? sender, RoutedEventArgs args) =>
        DataContext.InitializeCommand.Execute(null);
}
