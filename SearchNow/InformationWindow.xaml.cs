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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Drawing;

namespace SearchNow {
    /// <summary>
    /// Interaction logic for InformationWindow.xaml
    /// </summary> 

    public partial class InformationWindow : Window {
        private Queue<InformationMessage> message_queue;
        DoubleAnimation hide_animation;
        Storyboard storyboard;
        private bool none_left = false;

        public InformationWindow() {
            InitializeComponent();
            message_queue = new Queue<InformationMessage>();

            hide_animation = new DoubleAnimation() {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };
            hide_animation.AutoReverse = true;
            Storyboard.SetTarget(hide_animation, controlsGrid);
            Storyboard.SetTargetProperty(hide_animation, new PropertyPath(Grid.OpacityProperty));

            storyboard = new Storyboard();
            storyboard.Children.Add(this.hide_animation);
        }

        private void CenterWindowOnScreen() {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Left = (screenWidth / 2) - (this.Width / 2);
            this.Top = 50;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            CenterWindowOnScreen();
        }

        public void AddMessage(InformationMessage message) {
            if ((message_queue.Count == 0) && (!this.IsVisible)) {
                //This is the first message coming!
                ShowMessage(message, false);
                closeButton.Content = "Close";
                this.Show();
            } else {
                closeButton.Content = "Next";
                message_queue.Enqueue(message);
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e) {
            switch (message_queue.Count) {
                case 0:
                    this.Hide();
                    return;
                case 1:
                    closeButton.Content = "Close";
                    break;
                default:
                    closeButton.Content = "Next";
                    break;
            }
            ShowMessage(message_queue.Dequeue());
        }

        private void exitButton_Click(object sender, RoutedEventArgs e) {
            message_queue.Clear();
            this.Hide();
        }

        private void ShowMessage(InformationMessage Message, bool Animate = true) {
            this.Activate();
            Keyboard.Focus(closeButton);

            Action change_message = () => {
                infoBlock.Text = Message.Text;
                switch(Message.Type) {
                    case MessageType.Error:
                        titleLabel.Content = "Error:";
                        iconBox.Source = SystemIcons.Error.ToImageSource();
                        break;
                    case MessageType.Info:
                        titleLabel.Content = "Consider:";
                        iconBox.Source= SystemIcons.Information.ToImageSource();
                        break;
                    case MessageType.Warn:
                        titleLabel.Content = "Warning:";
                        iconBox.Source = SystemIcons.Warning.ToImageSource();
                        break;
                }
            };

            
            if (Animate) {
                change_message.DoAfter(TimeSpan.FromMilliseconds(150));
                storyboard.Begin(this);
            } else {
                change_message();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();
            }
        }
    }

    public class InformationMessage {
        public string Text;
        public MessageType Type;
        public InformationMessage(string text, MessageType type) {
            this.Text = text;
            this.Type = type;
        }
    }

    public enum MessageType {
        Info,
        Warn,
        Error
    }
}
