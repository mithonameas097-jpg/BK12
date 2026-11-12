
using Microsoft.Data.SqlClient;
using System.Data;

namespace _bk12
{
    public class Connection
    {
        private readonly string _connectionString;

        public Connection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<object> ExecuteDB(string procedureName, SqlParameter[]? parameters = null, bool returnTable = false)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                using SqlCommand command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                await connection.OpenAsync();

                if (returnTable)
                {
                    DataTable table = new DataTable();

                    using SqlDataReader reader = await command.ExecuteReaderAsync();

                    table.Load(reader);

                    return table;
                }
                else
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error in procedure '{procedureName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing procedure '{procedureName}': {ex.Message}", ex);
            }
        }

        public SqlParameter[] CreateParams(params (string Name, object? Value)[] parameters)
        {
            try
            {
                return parameters.Select(p => new SqlParameter(p.Name, p.Value ?? DBNull.Value)).ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating SQL parameters: {ex.Message}", ex);
            }
        }
    }
}
