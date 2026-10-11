using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;

namespace JunnyMES.WinForms
{
    public partial class PresetEditorControl : UserControl
    {
        private PresetEditSession _session;
        private PresetEditorRole _role;
        private int _systemIndex;

        private bool _changingSystem;

        private ComboBox _systemSelector;
        private Label _systemLabel;
        private TableLayoutPanel _fieldTable;

        private readonly Dictionary<string, TextBox> _editors =
            new Dictionary<string, TextBox>();

        private readonly Dictionary<string, string> _loadedTexts =
            new Dictionary<string, string>();

        public PresetEditorControl()
        {
            InitializeComponent();
            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            var header = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(10)
            };

            _systemLabel = new Label
            {
                Text = "System:",
                AutoSize = true,
                Padding = new Padding(0, 5, 10, 0)
            };

            _systemSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 260
            };

            _systemSelector.SelectedIndexChanged +=
                SystemSelector_SelectedIndexChanged;

            header.Controls.Add(_systemLabel);
            header.Controls.Add(_systemSelector);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            _fieldTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Padding = new Padding(10)
            };

            _fieldTable.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 190));

            _fieldTable.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            scrollPanel.Controls.Add(_fieldTable);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(scrollPanel, 0, 1);

            Controls.Add(root);
        }

        // 외부에서 설정과 실행 역할을 주입
        public void LoadPreset(
            Dictionary<string, List<string>> preset,
            PresetEditorRole role,
            int systemIndex)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            List<string> systemNames;

            if (!preset.TryGetValue(
                    "registered_SystemName",
                    out systemNames) ||
                systemNames == null ||
                systemNames.Count == 0)
            {
                throw new InvalidOperationException(
                    "등록된 시스템 목록이 없습니다.");
            }

            if (systemIndex < 0 ||
                systemIndex >= systemNames.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(systemIndex));
            }

            _role = role;
            _systemIndex = systemIndex;

            _session = new PresetEditSession(
                preset, role, systemIndex);

            _changingSystem = true;

            _systemSelector.Items.Clear();

            for (int i = 0; i < systemNames.Count; i++)
            {
                _systemSelector.Items.Add(
                    string.Format("{0:D2} - {1}",
                        i + 1, systemNames[i]));
            }

            _systemSelector.SelectedIndex = systemIndex;

            // Client는 자기 시스템만 표시
            _systemSelector.Enabled =
                role == PresetEditorRole.Server;

            _changingSystem = false;

            RenderFields();
        }

        private void RenderFields()
        {
            if (_session == null)
                return;

            _fieldTable.SuspendLayout();

            while (_fieldTable.Controls.Count > 0) _fieldTable.Controls[0].Dispose();
            _fieldTable.RowStyles.Clear();
            _fieldTable.RowCount = 0;

            _editors.Clear();
            _loadedTexts.Clear();

            foreach (PresetFieldDefinition field
                in PresetSchema.Fields)
            {
                bool editable = field.CanEdit(_role);

                string value;

                if (field.Kind == PresetValueKind.List)
                {
                    value = string.Join(
                        Environment.NewLine,
                        _session.GetValues(field.Key));
                }
                else
                {
                    value = _session.GetValue(field.Key);
                }

                var label = new Label
                {
                    Text = field.DisplayName,
                    AutoSize = true,
                    Margin = new Padding(3, 10, 8, 3)
                };

                bool multiline =
                    field.Kind == PresetValueKind.List;

                var editor = new TextBox
                {
                    Text = value,
                    ReadOnly = !editable,
                    Multiline = multiline,
                    Height = multiline ? 75 : 25,
                    ScrollBars = multiline
                        ? ScrollBars.Vertical
                        : ScrollBars.None,
                    Anchor = AnchorStyles.Left |
                             AnchorStyles.Right,
                    Width = 400,
                    Margin = new Padding(3, 5, 3, 5),
                    BackColor = editable
                        ? SystemColors.Window
                        : SystemColors.ControlLight
                };

                int row = _fieldTable.RowCount++;

                _fieldTable.RowStyles.Add(
                    new RowStyle(SizeType.AutoSize));

                _fieldTable.Controls.Add(label, 0, row);
                _fieldTable.Controls.Add(editor, 1, row);

                _editors[field.Key] = editor;
                _loadedTexts[field.Key] = value;
            }

            _fieldTable.ResumeLayout(true);
        }

        // 입력값을 편집용 복사본에 반영
        private bool TryApplyChanges(out string error)
        {
            error = null;

            if (_session == null)
            {
                error = "Preset이 초기화되지 않았습니다.";
                return false;
            }

            // 검증에 실패하더라도 기존 편집본은 보존
            var candidate = new PresetEditSession(
                _session.GetResult(),
                _role,
                _systemIndex);

            foreach (PresetFieldDefinition field
                in PresetSchema.Fields)
            {
                if (!field.CanEdit(_role))
                    continue;

                string key = field.Key;
                string currentText = _editors[key].Text;

                // 사용자가 수정하지 않은 값은 그대로 유지
                if (currentText == _loadedTexts[key])
                    continue;

                try
                {
                    if (field.Kind == PresetValueKind.List)
                    {
                        string[] values = currentText.Split(
                            new[] { "\r\n", "\n" },
                            StringSplitOptions.None);

                        candidate.SetValues(key, values);
                    }
                    else
                    {
                        candidate.SetValue(key, currentText);
                    }
                }
                catch (Exception ex)
                {
                    error = field.DisplayName +
                            ": " + ex.Message;
                    return false;
                }
            }

            _session = candidate;
            return true;
        }

        // Server에서 시스템을 바꿔도 수정 중인 값 유지
        private void SystemSelector_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            if (_changingSystem ||
                _session == null ||
                _systemSelector.SelectedIndex < 0)
            {
                return;
            }

            int newIndex = _systemSelector.SelectedIndex;

            if (newIndex == _systemIndex)
                return;

            string error;

            if (!TryApplyChanges(out error))
            {
                MessageBox.Show(
                    this,
                    error,
                    "Preset Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _changingSystem = true;
                _systemSelector.SelectedIndex = _systemIndex;
                _changingSystem = false;

                return;
            }

            var workingPreset = _session.GetResult();

            _systemIndex = newIndex;

            _session = new PresetEditSession(
                workingPreset,
                _role,
                _systemIndex);

            RenderFields();
        }

        // 다음 단계에서 저장 버튼과 연결할 메서드
        public bool TryGetEditedPreset(
            out Dictionary<string, List<string>> result,
            out string error)
        {
            result = null;

            if (!TryApplyChanges(out error))
                return false;

            result = _session.GetResult();
            return true;
        }
    }
}
