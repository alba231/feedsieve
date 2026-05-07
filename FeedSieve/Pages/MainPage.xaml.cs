using FeedSieve.Models;
using FeedSieve.PageModels;

namespace FeedSieve.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}