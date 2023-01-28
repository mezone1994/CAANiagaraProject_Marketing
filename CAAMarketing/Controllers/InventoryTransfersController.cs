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
    public class InventoryTransfersController : Controller
    {
        private readonly CAAContext _context;

        public InventoryTransfersController(CAAContext context)
        {
            _context = context;
        }

        // GET: InventoryTransfers
        public async Task<IActionResult> Index(string SearchString, int? FromLocationID, int? ToLocationId,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "ItemTransfered")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

<<<<<<< HEAD
            
=======
            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryTransfers");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<InventoryTransfer>.CreateAsync(_context.InventoryTransfers.AsNoTracking(), page ?? 1, pageSize);
>>>>>>> 0c5fd6057a82587917b511e24d04ef4a864e4fb9


            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");

            var transfers = _context.InventoryTransfers
                .Include(i => i.FromLocation)
                .Include(i => i.Item)
                .Include(i => i.ToLocation)
                .AsNoTracking();

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "ItemTransfered", "FromLocation", "ToLocation", "Quantity", "TransferDate" };


            //Add as many filters as needed
            if (FromLocationID.HasValue)
            {
                transfers = transfers.Where(p => p.FromLocationId == FromLocationID);
                ViewData["Filtering"] = " show";
            }
            if (ToLocationId.HasValue)
            {
                transfers = transfers.Where(p => p.ToLocationId == ToLocationId);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                transfers = transfers.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.Item.UPC.Contains(SearchString.ToUpper()));
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
            if (sortField == "TransferDate")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.TransferDate);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.TransferDate);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderByDescending(p => p.Quantity);
                }
                else
                {
                    transfers = transfers
                        .OrderBy(p => p.Quantity);
                }
            }
            else if (sortField == "ToLocation")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.ToLocation.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.ToLocation.Name);
                }
            }
            else if (sortField == "FromLocation")
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.FromLocation.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.FromLocation.Name);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    transfers = transfers
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    transfers = transfers
                        .OrderByDescending(p => p.Item.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;



            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryTransfers");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<InventoryTransfer>.CreateAsync(transfers.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: InventoryTransfers/Create
        public IActionResult Create()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name");
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            return View();
        }

        // POST: InventoryTransfers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ItemId,FromLocationId,ToLocationId,Quantity,TransferDate")] InventoryTransfer inventoryTransfer)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {
                // Find the inventory record for the item being transferred from the specified location
                var fromInventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.FromLocationId);

                // Deduct the transferred quantity from the inventory record
                fromInventory.Quantity -= inventoryTransfer.Quantity;

                // Find the inventory record for the item being transferred to the specified location
                var toInventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.ToLocationId);

                // If the inventory record for the destination location does not exist, create a new one
                if (toInventory == null)
                {
                    toInventory = new Inventory
                    {
                        ItemID = inventoryTransfer.ItemId,
                        LocationID = inventoryTransfer.ToLocationId,
                        Quantity = inventoryTransfer.Quantity
                    };
                    _context.Add(toInventory);
                }
                else
                {
                    // Else, add the transferred quantity to the existing inventory record
                    toInventory.Quantity += inventoryTransfer.Quantity;
                }

                // Add the inventory transfer record to the database
                _context.Add(inventoryTransfer);

                // Save changes to the database
                await _context.SaveChangesAsync();

                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { inventoryTransfer.ItemId });

            }
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.FromLocationId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", inventoryTransfer.ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.ToLocationId);
            return View(inventoryTransfer);
        }

    
    // GET: InventoryTransfers/Edit/5
    public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.InventoryTransfers == null)
            {
                return NotFound();
            }

            var inventoryTransfer = await _context.InventoryTransfers.FindAsync(id);
            if (inventoryTransfer == null)
            {
                return NotFound();
            }
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.FromLocationId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", inventoryTransfer.ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", inventoryTransfer.ToLocationId);
            return View(inventoryTransfer);
        }

        // POST: InventoryTransfers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the InventoryTransfers to update
            var transferToUpdate = await _context.InventoryTransfers.FirstOrDefaultAsync(t => t.Id == id);

            if (transferToUpdate == null)
            {
                return NotFound();
            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(transferToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<InventoryTransfer>(transferToUpdate, "",
                t => t.ItemId, t => t.FromLocationId, t => t.ToLocationId, t => t.Quantity, t => t.TransferDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { transferToUpdate.ItemId });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryTransferExists(transferToUpdate.Id))
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
            ViewData["FromLocationId"] = new SelectList(_context.Locations, "Id", "Name", transferToUpdate.FromLocationId);
            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", transferToUpdate.ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", transferToUpdate.ToLocationId);
            return View(transferToUpdate);
        }

        // GET: InventoryTransfers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.InventoryTransfers == null)
            {
                return NotFound();
            }

            var inventoryTransfer = await _context.InventoryTransfers
                .Include(i => i.FromLocation)
                .Include(i => i.Item)
                .Include(i => i.ToLocation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventoryTransfer == null)
            {
                return NotFound();
            }

            return View(inventoryTransfer);
        }

        // POST: InventoryTransfers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.InventoryTransfers == null)
            {
                return Problem("Entity set 'CAAContext.InventoryTransfers'  is null.");
            }
            var inventoryTransfer = await _context.InventoryTransfers.FindAsync(id);
            if (inventoryTransfer != null)
            {
                _context.InventoryTransfers.Remove(inventoryTransfer);
            }
            
            await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
            return Redirect(ViewData["returnURL"].ToString());

        }
        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool InventoryTransferExists(int id)
        {
          return _context.InventoryTransfers.Any(e => e.Id == id);
        }
    }
}
