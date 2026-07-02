using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using HubieTest.Dal;

namespace HubieTest.Business.Data
{
    /// <summary>
    /// Data access for TICKET, INTERACTION and ATTACHMENT.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement the methods below following the pattern shown in
    /// categoryDB.cs (open a DbContext in a using, turn off proxy/lazy,
    /// use EntityState for create/update). Feel free to adjust signatures
    /// if you think it is better — just explain your choices in the PR.
    /// ==================================================================
    /// </summary>
    public class ticketDB
    {
        // ---------- TICKET ----------

        /// <summary>Inserts a new ticket and returns the generated Id (Identity).</summary>
        public long create(TICKET ticket)
        {
            // HINT (Hubie pattern / ticketDB.create):
            using (var db = new HubieContext()) {
                  db.Entry(ticket).State = EntityState.Added;
                  db.SaveChanges();
                  return ticket.TICKET_ID; // populated after SaveChanges
            }
            //throw new NotImplementedException("TODO: create the ticket via EF and return TICKET_ID.");
        }

        /// <summary>Returns a ticket by id (or null).</summary>
        public TICKET get(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.TICKETS
                             .Where(t => t.TICKET_ID == ticketId)
                             .FirstOrDefault();
            }
            //throw new NotImplementedException("TODO: load the ticket by TICKET_ID.");
        }

        /// <summary>Lists the tickets opened by a requester (most recent first).</summary>
        public List<TICKET> listByRequester(long requesterId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.TICKETS
                         .Where(t => t.REQUESTER_ID == requesterId)
                         .OrderByDescending(t => t.TICKET_CREATED_DT)
                         .ToList();
            }
            //throw new NotImplementedException("TODO: filter by REQUESTER_ID, order by TICKET_CREATED_DT desc.");
        }

        /// <summary>
        /// Agent queue. If <paramref name="status"/> is null/empty, return every
        /// ticket that is not closed yet.
        /// </summary>
        public List<TICKET> listQueue(string status)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;

                var q = db.TICKETS.AsQueryable();
                if (string.IsNullOrEmpty(status))
                {
                    q = q.Where(t => t.TICKET_CLOSED_DT == null);
                }
                else
                {
                    q = q.Where(t => t.TICKET_STATUS == status);
                }

                return q.OrderByDescending(t => t.TICKET_CREATED_DT).ToList();
            }
            //throw new NotImplementedException("TODO: list tickets for the agent (optional status filter).");
        }

        /// <summary>Updates an existing ticket (status, agent, dates, etc.).</summary>
        public void update(TICKET ticket)
        {
            // HINT (Hubie pattern / ticketDB.update):
            //   db.Entry(ticket).State = EntityState.Modified; db.SaveChanges();
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();
            }
            //throw new NotImplementedException("TODO: update the ticket via EF.");
        }

        // ---------- INTERACTION ----------

        public INTERACTION addInteraction(INTERACTION interaction)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                if (interaction.INTERACTION_CREATED_DT == default(DateTime))
                    interaction.INTERACTION_CREATED_DT = DateTime.Now;
                db.Entry(interaction).State = EntityState.Added;
                db.SaveChanges();
                return interaction;
            }
            //throw new NotImplementedException("TODO: insert the interaction and return it with the generated id.");
        }

        public List<INTERACTION> listInteractions(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.INTERACTIONS
                         .Where(i => i.TICKET_ID == ticketId)
                         .OrderBy(i => i.INTERACTION_CREATED_DT)
                         .ToList();
            }
            //throw new NotImplementedException("TODO: list the ticket interactions in chronological order.");
        }

        // ---------- ATTACHMENT ----------

        public ATTACHMENT addAttachment(ATTACHMENT attachment)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                if (attachment.ATTACHMENT_CREATED_DT == default(DateTime))
                    attachment.ATTACHMENT_CREATED_DT = DateTime.Now;
                db.Entry(attachment).State = EntityState.Added;
                db.SaveChanges();
                return attachment;
            }
            //throw new NotImplementedException("TODO: insert the attachment record and return it with the generated id.");
        }

        public List<ATTACHMENT> listAttachments(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.ATTACHMENTS
                         .Where(a => a.TICKET_ID == ticketId)
                         .OrderBy(a => a.ATTACHMENT_CREATED_DT)
                         .ToList();
            }
            //throw new NotImplementedException("TODO: list the ticket attachments.");
        }
    }
}
