using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
//スクリーンサイズの取得に必要
using System.Windows.Forms;
//スクリーンサイズの取得に必要
using Drawing = System.Drawing;

namespace ShachikuChanTyping
{
    /// <summary>社畜ちゃんを表示するウィンドウを表します。</summary>
    public partial class MainWindow : Window
    {
        public const double DefaultWidth = 600;
        public const double DefaultHeight = 460;
        //ここの値はテキトーに決めてよい: 強いて言えば小さすぎると面白くない。
        public const double NadeThreshold = 10000;

        //著作権表記も兼ねて社畜ちゃんの出身地？的URL
        public const string ShachikuChanSerihuMaker = @"http://blog.oukasoft.com/OS/";

        public MainWindow()
        {
            InitializeComponent();

            _setting = ShachikuChanSetting.Load();
            _keyboardHook = new KeyboardHook(OnKeyboardKeyDown);

            this.SliderSize.Value = _setting.ScaleFactor;
            this.MenuItemEnergyMode.IsChecked = _setting.IsEnergyMode;
            this.MenuItemTopmost.IsChecked = _setting.IsTopmost;
            this.Topmost = _setting.IsTopmost;

            ApplyEnergyMode(_setting.IsEnergyMode);
        }

        /// <summary>ShachikuChan.xamlに関連づけられたアニメーションを実行します。</summary>
        /// <param name="keyName">アニメーションのキー文字列</param>
        private void DoShachikuChanAnimation(string keyName)
        {
            Storyboard sb = this.ShachikuChan.TryFindResource(keyName) as Storyboard;
            if (sb != null) sb.Begin();
        }

        /// <summary>スクリーンの左下にウィンドウを移動させます。</summary>
        private void RelocateToLeftBottom()
        {
            var area = System.Windows.Forms.Screen.GetWorkingArea(Drawing.Point.Empty);

            var dpiFactor = GetDpiFactors();
            this.Left = (area.Left) / dpiFactor.X;
            this.Top = area.Bottom / dpiFactor.Y - this.Height;
        }

        /// <summary>キャラのサイズに指定されたスケールファクターを適用します。</summary>
        private void ApplyScaleFactor(double scaleFactor)
        {
            this.Width = DefaultWidth * scaleFactor;
            this.Height = DefaultHeight * scaleFactor;
        }

        /// <summary>
        /// DPIの補正値を取得します。
        /// </summary>
        /// <returns>DPI補正値(例: 192dpiなら192 / 96 = 2, 144dpiなら1.5)</returns>
        private Point GetDpiFactors()
        {
            var g = Drawing.Graphics.FromHwnd(IntPtr.Zero);
            return new Point(g.DpiX / 96.0, g.DpiY / 96.0);
        }

        /// <summary>元気モードかどうかを描画に反映させます。</summary>
        private void ApplyEnergyMode(bool isEnergyMode)
        {
            if (isEnergyMode)
            {
                DoShachikuChanAnimation("GetEnergy");
            }
            else
            {
                DoShachikuChanAnimation("LostEnergy");
            }
        }
        
        /// <summary>キーボードが押された時の処理です。</summary>
        private void OnKeyboardKeyDown(object sender, KeyboardHookedEventArgs e)
        {
            if (e.UpDown != KeyboardUpDown.Down)
            {
                DoShachikuChanAnimation("KeyboardType");
            }

        }

        /// <summary>「退社」がクリックされた時の処理です。</summary>
        private void OnLeaveOfficeClick(object sender, RoutedEventArgs e)
        {
            Close();        
        }

        /// <summary>元気モードの値が変更された時の処理です。</summary>
        private void OnEnergyModeChanged(object sender, RoutedEventArgs e)
        {
            if (_setting != null)
            {
                _setting.IsEnergyMode = this.MenuItemEnergyMode.IsChecked;
            }
            ApplyEnergyMode(this.MenuItemEnergyMode.IsChecked);
        }

        /// <summary>最前面表示の設定が変更された時の処理です。</summary>
        private void OnTopmostChanged(object sender, RoutedEventArgs e)
        {
            if(_setting != null) _setting.IsTopmost = this.MenuItemTopmost.IsChecked;
            this.Topmost = this.MenuItemTopmost.IsChecked;
        }

        /// <summary>サイズ変更の要求を処理します。</summary>
        private void OnSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_setting != null)
            {
                _setting.ScaleFactor = e.NewValue;                
            }
            ApplyScaleFactor(e.NewValue);
            RelocateToLeftBottom();
        }

        /// <summary>社畜ちゃん台詞メーカーを既定のウェブブラウザで開きます。</summary>
        private void OnOpenSerihuMakerClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Process.Start(new Uri(ShachikuChanSerihuMaker).AbsoluteUri));
        }

        /// <summary>社畜ちゃんが左クリックで掴まれた時の処理です。</summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
        
        /// <summary>終了前に設定を保存します。</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if(_setting != null) _setting.Save();
            base.OnClosing(e);
        }

        /// <summary>ウィンドウ上にマウスが入り込んだ時に発生します。</summary>
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _previousPos = e.GetPosition(this);
        }

        /// <summary>ウィンドウ上でのマウス移動時に発生します。</summary>
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var p = e.GetPosition(this);
            _nade += (p - _previousPos).Length;
            if(_nade > NadeThreshold)
            {
                _nade = 0;
                DoShachikuChanAnimation("Blush");
            }
        }

        private Point _previousPos;
        private double _nade;

        private readonly ShachikuChanSetting _setting;
        private KeyboardHook _keyboardHook;

    }
}
