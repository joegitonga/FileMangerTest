using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReadingFiles.Models;
using System.Web.Security;
using Dapper;


namespace ReadingFiles.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            ViewData["title"] = "Login Authentication";
            string returnUrl = string.Empty;
            Session.Abandon();
            Session.Clear();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (DAL.ValidateUser(model.Username, model.Password))
                    {
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        return View(model);
                    }
                }
                catch (Exception e)
                {
                    return View(model);
                }
            }
            else
            {
                //var message = string.Join(" | ", ModelState.Values
                //    .SelectMany(v => v.Errors)
                //    .Select(e => e.ErrorMessage));

                //ModelState.AddModelError("", "The user name or password provided is incorrect.");
                return View(model);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}