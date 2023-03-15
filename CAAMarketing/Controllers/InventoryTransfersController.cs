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
using Microsoft.AspNetCore.Authorization;
using NToastNotify;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class InventoryTransfersController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public InventoryTransfersController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: InventoryTransfers
        public async Task<IActionResult> Index(string SearchString, int? FromLocationID, int? ToLocationId, int? ItemID,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "ItemTransfered")
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
                ViewData["Filtering"] = "btn-danger";
            }
            if (ToLocationId.HasValue)
            {
                transfers = transfers.Where(p => p.ToLocationId == ToLocationId);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                long searchUPC;
                bool isNumeric = long.TryParse(SearchString, out searchUPC);

                transfers = transfers.Where(p => p.Item.Name.ToUpper().Contains(SearchString.ToUpper())
                                                 || (isNumeric && p.Item.UPC == searchUPC));
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

            Item item = _context.Items
               .Include(i => i.Category)
               .Include(i => i.Supplier)
               .Include(i => i.Employee)
               .Include(p => p.ItemThumbNail)
               .Where(p => p.ID == ItemID.GetValueOrDefault())
               .AsNoTracking()
               .FirstOrDefault();

            Inventory inventory = _context.Inventories
                 .Where(p => p.ItemID == ItemID.GetValueOrDefault())
                 .FirstOrDefault();



            ViewBag.Item = item;
            ViewBag.Inventory = inventory;



            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "InventoryTransfers");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<InventoryTransfer>.CreateAsync(transfers.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: InventoryTransfers/Create
        public IActionResult Create(int? ItemId, int FromLocationId, int ToLocationId, DateTime TransferDate)
        {

            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Transfer, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (!ItemId.HasValue)
            {
                return Redirect(ViewData["returnURL"].ToString());
            }
            ViewData["ItemName"] = ItemId;
            InventoryTransfer a = new InventoryTransfer()
            {
                ItemId = ItemId.GetValueOrDefault()
            };


            var quantity = 0;
            // Get the quantity value from TempData
            


            var existingLocationIds = _context.Inventories.Where(i => i.ItemID == ItemId).Select(i => i.LocationID).Distinct().ToList();
            var locations = _context.Locations.Where(l => existingLocationIds.Contains(l.Id)).ToList();
            var GetTotalStock = _context.Items
            .Where(i => i.ID == ItemId)
            .SelectMany(i => i.Inventories)
            .ToList();
            TempData["numOfLocations"] = locations.Count();
            foreach (var loc in locations)
            {
                var tempname = _context.Locations.Where(i=>i.Id == loc.Id).Select(i=>i.Name).First();
                quantity = 0;
                quantity += GetTotalStock
                    .Where(i => i.LocationID == loc.Id)
                    .Sum(i => i.Quantity);
                TempData[loc.Id.ToString()] = quantity;
            }

            ViewData["FromLocationId"] = new SelectList(locations, "Id", "Name", FromLocationId);

            ViewData["ItemId"] = new SelectList(_context.Items, "ID", "Name", ItemId);
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name", ToLocationId);


            return View(a);
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
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.FromLocationId);

                if (fromInventory.Quantity < inventoryTransfer.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");
                    return View(inventoryTransfer);
                }

                // Update the from location to the current location of the inventory item
                inventoryTransfer.FromLocationId = fromInventory.LocationID;

                // Update the inventory quantity at the from location
                fromInventory.Quantity -= inventoryTransfer.Quantity;
                _context.Update(fromInventory);

                // Find the inventory record for the item being transferred to the specified location
                var toInventory = await _context.Inventories
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.ToLocationId);

                if (toInventory == null)
                {
                    // Create a new inventory record if one doesn't exist at the to location
                    toInventory = new Inventory
                    {
                        ItemID = inventoryTransfer.ItemId,
                        LocationID = inventoryTransfer.ToLocationId,
                        Quantity = inventoryTransfer.Quantity,
                        Cost = fromInventory.Cost
                    };
                    _context.Add(toInventory);
                }
                else
                {
                    // Update the inventory quantity at the to location
                    toInventory.Quantity += inventoryTransfer.Quantity;
                    _context.Update(toInventory);
                }

                // Update the to location
                inventoryTransfer.ToLocationId = toInventory.LocationID;

                // Save changes to the database
                await _context.AddAsync(inventoryTransfer);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "OrderItems", new { inventoryTransfer.ItemId });
            }

            // If ModelState is invalid, return the view with the input inventoryTransfer
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ItemId,FromLocationId,ToLocationId,Quantity,TransferDate")] InventoryTransfer inventoryTransfer, Byte[] RowVersion)
        {
            // Get the URL with the last filter, sort, and page parameters for this controller
            ViewDataReturnURL();

            // Get the InventoryTransfer to update
            var transferToUpdate = await _context.InventoryTransfers.FirstOrDefaultAsync(t => t.Id == id);

            if (transferToUpdate == null)
            {
                return NotFound();
            }

            // Set the original RowVersion value for the entity
            _context.Entry(transferToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            // Find the inventory record for the item being transferred from the specified location
            var fromInventory = await _context.Inventories
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.FromLocationId);

            if (fromInventory.Quantity < inventoryTransfer.Quantity)
            {
                ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");
                
            }

            // Find the inventory record for the item being transferred to the specified location
            var toInventory = await _context.Inventories
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.ItemID == inventoryTransfer.ItemId && i.LocationID == inventoryTransfer.ToLocationId);

            // Calculate the difference in inventory quantities
            int quantityDifference = inventoryTransfer.Quantity - transferToUpdate.Quantity;

            // Update the from location inventory
            fromInventory.Quantity -= quantityDifference;
            _context.Update(fromInventory);

            if (toInventory == null)
            {
                // Create a new inventory record if one doesn't exist at the to location
                toInventory = new Inventory
                {
                    ItemID = inventoryTransfer.ItemId,
                    LocationID = inventoryTransfer.ToLocationId,
                    Quantity = inventoryTransfer.Quantity,
                    Cost = fromInventory.Cost
                };
                _context.Add(toInventory);
            }
            else
            {
                // Update the inventory quantity at the to location
                toInventory.Quantity += quantityDifference;
                _context.Update(toInventory);
            }

            // Update the InventoryTransfer with the values posted
            if (await TryUpdateModelAsync<InventoryTransfer>(transferToUpdate, "",
                t => t.ItemId, t => t.FromLocationId, t => t.ToLocationId, t => t.Quantity, t => t.TransferDate))
            {
                try
                {
                    // Save changes to the database
                    await _context.SaveChangesAsync();

                    // Redirect to the updated InventoryTransfer's details page
                    return RedirectToAction("Index", "OrderItems", new { transferToUpdate.ItemId });
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
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Please contact your system administrator.");
                }
            }

            // If the update was unsuccessful, populate ViewData with the original values and return to the Edit view
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



        // GET: SelectItems
        public async Task<IActionResult> SelectItems(string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Item")
        {

            ViewDataReturnURL();

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);


            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;




            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Date", "Location" };


            int toLocationId = Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer"));


            var items = _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.Inventories.Count(inv => inv.LocationID == toLocationId) < i.Inventories.Count)
                .AsNoTracking();




            if (!String.IsNullOrEmpty(SearchString))
            {
                items = items.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));
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

            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            var SelectedItems = await _context.Items.Include(i => i.Supplier).Include(i => i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
            .ToListAsync();

            ViewBag.SelectedItems = SelectedItems;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Items");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Item>.CreateAsync(items.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }




        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> SelectItems(int ItemID)
        {
            if (ModelState.IsValid)
            {
                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent = true;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "Events");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> RemoveSelectedItems(int ItemID)
        {
            if (ModelState.IsValid)
            {

                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent = false;
                _context.Update(itemsupdate);
                _context.SaveChanges();

                //_context.SaveChanges();

                return RedirectToAction("SelectItems", "Events");
            }
            else
            {
                // Return a validation error if the model is invalid
                _toastNotification.AddErrorToastMessage($"Oops! There was an issue saving the record, please check your input and try again, if the problem continues, try again later.");
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return View();
            }
        }



        // GET: SelectItems
        public async Task<IActionResult> ChooseItemQuantities()
        {

            ViewDataReturnURL();

            var events = _context.Items.Include(i => i.Supplier)
                .AsNoTracking();

            var SelectedItems = await _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .Where(i => i.isSlectedForEvent == true)
            .ToListAsync();


            return View(SelectedItems);
            

        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ChooseItemQuantities(int id)
        {
            string output = "";
            bool flag = false;
            foreach (var item in _context.Items)
            {
                if (Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                {
                    //Getting the quantity of the item and location selected
                    int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                    //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                    int fromlocationID = int.Parse(Request.Form["locationId" + item.ID.ToString()]);
                    //Getting the Name of the location they selected by id
                    var location = _context.Locations
                        .Where(i => i.Id == fromlocationID)
                        .FirstOrDefault();

                    //Outputted a message to see if my logic worked, and It Did!
                    output += "Name: " + item.Name.ToString() + ", Location: " + location.Name + ", Qty: " + Quantity + "\n";

                    
                    //// Update the inventory quantity
                    var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                        .Where(i => i.ItemID == item.ID && i.LocationID == fromlocationID)
                        .FirstOrDefaultAsync();


                    //// Find the inventory record for the item being transferred from the specified location
                    var fromInventory = await _context.Inventories
                        .Include(i => i.Location)
                       .FirstOrDefaultAsync(i => i.ItemID == item.ID && i.LocationID == fromlocationID);

                    if (fromInventory.Quantity < Quantity)
                    {
                        ModelState.AddModelError("Quantity", "Not enough inventory to transfer.");
                        flag = true;
                       _toastNotification.AddErrorToastMessage($"Oops, You entered a quantity that exceeds the stock of {fromInventory.Item.Name} At {fromInventory.Location.Name}, Please enter a valid Quantity that is under {fromInventory.Quantity}");
                        
                    }
                    else
                    {
                        // Update the from location to the current location of the inventory item
                        //inventoryTransfer.FromLocationId = fromInventory.LocationID;

                        // Update the inventory quantity at the from location
                        fromInventory.Quantity -= Quantity;
                        _context.Update(fromInventory);

                        // Find the inventory record for the item being transferred to the specified location
                        var toInventory = await _context.Inventories
                            .Include(i => i.Location)
                            .FirstOrDefaultAsync(i => i.ItemID == item.ID && i.LocationID == Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer")));

                        if (toInventory == null)
                        {
                            // Create a new inventory record if one doesn't exist at the to location
                            toInventory = new Inventory
                            {
                                ItemID = item.ID,
                                LocationID = Convert.ToInt32(HttpContext.Session.GetString("ToLocationForTransfer")),
                                Quantity = Quantity,
                                Cost = fromInventory.Cost
                            };
                            _context.Add(toInventory);
                           }
                        else
                        {
                            // Update the inventory quantity at the to location
                            toInventory.Quantity += Quantity;
                            _context.Update(toInventory);
                        }
                        string transferDateString = HttpContext.Session.GetString("TransferDateForTransfer");



                        var inventoryTransfer = new InventoryTransfer();
                        inventoryTransfer.ItemId = item.ID;
                        inventoryTransfer.Quantity = Quantity;
                        inventoryTransfer.ToLocationId = toInventory.LocationID;
                        inventoryTransfer.FromLocationId = fromlocationID;
                        //inventoryTransfer.TransferDate = DateTime.Today;
                        if (DateTime.TryParse(transferDateString, out DateTime transferDate))
                        {
                            inventoryTransfer.TransferDate = transferDate;
                        }
                        else
                        {
                            // handle the case where the string is not a valid date
                            _toastNotification.AddErrorToastMessage($"The Transfer Date Is Invalid...");
                        }

                        // Save changes to the database
                        await _context.AddAsync(inventoryTransfer);
                        await _context.SaveChangesAsync();
                    }


                }
            }

            //Means there wasnt any errors and all the create statements were added
            if (flag == false)
            {
                foreach (var item in _context.Items)
                {
                    item.isSlectedForEvent = false;
                    _context.Update(item);

                }
                _context.SaveChanges();
            }

            //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");
            _toastNotification.AddSuccessToastMessage("Item Transfer Created! You can view them all in this index.");
            //_toastNotification.AddSuccessToastMessage($"{output}");
             return RedirectToAction("Index", "InventoryTransfers");


            //return RedirectToAction("ChooseItemQuantities", "InventoryTransfers");
        }


        // GET: Events/Create
        public IActionResult CreateMultipleTransfers()
        {
            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Event, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");


            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> CreateMultipleTransfers(InventoryTransfer model)
        {
            ViewData["ToLocationId"] = new SelectList(_context.Locations, "Id", "Name");
            _toastNotification.AddErrorToastMessage($"ToLocation: {model.ToLocationId}");
            _toastNotification.AddErrorToastMessage($"Transfer Date: {model.TransferDate}");

            var location = _context.Locations
                .Where(i => i.Id == model.ToLocationId)
                .FirstOrDefault();


            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("ToLocationNameForTransfer", location.Name);

            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("ToLocationForTransfer", model.ToLocationId.ToString());

            // Get the value of MySessionVariable from the session state
            HttpContext.Session.SetString("TransferDateForTransfer", model.TransferDate.ToString());




            foreach (var item in _context.Items)
            {
                item.isSlectedForEvent = false;
                _context.Update(item);

            }
            _context.SaveChanges();
            return RedirectToAction("SelectItems", "InventoryTransfers");
            
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
