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

        public Shell()
        {
            InitializeComponent();

            Current = this;
            UpdateColor(ViewModel.Settings.MenuBackgroundColor,ViewModel.Settings.DarkTheme);
        }


        public void ShowOnlyHeader(string title)
        {
            //Title.Text = title;
        }

        /// <summary>
        /// Navigates to a Sample via a deep link.
        /// </summary>
        /// <param name="deepLink">The deep link. Specified as protocol://[collectionName]?sample=[sampleName]</param>
        /// <returns>A <see cref="Task"/> representing the
        /// 
        /*
        public  Task NavigateToSampleAsync(string deepLink)
        {
             NavigateToSample(typeof(AudioVideoPlayer.Pages.Player.PlayerPage));
        }
        */
        public void NavigateToSample(Type pageType)
        {

            if (pageType != null)
            {


                NavigationFrame.Navigate(pageType);
            }
        }


        protected override  void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


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
                new MenuItem{ Icon = "\xE779;", Name = "Sign In", PageType = typeof(AudioVideoPlayer.Pages.SignIn.SignInPage) },
                new MenuItem{ Icon = "\xE897;", Name = "About", PageType = typeof(AudioVideoPlayer.Pages.About.AboutPage) }
            }.ToList();


            NavigationFrame.Navigating += NavigationFrame_Navigating;
            NavigationFrame.Navigated += NavigationFrameOnNavigated;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            if (!string.IsNullOrWhiteSpace(e?.Parameter?.ToString()))
            {
//                var parser = DeepLinkParser.Create(e.Parameter.ToString());
//                var targetSample = await Sample.FindAsync(parser.Root, parser["sample"]);
  //              if (targetSample != null)
                {
                    NavigateToSample(typeof(AudioVideoPlayer.Pages.Player.PlayerPage));
                }
            }
        }

        private  void NavigationFrame_Navigating(object sender, NavigatingCancelEventArgs navigationEventArgs)
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
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = NavigationFrame.CanGoBack
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed;

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
                NavigationFrame.Navigate(appPage.PageType);
                HamburgerMenu.IsPaneOpen = false;
                
            }
        }

        private void HamburgerMenu_OnOptionsItemClick(object sender, ItemClickEventArgs e)
        {
            var option = e.ClickedItem as MenuItem;
            if (option == null)
            {
                return;
            }


            if (NavigationFrame.CurrentSourcePageType != option.PageType)
            {
                NavigationFrame.Navigate(option.PageType);
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
        public void UpdateColor(Windows.UI.Color color, bool darkTheme)
        {
            HamburgerMenu.PaneBackground = new Windows.UI.Xaml.Media.SolidColorBrush(color);
            Windows.UI.ViewManagement.ApplicationViewTitleBar formattableTitleBar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = color;
            formattableTitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
            formattableTitleBar.ButtonHoverBackgroundColor = color;
            formattableTitleBar.ButtonInactiveBackgroundColor = color;
            formattableTitleBar.BackgroundColor = color;
            formattableTitleBar.ForegroundColor = Windows.UI.Colors.White;
            if (darkTheme == true)
            {
                if (this.RequestedTheme != ElementTheme.Dark) this.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                if (this.RequestedTheme != ElementTheme.Light) this.RequestedTheme = ElementTheme.Light;
            }

        }
    }
}
