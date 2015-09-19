using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace TcpCommunication
{
    /// <summary>
    /// TCP/IPを使用して通信するクラスを定義します。
    /// </summary>
    /// <remarks>
    /// オブジェクト生成時にサーバと接続し、破棄時に切断します。
    /// 通信中に何らかの原因で切断した場合は再接続できないため、
    /// 再度オブジェクトを作り直す必要があります。
    /// </remarks>
    public class TcpCommunicator : IDisposable
    {
        // ------------------------------------------------------------------------------------------------------------
        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iLocalEP">ローカル側のエンドポイントを指定します。</param>
        /// <param name="iRemoteEP">リモート側のエンドポイントを指定します。</param>
        public TcpCommunicator(IPEndPoint iLocalEP, IPEndPoint iRemoteEP)
        {
            // 引数のチェック
            if (iLocalEP == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }
            else if (iRemoteEP == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[1].Name;
                throw new ArgumentNullException(nm);
            }

            try
            {
                // 接続開始
                var client = new TcpClient(iLocalEP);
                client.Connect(iRemoteEP);
                var ns = client.GetStream();

                this.tcpClient = client;
                this.stream = ns;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
            this.isConnected = true;
            this.executeTask = Task.Factory.StartNew(this.Receive);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iAddress">接続するリモート側のIPアドレスを指定します。</param>
        /// <param name="iPort">接続するリモート側のポート番号を指定します。</param>
        public TcpCommunicator(IPAddress iAddress, int iPort)
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
                // 接続開始
                var client = new TcpClient();
                client.Connect(iAddress, iPort);
                var ns = client.GetStream();

                this.tcpClient = client;
                this.stream = ns;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
            this.isConnected = true;

            // 受信データ待受け開始
            this.executeTask = Task.Factory.StartNew(this.Receive);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iRemoteEP">リモート側のエンドポイントを指定します。</param>
        public TcpCommunicator(IPEndPoint iRemoteEP)
        {
            // 引数のチェック
            if (iRemoteEP == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }

            try
            {
                // 接続開始
                var client = new TcpClient();
                client.Connect(iRemoteEP);
                var ns = client.GetStream();

                this.tcpClient = client;
                this.stream = ns;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
            this.isConnected = true;

            // 受信データ待受け開始
            this.executeTask = Task.Factory.StartNew(this.Receive);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iClient">管理する通信オブジェクトを指定します。</param>
        public TcpCommunicator(TcpClient iClient)
        {
            // 引数のチェック
            if (iClient == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }

            try
            {
                var client = iClient;
                var ns = client.GetStream();

                this.tcpClient = iClient;
                this.stream = ns;
            }
            catch (Exception)
            {
                // そのまま呼び出し元にスローする。
                throw;
            }
            this.isConnected = true;

            // 受信データ待受け開始
            this.executeTask = Task.Factory.StartNew(this.Receive);
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
                    this.isConnected = false;
                    this.quitRequest = true;
                    this.executeTask.Wait();

                    this.stream.Close();
                    this.stream = null;
                    this.tcpClient.Close();
                    this.tcpClient = null;
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
        ~TcpCommunicator()
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

        /// <summary>
        /// 実際に通信を行うオブジェクトを管理します。
        /// </summary>
        private TcpClient tcpClient;

        /// <summary>
        /// データの送受信を行うNetworkStreamを管理します。
        /// </summary>
        private NetworkStream stream;

        // ------------------------------------------------------------------------------------------------------------
        #region Tagプロパティ

        /// <summary>
        /// オブジェクトを識別するためのタグを設定／取得します。
        /// </summary>
        public object Tag { get; set; }

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// データを受信してログに表示します。
        /// </summary>
        /// <remarks>
        /// この処理はアプリケーションを終了するまで実行されます。
        /// </remarks>
        private void Receive()
        {
            var ns = this.stream;
            while (!this.quitRequest)
            {
                if (ns.DataAvailable)
                {
                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            var buff = new byte[1024];
                            int sz = 0;
                            while (ns.DataAvailable)
                            {
                                sz = ns.Read(buff, 0, buff.Length);
                                if (0 < sz)
                                {
                                    ms.Write(buff, 0, sz);
                                }
                                Thread.Sleep(5);
                            }
                            this.OnDataReceived(new DataReceivedEventArgs(ms.ToArray()));
                        }
                    }
                    catch (SocketException ex)
                    {
                        var msg = "ソケットにエラーが発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                        var exception = new ApplicationException(msg, ex);
                        this.isConnected = false;
                        this.quitRequest = true;
                        this.OnExceptionHappened(new ExceptionHappenedEventArgs(exception));
                    }
                    catch (Exception ex)
                    {
                        var msg = "データの受信処理でエラーが発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                        var exception = new ApplicationException(msg, ex);
                        this.isConnected = false;
                        this.quitRequest = true;
                        this.OnExceptionHappened(new ExceptionHappenedEventArgs(exception));
                    }
                }
                Thread.Sleep(100);
            }
        }


        /// <summary>
        /// データ受信用タスク
        /// </summary>
        private Task executeTask;

        /// <summary>
        /// アプリケーションの終了要求を保持します。
        /// </summary>
        /// <remarks>
        /// 終了要求が立つとtrueを保持します。
        /// </remarks>
        private bool quitRequest = false;

        // ------------------------------------------------------------------------------------------------------------
        #region DataReceivedイベント

        /// <summary>
        /// サーバとの接続状態を管理します。
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// サーバとの接続状態を取得します。
        /// </summary>
        /// <remarks>
        /// サーバと通信可能な状態であればtrueを返します。
        /// このプロパティがfalseを返す場合、もうこのオブジェクトは使用できません。
        /// この場合は再度オブジェクトを作り直す必要があります。
        /// </remarks>
        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region DataReceivedイベント

        /// <summary>
        /// データ受信イベントを定義します。
        /// </summary>
        public event DataReceivedEventHandler DataReceived;

        /// <summary>
        /// データ受信イベントを発生させます。このメソッドは派生クラスでオーバーライドできます。
        /// </summary>
        /// <param name="e">受信データを含むイベント引数を指定します。</param>
        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            if (this.DataReceived != null)
            {
                this.DataReceived(this, e);
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region Disposingイベント

        /// <summary>
        /// Dispose通知イベントを定義します。
        /// </summary>
        public event DisposingEventHandler Disposing;

        /// <summary>
        /// Dispose通知イベントを発生させます。このメソッドは派生クラスでオーバーライドできます。
        /// </summary>
        /// <param name="e">イベント引数を指定します。</param>
        protected virtual void OnDisposing(EventArgs e)
        {
            if (this.Disposing != null)
            {
                this.Disposing(this, e);
            }
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

        /// <summary>
        /// データを送信します。
        /// </summary>
        /// <param name="iData">送信するデータを指定します。</param>
        public void Send(byte[] iData)
        {
            // 引数のチェック
            if (iData == null)
            {
                var nm = MethodBase.GetCurrentMethod().GetParameters()[0].Name;
                throw new ArgumentNullException(nm);
            }

            var ns = this.stream;
            try
            {
                ns.Write(iData, 0, iData.Length);
            }
            catch (SocketException ex)
            {
                var msg = "ソケットにエラーが発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                var exception = new ApplicationException(msg, ex);
                this.isConnected = false;
                this.quitRequest = true;
                this.OnExceptionHappened(new ExceptionHappenedEventArgs(exception));
            }
            catch (Exception ex)
            {
                var msg = "データの受信処理でエラーが発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")";
                var exception = new ApplicationException(msg, ex);
                this.isConnected = false;
                this.quitRequest = true;
                this.OnExceptionHappened(new ExceptionHappenedEventArgs(exception));
            }
        }
    }

}
