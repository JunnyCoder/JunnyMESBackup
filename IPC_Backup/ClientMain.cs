using FileManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ScheduleManager;
using System.Threading;

using JunnyMES.Core.Interfaces;
using JunnyMES.Infrastructure.Configuration;
using JunnyMES.Core.Logging;
using JunnyMES.Infrastructure.Logging;
using JunnyMES.WinForms.Logging;

namespace IPC_Backup
{
    public partial class ClientMain : Form
    {
        
        
        string localIPAdress;
        private bool _connecting;
        private IPresetStore _presetStore;
        ScheduleManager.ScheduleMaster appSchedule = new ScheduleManager.ScheduleMaster();
        PresetManager presetManager;
        int sysIndex;
        public Dictionary<string, List<string>> Preset;
        internal string checkingTargetPath;
        private readonly IAppLogger _logger;
        private LogListViewBinder _logBinder;
        public IAppLogger AppLogger => _logger;

        public ClientMain()
        {
            InitializeComponent();
            _logger = new AppLogger(new FileLogWriter(DemoPaths.State("client")));
            // 로그 발생 시 Form의 LogView에 표시
            AppLogHub.Configure(_logger);
            _logBinder = new LogListViewBinder(LogView, _logger);
            FormClosed += (sender, e) => _logBinder.Dispose();

            InitializeLogView();

            _presetStore = new JsonPresetStore(
                DemoPaths.Config("client"));

            EnterUnavailableState("Connecting...");
            // 연결 재시도는 작업 스레드에서 수행하고 닫을 때 취소한다.
            Text = "JunnyMES Backup Client";
            FormClosing += OnClientClosing;
            Shown += async (sender, e) => { _connectWork=TryConnectAsync(); await _connectWork; };
        }


        private void EnterUnavailableState(string status)
        {
            // 폼 전체를 끄면 Try reconnect까지 비활성화된다.
            Enabled = true;
            AutoModePanel.Enabled = false;
            ManualPanel.Enabled = false;
            SettingPanel.Enabled = false;
            toolStrip1.Enabled = true;
            BtnPreset.Enabled = false;
            ServerName.Enabled = false;
            BtnReconnect.Visible = true;
            BtnReconnect.Text = "Try reconnect";
            BtnReconnect.Enabled = !_connecting;
            SysName.Text = status;
            AlertChange(status, Color.LightGray, Color.Black);
        }

        private void EnterReadyState()
        {
            AlertChange("Ready - check files", Color.LightGray, Color.Black);
            SetWinodwToDefalut();
            SettingPanel.Enabled = true;
            BtnPreset.Enabled = true;
            ServerName.Enabled = true;
            BtnReconnect.Visible = false;
            SetServerName();
            SetSysName();
            InitializeListView();
        }

        private async System.Threading.Tasks.Task TryConnectAsync()
        {
            if (_connecting) return;
            _connecting = true;
            EnterUnavailableState("Connecting...");
            BtnReconnect.Enabled = false;
            _logger.Log(
                LogLevel.Information,
                "NetworkClient",
                "Preset connection sequence started."
            );

            try
            {
                // 정상 경로는 항상 서버 우선 검증·병합·저장 수행.
                var validated = await System.Threading.Tasks.Task.Run(
                    () => new PresetManager(_presetStore, _logger, _lifetime.Token));

                _lifetime.Token.ThrowIfCancellationRequested();
                var candidate = validated.Preset;
                var ips = candidate["registered_IP"];
                // 등록 판정은 실제 서버 접속에 사용된 로컬 IP 사용.
                string connectionIP = validated.LastConnectedLocalIP;
                int index = ips.IndexOf(connectionIP);
                if (index < 0)
                {
                    presetManager = validated;
                    Preset = candidate;
                    localIPAdress = connectionIP;
                    _logger.Log(
                        LogLevel.Warning,
                        "Preset",
                        "Server connected, but this system is not registered."
                    );
                    EnterUnavailableState("Not Registered system.");
                    return;
                }

                // 사용 전 필요한 설정 검증 후 기존 상태 교체.
                if (!candidate.ContainsKey("checkingTargetPath") ||
                    candidate["checkingTargetPath"] == null ||
                    index >= candidate["checkingTargetPath"].Count)
                    throw new InvalidDataException("checkingTargetPath 설정 누락");

                presetManager = validated;
                Preset = candidate;
                localIPAdress = connectionIP;
                sysIndex = index;
                checkingTargetPath = candidate["checkingTargetPath"][index];
                appSchedule.Dispose();
                appSchedule = InitializeScheduler();
                _workflow = new JunnyMES.Infrastructure.Backup.BackupWorkflow(new JunnyMES.Infrastructure.Backup.FileSnapshotScanner(), new JunnyMES.Infrastructure.Backup.JsonSuccessSnapshotStore(DemoPaths.State("client")));
                EnterReadyState();
                _logger.Log(
                    LogLevel.Information,
                    "NetworkClient",
                    "Server connected and Preset validated successfully."
                );
            }
            catch (OperationCanceledException) { }
            catch (PresetConnectionException)
            {
                // 연결/응답 실패만 로컬 설정 폴백.
                // 로컬 설정만으로 등록 성공 또는 백업 허용하지 않는다.

                try
                {
                    presetManager = PresetManager.LoadOffline(_presetStore);
                    Preset = presetManager.Preset;
                    _logger.Log(
                        LogLevel.Warning,
                        "Preset",
                        "Local Preset loaded. Offline mode activated."
                    );
                }
                catch (Exception localEx)
                {
                    presetManager = null;
                    Preset = null;
                    _logger.Log(
                        LogLevel.Error,
                        "Preset",
                        "Local Preset loading failed: " + localEx.Message
                    );
                }
                EnterUnavailableState("Server unavailable");
            }
            catch (Exception ex)
            {
                // JSON/필수 키/등록정보 등 일반 오류는 연결 실패로 취급하지 않는다.
                EnterUnavailableState("Preset initialization failed");
                if (_closing) return;
                MessageBox.Show(this, ex.Message, "Preset Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logger.Log(
                    LogLevel.Error,
                    "Preset",
                    "Preset initialization failed: " + ex.Message
                );
            }
            finally
            {
                _connecting = false;
                if (!IsDisposed && !_closing) BtnReconnect.Enabled = true;
            }
        }

        public int PresetSystemIndex => sysIndex;

        public void SavePresetEdits(
            Dictionary<string, List<string>> original,
            Dictionary<string, List<string>> edited)
        {
            // 자동 검사나 자동 백업이 실행 중이면 저장 제한
            if (_busy || appSchedule.isTimerOn || _connecting)
            {
                throw new InvalidOperationException(
                    "자동 실행을 중지한 뒤 설정을 저장하세요.");
            }

            var merged = JunnyMES.Core.Configuration.PresetEditMerger.Merge(
                presetManager.Preset,
                original,
                edited,
                JunnyMES.Core.Configuration.PresetEditorRole.Client,
                sysIndex
            );

            merged = DemoPaths.Normalize(merged);
            presetManager.SaveEditedPreset(merged);

            // 저장 성공 후 현재 설정 갱신
            presetManager.Preset = merged;
            Preset = merged;

            checkingTargetPath =
                Preset["checkingTargetPath"][sysIndex];
            _plan = null;
            InitializeListView();
        }




        // Initializes
        private ScheduleMaster InitializeScheduler()
        {
            return new ScheduleManager.ScheduleMaster((ScheduleManager.ScheduleType)int.Parse(presetManager.Preset["check_interval_type"][sysIndex]), int.Parse(presetManager.Preset["check_interval_hour"][sysIndex]), int.Parse(presetManager.Preset["check_interval_minute"][sysIndex]), int.Parse(presetManager.Preset["check_interval_second"][sysIndex]));

        }
        
        private void InitializeStripbar()
        {
            if (SetSysName())
                SetServerName();
            else
                EnterUnavailableState("Not Registered system.");
        }
        private bool SetSysName()
        {
            
            if (presetManager.Preset["registered_IP"].Contains(localIPAdress))
            {
                string SystemName = presetManager.Preset["registered_SystemName"][presetManager.Preset["registered_IP"].IndexOf(localIPAdress)];
                SysName.Text = $"System Name : {SystemName}";
                ServerName.Text = $"Target Server : {presetManager.Preset["ServerIP"][0]} ({presetManager.Preset["ServerPort"][0]})";

                return true;
            }
            else
            {
                SysName.Text = "Not Registered system.";
                //DisableAll();
                return false;
            }
        }
        public void SetServerName()
        {
            if (ServerName.Text != $"Target Server : {presetManager.Preset["ServerIP"][0]} ({presetManager.Preset["ServerPort"][0]})")
            {
                presetManager.SavePreset();
            }
            ServerName.Text = $"Target Server : {presetManager.Preset["ServerIP"][0]} ({presetManager.Preset["ServerPort"][0]})";
            
        }

        private void InitializeLogView()
        {
            LogView.Columns.Add("Time", 130);
            LogView.Columns.Add("Log", 300);
            windowActionLog("System Start");
        }
        
        private void InitializeListView()
        {
            if (listView1.Columns.Count != 3)
            { listView1.Columns.Clear(); listView1.Columns.Add("",24); listView1.Columns.Add("Program",110); listView1.Columns.Add("Status",240); }
            addItemsToListView(); AlertBox.TabStop=false;
        }
        private void addItemsToListView()
        {
            listView1.Items.Clear();
            if (Preset == null) return;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(checkingTargetPath))
            {
                foreach(string path in Directory.GetDirectories(checkingTargetPath)) names.Add(Path.GetFileName(path));
                if (Directory.GetFiles(checkingTargetPath).Length>0) names.Add(".");
            }
            if (_plan != null) foreach(string name in _plan.Programs) names.Add(name);
            List<string> selected;
            if (!Preset.TryGetValue("FilesToCheck",out selected)) selected = names.ToList();
            foreach(string name in selected) names.Add(name);
            foreach(string name in names.OrderBy(x=>x))
            {
                string status = _plan == null ? "Not checked" : string.Format("New:{0} Modified:{1} Deleted:{2}",CountProgram(_plan.Added,name),CountProgram(_plan.Modified,name),CountProgram(_plan.Deleted,name));
                var item = new ListViewItem(new[]{"",name,status});item.Checked=selected.Contains(name);item.BackColor=item.Checked?Color.White:Color.LightGray;listView1.Items.Add(item);
            }
            ShowSuccessTime();
        }



        // Toolbar clicked events.
        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
        private void SysName_Click(object sender, EventArgs e)
        {

        }
        private void BtnPreset_Click(object sender, EventArgs e)
        {
            if (!_busy) using(var dialog=new PresetSettingWindow(this)) {dialog.StartPosition=FormStartPosition.CenterParent;dialog.ShowDialog(this);}

        }
        private void ServerName_Click(object sender, EventArgs e)
        {
            if (!_busy) using(var dialog=new ServerSettingWindow(this)) {dialog.StartPosition=FormStartPosition.CenterParent;dialog.ShowDialog(this);}
        }
        private async void BtnReconnect_Click(object sender, EventArgs e)
        {
            _connectWork=TryConnectAsync();await _connectWork;
        }

        // 서브 윈도우 관련 영역.

        //UI Activation Control
        private void UITasks(Action UIModify = null)
        {
            if (UIModify != null)
            {
                if (InvokeRequired)
                {
                    Invoke((MethodInvoker)(() => { UIModify(); }));
                }
                else
                {
                    UIModify();
                }
            }
        }
        private void DisableExceptUpdate()
        {
            AutoModePanel.Enabled = false;
            //RBtnAutoUpdate.Enabled = false;
            //RBtnCheckOnly.Enabled = false;
            //BtnRun.Enabled = false;
            //BtnStop.Enabled = false;
            ManualPanel.Enabled = true;
            ManualCheck.Enabled = false;
            Modify.Enabled = false;
            ManualUpdate.Enabled = true;
            toolStrip1.Enabled = true;

        }
        private void DisableExceptStop()
        {
            AutoModePanel.Enabled = true;
            RBtnAutoUpdate.Enabled = false;
            RBtnCheckOnly.Enabled = false;
            BtnRun.Enabled = false;
            BtnStop.Enabled = true;
            ManualPanel.Enabled = false;
            toolStrip1.Enabled = false;
        }
        private void SetWinodwToDefalut()
        {
            AutoModePanel.Enabled = true;
            RBtnAutoUpdate.Enabled = true;
            RBtnCheckOnly.Enabled = true;
            BtnRun.Enabled = true;
            BtnStop.Enabled = false;
            ManualPanel.Enabled = true;
            ManualCheck.Enabled = true;
            Modify.Enabled = true;
            ManualUpdate.Enabled = false;
            toolStrip1.Enabled = true;
        }
        private void DisableAll()
        {
            toolStrip1.Enabled = false;
            AutoModePanel.Enabled = false;
            ManualPanel.Enabled = false;
        }
        private void AlertChange(string message, Color bgColor, Color fontColor)
        {
            AlertBox.Text = message;
            AlertBox.BackColor = bgColor;
            AlertBox.SelectionColor = fontColor;
            AlertBox.SelectionAlignment = HorizontalAlignment.Center;
            AlertBox.TabStop = false;
        }
        private void AlertChange(string message, Color bgColor)
        {
            AlertBox.Text = message;
            AlertBox.BackColor = bgColor;
            AlertBox.SelectionAlignment = HorizontalAlignment.Center;
            AlertBox.TabStop = false;
        }

        //Program side load
        private Task<bool> CompareHash(string message)
        { return RunWorkAsync(async token => {
            var policy = CapturePolicy();
            _plan = await Task.Run(()=>_workflow.Check(policy,token),token);
            token.ThrowIfCancellationRequested();
            addItemsToListView();
            AlertChange(_plan.HasChanges ? "Backup required" : "No changes",_plan.HasChanges?Color.LightYellow:Color.LawnGreen,Color.Black);
            windowActionLog(message + (_plan.HasChanges ? ": backup required" : ": no changes"));
            return _plan.HasChanges;
        }); }
        private Task<bool> SendChangedFiles()
        { return RunWorkAsync(async token => {
            if (_plan == null) throw new InvalidOperationException("Check before backup.");
            var plan=_plan;var preset=DemoPaths.Normalize(Preset);
            var transport=new JunnyMES.Infrastructure.Backup.LoopbackChangeSetTransport(()=>new NetworkManager.Client(preset["ServerIP"][0],preset["ServerPort"][0],3,1,_logger,cancellationToken:token));
            var result=await Task.Run(()=>_workflow.Execute(plan,transport,token),token);
            _plan=null;
            if(!result.Success)throw new IOException(result.Error);
            AlertChange("Backup successful",Color.LawnGreen,Color.Black);windowActionLog("Backup completed successfully.");addItemsToListView();return true;
        }); }
        private void BtnRun_Click(object sender,EventArgs e)
        {
            if(_busy||_connecting||appSchedule.isTimerOn)return;
            appSchedule.Dispose();appSchedule=InitializeScheduler();
            bool update=RBtnAutoUpdate.Checked;
            appSchedule.SetAsyncTask(token=>OnUiAsync(async()=>{if(token.IsCancellationRequested)return;bool changes=await CompareHash("Scheduled check");if(changes&&update&&!token.IsCancellationRequested)await SendChangedFiles();},token));
            appSchedule.Start();DisableExceptStop();windowActionLog("Schedule started.");
        }
        private async void BtnStop_Click(object sender,EventArgs e)
        { await StopWorkAsync(); if(!_closing)RenderReadyControls(); }
        private void RBtnAutoUpdate_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void RBtnCheckOnly_CheckedChanged(object sender, EventArgs e)
        {

        }
        // ManualUpdate Panel
        //To Comit
        private async void ManualUpdate_Click(object sender, EventArgs e)
        { await SendChangedFiles(); }
        private async void ManualCheck_Click(object sender, EventArgs e)
        { await CompareHash("Manual check"); }
        // listView 
        private void AddCheckBoxToHeader()
        {
            // 체크 박스 생성
            CheckBox checkBox = new CheckBox();
            checkBox.Size = new System.Drawing.Size(13, 13);
            checkBox.Name = "SelectAll";

            // 체크 박스 이벤트 핸들러 추가
            checkBox.CheckedChanged += (sender, e) =>
            {
                // 체크 박스 상태에 따라 리스트뷰의 모든 아이템의 체크 상태 변경
                foreach (ListViewItem item in listView1.Items)
                {
                    item.Checked = checkBox.Checked;
                }
            };


            // 체크 박스를 컨트롤로서 리스트뷰의 열에 추가
            listView1.Controls.Add(checkBox);
            checkBox.Location = new System.Drawing.Point(4, 5);
            
        }
        //private void ListView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        //{
        //    // 헤더 색상을 빨간색으로 변경
        //    e.Graphics.FillRectangle(Brushes.Red, e.Bounds);
        //    e.DrawText();
        //}
        //private void ListView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        //{
        //    // 배경색을 번갈아가며 설정
        //    Color backColor = e.ItemIndex % 2 == 0 ? Color.LightGray : Color.White;
        //    using (SolidBrush brush = new SolidBrush(backColor))
        //    {
        //        e.Graphics.FillRectangle(brush, e.Bounds);
        //    }
        //    // 텍스트를 중앙 정렬하여 그리기
        //    StringFormat stringFormat = new StringFormat();
        //    stringFormat.Alignment = StringAlignment.Center;
        //    stringFormat.LineAlignment = StringAlignment.Center;
        //    using (SolidBrush brush = new SolidBrush(Color.Black))
        //    {
        //        e.Graphics.DrawString(e.Item.Text, e.Item.Font, brush, e.Bounds, stringFormat);
        //    }
        //}
        private void Modify_Click(object sender, EventArgs e)
        {
            disableListViewModify();
        }
        private void disableListViewModify()
        {
            if(_busy || appSchedule.isTimerOn)return;
            if(!listView1.CheckBoxes)
            { listView1.CheckBoxes=true;Modify.Text="done";ManualCheck.Enabled=false;ManualUpdate.Enabled=false;AutoModePanel.Enabled=false;toolStrip1.Enabled=false; }
            else
            {
                try
                {
                    var next=JunnyMES.Core.Configuration.PresetEditMerger.Clone(Preset);
                    next["FilesToCheck"]=listView1.Items.Cast<ListViewItem>().Where(x=>x.Checked).Select(x=>x.SubItems[1].Text).ToList();
                    presetManager.SaveEditedPreset(next);presetManager.Preset=next;Preset=next;_plan=null;
                    listView1.CheckBoxes=false;Modify.Text="Modify";RenderReadyControls();addItemsToListView();
                }
                catch(Exception ex){windowActionLog("Selection save failed: "+ex.Message);}
            }
        }
        private void windowActionLog(string message)
        { _logger.Log(LogLevel.Operation,"Client",message);ShowSuccessTime(); }
        private void windowActionLog(string message, bool UpdateOnly)
        { windowActionLog(message); }




        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
