﻿using System;

using Demoo.ViewModels;

using Windows.UI.Xaml.Controls;

namespace Demoo.Views
{
    // TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private SettingsViewModel ViewModel
        {
            get { return DataContext as SettingsViewModel; }
        }
    }
}
