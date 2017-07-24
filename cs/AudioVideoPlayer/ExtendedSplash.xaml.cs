// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace AudioVideoPlayer
{
    partial class ExtendedSplash 
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame rootFrame;

        private SplashScreen splash; // Variable to hold the splash screen object.
        private double ScaleFactor; //Variable to hold the device scale factor (use to determine phone screen resolution)
        CoreDispatcher dispatcher;
        public ExtendedSplash(SplashScreen splashscreen, bool loadState)
        {
            InitializeComponent();
            dispatcher = Window.Current.Dispatcher;
            // Set Minimum size for the view
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size
            {
                Height = 240,
                Width = 320
            });

            ViewModels.ViewModel vm = new ViewModels.ViewModel();
            this.Background = new Windows.UI.Xaml.Media.SolidColorBrush(vm.Settings.MenuBackgroundColor);
            
            progressRing.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(vm.Settings.MenuForegroundColor);

            // Listen for window resize events to reposition the extended splash screen image accordingly.
            // This is important to ensure that the extended splash screen is formatted properly in response to snapping, unsnapping, rotation, etc...
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(ExtendedSplash_OnResize);

            ScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            splash = splashscreen;

            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                //splashImageRect = splash.ImageLocation;
                //PositionImage();
            }

            // Create a Frame to act as the navigation context
            rootFrame = new Frame();
            this.Loaded += ExtendedSplash_Loaded;
            // Update Title bar
            waitRing.Visibility = Visibility.Visible;
            Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
           
            Windows.UI.ViewManagement.ApplicationViewTitleBar formattableTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            if(formattableTitleBar!=null)
            {
                

                formattableTitleBar.BackgroundColor = vm.Settings.MenuBackgroundColor;
                formattableTitleBar.ForegroundColor = vm.Settings.MenuForegroundColor;

                formattableTitleBar.InactiveBackgroundColor = vm.Settings.MenuBackgroundColor;
                formattableTitleBar.InactiveForegroundColor = vm.Settings.MenuForegroundColor;


                formattableTitleBar.ButtonBackgroundColor = vm.Settings.MenuBackgroundColor;
                formattableTitleBar.ButtonHoverBackgroundColor = vm.Settings.MenuBackgroundColor;
                formattableTitleBar.ButtonInactiveBackgroundColor = vm.Settings.MenuBackgroundColor;
                formattableTitleBar.ButtonPressedBackgroundColor = vm.Settings.MenuBackgroundColor;

                formattableTitleBar.ButtonForegroundColor = vm.Settings.MenuForegroundColor;
                formattableTitleBar.ButtonHoverForegroundColor = vm.Settings.MenuForegroundColor;
                formattableTitleBar.ButtonInactiveForegroundColor = vm.Settings.MenuForegroundColor;
                formattableTitleBar.ButtonPressedForegroundColor = vm.Settings.MenuForegroundColor;
                                  

            }
        }



        private async void ExtendedSplash_Loaded(object sender, RoutedEventArgs e)
        {

            await Task.Delay(TimeSpan.FromSeconds(5));
            // Navigate to mainpage
            rootFrame.Navigate(typeof(Shell));
            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
            // Hide Systray on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Hide Status bar
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }
        }


        // Position the extended splash screen image in the same location as the system splash screen image.
        void PositionImage()
        {
            extendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.Left);
            extendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Top);
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                extendedSplashImage.Height = splashImageRect.Height / (ScaleFactor*2);
                extendedSplashImage.Width = splashImageRect.Width / (ScaleFactor*2);
            }
            else
            {
                extendedSplashImage.Height = splashImageRect.Height;
                extendedSplashImage.Width = splashImageRect.Width;
            }
        }

        void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
               // splashImageRect = splash.ImageLocation;
               // PositionImage();
            }
        }



        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;

            // Navigate away from the app's extended splash screen after completing setup operations here...
            // This sample navigates away from the extended splash screen when the "Learn More" button is clicked.

        }
    }
}