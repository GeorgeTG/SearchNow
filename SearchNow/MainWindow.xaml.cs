using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Windows.Media.Animation;
using System.Windows.Threading;


using UnManaged;

namespace SearchNow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        DoubleAnimation show_animation, hide_animation;
        HotKey hot_key;
        SearchEngines Engines;
        Flag log_vissible = false, hide_pending = true;

        public MainWindow(){
            InitializeComponent();

            show_animation = new DoubleAnimation() {
                From = -40,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            hide_animation = new DoubleAnimation() {
                From = 0,
                To = -40,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            Storyboard.SetTarget(show_animation, this);
            Storyboard.SetTargetProperty(show_animation, new PropertyPath(Window.TopProperty));

            Storyboard.SetTarget(hide_animation, this);
            Storyboard.SetTargetProperty(hide_animation, new PropertyPath(Window.TopProperty));

            hot_key = new HotKey(Key.F, KeyModifier.Alt | KeyModifier.Ctrl, OnHotKeyPressed);

            Engines = new SearchEngines();
            Engines.MessageRecieved += Engines_MessageRecieved;
        }

        private void LogAppend(string message, MessageType type) {
            logBlock.Inlines.Add(DateTime.Now.ToString("MMM ddd d HH: mm > "));
            switch (type) {
                case MessageType.Error:
                    logBlock.Inlines.Add(new Run("[Error] ") { Foreground = Brushes.Red });
                    break;
                case MessageType.Info:
                    logBlock.Inlines.Add(new Run("[Info] ") { Foreground = Brushes.Blue });
                    break;
                case MessageType.Warn:
                    logBlock.Inlines.Add(new Run("[Warn] ") { Foreground = Brushes.Yellow });
                    break;
            }
            logBlock.Inlines.Add(message + "\n");
        }

        private void Engines_MessageRecieved(object sender, MessageEventArgs e) {
            if (e.Message.IsCommand) {
                switch (e.Message.Command) {
                    case MessageCommand.ShowLog:
                        ShowLog();
                        break;
                }
            } else {
                LogAppend(e.Message.Text, e.Message.Type);
                if (!log_vissible) {
                    ShowLog();
                    Action hide_log = () => {
                        HideLog();
                    };
                    hide_log.DoAfter(TimeSpan.FromSeconds(5));
                }
            }
        }

        void OnHotKeyPressed(HotKey hotKey) {
            if  ((this.Visibility == Visibility.Visible) && this.IsActive) {
                HideWindow();
            } else if ((this.Visibility == Visibility.Visible) && !this.IsActive) {
                this.Activate();
                Keyboard.Focus(searchTextBox);
            } else {
                ShowWindow();
            }
        }

        #region WindowFunctions
        private void ShowWindow() {
            this.Visibility = Visibility.Visible;
            Storyboard sb = new Storyboard();
            sb.Children.Add(this.show_animation);

            sb.Begin(this);

            //Get focus to our textbox
            this.Activate();
            this.Focus();
            Keyboard.Focus(searchTextBox);
        }

        private void HideWindow() {
            if (log_vissible) {
                hide_pending = true;
            } else {
                Storyboard sb = new Storyboard();
                sb.Children.Add(this.hide_animation);

                sb.Begin();
                Action hide = () => { this.Visibility = Visibility.Hidden; };
                hide.DoAfter(TimeSpan.FromMilliseconds(550));
            }
        }     

        private void CenterWindowOnScreen() {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = (screenWidth / 2) - (this.Width / 2);
            this.Top = -40;
        }

        private void ShowLog() {
            if (log_vissible.IsNotSetToggle() ) {
                this.Height = 180;
            }
        }

        private void HideLog() {
            if (log_vissible.IsSetToggle()) {
                if (hide_pending) {
                    //We have to hide the application
                    hide_pending.Toggle();
                    HideWindow();
                }
                this.Height = 40;
            }
        }
        #endregion

        void engines_menu_click(object sender, RoutedEventArgs e) {
            //Original source is the menu item
            MenuItem menu_item= (MenuItem)e.OriginalSource;
            //Header is a dictionary entry(because of source)
            string header = (string)menu_item.Header; 
            Engines.SetDefault(header);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.Visibility = Visibility.Hidden; //Hide window for now
            CenterWindowOnScreen();



            //Populate enginemenu
            ContextMenu = new ContextMenu();
            ContextMenu.ItemsSource = Engines.GetEngines();
            ContextMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(engines_menu_click));

            
            ShowWindow();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key != Key.Enter) {
                return;
            }
            SearchTextBox text_box = (SearchTextBox)sender;
            string input = text_box.Text;

            Engines.Search(input);

            HideWindow();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e) {
            Engines.Search(searchTextBox.Text);
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            Window sender_window = (Window)sender;
            sender_window.ContextMenu.IsOpen = true;
        }     
    }

    class Flag {
        public bool IsSet { get; private set; }

        public Flag(bool IsSet) {
            this.IsSet = IsSet;
        }

        public void Set() {
            this.IsSet = true;
        }

        public void UnSet() {
            this.IsSet = false;
        }

        public void Toggle() {
            this.IsSet = !this.IsSet;
        }

        public bool IsSetToggle() {
            if (IsSet) {
                this.UnSet();
                return true;
            } else {
                return false;
            }
        }

        public bool IsNotSetToggle() {
            if (!IsSet) {
                this.Set();
                return true;
            } else {
                return false;
            }
        }

        public static implicit operator bool(Flag instance) {
            //implicit cast logic
            return instance.IsSet;
        }

        public static implicit operator Flag(bool value) {
            //implicit cast logic
            return new Flag(value);
        }
    }
}
