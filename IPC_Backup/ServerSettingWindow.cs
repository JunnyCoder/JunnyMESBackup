using FileManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPC_Backup
{
    public partial class ServerSettingWindow : Form
    {
        private readonly ClientMain _mainWindowInstance;
        public ServerSettingWindow(ClientMain mainWindowInstance)
        {
            InitializeComponent();
            TextBox_ServerIP.Text = mainWindowInstance.Preset["ServerIP"][0];
            TextBox_ServerPort.Text = mainWindowInstance.Preset["ServerPort"][0];
            _mainWindowInstance = mainWindowInstance; // MainWindow 객체의 참조를 보존합니다.
            this.AcceptButton = BtnSaveServerSet; // btnSaveServerSetButton은 엔터 키를 눌렀을 때 클릭되길 원하는 버튼의 이름입니다.
            
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void label2_Click(object sender, EventArgs e)
        {

        }
        private void ServerSetWindow_Load(object sender, EventArgs e)
        {

        }
        private void BtnSaveServerSet_Click(object sender, EventArgs e)
        {
            try{_mainWindowInstance.SaveConnectionSettings(TextBox_ServerIP.Text,TextBox_ServerPort.Text);Close();}
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Invalid connection",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
