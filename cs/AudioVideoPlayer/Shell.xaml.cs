// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Linq;
using System.Threading.Tasks;
using AudioVideoPlayer.Pages.Player;
using AudioVideoPlayer.Pages.Remote;
using AudioVideoPlayer.Pages.Playlist;
using AudioVideoPlayer.Pages.Settings;
using AudioVideoPlayer.Controls;
using AudioVideoPlayer.Models;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Activation;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using AudioVideoPlayer.Pages.About;
using AudioVideoPlayer.Pages.SignIn;

namespace AudioVideoPlayer
{
    public sealed partial class Shell
    {
        public static Shell Current { get; private set; }

        private bool _isPaneOpen;

        public bool DisplayWaitRing
        {
            set { waitRing.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public async System.Threading.Tasks.Task<bool> DisplayNetworkWarning(bool bDisplay, string Message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            () =>
            {

                if (string.IsNullOrEmpty(Message))
                    networkMessage.Text = string.Empty;
                else
                    networkMessage.Text = Message;
                networkWarning.Visibility = bDisplay ? Visibility.Visible : Visibility.Collapsed;
            });
            return true;
        }
        private async void CheckNetwork_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:network-status"));
        }
        private async void RefreshNetwork_Click(object sender, RoutedEventArgs e)
        {
            await DisplayNetworkWarning(false, string.Empty);
        }

        public Shell()
        {
            InitializeComponent();

            Current = this;
            Window.Current.Activated += Current_Activated;
            UpdateTitleBarAndColor(true);
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e)
        {
            
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
            {
                AppLogo.Opacity = 1;
                MainTitleBar.Opacity = 1;
            }
            else
            {
                AppLogo.Opacity = 0.5;
                MainTitleBar.Opacity = 0.5;
            }
        }
        public void NavigateToSample(Type pageType,Object parameter)
        {
            if (pageType != null)
            {
                NavigationFrame.Navigate(pageType,parameter);
            }
        }


        protected override async  void OnNavigatedTo(NavigationEventArgs e)
        {
            // Set Minimum size for the view
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size
            {
                Height = 240,
                Width = 320
            });
            // Hide Systray on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Hide Status bar
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }
            HamburgerMenu.ItemsSource = new[]
            {
                new MenuItem { Icon = "\xE768;", Name = "Player", PageType = typeof(AudioVideoPlayer.Pages.Player.PlayerPage) },
                new MenuItem{ Icon = "\xE8FD;", Name = "Playlist", PageType = typeof(AudioVideoPlayer.Pages.Playlist.PlaylistPage) },
                new MenuItem{ Icon = "\xE8EF;", Name = "Remote", PageType = typeof(AudioVideoPlayer.Pages.Remote.RemotePage) },
                new MenuItem{ Icon = "\xE713;", Name = "Settings", PageType = typeof(AudioVideoPlayer.Pages.Settings.SettingsPage) },

                }.ToList();

            // Options
            HamburgerMenu.OptionsItemsSource = new[]
            {
                //new MenuItem{ Icon = "\xE779;", Name = "Sign In", PageType = typeof(AudioVideoPlayer.Pages.SignIn.SignInPage) },
                new MenuItem{ Icon = "\xE897;", Name = "About", PageType = typeof(AudioVideoPlayer.Pages.About.AboutPage) }
            }.ToList();


            NavigationFrame.Navigating += NavigationFrame_Navigating;
            NavigationFrame.Navigated += NavigationFrameOnNavigated;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            ProtocolActivatedEventArgs a = e.Parameter as ProtocolActivatedEventArgs;
            if (a!=null)
                SetProtocolArgs(a.Uri);
            else
                NavigateToSample(typeof(PlayerPage), null);
        }
        private static readonly Regex _regex = new Regex(@"[?|&]([\w\.]+)=([^?|^&]+)");

        public static IReadOnlyDictionary<string, string> ParseQueryString(Uri uri)
        {
            var match = _regex.Match(uri.PathAndQuery);
            var paramaters = new Dictionary<string, string>();
            while (match.Success)
            {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }
        Type GetPageFromUri(Uri uri)
        {
            //Uri
            // testmediaapp://home/?page=playerpage
            //
            Type t = typeof(PlayerPage);
            IReadOnlyDictionary<string, string> param = ParseQueryString(uri);
            if(param!=null)
            {
                if(param.ContainsKey("page"))
                {
                    string pageName = param["page"];
                    if(!string.IsNullOrEmpty(pageName))
                    {
                        if(string.Equals(pageName,nameof(PlayerPage),StringComparison.OrdinalIgnoreCase))
                            t = typeof(PlayerPage);
                        else if (string.Equals(pageName, nameof(AboutPage), StringComparison.OrdinalIgnoreCase))
                            t = typeof(AboutPage);
                        else if (string.Equals(pageName, nameof(PlaylistPage), StringComparison.OrdinalIgnoreCase))
                            t = typeof(PlaylistPage);
                        else if (string.Equals(pageName, nameof(RemotePage), StringComparison.OrdinalIgnoreCase))
                            t = typeof(RemotePage);
                        else if (string.Equals(pageName, nameof(SettingsPage), StringComparison.OrdinalIgnoreCase))
                            t = typeof(SettingsPage);
                        else if (string.Equals(pageName, nameof(SignInPage), StringComparison.OrdinalIgnoreCase))
                            t = typeof(SignInPage);
                    }
                }
            }
            return t;
        }
        public void SetProtocolArgs(Uri uri)
        {
            if (uri != null)
            {
                Type t  = GetPageFromUri(uri);
                if ((NavigationFrame.Content == null) || (NavigationFrame.Content.GetType() != t))
                {
                    if (!string.IsNullOrWhiteSpace(uri.ToString()))
                        NavigateToSample(t, uri);
                    else
                        NavigateToSample(t, null);
                }
                else
                {
                    if (t == typeof(PlayerPage))
                    {
                        var p = NavigationFrame.Content as PlayerPage;
                        if(p!=null)
                            p.SetProtocolArgs(uri);
                    }
                }
            }

        }
        public async void SetPath(string path)
        {
            var p = NavigationFrame.Content as PlayerPage;
            if (p == null)
                NavigateToSample(typeof(AudioVideoPlayer.Pages.Player.PlayerPage), null);
            p = NavigationFrame.Content as PlayerPage;
            if (p != null)
                await p.SetPath(path);
        }
        private void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs navigationEventArgs)
        {
          // HamburgerMenu.SelectedItem = category;
           HamburgerMenu.SelectedOptionsItem = null;
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandOrCloseProperties();
        }

        private void ExpandOrCloseProperties()
        {
            var states = VisualStateManager.GetVisualStateGroups(HamburgerMenu).FirstOrDefault();
            if ((states != null) && (states.CurrentState != null))
            {
                string currentState = states.CurrentState.Name;

                switch (currentState)
                {
                    case "NarrowState":
                    case "MediumState":
                        // If pane is open, close it
                        if (_isPaneOpen)
                        {
                            _isPaneOpen = false;
                        }
                        else
                        {
                            // pane is closed, so let's open it
                            _isPaneOpen = true;
                        }

                        break;

                    case "WideState":
                        // If pane is open, close it
                        if (_isPaneOpen)
                        {
                            _isPaneOpen = false;
                        }
                        else
                        {
                            // Pane is closed, so let's open it
                            _isPaneOpen = true;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Called when [back requested] event is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="backRequestedEventArgs">The <see cref="BackRequestedEventArgs"/> instance containing the event data.</param>
        private void OnBackRequested(object sender, BackRequestedEventArgs backRequestedEventArgs)
        {
            if (backRequestedEventArgs.Handled)
            {
                return;
            }

            if (NavigationFrame.CanGoBack)
            {
                backRequestedEventArgs.Handled = true;

                NavigationFrame.GoBack();
            }
        }

        /// <summary>
        /// When the frame has navigated this method is called.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="navigationEventArgs">The <see cref="NavigationEventArgs"/> instance containing the event data.</param>
        private void NavigationFrameOnNavigated(object sender, NavigationEventArgs navigationEventArgs)
        {
//            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = NavigationFrame.CanGoBack
//                ? AppViewBackButtonVisibility.Visible
//                : AppViewBackButtonVisibility.Collapsed;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            if (_isPaneOpen)
            {
                ExpandOrCloseProperties();
            }
        }

        private void HamburgerMenu_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var appPage = e.ClickedItem as MenuItem;
            if (appPage == null)
            {
                return;
            }


            if (NavigationFrame.CurrentSourcePageType != appPage.PageType)
            {


                
                var backStack = NavigationFrame.BackStack;
                var backStackCount = backStack.Count;

                if (backStackCount > 0)
                {
                    var masterPageEntry = backStack[backStackCount - 1];
                    backStack.RemoveAt(backStackCount - 1);
                }
                NavigationFrame.Navigate(appPage.PageType);
                HamburgerMenu.IsPaneOpen = false;
                
            }
        }




        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext == null)
            {
                return;
            }
        }



        private void Shell_OnLoaded(object sender, RoutedEventArgs e)
        {
        }
        Windows.ApplicationModel.Core.CoreApplicationViewTitleBar coreTitleBar;
        public void UpdateTitleBarAndColor(bool bShowTitleBar)
        {
            Windows.UI.Color backgroundColor = ViewModelLocator.Settings.MenuBackgroundColor;
            Windows.UI.Color foregroundColor = ViewModelLocator.Settings.MenuForegroundColor;
            waitRingRectangle.Fill = new Windows.UI.Xaml.Media.SolidColorBrush(ViewModelLocator.Settings.BackgroundColor); 
            bool darkTheme = !ViewModelLocator.Settings.LightTheme;
            
            if (coreTitleBar != null)
            {
                coreTitleBar.IsVisibleChanged -= CoreTitleBar_IsVisibleChanged;
                coreTitleBar.LayoutMetricsChanged -= CoreTitleBar_LayoutMetricsChanged;
                coreTitleBar = null;
            }
            if (coreTitleBar == null)
            {
                coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
                coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            }
            coreTitleBar.ExtendViewIntoTitleBar = true;

            if (bShowTitleBar == true)
            {
                TitleBar.Background = new Windows.UI.Xaml.Media.SolidColorBrush(backgroundColor);
                MainTitleBarTextBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(foregroundColor);
                if (coreTitleBar.Height > 0)
                    TitleBar.Height = coreTitleBar.Height;
                Window.Current.SetTitleBar(MainTitleBar);
            }
            else
                Window.Current.SetTitleBar(null);

            HamburgerMenu.PaneBackground = new Windows.UI.Xaml.Media.SolidColorBrush(backgroundColor);            
            Windows.UI.ViewManagement.ApplicationViewTitleBar formattableTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonForegroundColor = foregroundColor;
            formattableTitleBar.ForegroundColor = foregroundColor;
            formattableTitleBar.ButtonHoverForegroundColor = foregroundColor;
            formattableTitleBar.ButtonInactiveForegroundColor = foregroundColor;
            formattableTitleBar.ButtonForegroundColor = foregroundColor;

            if (bShowTitleBar == false)
            {
                formattableTitleBar.BackgroundColor = Windows.UI.Colors.Transparent;
                formattableTitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                formattableTitleBar.ButtonHoverBackgroundColor = Windows.UI.Colors.Transparent;
                formattableTitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
               // AppLogo.Opacity = 0.5;
               // MainTitleBar.Opacity = 0.5;
            }
            else
            {
                formattableTitleBar.BackgroundColor = backgroundColor;
                formattableTitleBar.ButtonBackgroundColor = backgroundColor;
                formattableTitleBar.ButtonHoverBackgroundColor = backgroundColor;
                formattableTitleBar.ButtonInactiveBackgroundColor = backgroundColor;
              //  AppLogo.Opacity = 1;
              //  MainTitleBar.Opacity = 1;
            }

            if (darkTheme == true)
            {
                if (this.RequestedTheme != ElementTheme.Dark) this.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                if (this.RequestedTheme != ElementTheme.Light) this.RequestedTheme = ElementTheme.Light;
            }

        }

        private void CoreTitleBar_IsVisibleChanged(Windows.ApplicationModel.Core.CoreApplicationViewTitleBar sender, object args)
        {
            TitleBar.Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        private void CoreTitleBar_LayoutMetricsChanged(Windows.ApplicationModel.Core.CoreApplicationViewTitleBar sender, object args)
        {
            TitleBar.Height = sender.Height;
           // RightMask.Width = sender.SystemOverlayRightInset;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
