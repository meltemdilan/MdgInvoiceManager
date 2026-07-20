using MdgInvoiceManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MdgInvoiceManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // 1. DTO Model Kuralları Geçerli mi? ([EmailAddress], [Required] vb.)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 2. Kullanıcı Adı Var mı?
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return BadRequest(new { message = "Bu kullanıcı adı zaten alınmış!" });

            // 3. E-Posta Zaten Kayıtlı mı?
            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null)
                return BadRequest(new { message = "Bu e-posta adresi zaten kullanımda!" });

            // 4. Girilen Rol Sistemde Var mı? (Geçersiz/sallamasyon rolleri engeller)
            var roleExists = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExists)
                return BadRequest(new { message = $"'{model.Role}' adında geçerli bir rol sistemde bulunamadı!" });

            // 5. Kullanıcı Nesnesi Oluşturma
            IdentityUser user = new IdentityUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            // 6. Veritabanına Kaydetme (Şifre Hash'lenerek)
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "Kullanıcı oluşturulamadı.", errors = result.Errors });

            // 7. Kullanıcıya Rolü Atama
            await _userManager.AddToRoleAsync(user, model.Role);

            return Ok(new { message = $"Kullanıcı ({model.Username}) [{model.Role}] rolüyle başarıyla oluşturuldu!" });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                // Şifreleme Anahtarı ve İmzayı hazırlıyoruz
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mdg1234567891234mdg1234567891234"));
                var signingCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);
                // şu kısmın mantığını çok oturtamadım kafama *********************?????????????????????
                var token = new JwtSecurityToken(
                    issuer: "mdgadmin",
                    audience: "mdgkullanici",
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: signingCredentials
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    role = userRoles.FirstOrDefault()
                });
            }

            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı!" });
        }
    }
}