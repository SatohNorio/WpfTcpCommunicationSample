using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Log
{
    /// <summary>
    /// ログを管理する機能を提供します
    /// </summary>
    /// <remarks>
    /// このクラスはシングルトンパターンで実装されています。
    /// 直接このクラスのインスタンスを作成することはできません。
    /// Log.Instanceプロパティで唯一のインスタンスを取得して使用します。
    /// </remarks>
    public sealed class Log : IDisposable
    {
        // ------------------------------------------------------------------------------------------------------------
        #region Instanceプロパティ

        /// <summary>
        /// ログ管理オブジェクトのインスタンスを返します。
        /// </summary>
        public static Log Instance
        {
            get
            {
                return Log.instance;
            }
        }

        /// <summary>
        /// ログ管理クラスの唯一のインスタンスを保持します。
        /// </summary>
        private static Log instance = new Log();

        #endregion
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// private宣言されているので、直接インスタンスを作成することはできません。
        /// インスタンスを取得したい場合は、Log.Instanceプロパティを使用してください。
        /// </remarks>
        private Log()
        {
            // 初期値にカレントディレクトリを設定する
            var path = Assembly.GetExecutingAssembly().Location;
            this.LogPath = Path.GetDirectoryName(path) + @"\Log\";

            // プログラム名を取得する。
            this.programName = Path.GetFileNameWithoutExtension(path);

            // Logフォルダが無い場合は作成する
            if (!Directory.Exists(this.LogPath))
            {
                Directory.CreateDirectory(this.LogPath);
            }
        }

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
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
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
        ~Log()
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
            // GC.SuppressFinalize(this);
        }
        #endregion

        #region LogPathプロパティ

        /// <summary>
        /// ログを保存するパスを管理します。
        /// </summary>
        public string LogPath
        {
            get
            {
                return this.logPath;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    this.logPath = value;
                }
                else
                {
                    var s = (value.Substring(value.Length - 1) == @"\") ? value : value + @"\";
                    this.logPath = s;
                }
            }
        }

        /// <summary>
        /// ログを保存するパスを保持します。
        /// </summary>
        private string logPath = null;

        #endregion

        /// <summary>
        /// LogPathプロパティに設定されているパスの有効状態を取得します。
        /// </summary>
        /// <remarks>
        /// LogPathプロパティで設定されているパスにログの保存が可能ならばtrueを返します。
        /// </remarks>
        public bool Enabled
        {
            get
            {
                return (String.IsNullOrEmpty(this.LogPath) && Directory.Exists(this.LogPath));
            }
        }

        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// <param name="iPath">保存先のパスを指定します。</param>
        public void Initialize(string iPath)
        {
            this.LogPath = iPath;
        }

        /// <summary>
        /// スレッドセーフなFIFOバッファのオブジェクトを保持します。
        /// </summary>
        private ConcurrentQueue<LogRecord> queue = new ConcurrentQueue<LogRecord>();

        /// <summary>
        /// ログのキューが空になるまで待機する
        /// </summary>
        public void WaitLog()
        {
            int n = 0;
            do
            {
                Thread.Sleep(100);
                if (this.Enabled)
                {
                    n = this.queue.Count;
                }
            } while (0 < n);
        }

        /// <summary>
        /// ログをファイルに書き込みます。
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <param name="iLevel">ログの重要度を表す警告レベルを指定します。</param>
        /// <param name="iDescription">ログの詳細情報を指定します。このパラメータは省略できます。</param>
        public void Write(string iMsg, LogRecord.WarningLevel iLevel, string iDescription = "")
        {
            this.queue.Enqueue(new LogRecord(iMsg, iLevel, iDescription));
        }

        /// <summary>
        /// ログの保存を行います。
        /// </summary>
        private void Execute()
        {
            bool bQuit = false;
            LogRecord lgRec = null;
            DateTime lgDate = DateTime.MinValue;
            bool lgLogDeleteRequest = false;
            string lgFileName = null;

            while (bQuit)
            {
                try
                {
                    // ログ情報を取り出したらファイルに書き込む
                    if (lgRec != null)
                    {
                        // 日付が変わったかどうか判定
                        var nwDate = lgRec.LoggingDate;
                        if (lgDate.DayOfYear != nwDate.DayOfYear)
                        {
                            lgLogDeleteRequest = !String.IsNullOrEmpty(lgFileName);
                            lgDate = nwDate;
                        }

                        // ファイルに書き込み
                        if (this.Enabled)
                        {
                            // 初回時または日付が変わったタイミングで
                            // ファイル名を作成する
                            if (String.IsNullOrEmpty(lgFileName))
                            {
                                var f = this.programName + "_" + lgRec.LoggingDate.Date.ToString("dd") + ".log";
                                lgFileName = this.LogPath + f;
                            }

                            // ファイル削除要求が立っている(svLogDeleteRequest=True)のとき、AppendフラグはFalseを設定する
                            using (var sw = new StreamWriter(lgFileName, !lgLogDeleteRequest))
                            {
                                sw.WriteLine(lgRec.ToString());
                            }

                            // 任意のパスにログ情報を保存する場合、ネットワークの状態により保存できない場合があるので
                            // 保存できるまでログ情報は破棄しない。
                            lgRec = null;
                            lgLogDeleteRequest = false;
                        }
                    }

                    // 未処理件数
                    var remain = 0;

                    // 書き込み終わったら次のログを取り出す
                    if (lgRec == null)
                    {
                        remain = this.queue.Count;
                        if (0 < remain)
                        {
                            // キューからログデータを取り出す。
                            // 失敗しても処理は続行する。
                            this.queue.TryDequeue(out lgRec);
                        }
                    }
                    // 終了判定が先に処理されないために、メモリバリアで同期する。
                    Thread.MemoryBarrier();

                    // 終了判定
                    // 1.終了要求が立つ
                    // 2.未処理のログ件数が0になる。または、フォルダにアクセスできない。
                    bQuit = this.quitRequest && (remain == 0 || this.Enabled);

                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    Thread.Sleep(10000);
                }
            }
        }

        /// <summary>
        /// ログ保存処理の終了要求を保持します。
        /// </summary>
        /// <remarks>
        /// オブジェクトの終了要求が立ったらtrueを保持します。
        /// </remarks>
        private bool quitRequest = false;

        /// <summary>
        /// ファイル名に使用するプログラム名を保持します。
        /// </summary>
        private string programName = null;
    }

    /// <summary>
    /// 1件のログを表現するクラスを定義します。
    /// </summary>
    public class LogRecord
    {
        /// <summary>
        /// ログの重要度となる警告レベルを定義します。
        /// </summary>
        public enum WarningLevel
        {
            /// <summary>
            /// 重要度の低い通常のログを表します。
            /// </summary>
            Normal,

            /// <summary>
            /// 通常のログとは区別したい情報を持つログを表します。
            /// </summary>
            Information,

            /// <summary>
            /// 好ましくない動作をしたときに通知するためのログを表します。
            /// </summary>
            Notice,

            /// <summary>
            /// アプリケーションの動作は続行できますが、深刻な問題が発生した時に出力するログを表します。
            /// </summary>
            Warning,

            /// <summary>
            /// アプリケーションが正常な状態ではなくなってしまう、致命的な問題が発生した時に出力するログを表します。
            /// </summary>
            Error,
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        public LogRecord(string iMsg) : this(iMsg, WarningLevel.Normal)
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <param name="iLevel">ログの重要度を表す警告レベルを指定します。</param>
        public LogRecord(string iMsg, WarningLevel iLevel) : this(iMsg, iLevel, "")
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <param name="iLevel">ログの重要度を表す警告レベルを指定します。</param>
        /// <param name="iDescription">ログの詳細情報を指定します。</param>
        public LogRecord(string iMsg, WarningLevel iLevel, string iDescription) : this(iMsg, iLevel, iDescription, DateTime.Now)
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iMsg">ログのメイン情報となるメッセージを指定します。</param>
        /// <param name="iLevel">ログの重要度を表す警告レベルを指定します。</param>
        /// <param name="iDescription">ログの詳細情報を指定します。</param>
        /// <param name="iDateTime">ログの作成日時を指定します。</param>
        public LogRecord(string iMsg, WarningLevel iLevel, string iDescription, DateTime iDateTime)
        {
            this.message = iMsg;
            this.description = iDescription;
            this.loggingDate = iDateTime;
            this.level = iLevel;
        }

        // ------------------------------------------------------------------------------------------------------------
        #region Messageプロパティ

        /// <summary>
        /// ログのメイン情報となるメッセージを保持します。
        /// </summary>
        private string message;

        /// <summary>
        /// ログのメイン情報となるメッセージを取得します。
        /// </summary>
        public string Message
        {
            get
            {
                return this.message;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region Descriptionプロパティ

        /// <summary>
        /// ログの詳細情報を保持します。
        /// </summary>
        private string description;

        /// <summary>
        /// ログの詳細情報を取得します。
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region LoggingDateプロパティ

        /// <summary>
        /// ログの作成日時を保持します。
        /// </summary>
        private DateTime loggingDate;

        /// <summary>
        /// ログの作成日時を取得します。
        /// </summary>
        public DateTime LoggingDate
        {
            get
            {
                return this.loggingDate;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region Levelプロパティ

        /// <summary>
        /// ログの重要度を表す警告レベルを保持します。
        /// </summary>
        private WarningLevel level;

        /// <summary>
        /// ログの重要度を表す警告レベルを取得します。
        /// </summary>
        public WarningLevel Level
        {
            get
            {
                return this.level;
            }
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
        // ------------------------------------------------------------------------------------------------------------
        #region ToStringメソッド

        /// <summary>
        /// ログの内容を文字列に変換して返します。
        /// </summary>
        /// <returns>変換した文字列を返します。</returns>
        public override string ToString()
        {
            var s = this.LoggingDate.ToString() + " " + this.GetWarningLevelFixedString() + this.GetLogMessage();
            return s;
        }

        /// <summary>
        /// ログの内容を文字列に変換して返します。
        /// </summary>
        /// <param name="iFormat">ログの日付部分のフォーマットを指定します。</param>
        /// <returns>変換した文字列を返します。</returns>
        public string ToString(string iFormat)
        {
            var s = this.LoggingDate.ToString(iFormat) + " " + this.GetWarningLevelFixedString() + this.GetLogMessage();
            return s;
        }

        /// <summary>
        /// ログの内容を文字列に変換して返します。
        /// </summary>
        /// <param name="iProvider"></param>
        /// <returns>変換した文字列を返します。</returns>
        public string ToString(IFormatProvider iProvider)
        {
            var s = this.LoggingDate.ToString(iProvider) + " " + this.GetWarningLevelFixedString() + this.GetLogMessage();
            return s;
        }

        /// <summary>
        /// 警告レベルをログ用の固定長フォーマット文字列に変換して返します。
        /// </summary>
        /// <returns></returns>
        private string GetWarningLevelFixedString()
        {
            string s = "";
            if (WarningLevel.Normal != this.Level)
            {
                s = "[" + this.Level.ToString() + "]";
            }
            return s.PadRight(10);
        }

        /// <summary>
        /// ログのメッセージに詳細情報があれば付加した文字列を作成して返します。
        /// </summary>
        /// <returns></returns>
        private string GetLogMessage()
        {
            var s = this.Message;
            if (!String.IsNullOrEmpty(this.Description))
            {
                s += "\r\n" + this.Description;
            }
            return s;
        }

        #endregion
        // ------------------------------------------------------------------------------------------------------------
    }
}
