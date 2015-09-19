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

namespace WpfTcpServerSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ServerWindow : Window
    {
        // ------------------------------------------------------------------------------------------------------------
        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ServerWindow()
        {
            InitializeComponent();

            // ログがリストボックスに表示されるようにする。
            this.DataContext = this.LogCollection;

            this.AddLog("***** サーバアプリケーション開始 *****");

            // 接続待受け
            //var addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            var addr = IPAddress.Any;
            //var addr = new IPAddress(new byte[] { 192, 168, 150, 132 });
            var port = 2015;
            var acceptor = new TcpAcceptor(addr, port);
            acceptor.ExceptionHappened += AcceptorExceptionHappened;
            acceptor.TcpCommunicatorConnected += AcceptorTcpCommunicatorConnected;
            acceptor.TcpCommunicatorDisconnected += AcceptorTcpCommunicatorDisconnected;
            this.AddLog("接続待受けを開始します。(IP:" + addr.ToString() + "PORT:" + port.ToString() + ") ");

            this.tcpAcceptor = acceptor;
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
            this.tcpAcceptor.Dispose();
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorConnectedイベント処理

        /// <summary>
        /// クライアント接続通知イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">TcpCommunicatorを含むイベント引数を指定します。</param>
        private void AcceptorTcpCommunicatorConnected(object sender, TcpCommunicatorConnectedEventArgs e)
        {
            var communicator = e.TcpCommunicator;
            communicator.DataReceived += CommunicatorDataReceived;
            communicator.ExceptionHappened += CommunicatorExceptionHappened;
            communicator.Tag = this.tcpAcceptor.CommunicatorCount + 1;
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorDisconnectedイベント処理

        /// <summary>
        /// クライアント切断通知イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">TcpCommunicatorを含むイベント引数を指定します。</param>
        private void AcceptorTcpCommunicatorDisconnected(object sender, TcpCommunicatorDisconnectedEventArgs e)
        {
            var communicator = e.TcpCommunicator;
            communicator.DataReceived -= CommunicatorDataReceived;
            communicator.ExceptionHappened -= CommunicatorExceptionHappened;
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorのDataReceivedイベント処理

        /// <summary>
        /// データ受信イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">受信データを含むイベント引数を指定します。</param>
        private void CommunicatorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var d = this.Dispatcher;
            if (!d.CheckAccess())
            {
                d.Invoke(()=> this.CommunicatorDataReceived(sender, e));
            }
            else
            {
                var communicator = sender as TcpCommunicator;
                if (communicator != null)
                {
                    try
                    {
                        var data = Encoding.UTF8.GetString(e.ReceivedData);
                        this.AddLog("クライアント" + (int)communicator.Tag + ":" + data);
                        communicator.Send(e.ReceivedData);
                    }
                    catch (Exception ex)
                    {
                        var msg = "受信データの変換に失敗しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                        this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
                    }
                }
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorのExceptionHappenedイベント処理

        /// <summary>
        /// 例外通知イベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">発生した例外を含むイベント引数を指定します。</param>
        private void CommunicatorExceptionHappened(object sender, ExceptionHappenedEventArgs e)
        {
            var d = this.Dispatcher;
            if (!d.CheckAccess())
            {
                d.Invoke(() => this.CommunicatorExceptionHappened(sender, e));
            }
            else
            {
                var communicator = sender as TcpCommunicator;
                if (communicator != null)
                {
                    var ex = e.Exception;
                    var msg = ex.GetType().ToString() + ":" + ex.Message;
                    this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
                    if (!communicator.IsConnected)
                    {
                        communicator.Dispose();
                    }
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
        private void AcceptorExceptionHappened(object sender, ExceptionHappenedEventArgs e)
        {
            var d = this.Dispatcher;
            if (!d.CheckAccess())
            {
                d.Invoke(() => this.AcceptorExceptionHappened(sender, e));
            }
            else
            {
                var ex = e.Exception;
                var msg = ex.GetType().ToString() + ":" + ex.Message;
                this.AddLog(msg, LogRecord.WarningLevel.Error, ex.StackTrace);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// クライアントからのTCP接続を待受けるオブジェクトを管理します。
        /// </summary>
        private TcpAcceptor tcpAcceptor;

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
                    //var txt = this.comboBox.Text;
                    //var bt = this.encoding.GetBytes(txt);
                    //var src = this.comboBox.Items;

                    //if (src.Contains(txt))
                    //{
                    //    src.Remove(txt);
                    //}
                    //this.comboBox.Items.Insert(0, txt);
                    //this.comboBox.Text = "";

                    //// 送信処理
                    //this.tcpCommunicator.Send(bt);
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
