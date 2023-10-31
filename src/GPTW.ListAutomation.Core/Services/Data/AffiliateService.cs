using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IAffiliateService : IListAutomationService
{
    IQueryable<Affiliate> GetTable();
}

public class AffiliateService : IAffiliateService
{
    private readonly IRepository<Affiliate> _affiliateRepository;

    public AffiliateService(IRepository<Affiliate> affiliateRepository)
    {
        _affiliateRepository = affiliateRepository;
    }

    public void Delete(Affiliate entity)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");

        _affiliateRepository.Delete(entity);
    }

    public IQueryable<Affiliate> GetTable()
    {
        return _affiliateRepository.Table;
    }
}
