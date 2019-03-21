using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiteDB;

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
                id                integer primary key, -- start from 1
                x                 integer default 0,   -- unit: mm
                y                 integer default 0,
                position_barcode  integer default 0,   -- struct {{ short i = Y, short j = X }}
                up_point_id       integer default 0,   -- 0 is null
                up_check1_mode    integer default 0,   -- 1..7, 0 is illegal
                up_check2_mode    integer default 0,   -- 1..7, 0 is illegal
                up_pose           integer default 0,   -- 0 前进, 1 后退
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
            ); -- int[][20]; // 0.0
        ";

        public string ToSql => $@"insert into coords values (
            {Id}, {(int)X}, {(int)Y}, {(I << 16) | J},
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
            double x = X + ox, y = Y + oy;
            InkCanvas.SetLeft(Dot, x - Dot.Width / 2);
            InkCanvas.SetTop(Dot, y - Dot.Height / 2);
            Label.Text = $"#{Id} ({I}, {J})\n(Y={X},X={Y})";
            InkCanvas.SetLeft(Label, x);
            InkCanvas.SetTop(Label, y + Dot.Height / 2);
            if (Up != null) {
                L_Up.MoveTo(x, y, Up.X + ox, Up.Y + oy);
                S_Up.Points.Clear();
                S_Up.Points.Add(new Point(Up.X + ox - 6, Up.Y + oy + 10));
                S_Up.Points.Add(new Point(Up.X + ox, Up.Y + oy));
                S_Up.Points.Add(new Point(Up.X + ox + 6, Up.Y + oy + 10));
                Label_Up.Text = $"{Y - Up.Y} mm";
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
                Label_Down.Text = $"{Down.Y - Y} mm";
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
                Label_Left.Text = $"{X - Left.X} mm";
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
                Label_Right.Text = $"{Right.X - X} mm";
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
                ic.Children.Remove(Label);
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
            sb.Visibility = NeedSync ? Visibility.Visible : Visibility.Hidden;
            PropertiesWindow = new PropertiesWindow { Owner = this };
            using (LiteDatabase db = new LiteDatabase("./MapData.db")) {
                LiteCollection<Coord> map = db.GetCollection<Coord>("map");
                Map = map.FindAll().ToList();
            }
            foreach (Coord c in Map) c.FindNeighbours(Map);
            if (Map.Count > 0) Coord.Id_ = Map.Max(c => c.Id);
            RefreshMap();
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
                sb.Visibility = NeedSync ? Visibility.Visible : Visibility.Hidden;
                RefreshMap();
            }
        }

        private void Sb_Click(object sender, RoutedEventArgs e) {
            sb.Content = "Saving...";
            foreach (Coord c in Map) c.UpdateNeighbours();
            using (LiteDatabase db = new LiteDatabase("./MapData.db")) {
                db.DropCollection("map");
                LiteCollection<Coord> map = db.GetCollection<Coord>("map");
                map.EnsureIndex(c => c.Id);
                map.InsertBulk(Map);
            }
            MessageBox.Show("Saved");
            NeedSync = false;
            sb.Visibility = Visibility.Hidden;
            sb.Content = "Save";
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
            sb.Visibility = NeedSync ? Visibility.Visible : Visibility.Hidden;
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

        private void Ic_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => LastCur = Cur;

        private void Ic_MouseRightButtonDown(object sender, MouseButtonEventArgs e) => IsPressingRight = true;

        private void Ic_MouseRightButtonUp(object sender, MouseButtonEventArgs e) => IsPressingRight = false;

        private void Ic_MouseMove(object sender, MouseEventArgs e) {
            Point pos = e.GetPosition(ic);
            double x = pos.X, y = pos.Y;
            if (IsPressingRight) {
                HLine.Hide();
                VLine.Hide();
                if (Double.IsNaN(LastX)) {
                    LastX = x; LastY = y;
                } else {
                    OffsetX += x - LastX;
                    OffsetY += y - LastY;
                    LastX = x; LastY = y;
                }
            } else {
                LastX = Double.NaN; LastY = Double.NaN;
                if (!IsPressingCtrl) {
                    foreach (Coord c in Map) {
                        if (c == Cur) continue;
                        double cx = c.X + OffsetX, cy = c.Y + OffsetY;
                        if (cx - 5 <= x && x <= cx + 5) x = cx;
                        if (cy - 5 <= y && y <= cy + 5 && x != c.X) y = cy;
                    }
                }
                CurX = x - OffsetX; CurY = y - OffsetY;
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
            foreach (Coord c in Map) c.Update(ic, OffsetX, OffsetY);
            sb.Visibility = NeedSync ? Visibility.Visible : Visibility.Hidden;
        }

        private void Button_MouseMove(object sender, MouseEventArgs e) => Ic_MouseMove(sender, e);
    }
}
