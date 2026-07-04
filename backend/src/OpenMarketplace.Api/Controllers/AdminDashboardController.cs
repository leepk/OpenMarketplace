using Microsoft.AspNetCore.Mvc;
namespace OpenMarketplace.Api.Controllers;
[ApiController]
[Route("api/v1/admin/dashboard-legacy")]
public sealed class AdminDashboardController:ControllerBase{[HttpGet]public IActionResult Get()=>Redirect("/api/v1/admin/dashboard");}
