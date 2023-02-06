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
using Org.BouncyCastle.Utilities;

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
        public async Task<IActionResult> Index(int? SupplierID, int? CategoryID, string SearchString, int? LocationID, bool? LowQty,
           int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Name")
        {
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
            string[] sortOptions = new[] { "Name", "Category", "DateRecieved", "Supplier" };

            var inventories = _context.Inventories
                .Include(i=>i.ItemThumbNail)
                .Include(i => i.Category)
                .Include(i => i.Employee)
                .Include(i => i.Supplier)
                .Include(i=>i.ItemThumbNail)
                .AsNoTracking();
            if (SupplierID.HasValue)
            {
                inventories = inventories.Where(p => p.SupplierID == SupplierID);
                ViewData["Filtering"] = " show";
            }
            if (CategoryID.HasValue)
            {
                inventories = inventories.Where(p => p.CategoryID == CategoryID);
                ViewData["Filtering"] = " show";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                inventories = inventories.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper())
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
                    inventories = inventories
                        .OrderBy(p => p.Category.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Category.Name);
                }
            }
            else if (sortField == "DateRecieved")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderByDescending(p => p.DateReceived);
                }
                else
                {
                    inventories = inventories
                        .OrderBy(p => p.DateReceived);
                }
            }
            else if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    inventories = inventories
                        .OrderBy(p => p.Supplier.Name);
                }
                else
                {
                    inventories = inventories
                        .OrderByDescending(p => p.Supplier.Name);
                }
            }
            else //Sorting by Patient Name
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
            var pagedData = await PaginatedList<Inventory>.CreateAsync(inventories.AsNoTracking(), page ?? 1, pageSize);
            return View(pagedData);
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Category)
                .Include(i => i.Employee)
                .Include(i => i.Supplier)
                .Include(i=>i.ItemImages)
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

            ViewData["CategoryID"] = new SelectList(_context.Categories, "Id", "Id");
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "ID");
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Address");
            return View();
        }

        // POST: Inventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Notes,UPC,Cost,Quantity,LocationID,DateReceived,SupplierID,CategoryID,EmployeeID,IsLowInventory,LowInventoryThreshold")] Inventory inventory)
        {

            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();
            var email = User.Identity.Name;

            var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

            if (ModelState.IsValid)
            {
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryID"] = new SelectList(_context.Categories, "Id", "Id", inventory.CategoryID);
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "ID", inventory.EmployeeID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Address", inventory.SupplierID);
            return View(inventory);
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i=>i.ItemImages)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }
            ViewData["CategoryID"] = new SelectList(_context.Categories, "Id", "Id", inventory.CategoryID);
            ViewData["EmployeeID"] = new SelectList(_context.Employees, "ID", "ID", inventory.EmployeeID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Address", inventory.SupplierID);
            return View(inventory);
        }

        // POST: Inventories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string chkRemoveImage, IFormFile thePicture)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the patient to update
            var itemToUpdate = await _context.Inventories
                .Include(p => p.ItemImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            //Check that you got it or exit with a not found error
            if (itemToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(itemToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Inventory>(itemToUpdate, "",
                p => p.Name, p => p.Description, p => p.Notes, p => p.CategoryID, p => p.UPC,
                p => p.DateReceived, p=>p.Cost, p=>p.Quantity, p => p.SupplierID , p=>p.SupplierID))
            {
                try
                {

                    var email = User.Identity.Name;

                    var employee = _context.Employees.FirstOrDefault(e => e.Email == email);

                    itemToUpdate.EmployeeNameUser = employee.FullName;
                    //foreach (Employee emp in _context.Employees)
                    //{
                    //    if (itemToUpdate.UpdatedBy == emp.Email)
                    //    {
                    //        itemToUpdate.EmployeeNameUser = emp.FullName;
                    //    }
                    //}

                    //For the image
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        itemToUpdate.ItemThumbNail = _context.ItemThumbNails.Where(p => p.InventoryID == itemToUpdate.Id).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        itemToUpdate.ItemImages = null;
                        itemToUpdate.ItemThumbNail = null;
                    }
                    else
                    {
                        await AddPicture(itemToUpdate, thePicture);
                    }

                    await _context.SaveChangesAsync();
                    // return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { itemToUpdate.Id });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(itemToUpdate.Id))
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

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Inventories == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .Include(i => i.Category)
                .Include(i => i.Employee)
                .Include(i => i.Supplier)
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
            if (_context.Inventories == null)
            {
                return Problem("Entity set 'CAAContext.Inventories'  is null.");
            }
            var inventory = await _context.Inventories.FindAsync(id);
            try
            {
                if (inventory != null)
                {
                    _context.Inventories.Remove(inventory);
                }

                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(inventory);

        }

        private string ControllerName()
        {
            return this.ControllerContext.RouteData.Values["controller"].ToString();
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }

        private async Task AddPicture(Inventory inv, IFormFile thePicture)
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
                        if (inv.ItemImages != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            inv.ItemImages.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            inv.ItemThumbNail = _context.ItemThumbNails.Where(p => p.InventoryID == inv.Id).FirstOrDefault();
                            inv.ItemThumbNail.Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120);
                        }
                        else //No pictures saved so start new
                        {
                            inv.ItemImages = new ItemImages
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            inv.ItemThumbNail = new ItemThumbNail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }

        private bool InventoryExists(int id)
        {
          return _context.Inventories.Any(e => e.Id == id);
        }
    }
}
