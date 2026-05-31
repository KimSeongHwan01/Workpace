\# Workpace HANDOVER.md



\## 완료된 작업

\- 프로젝트 생성 + 폴더 구조 세팅

\- NuGet 패키지 설치

&#x20; (CommunityToolkit.Mvvm / Microsoft.Data.Sqlite / DocumentFormat.OpenXml / Microsoft.Toolkit.Uwp.Notifications)

\- Models 작성 (Project.cs / WorkTask.cs / Issue.cs)

\- DatabaseService.cs — DB 초기화 + Projects CRUD 완성

\- MainViewModel.cs — Projects CRUD 커맨드 연결

\- MainWindow.xaml — 사이드바 + 기본 레이아웃 완성

\- 앱 실행 확인 완료



\## 다음 할 작업

\- ProjectViewModel.cs 작성

\- 오른쪽 콘텐츠 영역에 프로젝트 선택 시 칸반 보드 표시

\- 칸반 보드 UI (할일 / 진행중 / 완료 컬럼)

\- 드래그앤드롭 구현



\## 현재 에러 및 미해결 사항

\- DaysLeft 바인딩 임시로 Text="진행중" 으로 대체해놓음

&#x20; → 나중에 Converter 방식으로 제대로 연결 필요



\## 현재 폴더 구조

Workpace/

├── Models/

│   ├── Project.cs

│   ├── WorkTask.cs

│   └── Issue.cs

├── ViewModels/

│   └── MainViewModel.cs

├── Views/

│   └── MainWindow.xaml

│       └── MainWindow.xaml.cs

├── Services/

│   └── DatabaseService.cs

├── App.xaml

└── docs/

&#x20;   └── HANDOVER.md

