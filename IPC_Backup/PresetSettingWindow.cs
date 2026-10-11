using System;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;
using JunnyMES.WinForms.Configuration;

namespace IPC_Backup
{
    public partial class PresetSettingWindow : Form
    {
        public PresetSettingWindow(ClientMain owner)
        {
            InitializeComponent();
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            PresetDialogLayout.Initialize(this, owner.Preset,
                PresetEditorRole.Client, owner.PresetSystemIndex,
                "Client Preset Settings",
                "설정을 저장했습니다.\n설정을 안정적으로 적용하려면 프로그램을 재시작하세요.",
                owner.SavePresetEdits, owner.AppLogger);
        }
        private void PresetSettingWindow_Load(object sender, EventArgs e) { }
    }
}
