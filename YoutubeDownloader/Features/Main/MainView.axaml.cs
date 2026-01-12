using Avalonia.Interactivity;
using YoutubeDownloader.Framework;

namespace YoutubeDownloader.Features.Main;

public partial class MainView : Window<MainViewModel>
{
    public MainView() => InitializeComponent();

    private void DialogHost_OnLoaded(object? sender, RoutedEventArgs args) =>
        DataContext.InitializeCommand.Execute(null);
}
