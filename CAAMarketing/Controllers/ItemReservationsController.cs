using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;

namespace CAAMarketing.Controllers
{
    public class ItemReservationsController : Controller
    {
        private readonly CAAContext _context;

        public ItemReservationsController(CAAContext context)
        {
            _context = context;
        }

        // GET: ItemReservations
        public async Task<IActionResult> Index()
        {
            var cAAContext = _context.ItemReservations.Include(i => i.Event).Include(i => i.Item);
            return View(await cAAContext.ToListAsync());
        }

        // GET: ItemReservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ItemReservations == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations
                .Include(i => i.Event)
                .Include(i => i.Item)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            ViewBag.LogBackInDate = itemReservation.ReturnDate.HasValue ? itemReservation.ReturnDate.Value.ToString("MM/dd/yyyy hh:mm tt") : "";

            return View(itemReservation);
        }

        // GET: ItemReservations/Create
        public IActionResult Create(int? itemId)
        {
            if (itemId == null || !_context.Items.Any(i => i.ID == itemId))
            {
                return NotFound();
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name");
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemId);

            return View(new ItemReservation { ItemId = itemId.Value });
        }

        // POST: ItemReservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EventId,ItemId,Quantity,ReservedDate,ReturnDate")] ItemReservation itemReservation, bool isLogBack)
        {
            if (ModelState.IsValid)
            {
                // Get the associated Event and Item objects
                itemReservation.Event = await _context.Events.FindAsync(itemReservation.EventId);
                itemReservation.Item = await _context.Items.FindAsync(itemReservation.ItemId);

                // Check if the selected item is already reserved for another event during the same time period
                bool isItemAvailable = _context.ItemReservations
                    .Where(ir => ir.ItemId == itemReservation.ItemId)
                    .All(ir => itemReservation.ReservedDate > ir.ReturnDate || itemReservation.ReturnDate < ir.ReservedDate);

                if (isItemAvailable)
                {
                    _context.Add(itemReservation);
                    await _context.SaveChangesAsync();

                    // Create a new event log entry
                    var eventLog = new EventLog
                    {
                        EventName = itemReservation.Event.Name,
                        ItemName = itemReservation.Item.Name,
                        Quantity = itemReservation.Quantity,
                        LogDate = DateTime.Now,
                        ItemReservation = itemReservation
                    };
                    _context.Add(eventLog);
                    await _context.SaveChangesAsync();

                    // Update the inventory quantity
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);
                    if (inventory != null)
                    {
                        if (isLogBack)
                        {
                            inventory.Quantity += itemReservation.Quantity;
                            itemReservation.LogBackInDate = DateTime.Now;
                        }
                        else
                        {
                            inventory.Quantity -= itemReservation.Quantity;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));

                }
                else
                {
                    ModelState.AddModelError("", "The selected item is already reserved for another event during the same time period.");
                }
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);
            return View(itemReservation);
        }


        // GET: ItemReservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations.FindAsync(id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);

            // Retrieve the original event log record
            var eventLog = await _context.EventLogs
                .FirstOrDefaultAsync(el => el.ItemReservation.Id == itemReservation.Id);

            // Store the original quantity and dates
            var originalQuantity = eventLog.Quantity;
            var originalReservedDate = itemReservation.ReservedDate;
            var originalReturnDate = itemReservation.ReturnDate;
            var originalLoggedOutDate = itemReservation.LoggedOutDate;

            return View(itemReservation);
        }

        // POST: ItemReservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EventId,ItemId,Quantity,ReservedDate,ReturnDate,LogedOutDate")] ItemReservation itemReservation)
        {
            if (id != itemReservation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if the selected item is already reserved for another event during the same time period
                    bool isItemAvailable = _context.ItemReservations
                        .Where(ir => ir.ItemId == itemReservation.ItemId && ir.Id != itemReservation.Id)
                        .All(ir => itemReservation.ReservedDate > ir.ReturnDate || itemReservation.ReturnDate < ir.ReservedDate);

                    if (isItemAvailable)
                    {
                        // Get the original item reservation
                        var originalItemReservation = await _context.ItemReservations.AsNoTracking().FirstOrDefaultAsync(ir => ir.Id == itemReservation.Id);

                        // Get the original event log
                        var originalEventLog = await _context.EventLogs.FirstOrDefaultAsync(el => el.ItemReservation.Id == originalItemReservation.Id);

                        // Update the inventory quantity
                        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);
                        if (inventory != null)
                        {
                            // Check if quantity is being added or subtracted
                            if (itemReservation.Quantity > originalItemReservation.Quantity)
                            {
                                inventory.Quantity -= (itemReservation.Quantity - originalItemReservation.Quantity);
                            }
                            else if (itemReservation.Quantity < originalItemReservation.Quantity)
                            {
                                inventory.Quantity += (originalItemReservation.Quantity - itemReservation.Quantity);
                            }

                            // Check if log out date is being updated
                            if (itemReservation.LoggedOutDate != originalItemReservation.LoggedOutDate)
                            {
                                inventory.Quantity += originalItemReservation.Quantity;
                            }
                        }

                        _context.Update(itemReservation);
                        //await _context.SaveChangesAsync();

                        //// Update the event log
                        //if (originalEventLog != null)
                        //{
                        //    originalEventLog.EventName = itemReservation.Event.Name;
                        //    originalEventLog.ItemName = itemReservation.Item.Name;
                        //    originalEventLog.Quantity = itemReservation.Quantity;
                        //    originalEventLog.LogDate = DateTime.Now;

                        //    _context.Update(originalEventLog);
                        //}

                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "The selected item is already reserved for another event during the same time period.");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemReservationExists(itemReservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["EventId"] = new SelectList(_context.Events, "ID", "Name", itemReservation.EventId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", itemReservation.ItemId);
            return View(itemReservation);
        }


        // GET: ItemReservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemReservation = await _context.ItemReservations
                .Include(ir => ir.Event)
                .Include(ir => ir.Item)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            return View(itemReservation);
        }

        // POST: ItemReservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var itemReservation = await _context.ItemReservations.FindAsync(id);
            if (itemReservation == null)
            {
                return NotFound();
            }

            // Mark the ItemReservation as deleted
            itemReservation.IsDeleted = true;

            // Update the inventory quantity
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);
            if (inventory != null)
            {
                inventory.Quantity += itemReservation.Quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogBackIn(int id)
        {
            var itemReservation = await _context.ItemReservations
                .Include(ir => ir.Item)
                .FirstOrDefaultAsync(ir => ir.Id == id);

            if (itemReservation == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemReservation.ItemId);

            if (inventory == null)
            {
                ModelState.AddModelError("", "The selected item does not exist in the inventory.");
                return RedirectToAction("Details", "Items", new { id = itemReservation.ItemId });
            }

            inventory.Quantity += itemReservation.Quantity;
            itemReservation.ReturnDate = DateTime.Now;
            itemReservation.LogBackInDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Items", new { id = itemReservation.ItemId });
        }

        private bool ItemReservationExists(int id)
        {
            return _context.ItemReservations.Any(e => e.Id == id);
        }
    }
}
