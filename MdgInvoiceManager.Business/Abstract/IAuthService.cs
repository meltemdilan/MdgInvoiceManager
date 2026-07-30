using System.Threading.Tasks;
using MdgInvoiceManager.Core.Dtos;

namespace MdgInvoiceManager.Business.Abstract
{
	public interface IAuthService
	{
		Task<ResponseModel> RegisterAsync(RegisterDto model);
		Task<ResponseModel> LoginAsync(LoginDto model);
	}
}