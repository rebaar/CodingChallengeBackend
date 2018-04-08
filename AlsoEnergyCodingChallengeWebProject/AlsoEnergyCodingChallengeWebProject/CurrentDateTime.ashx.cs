using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace AlsoEnergyCodingChallengeWebProject
{
    /// <summary>
    /// Summary description for CurrentDateTime
    /// </summary>
    public class CurrentDateTime : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string queryError = context.Request.QueryString["error"];
            if (!string.IsNullOrEmpty(queryError))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            else
            {
                context.Response.Write(DateTime.Now);
            }
            
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}