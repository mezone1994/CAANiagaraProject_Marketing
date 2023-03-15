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
using CAAMarketing.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace CAAMarketing.Controllers
{
    public class EventsController : Controller
    {
        private readonly CAAContext _context;
        private readonly IToastNotification _toastNotification;

        public EventsController(CAAContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        // GET: Events
        public async Task<IActionResult> Index(string SearchString, int? LocationID, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Event")
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




            // List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Event", "Date", "Location" };


            var events = _context.Events
                .Include(i=>i.ItemReservations).ThenInclude(i=>i.Item)
                .Include(i => i.ItemReservations).ThenInclude(i => i.Location)
                .AsNoTracking();

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
                        .OrderBy(p => p.location);
                }
                else
                {
                    events = events
                        .OrderByDescending(p => p.location);
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

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Events");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Event>.CreateAsync(events.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
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
            _toastNotification.AddAlertToastMessage($"Please Start By Entering Information Of The Event, You Can Cancel By Clicking The Exit Button.");

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create(Event model)
        {
            if (ModelState.IsValid)
            {
                // Add the new event to the database
                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("EventID", model.ID);


                return RedirectToAction("SelectItems", "Events", new { id = model.ID });
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

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Events == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

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
                e => e.Name, e => e.Description, e => e.Date, e => e.location, e => e.ReservedEventDate, e => e.ReturnEventDate))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { eventToUpdate.ID });

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



        // GET: SelectItems
        public async Task<IActionResult> SelectItems(int? EventID, string SearchString, int? page, int? pageSizeID
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


            var items = _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Category)
                .Include(i => i.Inventories)
                .Include(i=>i.ItemImages)
                .Include(i=>i.ItemThumbNail)
                .Include(i => i.Inventories).ThenInclude(i => i.Location)
                .AsNoTracking();

            var inv = _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Location)
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

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            var SelectedItems = await _context.Items.Include(i=>i.Supplier).Include(i=>i.Category).Include(i => i.ItemImages).Include(i => i.ItemThumbNail)
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
                //ItemReservation createItemReserv = new ItemReservation();
                //createItemReserv.ItemId = ItemID;
                //createItemReserv.EventId = 1;
                //createItemReserv.LocationID = 1;
                //createItemReserv.Quantity = 0;
                //createItemReserv.LoggedOutDate = DateTime.Now;
                //createItemReserv.ReturnDate = DateTime.Now;
                //createItemReserv.ReservedDate = DateTime.Now;
                //_context.Add(createItemReserv);



                var itemsupdate = _context.Items.Include(i => i.ItemImages).Include(i => i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

                itemsupdate.isSlectedForEvent= true;
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
                //ItemReservation createItemReserv = new ItemReservation();
                //createItemReserv.ItemId = ItemID;
                //createItemReserv.EventId = 1;
                //createItemReserv.LocationID = 1;
                //createItemReserv.Quantity = 0;
                //createItemReserv.LoggedOutDate = DateTime.Now;
                //createItemReserv.ReturnDate = DateTime.Now;
                //createItemReserv.ReservedDate = DateTime.Now;
                //_context.Add(createItemReserv);



                var itemsupdate = _context.Items.Include(i=>i.ItemImages).Include(i=>i.ItemThumbNail).Where(i => i.ID == ItemID).FirstOrDefault();

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
                .Include(i=>i.Inventories)
                .Include(i=>i.ItemImages)
                .Include(i=>i.ItemThumbNail)
                .Include(i=>i.Inventories).ThenInclude(i=>i.Location)
                .Where(i=>i.isSlectedForEvent == true)
            .ToListAsync();
            int? EventID = HttpContext.Session.GetInt32("EventID") ?? default;
            //if (EventID == 0)
            //{
            //    return NotFound();
            //}
            //else
            //{
                return View(SelectedItems);
            //}

        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> ChooseItemQuantities(int id)
        {
            string output = "";
            int EventID = HttpContext.Session.GetInt32("EventID") ?? default;
            foreach (var item in _context.Items)
            {
                if(Request.Form.ContainsKey("itemId" + item.ID.ToString()))
                {
                    //Getting the quantity of the item and location selected
                    int Quantity = int.Parse(Request.Form["itemId" + item.ID.ToString()]);

                    //Getting id of location so I can display the name (I dont think I need to siplay name but for testing purposes)
                    int locationID = int.Parse(Request.Form["locationId" + item.ID.ToString()]);
                    //Getting the Name of the location they selected by id
                    var location = _context.Locations
                        .Where(i => i.Id == locationID)
                        .FirstOrDefault();
                    
                    //Outputted a message to see if my logic worked, and It Did!
                    output += "Name: " + item.Name.ToString() + ", Location: " + location.Name+ ", Qty: " + Quantity + "\n";

                    var events = _context.Events
                        .Where(i=>i.ID == EventID)
                        .FirstOrDefault();

                    // Update the inventory quantity
                    var inventory = await _context.Inventories.Include(i=>i.Location).Include(i=>i.Item)
                        .Where(i => i.ItemID == item.ID && i.LocationID == locationID)
                        .FirstOrDefaultAsync();


                    if (events.ReservedEventDate <= DateTime.Today)
                    {
                        if (inventory != null)
                        {
                            inventory.Quantity -= Quantity;
                            _context.Update(inventory);
                            _context.SaveChanges();
                           
                        }
                    }

                    //CREATING THE RECORDS NOW:
                    ItemReservation createItemReserv = new ItemReservation();
                    createItemReserv.ItemId = item.ID;
                    createItemReserv.EventId = EventID;
                    createItemReserv.LocationID = locationID;
                    createItemReserv.Quantity = Quantity;
                    createItemReserv.LoggedOutDate = DateTime.Now;
                    _context.Add(createItemReserv);

                    _context.SaveChanges();
                }
            }
            foreach (var item in _context.Items)
            {
                item.isSlectedForEvent= false;
                _context.Update(item);
            
            }
            _context.SaveChanges();

            //_toastNotification.AddErrorToastMessage($"{output} EventID: {EventID.ToString()}");
            _toastNotification.AddSuccessToastMessage("Item Bookings Created! You can view them all in this index.");
            return RedirectToAction("Index", "Events");
           
            
           // return RedirectToAction("ChooseItemQuantities", "Events");
        }


        public async Task<IActionResult> LogBackInMultiple(int? id)
        {

            ViewDataReturnURL();


            var eventDetails = await _context.Events
                .Include(e => e.ItemReservations).ThenInclude(i => i.Item)
                .Include(e => e.ItemReservations).ThenInclude(i => i.Location)
                .Include(i=>i.ItemReservations).ThenInclude(i=>i.Item).ThenInclude(i=>i.Inventories)
                .Include(i => i.ItemReservations).ThenInclude(i => i.Item).ThenInclude(i => i.Inventories).ThenInclude(i=>i.Location)
                .FirstOrDefaultAsync(m => m.ID == id);


            return View(eventDetails.ItemReservations);
        }


        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> LogBackInMultiple(int id, IFormCollection form, string missingReportBool, string Notes, string Reason)
        {

            if (missingReportBool == "True")
            {

                // Get the value of MySessionVariable from the session state
               int itemReservID = Convert.ToInt32(HttpContext.Session.GetString("ItemReservationForLogMethod"));

                // Get the value of MySessionVariable from the session state
               int inventoryID = Convert.ToInt32(HttpContext.Session.GetString("InventoryForLogMethod"));

                // Get the value of MySessionVariable from the session state
               int userQuantity = Convert.ToInt32(HttpContext.Session.GetString("UserQuantityForLogMethod"));

                // Get the value of MySessionVariable from the session state
                int itemQtyVariance = Convert.ToInt32(HttpContext.Session.GetString("ItemQtyVarianceForLogMethod"));

                //Go get the ItemReservation to update
                var itemReservationToRemove = await _context.ItemReservations.Include(ir => ir.Event).Include(ir => ir.Item).Include(i => i.Location)
                    .Where(i=>i.Id == itemReservID)
                    .FirstOrDefaultAsync();
                // Update the inventory quantity
                var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.Id == inventoryID)
                    .FirstOrDefaultAsync();

                //_toastNotification.AddSuccessToastMessage($"Notes: {Notes}, Reason: {Reason}, Item: {itemReservationToRemove.Item.Name}, Qty: {itemReservationToRemove.Quantity}, UserQty: {userQuantity}, Inv Record: {inventory.Item.Name} - {inventory.Location.Name}");
                
                //FOR GETTING THE EMPLOYEE THAT LOGGED THIS AND CAUSED A QUANTITY VARIANCE
                var email = User.Identity.Name;
                var employee = _context.Employees.FirstOrDefault(e => e.Email == email);
                
                //MAKING A MISSINGITEMLOG RECORD FOR THE VARIANCE OF THIS ITEM
                var addMissingItemLog = new MissingItemLog();
                addMissingItemLog.Notes = Notes;
                addMissingItemLog.Reason = Reason;
                addMissingItemLog.Quantity = itemQtyVariance;
                addMissingItemLog.ItemId = itemReservationToRemove.ItemId;
                addMissingItemLog.EventId = itemReservationToRemove.EventId;
                addMissingItemLog.LocationID = itemReservationToRemove.LocationID;
                addMissingItemLog.EmployeeID = employee.ID;
                addMissingItemLog.Date = DateTime.Now;
                
                //SINCE WE ARE GOING TO LOGGED THIS ITEM BACK IN AND ADD THE QUANTITY, I WILL ASSIGN THE BOOL TO TRUE AS IF TECHNICALLY DELETED AND THE USER WILL SEE ITS LOGGED
                itemReservationToRemove.IsLoggedIn = true;
                itemReservationToRemove.LoggedInQuantity = userQuantity;
                inventory.Quantity += Convert.ToInt32(userQuantity); //ADDING THE INVENTORY QUANTITY BACK FOR ITS ASSIGNED LOCATION

                //ADDING EVERYTHING IN THE DATABASE.
                _context.Add(addMissingItemLog);
                _context.Update(inventory);
                _context.Update(itemReservationToRemove);
                _context.SaveChanges();


            }
            else
            {
                int EventID = id;


                string itemID = form["formId"]; // Get the form ID
                string locationId = form[$"locations-{itemID}"].ToString(); // Get the location ID
                string Quantity = form[$"itemId{itemID}"].ToString(); // Get the item ID



                var location = _context.Locations
                   .Where(i => i.Id == Convert.ToInt32(locationId))
                   .FirstOrDefault();
                var item = _context.Items
                   .Where(i => i.ID == Convert.ToInt32(itemID))
                   .FirstOrDefault();




                //Go get the ItemReservation to update
                var itemReservationToRemove = await _context.ItemReservations.Include(ir => ir.Event).Include(ir => ir.Item).Include(i => i.Location)
                    .Where(i => i.ItemId == Convert.ToInt32(itemID) && i.LocationID == location.Id && i.EventId == EventID && i.IsLoggedIn == false)
                    .FirstOrDefaultAsync();
                // Update the inventory quantity
                var inventory = await _context.Inventories.Include(i => i.Location).Include(i => i.Item)
                    .Where(i => i.ItemID == item.ID && i.LocationID == location.Id)
                    .FirstOrDefaultAsync();


                if (itemReservationToRemove == null)
                {
                    _toastNotification.AddErrorToastMessage($"Reservation Not Found");
                }
                else if (inventory == null)
                {
                    _toastNotification.AddErrorToastMessage($"Inventory Record Not Found");
                }
                else
                {
                    if (itemReservationToRemove.Quantity < Convert.ToInt32(Quantity))
                    {
                        _toastNotification.AddErrorToastMessage($"Oops, you entered a quantity that is more than what you logged in. Please enter the correct amount.");
                    }
                    //THIS METHOD IS TO POP UP THE MODEL FOR A DISCREPANCY LOG, TOOK A WHILE BUT MADE IT WORK.
                    else if (itemReservationToRemove.Quantity != Convert.ToInt32(Quantity))
                    {
                        TempData["InitateMissingItemLog"] = "Data received";
                       
                        var missingamount = itemReservationToRemove.Quantity - Convert.ToInt32(Quantity);
                        _toastNotification.AddErrorToastMessage($"{itemReservationToRemove.Quantity} {itemReservationToRemove.Item.Name}'s were logged out, you only logged in {Quantity}. Please specify the reason why <u>{missingamount}</u> of {itemReservationToRemove.Item.Name}'s aren't logged.");


                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ItemReservationForLogMethod", itemReservationToRemove.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("InventoryForLogMethod", inventory.Id.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("UserQuantityForLogMethod", Quantity.ToString());

                        // Get the value of MySessionVariable from the session state
                        HttpContext.Session.SetString("ItemQtyVarianceForLogMethod", missingamount.ToString());

                    }

                    else
                    {
                        itemReservationToRemove.IsLoggedIn = true;
                        itemReservationToRemove.LoggedInQuantity = Convert.ToInt32(Quantity);
                        inventory.Quantity += Convert.ToInt32(Quantity);
                        _context.Update(inventory);
                        _context.Update(itemReservationToRemove);
                        _context.SaveChanges();
                    }

                }

            }
            
            return RedirectToAction("LogBackInMultiple", "Events", id);
        }


        

        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.ID == id);
        }
    }
}
