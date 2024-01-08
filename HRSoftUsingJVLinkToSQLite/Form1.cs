/* 
 * File: Form1.cs
 * 
 * Author: Akira Sugiura (urasandesu@gmail.com)
 * 
 * 
 * Copyright (c) 2024 Akira Sugiura
 *  
 *  This software is MIT License.
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */

using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HRSoftUsingJVLinkToSQLite
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InstallJVLinkToSQLiteIfNeccesary();

            var jvLinkToSQLiteExe = GetjvLinkToSQLiteExe();
            var arguments = "-m init";
            textBox1.AppendText("JVLinkToSQLite を初期化します。しばらくお待ちください．．．" + "\r\n");
            ExecuteProcess(jvLinkToSQLiteExe, arguments);
            textBox1.AppendText("JVLinkToSQLite を初期化しました。" + "\r\n");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InstallJVLinkToSQLiteIfNeccesary();
            SetDefaultJVLinkToSQLiteIfNeccesary();

            var jvLinkToSQLiteExe = GetjvLinkToSQLiteExe();
            var arguments = "-m exec";
            textBox1.AppendText("JVLinkToSQLite を実行します。しばらくお待ちください．．．" + "\r\n");
            var exitCode = ExecuteProcess(jvLinkToSQLiteExe, arguments);
            textBox1.AppendText("JVLinkToSQLite を実行しました。" + "\r\n");
            if (exitCode != 0)
            {
                return;
            }

            textBox1.AppendText("\r\n");
            textBox1.AppendText("最新のレース詳細情報を一覧します。しばらくお待ちください．．．" + "\r\n");
            var jvLinkToSQLiteArtifactDirectory = GetJVLinkToSQLiteArtifactDirectory();
            var racedb = Path.Combine(jvLinkToSQLiteArtifactDirectory, "race.db");
            var connStr = new SQLiteConnectionStringBuilder(@"
PRAGMA MMAP_SIZE = 2147483648;
PRAGMA ENCODING = ""UTF-8"";
");
            connStr.DataSource = racedb;
            connStr.Version = 3;
            using (var conn = new SQLiteConnection(connStr.ToString()))
            {
                conn.Open();

                var sql = new StringBuilder();
                sql.Append("with WTMP_1 as ( ");
                sql.Append("  select distinct ");
                sql.Append("    t1.idYear ");
                sql.Append("    , t1.idMonthDay ");
                sql.Append("  from ");
                sql.Append("    NL_RA_RACE t1 ");
                sql.Append("  order by ");
                sql.Append("    t1.idYear desc ");
                sql.Append("    , t1.idMonthDay desc ");
                sql.Append("  limit ");
                sql.Append("    1 ");
                sql.Append(") ");
                sql.Append("select ");
                sql.Append("  * ");
                sql.Append("from ");
                sql.Append("  NL_RA_RACE t1 ");
                sql.Append("  inner join WTMP_1 t2 ");
                sql.Append("    on t1.idYear = t2.idYear and ");
                sql.Append("    t1.idMonthDay = t2.idMonthDay ");

                var cmd = new SQLiteCommand(sql.ToString(), conn);
                var dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                foreach (DataRow row in dt.Rows)
                {
                    var idYear = row.Field<string>("idYear");
                    var idMonthDay = row.Field<string>("idMonthDay");
                    var idJyoCD = row.Field<string>("idJyoCD");
                    var idKaiji = row.Field<string>("idKaiji");
                    var idNichiji = row.Field<string>("idNichiji");
                    var idRaceNum = row.Field<string>("idRaceNum");
                    var RaceInfoHondai = row.Field<string>("RaceInfoHondai");
                    textBox1.AppendText($"{idYear} {idMonthDay} {idJyoCD} {idKaiji} {idNichiji} {idRaceNum} {RaceInfoHondai}" + "\r\n");
                }
            }
            textBox1.AppendText("最新のレース詳細情報を一覧しました。" + "\r\n");
        }

        private void InstallJVLinkToSQLiteIfNeccesary()
        {
            var jvLinkToSQLiteArtifactDirectory = GetJVLinkToSQLiteArtifactDirectory();
            if (Directory.Exists(jvLinkToSQLiteArtifactDirectory))
            {
                return;
            }

            var jvLinkToSQLiteArtifactExe = GetJVLinkToSQLiteArtifactExe();
            if (!File.Exists(jvLinkToSQLiteArtifactExe))
            {
                return;
            }

            textBox1.AppendText("JVLinkToSQLite をインストールします。しばらくお待ちください．．．" + "\r\n");
            ExecuteProcess(jvLinkToSQLiteArtifactExe, "");
            textBox1.AppendText("JVLinkToSQLite をインストールしました。" + "\r\n");
        }

        private void SetDefaultJVLinkToSQLiteIfNeccesary()
        {
            var settingXml = GetSettingXml();
            if (File.Exists(settingXml))
            {
                return;
            }

            var jvLinkToSQLiteExe = GetjvLinkToSQLiteExe();
            var arguments = "-m defaultsetting";
            textBox1.AppendText("JVLinkToSQLite のデフォルト動作設定を生成します。しばらくお待ちください．．．" + "\r\n");
            ExecuteProcess(jvLinkToSQLiteExe, arguments);
            textBox1.AppendText("JVLinkToSQLite のデフォルト動作設定を生成しました。" + "\r\n");

            var twoWeekAgo = DateTime.Today.AddDays(-14).ToString("o");
            textBox1.AppendText("JVLinkToSQLite の動作設定を更新します。しばらくお待ちください．．．" + "\r\n");
            {
                arguments = "setting ";
                arguments += "-x \"/JVLinkToSQLiteSetting/Details/JVNormalUpdateSetting/DataSpecSettings/JVDataSpecSetting/JVKaisaiDateTimeKey/KaisaiDateTime\" ";
                arguments += $"-v \"{twoWeekAgo}\" ";
                arguments += "-f";
                ExecuteProcess(jvLinkToSQLiteExe, arguments);
            }
            {
                arguments = "setting ";
                arguments += "-x \"/JVLinkToSQLiteSetting/Details/JVNormalUpdateSetting/DataSpecSettings/JVDataSpecSetting[DataSpec='BLDN']\" ";
                arguments += "-v \"<IsEnabled>true</IsEnabled>" +
                                  "<DataSpec>BLDN</DataSpec>" +
                                  "<JVKaisaiDateTimeKey>" +
                                   $"<KaisaiDateTime>{twoWeekAgo}</KaisaiDateTime>" +
                                  "</JVKaisaiDateTimeKey>" +
                                  "<TimeIntervalUnit>P121DT18H</TimeIntervalUnit>\" ";
                arguments += "-f";
                ExecuteProcess(jvLinkToSQLiteExe, arguments);
            }
            textBox1.AppendText("JVLinkToSQLite の動作設定を更新しました。" + "\r\n");
        }

        private static string GetJVLinkToSQLiteArtifactDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var jvLinkToSQLiteArtifactDirectory = Path.Combine(currentDirectory, "JVLinkToSQLiteArtifact");
            return jvLinkToSQLiteArtifactDirectory;
        }

        private static string GetJVLinkToSQLiteArtifactExe()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var jvLinkToSQLiteArtifactExe = Path.Combine(currentDirectory, "JVLinkToSQLiteArtifact_0.1.0.0.exe");
            return jvLinkToSQLiteArtifactExe;
        }

        private static string GetjvLinkToSQLiteExe()
        {
            var jvLinkToSQLiteArtifactDirectory = GetJVLinkToSQLiteArtifactDirectory();
            var jvLinkToSQLiteExe = Path.Combine(jvLinkToSQLiteArtifactDirectory, "JVLinkToSQLite.exe");
            return jvLinkToSQLiteExe;
        }

        private static string GetSettingXml()
        {
            var jvLinkToSQLiteArtifactDirectory = GetJVLinkToSQLiteArtifactDirectory();
            var settingXml = Path.Combine(jvLinkToSQLiteArtifactDirectory, "setting.xml");
            return settingXml;
        }

        private int ExecuteProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = Path.GetDirectoryName(fileName);
            var p = Process.Start(psi);

            var task = p.StandardOutput.ReadLineAsync();
            while (!p.WaitForExit(1))
            {
                if (task.IsCompleted)
                {
                    var line = task.Result;
                    textBox1.AppendText(line + "\r\n");
                    task.Dispose();
                    task = p.StandardOutput.ReadLineAsync();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            if (p.ExitCode != 0)
            {
                MessageBox.Show($"'{fileName}' の実行中にエラーが発生しました。終了コード={p.ExitCode}");
            }

            return p.ExitCode;
        }
    }
}
