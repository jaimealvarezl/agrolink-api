using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;

namespace AgroLink.Infrastructure.Repositories;

public class OwnerBrandRepository : Repository<OwnerBrand>, IOwnerBrandRepository
{
    public OwnerBrandRepository(AgroLinkDbContext context)
        : base(context) { }
}
