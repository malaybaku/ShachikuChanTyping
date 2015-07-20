using System;
using System.IO;
using System.Xml.Serialization;

namespace ShachikuChanTyping
{
    /// <summary>社畜ちゃんの表示設定を表します。</summary>
    public class ShachikuChanSetting
    {
        /// <summary>設定保存に使うファイルの名前です。</summary>
        public const string SettingFileName = "ShachikuChanSetting.xml";

        private double _scaleFactor = 0.5;
        private const double ScaleFactorMinimum = 0.3;
        private const double ScaleFactorMaximum = 2.0;
        /// <summary>キャラのサイズ補正値を取得、設定します。</summary>
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                if (_scaleFactor < ScaleFactorMinimum)
                {
                    _scaleFactor = ScaleFactorMinimum;
                }
                else if(_scaleFactor > ScaleFactorMaximum)
                {
                    _scaleFactor = ScaleFactorMaximum;
                }
                else
                {
                    _scaleFactor = value;
                }
            }
        }

        /// <summary>元気モードかどうかを取得、設定します。</summary>
        public bool IsEnergyMode { get; set; }
        /// <summary>キャラを最前面に表示するかどうかを取得、設定します。</summary>
        public bool IsTopmost { get; set; }
        /// <summary>セクシーモードが適用されているかどうかを取得、設定します。</summary>
        public bool IsSexyModeEnabled { get; set; }

        /// <summary>設定を保存します。</summary>
        public void Save()
        {
            using (var sw = new StreamWriter(SettingFileName))
            {
                var serializer = new XmlSerializer(typeof(ShachikuChanSetting));
                serializer.Serialize(sw, this);
            }
        }

        /// <summary>保存済みの設定または既定の設定を取得します。</summary>
        public static ShachikuChanSetting Load()
        {
            try
            {
                using (var sr = new StreamReader(SettingFileName))
                {
                    var serializer = new XmlSerializer(typeof(ShachikuChanSetting));
                    return serializer.Deserialize(sr) as ShachikuChanSetting;
                }
            }
            catch (Exception)
            {
                return new ShachikuChanSetting();
            }
        }

    }
}
