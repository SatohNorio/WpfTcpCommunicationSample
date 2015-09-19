using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net.Sockets;
using System.Net;

using Log;
using TcpCommunication;

namespace WpfTcpCommunicationSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ClientWindow : Window
    {
        // ------------------------------------------------------------------------------------------------------------
        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ClientWindow()
        {
            InitializeComponent();

            // ログがリストボックスに表示されるようにする。
            this.DataContext = this.LogCollection;

            this.AddLog("***** クライアントアプリケーション開始 *****");

            //var lcAddr = new IPAddress(new byte[] { 192, 168, 150, 131 });
            var lcAddr = new IPAddress(new byte[] { 192, 168, 25, 100 });
            var lcPort = 65000;
            var lcEndPoint = new IPEndPoint(lcAddr, lcPort);
            //var rmAddr = new IPAddress(new byte[] { 192, 168, 150, 132 });
            var rmAddr = new IPAddress(new byte[] { 192, 168, 25, 85 });
            var rmPort = 65000;
            var rmEndPoint = new IPEndPoint(rmAddr, rmPort);
            try
            {
                this.tcpCommunicator = new TcpCommunicator(lcEndPoint, rmEndPoint);
                this.tcpCommunicator.DataReceived += TcpCommunicatorDataReceived;
                this.tcpCommunicator.ExceptionHappened += TcpCommunicatorExceptionHappened;
                var msg = "サーバに接続しました。(Local側IP:" + lcEndPoint.Address.ToString() + ", ポート:" + lcEndPoint.Port.ToString();
                msg += ", Remode側IP:" + rmEndPoint.Address.ToString() + ", ポート:" + rmEndPoint.Port.ToString() + ")";
                this.AddLog(msg);
            }
            catch (Exception ex)
            {
                var msg = "サーバとの接続に失敗しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
                return;
            }
            this.encoding = Encoding.UTF8;
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region WindowClosedイベント処理

        /// <summary>
        /// プログラム終了時の処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">イベント引数を指定します。</param>
        private void WindowClosed(object sender, EventArgs e)
        {
            if (this.tcpCommunicator != null)
            {
                this.tcpCommunicator.DataReceived -= TcpCommunicatorDataReceived;
                this.tcpCommunicator.ExceptionHappened -= TcpCommunicatorExceptionHappened;
                this.tcpCommunicator.Dispose();
                this.tcpCommunicator = null;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region DataReceivedイベント処理

        /// <summary>
        /// データ受信イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">受信データを含むイベント引数を指定します。</param>
        private void TcpCommunicatorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var d = this.Dispatcher;
            if (!d.CheckAccess())
            {
                d.Invoke(() => this.TcpCommunicatorDataReceived(sender, e));
            }
            else
            {
                try
                {
                    var data = this.encoding.GetString(e.ReceivedData);
                    this.AddLog("受信データ:" + data);
                }
                catch (Exception ex)
                {
                    var msg = "受信データの変換に失敗しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                    this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
                }
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region ExceptionHappenedイベント処理

        /// <summary>
        /// 例外通知イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">発生した例外を含むイベント引数を指定します。</param>
        private void TcpCommunicatorExceptionHappened(object sender, ExceptionHappenedEventArgs e)
        {
            var d = this.Dispatcher;
            if (!d.CheckAccess())
            {
                d.Invoke(() => this.TcpCommunicatorExceptionHappened(sender, e));
            }
            else
            {
                var ex = e.Exception;
                var msg = ex.GetType().ToString() + ":" + ex.Message;
                this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
                if (!this.tcpCommunicator.IsConnected)
                {
                    this.tcpCommunicator.Dispose();
                }
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TCPで通信するオブジェクトを管理します。
        /// </summary>
        private TcpCommunicator tcpCommunicator;

        /// <summary>
        /// 通信するテキストデータのエンコードを保持します。
        /// </summary>
        private Encoding encoding;

        // ------------------------------------------------------------------------------------------------------------
        #region リストボックス表示用コレクション

        /// <summary>
        /// リストボックス表示用のログコレクションを保持します。
        /// </summary>
        private ObservableCollection<LogRecord> logCollection;

        /// <summary>
        /// リストボックス表示用のログコレクションを管理します。
        /// </summary>
        private ObservableCollection<LogRecord> LogCollection
        {
            get
            {
                return this.logCollection = this.logCollection ?? new ObservableCollection<LogRecord>();
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// ログを1行追加してリストボックスに表示します。
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <remarks>
        /// 100件を超えたデータは古いものから削除されます。
        /// </remarks>
        private void AddLog(string iMsg)
        {
            this.AddLog(iMsg, LogRecord.WarningLevel.Normal);
        }

        /// <summary>
        /// ログを1行追加してリストボックスに表示します。
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <param name="iLevel">ログの重要度を表す警告レベルを指定します。</param>
        /// <param name="iDescription">ログの詳細情報を指定します。このパラメータは省略できます。</param>
        /// <remarks>
        /// 100件を超えたデータは古いものから削除されます。
        /// </remarks>
        private void AddLog(string iMsg, LogRecord.WarningLevel iLevel, string iDescription = "")
        {
            var rec = new LogRecord(iMsg, iLevel, iDescription);
            var c = this.LogCollection;
            var cnt = c.Count;
            if (100 <= cnt)
            {
                c.RemoveAt(0);
            }
            c.Add(rec);
        }

        // ------------------------------------------------------------------------------------------------------------
        #region コンボボックスKeyUpイベント処理

        /// <summary>
        /// コンボボックスでキーが離されたときのイベントを処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">イベント引数を指定します。</param>
        private void comboBoxKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var txt = this.comboBox.Text;
                    var list = new List<byte>();
                    list.Add((byte)0x02);
                    list.Add((byte)'R');
                    list.Add((byte)0x01);
                    list.AddRange(this.encoding.GetBytes(txt));
                    //list.Add((byte)'a');
                    //list.Add((byte)'0');
                    //list.Add((byte)'0');
                    //list.Add((byte)'1');
                    list.Add((byte)0x03);
                    var bt = list.ToArray();
                    var src = this.comboBox.Items;

                    if (src.Contains(txt))
                    {
                        src.Remove(txt);
                    }
                    this.comboBox.Items.Insert(0, txt);
                    this.comboBox.Text = "";

                    // 送信処理
                    this.tcpCommunicator.Send(bt);
                }
            }
            catch (Exception ex)
            {
                var msg = "comboBoxKeyUpでエラーが発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }
}
