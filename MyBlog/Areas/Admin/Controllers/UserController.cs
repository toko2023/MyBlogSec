using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBlog.Models;
using MyBlog.Utilites;
using MyBlog.ViewModels;

namespace MyBlog.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public INotyfService _notification { get; }
        public UserController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,INotyfService notyfService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _notification = notyfService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var vm = users.Select(x => new UserVM()
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                UserName = x.UserName
            }).ToList();

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVM());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if(!ModelState.IsValid) { return View(vm); }
            var checkUserByEmail = await _userManager.FindByEmailAsync(vm.Email);
            if(checkUserByEmail!= null)
            {
                _notification.Error("Email already exists");
                return View(vm);
            }
            var checkUserByUsername = await _userManager.FindByNameAsync(vm.UserName);
            if(checkUserByEmail!= null)
            {
                _notification.Error("Username already exists");
                return View(vm);
            }

            var applicationUser = new ApplicationUser()
            {
                Email = vm.Email,
                UserName = vm.UserName,
                FirstName = vm.FirstName,
                LastName = vm.LastName
            };
            var result = await _userManager.CreateAsync(applicationUser, vm.Password);
            if(result.Succeeded)
            {
                if(vm.IsAdmin)
                {
                    await _userManager.AddToRoleAsync(applicationUser, WebsiteRoles.WebsiteAdmin);
                }
                else
                {
                    await _userManager.AddToRoleAsync(applicationUser, WebsiteRoles.WebsiteAuthor);
                }
                _notification.Success("User registered successfully");
                return RedirectToAction("Index", "User", new { area = "Admin" });
            }
            return View(vm);
        }


        [HttpGet("Login")]
        public IActionResult Login()
        {
            if(!HttpContext.User.Identity!.IsAuthenticated)
            {
                return View(new LoginVM());
            }
            return RedirectToAction("Index", "User", new { area = "Admin" });
            
        }

      
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginVM vm)
        {
           if(!ModelState.IsValid) return View(vm);
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == vm.Username);
            if (existingUser == null)
            {
                _notification.Error("Хэрэглэгч олдсонгүй!");
                return View(vm);
            }
            var verifyPassword = await _userManager.CheckPasswordAsync(existingUser, vm.Password);
            if(!verifyPassword)
            {
                _notification.Error("Нууц үг буруу байна!");
                return View(vm);
            }
            await _signInManager.PasswordSignInAsync(vm.Username, vm.Password, vm.RememberMe, true);
            _notification.Success("Та амжилттай нэвтэрлээ");
            return RedirectToAction("Index", "User", new {area = "Admin"});
        }

        [HttpPost]
        public IActionResult Logout()
        {
            _signInManager.SignOutAsync();
            _notification.Success("Та амжилттай гарлаа");
            return RedirectToAction("Index", "Home", new {area = ""});
        }
        
    }
}
