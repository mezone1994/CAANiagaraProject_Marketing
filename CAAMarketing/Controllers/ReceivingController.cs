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
using NToastNotify;

namespace CAAMarketing.Controllers
{
    public class ReceivingController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public ReceivingController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string SearchString, int? SupplierID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
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
            string[] sortOptions = new[] { "Item", "Quantity", "UPC","Cost", "DateMade", "DeliveryDate" };

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
            //if (sortField == "DeliveryDate")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        orders = orders
            //            .OrderBy(p => p.DeliveryDate);
            //    }
            //    else
            //    {
            //        orders = orders
            //            .OrderByDescending(p => p.DeliveryDate);
            //    }
            //}
            //else if (sortField == "DateMade")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        orders = orders
            //            .OrderByDescending(p => p.DateMade);
            //    }
            //    else
            //    {
            //        orders = orders
            //            .OrderBy(p => p.DateMade);
            //    }
            //}
            if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    orders = orders
                        .OrderBy(p => p.Item.UPC);
                }
                else
                {
                    orders = orders
                        .OrderByDescending(p => p.Item.UPC);
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
            else //Sorting by Item Name
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
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Receiving");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Receiving>.CreateAsync(orders.AsNoTracking(), page ?? 1, pageSize);
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
        
        public IActionResult Create(int? id)
        {
            // existing code...

            if (id != null)
            {
                ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", id);
            }
            else
            {
                ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name");
            }

            _toastNotification.AddSuccessToastMessage($"Item Created!");

            _toastNotification.AddAlertToastMessage($"You Can Now Enter Any Recieving That Was Ordered For This Product. If There Wasnt Any Recieving, You Can Skip By Clicking The 'Skip' Button");


            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Quantity,DateMade,DeliveryDate,Cost,ItemID")] Receiving order)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            Receiving newOrder = new Receiving();
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
                newOrder.ItemID = order.ItemID;
                newOrder.Cost = order.Cost;
                newOrder.DateMade = order.DateMade;
                newOrder.DeliveryDate = order.DeliveryDate;
                newOrder.Quantity= order.Quantity;
                newOrder.CreatedBy = order.CreatedBy;
                newOrder.UpdatedBy = order.UpdatedBy;
                newOrder.UpdatedOn = order.UpdatedOn;
                newOrder.EmployeeNameUser = order.EmployeeNameUser;
                // Add order to orders table
                _context.Add(newOrder);
                await _context.SaveChangesAsync();

                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == order.ItemID);
                if (inventory != null)
                {
                    inventory.Quantity += order.Quantity;
                    inventory.Cost = order.Cost;
                    inventory.Item.DateReceived = order.DeliveryDate.Value;
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // If inventory for the item doesn't exist, create a new inventory
                    inventory = new Inventory { ItemID = order.ItemID, Quantity = order.Quantity, Cost = order.Cost };
                    _context.Add(inventory);
                    await _context.SaveChangesAsync();
                }
                // return RedirectToAction(nameof(Index));
                ViewBag.Message = "This is a message from Controller 1.";
                //Send on to add orders
                return RedirectToAction("Index", "OrderItems", new { ItemID = order.ItemID });
                //return RedirectToAction("Details", new { order.ID });

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

            var oldOrderQuantity = orderToUpdate.Quantity;
            if (await TryUpdateModelAsync<Receiving>(orderToUpdate, "",
                o => o.Quantity, o => o.DateMade, o => o.DeliveryDate, o => o.Cost, o => o.ItemID))
            {
                try
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == orderToUpdate.ItemID);
                    if (inventory != null)
                    {
                        var newInventoryQuantity = inventory.Quantity + (orderToUpdate.Quantity - oldOrderQuantity);
                        if (newInventoryQuantity > 0)
                        {
                            inventory.Quantity = newInventoryQuantity;
                            inventory.Cost = orderToUpdate.Cost;
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
                                ItemID = orderToUpdate.ItemID,
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
            var order = await _context.Orders
              .Include(o => o.Item)
              .FirstOrDefaultAsync(m => m.ID == id);

            try
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == order.ItemID);
                if (inventory != null)
                {
                    var newInventoryQuantity = inventory.Quantity - order.Quantity;
                    if (newInventoryQuantity > 0)
                    {
                        inventory.Quantity = newInventoryQuantity;
                        _context.Inventories.Update(inventory);
                    }
                    else
                    {
                        _context.Inventories.Remove(inventory);
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                    "persists see your system administrator.");
            }

            return View(order);
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
