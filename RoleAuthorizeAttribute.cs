using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

// Put [RoleAuthorize("Organiser")] on a controller to restrict it to that role
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string _role;
    public RoleAuthorizeAttribute(string role) { _role = role; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
            context.Result = new RedirectToActionResult("Login", "Account", null);   // not logged in
        else if (role != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);   // wrong role
    }
}