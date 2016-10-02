﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TuringMachine.Core;
using TuringMachine.Core.Enums;
using TuringMachine.Core.Inputs;
using TuringMachine.Forms;

namespace TuringMachine
{
    public partial class FMain : Form
    {
        int _Test = 0, _Crash = 0, _Fails = 0;
        FuzzerServer _Fuzzer;

        /// <summary>
        /// Allow edit when is playing
        /// </summary>
        const bool AllowHotEdit = false;

        public FMain()
        {
            InitializeComponent();

            // Create fuzzer
            _Fuzzer = new FuzzerServer();
            _Fuzzer.OnInputsChange += _Fuzzer_OnInputsChange;
            _Fuzzer.OnConfigurationsChange += _Fuzzer_OnConfigurationsChange;
            _Fuzzer.OnListenChange += _Fuzzer_OnListenChange;
            _Fuzzer.OnTestEnd += _Fuzzer_OnTestEnd;
            _Fuzzer.OnCrashLog += _Fuzzer_OnCrashLog;

            // Configure grids
            gridConfig.AutoGenerateColumns = false;
            gridInput.AutoGenerateColumns = false;
            gridLog.AutoGenerateColumns = false;

            // Default values
            for (int x = 0; x < 100; x++)
            {
                AddToSerie(chart1.Series["Test"], 0);
                AddToSerie(chart1.Series["Crash"], 0);
                AddToSerie(chart1.Series["Fails"], 0);
            }

            _Fuzzer_OnListenChange(null, null);
        }
        void _Fuzzer_OnListenChange(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = _Fuzzer.Listen.ToString();
            toolStripStatusLabel2.ForeColor = Color.Black;
        }
        #region Add to grids
        void _Fuzzer_OnCrashLog(object sender, FuzzerLog log)
        {
            if (InvokeRequired)
            {
                Invoke(new FuzzerServer.delOnCrashLog(_Fuzzer_OnCrashLog), sender, log);
                return;
            }
            gridLog.DataSource = _Fuzzer.Logs;
            toolStripLabel2.Text = "Crashes" + (_Fuzzer.Logs.Length <= 0 ? "" : " (" + _Fuzzer.Logs.Length.ToString() + ")");
        }
        void _Fuzzer_OnConfigurationsChange(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(_Fuzzer_OnConfigurationsChange), sender, e);
                return;
            }
            gridConfig.DataSource = _Fuzzer.Configurations.ToArray();
            toolStripLabel3.Text = "Configurations" + (_Fuzzer.Configurations.Count <= 0 ? "" : " (" + _Fuzzer.Configurations.Count.ToString() + ")");
        }
        void _Fuzzer_OnInputsChange(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(_Fuzzer_OnInputsChange), sender, e);
                return;
            }
            gridInput.DataSource = _Fuzzer.Inputs.ToArray();
            toolStripLabel1.Text = "Inputs" + (_Fuzzer.Inputs.Count <= 0 ? "" : " (" + _Fuzzer.Inputs.Count.ToString() + ")");
        }
        #endregion
        #region Add Inputs
        void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                DefaultExt = "*",
                Filter = "All files|*"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    foreach (string s in dialog.FileNames)
                        _Fuzzer.AddInput(new FileFuzzingInput(s));
            }
        }
        void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    foreach (FileInfo s in new DirectoryInfo(dialog.SelectedPath).GetFiles())
                    {
                        if (File.Exists(s.FullName))
                            _Fuzzer.AddInput(new FileFuzzingInput(s.FullName));
                    }
            }
        }
        void socketToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (EndPointDialog dialog = new EndPointDialog() { ShowRequestFile = true })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.RequestFile) || File.Exists(dialog.RequestFile))
                        _Fuzzer.AddInput(new TcpQueryFuzzingInput(dialog.EndPoint, dialog.RequestFile));
                }
            }
        }
        void executeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ExecuteDialog dialog = new ExecuteDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    _Fuzzer.AddInput(new ExecutionFuzzingInput(dialog.FileName, dialog.Arguments));
            }
        }
        void randomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (LongDialog dialog = new LongDialog()
            {
                Title = "Stream length:",
                MinValue = 0,
                MaxValue = long.MaxValue,
                Input = 1024 * 1024
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    _Fuzzer.AddInput(new RandomFuzzingInput(dialog.Input));
            }
        }
        #endregion
        #region Add Configs
        void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                DefaultExt = "*",
                Filter = "Turing Machine Mutation files|*.fmut"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    foreach (string file in dialog.FileNames)
                        _Fuzzer.AddConfig(file);
            }
        }
        void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    foreach (FileInfo s in new DirectoryInfo(dialog.SelectedPath).GetFiles())
                        _Fuzzer.AddConfig(s.FullName);
            }
        }
        #endregion
        #region Test End
        void _Fuzzer_OnTestEnd(object sender, ETestResult result)
        {
            _Test++;
            switch (result)
            {
                case ETestResult.Crash: _Crash++; break;
                case ETestResult.Fail: _Fails++; break;
            }
        }
        void timer1_Tick(object sender, EventArgs e)
        {
            AddToSerie(chart1.Series["Test"], _Test);
            AddToSerie(chart1.Series["Crash"], _Crash);
            AddToSerie(chart1.Series["Fails"], _Fails);

            _Test = _Crash = _Fails = 0;
        }
        void AddToSerie(Series series, int r)
        {
            if (series.Points.Count > 100)
                series.Points.RemoveAt(0);

            series.Points.Add(r);
        }
        #endregion
        void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            using (EndPointDialog dialog = new EndPointDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    try { _Fuzzer.Listen = dialog.EndPoint; }
                    catch
                    {
                        toolStripStatusLabel2.ForeColor = Color.Red;
                    }
            }
        }
        void tbPlay_Click(object sender, EventArgs e)
        {
            if (_Fuzzer.Play())
            {
                tbPlay.Enabled = false;
                tbPause.Enabled = true;
                tbStop.Enabled = true;

                if (!AllowHotEdit)
                {
                    toolStripDropDownButton1.Enabled = false;
                    toolStripDropDownButton2.Enabled = false;
                }
            }
        }
        void tbPause_Click(object sender, EventArgs e)
        {
            if (_Fuzzer.Pause())
            {
                tbPause.Enabled = false;
                tbPlay.Enabled = true;

                toolStripDropDownButton1.Enabled = true;
                toolStripDropDownButton2.Enabled = true;
            }
        }
        void tbStop_Click(object sender, EventArgs e)
        {
            if (_Fuzzer.Stop())
            {
                tbPlay.Enabled = true;
                tbPause.Enabled = false;
                tbStop.Enabled = false;

                toolStripDropDownButton1.Enabled = true;
                toolStripDropDownButton2.Enabled = true;
            }
        }
        void gridLog_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (gridLog.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                FuzzerLog log = (FuzzerLog)gridLog.Rows[e.RowIndex].DataBoundItem;
                if (Directory.Exists(log.Path))
                    Process.Start(log.Path);
            }
        }
    }
}