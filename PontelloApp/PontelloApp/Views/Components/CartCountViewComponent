using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontelloApp.Data;
using PontelloApp.Models;

public class CartCountViewComponent : ViewComponent
{
    private readonly PontelloAppContext _context;

    public CartCountViewComponent(PontelloAppContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int dealerId = 1; // TODO: replace with logged-in dealer

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.DealerId == dealerId && o.Status == OrderStatus.Draft);

        int count = order?.Items?.Sum(i => i.Quantity) ?? 0;

        return View(count);
    }
}
