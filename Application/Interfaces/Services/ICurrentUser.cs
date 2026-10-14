namespace Application.Interfaces.Services
{
    public interface ICurrentUser
    {
        Guid? Id { get; }

        int? OrganizationId { get; }

        bool IsOwner { get; }
    }
}
