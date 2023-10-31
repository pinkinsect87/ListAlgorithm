using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IStatusService : IListAutomationService
{
    IQueryable<Status> GetTable();
    void Delete(Status mStatus);
    Status? GetById(int Id);
    void Insert(Status mStatus);
    void Update(Status mStatus);
}

public class StatusService : IStatusService
{
    private readonly IRepository<Status> _statusRepository;

    public StatusService(IRepository<Status> statusRepository)
    {
        _statusRepository = statusRepository;
    }

    public IQueryable<Status> GetTable()
    {
        return _statusRepository.Table;
    }

    public void Delete(Status mStatus)
    {
        if (mStatus == null)
            throw new ArgumentNullException("Status");

        _statusRepository.Delete(mStatus);
    }

    public Status? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.StatusId == Id);
    }

    public void Insert(Status mStatus)
    {
        if (mStatus == null)
            throw new ArgumentNullException("Status");

        _statusRepository.Insert(mStatus);
    }

    public void Update(Status mStatus)
    {
        if (mStatus == null)
            throw new ArgumentNullException("Status");

        _statusRepository.Update(mStatus);
    }
}
