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
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using NToastNotify;
using Org.BouncyCastle.Utilities;
using CAAMarketing.ViewModels;
using AspNetCoreHero.ToastNotification.Abstractions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Syncfusion.Blazor.Buttons;
using System.ComponentModel;
using AspNetCore;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Microsoft.AspNetCore.Authorization;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class InventoriesController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;
        private readonly INotyfService _itoastNotify;

        public InventoriesController(CAAContext context, IToastNotification toastNotification, INotyfService itoastNotify)
        {
            _context = context;
            _toastNotification = toastNotification;
            _itoastNotify = itoastNotify;
        }

        // GET: Inventories
        public async Task<IActionResult> Index(string SearchString, int?[] LocationID, bool? LowQty,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            ViewDataReturnURL();

            //FOR THE SILENTMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForSilent = _context.Inventories.Where(i => i.DismissNotification > DateTime.Now && i.Item.Archived != true).Count();
            var invnullsForSilent = _context.Inventories.Where(i => i.DismissNotification == null && i.Item.Archived != true).Count();
            ViewData["SilencedMessageCount"] = (invForSilent + invnullsForSilent).ToString();
            //--------------------------------------------------------------------

            // FOR THE ACTIVEMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForActive = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Where(i => i.DismissNotification <= DateTime.Now && i.Quantity < i.Item.Category.LowCategoryThreshold && i.Item.Archived != true && i.DismissNotification != null).Count();
            
            ViewData["ActiveMessageCount"] = (invForActive).ToString();
            //--------------------------------------------------------------------

            // FOR THE RECOVERALLMESSAGE BUTTON SHOWING HOW MANY NOTIF ARE INSIDE
            var invForRecover = _context.Inventories.Where(i => i.DismissNotification > DateTime.Now).Count();
            var invnullsForRecover = _context.Inventories.Where(i => i.DismissNotification == null && i.Item.Archived != true).Count();
            ViewData["RecoverMessageCount"] = (invForRecover + invnullsForRecover).ToString();
            //--------------------------------------------------------------------

            if (TempData["RecoverNotifMessageBool"] != null)
            {
                _toastNotification.AddSuccessToastMessage(@$"Message Recovered!");
            }
            if (TempData["SilenceNotifMessageBool"] != null)
            {
                _toastNotification.AddSuccessToastMessage(@$"Message Silenced!");
            }
            if (TempData.ContainsKey("NotifFromPopupSuccess") && TempData["NotifFromPopupSuccess"] != null)
            {
                if (TempData["NotifFromPopupSuccess"].ToString() == "Silent")
                {
                    _toastNotification.AddSuccessToastMessage(@$"Message Silenced!");
                }
                if (TempData["NotifFromPopupSuccess"].ToString() == "Activate")
                {
                    _toastNotification.AddSuccessToastMessage(@$"Message Activated!");
                }
            }

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;
            var inventories = _context.Inventories
                .Include(i => i.Item.ItemThumbNail)
                .Include(i => i.Item.Employee)
                .Include(i => i.Location)
                .Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i=>i.Item.ItemLocations).ThenInclude(i=>i.Location)
            .AsNoTracking();


            inventories = inventories.Where(p => p.Item.Archived == false);
            //CheckInventoryLevel(inventories.ToList());

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Location", "UPC", "Quantity", "Cost" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                inventories = inventories.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
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
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString, out searchUPC);

                inventories = inventories.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || (isNumeric && p.Item.UPC == searchUPC));
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
            //if (sortField == "Costs")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        inventories = inventories
            //            .OrderBy(p => p.Cost.ToString());
            //    }
            //    else
            //    {
            //        inventories = inventories
            //            .OrderByDescending(p => p.Cost.ToString());
            //    }
            //}
            if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(i => i.Quantity);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(i => i.Quantity);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Item.UPC);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Item.UPC);
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
            else //Sorting by Item Name
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
                .Include(i => i.Item.ItemImages)
                .Include(i => i.Location)
                .Include(i => i.Item.Employee)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
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
        public async Task<IActionResult> Create(string selectedValue, string selectedItemId, string selectedItemId1, string TypeOfOperation)
        {
            string typeofoperation = HttpContext.Session.GetString("NotifOperation");
            

            

            if (TypeOfOperation == "Activate")
            {
                // HttpContext.Session.SetString("SelectedDDLValueForSilentNotif", selectedValue);
                HttpContext.Session.SetString("ItemIdForPartialNotif", selectedItemId1);

                int itemID = Convert.ToInt32(HttpContext.Session.GetString("ItemIdForPartialNotif"));

                var invEditNotif = _context.Inventories.FirstOrDefault(i => i.ItemID == itemID);

                try
                {
                    invEditNotif.DismissNotification = DateTime.Now;

                    _context.Update(invEditNotif);
                    _context.SaveChanges();
                }
                catch (Exception)
                {

                    throw;
                }
                TempData["NotifFromPopupSuccess"] = "Activate";
            }
               


            else if (TypeOfOperation == "Silent")
            {
                // HttpContext.Session.SetString("SelectedDDLValueForSilentNotif", selectedValue);
                HttpContext.Session.SetString("ItemIdForPartialNotif", selectedItemId);

                int itemID = Convert.ToInt32(HttpContext.Session.GetString("ItemIdForPartialNotif"));

                var invEditNotif = _context.Inventories.FirstOrDefault(i => i.ItemID == itemID);

                int days = 10;

                if (selectedValue == "1") { days = 1; }
                else if (selectedValue == "2") { days = 2; }
                else if (selectedValue == "3") { days = 3; }
                else if (selectedValue == "7") { days = 7; }
                else if (selectedValue == "0") { days = 0; }


                if (invEditNotif != null)
                {
                    if (days == 0)
                    {
                        invEditNotif.DismissNotification = null;
                        _context.Update(invEditNotif);
                        _context.SaveChanges();
                    }
                    else
                    {
                        int numofDays = Convert.ToInt32(HttpContext.Session.GetString("SelectedDDLValueForSilentNotif"));
                        invEditNotif.DismissNotification = DateTime.Now;
                        invEditNotif.DismissNotification = DateTime.Now.AddDays(days);
                        _context.Update(invEditNotif);
                        _context.SaveChanges();
                    }
                    TempData["NotifFromPopupSuccess"] = "Silent";
                }
            }





            return RedirectToAction(nameof(Index));
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
                i => i.Cost, i => i.Quantity, i => i.ItemID, i => i.LocationID))
            {
                try
                {

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                    //return RedirectToAction("Details", new { inventoryToUpdate.ItemID });

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
                .Include(i => i.Item.Employee)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == inventory.ItemID);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
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

        private void CheckInventoryLevel(List<Inventory> inventories)
        {
            foreach (var inventory in inventories)
            {

                if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                {
                    if (inventory.DismissNotification <= DateTime.Now)
                    {
                        inventory.IsLowInventory = true;
                        _toastNotification.AddInfoToastMessage(
                            $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a>
                                    <br><br>Qiuck Actions:
                                    <button style='background:#3630a3;color:white;'>
                                    <a href='#' onclick='redirectToSilenceNotif({inventory.Item.ID}); return false;'>Silent This Notification?</a>
                                    
                                    <button class='btn btn-outline-secondary' id='nowEditNotifSilent1' data-bs-toggle='modal' data-bs-target='#addNotifModal' type='button'>
                                        <strong>Check Messages</strong>
                                    </button>
                                    ");
                    }
                    
        
    
                }
                else
                {
                    inventory.IsLowInventory = false;
                }
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

        //Method for Viewing Full Inventory Report
        public async Task<IActionResult> InventoryReport(string SearchString, int?[] LocationID, int? page, int? pageSizeID, string actionButton,
            string sortDirection = "asc", string sortField = "ItemName")
        {
            //For the Report View
            //var sumQ = _context.Inventories.Include(a => a.Item).Include(p => p.Location)
            //   .GroupBy(a => new { a.ItemID, a.Item.Name })
            //   .Select(grp => new InventoryReportVM
            //   {
            //       ID = grp.Key.ItemID,
            //       Category = grp.Select(grp => grp.Item.Category.Name).ToString(),
            //       UPC = grp.Select(grp => grp.Item.UPC).ToString(),
            //       ItemName = grp.Select(grp => grp.Item.Name).ToString(),
            //       Cost = Convert.ToDecimal(grp.Select(grp => grp.Cost)),
            //       Quantity = Convert.ToInt32(grp.Select(grp => grp.Quantity)),
            //       Location = grp.Select(grp => grp.Location.Name).ToString(),
            //       Supplier = grp.Select(grp => grp.Item.Supplier.Name).ToString(),
            //       DateReceived = Convert.ToDateTime(grp.Select(grp => grp.Item.DateReceived)),
            //       Notes = grp.Select(grp => grp.Item.Notes).ToString()
            //   }).OrderBy(s => s.ItemName);

            //For report
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        .Include(i => i.Item.ItemLocations)
                        .Include(i => i.Location)

                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           UPC = i.Item.UPC.ToString(),
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Category", "UPC", "ItemName", "Cost", "Quantity", "Location", "Supplier", "DateReveived" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.UPC.Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
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
            if (sortField == "Category")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(i => i.Category);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(i => i.Category);
                }
            }
            else if (sortField == "UPC")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            //else if (sortField == "ItemName")
            //{
            //    if (sortDirection == "asc")
            //    {
            //        sumQ = sumQ
            //            .OrderBy(p => p.ItemName);
            //    }
            //    else
            //    {
            //        sumQ = sumQ
            //            .OrderByDescending(p => p.ItemName);
            //    }
            //}
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Supplier);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Supplier);
                }
            }
            else if (sortField == "DateReveived")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.DateReceived);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.DateReceived);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryReport");//Remember for this View
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        //Method for Excel Full Inventory Report
        public IActionResult DownloadInventory()
        {
            //Get the inventory
            var items = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        orderby i.Item.Name ascending
                        select new
                        {
                            Category = i.Item.Category.Name,
                            UPC = i.Item.UPC,
                            Item = i.Item.Name,
                            Cost = i.Cost,
                            Quantity = i.Quantity,
                            Location = i.Location.Name,
                            Supplier = i.Item.Supplier.Name,
                            DateRecieved = i.Item.DateReceived,
                            Notes = i.Item.Notes
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {

                    //Note: you can also pull a spreadsheet out of the database if you
                    //have saved it in the normal way we do, as a Byte Array in a Model
                    //such as the UploadedFile class.
                    //
                    // Suppose...
                    //
                    // var theSpreadsheet = _context.UploadedFiles.Include(f => f.FileContent).Where(f => f.ID == id).SingleOrDefault();
                    //
                    //    //Pass the Byte[] FileContent to a MemoryStream
                    //
                    // using (MemoryStream memStream = new MemoryStream(theSpreadsheet.FileContent.Content))
                    // {
                    //     ExcelPackage package = new ExcelPackage(memStream);
                    // }

                    var workSheet = excel.Workbook.Worksheets.Add("Inventory");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style 6th column for dates (DateRecieved)
                    workSheet.Column(8).Style.Numberformat.Format = "yyyy-mm-dd";

                    //Style fee column for currency
                    workSheet.Column(4).Style.Numberformat.Format = "$###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Category Bold
                    workSheet.Cells[4, 1, numRows + 3, 1].Style.Font.Bold = true;
                    //Make Item Names Bold
                    workSheet.Cells[4, 3, numRows + 3, 3].Style.Font.Bold = true;
                    //Make Item Locations Bold
                    workSheet.Cells[4, 6, numRows + 3, 6].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 5, numRows + 3, 5].Style.Font.Bold = true;
                    var item = from i in _context.Inventories
                               orderby i.Item.Name ascending
                               select i.Quantity;
                    int row = 4;
                    foreach (var qty in item)
                    {
                        if (row <= (numRows + 3))
                        {
                            if (qty == 0)
                            {
                                workSheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Red);
                                row++;
                            }
                            else if ((qty <= 10) && (qty > 0))
                            {
                                workSheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Orange);
                                row++;
                            }
                            else if (qty > 10)
                            {
                                workSheet.Cells[row, 5].Style.Font.Color.SetColor(Color.Green);
                                row++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 4])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 3].Value = "Total Cost:";
                        workSheet.Cells[numRows + 4, 3].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalfees.Formula = "Sum(" + (workSheet.Cells[4, 4].Address) + ":" + workSheet.Cells[numRows + 3, 4].Address + ")" + "*" + "Sum(" +
                            (workSheet.Cells[4, 5].Address) + ":" + workSheet.Cells[numRows + 3, 5].Address + ")";
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,###,##0.00";
                        var range = workSheet.Cells[numRows + 4, 4, numRows + 4, 5];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 9])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    ////Boy those notes are BIG!
                    ////Lets put them in comments instead.
                    //for (int i = 4; i < numRows + 4; i++)
                    //{
                    //    using (ExcelRange Rng = workSheet.Cells[i, 7])
                    //    {
                    //        string[] commentWords = Rng.Value.ToString().Split(' ');
                    //        Rng.Value = commentWords[0] + "...";
                    //        //This LINQ adds a newline every 7 words
                    //        string comment = string.Join(Environment.NewLine, commentWords
                    //            .Select((word, index) => new { word, index })
                    //            .GroupBy(x => x.index / 7)
                    //            .Select(grp => string.Join(" ", grp.Select(x => x.word))));
                    //        ExcelComment cmd = Rng.AddComment(comment, "Apt. Notes");
                    //        cmd.AutoFit = true;
                    //    }
                    //}

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 9])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 9])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Levels Report
        public async Task<IActionResult> InventoryLevelsReport(string SearchString, int?[] LocationID, int? page, int? pageSizeID, string actionButton,
            string sortDirection = "asc", string sortField = "ItemName")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        .Include(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           UPC = i.Item.UPC.ToString(),
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "ItemName", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.UPC.Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
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
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryLevelsReport");//Remember for this View
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        //Method for Excel Inventory Levels Report
        public ActionResult DownloadInventoryLevels()
        {
            //Get the inventory
            var items = from i in _context.Inventories
                        orderby i.Location, i.Item.Name, i.Quantity ascending
                        select new
                        {
                            UPC = i.Item.UPC,
                            Item = i.Item.Name,
                            Quantity = i.Quantity,
                            Location = i.Location.Name
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Inventory Levels");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for currency
                    workSheet.Column(3).Style.Numberformat.Format = "###,###,##0";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Name Bold
                    workSheet.Cells[4, 2, numRows + 3, 2].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 3, numRows + 3, 3].Style.Font.Bold = true;
                    var item = from i in _context.Inventories
                               orderby i.Location, i.Item.Name, i.Quantity ascending
                               select i.Quantity;
                    int row = 4;
                    foreach (var qty in item)
                    {
                        if (row <= (numRows + 3))
                        {
                            if (qty == 0)
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Red);
                                row++;
                            }
                            else if ((qty <= 10) && (qty > 0))
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Orange);
                                row++;
                            }
                            else if (qty > 10)
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Green);
                                row++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    using (ExcelRange totalQty = workSheet.Cells[numRows + 4, 3])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 2].Value = "Total Quantity:";
                        workSheet.Cells[numRows + 4, 2].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalQty.Formula = "Sum(" + (workSheet.Cells[4, 3].Address) + ":" + workSheet.Cells[numRows + 3, 3].Address + ")";
                        totalQty.Style.Font.Bold = true;
                        totalQty.Style.Numberformat.Format = "###,###,##0";
                        var range = workSheet.Cells[numRows + 4, 4, numRows + 4, 5];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 4])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Levels Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 4])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 4])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryLevelsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Costs Report
        public async Task<IActionResult> InventoryCostsReport(string SearchString, int?[] LocationID, int? page, int? pageSizeID, string actionButton,
            string sortDirection = "asc", string sortField = "ItemName")
        {
            //For the Report View
            var sumQ = from i in _context.Inventories
                        .Include(i => i.Item.Supplier)
                        .Include(i => i.Item.Category)
                        .Include(i => i.Item.Employee)
                        .Include(i => i.Location)
                       orderby i.Item.Name ascending
                       select new InventoryReportVM
                       {
                           ID = i.ItemID,
                           Category = i.Item.Category.Name,
                           UPC = i.Item.UPC.ToString(),
                           ItemName = i.Item.Name,
                           Cost = i.Cost,
                           Quantity = i.Quantity,
                           Location = i.Location.Name,
                           LocationID = i.LocationID,
                           Supplier = i.Item.Supplier.Name,
                           DateReceived = (DateTime)i.Item.DateReceived,
                           Notes = i.Item.Notes
                       };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "UPC", "ItemName", "Cost", "Quantity", "Location" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(SearchString))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.UPC.Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
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
                    sumQ = sumQ
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Cost")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Cost.ToString());
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Cost.ToString());
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Location);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Location);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryCostsReport");//Remember for this View
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<InventoryReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        //Method for Excel Inventory Costs Report
        public ActionResult DownloadInventoryCosts()
        {
            //Get the inventory
            var items = from i in _context.Inventories
                        orderby i.Location, i.Item.Name, i.Quantity ascending
                        select new
                        {
                            UPC = i.Item.UPC,
                            Item = i.Item.Name,
                            Cost = i.Cost,
                            Quantity = i.Quantity,
                            Location = i.Location.Name
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Inventory Costs");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for quantity
                    workSheet.Column(4).Style.Numberformat.Format = "###,###,##0";

                    //Style fee column for currency
                    workSheet.Column(3).Style.Numberformat.Format = "$###,###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Item Name Bold
                    workSheet.Cells[4, 2, numRows + 3, 2].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 3, numRows + 3, 4].Style.Font.Bold = true;
                    var item = from i in _context.Inventories
                               orderby i.Location, i.Item.Name, i.Quantity ascending
                               select i.Quantity;
                    int row = 4;
                    foreach (var qty in item)
                    {
                        if (row <= (numRows + 3))
                        {
                            if (qty == 0)
                            {
                                workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                                row++;
                            }
                            else if ((qty <= 10) && (qty > 0))
                            {
                                workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Orange);
                                row++;
                            }
                            else if (qty > 10)
                            {
                                workSheet.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                                row++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    //Total Cost for all Items in Inventory
                    //workSheet.Cells[4, 4, numRows + 3, 5].Calculate();
                    //Total Cost
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 3])//
                    {
                        //Total Cost Text
                        workSheet.Cells[numRows + 4, 2].Value = "Total Cost:";
                        workSheet.Cells[numRows + 4, 2].Style.Font.Bold = true;
                        workSheet.Cells[numRows + 4, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        //Total Cost Sum - get cost * qty for each row
                        totalfees.Formula = "Sum(" + (workSheet.Cells[4, 3].Address) + ":" + workSheet.Cells[numRows + 3, 3].Address + ")" + "*" + "Sum(" +
                            (workSheet.Cells[4, 4].Address) + ":" + workSheet.Cells[numRows + 3, 4].Address + ")";
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,###,##0.00";
                        var range = workSheet.Cells[numRows + 4, 3, numRows + 4, 4];
                        range.Merge = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 5])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Costs Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 5])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 5])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel

                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryCostsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        //Method for Viewing Inventory Events Report
        public async Task<IActionResult> InventoryEventsReport(string SearchString, int?[] LocationID, int? page, int? pageSizeID, string actionButton,
            string sortDirection = "asc", string sortField = "EventName")
        {
            //For the Report View
            var sumQ = from i in _context.EventLogs
                       orderby i.EventName, i.ItemName ascending
                       select new EventReportVM
                       {
                           Id = i.Id,
                           EventName = i.EventName,
                           ItemName = i.ItemName,
                           Quantity = i.Quantity
                       };

            //var sumQ = from i in _context.ItemReservations
            //            .Include(i => i.Event)
            //            .Include(i => i.Item)
            //           orderby i.Event.Name, i.Item.Name ascending
            //           select new EventReportVM
            //           {
            //               Id = i.EventId,
            //               EventName = i.Event.Name,
            //               ItemName = i.Item.Name,
            //               Quantity = i.Quantity
            //           };

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering

            ////Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            //ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "EventName", "ItemName", "Quantity", "LogDate" };

            ////Add as many filters as needed
            //if (LocationID.Length > 0)
            //{
            //    sumQ = sumQ.Where(p => LocationID.Contains(p.LocationID));
            //    ViewData["Filtering"] = "btn-danger";
            //}

            if (!String.IsNullOrEmpty(SearchString))
            {
                sumQ = sumQ.Where(p => p.ItemName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.EventName.ToUpper().Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
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
            if (sortField == "ItemName")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            else if (sortField == "Quantity")
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.Quantity);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.Quantity);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    sumQ = sumQ
                        .OrderBy(p => p.ItemName);
                }
                else
                {
                    sumQ = sumQ
                        .OrderByDescending(p => p.ItemName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryEventsReport");//Remember for this View
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<EventReportVM>.CreateAsync(sumQ.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        //Method for Excel Inventory Events Report
        public ActionResult DownloadInventoryEvents()
        {
            //Get the inventory
            var items = from i in _context.EventLogs
                        orderby i.EventName, i.ItemName ascending
                        select new
                        {
                            EventName = i.EventName,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity
                        };
            //How many rows?
            int numRows = items.Count();

            if (numRows > 0) //We have data
            {
                //Create a new spreadsheet from scratch.
                using (ExcelPackage excel = new ExcelPackage())
                {
                    var workSheet = excel.Workbook.Worksheets.Add("Event Items");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style fee column for quantity
                    workSheet.Column(3).Style.Numberformat.Format = "###,###,##0";

                    ////Style fee column for currency
                    //workSheet.Column(3).Style.Numberformat.Format = "$###,###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Event Name Bold
                    workSheet.Cells[4, 1, numRows + 3, 1].Style.Font.Bold = true;

                    //Make Item Quantity Bold/Colour coded
                    workSheet.Cells[4, 3, numRows + 3, 3].Style.Font.Bold = true;
                    var item = from i in _context.EventLogs
                               orderby i.EventName, i.ItemName ascending
                               select i.Quantity;
                    int row = 4;
                    foreach (var qty in item)
                    {
                        if (row <= (numRows + 3))
                        {
                            if (qty == 0)
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Red);
                                row++;
                            }
                            else if ((qty <= 10) && (qty > 0))
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Orange);
                                row++;
                            }
                            else if (qty > 10)
                            {
                                workSheet.Cells[row, 3].Style.Font.Color.SetColor(Color.Green);
                                row++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 3])
                    {
                        headings.Style.Font.Bold = true;
                        var fill = headings.Style.Fill;
                        fill.PatternType = ExcelFillStyle.Solid;
                        fill.BackgroundColor.SetColor(Color.LightBlue);
                    }

                    //Autofit columns
                    workSheet.Cells.AutoFitColumns();
                    //Note: You can manually set width of columns as well
                    //workSheet.Column(7).Width = 10;

                    //Add a title and timestamp at the top of the report
                    workSheet.Cells[1, 1].Value = "Inventory Events Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 3])
                    {
                        Rng.Merge = true; //Merge columns start and end range
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 18;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //Since the time zone where the server is running can be different, adjust to 
                    //Local for us.
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);
                    using (ExcelRange Rng = workSheet.Cells[2, 3])
                    {
                        Rng.Value = "Created: " + localDate.ToShortTimeString() + " on " +
                            localDate.ToShortDateString();
                        Rng.Style.Font.Bold = true; //Font should be bold
                        Rng.Style.Font.Size = 12;
                        Rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    //Ok, time to download the Excel
                    try
                    {
                        Byte[] theData = excel.GetAsByteArray();
                        string filename = "InventoryEventsReport.xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(theData, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not build and download the file.");
                    }
                }
            }
            return NotFound("No data.");
        }

        public async Task<IActionResult> SilencingToastrNottifPopup(int id)
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
            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 day" },
                new SelectListItem { Value = "2", Text = "2 days" },
                new SelectListItem { Value = "3", Text = "3 days" },
                new SelectListItem { Value = "7", Text = "1 week" },
                new SelectListItem { Value = "0", Text = "Permanently" }
            };

            ViewData["SilentID"] = new SelectList(options, "Value", "Text");

            return View(inventory);
        }


        [HttpPost]
        public async Task<IActionResult> SilencingToastrNottifPopup(int id, Byte[] RowVersion, string SilentID)
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

            try
            {
                int days = 0;
                // Use the selected value in your code
                if (!string.IsNullOrEmpty(SilentID))
                {
                    if (SilentID == "1") { days = 1; }
                    else if (SilentID == "2") { days = 2; }
                    else if (SilentID == "3") { days = 3; }
                    else if (SilentID == "7") { days = 7; }
                    else if (SilentID == "0")
                    {
                        days = 0;

                    }

                }
                if (days <= 0)
                { inventoryToUpdate.DismissNotification = null; }
                else
                {
                    inventoryToUpdate.DismissNotification = null;
                    inventoryToUpdate.DismissNotification = DateTime.Now.AddDays(days);
                }
                _context.Update(inventoryToUpdate);
                _context.SaveChanges();

                _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
            ViewData["ItemID"] = new SelectList(_context.Items, "ID", "Name", inventoryToUpdate.ItemID);
            var options = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 day" },
                new SelectListItem { Value = "2", Text = "2 days" },
                new SelectListItem { Value = "3", Text = "3 days" },
                new SelectListItem { Value = "7", Text = "1 week" },
                new SelectListItem { Value = "0", Text = "Permanently" }
            };

            ViewData["SilentID"] = new SelectList(options, "Value", "Text");

            TempData["SilenceNotifMessageBool"] = "true";
            return RedirectToAction("Index", "Inventories");
        }

        public async Task<IActionResult> RecoveringToastrNottifPopup(int id)
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


            return View(inventory);
        }


        [HttpPost]
        public async Task<IActionResult> RecoveringToastrNottifPopup(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the Event to update
            var inventoryToUpdate = await _context.Inventories.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryToUpdate == null)
            {
                return NotFound();
            }


            try
            {
                inventoryToUpdate.DismissNotification = DateTime.Now;

                _context.Update(inventoryToUpdate);
                _context.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
            TempData["RecoverNotifMessageBool"] = "true";
            return RedirectToAction("Index", "Items");
        }


        //Method for Viewing Inventory Report
        public async Task<IActionResult> RecoverAllSilencedNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int records = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            try
            {

                foreach (var inventory in inventories)
                {
                    if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                    {
                        if (inventory.DismissNotification >= DateTime.Now || inventory.DismissNotification == null)
                        {
                            records += 1;
                        }
                    }
                    inventory.DismissNotification = DateTime.Now;
                    _context.Update(inventory);
                    _context.SaveChanges();
                }
                if (records == 0)
                {
                    _toastNotification.AddSuccessToastMessage(
                                $@"No Messages To Recover");
                }
                else if (records > 0)
                {
                    _toastNotification.AddSuccessToastMessage(
                                $@"{records} Record(s) Recovered");

                }


            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Index", "Items");
        }

        //Method for Viewing Silenced Messages
        public async Task<IActionResult> ViewSilencedNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int records = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            try
            {
                foreach (var inventory in inventories)
                {
                    if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                    {
                        if (inventory.DismissNotification >= DateTime.Now || inventory.DismissNotification == null)
                        {
                            
                            DateTime inventoryDismissNotification = inventory.DismissNotification ?? DateTime.MinValue; // Use DateTime.MinValue as a default value if inventory.DismissNotification is null
                            TimeSpan timeDifference = inventoryDismissNotification - DateTime.Now;
                            int daysApart = timeDifference.Days;
                            if (timeDifference.Days == 0)
                            {
                                daysApart = 1;
                            }
                            else if (timeDifference.Days < 0)
                            {
                                daysApart = 0;
                            }


                            if (timeDifference.Days >= 0)
                            {
                                records++;
                                _toastNotification.AddInfoToastMessage(
                                $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a> <br>***Silenced {daysApart} day(s) left***
                                    
                                    <button class='btn btn-outline-secondary' id='nowEditActivateNotif' data-bs-toggle='modal' data-bs-target='#addNotifActivateModal' type='button'
                                    onclick='setItemIdForPartialNotifActivate({inventory.Item.ID})'>
                                        <strong>Recover Message</strong>
                                    </button> 

                                    ");
                            }
                            else
                            {
                                records++;
                                _toastNotification.AddInfoToastMessage(
                                $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                    <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a> <br>***Silenced Permanantly***
                                    
                               
                                    <button class='btn btn-outline-secondary' id='nowEditActivateNotifNull' data-bs-toggle='modal' data-bs-target='#addNotifActivateModal' type='button'
                                    onclick='setItemIdForPartialNotifActivate({inventory.Item.ID})'>
                                        <strong>Recover Message</strong>
                                    </button> 

                                    ");
                            }


                        }

                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
            if (records == 0)
            {
                _toastNotification.AddSuccessToastMessage(
            $@"No Silent Messages At This Time");
            }
            return RedirectToAction("Index", "Items");
        }

        //Method for Viewing Silenced Messages
        public async Task<IActionResult> ViewActiveNotif()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            int itemID = 0;
            int norecords = 0;
            var inventories = _context.Inventories.Include(i => i.Location).Include(i => i.Item).ThenInclude(i => i.Category)
                .Include(i => i.Item.ItemLocations).ThenInclude(i => i.Location)
                .Where(t => t.Item.Archived == false).ToList();
            foreach (var inventory in inventories)
            {

                if (inventory.Quantity <= inventory.Item.Category.LowCategoryThreshold)
                {
                    if (inventory.DismissNotification <= DateTime.Now && inventory.DismissNotification != null)
                    {
                        itemID = inventory.ItemID;
                        HttpContext.Session.SetString("ItemIdForPartialNotif" + itemID, inventory.ItemID.ToString());
                        norecords += 1;
                        inventory.IsLowInventory = true;
                        _toastNotification.AddInfoToastMessage(
                            $@"Inventory for {inventory.Item.Name} at location {inventory.Location.Name} is running low. Current quantity: {inventory.Quantity}
                                <a href='#' onclick='redirectToEdit({inventory.Item.ID}); return false;'>Edit</a>
                                <br><br>Qiuck Actions:
                                
                                <button class='btn btn-outline-secondary' id='nowEditNotifSilent' data-bs-toggle='modal' data-bs-target='#addNotifModal' type='button'
                                    onclick='setItemIdForPartialNotif({inventory.Item.ID})'>
                                    <strong>Silence Message</strong>
                                </button>");

                    }

                }
                else
                {

                }
            }

            if (norecords == 0)
            {
                _toastNotification.AddSuccessToastMessage(
                            $@"All Caught Up!");
            }
            return RedirectToAction("Index", "Items");
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
