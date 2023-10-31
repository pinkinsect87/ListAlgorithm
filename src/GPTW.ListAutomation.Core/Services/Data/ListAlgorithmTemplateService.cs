using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListAlgorithmTemplateService : IListAutomationService
{
    IQueryable<ListAlgorithmTemplate> GetTable();
    void Delete(ListAlgorithmTemplate mListAlgorithmTemplate);
    ListAlgorithmTemplate? GetById(int Id);
    void Insert(ListAlgorithmTemplate mListAlgorithmTemplate);
    void Update(ListAlgorithmTemplate mListAlgorithmTemplate);
    List<ListAlgorithmTemplateModel> GetListAlgorithmTemplates();
}

public class ListAlgorithmTemplateService : IListAlgorithmTemplateService
{
    private readonly IRepository<ListAlgorithmTemplate> _listAlgorithmTemplateRepository;

    public ListAlgorithmTemplateService(IRepository<ListAlgorithmTemplate> listAlgorithmTemplateRepository)
    {
        _listAlgorithmTemplateRepository = listAlgorithmTemplateRepository;
    }

    public IQueryable<ListAlgorithmTemplate> GetTable()
    {
        return _listAlgorithmTemplateRepository.Table;
    }

    public void Delete(ListAlgorithmTemplate mListAlgorithmTemplate)
    {
        if (mListAlgorithmTemplate == null)
            throw new ArgumentNullException("ListAlgorithmTemplate");

        _listAlgorithmTemplateRepository.Delete(mListAlgorithmTemplate);
    }

    public ListAlgorithmTemplate? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.TemplateId == Id);
    }

    public void Insert(ListAlgorithmTemplate mListAlgorithmTemplate)
    {
        if (mListAlgorithmTemplate == null)
            throw new ArgumentNullException("ListAlgorithmTemplate");

        _listAlgorithmTemplateRepository.Insert(mListAlgorithmTemplate);
    }

    public void Update(ListAlgorithmTemplate mListAlgorithmTemplate)
    {
        if (mListAlgorithmTemplate == null)
            throw new ArgumentNullException("ListAlgorithmTemplate");

        _listAlgorithmTemplateRepository.Update(mListAlgorithmTemplate);
    }

    public List<ListAlgorithmTemplateModel> GetListAlgorithmTemplates()
    {
        return GetTable().Select(t => new ListAlgorithmTemplateModel
        {
            TemplateId = t.TemplateId,
            TemplateName = t.TemplateName,
            ManifestFileXml = t.ManifestFileXml
        }).ToList();
    }
}
