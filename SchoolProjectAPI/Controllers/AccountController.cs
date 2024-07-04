using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SchoolProjectAPI.Model;
using SchoolProjectAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SchoolProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IConfiguration _config;

        
        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _config = config;
        }

        [Route("UserRegistration")]
        [HttpPost]
        public async Task<IActionResult> Registration(UserRegistrationModel model)
        {
            var user = new IdentityUser { UserName = model.UserName, Email = model.Email, PhoneNumber = model.PhoneNumber };
            var result = await _userManager.CreateAsync(user,model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                // Check if there are any users in the system
                var anyUsers = _userManager.Users.Count();

                if (anyUsers == 1)
                {
                    // Assign Admin role to the first user
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    // Assign User role to other users
                    await _userManager.AddToRoleAsync(user, "User");
                }

                return Ok(new { Status = "Success", Message = "User registered successfully!" });
            }

            return BadRequest(new { Status = "Error", Message = "User registration failed!", Errors = result.Errors });

        }


        [Route("UserLogin")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                var token = GenerateJwtToken(user);

                return Ok(new { Token = token });
            }

            return Unauthorized();
        }

        //private string GenerateJwtToken(IdentityUser user)
        //{
        //    var roles =  _userManager.GetRolesAsync(user).Result;
        //    var role = roles.FirstOrDefault();
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new[] {
        //            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        //            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //            new Claim(ClaimTypes.NameIdentifier, user.Id),
        //            new Claim(ClaimTypes.Role,role)
        //        }),
        //        Expires = DateTime.UtcNow.AddHours(1),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };

        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    return tokenHandler.WriteToken(token);
        //}

        private string GenerateJwtToken(IdentityUser user)
        {
            var roles = _userManager.GetRolesAsync(user).Result;
               var role = roles.FirstOrDefault();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.UserName),
                new Claim(ClaimTypes.Role,role)
            };
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials);


            return new JwtSecurityTokenHandler().WriteToken(token);

        }

    }
}
