using System.Security.Claims;
using Application.Interfaces.Services;
using Domain.Constants;

namespace Api.Security
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor accessor;
        public CurrentUser(IHttpContextAccessor accessor)
        {
            this.accessor = accessor;
        }

        private ClaimsPrincipal? Principal
        {
            get
            {
                return accessor.HttpContext?.User;
            }
        }

        public Guid? Id
        {
            get
            {
                return Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            }
        }

        public int? OrganizationId
        {
            get
            {
                return int.TryParse(Principal?.FindFirstValue("organization_id"), out var id) ? id : null;
            }
        }

        public bool IsOwner
        {
            get
            {
                return Principal?.IsInRole(Roles.Owner) == true;
            }
        }
    }
}
