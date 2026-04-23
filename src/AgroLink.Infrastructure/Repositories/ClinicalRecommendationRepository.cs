using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;

namespace AgroLink.Infrastructure.Repositories;

public class ClinicalRecommendationRepository(AgroLinkDbContext context)
    : Repository<ClinicalRecommendation>(context),
        IClinicalRecommendationRepository { }
