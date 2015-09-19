using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfTcpServerSample
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// ハンドルされていない例外を検出した時に発生するイベントを処理します。
        /// </summary>
        /// <param name="sender">イベントを送信したオブジェクトを指定します。</param>
        /// <param name="e">イベント引数を指定します。</param>
        private void ApplicationDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            var msg = "ハンドルされていない例外が発生しました。(" + ex.GetType().ToString() + ":" + ex.Message + ")\r\n" + ex.StackTrace;
            MessageBox.Show(msg);
        }
    }
}
