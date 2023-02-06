using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CAAMarketing.Data;
using CAAMarketing.Models;
using CAAMarketing.Utilities;

namespace CAAMarketing.Controllers
{
    public class OrdersController : Controller
    {
        private readonly CAAContext _context;

        public OrdersController(CAAContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string SearchString, int? SupplierID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "item")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name");


            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "item", "Quantity", "DateMade", "DeliveryDate" , "UPC" };

            var orders = _context.Orders
                .Include(o => o.Inventory)
                .AsNoTracking();

            //Add as many filters as needed
            if (SupplierID.HasValue)
            {
                orders = orders.Where(p => p.Inventory.SupplierID == SupplierID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                orders = orders.Where(p => p.Inventory.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.Inventory.UPC.Contains(SearchString.ToUpper()));
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
            if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Inventory.UPC);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Inventory.UPC);
                }
            }
            else if (sortField == "DateMade")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderByDescending(p => p.DateMade);
                }
                else
                {
                    orders = orders
                        .OrderBy(p => p.DateMade);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Inventory.Name);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Inventory.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Orders");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Order>.CreateAsync(orders.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Inventory)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Quantity,DateMade,DeliveryDate,Cost,InventoryID")] Order order)
        {
            if (ModelState.IsValid)
            {
                // Check if item exists in items table
                var existinginventory = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == order.InventoryID);
                if (existinginventory == null)
                {
                    ModelState.AddModelError("", "Item not found");
                    ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name", order.InventoryID);
                    return View(order);
                }

                // Add order to orders table
                _context.Add(order);
                await _context.SaveChangesAsync();

                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == order.InventoryID);
                if (inventory != null)
                {
                    inventory.Quantity += order.Quantity;
                    inventory.Cost = order.Cost;
                    inventory.DateReceived = order.DeliveryDate.Value;
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // If inventory for the item doesn't exist, create a new inventory
                    ModelState.AddModelError("", "Inventory not found");
                    ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name", order.InventoryID);
                    return View(order);
                }

                return RedirectToAction("Details", new { order.ID });
            }
            ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name", order.InventoryID);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["InventoryID"] = new SelectList(_context.Inventories, "Id", "Name", order.InventoryID);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the order to update
            var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(o => o.ID == id);

            //Check that you got it or exit with a not found error
            if (orderToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(orderToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            var oldOrderQuantity = orderToUpdate.Quantity;
            if (await TryUpdateModelAsync<Order>(orderToUpdate, "",
                o => o.Quantity, o => o.DateMade, o => o.DeliveryDate, o => o.Cost, o => o.InventoryID))
            {
                try
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == orderToUpdate.InventoryID);
                    if (inventory != null)
                    {
                        var newInventoryQuantity = inventory.Quantity + (orderToUpdate.Quantity - oldOrderQuantity);
                        if (newInventoryQuantity > 0)
                        {
                            inventory.Quantity = newInventoryQuantity;
                        }
                        else
                        {
                            _context.Inventories.Remove(inventory);
                        }
                        _context.Update(inventory);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        if (orderToUpdate.Quantity > 0)
                        {
                            inventory = new Inventory
                            {
                                Id = orderToUpdate.InventoryID,
                                Quantity = orderToUpdate.Quantity,
                                Cost = orderToUpdate.Cost
                            };
                            _context.Inventories.Add(inventory);
                            await _context.SaveChangesAsync();
                        }
                    }
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { orderToUpdate.ID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(orderToUpdate.ID))
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
            ViewData["InventoryID"] = new SelectList(_context.Inventories, "ID", "Name", orderToUpdate.InventoryID);
            return View(orderToUpdate);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Inventory)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Orders == null)
            {
                return Problem("Entity set 'CAAContext.Orders'  is null.");
            }
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
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

        private bool OrderExists(int id)
        {
          return _context.Orders.Any(e => e.ID == id);
        }
    }
}
