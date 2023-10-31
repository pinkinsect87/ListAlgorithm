using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListSourceFileService : IListAutomationService
{
    IQueryable<ListSourceFile> GetTable();
    void Delete(ListSourceFile mListSourceFile);
    ListSourceFile? GetById(int Id);
    void Insert(ListSourceFile mListSourceFile);
    void Update(ListSourceFile mListSourceFile);
}

public class ListSourceFileService : IListSourceFileService
{
    private readonly IRepository<ListSourceFile> _listSourceFileRepository;

    public ListSourceFileService(IRepository<ListSourceFile> ListSourceFileRepository)
    {
        _listSourceFileRepository = ListSourceFileRepository;
    }

    public IQueryable<ListSourceFile> GetTable()
    {
        return _listSourceFileRepository.Table;
    }

    public void Delete(ListSourceFile mListSourceFile)
    {
        if (mListSourceFile == null)
            throw new ArgumentNullException("ListSourceFile");

        _listSourceFileRepository.Delete(mListSourceFile);
    }

    public ListSourceFile? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListSourceFileId == Id);
    }

    public void Insert(ListSourceFile mListSourceFile)
    {
        if (mListSourceFile == null)
            throw new ArgumentNullException("ListSourceFile");

        _listSourceFileRepository.Insert(mListSourceFile);
    }

    public void Update(ListSourceFile mListSourceFile)
    {
        if (mListSourceFile == null)
            throw new ArgumentNullException("ListSourceFile");

        _listSourceFileRepository.Update(mListSourceFile);
    }
}
