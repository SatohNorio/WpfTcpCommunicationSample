using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace TcpCommunication
{
    /// <summary>
    /// クライアントからのTCPによる接続を待受けるクラスを定義します。
    /// </summary>
    public class TcpAcceptor : IDisposable
    {
        // ------------------------------------------------------------------------------------------------------------
        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iAddress">ローカルのIPアドレスを指定します。</param>
        /// <param name="iPort">待受けに使用するポート番号を指定します。</param>
        public TcpAcceptor(IPAddress iAddress, int iPort)
        {
            // 引数のチェック
            if (iAddress == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }
            else if (iPort <= 0)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[1].Name;
                throw new ArgumentOutOfRangeException(nm, "不正なポート番号が指定されました。");
            }

            try
            {
                // 待受け開始
                var listener = new TcpListener(iAddress, iPort);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptCallback), null);

                this.tcpListener = listener;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iLocalEP">ローカル側のエンドポイントを指定します。</param>
        public TcpAcceptor(IPEndPoint iLocalEP)
        {
            // 引数のチェック
            if (iLocalEP == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }

            try
            {
                // 待受け開始
                var listener = new TcpListener(iLocalEP);
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptCallback), null);

                this.tcpListener = listener;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// クライアント接続待受けオブジェクトを管理します。
        /// </summary>
        private TcpListener tcpListener;

        /// <summary>
        /// 接続しているクライアントのリストを管理します。
        /// </summary>
        private List<TcpCommunicator> tcpCommunicatorList = new List<TcpCommunicator>();

        /// <summary>
        /// 接続待受けスレッド
        /// </summary>
        /// <param name="iResult">非同期処理の結果を指定します。</param>
        private void AcceptCallback(IAsyncResult iResult)
        {
            var listener = this.tcpListener;
            TcpCommunicator communicator = null;
            try
            {
                lock (listener)
                {
                    // クライアントと接続
                    communicator = new TcpCommunicator(listener.EndAcceptTcpClient(iResult));
                }

                // クライアント接続を通知
                this.OnTcpCommunicatorConnected(new TcpCommunicatorConnectedEventArgs(communicator));
                if (communicator.IsConnected)
                {
                    // 接続が維持されている場合はリストに追加する。
                    communicator.Disposing += CommunicatorDisposing;
                    this.tcpCommunicatorList.Add(communicator);
                }
                else
                {
                    // 接続イベント内で切断された場合は破棄する。
                    communicator.Dispose();
                }
            }
            catch (Exception ex)
            {
                var msg = "接続待受け処理で例外が発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                var exception = new ApplicationException(msg, ex);
                this.OnExceptionHappened(new ExceptionHappenedEventArgs(exception));
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        #region Disposingイベント処理

        /// <summary>
        /// TcpCpmminicatorのDisposingイベントの処理を行います。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">イベント引数を指定します。</param>
        private void CommunicatorDisposing(object sender, EventArgs e)
        {
            var communicator = sender as TcpCommunicator;
            if (communicator != null)
            {
                if (this.tcpCommunicatorList.Contains(communicator))
                {
                    this.tcpCommunicatorList.Remove(communicator);
                    communicator.Disposing -= CommunicatorDisposing;
                }
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region IDisposable Support

        /// <summary>
        /// リソースが既に解放されていればtrueを保持します。
        /// </summary>
        /// <remarks>
        /// 重複して解放処理が行われないようにするために使用します。
        /// </remarks>
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        /// <summary>
        /// オブジェクトの破棄処理
        /// </summary>
        /// <param name="disposing">マネージオブジェクトを破棄する場合はtrueを指定します。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    foreach (var item in this.tcpCommunicatorList)
                    {
                        item.Disposing -= CommunicatorDisposing;

                        // クライアント切断を通知
                        this.OnTcpCommunicatorDisconnected(new TcpCommunicatorDisconnectedEventArgs(item));
                        item.Dispose();
                    }
                    this.tcpCommunicatorList.Clear();
                    this.tcpCommunicatorList = null;

                    this.tcpListener.Stop();
                    this.tcpListener = null;
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        /// <summary>
        /// ファイナライザ
        /// </summary>
        /// <remarks>
        /// アンマネージリソースを解放します。
        /// </remarks>
        ~TcpAcceptor()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        /// <summary>
        /// オブジェクトの終了処理を行います。
        /// </summary>
        /// <remarks>
        /// このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        /// </remarks>
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            GC.SuppressFinalize(this);
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region ExceptionHappenedイベント

        /// <summary>
        /// 例外発生イベントを定義します。
        /// </summary>
        public event ExceptionHappenedEventHandler ExceptionHappened;

        /// <summary>
        /// 例外発生イベントを発生させます。このメソッドは派生クラスでオーバーライドできます。
        /// </summary>
        /// <param name="e">発生した例外を含むイベント引数を指定します。</param>
        protected virtual void OnExceptionHappened(ExceptionHappenedEventArgs e)
        {
            if (this.ExceptionHappened != null)
            {
                this.ExceptionHappened(this, e);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorConnectedイベント

        /// <summary>
        /// クライアント接続イベントを定義します。
        /// </summary>
        public event TcpCommunicatorConnectedEventHandler TcpCommunicatorConnected;

        /// <summary>
        /// クライアント接続イベントを発生させます。このメソッドは派生クラスでオーバーライドできます。
        /// </summary>
        /// <param name="e">接続されたクライアントを含むイベント引数を指定します。</param>
        protected virtual void OnTcpCommunicatorConnected(TcpCommunicatorConnectedEventArgs e)
        {
            if (this.TcpCommunicatorConnected != null)
            {
                this.TcpCommunicatorConnected(this, e);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorDisconnectedイベント

        /// <summary>
        /// クライアント接続イベントを定義します。
        /// </summary>
        public event TcpCommunicatorDisconnectedEventHandler TcpCommunicatorDisconnected;

        /// <summary>
        /// クライアント接続イベントを発生させます。このメソッドは派生クラスでオーバーライドできます。
        /// </summary>
        /// <param name="e">接続されたクライアントを含むイベント引数を指定します。</param>
        protected virtual void OnTcpCommunicatorDisconnected(TcpCommunicatorDisconnectedEventArgs e)
        {
            if (this.TcpCommunicatorDisconnected != null)
            {
                this.TcpCommunicatorDisconnected(this, e);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region CommunicatorCountプロパティ

        /// <summary>
        /// 接続しているTcpCommunicatorの数を取得します。
        /// </summary>
        public int CommunicatorCount
        {
            get
            {
                return this.tcpCommunicatorList.Count;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }
}
