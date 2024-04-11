using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarSharing.Data;
using CarSharing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace CarSharing.Controllers
{
    public class UsersController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsersController(DataContext context, IHttpContextAccessor httpContextAccessor)
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

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }

            var users = await _context.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var rents = await _context.Rents
                    .Include(r => r.Car!.Brand)
                    .Where(r => r.UserId == user.Id)
                    .ToListAsync();

                var userViewModel = new UserViewModel
                {
                    User = user,
                    Rents = rents
                };

                userViewModels.Add(userViewModel);
            }

            return View(userViewModels);
        }

        public IActionResult Login()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("Id,Login,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            var exitingUser = await _context.Users.FirstOrDefaultAsync(
                u => u.Login == user.Login && 
                u.Password == user.Password);

            if (exitingUser != null && _httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.SetInt32("Id", exitingUser.Id);
                _httpContextAccessor.HttpContext.Session.SetString("Login", exitingUser.Login);
                _httpContextAccessor.HttpContext.Session.SetString("Name", exitingUser.Name);
                _httpContextAccessor.HttpContext.Session.SetString("Password", exitingUser.Password);
                _httpContextAccessor.HttpContext.Session.SetInt32("isAdmin", exitingUser.IsAdmin ? 1 : 0);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid login or password";
                return RedirectToAction(nameof(Login));
            }
        }

        // GET: Users/Register
        public IActionResult Register()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Users/Register
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Id,IsAdmin,Login,Password,Name,Surname,Email,Address,PostalCode")] User user)
        {
            if (_httpContextAccessor.HttpContext != null && ModelState.IsValid)
            {
                // Check if a user with the same login already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(
                    u => u.Login == user.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }

                // If no user with the same login exists, proceed with registration
                _context.Add(user);
                await _context.SaveChangesAsync();
                _httpContextAccessor.HttpContext.Session.SetInt32("Id", user.Id);
                _httpContextAccessor.HttpContext.Session.SetString("Login", user.Login);
                _httpContextAccessor.HttpContext.Session.SetString("Name", user.Name);
                _httpContextAccessor.HttpContext.Session.SetString("Password", user.Password);
                _httpContextAccessor.HttpContext.Session.SetInt32("isAdmin", user.IsAdmin ? 1 : 0);

                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        public async Task<IActionResult> Account()
        {
            if (IsLoggedIn())
            {
                string name = ViewBag.Name;
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Name == name);
                var rents = await _context.Rents
                    .Include(r => r.Car!.Brand)  // Include the Car navigation property
                    .Where(r => r.User!.Name == name)
                    .ToListAsync();

                if (user != null && rents != null)
                {
                    var viewModel = new UserViewModel
                    {
                        User = user,
                        Rents = rents,
                    };

                    return View(viewModel);
                }
            }

            // If user is not logged in or user not found, redirect to home
            return RedirectToAction("Index", "Home");
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            if (!IsAdminJoined())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,IsAdmin,Login,Password,Name,Surname,Email,Address,PostalCode")] User user)
        {
            if (ModelState.IsValid)
            {
                // Check if a user with the same login already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(
                    u => u.Login == user.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Login", "User with this login already exists.");
                    return View(user);
                }
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminJoined() && id != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,IsAdmin,Login,Password,Name,Surname,Email,Address,PostalCode")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing user from the context
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    var sameLoginUser = await _context.Users.FirstOrDefaultAsync(
                        u => u.Login == user.Login && 
                        u.Id != id);

                    if (sameLoginUser != null)
                    {
                        ModelState.AddModelError("Login", "User with this login already exists.");
                        return View(user);
                    }

                    // Update the properties of the existing user with the values from the edited user
                    existingUser.IsAdmin = user.IsAdmin;
                    existingUser.Login = user.Login;
                    existingUser.Password = user.Password;
                    existingUser.Name = user.Name;
                    existingUser.Surname = user.Surname;
                    existingUser.Email = user.Email;
                    existingUser.Address = user.Address;
                    existingUser.PostalCode = user.PostalCode;

                    // Update the user in the context and save changes
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();

                    if (existingUser.Id == _httpContextAccessor.HttpContext!.Session.GetInt32("Id"))
                    {
                        _httpContextAccessor.HttpContext!.Session.SetInt32("Id", user.Id);
                        _httpContextAccessor.HttpContext!.Session.SetString("Login", user.Login);
                        _httpContextAccessor.HttpContext!.Session.SetString("Name", user.Name);
                        _httpContextAccessor.HttpContext!.Session.SetString("Password", user.Password);
                        _httpContextAccessor.HttpContext!.Session.SetInt32("isAdmin", user.IsAdmin ? 1 : 0);
                    }

                    if (user.Id == GetCurrentUserId() && !IsAdminJoined())
                    {
                        return RedirectToAction(nameof(Account));
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdminJoined() && id != GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                if (user.Name == _httpContextAccessor.HttpContext!.Session.GetString("Name"))
                {
                    _httpContextAccessor.HttpContext.Session.Remove("Id");
                    _httpContextAccessor.HttpContext.Session.Remove("Login");
                    _httpContextAccessor.HttpContext.Session.Remove("Name");
                    _httpContextAccessor.HttpContext.Session.Remove("Password");
                    _httpContextAccessor.HttpContext.Session.Remove("isAdmin");
                    ViewBag.Name = null;
                    ViewBag.isAdmin = null;

                    return RedirectToAction("Index", "Home");
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult LogOut()
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.Remove("Id");
                _httpContextAccessor.HttpContext.Session.Remove("Login");
                _httpContextAccessor.HttpContext.Session.Remove("Name");
                _httpContextAccessor.HttpContext.Session.Remove("Password");
                _httpContextAccessor.HttpContext.Session.Remove("isAdmin");
                ViewBag.Name = null;
                ViewBag.isAdmin = null;
            }
            return RedirectToAction("Index", "Home");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
        private bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("isAdmin") == 1;
        }
        private int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }
        private bool IsLoggedIn()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Name").IsNullOrEmpty();
        }
    }
}
