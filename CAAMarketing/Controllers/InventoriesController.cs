using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using Microsoft.Extensions.Logging;
using CAAMarketing.Utilities;

namespace CAAMarketing.Controllers
{
    public class InventoriesController : Controller
    {
        private readonly CAAContext _context;

        public InventoriesController(CAAContext context)
        {
            _context = context;
        }

        // GET: Inventories
        public async Task<IActionResult> Index(string SearchString, int? LocationID, bool? LowQty,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            CheckInventoryLevel();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;
            var inventories = _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
                .AsNoTracking();

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");


            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Location", "Quantity", "Cost" };

            //Add as many filters as needed
            if (LocationID.HasValue)
            {
                inventories = inventories.Where(p => p.LocationID == LocationID);
                ViewData["Filtering"] = " show";
            }
            //if (LowQty.HasValue)
            //{
            //    inventories = inventories.Where(p => p.Quantity <= 10);
            //    ViewData["Filtering"] = " show";
            //}
            //if (!LowQty.HasValue)
            //{
            //    inventories = inventories.Where(p => p.Quantity >= 0);
            //    ViewData["Filtering"] = " show";
            //}
            if (!String.IsNullOrEmpty(SearchString))
            {
                inventories = inventories.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.Item.UPC.Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = " show";
            }
            

            if (TempData["InventoryLow"] != null)
            {
                ViewBag.InventoryLow = TempData["InventoryLow"].ToString();
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
            if (sortField == "Costs")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Cost);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Cost);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Quantity);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Location.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Location.Name);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Item.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Inventories");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Inventory>.CreateAsync(inventories.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventories/Create
        public IActionResult Create()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name");
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");
            return View();
        }

        // POST: Inventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Cost,Quantity,ItemID,LocationID,IsLowInventory,LowInventoryThreshold")] Inventory inventory)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {
                var existingInventory = _context.Inventories.FirstOrDefault(i => i.ItemID == inventory.ItemID);
                if (existingInventory != null)
                {
                    existingInventory.Quantity += inventory.Quantity;
                    _context.Update(existingInventory);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.Add(inventory);
                    await _context.SaveChangesAsync();
                }
                //Call the CheckInventoryLevel method after adding new inventory
                CheckInventoryLevel();
                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { inventory.ItemID });

            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventory.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventory.LocationID);
            return View(inventory);
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventory.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventory.LocationID);
            return View(inventory);
        }

        // POST: Inventories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var inventoryToUpdate = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(inventoryToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Inventory>(inventoryToUpdate, "",
                i => i.Cost, i => i.Quantity, i => i.ItemID, i => i.LocationID, i => i.IsLowInventory, i => i.LowInventoryThreshold))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { inventoryToUpdate.ItemID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventoryToUpdate.Id))
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
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventoryToUpdate.ItemID);
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name", inventoryToUpdate.LocationID);
            return View(inventoryToUpdate);
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Inventories == null)
            {
                return Problem("Entity set 'CAAContext.Inventories'  is null.");
            }
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventories.Remove(inventory);
            }
            
            await _context.SaveChangesAsync();
            // return RedirectToAction(nameof(Index));
            return Redirect(ViewData["returnURL"].ToString());

        }

        private void CheckInventoryLevel()
        {
            var lowInventoryItems = _context.Inventories.Where(i => i.Quantity <= i.LowInventoryThreshold);
            if (lowInventoryItems.Any())
            {
                TempData["InventoryLow"] = "Inventory is low for some items. Please check your inventory and reorder soon.";
            }
        }

        public IActionResult InventoryTransfer(int id, InventoryTransfer inventoryTransfer)
        {
            var inventory = _context.Inventories.Find(id);
            if (inventory == null)
            {
                return NotFound();
            }
            inventory.Location = _context.Locations.Find(inventoryTransfer.ToLocationId);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool InventoryExists(int id)
        {
          return _context.Inventories.Any(e => e.Id == id);
        }
    }
}
