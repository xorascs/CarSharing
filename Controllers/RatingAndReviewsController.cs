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
    public class RatingAndReviewsController : Controller
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Helper _helper;

        public RatingAndReviewsController(DataContext context, IHttpContextAccessor httpContextAccessor, Helper helper)
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

        // GET: RatingAndReviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ratingAndReview = await _context.RatingAndReviews
                .Include(r => r.Car!.Brand)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ratingAndReview == null)
            {
                return NotFound();
            }

            if (!_helper.IsAdminJoined() && ratingAndReview.UserId != _helper.GetCurrentUserId())
            {
                return RedirectToAction("Index", "Cars");
            }

            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "Model", ratingAndReview.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Login", ratingAndReview.UserId);
            return View(ratingAndReview);
        }

        // POST: RatingAndReviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Comment,Rating")] RatingAndReview ratingAndReview)
        {
            if (id != ratingAndReview.Id)
            {
                return NotFound();
            }

            var existingRatingAndReview = await _context.RatingAndReviews.FindAsync(id);

            if (existingRatingAndReview == null)
            {
                return NotFound();
            }

            if (!_helper.IsAdminJoined() && existingRatingAndReview.UserId != _helper.GetCurrentUserId())
            {
                return RedirectToAction("Index", "Cars");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only the necessary properties
                    existingRatingAndReview.Comment = ratingAndReview.Comment;
                    existingRatingAndReview.Rating = ratingAndReview.Rating;
                    existingRatingAndReview.CreatedAt = DateTime.Now;

                    // Save the changes
                    _context.Update(existingRatingAndReview);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RatingAndReviewExists(existingRatingAndReview.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Cars", new { id = existingRatingAndReview.CarId });
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "Id", "Model", existingRatingAndReview.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Login", existingRatingAndReview.UserId);
            return View(existingRatingAndReview);
        }

        // GET: RatingAndReviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ratingAndReview = await _context.RatingAndReviews
                .Include(r => r.Car!.Brand)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ratingAndReview == null)
            {
                return NotFound();
            }

            if (!_helper.IsAdminJoined() && ratingAndReview!.UserId != _helper.GetCurrentUserId())
            {
                return RedirectToAction("Index", "Cars");
            }

            return View(ratingAndReview);
        }

        // POST: RatingAndReviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ratingAndReview = await _context.RatingAndReviews.FindAsync(id);
            if (ratingAndReview != null)
            {
                _context.RatingAndReviews.Remove(ratingAndReview);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Cars", new { id = ratingAndReview!.CarId });
        }

        private bool RatingAndReviewExists(int id)
        {
            return _context.RatingAndReviews.Any(e => e.Id == id);
        }

    }
}
