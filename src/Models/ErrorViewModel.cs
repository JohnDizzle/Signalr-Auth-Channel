using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph;

namespace AuthChannel.Models
{
    public partial class ErrorViewModel : ObservableObject
    {
        public ErrorViewModel(GraphServiceClient graphServiceClient)
        {

        }
        public ErrorViewModel() { }

        [ObservableProperty()]
        [NotifyPropertyChangedFor(nameof(ShowRequestId))]
        public string? _requestId;
        partial void OnRequestIdChanging(string? value)
        {
            ShowRequestId = !string.IsNullOrEmpty(RequestId);
        }

        [ObservableProperty]
        public bool _showRequestId;
    }
}
