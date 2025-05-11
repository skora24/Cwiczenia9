using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] WarehouseRequest request)
    {
        var result = await _dbService.AddProductToWarehouseAsync(request);
        if (result.IsT0)
        {
            return Ok(result.AsT0);
        }
        else
        {
            var error = result.AsT1;
            return error switch
            {
                "Product not found" => NotFound(error),
                "Warehouse not found" => NotFound(error),
                "Order not found" => NotFound(error),
                "Order already fulfilled" => Conflict(error),
                _ => StatusCode(500, error)
            };
        }

    }
}