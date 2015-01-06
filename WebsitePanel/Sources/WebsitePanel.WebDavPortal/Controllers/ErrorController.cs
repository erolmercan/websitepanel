﻿using System;
using System.Web.Mvc;
using WebsitePanel.WebDavPortal.Config;
using WebsitePanel.WebDavPortal.Models;

namespace WebsitePanel.WebDavPortal.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult Index(int statusCode, Exception exception, bool isAjaxRequet)
        {
            var model = new ErrorModel
            {
                HttpStatusCode = statusCode,
                Message = WebDavAppConfigManager.Instance.HttpErrors[statusCode],
                Exception = exception
            };
            
            Response.StatusCode = statusCode;

            if (!isAjaxRequet)
                return View(model);

            var errorObject = new { statusCode = model.HttpStatusCode, message = model.Message };
            return Json(errorObject, JsonRequestBehavior.AllowGet);
        }
    }
}