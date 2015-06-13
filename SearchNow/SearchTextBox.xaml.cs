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
using System.Windows.Controls.Primitives;

namespace SearchNow{

    public partial class SearchTextBox :UserControl {

        List<string> history_list;
        int current_index = 0;

        public string Text {
            set {
                searchBox.Text = value;
            }
            get {
                if(searchBox.Text  == String.Empty && history_list.Count > 0) {
                    return history_list[1];
                }
                return searchBox.Text;
            }
        }

        new public bool IsFocused {
            get {
                return searchBox.IsFocused;
            }
        }
        new public bool IsKeyboardFocused {
            get {
                return searchBox.IsKeyboardFocused;
            }
        }

        public SearchTextBox() {
            InitializeComponent();
            history_list = new List<string>();
            history_list.Add("");

            searchBox.KeyUp += SearchBox_KeyUp;
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Up:
                    if ((current_index + 1).IsWithinRange(0, history_list.Count)) {
                        if (current_index == 0) {
                            history_list[0] = searchBox.Text;
                        }
                        searchBox.Text = history_list[++current_index];
                        searchBox.CaretIndex = searchBox.Text.Length;
                    }
                    break;
                case Key.Down:
                    if ((current_index - 1).IsWithinRange(0, history_list.Count)) {
                        searchBox.Text = history_list[--current_index];
                        searchBox.CaretIndex = searchBox.Text.Length;
                    }
                    break;
                case Key.Enter:
                    if (history_list.Contains(searchBox.Text)) {
                        history_list.Remove(searchBox.Text);
                        current_index = 0;
                    }
                    history_list.Insert(1, searchBox.Text);
                    searchBox.Clear();
                    break;
            }
        }

        private void UserControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            Keyboard.Focus(searchBox);
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e) {
            searchBox.Focus();
        }
    }
}
