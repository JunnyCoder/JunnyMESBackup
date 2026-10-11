using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JunnyMES.Core.Configuration;
using JunnyMES.Core.Interfaces;
using JunnyMES.Core.Logging;
using JunnyMES.WinForms;

namespace JunnyMES.WinForms.Configuration
{
    public static class PresetDialogLayout
    {
        public static void Initialize(
            Form dialog,
            Dictionary<string, List<string>> source,
            PresetEditorRole role,
            int systemIndex,
            string title,
            string successMessage,
            Action<Dictionary<string, List<string>>, Dictionary<string, List<string>>> save,
            IAppLogger logger)
        {
            if (dialog == null) throw new ArgumentNullException(nameof(dialog));
            if (save == null) throw new ArgumentNullException(nameof(save));
            var original = PresetEditMerger.Clone(source);

            dialog.Text = title;
            dialog.ClientSize = new Size(780, 600);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
            var editor = new PresetEditorControl { Dock = DockStyle.Fill };
            editor.LoadPreset(original, role, systemIndex);

            var footer = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };
            var btnSave = new Button { Text = "Save", Width = 100, Height = 30 };
            var btnCancel = new Button {
                Text = "Cancel", Width = 100, Height = 30,
                DialogResult = DialogResult.Cancel
            };
            string sourceName = role == PresetEditorRole.Server ? "ServerPresetUI" : "ClientPresetUI";

            btnSave.Click += (sender, args) =>
            {
                Dictionary<string, List<string>> edited;
                string error;
                if (!editor.TryGetEditedPreset(out edited, out error))
                {
                    logger?.Log(LogLevel.Warning, sourceName, "Preset validation failed: " + error);
                    MessageBox.Show(dialog, error, "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    save(original, edited);
                    MessageBox.Show(dialog, successMessage, "Preset Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    logger?.Log(LogLevel.Error, sourceName, "Preset save failed: " + ex.Message);
                    MessageBox.Show(dialog, ex.Message, "Save Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancel.Click += (sender, args) => {
                logger?.Log(LogLevel.Information, sourceName, "Preset editing cancelled.");
                dialog.Close();
            };

            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);
            layout.Controls.Add(editor, 0, 0);
            layout.Controls.Add(footer, 0, 1);
            dialog.Controls.Add(layout);
            dialog.AcceptButton = btnSave;
            dialog.CancelButton = btnCancel;
        }
    }
}
