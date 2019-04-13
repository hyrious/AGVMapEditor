using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Editor {
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window {
        public PropertiesWindow() => InitializeComponent();

        protected override void OnSourceInitialized(EventArgs e) => this.RemoveIcon();

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        public bool ShouldClose;
        private Coord Coord_;
        public Coord Coord {
            get => Coord_;
            set {
                Coord_ = null;
                X.Text = ((int)value.X).ToString();
                Y.Text = ((int)value.Y).ToString();
                I.Text = value.I.ToString();
                J.Text = value.J.ToString();
                if (value.Up != null) {
                    for (int i = 1; i < UpId.Items.Count; i++) {
                        if (Ids[i - 1] == value.Up.Id) {
                            UpId.SelectedIndex = i;
                            break;
                        }
                    }
                    UpCheck1Mode.SelectedIndex = (value.M_Up & 0b01110000) >> 4;
                    UpCheck2Mode.SelectedIndex = (value.M_Up & 0b00001110) >> 1;
                    UpPose.SelectedIndex = value.M_Up & 0b00000001;
                } else {
                    UpId.SelectedIndex = 0;
                }
                if (value.Down != null) {
                    for (int i = 1; i < DownId.Items.Count; i++) {
                        if (Ids[i - 1] == value.Down.Id) {
                            DownId.SelectedIndex = i;
                            break;
                        }
                    }
                    DownCheck1Mode.SelectedIndex = (value.M_Down & 0b01110000) >> 4;
                    DownCheck2Mode.SelectedIndex = (value.M_Down & 0b00001110) >> 1;
                    DownPose.SelectedIndex = value.M_Down & 0b00000001;
                } else {
                    DownId.SelectedIndex = 0;
                }
                if (value.Left != null) {
                    for (int i = 1; i < LeftId.Items.Count; i++) {
                        if (Ids[i - 1] == value.Left.Id) {
                            LeftId.SelectedIndex = i;
                            break;
                        }
                    }
                    LeftCheck1Mode.SelectedIndex = (value.M_Left & 0b01110000) >> 4;
                    LeftCheck2Mode.SelectedIndex = (value.M_Left & 0b00001110) >> 1;
                    LeftPose.SelectedIndex = value.M_Left & 0b00000001;
                } else {
                    LeftId.SelectedIndex = 0;
                }
                if (value.Right != null) {
                    for (int i = 1; i < RightId.Items.Count; i++) {
                        if (Ids[i - 1] == value.Right.Id) {
                            RightId.SelectedIndex = i;
                            break;
                        }
                    }
                    RightCheck1Mode.SelectedIndex = (value.M_Right & 0b01110000) >> 4;
                    RightCheck2Mode.SelectedIndex = (value.M_Right & 0b00001110) >> 1;
                    RightPose.SelectedIndex = value.M_Right & 0b00000001;
                } else {
                    RightId.SelectedIndex = 0;
                }
                Coord_ = value;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!ShouldClose) {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
            }
        }

        private List<Coord> Map;
        private int[] Ids;
        private bool Guard = false;

        public void LoadMap(List<Coord> map) {
            Map = map;
            Ids = map.Select(c => c.Id).ToArray();
            Guard = true;
            FillIds(Ids, UpId);
            FillIds(Ids, DownId);
            FillIds(Ids, LeftId);
            FillIds(Ids, RightId);
            Guard = false;
        }

        private static void FillIds(IEnumerable<int> ids, ComboBox comboBox) {
            object theBlank = comboBox.Items.GetItemAt(0);
            comboBox.Items.Clear();
            comboBox.Items.Add(theBlank);
            foreach (int id in ids) {
                comboBox.Items.Add(new ComboBoxItem { Content = id });
            }
        }

        private void X_TextChanged(object sender, TextChangedEventArgs e) {
            if (Coord_ == null) return;
            if (Int32.TryParse(X.Text, out int _x)) {
                Coord_.X = _x;
            } else {
                X.Text = ((int)Coord_.X).ToString();
            }
        }

        private void Y_TextChanged(object sender, TextChangedEventArgs e) {
            if (Coord_ == null) return;
            if (Int32.TryParse(Y.Text, out int _y)) {
                Coord_.Y = _y;
            } else {
                Y.Text = ((int)Coord_.Y).ToString();
            }
        }

        private void I_TextChanged(object sender, TextChangedEventArgs e) {
            if (Coord_ == null) return;
            if (Int16.TryParse(I.Text, out short i)) {
                Coord_.I = i;
            } else {
                I.Text = Coord_.I.ToString();
            }
        }

        private void J_TextChanged(object sender, TextChangedEventArgs e) {
            if (Coord_ == null) return;
            if (Int16.TryParse(J.Text, out short j)) {
                Coord_.J = j;
            } else {
                J.Text = Coord_.J.ToString();
            }
        }

        private void UpId_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Guard || Coord_ == null) return;
            if (UpId.SelectedItem == null || UpId.SelectedIndex == 0)
                Coord_.Up = null;
            else
                Coord_.Up = Map.Find(c => c.Id == Ids[UpId.SelectedIndex - 1]);
            if (Owner is MainWindow main) main.RefreshMap();
        }

        private void UpCheck1Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Up &= 0b00001111;
            Coord_.M_Up |= UpCheck1Mode.SelectedIndex << 4;
        }

        private void UpCheck2Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Up &= 0b01110001;
            Coord_.M_Up |= UpCheck2Mode.SelectedIndex << 1;
        }

        private void UpPose_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Up &= 0b01111110;
            Coord_.M_Up |= UpPose.SelectedIndex;
        }

        private void DownId_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Guard || Coord_ == null) return;
            if (DownId.SelectedItem == null || DownId.SelectedIndex == 0)
                Coord_.Down = null;
            else
                Coord_.Down = Map.Find(c => c.Id == Ids[DownId.SelectedIndex - 1]);
            if (Owner is MainWindow main) main.RefreshMap();
        }

        private void DownCheck1Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Down &= 0b00001111;
            Coord_.M_Down |= DownCheck1Mode.SelectedIndex << 4;
        }

        private void DownCheck2Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Down &= 0b01110001;
            Coord_.M_Down |= DownCheck2Mode.SelectedIndex << 1;
        }

        private void DownPose_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Down &= 0b01111110;
            Coord_.M_Down |= DownPose.SelectedIndex;
        }

        private void LeftId_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Guard || Coord_ == null) return;
            if (LeftId.SelectedItem == null || LeftId.SelectedIndex == 0)
                Coord_.Left = null;
            else
                Coord_.Left = Map.Find(c => c.Id == Ids[LeftId.SelectedIndex - 1]);
            if (Owner is MainWindow main) main.RefreshMap();
        }

        private void LeftCheck1Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Left &= 0b00001111;
            Coord_.M_Left |= LeftCheck1Mode.SelectedIndex << 4;
        }

        private void LeftCheck2Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Left &= 0b01110001;
            Coord_.M_Left |= LeftCheck2Mode.SelectedIndex << 1;
        }

        private void LeftPose_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Left &= 0b01111110;
            Coord_.M_Left |= LeftPose.SelectedIndex;
        }

        private void RightId_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Guard || Coord_ == null) return;
            if (RightId.SelectedItem == null || RightId.SelectedIndex == 0)
                Coord_.Right = null;
            else
                Coord_.Right = Map.Find(c => c.Id == Ids[RightId.SelectedIndex - 1]);
            if (Owner is MainWindow main) main.RefreshMap();
        }

        private void RightCheck1Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Right &= 0b00001111;
            Coord_.M_Right |= RightCheck1Mode.SelectedIndex << 4;
        }

        private void RightCheck2Mode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Right &= 0b01110001;
            Coord_.M_Right |= RightCheck2Mode.SelectedIndex << 1;
        }

        private void RightPose_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (Coord_ == null) return;
            Coord_.M_Right &= 0b01111110;
            Coord_.M_Right |= RightPose.SelectedIndex;
        }

        private void UpId_TextChanged(object sender, TextChangedEventArgs e) {
            if (UpId.SelectedIndex == -1 && UpId.Text != String.Empty) {
                MessageBox.Show("Invalid Id: Not exist");
                UpId.Text = String.Empty;
            }
        }

        private void DownId_TextChanged(object sender, TextChangedEventArgs e) {
            if (DownId.SelectedIndex == -1 && DownId.Text != String.Empty) {
                MessageBox.Show("Invalid Id: Not exist");
                DownId.Text = String.Empty;
            }
        }

        private void LeftId_TextChanged(object sender, TextChangedEventArgs e) {
            if (LeftId.SelectedIndex == -1 && LeftId.Text != String.Empty) {
                MessageBox.Show("Invalid Id: Not exist");
                LeftId.Text = String.Empty;
            }
        }

        private void RightId_TextChanged(object sender, TextChangedEventArgs e) {
            if (RightId.SelectedIndex == -1 && RightId.Text != String.Empty) {
                MessageBox.Show("Invalid Id: Not exist");
                RightId.Text = String.Empty;
            }
        }
    }
}
 