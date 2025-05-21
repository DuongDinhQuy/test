using System;
using Microsoft.Maui.Controls;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            // Hiển thị cố định các thông số
            TemperatureLabel.Text = "33";
            HumidityLabel.Text = "75%";
            WaterLevelLabel.Text = "3.2 m";
        }
    }
}