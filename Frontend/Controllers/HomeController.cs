using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Frontend.Models;

namespace Frontend.Controllers;


public class HomeController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _gatewayUrl; 

    public HomeController(IHttpClientFactory clientFactory, IConfiguration conf) 
    {
        _clientFactory = clientFactory;
        _gatewayUrl = conf["GatewayUrl"];
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Dashboard(Guid userId)
    {
        var client = _clientFactory.CreateClient();
        decimal balance = 0;
        try {
            var res = await client.GetFromJsonAsync<JsonElement>($"{_gatewayUrl}/api/accounts/{userId}");
            balance = res.GetProperty("balance").GetDecimal();
        } catch {}

        List<OrderViewModel> orders = new();
        try {
            orders = await client.GetFromJsonAsync<List<OrderViewModel>>($"{_gatewayUrl}/api/orders/{userId}") ?? new();
        } catch {}

        ViewBag.UserId = userId;
        ViewBag.Balance = balance;
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(Guid userId)
    {
        var client = _clientFactory.CreateClient();
        await client.PostAsJsonAsync($"{_gatewayUrl}/api/accounts", new { UserId = userId });
        return RedirectToAction("Dashboard", new { userId });
    }

    [HttpPost]
    public async Task<IActionResult> TopUp(Guid userId, decimal amount)
    {
        var client = _clientFactory.CreateClient();
        await client.PostAsJsonAsync($"{_gatewayUrl}/api/accounts/topup", new { UserId = userId, Amount = amount });
        return RedirectToAction("Dashboard", new { userId });
    }

    [HttpPost]
    public async Task<IActionResult> Buy(Guid userId, decimal amount, string desc)
    {
        var client = _clientFactory.CreateClient();
        await client.PostAsJsonAsync($"{_gatewayUrl}/api/orders", new { UserId = userId, Amount = amount, Description = desc });
        await Task.Delay(800); 
        return RedirectToAction("Dashboard", new { userId });
    }
}