using System;

namespace TcpCommunication
{
    /// <summary>
    /// 例外の発生を通知するイベントのイベントハンドラを定義します。
    /// </summary>
    /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
    /// <param name="e">発生した例外を含むイベント引数を指定します。</param>
    public delegate void ExceptionHappenedEventHandler(object sender, ExceptionHappenedEventArgs e);

    /// <summary>
    /// 例外通知イベントで使用する引数クラスを定義します。
    /// </summary>
    public class ExceptionHappenedEventArgs
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iException">通知する例外を指定します。</param>
        public ExceptionHappenedEventArgs(Exception iException)
        {
            this.exception = iException;
        }

        // ------------------------------------------------------------------------------------------------------------
        #region Exceptionプロパティ

        /// <summary>
        /// 通知する例外を管理します。
        /// </summary>
        private Exception exception;

        /// <summary>
        /// 通知する例外を取得します。
        /// </summary>
        public Exception Exception
        {
            get
            {
                return this.exception;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }
}
