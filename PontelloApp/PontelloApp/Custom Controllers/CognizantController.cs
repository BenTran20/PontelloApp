using Microsoft.AspNetCore.Mvc;

namespace PontelloApp.Custom_Controllers
{
    public class CognizantController : Controller
    {
        internal string ControllerName()
        {
            return ControllerContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        }
        internal string ActionName()
        {
            return ControllerContext.RouteData.Values["action"]?.ToString() ?? string.Empty;
        }
    }
}
