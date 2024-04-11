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

namespace CarSharing.Controllers
{
    public class BrandsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BrandsController(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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
                        ViewBag.isAdmin = sessionIsAdmin;
                    }
                    else
                    {
                        // User doesn't exist in the database, clear session and ViewBag
                        _httpContextAccessor.HttpContext.Session.Remove("Id");
                        _httpContextAccessor.HttpContext.Session.Remove("Login");
                        _httpContextAccessor.HttpContext.Session.Remove("Name");
                        _httpContextAccessor.HttpContext.Session.Remove("Password");
                        _httpContextAccessor.HttpContext.Session.Remove("isAdmin");
                        ViewBag.Name = null;
                        ViewBag.isAdmin = null;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        // GET: Brands
        public async Task<IActionResult> Index()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }

            var brandsAndElectricCarsCount = await _context.Brands
                .Select(brand => new BrandsViewModel
                {
                    Id = brand.Id,
                    Brand = brand.Name,
                    ElectricCarsCount = brand.Cars.Count(car => car.FuelType == FuelTypes.Electric)
                })
                .ToListAsync();

            return View(brandsAndElectricCarsCount);
        }

        // GET: Brands/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Brands/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brands/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
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
            return View(brand);
        }

        // GET: Brands/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            // Find all cars associated with the brand
            var cars = await _context.Cars.Where(c => c.BrandId == id).ToListAsync();

            // Remove the cars
            if (cars != null && cars.Count != 0)
            {
                _context.Cars.RemoveRange(cars);
            }

            if (brand != null)
            {
                _context.Brands.Remove(brand);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("isAdmin") == 1;
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
