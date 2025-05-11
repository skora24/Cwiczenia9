using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;
using OneOf;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<OneOf<int, string>> AddProductToWarehouseAsync(WarehouseRequest request)
    {
        await using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default"));
        await conn.OpenAsync();

        await using SqlTransaction tx = (SqlTransaction)await conn.BeginTransactionAsync();

        try
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;

            // Validate Product
            cmd.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            if (await cmd.ExecuteScalarAsync() is null)
                return "Product not found";

            cmd.Parameters.Clear();
            // Validate Warehouse
            cmd.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            if (await cmd.ExecuteScalarAsync() is null)
                return "Warehouse not found";

            cmd.Parameters.Clear();
            // Find matching Order
            cmd.CommandText = @"
                SELECT TOP 1 * FROM [Order]
                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt AND FulfilledAt IS NULL
                ORDER BY CreatedAt";
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            cmd.Parameters.AddWithValue("@Amount", request.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

            SqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!reader.Read())
            {
                await reader.DisposeAsync();
                return "Order not found";
            }

            int idOrder = (int)reader["IdOrder"];
            reader.Close();

            // Check if already fulfilled
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            if (await cmd.ExecuteScalarAsync() is not null)
                return "Order already fulfilled";

            // Get price
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            decimal price = (decimal)(await cmd.ExecuteScalarAsync() ?? 0);
            decimal totalPrice = price * request.Amount;

            // Fulfill order
            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            await cmd.ExecuteNonQueryAsync();

            // Insert into Product_Warehouse
            cmd.Parameters.Clear();
            cmd.CommandText = @"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            cmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@Amount", request.Amount);
            cmd.Parameters.AddWithValue("@Price", totalPrice);

            int idProductWarehouse = (int)(await cmd.ExecuteScalarAsync() ?? -1);
            await tx.CommitAsync();

            return idProductWarehouse;
        }
        catch (Exception e)
        {
            await tx.RollbackAsync();
            return $"Error: {e.Message}";
        }
    }

    public async Task DoSomethingAsync()
    {
        // Example method
    }

    public async Task ProcedureAsync()
    {
        // Example procedure
    }
}
