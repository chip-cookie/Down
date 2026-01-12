using Avalonia.Interactivity;
using YoutubeDownloader.Framework;

namespace YoutubeDownloader.Features.Dashboard;

public partial class DownloadMultipleSetupView : UserControl<DownloadMultipleSetupViewModel>
{
    public DownloadMultipleSetupView() => InitializeComponent();

    private void UserControl_OnLoaded(object? sender, RoutedEventArgs args) =>
        DataContext.InitializeCommand.Execute(null);
}
