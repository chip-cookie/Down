using Avalonia.Controls;
using Avalonia.Controls.Templates;
using YoutubeDownloader.Features.Dashboard;
using YoutubeDownloader.Features.History;
using YoutubeDownloader.Features.Main;
using YoutubeDownloader.Features.Settings;
using YoutubeDownloader.Features.Shared;
using YoutubeDownloader.Features.VideoSummary;

namespace YoutubeDownloader.Framework;

public partial class ViewManager
{
    private Control? TryCreateView(ViewModelBase viewModel) =>
        viewModel switch
        {
            MainViewModel => new MainView(),
            DashboardViewModel => new DashboardView(),
            AuthSetupViewModel => new AuthSetupView(),
            DownloadMultipleSetupViewModel => new DownloadMultipleSetupView(),
            DownloadSingleSetupViewModel => new DownloadSingleSetupView(),
            MessageBoxViewModel => new MessageBoxView(),
            SettingsViewModel => new SettingsView(),
            _ => null,
        };

    public Control? TryBindView(ViewModelBase viewModel)
    {
        var view = TryCreateView(viewModel);
        if (view is null)
            return null;

        view.DataContext ??= viewModel;

        return view;
    }
}

public partial class ViewManager : IDataTemplate
{
    bool IDataTemplate.Match(object? data) => data is ViewModelBase;

    Control? ITemplate<object?, Control?>.Build(object? data) =>
        data is ViewModelBase viewModel ? TryBindView(viewModel) : null;
}
