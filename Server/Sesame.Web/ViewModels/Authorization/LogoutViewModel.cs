using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sesame.Web.ViewModels.Authorization
{
    public class LogoutViewModel
    {
        [BindNever]
        public string RequestId { get; set; }
    }
}
