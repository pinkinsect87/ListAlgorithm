using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListRequestService : IListAutomationService
{
    IQueryable<ListRequest> GetTable();
    void Delete(ListRequest mListRequest);
    ListRequest? GetById(int Id);
    void Insert(ListRequest mListRequest);
    void Update(ListRequest mListRequest);
}

public class ListRequestService : IListRequestService
{
    private readonly IRepository<ListRequest> _listRequestRepository;

    public ListRequestService(IRepository<ListRequest> ListRequestRepository)
    {
        _listRequestRepository = ListRequestRepository;
    }

    public IQueryable<ListRequest> GetTable()
    {
        return _listRequestRepository.Table;
    }

    public void Delete(ListRequest mListRequest)
    {
        if (mListRequest == null)
            throw new ArgumentNullException("ListRequest");

        _listRequestRepository.Delete(mListRequest);
    }

    public ListRequest? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListRequestId == Id);
    }

    public void Insert(ListRequest mListRequest)
    {
        if (mListRequest == null)
            throw new ArgumentNullException("ListRequest");

        _listRequestRepository.Insert(mListRequest);
    }

    public void Update(ListRequest mListRequest)
    {
        if (mListRequest == null)
            throw new ArgumentNullException("ListRequest");

        _listRequestRepository.Update(mListRequest);
    }
}
