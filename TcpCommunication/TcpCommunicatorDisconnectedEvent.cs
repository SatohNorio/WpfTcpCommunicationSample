namespace TcpCommunication
{
    /// <summary>
    /// クライアントからの切断を通知するイベントのイベントハンドラを定義します。
    /// </summary>
    /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
    /// <param name="e">切断されるクライアントを含むイベント引数を指定します。</param>
    public delegate void TcpCommunicatorDisconnectedEventHandler(object sender, TcpCommunicatorDisconnectedEventArgs e);

    /// <summary>
    /// クライアント切断イベントで使用する引数クラスを定義します。
    /// </summary>
    public class TcpCommunicatorDisconnectedEventArgs
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iTcpCommunicator">切断されるTcpCommunicatorを指定します。</param>
        public TcpCommunicatorDisconnectedEventArgs(TcpCommunicator iTcpCommunicator)
        {
            this.tcpCommunicator = iTcpCommunicator;
        }

        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorプロパティ

        /// <summary>
        /// 切断されるTcpCommunicatorを管理します。
        /// </summary>
        private TcpCommunicator tcpCommunicator;

        /// <summary>
        /// 切断されるTcpCommunicatorを取得します。
        /// TcpCommunicatorのDisposeは呼び出し元で行います。このオブジェクトはDisposeしないで下さい。
        /// </summary>
        public TcpCommunicator TcpCommunicator
        {
            get
            {
                return this.tcpCommunicator;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }
}
