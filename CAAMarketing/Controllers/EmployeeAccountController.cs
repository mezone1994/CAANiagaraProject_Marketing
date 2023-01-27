using CAAMarketing.Data;
using CAAMarketing.Models;
using CAAMarketing.Utilities;
using CAAMarketing.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CAAMarketing.Controllers
{
    [Authorize]
    public class EmployeeAccountController : Controller
    {
        //Specialized controller just used to allow an 
        //Authenticated user to maintain their own account details.

        private readonly CAAContext _context;

        public EmployeeAccountController(CAAContext context)
        {
            _context = context;
        }

        // GET: EmployeeAccount
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Details));
        }

        // GET: EmployeeAccount/Details/5
        public async Task<IActionResult> Details()
        {

            var employee = await _context.Employees
               .Include(e => e.Subscriptions)
               .Where(e => e.Email == User.Identity.Name)
               .Select(e => new EmployeeVM
               {
                   ID = e.ID,
                   FirstName = e.FirstName,
                   LastName = e.LastName,
                   Phone = e.Phone,
                   NumberOfPushSubscriptions = e.Subscriptions.Count()
               })
               .FirstOrDefaultAsync();
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: EmployeeAccount/Edit/5
        public async Task<IActionResult> Edit()
        {
            var employee = await _context.Employees
                .Where(e => e.Email == User.Identity.Name)
                .Select(e => new EmployeeVM
                {
                    ID = e.ID,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Phone = e.Phone,
                    NumberOfPushSubscriptions = e.Subscriptions.Count()
                })
                .FirstOrDefaultAsync();
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: EmployeeAccount/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            var employeeToUpdate = await _context.Employees.FirstOrDefaultAsync(m => m.ID == id);

            //Note: Using TryUpdateModel we do not need to invoke the ViewModel
            //Only allow some properties to be updated
            if (await TryUpdateModelAsync<Employee>(employeeToUpdate, "",
                c => c.FirstName, c => c.LastName, c => c.Phone))
            {
                try
                {
                    _context.Update(employeeToUpdate);
                    await _context.SaveChangesAsync();
                    UpdateUserNameCookie(employeeToUpdate.FullName);
                    return RedirectToAction(nameof(Details));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employeeToUpdate.ID))
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
                    //Since we do not allow changing the email, we cannot introduce a duplicate
                    ModelState.AddModelError("", "Something went wrong in the database.");
                }
            }
            return View(employeeToUpdate);

        }

        private void UpdateUserNameCookie(string userName)
        {
            CookieHelper.CookieSet(HttpContext, "userName", userName, 960);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.ID == id);
        }
    }
}
