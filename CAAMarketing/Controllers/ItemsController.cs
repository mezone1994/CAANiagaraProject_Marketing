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

namespace CAAMarketing.Controllers
{
    public class ItemsController : Controller
    {
        private readonly CAAContext _context;

        public ItemsController(CAAContext context)
        {
            _context = context;
        }

        // GET: Items
        public async Task<IActionResult> Index(string SearchString, int? SupplierID, int? CategoryID,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Item")
        {
            //For Status
            ViewBag.StatusPage = "Item";
            ViewBag.MessageStatus += "Hello World!";

            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
            //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;


            //Populating the DropDownLists for the Search/Filtering criteria, which are the Category and Supplier DDL
            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name");

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Item", "Category", "DateRecieved", "Supplier" };


            var items = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .Include(i=>i.Employee)
                .Include(p => p.ItemThumbNail)
                .AsNoTracking();

            items = items.Where(p => p.Archived == false);
            //foreach (Employee emp in _context.Employees)
            //{
            //    if ()
            //    {

            //    }
            //}



            //Add as many filters as needed
            //Add as many filters as needed
            if (SupplierID.HasValue)
            {
                items = items.Where(p => p.SupplierID == SupplierID);
                ViewData["Filtering"] = " show";
            }
            if (CategoryID.HasValue)
            {
                items = items.Where(p => p.CategoryID == CategoryID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                items = items.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper())
                                       || p.UPC.Contains(SearchString.ToUpper()));
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
            if (sortField == "Category")
            {
                if (sortDirection == "asc")
                {
                    items = items
                        .OrderBy(p => p.Name);
                }
                else
                {
                    items = items
                        .OrderByDescending(p => p.Name);
                }
            }
            else if (sortField == "DateRecieved")
            {
                if (sortDirection == "asc")
                {
                    items = items
                        .OrderByDescending(p => p.DateReceived);
                }
                else
                {
                    items = items
                        .OrderBy(p => p.DateReceived);
                }
            }
            else if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    items = items
                        .OrderBy(p => p.Supplier.Name);
                }
                else
                {
                    items = items
                        .OrderByDescending(p => p.Supplier.Name);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    items = items
                        .OrderBy(p => p.Name);
                }
                else
                {
                    items = items
                        .OrderByDescending(p => p.Name);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Items");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Item>.CreateAsync(items.AsNoTracking(), page ?? 1, pageSize);
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
        public async Task<IActionResult> Create([Bind("ID,Name,Description,Notes,CategoryID,UPC,DateReceived,SupplierID")] Item item, Inventory inventory, IFormFile thePicture)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            var email = User.Identity.Name;

            var employee = _context.Employees.FirstOrDefault(e => e.Email == email);
            item.EmployeeID = employee.ID;
            item.DateReceived = DateTime.Now;


                //item.EmployeeID = 1;
                _context.Add(item);
                await AddPicture(item, thePicture);
                await _context.SaveChangesAsync();
                _context.SaveChanges();
                try
                {
                    
                    inventory.Cost = 0;
                    inventory.LocationID = 1;
                    inventory.Quantity = 0;
                    inventory.ItemID = item.ID;
                    _context.Add(inventory);
                    _context.SaveChanges();
                }
                catch { }


            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);

            //return RedirectToAction(nameof(Index));
            //return RedirectToAction("Details", new { item.ID });
            return RedirectToAction("Create", "Orders", new { id = item.ID });


            

            //if (ModelState.IsValid)
            //{
            //    try
            //    {
            //        inventory.Cost = 0;
            //        inventory.LocationID = 1;
            //        inventory.Quantity = 0;
            //        inventory.ItemID = _context.Items.Count();
            //        _context.Add(inventory);
            //        await _context.SaveChangesAsync();
            //    }
            //    catch { }
            //}

        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Items == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .Include(p => p.ItemImages)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (item == null)
            {
                return NotFound();
            }

            //Go get the patient to update
            var inventoryToUpdate = await _context.Inventories
                .FirstOrDefaultAsync(p => p.ItemID == id);

            item.Quantity = inventoryToUpdate.Quantity;
            item.Cost = inventoryToUpdate.Cost;

            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string chkRemoveImage, IFormFile thePicture,
            string InvCost, string InvQty)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the patient to update
            var itemToUpdate = await _context.Items
                .Include(p => p.ItemImages)
                .FirstOrDefaultAsync(p => p.ID == id);





            //Check that you got it or exit with a not found error
            if (itemToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(itemToUpdate).Property("RowVersion").OriginalValue = RowVersion;

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
            //Get the picture and save it with the Patient (2 sizes)
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
    }
}
