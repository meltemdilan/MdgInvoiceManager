using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Dtos;

namespace MdgInvoiceManager.Business.Concreate
{
    public class AuthManager : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthManager(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<ResponseModel> RegisterAsync(RegisterDto model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null) return new ResponseModel { IsSuccess = false, Message = "Bu kullanıcı adı zaten alınmış!" };

            var emailExists = await _userManager.FindByEmailAsync(model.Email);
            if (emailExists != null) return new ResponseModel { IsSuccess = false, Message = "Bu e-posta adresi zaten kullanımda!" };

            var roleExists = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExists) return new ResponseModel { IsSuccess = false, Message = $"'{model.Role}' adında geçerli bir rol bulunamadı!" };

            IdentityUser user = new IdentityUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return new ResponseModel { IsSuccess = false, Message = "Kullanıcı oluşturulamadı." };

            await _userManager.AddToRoleAsync(user, model.Role);
            return new ResponseModel { IsSuccess = true, Message = $"Kullanıcı ({model.Username}) [{model.Role}] rolüyle başarıyla oluşturuldu!" };
        }

        public async Task<ResponseModel> LoginAsync(LoginDto model)
        {
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

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mdg1234567891234mdg1234567891234"));
                var signingCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "mdgadmin",
                    audience: "mdgkullanici",
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: signingCredentials
                );

                return new ResponseModel
                {
                    IsSuccess = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Message = "Giriş başarılı."
                };
            }

            return new ResponseModel { IsSuccess = false, Message = "Kullanıcı adı veya şifre hatalı!" };
        }
    }
}