using System.Web;
using HubieTest.Business;
using HubieTest.Dal;
using HubieTest.Web.ashx;
using Newtonsoft.Json;

namespace HubieTest.Web.process
{
    /// <summary>
    /// TICKET handler. Mirrors process/ticket.ashx in Hubie: a single .ashx that
    /// dispatches several operations through the "method" field.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement each "case" of the switch following the categories.ashx model:
    ///   1. deserialize "data" (Newtonsoft) into the proper object/entity;
    ///   2. call the matching method on ticketBusiness;
    ///   3. serialize the result to JSON (JsonConvert.SerializeObject).
    ///
    /// IMPORTANT (security): the logged-in user id/profile/name ALREADY come from
    /// the token (UserId/UserProfile/UserName, filled by AshxBase). Use them —
    /// never trust a user id coming from the request body.
    ///
    /// "method" contract expected by the frontend (keep these names):
    ///   open | listMine | listQueue | get | assign | changeStatus |
    ///   addInteraction | listInteractions | listAttachments
    /// ==================================================================
    /// </summary>
    public class ticket : AshxBase
    {
        public override void ProcessRequest(HttpContext context)
        {
            base.ProcessRequestSafe(context); // validates the JWT
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentType = "application/json";

            if (HttpStatusReturn == 200)
            {
                strContextResponse = processRequest(strMETHOD, strData);
            }

            context.Response.StatusCode = HttpStatusReturn;
            context.Response.Write(strContextResponse);
        }

        private string processRequest(string method, string data)
        {
            // inject the logged-in user (from the token) into the business layer
            var business = new ticketBusiness
            {
                loggedUserId = UserId,
                loggedUserName = UserName,
                loggedUserProfile = UserProfile
            };

            switch (method)
            {
                // deserialize TICKET from "data", call business.open(...) and serialize
                case "open":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var ticketToOpen = JsonConvert.DeserializeObject<TICKET>(data);
                    var created = business.open(ticketToOpen);
                    if (business.hasError)
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = business.ErrorMessage });
                    }

                    return JsonConvert.SerializeObject(created);

                // return JsonConvert.SerializeObject(business.listMyTickets());
                case "listMine":
                    return JsonConvert.SerializeObject(business.listMyTickets());

                // read status (optional) from "data" and call business.listQueue(status)
                case "listQueue":
                    string status = null;
                    if (!string.IsNullOrEmpty(data))
                    {
                        var obj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                        status = (string)obj["status"];
                    }

                    return JsonConvert.SerializeObject(business.listQueue(status));

                // read ticketId from "data" and call business.get(ticketId)
                case "get":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var oget = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)oget["ticketId"], out long getId))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId is required and must be a number" });
                    }

                    var ticket = business.get(getId);
                    if (business.hasError)
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = business.ErrorMessage });
                    }

                    return JsonConvert.SerializeObject(ticket);

                // read ticketId from "data" and call business.assign(ticketId)
                case "assign":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var oassign = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)oassign["ticketId"], out long assignId))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId is required and must be a number" });
                    }

                    business.assign(assignId);
                    if (business.hasError)
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = business.ErrorMessage });
                    }

                    var assigned = business.get(assignId);
                    return JsonConvert.SerializeObject(assigned);

                // read ticketId + status from "data" and call business.changeStatus(...)
                case "changeStatus":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var ochg = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)ochg["ticketId"], out long chgId) || string.IsNullOrEmpty((string)ochg["status"]))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId and status are required" });
                    }

                    string newStatus = (string)ochg["status"];
                    business.changeStatus(chgId, newStatus);
                    if (business.hasError)
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = business.ErrorMessage });
                    }

                    var changed = business.get(chgId);
                    return JsonConvert.SerializeObject(changed);

                // read ticketId + message from "data" and call business.addInteraction(...)
                case "addInteraction":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var oi = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)oi["ticketId"], out long itTicketId) || string.IsNullOrEmpty((string)oi["message"]))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId and message are required" });
                    }

                    string message = (string)oi["message"];
                    var interaction = business.addInteraction(itTicketId, message);
                    if (business.hasError)
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = business.ErrorMessage });
                    }

                    return JsonConvert.SerializeObject(interaction);

                // read ticketId from "data" and call business.listInteractions(ticketId)
                case "listInteractions":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var oli = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)oli["ticketId"], out long listIntId))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId is required and must be a number" });
                    }

                    return JsonConvert.SerializeObject(business.listInteractions(listIntId));

                // read ticketId from "data" and call business.listAttachments(ticketId)
                case "listAttachments":
                    if (string.IsNullOrEmpty(data))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "data is required" });
                    }

                    var ola = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(data);
                    if (!long.TryParse((string)ola["ticketId"], out long listAttId))
                    {
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "ticketId is required and must be a number" });
                    }

                    return JsonConvert.SerializeObject(business.listAttachments(listAttId));

                default:
                    HttpStatusReturn = 400;
                    return JsonConvert.SerializeObject(new { error = "Unsupported method: " + method });
            }
        }

        //private string notImplemented(string method)
        //{
        //    HttpStatusReturn = 501; // Not Implemented
        //    return JsonConvert.SerializeObject(new { error = "Method not implemented yet: " + method });
        //}
    }
}
