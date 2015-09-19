namespace TcpCommunication
{
    /// <summary>
    /// データの受信を通知するイベントのイベントハンドラを定義します。
    /// </summary>
    /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
    /// <param name="e">受信データを含むイベント引数を指定します。</param>
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    /// <summary>
    /// データ受信イベントで使用する引数クラスを定義します。
    /// </summary>
    public class DataReceivedEventArgs
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iData">受信データを指定します。</param>
        public DataReceivedEventArgs(byte[] iData)
        {
            this.receivedData = iData;
        }

        // ------------------------------------------------------------------------------------------------------------
        #region ReceivedDataプロパティ

        /// <summary>
        /// 受信データを管理します。
        /// </summary>
        private byte[] receivedData;

        /// <summary>
        /// 受信データを取得します。
        /// </summary>
        public byte[] ReceivedData
        {
            get
            {
                return this.receivedData;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }

}
