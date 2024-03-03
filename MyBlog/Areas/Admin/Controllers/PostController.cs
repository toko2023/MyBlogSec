using AspNetCoreHero.ToastNotification.Abstractions;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBlog.Data;
using MyBlog.Models;
using MyBlog.Utilites;
using MyBlog.ViewModels;

namespace MyBlog.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        public INotyfService _notification { get; }
        public PostController(ApplicationDbContext context, 
                                INotyfService notyfService, 
                                IWebHostEnvironment webHostEnvironment,
                                UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notification = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var listOfPosts = new List<Post>();

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var loggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);
            if(loggedInUserRole[0] == WebsiteRoles.WebsiteAdmin)
            {
                listOfPosts = await _context.Posts!.Include(x => x.ApplicationUser).ToListAsync();
            }
            else
            {
                listOfPosts = await _context.Posts!.Include(x => x.ApplicationUser).Where(x=>x.ApplicationUser!.Id==loggedInUser!.Id).ToListAsync();
            }

            var listOfPostsVM = listOfPosts.Select(x => new PostVM()
            {
                Id = x.Id,
                Title = x.Title,
                CreatedDate = x.CreatedDate,
                ThumbnailUrl = x.ThumbnailUrl,
                AuthorName = x.ApplicationUser!.FirstName +" "+ x.ApplicationUser.LastName
            }).ToList();
            return View(listOfPostsVM);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreatePostVM());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePostVM vm)
        {
            if(!ModelState.IsValid) { return View(vm); }

            // Get logged in user Id
            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            
            var post = new Post();
            post.Title = vm.Title;
            post.ShortDescription = vm.ShortDescription;
            post.Description = vm.Description;
            post.ApplicationUserId = loggedInUser!.Id;

            if(post.Title!= null)
            {
                string slug = vm.Title!.Trim();
                slug = slug.Replace(" ", "-");
                post.Slug = slug + "-" + Guid.NewGuid();
            }

            if(vm.Thumbnail != null)
            {
               post.ThumbnailUrl = UploadImage(vm.Thumbnail);
            }
            await _context.Posts!.AddAsync(post);
            await _context.SaveChangesAsync();
            _notification.Success("Нийтлэл амжилттай хадгалагдлаа");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts!.FirstOrDefaultAsync(X => X.Id == id);

            var loggedInUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var LoggedInUserRole = await _userManager.GetRolesAsync(loggedInUser!);

            if (LoggedInUserRole[0] == WebsiteRoles.WebsiteAdmin || loggedInUser?.Id == post?.ApplicationUserId)
            {
                _context.Posts!.Remove(post!);
                await _context.SaveChangesAsync();
                _notification.Success("Нийтлэлийг амжилттай устгалаа.");
                return RedirectToAction("Index", "Post", new { area = "Admin" });
            }
            return View();
        }
        private string UploadImage(IFormFile file)
        {
            string uniqueFileName = "";
            var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "thumbnails");
            uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(folderPath, uniqueFileName);
            using(FileStream fileStream = System.IO.File.Create(filePath))
            {
                file.CopyTo(fileStream);
            }
            return uniqueFileName;
        }

    }
}
