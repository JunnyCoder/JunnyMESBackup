using FileManager;
using JunnyMES.Core.Interfaces;
using JunnyMES.Infrastructure.Configuration;
using NetworkManager;
using ScheduleManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;
using JunnyMES.Core.Logging;
using JunnyMES.Infrastructure.Logging;
using JunnyMES.WinForms.Logging;

namespace IPC_Backup_Server
{
    public partial class ServerMain : Form
    {
        private Server server;
        private System.Threading.Tasks.Task serverTask = System.Threading.Tasks.Task.CompletedTask;
        private CancellationTokenSource serverLifetime;
        private bool closing,allowClose;
        private IPresetStore presetStore;
        private readonly IAppLogger _logger;
        private LogListViewBinder _logBinder;
        public IAppLogger AppLogger => _logger;
        PresetManager presetManager;
        ScheduleManager.ScheduleMaster appSchedule = new ScheduleManager.ScheduleMaster();
        Dictionary<string, List<string>> Server_Preset;
        Dictionary<string, Button> buttonDict;
        Dictionary<string, Label> labelDict;
        System.Threading.Timer BtnCheckTimer;



        public ServerMain()
        {
            InitializeComponent();Text="JunnyMES Backup Server";
            _logger=new AppLogger(new FileLogWriter(DemoPaths.State("server")));AppLogHub.Configure(_logger);
            _logBinder=new LogListViewBinder(LogView,_logger);_logBinder.EntryDisplayed+=OnLogEntryDisplayed;
            FormClosed+=(sender,e)=>_logBinder.Dispose();FormClosing+=MainForm_FormClosing;
            var settings=new ToolStripButton("Preset Settings");settings.Click+=OpenPresetSettings;toolStrip1.Items.Add(settings);
            try{LoadPreset();Shown+=(sender,e)=>StartConfiguredServer();UpdateOutdatedChecker();}
            catch(Exception ex){_logger.Log(LogLevel.Error,"ServerPreset","Initialization failed: "+ex.Message);BtnServerStart.Enabled=false;settings.Enabled=false;}
        }

        private ScheduleMaster InitializeScheduler()
        {
            return new ScheduleManager.ScheduleMaster((ScheduleManager.ScheduleType)int.Parse(presetManager.Preset["check_interval_type"][0]), int.Parse(presetManager.Preset["check_interval_hour"][0]), int.Parse(presetManager.Preset["check_interval_minute"][0]), int.Parse(presetManager.Preset["check_interval_second"][0]));
        }


        public Dictionary<string, List<string>> PresetForEditor
            => Server_Preset;

        public void SavePresetEdits(
            Dictionary<string, List<string>> original,
            Dictionary<string, List<string>> edited)
        {
            // 서버가 동작 중이라면 설정 변경 제한
            if (!serverTask.IsCompleted)
            {
                throw new InvalidOperationException(
                    "서버를 중지한 뒤 설정을 저장하세요.");
            }

            var merged = PresetEditMerger.Merge(
                Server_Preset,
                original,
                edited,
                PresetEditorRole.Server,
                0
            );

            merged=DemoPaths.Normalize(merged);
            presetManager.SaveEditedPreset(merged);

            // 기존 Dictionary 객체 유지
            // Server와 PresetManager가 동일 객체를 참조하기 때문
            foreach (var item in merged)
            {
                Server_Preset[item.Key] = item.Value;
            }
            RebuildStationButtons();
        }

        private void OpenPresetSettings(object sender, EventArgs e)
        {
            using (var window = new PresetSettingWindow(this))
            {
                window.StartPosition = FormStartPosition.CenterParent;
                window.ShowDialog(this);
            }
        }


        void UpdateOutdatedChecker()
        { if(closing)return;BtnCheckTimer?.Dispose();BtnCheckTimer=new System.Threading.Timer(ScheduledUITask,null,TimeSpan.FromHours(1),TimeSpan.FromHours(1)); }
        void ScheduledUITask(object state)
        {if(closing||IsDisposed||!IsHandleCreated)return;try{BeginInvoke(new Action(()=>{if(!closing&&!IsDisposed)btnColorUpdate();}));}catch(InvalidOperationException){} }
        private void btnColorUpdate() 
        {
            foreach (string buttonName in buttonDict.Keys)
            {

                labelDict[buttonName].Text = "Last Updated:\n" + Server_Preset["last_Check"][Server_Preset["registered_SystemName"].IndexOf(buttonName)];

                DateTime updateDate;
                if (DateTime.TryParse(labelDict[buttonName].Text.Split('\n')[1].Trim(), out updateDate))
                {
                    TimeSpan difference = DateTime.Now - updateDate;
                    if (difference.TotalDays < 7)
                    {
                        buttonDict[buttonName].BackColor = Color.LawnGreen;
                    }
                    else
                    {
                        buttonDict[buttonName].BackColor = Color.Yellow;
                    }
                }
                else if (labelDict[buttonName].Text.Split(':')[1].Trim() == "N/A")
                {
                    buttonDict[buttonName].BackColor = Color.LightGray;
                }
                else
                {
                    buttonDict[buttonName].BackColor = Color.Red;
                }
            }
        }
        private async void BtnServerStart_Click(object sender,EventArgs e)
        {BtnServerStart.Enabled=false;try{if(serverTask.IsCompleted)StartConfiguredServer();else await StopServerAsync();}catch(Exception ex){_logger.Log(LogLevel.Error,"ServerUI",ex.Message);}finally{if(!closing)BtnServerStart.Enabled=true;}}


        private async void MainForm_FormClosing(object sender,FormClosingEventArgs e)
        {if(allowClose)return;e.Cancel=true;if(closing)return;closing=true;BtnCheckTimer?.Dispose();await StopServerAsync();appSchedule.Dispose();allowClose=true;Close();}
        private void TcpCommunication_ObjectReceived(object sender, DataReceivedEventArgs e)
        {
            // UI를 업데이트하기 위해 UI 스레드로 작업을 전달
            BeginInvoke(new Action(() =>
            {
                // UI 업데이트 작업 수행
                //labelStatus.Text = e.StatusMessage;
            }));
        }
        // Existing UI messages (e.g. periodic update check) use the same logger.
        private void AddLogViewItem(string message)
        {
            _logger.Log(LogLevel.Operation, "ServerUI", message);
        }

        private void OnLogEntryDisplayed(object sender, LogEntry entry)
        {
            // Existing IPC status refresh on server messages only.
            if (entry.Source == "NetworkServer" && buttonDict != null && labelDict != null)
            {
                if (entry.Message.StartsWith("Server start failed") || entry.Message == "Server stopped") BtnServerStart.Text = "Start Server";
                btnColorUpdate();
                if (BtnCheckTimer == null) UpdateOutdatedChecker();
            }
        }

        //private void UITasks(Action UIModify = null)
        //{
        //    if (UIModify != null)
        //    {
        //        if (InvokeRequired)
        //        {
        //            Invoke((MethodInvoker)(() => { UIModify(); }));
        //        }
        //        else
        //        {
        //            UIModify();
        //        }
        //    }
        //}

        private void LoadPreset()
        {
            string presetPath = Path.Combine(
                DemoPaths.Root,
                "data", "config", "server.json"
            );

            IPresetStore store = new JsonPresetStore(presetPath);
            presetStore=store;

            // 지역 변수를 선언하지 않고 클래스 필드에 할당
            presetManager = new PresetManager(store);
            Server_Preset = presetManager.Preset;

            List<string> systemNames =
                Server_Preset["registered_SystemName"];

            List<string> lastChecks =
                Server_Preset["last_Check"];

            List<string> savePaths =
                Server_Preset["savePath"];

            // 설정 데이터의 개수 검증
            if (systemNames.Count > lastChecks.Count ||
                systemNames.Count > savePaths.Count)
            {
                throw new InvalidDataException(
                    "Preset configuration count mismatch."
                );
            }

            buttonDict = new Dictionary<string, Button>();
            labelDict = new Dictionary<string, Label>();

            for (int i = 0; i < systemNames.Count; i++)
            {
                string systemName = systemNames[i];

                if (string.IsNullOrWhiteSpace(systemName))
                    continue;

                if (buttonDict.ContainsKey(systemName))
                {
                    throw new InvalidDataException(
                        $"Duplicated system name: {systemName}"
                    );
                }

                SetIPCBtn(systemName, i % 5, i / 5);

                Button button =
                    Controls["IPCBtn_" + systemName] as Button;

                Label label =
                    Controls["IPCUpdateLabel_" + systemName] as Label;

                if (button == null || label == null)
                {
                    throw new InvalidOperationException(
                        $"Control creation failed: {systemName}"
                    );
                }

                buttonDict.Add(systemName, button);
                labelDict.Add(systemName, label);
            }
        }



        private void SetIPCBtn(string IPCName, int XIndex, int YIndex)
        {
            Label updateLabel = new Label();
            updateLabel.Name = "IPCUpdateLabel_" + IPCName;
            int systemIndex = Server_Preset["registered_SystemName"].IndexOf(IPCName);

            if (systemIndex < 0 ||
                systemIndex >= Server_Preset["last_Check"].Count)
            {
                throw new InvalidDataException(
                    $"Invalid preset system: {IPCName}"
                );
            }

            updateLabel.Text =
                "Last Updated:\n" +
                Server_Preset["last_Check"][systemIndex];


            updateLabel.Location = new Point(25 + XIndex * 150, 135 + YIndex * 140);
            updateLabel.Margin = new Padding(0);
            updateLabel.TextAlign = ContentAlignment.TopCenter;
            updateLabel.Height = 50;
            updateLabel.Width = 130;
            Controls.Add(updateLabel);

            Button addButton = new Button();
            addButton.Name = "IPCBtn_" + IPCName;
            addButton.Text = IPCName;
            addButton.Click += IPCBtnClickFunc;
            addButton.Size = new Size(80, 80);
            addButton.Location = new Point(50 + XIndex * 150, 50 + YIndex * 140);
            addButton.Margin = new Padding(0);
            //addButton.DoubleClick += IPCBtnDoubleClickFunc;
            addButton.Tag = AppContext.BaseDirectory.ToString(); // 경로 저장.
            
            addButton.FlatStyle = FlatStyle.Flat;

            DateTime updateDate;
            if (DateTime.TryParse(updateLabel.Text.Split('\n')[1].Trim(), out updateDate))
            {
                TimeSpan difference = DateTime.Now - updateDate;
                if(difference.TotalDays < 7)
                {
                    addButton.BackColor = Color.LawnGreen;
                }
                else
                {
                    addButton.BackColor = Color.Yellow;
                }
            }
            else if (updateLabel.Text.Split(':')[1].Trim() == "N/A")
            {
                addButton.BackColor = Color.LightGray;
            }
            else
            {
                addButton.BackColor = Color.Red;
            }

            // 버튼을 MainWindow에 추가
            Controls.Add(addButton);
        }
        private void IPCBtnClickFunc(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            string buttonName = clickedButton?.Text ?? "Unknown";
            // 버튼이 클릭되었을 때 수행할 작업
            Process.Start("explorer.exe", Server_Preset["savePath"][Server_Preset["registered_SystemName"].IndexOf(buttonName)]);
        }
        private void IPCBtnDoubleClickFunc(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            string buttonPath = clickedButton?.Tag.ToString() ?? AppContext.BaseDirectory.ToString();
            // 버튼이 클릭되었을 때 수행할 작업
            Process.Start("explorer.exe", buttonPath);
        }

    }
}
