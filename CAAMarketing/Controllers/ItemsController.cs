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
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Drawing;
using NToastNotify;
using CAAMarketing.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;

namespace CAAMarketing.Controllers
{
    public class ItemsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public ItemsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
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
            //var inventories = _context.Inventories
            //    .Include(i => i.Item.ItemThumbNail)
            //    .Include(i => i.Item.Employee)
            //    .Include(i => i.Location)
            //    .Include(i => i.Item).ThenInclude(i => i.Category)
            //    .Include(i=>i.Item.ItemLocations).ThenInclude(i=>i.Location)
            //.AsNoTracking();


            //inventories = inventories.Where(p => p.Item.Archived == false);
            //CheckInventoryLevel(inventories.ToList());

            var inventories = _context.Items
                .Include(p => p.Inventories).ThenInclude(p => p.Location)
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(p => p.ItemThumbNail)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .AsNoTracking();

            inventories = inventories.Where(p => p.Archived == false);


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Location
            ViewData["LocationID"] = new SelectList(_context.Locations, "Id", "Name");




            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Location", "UPC", "Quantity", "Cost" };

            //Add as many filters as needed
            if (LocationID.Length > 0)
            {
                inventories = inventories.Where(p => p.Inventories.Any(i => i.LocationID.Equals(LocationID)));

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
                inventories = inventories.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.UPC.Contains(SearchString.ToUpper()));
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
            //            .OrderBy(p => p.Cost);
            //    }
            //    else
            //    {
            //        inventories = inventories
            //            .OrderByDescending(p => p.Cost);
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
                        .OrderBy(p => p.UPC);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.UPC);
                }
            }
            else if (sortField == "Location")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories.OrderBy(p => p.Inventories.FirstOrDefault().Location.Name);

                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Inventories.FirstOrDefault().Location.Name);
                }
            }
            else //Sorting by Item Name
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Inventories");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Item>.CreateAsync(inventories.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Items/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i => i.Employee)
                .Include(p => p.ItemImages)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // GET: Items/Create
        public IActionResult Create()
        {
            //Add all (unchecked) options for the locations
            var item = new Item();
            PopulateAssignedLocationData(item);

            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Item, You Can Cancel By Clicking The Exit Button.");
            


            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name");
            return View();
        }

        // POST: Items/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,Notes,CategoryID,UPC,DateReceived,SupplierID")] Item item, Inventory inventory, IFormFile thePicture, string[] selectedOptions)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();


            try
            {
                ////Add the selected conditions
                //if (selectedOptions != null)
                //{
                //    foreach (var location in selectedOptions)
                //    {
                //        var locationToAdd = new ItemLocation { ItemID = item.ID, LocationID = int.Parse(location) };
                //        item.ItemLocations.Add(locationToAdd);
                //    }
                //}
                //else
                //{
                //    _toastNotification.AddErrorToastMessage($"Oops, you have selected any locations for this item. Please Select one or more locations ");
                //}

                var email = User.Identity.Name;

                var employee = _context.Employees.FirstOrDefault(e => e.Email == email);
                item.EmployeeID = employee.ID;
                item.DateReceived = DateTime.Now;


                //item.EmployeeID = 1;
                _context.Add(item);
                await AddPicture(item, thePicture);
                await _context.SaveChangesAsync();
                _context.SaveChanges();




                //FOR THE INVENTORY ADD

                int locationCount = 0;
                string LocationName = "";


                //var selectedOptionsHS = new HashSet<string>(selectedOptions);
                //var inventories = _context.Inventories
                //    .Where(p => p.ItemID == item.ID && p.Item.Archived == false)
                //    .ToArray();



                //foreach (var locationoption in _context.Locations)
                //{
                //    if (selectedOptionsHS.Contains(locationoption.Id.ToString())) //It is checked
                //    {
                //        HttpContext.Session.SetString("IfStatement", "I made it past this if statement");
                //        //foreach (var inv in inventories)
                //        //{
                //        //if (!inv.LocationID.Equals(locationoption.Id))  //but not currently in the history
                //        // {
                //        locationCount++;
                //                var location = _context.Locations.FirstOrDefault(c => c.Id == locationoption.Id);
                //                if (location != null)
                //                {
                //                    LocationName += location.Name + ", ";
                //                }
                //            var quantity = int.Parse(Request.Form[$"quantity_{locationoption.Id}"]);
                //            Inventory invCreate = new Inventory();

                //                invCreate.LocationID = locationoption.Id;
                //                invCreate.ItemID = item.ID;
                //                invCreate.Quantity = quantity;
                //                _context.Add(invCreate);

                //                _context.SaveChanges();


                //           // }
                //       // }
                //    }
                //}
                //HttpContext.Session.SetString("LocationNames", LocationName);
                //HttpContext.Session.SetString("NumOfLocationsSelected", locationCount.ToString());


                ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
                ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);


                // Get the value of MySessionVariable from the session state
                HttpContext.Session.SetString("GetItemIDForSkipOrder", item.ID.ToString());


            }
            catch (DbUpdateException  dex )
            {
                if(dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Items.UPC"))
                {
                    ModelState.AddModelError("UPC", "Unable to save changes. You can have duplicate UPC's");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, looks like you have a duplicate UPC number with another item. Please enter a different UPC");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later");
                }
                
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later. (Database rolled back record 5+ times).");
            }

            //return RedirectToAction(nameof(Index));
            //return RedirectToAction("Details", new { item.ID });

            //PopulateAssignedLocationData(item);
            return RedirectToAction("Create", "Receiving", new { id = item.ID });


        
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Get the URL with the last filter, sort and page parameters from THE itemS Index View
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Inventories");


            if (!id.HasValue)
            {
                //Go back to the proper return URL for the items controller
                return Redirect(ViewData["returnURL"].ToString());
            }

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(p => p.ItemImages)
                //.Include(p=>p.Supplier)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (item == null)
            {
                return NotFound();
            }

            // Go get the item to update
            var inventoryToUpdate = await _context.Inventories
                .Include(i => i.Location)
                .Include(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(p => p.ItemID == id);

            var locationQuantities = new Dictionary<int, int>();
            string editedlocations = "";
            int count = 10;  
            foreach (var inventoryLocation in inventoryToUpdate.Item.Inventories)
            {
                locationQuantities[inventoryLocation.LocationID] = inventoryLocation.Quantity;
                editedlocations += inventoryLocation.Location.Name + ", ";
                count++;

            }
            HttpContext.Session.SetString("EditedLocations", editedlocations);
            HttpContext.Session.SetString("EditCount", count.ToString());

            ViewBag.LocationQuantities = locationQuantities;


            //item.Quantity = inventoryToUpdate.Quantity;
            //item.Cost = inventoryToUpdate.Cost;

            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);
            PopulateAssignedLocationData(inventoryToUpdate.Item);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string chkRemoveImage, IFormFile thePicture,
            string InvCost, string InvQty, string[] selectedOptions)
        {
            //Get the URL with the last filter, sort and page parameters from THE itemS Index View
            ViewDataReturnURL();


            //Go get the item to update
            var itemToUpdate = await _context.Items
                .Include(p => p.ItemImages)

                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(p => p.ID == id);





            //Check that you got it or exit with a not found error
            if (itemToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(itemToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            UpdateItemLocations(selectedOptions, itemToUpdate);


            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Item>(itemToUpdate, "",
                p => p.Name, p => p.Description, p => p.Notes, p => p.CategoryID, p => p.UPC, p => p.Cost, p => p.Quantity,
                p => p.DateReceived, p => p.SupplierID))
            {
                try
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ItemID == itemToUpdate.ID);
                    if (inventory != null)
                    {
                        inventory.Quantity = itemToUpdate.Quantity;
                        inventory.Cost = itemToUpdate.Cost;

                        _context.Update(inventory);
                        await _context.SaveChangesAsync();


                        var email = User.Identity.Name;

                        var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

                        itemToUpdate.EmployeeNameUser = employee.FullName;


                        //inventoryToUpdate.Cost = Convert.ToDecimal(InvCost);
                        //inventoryToUpdate.Quantity = Convert.ToInt32(InvQty);



                        //For the image
                        if (chkRemoveImage != null)
                        {
                            //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                            //about them both so go get the Thumbnail since we did not include it.
                            itemToUpdate.ItemThumbNail = _context.ItemThumbNails.Where(p => p.ItemID == itemToUpdate.ID).FirstOrDefault();
                            //Then, setting them to null will cause them to be deleted from the database.
                            itemToUpdate.ItemImages = null;
                            itemToUpdate.ItemThumbNail = null;
                        }
                        else
                        {
                            await AddPicture(itemToUpdate, thePicture);
                        }

                        await _context.SaveChangesAsync();

                        //_context.Add(inventoryToUpdate);
                        //_context.SaveChanges();
                        // return RedirectToAction(nameof(Index));
                       return RedirectToAction("Index", "OrderItems", new { ItemID = itemToUpdate.ID });

                    }
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                    _toastNotification.AddErrorToastMessage("There was an issue saving to the database, Please try again later. (Database rolled back record 5+ times).");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(itemToUpdate.ID))
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
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                    {
                        ModelState.AddModelError("UPC", "Unable to save changes. Remember, you cannot have duplicate UPC numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", itemToUpdate.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", itemToUpdate.SupplierID);
            return View(itemToUpdate);
        }
        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i=>i.Employee)
                .Include(i => i.ItemLocations).ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Items == null)
            {
                return Problem("Entity set 'CAAContext.Items'  is null.");
            }
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            try
            {
                //_context.Add(archive);
                //_context.Items.Remove(item);
                item.Archived = true;
                await _context.SaveChangesAsync();
                // return RedirectToAction(nameof(Index));
                //return Redirect(ViewData["returnURL"].ToString());
                return RedirectToAction("Index", "Inventories");

            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(item);

        }

        //For Adding Supplier
        [HttpGet]
        public JsonResult GetSuppliers(int? id)
        {
            return Json(SupplierSelectList(id));
        }
        //For Adding Category
        [HttpGet]
        public JsonResult GetCategories(int? id)
        {
            return Json(CategorySelectList(id));
        }
        //For Adding Supplier
        private SelectList SupplierSelectList(int? selectedId)
        {
            return new SelectList(_context
                .Suppliers
                .OrderBy(c => c.Name), "ID", "Name", selectedId);
        }
        //For Adding Category
        private SelectList CategorySelectList(int? selectedId)
        {
            return new SelectList(_context
                .Categories
                .OrderBy(c=>c.Name), "Id", "Name", selectedId);
        }


        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ID == id);
        }
        private async Task AddPicture(Item item, IFormFile thePicture)
        {
            //Get the picture and save it with the item (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (item.ItemImages != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            item.ItemImages.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            item.ItemThumbNail = _context.ItemThumbNails.Where(p => p.ItemID == item.ID).FirstOrDefault();
                            item.ItemThumbNail.Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120);
                        }
                        else //No pictures saved so start new
                        {
                            item.ItemImages = new ItemImages
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            item.ItemThumbNail = new ItemThumbNail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }

        private void PopulateAssignedLocationData(Item item)
        {
            //For this to work, you must have Included the itemLocations 
            //in the item
            var allOptions = _context.Locations;
            var currentOptionIDs = new HashSet<int>(item.ItemLocations.Select(b => b.LocationID));
            var checkBoxes = new List<CheckOptionsManyToManyVM>();
            foreach (var locationoption in allOptions)
            {
                checkBoxes.Add(new CheckOptionsManyToManyVM
                {
                    ID = locationoption.Id,
                    DisplayText = locationoption.Name,
                    Assigned = currentOptionIDs.Contains(locationoption.Id)
                });
            }
            ViewData["LocationOptions"] = checkBoxes;
        }
        private void UpdateItemLocations(string[] selectedOptions, Item itemToUpdate)
        {
            int locationCount = 0;
            string LocationName = "";
            if (selectedOptions == null)
            {
                itemToUpdate.ItemLocations = new List<ItemLocation>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var itemOptionsHS = new HashSet<int>
                (itemToUpdate.ItemLocations.Select(c => c.LocationID));//IDs of the currently selected Locations
            foreach (var locationoption in _context.Locations)
            {
                if (selectedOptionsHS.Contains(locationoption.Id.ToString())) //It is checked
                {
                    if (!itemOptionsHS.Contains(locationoption.Id))  //but not currently in the history
                    {
                        locationCount++;
                        var location = _context.Locations.FirstOrDefault(c => c.Id == locationoption.Id);
                        if (location != null)
                        {
                            LocationName += location.Name + ", ";
                        }
                        itemToUpdate.ItemLocations.Add(new ItemLocation { ItemID = itemToUpdate.ID, LocationID = locationoption.Id });
                    }
                }
                else
                {
                    //Checkbox Not checked
                    if (itemOptionsHS.Contains(locationoption.Id)) //but it is currently in the history - so remove it
                    {
                        ItemLocation conditionToRemove = itemToUpdate.ItemLocations.SingleOrDefault(c => c.LocationID == locationoption.Id);
                        _context.Remove(conditionToRemove);
                    }
                }

            }
            HttpContext.Session.SetString("LocationNames", LocationName);
            HttpContext.Session.SetString("NumOfLocationsSelected", locationCount.ToString());

        }

        private void CreatingInventoryLocations(string[] selectedOptions, Inventory invToCreate, int ItemID)
        {
            

        }






    }
}
