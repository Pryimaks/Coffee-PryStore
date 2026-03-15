using Coffee_PryStore.Models;
using Coffee_PryStore.Models.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Coffee_PryStore.Controllers
{
    public class AuthController(TokenService tokenService, DataBaseHome dataBaseHome) : Controller
    {
        private readonly TokenService _tokenService = tokenService;
        private readonly DataBaseHome _dataBaseHome = dataBaseHome;

        public class UserLoginDto
        {
            public string Email { get; set; } = string.Empty; 
            public string Password { get; set; } = string.Empty; 
        }


        public void RegisterUser(Models.User user, string password)
        {
            var passwordHasher = new PasswordHasher<Models.User>();
            user.Password = passwordHasher.HashPassword(user, password);
            _dataBaseHome.Users.Add(user);
            _dataBaseHome.SaveChanges();
        }

        public async Task<IActionResult> Login(UserLoginDto loginDto)
        {
            var user = _dataBaseHome.Users.FirstOrDefault(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return Unauthorized();
            }

            var passwordHasher = new PasswordHasher<Models.User>();
            var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Email), 
                new(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

            return RedirectToAction("Index", "Home");
        }
    }
}
