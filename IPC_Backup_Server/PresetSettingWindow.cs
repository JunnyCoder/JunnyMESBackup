using System;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;
using JunnyMES.WinForms.Configuration;

namespace IPC_Backup_Server
{
    public partial class PresetSettingWindow : Form
    {
        public PresetSettingWindow(ServerMain owner)
        {
            InitializeComponent();
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            PresetDialogLayout.Initialize(this, owner.PresetForEditor,
                PresetEditorRole.Server, 0,
                "Server Preset Settings",
                "서버 설정이 저장되었습니다.\n서버 프로그램을 재시작해 적용 상태를 확인하세요.",
                owner.SavePresetEdits, owner.AppLogger);
        }
    }
}
