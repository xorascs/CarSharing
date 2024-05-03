using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarSharing.Data;
using CarSharing.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using CarSharing.Controllers.Utilities;

namespace CarSharing.Controllers
{
    public class RentsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Helper _helper;

        public RentsController(DataContext context, IHttpContextAccessor httpContextAccessor, Helper helper)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _helper = helper;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                var sessionId = _httpContextAccessor.HttpContext.Session.GetInt32("Id");
                var sessionName = _httpContextAccessor.HttpContext.Session.GetString("Name");
                var sessionIsAdmin = _httpContextAccessor.HttpContext.Session.GetInt32("isAdmin");
                var sessionLogin = _httpContextAccessor.HttpContext.Session.GetString("Login");
                var sessionPassword = _httpContextAccessor.HttpContext.Session.GetString("Password");

                if (!string.IsNullOrEmpty(sessionLogin) &&
                    !string.IsNullOrEmpty(sessionPassword) &&
                    sessionIsAdmin.HasValue &&
                    !string.IsNullOrEmpty(sessionName) &&
                    sessionId.HasValue)
                {
                    // Check if the user exists in the database
                    var existingUser = _context.Users.FirstOrDefault(
                        u => u.Login == sessionLogin &&
                        u.Password == sessionPassword &&
                        u.Name == sessionName &&
                        u.Id == sessionId &&
                        (sessionIsAdmin == 1 ? u.IsAdmin : !u.IsAdmin));

                    if (existingUser != null)
                    {
                        ViewBag.Name = sessionName;
                        ViewBag.Id = sessionId;
                        ViewBag.isAdmin = sessionIsAdmin;
                    }
                    else
                    {
                        // User doesn't exist in the database, clear session and ViewBag
                        _httpContextAccessor.HttpContext.Session.Clear();
                        ViewBag.Name = null;
                        ViewBag.Id = null;
                        ViewBag.isAdmin = null;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        // GET: Rents
        public async Task<IActionResult> Index()
        {
            if (!_helper.IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            var dataContext = _context.Rents.Include(r => r.Car!.Brand).Include(r => r.User);
            return View(await dataContext.ToListAsync());
        }
        // GET: Rents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!_helper.IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var rents = await _context.Rents.FindAsync(id);
            if (rents == null)
            {
                return NotFound();
            }

            // Fetch the cars with their corresponding brand names, excluding the currently rented car
            var carsWithBrands = await _context.Cars
                .Include(c => c.Brand)
                .Where(c => c.Id == rents.CarId || c.Status != Status.Renting) // Include the current rented car and exclude cars that are currently being rented
                .ToListAsync();

            // Create a list of SelectListItem to store car IDs and their corresponding brand names
            var carList = carsWithBrands.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Brand!.Name} {c.Model}" // Assuming "Name" is the property storing the brand name
            });

            ViewData["CarId"] = new SelectList(carList, "Value", "Text", rents.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Login", rents.UserId);
            return View(rents);
        }


        // POST: Rents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,CarId,RentTime,TimeForRent,CardNumber,CardHolderName,CardExDate,CVV")] Rents rents)
        {
            if (id != rents.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the previous rent to get the previous car ID
                    var previousRent = await _context.Rents.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
                    if (previousRent != null)
                    {
                        // Change the status of the previous car to Free
                        var previousCar = await _context.Cars.FirstOrDefaultAsync(c => c.Id == previousRent.CarId);
                        if (previousCar != null)
                        {
                            previousCar.Status = Status.Free;
                            _context.Update(previousCar);
                        }
                    }

                    // Change the status of the new car to Renting
                    var newCar = await _context.Cars.FirstOrDefaultAsync(c => c.Id == rents.CarId);
                    if (newCar != null)
                    {
                        newCar.Status = Status.Renting;
                        _context.Update(newCar);
                    }

                    // Update the current rent
                    _context.Update(rents);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RentsExists(rents.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "Model", rents.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Login", rents.UserId);

            return View(rents);
        }


        // GET: Rents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var rents = await _context.Rents
                .Include(r => r.Car!.Brand)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (!_helper.IsAdminJoined() && rents?.UserId != _helper.GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            if (rents == null)
            {
                return NotFound();
            }

            return View(rents);
        }

        // POST: Rents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rents = await _context.Rents.FindAsync(id);
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == rents!.CarId);
            if (rents != null)
            {
                car!.Status = Status.Free;
                _context.Rents.Remove(rents);
            }

            await _context.SaveChangesAsync();
            if (rents!.UserId == _helper.GetCurrentUserId() && !_helper.IsAdminJoined())
            {
                return RedirectToAction("Account", "Users", new { id = rents.UserId });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RentsExists(int id)
        {
            return _context.Rents.Any(e => e.Id == id);
        }
    }
}
