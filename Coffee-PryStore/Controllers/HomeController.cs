using Microsoft.AspNetCore.Mvc;
using Coffee_PryStore.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using static Coffee_PryStore.Models.Configurations.Startup;
using Microsoft.AspNetCore.Localization;
using System.Diagnostics;
using System.Globalization;

namespace Coffee_PryStore.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataBaseHome _context;

        public HomeController(DataBaseHome context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult ChangeLanguage(string culture, string returnUrl)
        {
            
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

           
            Response.Cookies.Append("lang", culture, new CookieOptions { Expires = DateTimeOffset.Now.AddYears(1) });

           
            return Redirect(returnUrl ?? "/");
        }

        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    


    public IActionResult Search()
        {
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View();
        }

      
        public IActionResult DetailsProduct(int id)
        {
            var product = _context.Table.FirstOrDefault(p => p.CofId == id);
            if (product == null)
            {
                return NotFound();
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(product);
        }



        [HttpGet]
        public IActionResult UserProfile(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("PersonRegistration", "PersonRegistration");
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(user);
        }
        
        public IActionResult CreateProduct()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Table product, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await ImageFile.CopyToAsync(memoryStream);
                    product.ImageData = memoryStream.ToArray();
                }

                await _context.AddAsync(product);
                await _context.SaveChangesAsync(); 
                return RedirectToAction("Index", "Home");
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(product);
        }



        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Table.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Table product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Table.FindAsync(product.CofId);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.CofName = product.CofName;
                existingProduct.CofCateg = product.CofCateg;
                existingProduct.CofPrice = product.CofPrice;
                existingProduct.CofAmount = product.CofAmount;
                existingProduct.CofDuration = product.CofDuration;

                if (imageFile != null && imageFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await imageFile.CopyToAsync(memoryStream);
                    existingProduct.ImageData = memoryStream.ToArray();
                }

                await _context.SaveChangesAsync(); 
                return RedirectToAction(nameof(Table));
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US";
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(product);
        }


        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchName, string searchCategory, string sortOrder)
        {
            var products = from p in _context.Table select p;

            if (!String.IsNullOrEmpty(searchName))
            {
                products = products.Where(p => p.CofName.Contains(searchName));
            }

            if (!String.IsNullOrEmpty(searchCategory))
            {
                products = products.Where(p => p.CofCateg == searchCategory);
            }


            products = sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.CofPrice),
                "price_desc" => products.OrderByDescending(p => p.CofPrice),
                "name_asc" => products.OrderBy(p => p.CofName),
                "name_desc" => products.OrderByDescending(p => p.CofName),
                _ => products.OrderBy(p => p.CofName),
            };
            var productList = await products.ToListAsync();

            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user != null)
                {
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserRole = user.Role;
                }
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 

          
            ViewData["CurrentLanguage"] = currentLanguage;

            return View(productList);
        }

        [HttpPost]
        public IActionResult SetLanguage(string language)
        {
    
            HttpContext.Session.SetString("CurrentLanguage", language);

    
            return Redirect(Request.Headers["Referer"].ToString());
        }




        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
       
            var product = await _context.Table.FirstOrDefaultAsync(p => p.CofId == productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = HttpContext.Session.GetObjectFromJson<Baskets>("Cart") ?? new Baskets();

            var userId = HttpContext.Session.GetInt32("UserId");

            Basket existingCartItem = null;
            if (userId.HasValue)
            {
                existingCartItem = await _context.Basket.FirstOrDefaultAsync(b => b.CofId == productId && b.Id == userId.Value);
            }

            if (existingCartItem != null)
            {
   
                existingCartItem.Quantity += quantity;
                var sessionCartItem = cart.Items.FirstOrDefault(b => b.CofId == productId);
                if (sessionCartItem != null)
                {
                    sessionCartItem.Quantity += quantity;
                }
            }
            else
            {
     
                var newCartItem = new Basket
                {
                    CofId = productId,
                    Quantity = quantity,
                    Id = userId ?? 0 
                };

                cart.Items.Add(newCartItem);
                await _context.Basket.AddAsync(newCartItem); 
            }

         
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            await _context.SaveChangesAsync();
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return RedirectToAction("Basket");
        }


        public async Task<IActionResult> Basket()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); 
            }

         
            var cartItems = await _context.Basket
                .Include(b => b.Cof)
                .Where(b => b.Id == userId.Value)
                .ToListAsync();

  
            var cart = new Baskets
            {
                Items = cartItems
            };

         
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return View(cart); 
        }


        private Baskets GetCartFromSession()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Baskets>("Cart");
            return cart ?? new Baskets();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var cartItem = userId != null
                ? await _context.Basket.FirstOrDefaultAsync(b => b.CofId == productId && b.Id == userId.Value)
                : null;

            if (cartItem != null)
            {
                _context.Basket.Remove(cartItem);
                await _context.SaveChangesAsync();

                var cart = HttpContext.Session.GetObjectFromJson<Baskets>("Cart") ?? new Baskets();
                cart.Items.RemoveAll(i => i.CofId == productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US"; 
            ViewData["CurrentLanguage"] = currentLanguage;
            return RedirectToAction("Basket");
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = HttpContext.Session.GetObjectFromJson<Baskets>("Cart");

            if (cart != null && cart.Items.Any())
            {
                var order = new Order
                {
                    UserId = userId.Value,
                    OrderItems = cart.Items.Select(item => new OrderItem
                    {
                        CofId = item.CofId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Cof.CofPrice
                    }).ToList(),
                    OrderDate = DateTime.Now,
                    TotalAmount = cart.Items.Sum(item => item.Quantity * item.Cof.CofPrice),
                    Status = "Pending"
                };

                await _context.Orders.AddAsync(order);
                _context.Basket.RemoveRange(cart.Items); 
                HttpContext.Session.Remove("Cart"); 
                await _context.SaveChangesAsync();
            }
            var currentLanguage = Request.Cookies["lang"] ?? "en-US";
            ViewData["CurrentLanguage"] = currentLanguage;
            return RedirectToAction("OrderConfirmation");
        }


        private Table? GetProductById(int cofId)
        {
            return _context.Table.Find(cofId);
        }


    }

}
