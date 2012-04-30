﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace App
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            App thisApp = App.Current as App;
            if (thisApp.Model.SongsOnDevice == null)
            {
                result.Text = "null";
            }
            else
            {
                result.Text = thisApp.Model.SongsOnDevice.Count.ToString();
            }

        }
    }
}