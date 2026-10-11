namespace JunnyMES.Core.Configuration
{
    public enum PresetEditorRole
    {
        Client,
        Server
    }

    public enum PresetValueKind
    {
        Single,       // 공통 단일 값: ServerPort
        PerSystem,    // 시스템별 값: folderDepth
        List          // 공통 목록: skipKeywordsFile
    }

    public class PresetFieldDefinition
    {
        public string Key { get; }
        public string DisplayName { get; }
        public PresetValueKind Kind { get; }

        public bool ClientEditable { get; }
        public bool ServerEditable { get; }

        public PresetFieldDefinition(
            string key,
            string displayName,
            PresetValueKind kind,
            bool clientEditable,
            bool serverEditable)
        {
            Key = key;
            DisplayName = displayName;
            Kind = kind;
            ClientEditable = clientEditable;
            ServerEditable = serverEditable;
        }

        public bool CanEdit(PresetEditorRole role)
        {
            return role == PresetEditorRole.Client
                ? ClientEditable
                : ServerEditable;
        }
    }
}