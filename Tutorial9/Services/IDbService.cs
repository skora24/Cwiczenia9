namespace Tutorial9.Services;

using Tutorial9.Model;
using OneOf;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task<OneOf<int, string>> AddProductToWarehouseAsync(WarehouseRequest request);
}