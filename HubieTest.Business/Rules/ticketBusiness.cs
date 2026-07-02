using System;
using System.Collections.Generic;
using HubieTest.Business.Data;
using HubieTest.Dal;

namespace HubieTest.Business
{
    /// <summary>
    /// Ticket business rules. Orchestrates ticketDB and applies the status
    /// transition rules, interaction authorship, etc.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement the rules below. The logged-in user (id/profile/name) is
    /// injected by the handler from the JWT — use it instead of trusting an
    /// id that comes from the request body.
    /// ==================================================================
    /// </summary>
    public class ticketBusiness
    {
        private readonly ticketDB _db = new ticketDB();

        // logged-in user context (set by the handler from the token)
        public long loggedUserId { get; set; }
        public string loggedUserName { get; set; }
        public string loggedUserProfile { get; set; }

        public bool hasError { get; set; }
        public string ErrorMessage { get; set; }

        // Valid ticket status values (suggestion).
        public const string STATUS_OPEN = "OPEN";
        public const string STATUS_IN_PROGRESS = "IN_PROGRESS";
        public const string STATUS_ANSWERED = "ANSWERED";
        public const string STATUS_CLOSED = "CLOSED";

        /// <summary>REQUESTER opens a new ticket. Returns the created ticket.</summary>
        public TICKET open(TICKET ticket)
        {
            // TODO:
            //  - validate title/description/category
            //  - fill REQUESTER_ID/NAME from the logged-in user
            //  - set TICKET_STATUS = STATUS_OPEN and TICKET_CREATED_DT
            //  - persist via _db.create(...) and return the object
            if (ticket == null) { 
                hasError = true; 
                ErrorMessage = "Ticket is required."; 
                return null;
            }
            if (string.IsNullOrWhiteSpace(ticket.TICKET_TITLE) || string.IsNullOrWhiteSpace(ticket.TICKET_DESCRIPTION))
            { 
                hasError = true; 
                ErrorMessage = "Title and description are required."; 
                return null; 
            }
            if (ticket.CATEGORY_ID <= 0) { 
                hasError = true; 
                ErrorMessage = "Invalid category."; 
                return null; 
            }

            ticket.REQUESTER_ID = loggedUserId;
            ticket.REQUESTER_NAME = loggedUserName;
            ticket.TICKET_STATUS = STATUS_OPEN;
            ticket.TICKET_CREATED_DT = DateTime.Now;
            ticket.AGENT_ID = null;
            ticket.AGENT_NAME = null;

            var id = _db.create(ticket);
            ticket.TICKET_ID = id;
            return ticket;
            //throw new NotImplementedException("TODO: open ticket.");
        }

        /// <summary>Lists the tickets of the logged-in requester.</summary>
        public List<TICKET> listMyTickets()
        {
            hasError = false;
            return _db.listByRequester(loggedUserId);
            //throw new NotImplementedException("TODO: list the logged-in requester's tickets.");
        }

        /// <summary>Service queue (AGENT view).</summary>
        public List<TICKET> listQueue(string status)
        {
            if (string.Equals(loggedUserProfile, "AGENT", StringComparison.OrdinalIgnoreCase) == false)
            {
                hasError = true; ErrorMessage = "Only agents can access the queue."; return new List<TICKET>();
            }
            hasError = false;
            return _db.listQueue(status);
            //throw new NotImplementedException("TODO: list the agent queue.");
        }

        /// <summary>Ticket detail + interactions + attachments (to build the screen).</summary>
        public TICKET get(long ticketId)
        {
            hasError = false;
            var ticket = _db.get(ticketId);
            return ticket;
            //throw new NotImplementedException("TODO: get the ticket (and related data, if you want).");
        }

        /// <summary>AGENT takes the ticket (status -> IN_PROGRESS).</summary>
        public void assign(long ticketId)
        {
            // TODO: validate AGENT profile, store AGENT_ID/NAME and change the status.
            if (!string.Equals(loggedUserProfile, "AGENT", StringComparison.OrdinalIgnoreCase))
            { 
                hasError = true; 
                ErrorMessage = "Only agents can assign tickets."; 
                return; 
            }

            var ticket = _db.get(ticketId);
            if (ticket == null) { 
                hasError = true; 
                ErrorMessage = "Ticket not found."; 
                return; 
            }

            ticket.AGENT_ID = loggedUserId;
            ticket.AGENT_NAME = loggedUserName;
            ticket.TICKET_STATUS = STATUS_IN_PROGRESS;
            ticket.TICKET_UPDATED_DT = DateTime.Now;

            _db.update(ticket);
            hasError = false;
            //throw new NotImplementedException("TODO: assign ticket.");
        }

        /// <summary>Changes the ticket status, respecting valid transitions.</summary>
        public void changeStatus(long ticketId, string newStatus)
        {
            hasError = false;
            if (string.IsNullOrWhiteSpace(newStatus)) { 
                hasError = true; 
                ErrorMessage = "New status required."; 
                return; 
            }

            var allowed = new[] { STATUS_OPEN, STATUS_IN_PROGRESS, STATUS_ANSWERED, STATUS_CLOSED };
            if (Array.IndexOf(allowed, newStatus) < 0) { 
                hasError = true; 
                ErrorMessage = "Invalid status."; 
                return; 
            }

            var ticket = _db.get(ticketId);
            if (ticket == null) { 
                hasError = true; 
                ErrorMessage = "Ticket not found."; 
                return; 
            }

            if (ticket.TICKET_STATUS == STATUS_CLOSED && newStatus != STATUS_CLOSED)
            { 
                hasError = true; 
                ErrorMessage = "Cannot reopen a closed ticket."; 
                return; 
            }

            if (ticket.TICKET_STATUS == newStatus) { return; }

            ticket.TICKET_STATUS = newStatus;
            ticket.TICKET_UPDATED_DT = DateTime.Now;
            if (newStatus == STATUS_CLOSED) ticket.TICKET_CLOSED_DT = DateTime.Now;

            _db.update(ticket);
            //throw new NotImplementedException("TODO: change status with transition validation.");
        }

        /// <summary>
        /// Adds a message to the ticket thread. Valid for BOTH profiles
        /// (requester and agent). Set authorship from the logged-in user.
        /// </summary>
        public INTERACTION addInteraction(long ticketId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { hasError = true; ErrorMessage = "Message is required."; return null; }

            var ticket = _db.get(ticketId);
            if (ticket == null) { hasError = true; ErrorMessage = "Ticket not found."; return null; }

            var interaction = new INTERACTION
            {
                TICKET_ID = ticketId,
                USER_ID = loggedUserId,
                USER_NAME = loggedUserName,
                USER_PROFILE = loggedUserProfile,
                INTERACTION_MESSAGE = message,
                INTERACTION_CREATED_DT = DateTime.Now
            };

            var created = _db.addInteraction(interaction);

            // update ticket status: agent replies => ANSWERED, requester reply => IN_PROGRESS (simple rule)
            if (string.Equals(loggedUserProfile, "AGENT", StringComparison.OrdinalIgnoreCase))
                ticket.TICKET_STATUS = STATUS_ANSWERED;
            else
                ticket.TICKET_STATUS = STATUS_IN_PROGRESS;

            ticket.TICKET_UPDATED_DT = DateTime.Now;
            _db.update(ticket);

            hasError = false;
            return created;
            //throw new NotImplementedException("TODO: add interaction (authorship = logged-in user).");
        }

        public List<INTERACTION> listInteractions(long ticketId)
        {
            hasError = false;
            return _db.listInteractions(ticketId);
            //throw new NotImplementedException("TODO: list the ticket interactions.");
        }

        /// <summary>Registers an attachment already saved to disk by the upload handler.</summary>
        public ATTACHMENT registerAttachment(ATTACHMENT attachment)
        {
            if (attachment == null) { 
                hasError = true; 
                ErrorMessage = "Attachment is required."; 
                return null; 
            }
            attachment.USER_ID = loggedUserId;
            attachment.ATTACHMENT_CREATED_DT = DateTime.Now;
            var created = _db.addAttachment(attachment);
            hasError = false;
            return created;
            //throw new NotImplementedException("TODO: register the attachment metadata.");
        }

        public List<ATTACHMENT> listAttachments(long ticketId)
        {
            hasError = false;
            return _db.listAttachments(ticketId);
            //throw new NotImplementedException("TODO: list the ticket attachments.");
        }
    }
}
