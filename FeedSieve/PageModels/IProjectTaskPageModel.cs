using CommunityToolkit.Mvvm.Input;
using FeedSieve.Models;

namespace FeedSieve.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
    bool IsBusy { get; }
}