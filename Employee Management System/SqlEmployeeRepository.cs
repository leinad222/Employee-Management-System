using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Employee_Management_System
{
    // Uses SQL Server LocalDB ("(LocalDB)\\MSSQLLocalDB").
    // This repository will attempt to create a local database file under the application's base directory
    // named EmployeeDB.mdf if it does not already exist, then create the Employees table.
    public class SqlEmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;
        private readonly string _dbName = "EmployeeDB";

        public SqlEmployeeRepository()
        {
            // Initial connection string to the database by name. If the DB does not exist, we'll create it.
            _connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog={_dbName};Integrated Security=True;";

            EnsureDatabaseAndTable();
        }

        private void EnsureDatabaseAndTable()
        {
            // Try opening a connection to the target DB. If it succeeds, ensure table exists.
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    EnsureTable(conn);
                    return;
                }
            }
            catch
            {
                // Database probably does not exist. Create it by connecting to master and issuing CREATE DATABASE
            }

            // Create MDF/LDF files under app base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var mdfPath = Path.Combine(baseDir, _dbName + ".mdf");
            var ldfPath = Path.Combine(baseDir, _dbName + "_log.ldf");

            var masterConnString = "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

            var createDbSql = $@"CREATE DATABASE [{_dbName}] ON (NAME = N'{_dbName}', FILENAME = '{mdfPath}') LOG ON (NAME = N'{_dbName}_log', FILENAME = '{ldfPath}');";

            using (var conn = new SqlConnection(masterConnString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = createDbSql;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }

            // Now ensure the table exists
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                EnsureTable(conn);
            }
        }

        private void EnsureTable(SqlConnection conn)
        {
            var checkSql = "IF OBJECT_ID('dbo.Employees', 'U') IS NULL BEGIN CREATE TABLE dbo.Employees (" +
                           "Id NVARCHAR(50) NOT NULL PRIMARY KEY, " +
                           "Name NVARCHAR(200) NULL, " +
                           "Designation NVARCHAR(100) NULL, " +
                           "BasicPay DECIMAL(18,2) NULL, " +
                           "Conveyance DECIMAL(18,2) NULL, " +
                           "Medical DECIMAL(18,2) NULL, " +
                           "HouseRent DECIMAL(18,2) NULL, " +
                           "GrossPay DECIMAL(18,2) NULL, " +
                           "IncomeTax DECIMAL(18,2) NULL, " +
                           "NetSalary DECIMAL(18,2) NULL); END";

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = checkSql;
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        public List<Employee> LoadAll()
        {
            var list = new List<Employee>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name, Designation, BasicPay, Conveyance, Medical, HouseRent, GrossPay, IncomeTax, NetSalary FROM dbo.Employees";
                    cmd.CommandType = CommandType.Text;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var e = new Employee
                            {
                                Id = reader.GetString(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Designation = reader.IsDBNull(2) ? null : reader.GetString(2),
                                BasicPay = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3),
                                Conveyance = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                Medical = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                                HouseRent = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                                GrossPay = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7),
                                IncomeTax = reader.IsDBNull(8) ? 0m : reader.GetDecimal(8),
                                NetSalary = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9)
                            };

                            list.Add(e);
                        }
                    }
                }
            }

            return list;
        }

        public void Save(Employee employee)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // check exists
                using (var check = conn.CreateCommand())
                {
                    check.CommandText = "SELECT COUNT(1) FROM dbo.Employees WHERE Id = @Id";
                    check.Parameters.AddWithValue("@Id", employee.Id);
                    var exists = Convert.ToInt32(check.ExecuteScalar()) > 0;

                    if (exists)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"UPDATE dbo.Employees SET Name=@Name, Designation=@Designation, BasicPay=@BasicPay,
                                                Conveyance=@Conveyance, Medical=@Medical, HouseRent=@HouseRent,
                                                GrossPay=@GrossPay, IncomeTax=@IncomeTax, NetSalary=@NetSalary WHERE Id=@Id";
                            AddParameters(cmd, employee);
                            cmd.ExecuteNonQuery();
                        }
                        return;
                    }
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO dbo.Employees (Id, Name, Designation, BasicPay, Conveyance, Medical, HouseRent, GrossPay, IncomeTax, NetSalary)
                                        VALUES (@Id, @Name, @Designation, @BasicPay, @Conveyance, @Medical, @HouseRent, @GrossPay, @IncomeTax, @NetSalary)";
                    AddParameters(cmd, employee);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM dbo.Employees WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParameters(SqlCommand cmd, Employee e)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", e.Id ?? string.Empty);
            cmd.Parameters.AddWithValue("@Name", (object)e.Name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Designation", (object)e.Designation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BasicPay", e.BasicPay);
            cmd.Parameters.AddWithValue("@Conveyance", e.Conveyance);
            cmd.Parameters.AddWithValue("@Medical", e.Medical);
            cmd.Parameters.AddWithValue("@HouseRent", e.HouseRent);
            cmd.Parameters.AddWithValue("@GrossPay", e.GrossPay);
            cmd.Parameters.AddWithValue("@IncomeTax", e.IncomeTax);
            cmd.Parameters.AddWithValue("@NetSalary", e.NetSalary);
        }
    }
}
