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
            , string actionButton, string sortDirection = "asc", string sortField = "OrderItem")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name");


            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "OrderItem", "Quantity", "DateMade", "DeliveryDate" };

            var orders = _context.Orders
                .Include(o => o.Item)
                .AsNoTracking();

            //Add as many filters as needed
            if (SupplierID.HasValue)
            {
                orders = orders.Where(p => p.Item.SupplierID == SupplierID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                orders = orders.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
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
            if (sortField == "DeliveryDate")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.DeliveryDate);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.DeliveryDate);
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
                        .OrderBy(p => p.Item.Name);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Item.Name);
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
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Item)
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
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Quantity,DateMade,DeliveryDate,ItemID")] Order order)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {
                // Check if item already exists in items table
                var item = await _context.Items.FirstOrDefaultAsync(i => i.ID == order.ItemID);
                if (item == null)
                {
                    // If item doesn't exist, create a new item
                    item = new Item { ID = order.ItemID };
                    _context.Add(item);
                }
                // Add order to orders table
                _context.Add(order);
                await _context.SaveChangesAsync();

                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == order.ItemID);
                if (inventory != null)
                {
                    inventory.Quantity += order.Quantity;
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // If inventory for the item doesn't exist, create a new inventory
                    inventory = new Inventory { ItemID = order.ItemID, Quantity = order.Quantity };
                    _context.Add(inventory);
                    await _context.SaveChangesAsync();
                }
                // return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { order.ID });

            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", order.ItemID);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", order.ItemID);
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

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Order>(orderToUpdate, "",
                o => o.Quantity, o => o.DateMade, o => o.DeliveryDate, o => o.ItemID))
            {
                try
                {
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
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", orderToUpdate.ItemID);
            return View(orderToUpdate);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Item)
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
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

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
