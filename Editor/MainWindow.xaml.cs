using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiteDB;
using Microsoft.Win32;

namespace Editor {
    internal static class NativeMethods {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static void RemoveIcon(this Window window) {
            window.Icon = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4);
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            SendMessage(hwnd, 0x0080, new IntPtr(1), IntPtr.Zero);
            SendMessage(hwnd, 0x0080, IntPtr.Zero, IntPtr.Zero);
            int extendedStyle = GetWindowLong(hwnd, -20);
            SetWindowLong(hwnd, -20, extendedStyle | 0x0001);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0004 | 0x0020);
        }
    }

    public class Coord {
        public static int Id_;
        private static TextBlock MakeDefaultLabel() => new TextBlock {
            FontFamily = new FontFamily("Segoe UI"),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        private static Ellipse MakeDefaultDot() => new Ellipse {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Width = 10, Height = 10
        };
        private static Line MakeDefaultLine() => new Line {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Visibility = Visibility.Hidden
        };
        private static Polyline MakeDefaultArrow() => new Polyline {
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };

        public int Id { get; set; } = ++Id_;
        public double X { get; set; }
        public double Y { get; set; } // for convenience, still saved as int
        public int I { get; set; }
        public int J { get; set; } // PositionBarcode, you know
        public Coord Up, Down, Left, Right; // better than an iced UpId
        public int UpId_ { get; set; }
        public int DownId_ { get; set; }
        public int LeftId_ { get; set; }
        public int RightId_ { get; set; }
        public int UpId => Up == null ? UpId_ : Up.Id;
        public int DownId => Down == null ? DownId_ : Down.Id;
        public int LeftId => Left == null ? LeftId_ : Left.Id;
        public int RightId => Right == null ? RightId_ : Right.Id;
        public int M_Up { get; set; }
        public int M_Down { get; set; }
        public int M_Left { get; set; }
        public int M_Right { get; set; } // properties for each direction
        // [0|000|000|0]: 7: not use, 6-4: Check1Mode, 3-1: Check2Mode, 0: Pose

        public static string ToTableSql => $@"
            create table if not exists coords (
                id                integer primary key,
                x                 integer default 0,
                y                 integer default 0,
                position_barcode  integer default 0,
                up_point_id       integer default 0,
                up_check1_mode    integer default 0,
                up_check2_mode    integer default 0,
                up_pose           integer default 0,
                down_point_id     integer default 0,
                down_check1_mode  integer default 0,
                down_check2_mode  integer default 0,
                down_pose         integer default 0,
                left_point_id     integer default 0,
                left_check1_mode  integer default 0,
                left_check2_mode  integer default 0,
                left_pose         integer default 0,
                right_point_id    integer default 0,
                right_check1_mode integer default 0,
                right_check2_mode integer default 0,
                right_pose        integer default 0
            );
        ";

        public string ToSql => $@"insert into coords values (
            {Id}, {(int)X}, {(int)Y}, {I * 10000 + J},
            {UpId}, {(M_Up & 0b01110000) >> 4}, {(M_Up & 0b00001110) >> 1}, {(M_Up & 0b00000001)},
            {DownId}, {(M_Down & 0b01110000) >> 4}, {(M_Down & 0b00001110) >> 1}, {(M_Down & 0b00000001)},
            {LeftId}, {(M_Left & 0b01110000) >> 4}, {(M_Left & 0b00001110) >> 1}, {(M_Left & 0b00000001)},
            {RightId}, {(M_Right & 0b01110000) >> 4}, {(M_Right & 0b00001110) >> 1}, {(M_Right & 0b00000001)}
        )";

        public void FindNeighbours(IEnumerable<Coord> map) {
            if (UpId_ > 0) Up = map.First(c => c.Id == UpId_);
            if (DownId_ > 0) Down = map.First(c => c.Id == DownId_);
            if (LeftId_ > 0) Left = map.First(c => c.Id == LeftId_);
            if (RightId_ > 0) Right = map.First(c => c.Id == RightId_);
        }

        public void UpdateNeighbours() {
            UpId_ = Up == null ? 0 : Up.Id;
            DownId_ = Down == null ? 0 : Down.Id;
            LeftId_ = Left == null ? 0 : Left.Id;
            RightId_ = Right == null ? 0 : Right.Id;
        }

        public Ellipse Dot = MakeDefaultDot();
        public Line L_Up = MakeDefaultLine(),
                    L_Down = MakeDefaultLine(),
                    L_Left = MakeDefaultLine(),
                    L_Right = MakeDefaultLine();
        public Polyline S_Up = MakeDefaultArrow(),
                       S_Down = MakeDefaultArrow(),
                       S_Left = MakeDefaultArrow(),
                       S_Right = MakeDefaultArrow();
        public TextBlock Label = MakeDefaultLabel(),
                         Label_Up = MakeDefaultLabel(),
                         Label_Down = MakeDefaultLabel(),
                         Label_Left = MakeDefaultLabel(),
                         Label_Right = MakeDefaultLabel();

        public void Update(InkCanvas ic, double ox, double oy) {
            if (!ic.Children.Contains(Dot)) {
                ic.Children.Add(Dot);
                ic.Children.Add(L_Up);
                ic.Children.Add(L_Down);
                ic.Children.Add(L_Left);
                ic.Children.Add(L_Right);
                ic.Children.Add(S_Up);
                ic.Children.Add(S_Down);
                ic.Children.Add(S_Left);
                ic.Children.Add(S_Right);
                ic.Children.Add(Label);
                ic.Children.Add(Label_Up);
                ic.Children.Add(Label_Down);
                ic.Children.Add(Label_Left);
                ic.Children.Add(Label_Right);
            }
            Dot.Stroke = Brushes.Black;
            double x = X + ox, y = Y + oy;
            InkCanvas.SetLeft(Dot, x - Dot.Width / 2);
            InkCanvas.SetTop(Dot, y - Dot.Height / 2);
            Label.Text = $"#{Id} ({I}, {J})\n(Y={(int)X},X={(int)Y})";
            InkCanvas.SetLeft(Label, x);
            InkCanvas.SetTop(Label, y + Dot.Height / 2);
            if (Up != null) {
                L_Up.MoveTo(x, y, Up.X + ox, Up.Y + oy);
                S_Up.Points.Clear();
                S_Up.Points.Add(new Point(Up.X + ox - 6, Up.Y + oy + 10));
                S_Up.Points.Add(new Point(Up.X + ox, Up.Y + oy));
                S_Up.Points.Add(new Point(Up.X + ox + 6, Up.Y + oy + 10));
                Label_Up.Text = $"{(int)(Y - Up.Y)} mm";
                Label_Up.HorizontalAlignment = HorizontalAlignment.Left;
                Label_Up.VerticalAlignment = VerticalAlignment.Center;
                InkCanvas.SetLeft(Label_Up, (X + Up.X) / 2 + ox);
                InkCanvas.SetTop(Label_Up, (Y + Up.Y) / 2 + oy);
                Label_Up.Visibility = S_Up.Visibility = L_Up.Visibility = Visibility.Visible;
            } else {
                Label_Up.Visibility = S_Up.Visibility = L_Up.Visibility = Visibility.Hidden;
            }
            if (Down != null) {
                L_Down.MoveTo(x, y, Down.X + ox, Down.Y + oy);
                S_Down.Points.Clear();
                S_Down.Points.Add(new Point(Down.X + ox + 6, Down.Y + oy - 10));
                S_Down.Points.Add(new Point(Down.X + ox, Down.Y + oy));
                S_Down.Points.Add(new Point(Down.X + ox - 6, Down.Y + oy - 10));
                Label_Down.Text = $"{(int)(Down.Y - Y)} mm";
                Label_Down.HorizontalAlignment = HorizontalAlignment.Right;
                Label_Down.VerticalAlignment = VerticalAlignment.Center;
                InkCanvas.SetLeft(Label_Down, (X + Down.X) / 2 + ox);
                InkCanvas.SetTop(Label_Down, (Y + Down.Y) / 2 + oy);
                Label_Down.Visibility = S_Down.Visibility = L_Down.Visibility = Visibility.Visible;
            } else {
                Label_Down.Visibility = S_Down.Visibility = L_Down.Visibility = Visibility.Hidden;
            }
            if (Left != null) {
                L_Left.MoveTo(x, y, Left.X + ox, Left.Y + oy);
                S_Left.Points.Clear();
                S_Left.Points.Add(new Point(Left.X + ox + 10, Left.Y + oy + 6));
                S_Left.Points.Add(new Point(Left.X + ox, Left.Y + oy));
                S_Left.Points.Add(new Point(Left.X + ox + 10, Left.Y + oy - 6));
                Label_Left.Text = $"{(int)(X - Left.X)} mm";
                Label_Left.HorizontalAlignment = HorizontalAlignment.Center;
                Label_Left.VerticalAlignment = VerticalAlignment.Bottom;
                Label_Left.TextAlignment = TextAlignment.Center;
                InkCanvas.SetLeft(Label_Left, (X + Left.X) / 2 + ox);
                InkCanvas.SetTop(Label_Left, (Y + Left.Y) / 2 + oy);
                Label_Left.Visibility = S_Left.Visibility = L_Left.Visibility = Visibility.Visible;
            } else {
                Label_Left.Visibility = S_Left.Visibility = L_Left.Visibility = Visibility.Hidden;
            }
            if (Right != null) {
                L_Right.MoveTo(x, y, Right.X + ox, Right.Y + oy);
                S_Right.Points.Clear();
                S_Right.Points.Add(new Point(Right.X + ox - 10, Right.Y + oy - 6));
                S_Right.Points.Add(new Point(Right.X + ox, Right.Y + oy));
                S_Right.Points.Add(new Point(Right.X + ox - 10, Right.Y + oy + 6));
                Label_Right.Text = $"{(int)(Right.X - X)} mm";
                Label_Right.HorizontalAlignment = HorizontalAlignment.Center;
                Label_Right.VerticalAlignment = VerticalAlignment.Top;
                Label_Right.TextAlignment = TextAlignment.Center;
                InkCanvas.SetLeft(Label_Right, (X + Right.X) / 2 + ox);
                InkCanvas.SetTop(Label_Right, (Y + Right.Y) / 2 + oy);
                Label_Right.Visibility = S_Right.Visibility = L_Right.Visibility = Visibility.Visible;
            } else {
                Label_Right.Visibility = S_Right.Visibility = L_Right.Visibility = Visibility.Hidden;
            }
        }

        public void Remove(InkCanvas ic) {
            if (ic.Children.Contains(Dot)) {
                ic.Children.Remove(Dot);
                ic.Children.Remove(L_Up);
                ic.Children.Remove(L_Down);
                ic.Children.Remove(L_Left);
                ic.Children.Remove(L_Right);
                ic.Children.Remove(S_Up);
                ic.Children.Remove(S_Down);
                ic.Children.Remove(S_Left);
                ic.Children.Remove(S_Right);
                ic.Children.Remove(Label);
                ic.Children.Remove(Label_Up);
                ic.Children.Remove(Label_Down);
                ic.Children.Remove(Label_Left);
                ic.Children.Remove(Label_Right);
            }
        }
        public override string ToString() => $"#{Id} ({X}, {Y}) ({I}, {J})";
    }

    internal static class ExtensionMethods {
        public static void MoveTo(this Line line, double X1, double Y1, double X2, double Y2) {
            line.X1 = X1; line.X2 = X2; line.Y1 = Y1; line.Y2 = Y2;
        }

        public static void Hide(this Shape shape) => shape.Visibility = Visibility.Hidden;

        public static void Show(this Shape shape) => shape.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private static Line MakeDefaultHVLine() => new Line {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Opacity = 0.2,
            StrokeDashArray = new DoubleCollection(new double[] { 4, 2 })
        };

        public MainWindow() => InitializeComponent();

        protected override void OnSourceInitialized(EventArgs e) => this.RemoveIcon();

        private string FileName = "./MapData.db";
        private bool NeedSync = false;
        private List<Coord> Map = new List<Coord>();
        private double OffsetX, OffsetY;
        private Line HLine = MakeDefaultHVLine(),
                     VLine = MakeDefaultHVLine();
        private Coord LastCur, Cur;
        private double CurX, CurY, LastX = Double.NaN, LastY = Double.NaN;
        private bool IsPressingCtrl = false, IsPressingRight = false;
        private PropertiesWindow PropertiesWindow;

        public void RefreshMap() {
            foreach (Coord c in Map) c.Update(ic, OffsetX, OffsetY);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            PropertiesWindow.ShouldClose = true;
            PropertiesWindow.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ic.Children.Add(HLine);
            ic.Children.Add(VLine);
            ic.Children.Add(DummyLine);
            ic.Children.Add(DummyMark);
            PropertiesWindow = new PropertiesWindow { Owner = this };
            Open_Click(sender, e);
        }

        private void Ic_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.LeftCtrl) IsPressingCtrl = true;
            if (e.Key == Key.Home) {
                OffsetX = OffsetY = 0;
                RefreshMap();
            }
        }

        private void Ic_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.LeftCtrl) IsPressingCtrl = false;
            if (e.Key == Key.Delete) {
                if (LastCur != null) {
                    Map.Remove(LastCur);
                    foreach (Coord c in Map) {
                        if (c.Up == LastCur) c.Up = null;
                        if (c.Down == LastCur) c.Down = null;
                        if (c.Left == LastCur) c.Left = null;
                        if (c.Right == LastCur) c.Right = null;
                    }
                    LastCur.Remove(ic);
                    LastCur = Cur = null;
                    NeedSync = true;
                }
                RefreshMap();
            }
        }

        private void Sb_Click(object sender, RoutedEventArgs e) {
            foreach (Coord c in Map) c.UpdateNeighbours();
            using (LiteDatabase db = new LiteDatabase(FileName)) {
                db.DropCollection("map");
                LiteCollection<Coord> map = db.GetCollection<Coord>("map");
                map.EnsureIndex(c => c.Id);
                map.InsertBulk(Map);
            }
            MessageBox.Show("Saved");
            NeedSync = false;
        }

        private void Es_Click(object sender, RoutedEventArgs e) {
            StringBuilder builder = new StringBuilder(Coord.ToTableSql);
            foreach (Coord c in Map) {
                builder.Append(c.ToSql);
                builder.Append(';');
            }
            string result = builder.ToString();
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "coords",
                DefaultExt = ".sql",
                Filter = "SQL File (.sql)|*.sql|Text documents (.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true) {
                File.WriteAllText(dialog.FileName, result);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show($"AGV Map Editor 1.0\n\n" +
                "Copyright @hyrious 2019\n" +
                "https://github.com/hyrious/AGVMapEditor", "AGV Map Editor");
        }

        private void Help_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(@"Left Button: Select/Drag
Double Click: New Point/Open Properties Window
Press Ctrl: No Alignment
Press Right Button: Drag Canvas/Create Arrow");
        }

        private void Delete_Click(object sender, RoutedEventArgs e) {
            if (LastCur != null) {
                Map.Remove(LastCur);
                foreach (Coord c in Map) {
                    if (c.Up == LastCur) c.Up = null;
                    if (c.Down == LastCur) c.Down = null;
                    if (c.Left == LastCur) c.Left = null;
                    if (c.Right == LastCur) c.Right = null;
                }
                LastCur.Remove(ic);
                LastCur = Cur = null;
                NeedSync = true;
            }
            RefreshMap();
            n.Header = "#";
        }

        private void Edit_Click(object sender, RoutedEventArgs e) {
            if (LastCur != null) {
                HLine.Hide();
                VLine.Hide();
                PropertiesWindow.Title = $"#{LastCur.Id} - Properties";
                PropertiesWindow.LoadMap(Map);
                PropertiesWindow.Coord = LastCur;
                PropertiesWindow.Show();
                NeedSync = true;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                FileName = FileName,
                DefaultExt = ".db",
                Filter = "Data File (.db)|*.db",
                CheckFileExists = false
            };
            if (dialog.ShowDialog() == true) {
                FileName = dialog.FileName;
                using (LiteDatabase db = new LiteDatabase(FileName)) {
                    LiteCollection<Coord> map = db.GetCollection<Coord>("map");
                    Map = map.FindAll().ToList();
                }
                foreach (Coord c in Map) c.FindNeighbours(Map);
                if (Map.Count > 0) Coord.Id_ = Map.Max(c => c.Id);
                RefreshMap();
            } else {
                MessageBox.Show("Canceled");
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                FileName = FileName,
                DefaultExt = ".db",
                Filter = "Data File (.db)|*.db",
                CheckFileExists = false
            };
            if (dialog.ShowDialog() == true) {
                FileName = dialog.FileName;
                foreach (Coord c in Map) c.UpdateNeighbours();
                using (LiteDatabase db = new LiteDatabase(FileName)) {
                    db.DropCollection("map");
                    LiteCollection<Coord> map = db.GetCollection<Coord>("map");
                    map.EnsureIndex(c => c.Id);
                    map.InsertBulk(Map);
                }
                MessageBox.Show("Saved");
                NeedSync = false;
            } else {
                MessageBox.Show("Canceled");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) {
            if (NeedSync) {
                switch (MessageBox.Show("You have unsaved stuffs, save it now?", "Save?", MessageBoxButton.YesNoCancel)) {
                    case MessageBoxResult.Cancel: return;
                    case MessageBoxResult.Yes: Sb_Click(sender, e); break;
                    case MessageBoxResult.No: break;
                    case MessageBoxResult.None: break;
                    case MessageBoxResult.OK: break;
                    default: break;
                }
            }
            Close();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (LastCur == null && Cur == null) {
                Map.Add(Cur = new Coord { X = CurX, Y = CurY });
                foreach (Coord c in Map) c.Update(ic, OffsetX, OffsetY);
                NeedSync = true;
            } else if (LastCur == Cur) {
                HLine.Hide();
                VLine.Hide();
                PropertiesWindow.Title = $"#{Cur.Id} - Properties";
                PropertiesWindow.LoadMap(Map);
                PropertiesWindow.Coord = Cur;
                PropertiesWindow.Show();
                NeedSync = true;
            }
        }

        private void Ic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Cur = null;
            foreach (Coord c in Map) {
                if (c.X - 5 <= CurX && CurX <= c.X + 5 &&
                    c.Y - 5 <= CurY && CurY <= c.Y + 5) {
                    Cur = c;
                    break;
                }
            }
        }

        private void Ic_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            LastCur = Cur;
            if (LastCur != null) {
                LastCur.Dot.Stroke = Brushes.Red;
                n.Header = $"#{LastCur.Id}";
            } else
                n.Header = "#";
        }

        private void Ic_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            IsPressingRight = true;
            Cur = null;
            foreach (Coord c in Map) {
                if (c.X - 5 <= CurX && CurX <= c.X + 5 &&
                    c.Y - 5 <= CurY && CurY <= c.Y + 5) {
                    Cur = c;
                    break;
                }
            }
            LastCur = Cur;
        }

        private void Ic_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            IsPressingRight = false;
            DummyLine.Visibility = Visibility.Hidden;
            DummyMark.Visibility = Visibility.Hidden;
            if (LastCur != null) {
                Cur = null;
                foreach (Coord c in Map) {
                    if (c.X - 5 <= CurX && CurX <= c.X + 5 &&
                        c.Y - 5 <= CurY && CurY <= c.Y + 5) {
                        Cur = c;
                        break;
                    }
                }
                if (Cur != null && Cur != LastCur) {
                    double dx = Cur.X - LastCur.X,
                           dy = Cur.Y - LastCur.Y;
                    switch (GuessDirection(dx, dy)) {
                        case 2: LastCur.Down = Cur; break;
                        case 4: LastCur.Left = Cur; break;
                        case 6: LastCur.Right = Cur; break;
                        case 8: LastCur.Up = Cur; break;
                        default: break;
                    }
                }
            }
        }

        private static Line MakeDefaultLine() => new Line {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Visibility = Visibility.Hidden
        };

        private static Polyline MakeDefaultMark() => new Polyline {
            Stroke = Brushes.Black,
            StrokeThickness = 2,
            Visibility = Visibility.Hidden
        };

        private Line DummyLine = MakeDefaultLine();
        private Polyline DummyMark = MakeDefaultMark();
        private int DummyDirection = 0;

        private void Ic_MouseMove(object sender, MouseEventArgs e) {
            Point pos = e.GetPosition(ic);
            double x = pos.X, y = pos.Y;
            if (!IsPressingCtrl) {
                foreach (Coord c in Map) {
                    if (c == Cur) continue;
                    double cx = c.X + OffsetX, cy = c.Y + OffsetY;
                    if (cx - 5 <= x && x <= cx + 5) x = cx;
                    if (cy - 5 <= y && y <= cy + 5 && x != c.X) y = cy;
                }
            }
            CurX = x - OffsetX; CurY = y - OffsetY;
            if (IsPressingRight) {
                HLine.Hide();
                VLine.Hide();
                if (LastCur == null) {
                    if (Double.IsNaN(LastX)) {
                        LastX = x; LastY = y;
                    } else {
                        OffsetX += x - LastX;
                        OffsetY += y - LastY;
                        LastX = x; LastY = y;
                    }
                } else {
                    if (Double.IsNaN(LastX)) {
                        LastX = x; LastY = y;
                    } else {
                        DummyLine.Visibility = Visibility.Visible;
                        DummyMark.Visibility = Visibility.Visible;
                        double dx = x - LastX,
                               dy = y - LastY;
                        DummyDirection = GuessDirection(dx, dy);
                        UpdateDummyArrow(x, y);
                    }
                }
            } else {
                LastX = Double.NaN; LastY = Double.NaN;
                HLine.MoveTo(0, y, ic.ActualWidth, y);
                VLine.MoveTo(x, 0, x, ic.ActualHeight);
                if (e.LeftButton == MouseButtonState.Pressed) {
                    HLine.Hide();
                    VLine.Hide();
                    if (Cur != null && !PropertiesWindow.IsVisible) {
                        Cur.X = CurX;
                        Cur.Y = CurY;
                        NeedSync = true;
                    }
                } else {
                    Cur = null;
                    HLine.Show();
                    VLine.Show();
                }
            }
            foreach (Coord c in Map) {
                c.Update(ic, OffsetX, OffsetY);
                if (LastCur != null)
                    LastCur.Dot.Stroke = Brushes.Red;
            }
        }

        private void UpdateDummyArrow(double x, double y) {
            DummyLine.MoveTo(LastCur.X + OffsetX, LastCur.Y + OffsetY, x, y);
            DummyMark.Points.Clear();
            switch (DummyDirection) {
                case 8:
                    DummyMark.Points.Add(new Point(x - 6, y + 10));
                    DummyMark.Points.Add(new Point(x, y));
                    DummyMark.Points.Add(new Point(x + 6, y + 10));
                    break;
                case 2:
                    DummyMark.Points.Add(new Point(x + 6, y - 10));
                    DummyMark.Points.Add(new Point(x, y));
                    DummyMark.Points.Add(new Point(x - 6, y - 10));
                    break;
                case 4:
                    DummyMark.Points.Add(new Point(x + 10, y + 6));
                    DummyMark.Points.Add(new Point(x, y));
                    DummyMark.Points.Add(new Point(x + 10, y - 6));
                    break;
                case 6:
                    DummyMark.Points.Add(new Point(x - 10, y - 6));
                    DummyMark.Points.Add(new Point(x, y));
                    DummyMark.Points.Add(new Point(x - 10, y + 6));
                    break;
            }
        }

        private int GuessDirection(double dx, double dy) {
            if (Math.Abs(dx) > Math.Abs(dy)) {
                return dx > 0 ? 6 : 4;
            } else {
                return dy > 0 ? 2 : 8;
            }
        }

        private void Button_MouseMove(object sender, MouseEventArgs e) => Ic_MouseMove(sender, e);
    }
}
