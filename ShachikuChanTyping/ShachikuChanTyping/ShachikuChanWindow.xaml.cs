using System;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

using System.Windows.Forms;
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

        //ここで指定した回数おっぱいを揺らすと怒る
        public const int OppaiCountMax = 5;

        //胸の当たり判定の左右端を、ウィンドウ左端0, 右端1とした時の比率で表した値
        public const double OppaiLeft = 0.367;
        public const double OppaiRight = 0.625;
        //こちらは上下端を、ウィンドウ上端0, 下端1とした比率
        public const double OppaiTop = 0.717;
        public const double OppaiBottom = 0.870;

        public const double OriginalFontSize = 20.0;
        public const string SerihuFileName = "serihu.txt";

        /// <summary>おっぱいを触り続けたときに飛んでくる台詞の候補</summary>
        public static string[] Serihus
        {
            get
            {
                if (!File.Exists(SerihuFileName))
                {
                    File.WriteAllLines(SerihuFileName, DefaultSerihus, Encoding.UTF8);
                }
                return File.ReadAllLines(SerihuFileName);
            }
        }
        /// <summary>基本的な？台詞の一覧</summary>
        public static string[] DefaultSerihus
        {
            get
            {
                return new string[]
                {
                    "死ね！",
                    "…変態。",
                    "…バカ。",
                    "…訴えますよ？",
                    "…私、帰ります。",
                    "…ずいぶん嬉しそうですね。",
                    "…退職届、持ってきましょうか？",
                    "…ちょっと…今は休出中でしょ？",
                    "…喜ぶとでも思ってるんですか？",
                    "…訴状と退職届、どっちがいいですか？",
                    "…終電過ぎたからって何してるんですか。",
                    "…デスマの最中にふざけないでくれます？",
                    "…寝ぼけてんなら仮眠室で寝て来て下さい。",
                    "私は揉まれたいんじゃなくて揉みたいんです！",
                    "…そのニヤけ顔、気持ち悪いんでやめてくれますか？"
                };
            }
        }


        public MainWindow()
        {
            //InitializeComponentが先だと_settingのNullReferenceExceptionが鬱陶しいので先回り
            _setting = ShachikuChanSetting.Load();

            InitializeComponent();

            //HACK: Initializeの時点で_settingがイベントハンドラに書き換えられちゃうので更にリロード
            _setting = ShachikuChanSetting.Load();

            _keyboardHook = new KeyboardHook(OnKeyboardKeyDown);
            
            this.SliderSize.Value = _setting.ScaleFactor;
            this.MenuItemEnergyMode.IsChecked = _setting.IsEnergyMode;
            this.MenuItemTopmost.IsChecked = _setting.IsTopmost;
            this.MenuItemSexyMode.IsChecked = _setting.IsSexyModeEnabled;
            this.Topmost = _setting.IsTopmost;

            ApplyEnergyMode(_setting.IsEnergyMode);
            ApplySexyMode(_setting.IsSexyModeEnabled);

            Storyboard typing = ShachikuChanSexyVersion.TryFindResource("KeyboardType") as Storyboard;
            if (typing != null) typing.Completed += (_, __) => _typingEnabled = true;

            Storyboard shake = ShachikuChanSexyVersion.TryFindResource("Shake") as Storyboard;
            if (shake != null) shake.Completed += OnShakeCompleted;

            Storyboard getAngry = ShachikuChanSexyVersion.TryFindResource("GetAngry") as Storyboard;
            if (getAngry != null) getAngry.Completed += (_, __) => _shakeEnabled = true;
        }


        /// <summary>ShachikuChan.xamlに関連づけられたアニメーションを実行します。</summary>
        /// <param name="keyName">アニメーションのキー文字列</param>
        private void DoShachikuChanAnimation(string keyName)
        {
            Storyboard sb = this.ShachikuChan.TryFindResource(keyName) as Storyboard;
            if (sb != null) sb.Begin();

            Storyboard sb2 = this.ShachikuChanSexyVersion.TryFindResource(keyName) as Storyboard;
            if(sb2 != null) sb2.Begin();
        }

        /// <summary>スクリーンの左下にウィンドウを移動させます。</summary>
        private void RelocateToLeftBottom()
        {
            var area = Screen.GetWorkingArea(Drawing.Point.Empty);

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

        /// <summary>従来の社畜ちゃんと新しい社畜ちゃんの表示切り替えを適用します。</summary>
        private void ApplySexyMode(bool isSexyMode)
        {
            if (isSexyMode)
            {
                this.ShachikuChan.Visibility = Visibility.Hidden;
                this.ShachikuChanSexyVersion.Visibility = Visibility.Visible;
            }
            else
            {
                this.ShachikuChan.Visibility = Visibility.Visible;
                this.ShachikuChanSexyVersion.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>キーボードが押された時の処理です。</summary>
        private void OnKeyboardKeyDown(object sender, KeyboardHookedEventArgs e)
        {
            if (e.UpDown != KeyboardUpDown.Down)
            {
                if (this.ShachikuChan.Visibility == Visibility.Visible)
                {
                    DoShachikuChanAnimation("KeyboardType");
                }
                else
                {
                    if (_typingEnabled)
                    {
                        _typingEnabled = false;
                        DoShachikuChanAnimation("KeyboardType");                        
                    }
                }
            }
        }

        //デバッグ実行でもタイピング挙動を試したい場合はコメントアウト解除
        //protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        //{
        //    base.OnKeyDown(e);
        //    DoShachikuChanAnimation("KeyboardType");
        //}

        /// <summary>「退社」がクリックされた時の処理です。</summary>
        private void OnLeaveOfficeClick(object sender, RoutedEventArgs e)
        {
            Close();        
        }

        /// <summary>元気モードの値が変更された時の処理です。</summary>
        private void OnEnergyModeChanged(object sender, RoutedEventArgs e)
        {
            _setting.IsEnergyMode = this.MenuItemEnergyMode.IsChecked;
            ApplyEnergyMode(this.MenuItemEnergyMode.IsChecked);
        }

        /// <summary>最前面表示の設定が変更された時の処理です。</summary>
        private void OnTopmostChanged(object sender, RoutedEventArgs e)
        {
            _setting.IsTopmost = this.MenuItemTopmost.IsChecked;
            this.Topmost = this.MenuItemTopmost.IsChecked;
        }

        /// <summary>サイズ変更の要求を処理します。</summary>
        private void OnSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _setting.ScaleFactor = e.NewValue;                
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

            if (ShachikuChan.Visibility == Visibility.Visible)
            {
                this.DragMove();                
            }
            else
            {
                //当たり判定計算に用いる
                var pos = e.GetPosition(this);
                double x = pos.X / Width;
                double y = pos.Y / Height;

                //胸以外を触ってる場合
                if (x < OppaiLeft || OppaiRight < x || y < OppaiTop || OppaiBottom < y)
                {
                    DragMove();
                }
                else
                {
                    //アニメーション適用中は多重でアニメーションしない
                    if (!_shakeEnabled) return;

                    _shakeEnabled = false;
                    DoShachikuChanAnimation("Shake");                    
                }
                
            }
        }


        /// <summary>セクシーモードの設定変更を反映します。</summary>
        private void OnSexyModeChanged(object sender, RoutedEventArgs e)
        {
            _setting.IsSexyModeEnabled = this.MenuItemSexyMode.IsChecked;
            ApplySexyMode(_setting.IsSexyModeEnabled);
        }

        /// <summary>胸揺れアニメーション終了時の処理</summary>
        private void OnShakeCompleted(object sender, EventArgs e)
        {
            _oppaiCount++;
            if (_oppaiCount >= OppaiCountMax)
            {
                _oppaiCount = 0;
                SetRandomSerihu();
                DoShachikuChanAnimation("GetAngry");
            }
            else
            {
                _shakeEnabled = true;
            }
        }

        /// <summary>台詞を候補からランダムに選択</summary>
        private void SetRandomSerihu()
        {
            var rnd = new Random();
            //スケールが変わってもフォントサイズが不変になるようにする場合こんな感じ
            //ShachikuChanSexyVersion.Serihu.FontSize = OriginalFontSize / _setting.ScaleFactor;
            ShachikuChanSexyVersion.Serihu.Text = Serihus[rnd.Next(Serihus.Length)];
        }

        /// <summary>終了前に設定を保存します。</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            _setting.Save();
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

        private int _oppaiCount;
        private bool _typingEnabled = true;
        private bool _shakeEnabled = true;

    }
}
