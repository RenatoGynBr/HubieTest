using System;
using System.IO;
using System.Web;
using System.Configuration;
using HubieTest.Web.ashx;
using HubieTest.Business;
using HubieTest.Dal;
using Newtonsoft.Json;

namespace HubieTest.Web.process
{
    /// <summary>
    /// Ticket attachment upload. Unlike the other handlers, it receives
    /// multipart/form-data (binary file), so it reads from Request.Files.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Expected flow (method = "upload"):
    ///   1. validate that a file and a ticketId were sent;
    ///   2. save the file to disk (e.g. ~/uploads/{ticketId}/{guid}_{name});
    ///   3. register the metadata via ticketBusiness.registerAttachment(...);
    ///   4. return the created ATTACHMENT as JSON.
    /// Download/listing can be done by ticket.ashx (listAttachments) + a
    /// static/endpoint route that serves the saved file.
    /// ==================================================================
    /// </summary>
    public class attachment : AshxBase
    {
        public override void ProcessRequest(HttpContext context)
        {
            base.ProcessRequestSafe(context); // validates the JWT
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentType = "application/json";

            if (HttpStatusReturn == 200)
            {
                strContextResponse = processRequest(context);
            }

            context.Response.StatusCode = HttpStatusReturn;
            context.Response.Write(strContextResponse);
        }

        private string processRequest(HttpContext context)
        {
            if (strMETHOD != "upload")
            {
                HttpStatusReturn = 400;
                return JsonConvert.SerializeObject(new { error = "Unsupported method: " + strMETHOD });
            }
            // read file and ticketId
            HttpPostedFile file = context.Request.Files.Count > 0 ? context.Request.Files[0] : null;
            string ticketIdRaw = context.Request.Form["ticketId"] ?? context.Request.QueryString["ticketId"];

            // validate file
            if (file == null)
            {
                HttpStatusReturn = 400;
                return JsonConvert.SerializeObject(new { error = "No file uploaded." });
            }

            // validate ticketId
            if (string.IsNullOrEmpty(ticketIdRaw) || !long.TryParse(ticketIdRaw, out long ticketId))
            {
                HttpStatusReturn = 400;
                return JsonConvert.SerializeObject(new { error = "ticketId is required and must be a number." });
            }

            // basic size/extension checks
            const int maxBytes = 10 * 1024 * 1024; // 10 MB (matches Web.config comment)
            if (file.ContentLength <= 0 || file.ContentLength > maxBytes)
            {
                HttpStatusReturn = 413; // Payload Too Large
                return JsonConvert.SerializeObject(new { error = "File is empty or exceeds the maximum allowed size (10 MB)." });
            }

            // only allow certain extensions (for security reasons)
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".doc", ".docx", ".xls", ".xlsx" };
            string originalName = Path.GetFileName(file.FileName ?? string.Empty);
            string ext = Path.GetExtension(originalName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || Array.IndexOf(allowedExt, ext) < 0)
            {
                HttpStatusReturn = 415; // Unsupported Media Type
                return JsonConvert.SerializeObject(new { error = "File type is not allowed." });
            }

            // authorize / verify ticket exists
            var tb = new ticketBusiness();
            tb.loggedUserId = UserId;
            tb.loggedUserName = UserName;
            tb.loggedUserProfile = UserProfile;

            try
            {
                // check if ticket exists
                var existing = tb.get(ticketId);
                if (existing == null)
                {
                    HttpStatusReturn = 404;
                    return JsonConvert.SerializeObject(new { error = "Ticket not found." });
                }

                // Optional: only requester or agents can upload
                bool isRequester = existing.REQUESTER_ID == UserId;
                bool isAgent = string.Equals(UserProfile, "AGENT", StringComparison.OrdinalIgnoreCase);
                if (!isRequester && !isAgent)
                {
                    HttpStatusReturn = 403;
                    return JsonConvert.SerializeObject(new { error = "Not authorized to attach files to this ticket." });
                }
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Error validating ticket: " + ex.Message });
            }

            // save to disk
            string uploadRoot = ConfigurationManager.AppSettings["UPLOADS_PATH"] ?? "~/uploads";
            string uploadsFolder = context.Server.MapPath(uploadRoot);
            string ticketFolder = Path.Combine(uploadsFolder, ticketId.ToString());
            try
            {
                Directory.CreateDirectory(ticketFolder);
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Could not create upload directory: " + ex.Message });
            }

            // generate a unique filename to avoid collisions
            string storedName = $"{Guid.NewGuid():N}_{originalName}";
            string fullPath = Path.Combine(ticketFolder, storedName);

            try
            {
                file.SaveAs(fullPath);
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Error saving file: " + ex.Message });
            }

            // register metadata
            var attachment = new ATTACHMENT
            {
                TICKET_ID = ticketId,
                INTERACTION_ID = null,
                ATTACHMENT_NAME = originalName,
                ATTACHMENT_TYPE = file.ContentType,
                ATTACHMENT_SIZE = file.ContentLength,
                ATTACHMENT_PATH = Path.Combine(ticketId.ToString(), storedName).Replace("\\", "/"),
                USER_ID = UserId,
                ATTACHMENT_CREATED_DT = DateTime.Now
            };

            try
            {
                // register the attachment in the database
                var created = tb.registerAttachment(attachment);
                // return the created attachment as JSON
                HttpStatusReturn = 201;
                return JsonConvert.SerializeObject(created);
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Error registering attachment: " + ex.Message });
            }
        }
    }
}
