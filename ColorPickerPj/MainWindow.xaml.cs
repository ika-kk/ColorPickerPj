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

using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ColorPickerPj
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>サーチ半径を保持する変数</summary>
        private int _diam = 1;

        /// <summary>
        /// コンストラクタ。
        /// プログラム実行と同時に色取得処理を開始する。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // ウインドウ半透明にしたときにドラッグできるようにする
            // 以下参考URL
            // https://p4j4.hatenablog.com/entry/2018/03/31/170251
            MouseLeftButtonDown += (_,__) => { DragMove(); };
            
            // メインループ
            var task = new Task(MainLoop);
            task.Start();
        }

        /// <summary>
        /// プログラム実行中まわりつづけるループ。
        /// 常に現在のカーソル周辺の画素値を分析しつづける。
        /// タスクとして使用。
        /// </summary>
        private void MainLoop()
        {
            while (true)
            {
                // 現在のカーソル位置周辺の画像取得
                var bitmap = new Bitmap(_diam, _diam);
                var currentPos = System.Windows.Forms.Cursor.Position;
                using (var graphic = Graphics.FromImage(bitmap))
                {
                    graphic.CopyFromScreen(
                        new System.Drawing.Point(currentPos.X - _diam / 2, currentPos.Y - _diam / 2),
                        new System.Drawing.Point(0, 0),
                        new System.Drawing.Size(_diam, _diam));
                }
                // デバッグ用
                //bitmap.Save("temp.bmp");

                // 画素値(RGB)取得
                byte r, g, b;
                GetMeanValueFromBitmap(bitmap, out r, out g, out b);

                // HSVへ変換
                double h, s, v;
                RgbToHsv(r, g, b, out h, out s, out v);

                // グレースケール値
                byte gray = (byte)(0.30 * r + 0.59 * g + 0.11 * b);

                // 表示更新
                // 実際にはHSVも同時に計算して表示する
                var msg = new Action<byte, byte, byte, double, double, double, byte>(UpdateValue);
                Dispatcher.Invoke(msg, r, g, b, h, s, v, gray);
                
            }
        }

        /// <summary>
        /// 画像の各チャンネルの平均画素値を取得する。
        /// 下記参考URL
        /// https://www.84kure.com/blog/2014/07/13/c-%E3%83%93%E3%83%83%E3%83%88%E3%83%9E%E3%83%83%E3%83%97%E3%81%AB%E3%83%94%E3%82%AF%E3%82%BB%E3%83%AB%E5%8D%98%E4%BD%8D%E3%81%A7%E9%AB%98%E9%80%9F%E3%81%AB%E3%82%A2%E3%82%AF%E3%82%BB%E3%82%B9/
        /// </summary>
        /// <param name="bitmap">入力画像。デフォルトは32bitARGBのはず。</param>
        /// <param name="r">Rチャンネルの平均画素値</param>
        /// <param name="g">Gチャンネルの平均画素値</param>
        /// <param name="b">Bチャンネルの平均画素値</param>
        private void GetMeanValueFromBitmap(Bitmap bitmap, out byte r, out byte g, out byte b)
        {
            BitmapData data = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // リストに画素値を格納していく。byte型だと平均が出せないのでdouble型でやる。
            var rValues = new List<double>();
            var gValues = new List<double>();
            var bValues = new List<double>();
            int bytes = bitmap.Width * bitmap.Height * 4;
            for (int i = 0; i < bytes; i += 4)
            {
                bValues.Add(Marshal.ReadByte(data.Scan0, i));
                gValues.Add(Marshal.ReadByte(data.Scan0, i + 1));
                rValues.Add(Marshal.ReadByte(data.Scan0, i + 2));
            }
            bitmap.UnlockBits(data);

            // なんかの拍子に格納されてなかったら怖いので一応条件分岐
            if (rValues.Count > 0 && gValues.Count > 0 && bValues.Count > 0)
            {
                r = (byte)rValues.Average();
                g = (byte)gValues.Average();
                b = (byte)bValues.Average();
            }
            else
            {
                r = g = b = 0;
            }
        }

        /// <summary>
        /// RGBをHSVに変換する。
        /// Hは0~360、sとvは0~255で取得する。
        /// </summary>
        /// <param name="r">R値</param>
        /// <param name="g">G値</param>
        /// <param name="b">B値</param>
        /// <param name="h">H値。0~360</param>
        /// <param name="s">S値。0~255</param>
        /// <param name="v">V値。0~255</param>
        private void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            // 色相
            if (r == g && g == b)
            {
                h = 0;
            }
            else if (r >= g && r >= b)
            {
                h = 60.0 * ((double)(g - b) / (max - min));
            }
            else if (g >= r && g >= b)
            {
                h = 60.0 * ((double)(b - r) / (max - min)) + 120;
            }
            else
            {
                h = 60.0 * ((double)(r - g) / (max - min)) + 240;
            }
            if (h < 0)
            {
                h += 360;
            }

            // 彩度
            if (max == 0)
            {
                s = 0;
            }
            else
            {
                s = (max - min) / max;
                s *= 255;
            }

            // 明度
            v = max;
        }

        /// <summary>
        /// RGB値とHSV値をもとに画面表示を更新する。
        /// HSVに関しては画面の設定をもとに値域を変換する。
        /// </summary>
        /// <param name="r">R値</param>
        /// <param name="g">G値</param>
        /// <param name="b">B値</param>
        /// <param name="h">H値。0~360</param>
        /// <param name="s">S値。0~255</param>
        /// <param name="v">V値。0~255</param>
        /// <param name="gray">グレースケール値</param>
        private void UpdateValue(byte r, byte g, byte b, double h, double s, double v, byte gray)
        {
            // サーチ直径の表示色を見えやすい色に設定
            brushEllipse.Stroke = v < 128 ?
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 200, 200, 200)):   // 背景が暗い場合は白線
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(200,  55,  55,  55));   // 背景が明るい場合は黒線

            // hsvの値域を調整
            if (h255RadioButton.IsChecked == true) h = (byte)(h * 255.0 / 360.0);
            if (s100RadioButton.IsChecked == true) s = (byte)(s * 100.0 / 255.0);
            if (v100RadioButton.IsChecked == true) v = (byte)(v * 100.0 / 255.0);

            // 値更新
            string code = $"#{r.ToString("X2")}{g.ToString("X2")}{b.ToString("X2")}";
            colorCodeTextBox.Text = code;
            rTextBox.Text = r.ToString();
            gTextBox.Text = g.ToString();
            bTextBox.Text = b.ToString();
            hTextBox.Text = ((int)h).ToString();
            sTextBox.Text = ((int)s).ToString();
            vTextBox.Text = ((int)v).ToString();
            grayTextBox.Text = gray.ToString();
            colorRectangle.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }


        /// <summary>
        /// マウスホイールでスライダーを制御するイベント。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void diamSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                diamSlider.Value++;
            }
            else if (e.Delta < 0)
            {
                diamSlider.Value--;
            }
        }

        /// <summary>
        /// スライダーの値をもとにサーチ半径表示を更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void diamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _diam = (int)diamSlider.Value;
            diamTextBlock.Text = $"{_diam} px";
            brushEllipse.Width = brushEllipse.Height = _diam + 2;
        }
        
        /// <summary>
        /// 終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
    }
}
