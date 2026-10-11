# JunnyMESBackup

MES·스마트팩토리 환경의 IPC 설정·레시피 파일을 검사하고 변경된 파일을 백업하는 C# Windows Forms 애플리케이션입니다. 클라이언트는 대상 폴더의 변경을 확인하고, 서버는 파일을 수신하여 시스템별 백업 폴더에 저장합니다.

현재 실행 구성은 같은 PC에서 사용하는 loopback 데모입니다. 기본 주소는 `127.0.0.1:18080`이며 외부 서버 주소는 허용하지 않습니다.

## 주요 기능

- SHA-256 기반 파일 변경 검사와 추가·수정·삭제 백업
- 프로그램 폴더별 백업 대상 선택, 검사 깊이 및 제외 목록 설정
- 클라이언트·서버 프리셋 편집, JSON 저장과 이전 설정 보관
- 검사 전용 예약 및 자동 백업 예약
- 수신 완료 후 파일 교체, 이전 파일과 삭제 파일의 버전 보관
- 백업 성공 기준과 로그 기록, 연결 실패 시 재연결

## 개발·실행 환경

| 항목 | 환경 |
|---|---|
| 운영체제 | Windows |
| 언어·UI | C# / Windows Forms |
| 프레임워크 | .NET Framework 4.8 |
| 빌드 도구 | Visual Studio 2022, .NET 데스크톱 개발 워크로드 |
| 필수 구성 요소 | .NET Framework 4.8 Targeting Pack |
| 외부 NuGet 패키지 | 없음 |

VS Code로 소스를 열고 관리할 수 있습니다. 빌드는 Visual Studio 또는 Visual Studio의 MSBuild를 사용합니다. `dotnet run`으로 실행하는 프로젝트가 아닙니다.

## 폴더와 프로젝트 구조

```text
JunnyMESBackup.sln
IPC_Backup/                  클라이언트 앱
IPC_Backup_Server/           서버 앱
JunnyMES.Core/               공통 계약과 프리셋 모델·검증
JunnyMES.Infrastructure/     파일·통신·설정 저장·예약·로그
JunnyMES.WinForms/           공통 프리셋 편집 화면과 로그 표시
config/                     예제 설정 JSON
samples/                    가상 설정·레시피 파일
README.md                   구조·실행·운영 매뉴얼
```

| 프로젝트 | 주요 구성 요소 | 책임 |
|---|---|---|
| IPC_Backup | ClientMain, PresetManager, PresetSettingWindow, ServerSettingWindow | 연결·대상 선택·변경 검사·백업 실행·클라이언트 설정 |
| IPC_Backup_Server | ServerMain, PresetManager, PresetSettingWindow | 수신 서버 시작·중지·시스템 상태 표시·서버 설정 |
| JunnyMES.Core | IPresetStore, IAppLogger, PresetSchema, PresetEditSession, PresetEditMerger, PresetValidator | 저장·로그 계약, 편집 복사본, 필드 권한, 충돌·입력 검사 |
| JunnyMES.Infrastructure | PresetManagerBase, JsonPresetStore, BackupWorkflow, WireChannel, BackupFileStore, ScheduleMaster | 공통 설정 관리, JSON 저장, 변경 계획·성공 기준, 파일 송수신·보관, 비동기 예약 |
| JunnyMES.WinForms | PresetEditorControl, PresetDialogLayout, LogListViewBinder | 양쪽 앱에서 사용하는 편집 컨트롤·창 배치·로그 표시 |

앱은 공통 계층을 참조합니다. Core는 앱과 Infrastructure를 참조하지 않으며, 공통 WinForms 계층은 Core의 편집 모델을 사용합니다. 양쪽 PresetManager는 공통 추상 클래스 PresetManagerBase를 상속하고 각 앱의 설정 초기화·병합을 담당합니다.

## 1. 빌드

1. `JunnyMESBackup.sln`을 Visual Studio에서 엽니다.
2. 구성은 **Release**, 플랫폼은 **Any CPU**를 선택합니다.
3. **빌드 → 솔루션 빌드**를 실행합니다.

Developer PowerShell for VS 2022에서는 소스 루트에서 다음 명령으로 빌드할 수도 있습니다.

```powershell
MSBuild.exe .\JunnyMESBackup.sln /t:Build /p:Configuration=Release
```

실행 파일은 다음 위치에 생성됩니다.

- 서버: `IPC_Backup_Server/bin/Release/IPC_Backup_Server.exe`
- 클라이언트: `IPC_Backup/bin/Release/IPC_Backup.exe`

## 2. 데모 데이터 준비

탐색기에서 `JunnyMESBackup.sln`이 있는 폴더를 기준으로 다음과 같이 준비합니다. 기존 `data`가 있다면 먼저 내용을 확인하고 필요한 파일을 별도로 보관하세요.

1. `data/config`, `data/source`, `data/backup` 폴더를 만듭니다.
2. `config/client.example.json`을 `data/config/client.json`으로 복사합니다.
3. `config/server.example.json`을 `data/config/server.json`으로 복사합니다.
4. `samples/DemoSystem_01` 폴더를 통째로 `data/source` 안에 복사합니다.

```text
data/
  config/
    client.json
    server.json
  source/
    DemoSystem_01/
      ProgramA/settings.ini
      ProgramB/recipe.csv
  backup/
```

로그·성공 상태·백업 하위 폴더는 필요할 때 앱에서 생성합니다. 예제의 나머지 DemoSystem 항목은 화면과 설정 구조를 보여주는 등록 항목입니다. 해당 시스템을 실행하려면 대응하는 원본 폴더와 등록 IP를 준비해야 합니다.

## 3. 실행과 백업

1. 서버 EXE를 실행합니다. 서버가 시작되면 버튼이 **Stop Server**로 표시됩니다.
2. 클라이언트 EXE를 실행합니다. 서버 연결 후 ProgramA·ProgramB 목록을 확인합니다.
3. **Check**를 누르면 선택된 프로그램의 추가·수정·삭제 내역을 검사합니다.
4. **Update**를 누르면 검사 결과를 서버로 전송합니다.
5. `data/backup/DemoSystem_01`에 저장된 파일을 확인합니다.
6. 원본 `settings.ini`의 내용을 수정한 뒤 **Check → Update**를 반복하면 변경 백업을 확인할 수 있습니다.

서버 없이 클라이언트를 실행하면 백업이 비활성화되고 **Try reconnect**가 표시됩니다. 서버를 시작한 뒤 재연결하세요. 서버·클라이언트는 같은 버전으로 함께 실행합니다.

앱은 실행 파일의 상위 폴더에서 솔루션 파일을 찾아 데이터 루트를 결정합니다. EXE만 다른 폴더로 옮기지 말고 소스·빌드 폴더 구조를 유지하세요. 별도 루트를 사용할 경우 `JUNNY_MES_DEMO_ROOT` 환경 변수로 지정할 수 있으며, 해당 루트에도 같은 `data` 구조가 필요합니다.

## 프리셋 편집과 대상 선택

### 클라이언트

- **Manage Preset**에서 해당 시스템의 검사 경로·깊이·제외 목록을 편집하고 저장합니다.
- 대상 프로그램은 **Modify → 체크 변경 → done** 순서로 선택합니다.
- 선택 또는 설정 변경 후에는 다시 **Check → Update**를 실행합니다.
- 서버 주소·포트를 바꾸려면 클라이언트의 연결 설정도 맞춘 뒤 재시작합니다.

### 서버

1. **Stop Server**로 서버를 중지합니다.
2. **Preset Settings**에서 시스템을 선택하고 설정을 편집합니다.
3. 저장 후 **Start Server**로 다시 시작합니다.

서버 실행 중에는 프리셋 저장을 허용하지 않습니다. 저장 전 입력을 검증하며, 저장에 실패하면 기존 설정을 유지합니다. 편집 중 다른 작업이 변경한 상태는 병합하고, 같은 편집 필드의 충돌은 거절합니다.

### 설정 항목

| 항목 | 의미·제약 |
|---|---|
| ServerIP / ServerPort | IPv4 loopback 주소 / 1~65535 포트 |
| registered_IP / registered_SystemName | 시스템별 연결 IP / 고유 이름 |
| checkingTargetPath | 소스 루트의 `data/source` 아래 검사 폴더 |
| savePath | 소스 루트의 `data/backup` 아래 백업 폴더 |
| folderDepth | 검사 깊이, 1~32 |
| skipKeywordsFolder / skipKeywordsFile | 제외할 폴더·파일 키워드 목록; 빈 목록 허용 |
| check_interval_type | 0: 지정 시각, 1: 반복 간격 |
| check_interval_hour/minute/second | 시 0~23, 분·초 0~59; 반복 간격은 0보다 커야 함 |
| last_Check / update_only | 앱이 관리하는 상태; 편집 화면에서 읽기 전용 |

시스템별 배열은 등록 시스템 수와 길이가 같아야 합니다. JSON 예제는 상대경로를 사용하며 앱은 이를 데이터 루트 기준으로 해석합니다. 예전 EXE 옆의 Preset.json을 자동으로 가져오지는 않습니다.

## 예약 실행

- **Check Only → RUN**: 설정된 예약으로 변경 사항만 검사합니다.
- **Auto Update → RUN**: 검사 후 변경 사항이 있으면 백업합니다.
- **STOP**: 예약·진행 작업에 취소를 전달하고 종료를 기다립니다.

반복 간격은 직전 작업이 완료된 뒤 계산합니다. 예약이나 백업 중에는 설정을 바꾸지 말고 먼저 중지하세요.

## 저장 위치와 복원

| 위치 | 내용 |
|---|---|
| data/config/client.json, server.json | 현재 설정 |
| data/config/Preset_old/ | 이전 설정 |
| data/source/ | 검사할 원본 파일 |
| data/backup/ | 시스템별 최신 백업 |
| data/backup/시스템명/.junny-mes/versions/ | 교체 전 파일·삭제 파일의 보관본 |
| data/state/client/, server/ | 로그와 실행 상태 |

**Check는 백업 성공을 의미하지 않습니다.** 전체 전송·삭제 확인과 로컬 성공 상태 저장을 마친 뒤 성공 기준을 갱신합니다. 실패하면 원인을 확인한 뒤 다시 Check → Update를 실행하세요.

수신 파일은 임시 파일에 모두 저장한 뒤 기존 파일과 교체합니다. 수신 도중 연결이 끊기면 기존 파일을 유지합니다. 여러 파일 중 일부가 이미 반영된 뒤 다른 파일이 실패할 수 있으며, 전체 작업을 한 번에 롤백하지는 않습니다.

복원할 때는 양쪽 앱을 종료하고 `versions`에서 원하는 보관본을 별도 폴더로 복사하여 내용을 확인합니다. 확인된 파일만 필요한 위치에 복원하세요. 보관본 자동 정리는 없으므로 디스크 용량을 관리해야 합니다.

## 문제 해결

| 증상 | 확인할 내용 |
|---|---|
| 빌드 시 Framework를 찾지 못함 | .NET Framework 4.8 Targeting Pack과 데스크톱 개발 워크로드 설치 |
| 서버가 시작되지 않음 | 포트 사용 여부와 서버 로그; 동일 서버를 중복 실행했는지 확인 |
| Try reconnect 표시 | 서버 실행 여부, 양쪽 ServerIP·ServerPort 일치 여부 |
| 원본 폴더 없음 오류 | 샘플 복사 위치와 checkingTargetPath; 없는 폴더는 삭제로 간주하지 않음 |
| 설정 저장 거절 | 서버·예약·작업 중지 여부, 경로·범위·배열 길이·중복 이름 확인 |
| Update 비활성화 | 연결 여부, 대상 선택, Check 실행 여부와 실제 변경 유무 확인 |
| 파일 전송 실패 | 파일 접근 권한·잠금·검사 후 원본 변경·디스크 여유 확인 후 다시 검사 |

## 적용 범위

같은 PC에서 익명 샘플 파일로 동작을 확인하는 데모 구성입니다. 인증·TLS·원격 운영, 실행 중인 DB의 일관성 백업, PLC·OS 복구는 제공하지 않습니다. 기본 15개 등록 항목은 15대 설비 운영을 의미하지 않습니다.
