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

namespace CarSharing.Controllers
{
    public class CarsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CarsController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Cars
        public async Task<IActionResult> Index(int? brandId, Status? status, FuelTypes? fuelType, Color? color, string priceRange, DateTime? startDate, DateTime? endDate)
        {
            IQueryable<Car> cars = _context.Cars.Include(c => c.Brand);

            // Apply brand filter if brandId is provided
            if (brandId != null)
            {
                cars = cars.Where(c => c.BrandId == brandId);
            }

            // Apply brand filter if brandId is provided
            if (status != null)
            {
                cars = cars.Where(c => c.Status == status);
            }

            // Apply fuel type filter if fuelType is provided
            if (fuelType != null)
            {
                cars = cars.Where(c => c.FuelType == fuelType);
            }

            // Apply color filter if color is provided
            if (color != null)
            {
                cars = cars.Where(c => c.Color == color);
            }

            // Apply price range filter if priceRange is provided
            if (!string.IsNullOrEmpty(priceRange))
            {
                var priceRangeValues = priceRange.Split('-').Select(int.Parse).ToArray();
                cars = cars.Where(c => c.Price >= priceRangeValues[0] && c.Price <= priceRangeValues[1]);
            }

            // Apply manufacturing date range filter if startDate and endDate are provided
            if (startDate != null && endDate != null)
            {
                if (startDate > endDate)
                {
                    return BadRequest();
                }
                cars = cars.Where(c => c.ManufacturingDate >= startDate && c.ManufacturingDate <= endDate);
            }

            ViewBag.Brands = await _context.Brands.ToListAsync(); // Pass list of brands to the view

            return View(await cars.ToListAsync());
        }

        // GET: Cars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Brand)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // GET: Cars/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Cars");
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name");
            return View();
        }

        // POST: Cars/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BrandId,FuelType,Model,Color,ManufacturingDate,Price,ImagePaths")] Car car, List<IFormFile> ImagePaths)
        {
            if (ModelState.IsValid)
            {
                // Check if any files were uploaded
                if (ImagePaths != null && ImagePaths.Count > 0)
                {
                    foreach (var file in ImagePaths)
                    {
                        if (file != null && file.Length > 0)
                        {
                            // Define a unique filename for each uploaded image
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

                            // Define the directory where the image will be saved
                            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                            // Ensure the directory exists
                            if (!Directory.Exists(uploadsDirectory))
                            {
                                Directory.CreateDirectory(uploadsDirectory);
                            }

                            // Define the full path of the file
                            var filePath = Path.Combine(uploadsDirectory, uniqueFileName);

                            // Save the file to the server
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            // Append the filename to the list of ImagePaths in the Car object
                            car.ImagePaths.Add(uniqueFileName);
                        }
                    }
                }

                // Add the car object to the database
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid or no files were uploaded, return to the view with validation errors
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
            return View(car);
        }

        // GET: Cars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Cars");
            }

            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
            return View(car);
        }

        // POST: Cars/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BrandId,FuelType,Model,Color,ManufacturingDate,Price,ImagePaths")] Car car, List<IFormFile> ImagePaths)
        {
            if (id != car.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the original car object from the database
                    var originalCar = await _context.Cars.FirstOrDefaultAsync(c => c.Id == car.Id);

                    if (ImagePaths != null && ImagePaths.Count > 0)
                    {
                        // Remove existing images from the server
                        foreach (var imagePath in originalCar!.ImagePaths)
                        {
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imagePath);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }

                        // Clear the ImagePaths collection of the original car
                        originalCar.ImagePaths.Clear();

                        //Add new images
                        foreach (var file in ImagePaths)
                        {
                            if (file != null && file.Length > 0)
                            {
                                // Define a unique filename for each uploaded image
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

                                // Define the directory where the image will be saved
                                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                                // Ensure the directory exists
                                if (!Directory.Exists(uploadsDirectory))
                                {
                                    Directory.CreateDirectory(uploadsDirectory);
                                }

                                // Define the full path of the file
                                var filePath = Path.Combine(uploadsDirectory, uniqueFileName);

                                // Save the file to the server
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                }

                                // Append the filename to the list of ImagePaths in the original car object
                                originalCar.ImagePaths.Add(uniqueFileName);
                            }
                        }
                    }

                    if (originalCar == null)
                    {
                        return NotFound();
                    }

                    // Update other properties of the original car object
                    originalCar.BrandId = car.BrandId;
                    originalCar.FuelType = car.FuelType;
                    originalCar.Model = car.Model;
                    originalCar.Color = car.Color;
                    originalCar.ManufacturingDate = car.ManufacturingDate;
                    originalCar.Price = car.Price;

                    // Update the car object in the database
                    _context.Update(originalCar);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = car.Id });
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", car.BrandId);
            return RedirectToAction(nameof(Index));
        }

        // GET: Cars/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Cars");
            }

            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Brand)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                if (car.ImagePaths != null && car.ImagePaths.Count > 0)
                {
                    // Remove images from directory
                    foreach (var imagePath in car.ImagePaths)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", imagePath);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(int id, int rentalHours, string cardNumber, string cardHolderName, DateTime expirationDate, int cvv)
        {
            // Find the car by its id
            var car = await _context.Cars.FindAsync(id);

            if (car!.Status == Status.Renting)
            {
                return BadRequest();
            }

            if (car == null)
            {
                return NotFound(); // Car not found, return Not Found status
            }

            // Check if the user is logged in
            if (!IsLoggedIn())
            {
                // Redirect to the login page or display a message
                return RedirectToAction("Login", "Users"); // Redirect to the login page
            }

            // Retrieve the user from the session
            var userId = _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                // Handle case when user is not found
                return RedirectToAction("Index", "Home"); // Redirect to home page or display a message
            }

            // Update the car status to indicate that it is currently being rented
            car.Status = Status.Renting;

            // Create a new rent entry
            var rent = new Rents
            {
                UserId = user.Id,
                User = user,
                CarId = car.Id,
                Car = car,
                RentTime = DateTime.Now, // Record the current date and time as the rental time
                TimeForRent = rentalHours,
                CardNumber = cardNumber,
                CardHolderName = cardHolderName,
                CardExDate = expirationDate,
                CVV = cvv,
            };

            // Add the rent to the context and save changes
            _context.Rents.Add(rent);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Redirect to a page showing successful rental or display a message
            return RedirectToAction("Account", "Users"); // Redirect to the home page or a confirmation page
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return ViewBag.Name != null && ViewBag.isAdmin == 1;
        }
        private bool IsLoggedIn()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Name").IsNullOrEmpty();
        }
    }
}
