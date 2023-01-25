﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using Microsoft.Extensions.Options;

namespace CAAMarketing.Controllers
{
    public class EventsController : Controller
    {
        private readonly CAAContext _context;

        public EventsController(CAAContext context)
        {
            _context = context;
        }

        // GET: Events
        public async Task<IActionResult> Index(string SearchString, int? LocationID
            , string actionButton, string sortDirection = "asc", string sortField = "Event")
        {

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Date", "Location" };


            var events = _context.Events
                .Include(a => a.Location)
                .AsNoTracking();

            //Add as many filters as needed
            if (LocationID.HasValue)
            {
                events = events.Where(p => p.LocationID == LocationID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                events = events.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = " show";
            }

            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
            }

            //Now we know which field and direction to sort by
            if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.Location);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Date")
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.Date);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.Date);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    events = events
                        .OrderBy(p => p.Name);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            return View(await events.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(a => a.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,Date,LocationID")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", @event.LocationID);
            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", @event.LocationID);
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //Go get the Event to update
            var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.ID == id);

            if (eventToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(eventToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Event>(eventToUpdate, "",
                e => e.Name, e => e.Description, e => e.Date, e => e.LocationID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(eventToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user. Please go back and refresh.");
                    }
                }
                catch (DbUpdateException dex)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");

                }
            }
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", eventToUpdate.LocationID);
            return View(eventToUpdate);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(a => a.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Events == null)
            {
                return Problem("Entity set 'CAAContext.Events'  is null.");
            }
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
          return _context.Events.Any(e => e.ID == id);
        }
    }
}
