namespace TcpCommunication
{
    /// <summary>
    /// クライアントからの接続を通知するイベントのイベントハンドラを定義します。
    /// </summary>
    /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
    /// <param name="e">接続されたクライアントを含むイベント引数を指定します。</param>
    public delegate void TcpCommunicatorConnectedEventHandler(object sender, TcpCommunicatorConnectedEventArgs e);

    /// <summary>
    /// クライアント接続イベントで使用する引数クラスを定義します。
    /// </summary>
    public class TcpCommunicatorConnectedEventArgs
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iTcpCommunicator">接続されたTcpCommunicatorを指定します。</param>
        public TcpCommunicatorConnectedEventArgs(TcpCommunicator iTcpCommunicator)
        {
            this.tcpCommunicator = iTcpCommunicator;
        }

        // ------------------------------------------------------------------------------------------------------------
        #region TcpCommunicatorプロパティ

        /// <summary>
        /// 接続されたTcpCommunicatorを管理します。
        /// </summary>
        private TcpCommunicator tcpCommunicator;

        /// <summary>
        /// 接続されたTcpCommunicatorを取得します。
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
