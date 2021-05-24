using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSogo.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        [Route("google-login")]
        public IActionResult GoogleLogin()
        {
            var props = new AuthenticationProperties() { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }
        [Route("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync("Cookies");
            var f = HttpContext.User.Identity;
            var fd = HttpContext.User.Claims;
            foreach (var item in fd)
            {
                var dgf = item.Value;
            }


            var claims = result.Principal.Identities.FirstOrDefault().Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value,


            });

            //var service = new CalendarService(new BaseClientService.Initializer()
            //{
            //    //HttpClientInitializer = credential,
            //    ApiKey = "AIzaSyDdr1slmbKcsnR3_Ju6rGP6yQkHSqOPjBQ",
            //    ApplicationName = "test2"
            //});

            //var gs = service.Calendars.Get("primary");
            //var fgs = gs.Execute();
            return Json(claims);
        }
    }
}
