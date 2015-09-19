using System;

namespace TcpCommunication
{
    /// <summary>
    /// Disposeが実行されたことを通知するイベントのイベントハンドラを定義します。
    /// </summary>
    /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
    /// <param name="e">イベント引数を指定します。</param>
    public delegate void DisposingEventHandler(object sender, EventArgs e);
}
