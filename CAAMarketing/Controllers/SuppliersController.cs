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
    public class SuppliersController : Controller
    {
        private readonly CAAContext _context;

        public SuppliersController(CAAContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(string SearchString, int? page, int? pageSizeID
            , string actionButton, string sortDirection = "asc", string sortField = "Supplier")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "Suppliers");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Supplier>.CreateAsync(_context.Suppliers.AsNoTracking(), page ?? 1, pageSize);


            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = ""; //Assume not filtering
                                        //Then in each "test" for filtering, add ViewData["Filtering"] = " show" if true;

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Supplier" };

            var suppliers = _context.Suppliers
                .AsNoTracking();


            if (!String.IsNullOrEmpty(SearchString))
            {
                suppliers = suppliers.Where(p => p.Name.ToUpper().Contains(SearchString.ToUpper()));

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
            if (sortField == "Supplier")
            {
                if (sortDirection == "asc")
                {
                    suppliers = suppliers
                        .OrderBy(p => p.Name);
                }
                else
                {
                    suppliers = suppliers
                        .OrderByDescending(p => p.Name);
                }
            }
            
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;



            return View(pagedData);
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.ID == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            return View();
        }

        // POST: Suppliers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Email,Phone,Address")] Supplier supplier)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Details", new { supplier.ID });

            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            //Go get the supplier to update
            var supplierToUpdate = await _context.Suppliers.FirstOrDefaultAsync(s => s.ID == id);

            //Check that you got it or exit with a not found error
            if (supplierToUpdate == null)
            {
                return NotFound();

            }

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(supplierToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Supplier>(supplierToUpdate, "",
                s => s.Name, s => s.Email, s => s.Phone, s => s.Address))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    // return RedirectToAction(nameof(Index));
                    return RedirectToAction("Details", new { supplierToUpdate.ID });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplierToUpdate.ID))
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
            return View(supplierToUpdate);
        }

        // GET: Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.ID == id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //URL with the last filter, sort and page parameters for this controller
            ViewDataReturnURL();

            if (_context.Suppliers == null)
            {
                return Problem("Entity set 'CAAContext.Suppliers'  is null.");
            }
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
            }
            
            await _context.SaveChangesAsync();
            // return RedirectToAction(nameof(Index));
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
        private bool SupplierExists(int id)
        {
          return _context.Suppliers.Any(e => e.ID == id);
        }
    }
}
