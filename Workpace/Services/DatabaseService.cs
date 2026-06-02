using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
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
                    Background TEXT
                );
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Title TEXT NOT NULL,
                    Status TEXT DEFAULT '할일',
                    Priority TEXT DEFAULT '보통',
                    DueDate TEXT,
                    Stage TEXT DEFAULT '기획',
                    Progress INTEGER DEFAULT 0,
                    FOREIGN KEY(ProjectId) REFERENCES Projects(Id)
                );
                CREATE TABLE IF NOT EXISTS Issues (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER,
                    Problem TEXT,
                    Cause TEXT,
                    Solution TEXT,
                    CreatedAt TEXT,
                    FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
                );
                CREATE TABLE IF NOT EXISTS Streaks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Date TEXT,
                    WorkDone INTEGER DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS ActivityLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Description TEXT,
                    CreatedAt TEXT
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
                INSERT INTO Projects (Name, Type, StartDate, Deadline, Description, GitHubUrl, Background)
                VALUES (@Name, @Type, @StartDate, @Deadline, @Description, @GitHubUrl, @Background);
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

            // ExecuteScalar — 단일 값 하나를 반환받을 때 사용
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
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
                });
            }

            return projects;
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
                    GitHubUrl = @GitHubUrl
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

            // Tasks 먼저 삭제 — FK 제약조건 때문에 자식 테이블 먼저 지워야 함
            var deleteTasks = "DELETE FROM Tasks WHERE ProjectId = @ProjectId";
            using var cmdTasks = new SqliteCommand(deleteTasks, conn);
            cmdTasks.Parameters.AddWithValue("@ProjectId", projectId);
            cmdTasks.ExecuteNonQuery();

            // 그 다음 Projects 삭제
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
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "할일" : reader.GetString(reader.GetOrdinal("Status")),
                    Priority = reader.IsDBNull(reader.GetOrdinal("Priority")) ? "보통" : reader.GetString(reader.GetOrdinal("Priority")),
                    Stage = reader.IsDBNull(reader.GetOrdinal("Stage")) ? "기획" : reader.GetString(reader.GetOrdinal("Stage")),
                    Progress = reader.IsDBNull(reader.GetOrdinal("Progress")) ? 0 : reader.GetInt32(reader.GetOrdinal("Progress")),
                });
            }

            return tasks;
        }
    }
}