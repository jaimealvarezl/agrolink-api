using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;

namespace AgroLink.Infrastructure.Repositories;

public class ClinicalCaseEventRepository(AgroLinkDbContext context)
    : Repository<ClinicalCaseEvent>(context),
        IClinicalCaseEventRepository { }
