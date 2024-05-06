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
using CarSharing.Controllers.Utilities;

namespace CarSharing.Controllers
{
    public class UsersController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Helper _helper;

        public UsersController(DataContext context, IHttpContextAccessor httpContextAccessor, Helper helper)
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

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!_helper.IsAdminJoined())
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
            if (_helper.IsLoggedIn())
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

            if (exitingUser == null)
            {
                ModelState.AddModelError("Login", "Incorrect login or password.");
                return View(user);
            }

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
            if (_helper.IsLoggedIn())
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

        public async Task<IActionResult> Account(int? id)
        {
            if (_helper.IsLoggedIn())
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);
                var rents = await _context.Rents
                    .Include(r => r.Car!.Brand) 
                    .Where(r => r.User!.Id == id)
                    .ToListAsync();
                var reviews = await _context.RatingAndReviews
                    .Include(r => r.Car!.Brand!)
                    .Include(r => r.User!)
                    .Where(r => r.User!.Id == id)
                    .ToListAsync();

                if (user != null && rents != null)
                {
                    var viewModel = new UserViewModel
                    {
                        User = user,
                        Rents = rents,
                        Reviews = reviews,
                        totalRents = rents.Count(),
                        totalDollarsForCars = rents.Sum(r => r.Car!.Price)
                    };

                    return View(viewModel);
                }
            }

            // If user is not logged in or user not found, redirect to home
            return RedirectToAction("Login", "Users");
        }

        public async Task<IActionResult> ChatList(int? id)
        {
            if (!_helper.IsLoggedIn() || _helper.GetCurrentUserId() != id)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var chats = await _context.Chats
                .Include(c => c.UsersList)
                .Include(c => c.Messages)
                .Where(c => c.UsersList.Any(u => u.Id == user.Id))
                .ToListAsync();
            // Retrieve IDs of users who are already in chats with the specified user
            var usersInChatsIds = chats.Where(u => u.UsersList.Count < 3).SelectMany(c => c.UsersList.Select(u => u.Id)).Distinct().ToList();

            // Retrieve users who are not already in chats with the specified user
            var usersNotInChats = await _context.Users
                .Where(u => u.Id != id && !usersInChatsIds.Contains(u.Id))
                .ToListAsync();

            var chatViewModel = new ChatViewModel
            {
                Chats = chats,
                Users = usersNotInChats
            };

            return View(chatViewModel);
        }

        public async Task<IActionResult> CreateChat(int? creatorId, int? memberId)
        {
            if (creatorId == null || memberId == null)
            {
                // Handle the case where creatorId or memberId is null
                return BadRequest("CreatorId and MemberId are required.");
            }

            // Retrieve the users from the database
            var creator = await _context.Users.FindAsync(creatorId);
            var member = await _context.Users.FindAsync(memberId);

            if (creator == null || member == null)
            {
                // Handle the case where either creator or member is not found
                return NotFound("One or both users not found.");
            }

            var isChatExist = await _context.Chats
                .Include(c => c.UsersList)
                .Where(c => c.UsersList.Contains(creator) && c.UsersList.Contains(member) && c.UsersList.Count == 2)
                .FirstOrDefaultAsync();

            if (isChatExist != null)
            {
                return RedirectToAction("ShowChat", new { chatId = isChatExist.Id, userId = creator.Id });
            }

            // Create a new chat instance
            var chat = new Chat();

            // Add users to the chat
            chat.UsersList.Add(creator);
            chat.UsersList.Add(member);

            try
            {
                // Save the chat to the database
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                // Redirect to a page where the newly created chat can be viewed
                return RedirectToAction("ChatList", new {id = creator.Id});
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during saving
                return StatusCode(500, $"An error occurred while creating the chat: {ex.Message}");
            }
        }

        public async Task<IActionResult> ShowChat(int? chatId, int? userId)
        {
            if (!_helper.IsLoggedIn() && _helper.GetCurrentUserId() != userId)
            {
                return RedirectToAction("Index", "Home");
            }

            if (chatId == null)
            {
                return BadRequest("ChatId is required.");
            }

            // Retrieve the chat from the database including its messages
            var chat = await _context.Chats
                .Include(c => c.Messages)
                .Include(c => c.UsersList)
                .FirstOrDefaultAsync(c => c.Id == chatId);
            var usersInChat = await _context.Users
                .Where(u => u.ChatsList.Any(c => c.Id == chatId) && u.Id != userId)
                .ToListAsync();
            var usersNotInChat = await _context.Users
                .Where(u => !u.ChatsList.Any(c => c.Id == chatId))
                .ToListAsync();

            var user = await _context.Users
                .FirstOrDefaultAsync(c => c.Id == userId);

            var chatData = new ShowChatViewModel
            {
                Chat = chat,
                UsersInChat = usersInChat,
                UsersNotInChat = usersNotInChat
            };

            if (chat == null && user == null)
            {
                return NotFound("Chat not found.");
            }

            if (!chat!.UsersList.Contains(user!))
            {
                return NotFound("You have no access to this chat.");
            }

            return View(chatData);
        }

        public async Task<IActionResult> DeleteChat(int? chatId)
        {
            if (chatId == null)
            {
                return BadRequest("ChatId is required.");
            }

            // Retrieve the chat from the database including its users
            var chat = await _context.Chats
                .Include(c => c.UsersList)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                return NotFound("Chat not found.");
            }

            var userId = _helper.GetCurrentUserId();

            // Retrieve the current user from the database
            var currentUser = await _context.Users.FindAsync(userId);

            if (currentUser == null)
            {
                return NotFound("User not found.");
            }

            // Check if the current user has access to the chat
            if (chat.UsersList.Contains(currentUser))
            {
                _context.Chats.Remove(chat);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Current user has no access to this chat!");
            }

            return RedirectToAction("ChatList", new { id = userId });
        }

        public async Task<IActionResult> CreateMessage(int? chatId, string messageText)
        {
            if (chatId == null || string.IsNullOrEmpty(messageText))
            {
                return BadRequest("ChatId and message text are required.");
            }

            // Retrieve the chat from the database
            var chat = await _context.Chats
                .Include(c => c.UsersList)
                .Include(c => c.Messages) // Include messages for updating the chat
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                return NotFound("Chat not found.");
            }

            // Check if the current user has access to the chat
            var currentUserId = ViewBag.Id;
            if (!chat.UsersList.Any(u => u.Id == currentUserId))
            {
                return Unauthorized("You don't have access to this chat.");
            }

            // Create a new message
            var message = new Message
            {
                Text = messageText,
                UserId = currentUserId,
                ChatId = chat.Id,
                CreateTime = DateTime.Now
            };

            try
            {
                // Add the message to the chat and save changes
                chat.Messages.Add(message);

                // Update the chat with the latest message information
                chat.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Redirect to the chat view or any other appropriate action
                return RedirectToAction("ShowChat", new { chatId = chatId, userId = currentUserId });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, $"An error occurred while creating the message: {ex.Message}");
            }
        }

        public async Task<IActionResult> DeleteMessage(int? messageId, int? chatId, int? userId)
        {
            if (userId != _helper.GetCurrentUserId())
            {
                return RedirectToAction("Index", "Home");
            }

            if (messageId == null)
            {
                return BadRequest("MessageId is required.");
            }

            // Retrieve the message from the database
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                return NotFound("Message not found.");
            }

            try
            {
                // Remove the message from the context and save changes
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                // Redirect to the chat view or any other appropriate action
                return RedirectToAction("ShowChat", new { chatId = chatId, userId = userId });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, $"An error occurred while deleting the message: {ex.Message}");
            }
        }

        public async Task<IActionResult> AddUserToChat(int? chatId, int? userId)
        {
            if (chatId == null || userId == null)
            {
                return BadRequest("ChatId and UserId are required.");
            }

            // Retrieve the chat from the database
            var chat = await _context.Chats
                .Include(c => c.UsersList)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                return NotFound("Chat not found.");
            }

            // Retrieve the user to be added to the chat
            var userToAdd = await _context.Users.FindAsync(userId);

            if (userToAdd == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user is already in the chat
            if (chat.UsersList.Contains(userToAdd))
            {
                return BadRequest("User is already in the chat.");
            }

            var currentUserId = _helper.GetCurrentUserId();
            if (currentUserId == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Add the user to the chat and save changes
                chat.UsersList.Add(userToAdd);
                await _context.SaveChangesAsync();

                // Redirect to the chat view or any other appropriate action
                return RedirectToAction("ShowChat", new { chatId = chatId, userId = currentUserId });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, $"An error occurred while adding the user to the chat: {ex.Message}");
            }
        }

        public async Task<IActionResult> DeleteUserFromChat(int? chatId, int? userId)
        {
            if (chatId == null || userId == null)
            {
                return BadRequest("ChatId and UserId are required.");
            }

            // Retrieve the chat from the database
            var chat = await _context.Chats
                .Include(c => c.UsersList)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                return NotFound("Chat not found.");
            }

            // Retrieve the user to be removed from the chat
            var userToRemove = await _context.Users.FindAsync(userId);

            if (userToRemove == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user is in the chat
            if (!chat.UsersList.Contains(userToRemove))
            {
                return BadRequest("User is not in the chat.");
            }

            var currentUserId = _helper.GetCurrentUserId();
            if (currentUserId == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Remove the user from the chat and save changes
                chat.UsersList.Remove(userToRemove);
                await _context.SaveChangesAsync();

                // Redirect to the chat view or any other appropriate action
                return RedirectToAction("ShowChat", new { chatId = chatId, userId = currentUserId });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return StatusCode(500, $"An error occurred while removing the user from the chat: {ex.Message}");
            }
        }


        // GET: Users/Create
        public IActionResult Create()
        {
            if (!_helper.IsAdminJoined())
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
            if (!_helper.IsAdminJoined() && id != _helper.GetCurrentUserId())
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

                    if (user.Id == _helper.GetCurrentUserId() && !_helper.IsAdminJoined())
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
            if (!_helper.IsAdminJoined() && id != _helper.GetCurrentUserId())
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
    }
}
