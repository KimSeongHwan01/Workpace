using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Workpace.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // DB 파일은 앱 실행 폴더에 생성
            var dbPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "workpace.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS Projects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type TEXT,
                    StartDate TEXT,
                    Deadline TEXT,
                    Description TEXT,
                    GitHubUrl TEXT
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
            ");
        }

        // 연결 반환 (각 메서드에서 using으로 사용)
        public SqliteConnection GetConnection()
            => new SqliteConnection(_connectionString);
    }
}