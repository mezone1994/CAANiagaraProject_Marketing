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
                .Include(p => p.ItemThumbNail)
                .AsNoTracking();

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

            if (ModelState.IsValid)
            {
                
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


                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { item.ID });

            }

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

            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);
            return View(item);
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
            ViewData["CategoryID"] = new SelectList(_context.Category, "Id", "Name", item.CategoryID);
            ViewData["SupplierID"] = new SelectList(_context.Suppliers, "ID", "Name", item.SupplierID);
            return View(item);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion, string chkRemoveImage, IFormFile thePicture)
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
                p => p.Name, p => p.Description, p => p.Notes, p => p.CategoryID, p => p.UPC,
                p => p.DateReceived, p => p.SupplierID))
            {
                try
                {
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
                    // return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { itemToUpdate.ID });

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

            try
            {
                if (item != null)
                {
                    _context.Items.Remove(item);
                }

                await _context.SaveChangesAsync();
                // return RedirectToAction(nameof(Index));
                return Redirect(ViewData["returnURL"].ToString());

            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(item);

        }

        //Method for Excel Report
        public IActionResult DownloadItems()
        {
            //Get the appointments
            var items = from i in _context.Items
                        .Include(i => i.Supplier)
                        .Include(i => i.Category)
                        orderby i.Name descending
                        select new
                        {
                            ItemImage = i.ItemThumbNail,
                            Item = i.Name,
                            Description = i.Description,
                            Notes = i.Notes,
                            UPC = i.UPC,
                            DateRecieved = i.DateReceived,
                            Supplier = i.Supplier.Name,
                            Category = i.Category.Name
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

                    var workSheet = excel.Workbook.Worksheets.Add("Items");

                    //Note: Cells[row, column]
                    workSheet.Cells[3, 1].LoadFromCollection(items, true);

                    //Style first column for dates
                    workSheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd";

                    //Style fee column for currency
                    workSheet.Column(4).Style.Numberformat.Format = "###,##0.00";

                    //Note: You can define a BLOCK of cells: Cells[startRow, startColumn, endRow, endColumn]
                    //Make Date and Patient Bold
                    workSheet.Cells[4, 1, numRows + 3, 2].Style.Font.Bold = true;

                    //Note: these are fine if you are only 'doing' one thing to the range of cells.
                    //Otherwise you should USE a range object for efficiency
                    using (ExcelRange totalfees = workSheet.Cells[numRows + 4, 4])//
                    {
                        totalfees.Formula = "Sum(" + workSheet.Cells[4, 4].Address + ":" + workSheet.Cells[numRows + 3, 4].Address + ")";
                        totalfees.Style.Font.Bold = true;
                        totalfees.Style.Numberformat.Format = "$###,##0.00";
                    }

                    //Set Style and backgound colour of headings
                    using (ExcelRange headings = workSheet.Cells[3, 1, 3, 8])
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
                    workSheet.Cells[1, 1].Value = "Item Report";
                    using (ExcelRange Rng = workSheet.Cells[1, 1, 1, 6])
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
                    using (ExcelRange Rng = workSheet.Cells[2, 6])
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
                        string filename = "Items.xlsx";
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
