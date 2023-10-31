using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface ITemplateTypeService : IListAutomationService
{
    IQueryable<TemplateType> GetTable();
    void Delete(TemplateType mTemplateType);
    TemplateType? GetById(int Id);
    void Insert(TemplateType mTemplateType);
    void Update(TemplateType mTemplateType);
}

public class TemplateTypeService : ITemplateTypeService
{
    private readonly IRepository<TemplateType> _templateTypeRepository;

    public TemplateTypeService(IRepository<TemplateType> templateTypeRepository)
    {
        _templateTypeRepository = templateTypeRepository;
    }

    public IQueryable<TemplateType> GetTable()
    {
        return _templateTypeRepository.Table;
    }

    public void Delete(TemplateType mTemplateType)
    {
        if (mTemplateType == null)
            throw new ArgumentNullException("TemplateType");

        _templateTypeRepository.Delete(mTemplateType);
    }

    public TemplateType? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.TemplateTypeId == Id);
    }

    public void Insert(TemplateType mTemplateType)
    {
        if (mTemplateType == null)
            throw new ArgumentNullException("TemplateType");

        _templateTypeRepository.Insert(mTemplateType);
    }

    public void Update(TemplateType mTemplateType)
    {
        if (mTemplateType == null)
            throw new ArgumentNullException("TemplateType");

        _templateTypeRepository.Update(mTemplateType);
    }
}
