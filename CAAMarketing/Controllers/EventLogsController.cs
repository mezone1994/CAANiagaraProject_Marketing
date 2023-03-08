using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using Microsoft.AspNetCore.Authorization;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class EventLogsController : Controller
    {
        private readonly CAAContext _context;

        public EventLogsController(CAAContext context)
        {
            _context = context;
        }

        // GET: EventLogs
        public async Task<IActionResult> Index()
        {
            var eventLogs = await _context.EventLogs.ToListAsync();
            return View(eventLogs);
        }

        // GET: EventLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.EventLogs == null)
            {
                return NotFound();
            }

            var eventLog = await _context.EventLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventLog == null)
            {
                return NotFound();
            }

            return View(eventLog);
        }

        // GET: EventLogs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EventLogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EventName,ItemName,Quantity,LogDate")] EventLog eventLog)
        {
            if (ModelState.IsValid)
            {
                _context.Add(eventLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(eventLog);
        }

        // GET: EventLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.EventLogs == null)
            {
                return NotFound();
            }

            var eventLog = await _context.EventLogs.FindAsync(id);
            if (eventLog == null)
            {
                return NotFound();
            }
            return View(eventLog);
        }

        // POST: EventLogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EventName,ItemName,Quantity,LogDate")] EventLog eventLog)
        {
            if (id != eventLog.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(eventLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventLogExists(eventLog.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(eventLog);
        }

        // GET: EventLogs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.EventLogs == null)
            {
                return NotFound();
            }

            var eventLog = await _context.EventLogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventLog == null)
            {
                return NotFound();
            }

            return View(eventLog);
        }

        // POST: EventLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.EventLogs == null)
            {
                return Problem("Entity set 'CAAContext.EventLogs'  is null.");
            }
            var eventLog = await _context.EventLogs.FindAsync(id);
            if (eventLog != null)
            {
                _context.EventLogs.Remove(eventLog);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventLogExists(int id)
        {
          return _context.EventLogs.Any(e => e.Id == id);
        }
    }
}
