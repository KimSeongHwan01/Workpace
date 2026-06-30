using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Formats.Tar;
using System.IO;
using System.Security.Cryptography;
using Workpace.Models;

namespace Workpace.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            var dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "workpace.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        // ───────────────────────────────────────
        // DB 초기화 — 앱 실행 시 테이블이 없으면 자동 생성
        // ───────────────────────────────────────
        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                CREATE TABLE IF NOT EXISTS Projects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type TEXT,
                    StartDate TEXT,
                    Deadline TEXT,
                    Description TEXT,
                    GitHubUrl TEXT,
                    Background TEXT,
                    TechReason TEXT,
                    Role TEXT,
                    Architecture TEXT,
                    TechStack TEXT,
                    RetrospectLearn TEXT,
                    RetrospectRegret TEXT,
                    RetrospectImprove TEXT
                );
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Title TEXT NOT NULL,
                    Description TEXT DEFAULT '',
                    Status TEXT DEFAULT '할일',
                    Priority TEXT DEFAULT '보통',
                    DueDate TEXT,
                    Stage TEXT DEFAULT '기획',
                    Progress INTEGER DEFAULT 0,
                    IsCore INTEGER DEFAULT 0,
                    CoreLockedAt TEXT DEFAULT '',
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
                );
                CREATE TABLE IF NOT EXISTS Issues (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER,
                    Problem TEXT,
                    Cause TEXT,
                    Solution TEXT,
                    Result TEXT,
                    CreatedAt TEXT,
                    FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
                );
                CREATE TABLE IF NOT EXISTS Streaks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT,
                    WorkDone INTEGER DEFAULT 0,
                    UNIQUE(Date)  -- 같은 프로젝트, 같은 날 중복 방지
                );
                CREATE TABLE IF NOT EXISTS ActivityLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Description TEXT,
                    CreatedAt TEXT
                );
                CREATE TABLE IF NOT EXISTS Files (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId INTEGER,
                FileName TEXT,
                FilePath TEXT,
                FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
                );
                CREATE TABLE IF NOT EXISTS UserProfile (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    Email TEXT,
                    GitHub TEXT,
                    Blog TEXT,
                    LinkedIn TEXT,
                    Bio TEXT,
                    StreakReminderEnabled INTEGER DEFAULT 1,
                    ProjectDeadlineAlertEnabled INTEGER DEFAULT 1,
                    TaskDeadlineAlertEnabled INTEGER DEFAULT 1,
                    StreakReminderIntervalHours INTEGER DEFAULT 1
                );
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // CREATE — 새 프로젝트 저장
        // INSERT INTO로 새 행을 추가하고,
        // last_insert_rowid()로 방금 저장된 Id를 가져와서 반환
        // ───────────────────────────────────────
        public int AddProject(Project project)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO Projects (Name, Type, StartDate, Deadline, Description, GitHubUrl, Background, TechReason, Role, Architecture, TechStack)
                VALUES (@Name, @Type, @StartDate, @Deadline, @Description, @GitHubUrl, @Background, @TechReason, @Role, @Architecture, @TechStack);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            // @파라미터 방식을 쓰는 이유 — SQL Injection 방지 + 특수문자 자동 처리
            cmd.Parameters.AddWithValue("@Name", project.Name);
            cmd.Parameters.AddWithValue("@Type", project.Type);
            cmd.Parameters.AddWithValue("@StartDate", project.StartDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Deadline", project.Deadline.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Description", project.Description);
            cmd.Parameters.AddWithValue("@GitHubUrl", project.GitHubUrl);
            cmd.Parameters.AddWithValue("@Background", project.Background);
            cmd.Parameters.AddWithValue("@TechReason", project.TechReason);
            cmd.Parameters.AddWithValue("@Role", project.Role);
            cmd.Parameters.AddWithValue("@Architecture", project.Architecture);
            cmd.Parameters.AddWithValue("@TechStack", project.TechStack);

            // ExecuteScalar — 단일 값 하나를 반환받을 때 사용
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        public int AddTask(WorkTask workTask)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO Tasks (ProjectId, Title, Description, Status, Priority, DueDate, Stage, Progress, IsCore)
                VALUES (@ProjectId, @Title, @Description, @Status, @Priority, @DueDate, @Stage, @Progress, @IsCore);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            // @파라미터 방식을 쓰는 이유 — SQL Injection 방지 + 특수문자 자동 처리
            cmd.Parameters.AddWithValue("@ProjectId", workTask.ProjectId);
            cmd.Parameters.AddWithValue("@Title", workTask.Title);
            cmd.Parameters.AddWithValue("@Description", workTask.Description);
            cmd.Parameters.AddWithValue("@Status", workTask.Status);
            cmd.Parameters.AddWithValue("@Priority", workTask.Priority);
            cmd.Parameters.AddWithValue("@DueDate", workTask.DueDate.HasValue
                ? workTask.DueDate.Value.ToString("yyyy-MM-dd")
                : DBNull.Value);
            cmd.Parameters.AddWithValue("@Stage", workTask.Stage);
            cmd.Parameters.AddWithValue("@Progress", workTask.Progress);
            cmd.Parameters.AddWithValue("@IsCore", workTask.IsCore ? 1 : 0);

            // ExecuteScalar — 단일 값 하나를 반환받을 때 사용
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        // ───────────────────────────────────────
        // UPDATE — Task 정보 수정
        // 작업 상세 패널에서 수정 후 저장 시 호출
        // ───────────────────────────────────────
        public void UpdateTask(WorkTask task)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                UPDATE Tasks
                SET Title = @Title,
                    Description = @Description,
                    Status = @Status,
                    Priority = @Priority,
                    DueDate = @DueDate,
                    Stage = @Stage,
                    Progress = @Progress,
                    IsCore = @IsCore,
                    CoreLockedAt = @CoreLockedAt
                WHERE Id = @Id
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", task.Id);
            cmd.Parameters.AddWithValue("@Title", task.Title);
            cmd.Parameters.AddWithValue("@Description", task.Description);
            cmd.Parameters.AddWithValue("@Status", task.Status);
            cmd.Parameters.AddWithValue("@Priority", task.Priority);
            cmd.Parameters.AddWithValue("@DueDate", task.DueDate.HasValue
                ? task.DueDate.Value.ToString("yyyy-MM-dd")
                : DBNull.Value);
            cmd.Parameters.AddWithValue("@Stage", task.Stage);
            cmd.Parameters.AddWithValue("@Progress", task.Progress);
            cmd.Parameters.AddWithValue("@IsCore", task.IsCore ? 1 : 0);
            cmd.Parameters.AddWithValue("@CoreLockedAt", task.CoreLockedAt ?? string.Empty);

            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // DELETE — Task 삭제
        // Task 삭제 시 연결된 Issues도 같이 삭제
        // FK 제약조건 때문에 자식 테이블(Issues) 먼저 지워야 함
        // ───────────────────────────────────────
        public void DeleteTask(int taskId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 1. Issues 삭제
            var deleteIssues = "DELETE FROM Issues WHERE TaskId = @TaskId";
            using var cmdIssues = new SqliteCommand(deleteIssues, conn);
            cmdIssues.Parameters.AddWithValue("@TaskId", taskId);
            cmdIssues.ExecuteNonQuery();

            // 2. Files 삭제 (누락되어 있던 부분)
            var deleteFiles = "DELETE FROM Files WHERE TaskId = @TaskId";
            using var cmdFiles = new SqliteCommand(deleteFiles, conn);
            cmdFiles.Parameters.AddWithValue("@TaskId", taskId);
            cmdFiles.ExecuteNonQuery();

            // 3. Task 삭제
            var deleteTask = "DELETE FROM Tasks WHERE Id = @Id";
            using var cmdTask = new SqliteCommand(deleteTask, conn);
            cmdTask.Parameters.AddWithValue("@Id", taskId);
            cmdTask.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // READ — 특정 Task의 이슈 목록 조회
        // ───────────────────────────────────────
        public List<Issue> GetIssuesByTask(int taskId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT * FROM Issues WHERE TaskId = @TaskId ORDER BY CreatedAt DESC";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            using var reader = cmd.ExecuteReader();

            var issues = new List<Issue>();
            while (reader.Read())
            {
                issues.Add(new Issue
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    TaskId = reader.GetInt32(reader.GetOrdinal("TaskId")),
                    Problem = reader.IsDBNull(reader.GetOrdinal("Problem")) ? "" : reader.GetString(reader.GetOrdinal("Problem")),
                    Cause = reader.IsDBNull(reader.GetOrdinal("Cause")) ? "" : reader.GetString(reader.GetOrdinal("Cause")),
                    Solution = reader.IsDBNull(reader.GetOrdinal("Solution")) ? "" : reader.GetString(reader.GetOrdinal("Solution")),
                    Result = reader.IsDBNull(reader.GetOrdinal("Result")) ? "" : reader.GetString(reader.GetOrdinal("Result")),
                    CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")))
                });
            }
            return issues;
        }

        // ───────────────────────────────────────
        // CREATE — 이슈 저장
        // ───────────────────────────────────────
        public int AddIssue(Issue issue)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO Issues (TaskId, Problem, Cause, Solution, Result, CreatedAt)
                VALUES (@TaskId, @Problem, @Cause, @Solution, @Result, @CreatedAt);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskId", issue.TaskId);
            cmd.Parameters.AddWithValue("@Problem", issue.Problem);
            cmd.Parameters.AddWithValue("@Cause", issue.Cause);
            cmd.Parameters.AddWithValue("@Solution", issue.Solution);
            cmd.Parameters.AddWithValue("@Result", issue.Result);
            cmd.Parameters.AddWithValue("@CreatedAt", issue.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        // ───────────────────────────────────────
        // DELETE — 이슈 삭제
        // ───────────────────────────────────────
        public void DeleteIssue(int issueId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "DELETE FROM Issues WHERE Id = @Id";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", issueId);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // UPDATE — Task의 Status(칸반 컬럼) 변경
        // 드래그앤드롭으로 카드를 다른 컬럼으로 이동할 때 호출
        // "할일" / "진행중" / "완료" 중 하나로 바꿔줌
        // ───────────────────────────────────────
        public void UpdateTaskStatus(int taskId, string newStatus)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "UPDATE Tasks SET Status = @Status WHERE Id = @Id";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@Id", taskId);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // READ — 전체 프로젝트 목록 조회
        // SqliteDataReader로 행을 하나씩 읽어서 List로 반환
        // ───────────────────────────────────────
        public List<Project> GetAllProjects()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT * FROM Projects ORDER BY Id DESC";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var projects = new List<Project>();

            while (reader.Read())
            {
                projects.Add(new Project
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? "" : reader.GetString(reader.GetOrdinal("Type")),
                    StartDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("StartDate"))),
                    Deadline = DateTime.Parse(reader.GetString(reader.GetOrdinal("Deadline"))),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                    GitHubUrl = reader.IsDBNull(reader.GetOrdinal("GitHubUrl")) ? "" : reader.GetString(reader.GetOrdinal("GitHubUrl")),
                    Background = reader.IsDBNull(reader.GetOrdinal("Background")) ? "" : reader.GetString(reader.GetOrdinal("Background")),
                    TechReason = reader.IsDBNull(reader.GetOrdinal("TechReason")) ? "" : reader.GetString(reader.GetOrdinal("TechReason")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "" : reader.GetString(reader.GetOrdinal("Role")),
                    Architecture = reader.IsDBNull(reader.GetOrdinal("Architecture")) ? "" : reader.GetString(reader.GetOrdinal("Architecture")),
                    TechStack = reader.IsDBNull(reader.GetOrdinal("TechStack")) ? "" : reader.GetString(reader.GetOrdinal("TechStack")),
                    RetrospectLearn = reader.IsDBNull(reader.GetOrdinal("RetrospectLearn")) ? "" : reader.GetString(reader.GetOrdinal("RetrospectLearn")),
                    RetrospectRegret = reader.IsDBNull(reader.GetOrdinal("RetrospectRegret")) ? "" : reader.GetString(reader.GetOrdinal("RetrospectRegret")),
                    RetrospectImprove = reader.IsDBNull(reader.GetOrdinal("RetrospectImprove")) ? "" : reader.GetString(reader.GetOrdinal("RetrospectImprove")),
                });
            }

            return projects;
        }

        // ───────────────────────────────────────
        // 전체 프로젝트의 모든 Task 중 — DueDate가 임박한 것만 조회
        // 마감 임박 알림용
        // ───────────────────────────────────────
        public List<WorkTask> GetTasksWithUpcomingDueDate(int withinDays)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var limit = DateTime.Today.AddDays(withinDays).ToString("yyyy-MM-dd");
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            var sql = @"
                SELECT * FROM Tasks
                WHERE DueDate IS NOT NULL
                  AND DueDate >= @Today
                  AND DueDate <= @Limit
                  AND Status != '완료'
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Today", today);
            cmd.Parameters.AddWithValue("@Limit", limit);
            using var reader = cmd.ExecuteReader();

            var tasks = new List<WorkTask>();
            while (reader.Read())
            {
                tasks.Add(new WorkTask
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "할일" : reader.GetString(reader.GetOrdinal("Status")),
                    Priority = reader.IsDBNull(reader.GetOrdinal("Priority")) ? "보통" : reader.GetString(reader.GetOrdinal("Priority")),
                    Stage = reader.IsDBNull(reader.GetOrdinal("Stage")) ? "기획" : reader.GetString(reader.GetOrdinal("Stage")),
                    Progress = reader.IsDBNull(reader.GetOrdinal("Progress")) ? 0 : reader.GetInt32(reader.GetOrdinal("Progress")),
                    IsCore = reader.IsDBNull(reader.GetOrdinal("IsCore")) ? false : reader.GetInt32(reader.GetOrdinal("IsCore")) == 1,
                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("DueDate"))),
                });
            }
            return tasks;
        }

        // ───────────────────────────────────────
        // UPDATE — 프로젝트 정보 수정
        // WHERE Id = @Id 로 특정 행만 수정
        // ───────────────────────────────────────
        public void UpdateProject(Project project)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                UPDATE Projects
                SET Name = @Name,
                    Type = @Type,
                    StartDate = @StartDate,
                    Deadline = @Deadline,
                    Description = @Description,
                    GitHubUrl = @GitHubUrl,
                    Background = @Background,
                    TechReason = @TechReason,
                    Role = @Role,
                    Architecture = @Architecture,
                    TechStack = @TechStack,
                    RetrospectLearn = @RetrospectLearn,
                    RetrospectRegret = @RetrospectRegret,
                    RetrospectImprove = @RetrospectImprove
                WHERE Id = @Id
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", project.Id);
            cmd.Parameters.AddWithValue("@Name", project.Name);
            cmd.Parameters.AddWithValue("@Type", project.Type);
            cmd.Parameters.AddWithValue("@StartDate", project.StartDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Deadline", project.Deadline.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@Description", project.Description);
            cmd.Parameters.AddWithValue("@GitHubUrl", project.GitHubUrl);
            cmd.Parameters.AddWithValue("@Background", project.Background);
            cmd.Parameters.AddWithValue("@TechReason", project.TechReason);
            cmd.Parameters.AddWithValue("@Role", project.Role);
            cmd.Parameters.AddWithValue("@Architecture", project.Architecture);
            cmd.Parameters.AddWithValue("@TechStack", project.TechStack);
            cmd.Parameters.AddWithValue("@RetrospectLearn", project.RetrospectLearn);
            cmd.Parameters.AddWithValue("@RetrospectRegret", project.RetrospectRegret);
            cmd.Parameters.AddWithValue("@RetrospectImprove", project.RetrospectImprove);

            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // DELETE — 프로젝트 삭제
        // 프로젝트 삭제 시 연결된 Tasks도 같이 삭제
        // (나중에 이슈, 스트릭 등도 여기에 추가)
        // ───────────────────────────────────────
        public void DeleteProject(int projectId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 1. 이 프로젝트의 모든 TaskId 목록 먼저 수집
            var taskIds = new List<int>();
            var selectTasks = "SELECT Id FROM Tasks WHERE ProjectId = @ProjectId";
            using (var cmdSelect = new SqliteCommand(selectTasks, conn))
            {
                cmdSelect.Parameters.AddWithValue("@ProjectId", projectId);
                using var reader = cmdSelect.ExecuteReader();
                while (reader.Read())
                    taskIds.Add(reader.GetInt32(0));
            }

            // 2. 각 Task에 연결된 Issues, Files 삭제
            foreach (var taskId in taskIds)
            {
                var deleteIssues = "DELETE FROM Issues WHERE TaskId = @TaskId";
                using var cmdIssues = new SqliteCommand(deleteIssues, conn);
                cmdIssues.Parameters.AddWithValue("@TaskId", taskId);
                cmdIssues.ExecuteNonQuery();

                var deleteFiles = "DELETE FROM Files WHERE TaskId = @TaskId";
                using var cmdFiles = new SqliteCommand(deleteFiles, conn);
                cmdFiles.Parameters.AddWithValue("@TaskId", taskId);
                cmdFiles.ExecuteNonQuery();
            }

            // 3. Tasks 삭제
            var deleteTasks = "DELETE FROM Tasks WHERE ProjectId = @ProjectId";
            using var cmdTasks = new SqliteCommand(deleteTasks, conn);
            cmdTasks.Parameters.AddWithValue("@ProjectId", projectId);
            cmdTasks.ExecuteNonQuery();

            // 4. ActivityLogs 삭제
            var deleteLogs = "DELETE FROM ActivityLogs WHERE ProjectId = @ProjectId";
            using var cmdLogs = new SqliteCommand(deleteLogs, conn);
            cmdLogs.Parameters.AddWithValue("@ProjectId", projectId);
            cmdLogs.ExecuteNonQuery();

            // 5. 마지막으로 Project 삭제
            var deleteProject = "DELETE FROM Projects WHERE Id = @Id";
            using var cmdProject = new SqliteCommand(deleteProject, conn);
            cmdProject.Parameters.AddWithValue("@Id", projectId);
            cmdProject.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // READ — 특정 프로젝트의 Task 목록 조회
        // ProjectId로 필터링해서 해당 프로젝트 Task만 가져옴
        // GetAllProjects()와 구조 동일 — reader로 한 줄씩 읽어서 List로 반환
        // ───────────────────────────────────────
        public List<WorkTask> GetTasksByProject(int projectId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // WHERE ProjectId = @ProjectId — 이 프로젝트 소속 Task만 가져옴
            var sql = "SELECT * FROM Tasks WHERE ProjectId = @ProjectId";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            using var reader = cmd.ExecuteReader();

            var tasks = new List<WorkTask>();

            while (reader.Read())
            {
                tasks.Add(new WorkTask
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "할일" : reader.GetString(reader.GetOrdinal("Status")),
                    Priority = reader.IsDBNull(reader.GetOrdinal("Priority")) ? "보통" : reader.GetString(reader.GetOrdinal("Priority")),
                    Stage = reader.IsDBNull(reader.GetOrdinal("Stage")) ? "기획" : reader.GetString(reader.GetOrdinal("Stage")),
                    Progress = reader.IsDBNull(reader.GetOrdinal("Progress")) ? 0 : reader.GetInt32(reader.GetOrdinal("Progress")),
                    IsCore = reader.IsDBNull(reader.GetOrdinal("IsCore")) ? false : reader.GetInt32(reader.GetOrdinal("IsCore")) == 1,
                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("DueDate"))),
                });
            }

            return tasks;
        }

        // ───────────────────────────────────────
        // READ — 특정 Task의 연결된 파일 목록 조회
        // ───────────────────────────────────────
        public List<TaskFile> GetFilesByTask(int taskId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT * FROM Files WHERE TaskId = @TaskId";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskId", taskId);
            using var reader = cmd.ExecuteReader();

            var files = new List<TaskFile>();
            while (reader.Read())
            {
                files.Add(new TaskFile
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    TaskId = reader.GetInt32(reader.GetOrdinal("TaskId")),
                    FileName = reader.GetString(reader.GetOrdinal("FileName")),
                    FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                });
            }
            return files;
        }

        // ───────────────────────────────────────
        // CREATE — 파일 연결 저장
        // ───────────────────────────────────────
        public int AddFile(TaskFile file)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO Files (TaskId, FileName, FilePath)
                VALUES (@TaskId, @FileName, @FilePath);
                SELECT last_insert_rowid();
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskId", file.TaskId);
            cmd.Parameters.AddWithValue("@FileName", file.FileName);
            cmd.Parameters.AddWithValue("@FilePath", file.FilePath);

            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        // ───────────────────────────────────────
        // DELETE — 파일 연결 삭제
        // ───────────────────────────────────────
        public void DeleteFile(int fileId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "DELETE FROM Files WHERE Id = @Id";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", fileId);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // 프로필이 없으면 기본값으로 한 번 생성
        // 앱 최초 실행 시 UserProfile 행이 아예 없는 상태를 방지
        // (없으면 알림 관련 토글들이 화면엔 ON으로 보여도 실제로는 동작 안 하는 문제가 있었음)
        // ───────────────────────────────────────
        public void EnsureUserProfileExists()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO UserProfile (Id, StreakReminderEnabled, ProjectDeadlineAlertEnabled, TaskDeadlineAlertEnabled, StreakReminderIntervalHours)
                VALUES (1, 1, 1, 1, 1)
                ON CONFLICT(Id) DO NOTHING
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // READ — 사용자 프로필 불러오기
        // 앱에 프로필은 딱 하나만 존재 — LIMIT 1으로 첫 번째 행만 가져옴
        // ───────────────────────────────────────
        public UserProfile? GetUserProfile()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT * FROM UserProfile LIMIT 1";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new UserProfile
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "" : reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                Blog = reader.IsDBNull(reader.GetOrdinal("Blog")) ? "" : reader.GetString(reader.GetOrdinal("Blog")),
                LinkedIn = reader.IsDBNull(reader.GetOrdinal("LinkedIn")) ? "" : reader.GetString(reader.GetOrdinal("LinkedIn")),
                Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? "" : reader.GetString(reader.GetOrdinal("Bio")),
                StreakReminderEnabled = reader.IsDBNull(reader.GetOrdinal("StreakReminderEnabled")) ? true : reader.GetInt32(reader.GetOrdinal("StreakReminderEnabled")) == 1,
                ProjectDeadlineAlertEnabled = reader.IsDBNull(reader.GetOrdinal("ProjectDeadlineAlertEnabled")) ? true : reader.GetInt32(reader.GetOrdinal("ProjectDeadlineAlertEnabled")) == 1,
                TaskDeadlineAlertEnabled = reader.IsDBNull(reader.GetOrdinal("TaskDeadlineAlertEnabled")) ? true : reader.GetInt32(reader.GetOrdinal("TaskDeadlineAlertEnabled")) == 1,
                StreakReminderIntervalHours = reader.IsDBNull(reader.GetOrdinal("StreakReminderIntervalHours")) ? 1 : reader.GetInt32(reader.GetOrdinal("StreakReminderIntervalHours")),
            };
        }

        // ───────────────────────────────────────
        // SAVE — 사용자 프로필 저장
        // 프로필이 없으면 INSERT, 있으면 UPDATE
        // "UPSERT" 패턴 — 있든 없든 한 번에 처리
        // ───────────────────────────────────────
        public void SaveUserProfile(UserProfile profile)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO UserProfile (Id, Name, Email, Blog, LinkedIn, Bio, StreakReminderEnabled, ProjectDeadlineAlertEnabled, TaskDeadlineAlertEnabled, StreakReminderIntervalHours)
                VALUES (1, @Name, @Email, @Blog, @LinkedIn, @Bio, @StreakReminderEnabled, @ProjectDeadlineAlertEnabled, @TaskDeadlineAlertEnabled, @StreakReminderIntervalHours)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = @Name, Email = @Email, Blog = @Blog, LinkedIn = @LinkedIn, Bio = @Bio,
                    StreakReminderEnabled = @StreakReminderEnabled,
                    ProjectDeadlineAlertEnabled = @ProjectDeadlineAlertEnabled,
                    TaskDeadlineAlertEnabled = @TaskDeadlineAlertEnabled,
                    StreakReminderIntervalHours = @StreakReminderIntervalHours
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", profile.Name);
            cmd.Parameters.AddWithValue("@Email", profile.Email);
            cmd.Parameters.AddWithValue("@Blog", profile.Blog);
            cmd.Parameters.AddWithValue("@LinkedIn", profile.LinkedIn);
            cmd.Parameters.AddWithValue("@Bio", profile.Bio);
            cmd.Parameters.AddWithValue("@StreakReminderEnabled", profile.StreakReminderEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@ProjectDeadlineAlertEnabled", profile.ProjectDeadlineAlertEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@TaskDeadlineAlertEnabled", profile.TaskDeadlineAlertEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@StreakReminderIntervalHours", profile.StreakReminderIntervalHours);

            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // 알림 토글 개별 즉시 저장 — 토글 변경 시점에 바로 호출됨
        // 프로필을 한 번도 저장 안 한 상태(행이 없는 상태)일 수도 있어서
        // UPSERT 패턴 사용 — 없으면 새로 만들고, 있으면 그 컬럼만 갈아끼움
        // ───────────────────────────────────────
        public void UpdateStreakReminderEnabled(bool enabled)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO UserProfile (Id, StreakReminderEnabled)
                VALUES (1, @Value)
                ON CONFLICT(Id) DO UPDATE SET StreakReminderEnabled = @Value
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Value", enabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void UpdateProjectDeadlineAlertEnabled(bool enabled)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO UserProfile (Id, ProjectDeadlineAlertEnabled)
                VALUES (1, @Value)
                ON CONFLICT(Id) DO UPDATE SET ProjectDeadlineAlertEnabled = @Value
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Value", enabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void UpdateTaskDeadlineAlertEnabled(bool enabled)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO UserProfile (Id, TaskDeadlineAlertEnabled)
                VALUES (1, @Value)
                ON CONFLICT(Id) DO UPDATE SET TaskDeadlineAlertEnabled = @Value
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Value", enabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        /// 전체 프로젝트 통합 — ProjectId 없이 날짜만 기록
        public void RecordStreak()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT OR IGNORE INTO Streaks (Date, WorkDone)
                VALUES (@Date, 1)
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Date", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // 연속 작업일 계산(전체 프로젝트 통합)
        // 오늘부터 하루씩 거슬러 올라가며 기록이 있는 날을 카운트
        // 하루라도 빠지면 거기서 멈춤
        // ───────────────────────────────────────
        public int GetCurrentStreak()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT Date FROM Streaks ORDER BY Date DESC";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var dates = new List<DateTime>();
            while (reader.Read())
                dates.Add(DateTime.Parse(reader.GetString(0)));

            if (dates.Count == 0) return 0;

            var today = DateTime.Today;
            if (dates[0] < today.AddDays(-1)) return 0;

            int streak = 0;
            var expected = dates[0].Date;

            foreach (var date in dates)
            {
                if (date.Date == expected)
                {
                    streak++;
                    expected = expected.AddDays(-1);
                }
                else break;
            }

            return streak;
        }

        // 역대 최고 연속 스트릭 계산
        public int GetBestStreak()
        {
            // Streaks 테이블에서 WorkDone=1인 날짜를 오름차순으로 가져와
            // 연속된 날짜 시퀀스 중 가장 긴 것을 반환
            var dates = new List<DateTime>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Date FROM Streaks WHERE WorkDone=1 ORDER BY Date ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                dates.Add(DateTime.Parse(reader.GetString(0)));

            if (dates.Count == 0) return 0;

            int best = 1, current = 1;
            for (int i = 1; i < dates.Count; i++)
            {
                if ((dates[i] - dates[i - 1]).Days == 1)
                {
                    current++;
                    if (current > best) best = current;
                }
                else
                {
                    current = 1;
                }
            }
            return best;
        }

        // ───────────────────────────────────────
        // 이번 주 완료 작업 수 — 요일별로 카운트
        // 월요일부터 오늘까지 완료된 Task를 날짜별로 집계
        // ───────────────────────────────────────
        public Dictionary<string, int> GetWeeklyCompletedTasks(int projectId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var monday = today.AddDays(dayOfWeek == 0 ? -6 : -(dayOfWeek - 1));

            var sql = @"
                SELECT CreatedAt, COUNT(*) as Count
                FROM ActivityLogs
                WHERE ProjectId = @ProjectId
                  AND CreatedAt >= @Monday
                GROUP BY DATE(CreatedAt)
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@Monday", monday.ToString("yyyy-MM-dd"));
            using var reader = cmd.ExecuteReader();

            var result = new Dictionary<string, int>
            {
                { "월", 0 }, { "화", 0 }, { "수", 0 }, { "목", 0 },
                { "금", 0 }, { "토", 0 }, { "일", 0 }
            };

            var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };

            while (reader.Read())
            {
                var date = DateTime.Parse(reader.GetString(0));
                var dayName = dayNames[(int)date.DayOfWeek];
                result[dayName] += reader.GetInt32(1);
            }

            return result;
        }

        // ───────────────────────────────────────
        // 단계별 진행률 — 각 Stage의 완료/전체 비율
        // ───────────────────────────────────────
        public Dictionary<string, double> GetStageProgress(int projectId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = "SELECT Stage, Status FROM Tasks WHERE ProjectId = @ProjectId";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            using var reader = cmd.ExecuteReader();

            // 단계별 전체/완료 카운트
            var total = new Dictionary<string, int>();
            var done = new Dictionary<string, int>();
            var stages = new[] { "기획", "설계", "개발", "테스트", "배포" };

            foreach (var s in stages) { total[s] = 0; done[s] = 0; }

            while (reader.Read())
            {
                var stage = reader.GetString(0);
                var status = reader.GetString(1);
                if (!total.ContainsKey(stage)) continue;
                total[stage]++;
                if (status == "완료") done[stage]++;
            }

            // 진행률 계산 (0~100)
            var result = new Dictionary<string, double>();
            foreach (var s in stages)
                result[s] = total[s] > 0 ? Math.Round((double)done[s] / total[s] * 100) : 0;

            return result;
        }

        // ───────────────────────────────────────
        // 히트맵용 — 최근 N일간 Streaks 날짜 목록
        // ───────────────────────────────────────
        public List<DateTime> GetStreakDates(int days = 365)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var since = DateTime.Today.AddDays(-days).ToString("yyyy-MM-dd");
            var sql = "SELECT Date FROM Streaks WHERE Date >= @Since ORDER BY Date";
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Since", since);
            using var reader = cmd.ExecuteReader();

            var result = new List<DateTime>();
            while (reader.Read())
                result.Add(DateTime.Parse(reader.GetString(0)));

            return result;
        }

        // ───────────────────────────────────────
        // 활동 로그 기록 — Task 완료 시 호출
        // ───────────────────────────────────────
        public void AddActivityLog(int projectId, string description)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var sql = @"
                INSERT INTO ActivityLogs (ProjectId, Description, CreatedAt)
                VALUES (@ProjectId, @Description, @CreatedAt)
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        // ───────────────────────────────────────
        // 활동 로그 삭제 — Task 완료 취소 시 호출
        // 오늘 날짜의 해당 프로젝트 로그 1건 삭제
        // ───────────────────────────────────────
        public void RemoveActivityLog(int projectId, string description)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 오늘 날짜에 기록된 동일한 description 1건만 삭제
            var sql = @"
                DELETE FROM ActivityLogs
                WHERE Id = (
                    SELECT Id FROM ActivityLogs
                    WHERE ProjectId = @ProjectId
                      AND Description = @Description
                      AND DATE(CreatedAt) = @Today
                    LIMIT 1
                )
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@Today", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();
        }
    }
}