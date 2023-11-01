using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface ISegmentService : IListAutomationService
{
    IQueryable<Segment> GetTable();
    void Delete(Segment mSegment);
    Segment? GetById(int Id);
    void Insert(Segment mSegment);
    void Update(Segment mSegment);
}

public class SegmentService : ISegmentService
{
    private readonly IRepository<Segment> _SegmentRepository;

    public SegmentService(IRepository<Segment> SegmentRepository)
    {
        _SegmentRepository = SegmentRepository;
    }

    public IQueryable<Segment> GetTable()
    {
        return _SegmentRepository.Table;
    }

    public void Delete(Segment mSegment)
    {
        if (mSegment == null)
            throw new ArgumentNullException("Segment");

        _SegmentRepository.Delete(mSegment);
    }

    public Segment? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.SegmentId == Id);
    }

    public void Insert(Segment mSegment)
    {
        if (mSegment == null)
            throw new ArgumentNullException("Segment");

        _SegmentRepository.Insert(mSegment);
    }

    public void Update(Segment mSegment)
    {
        if (mSegment == null)
            throw new ArgumentNullException("Segment");

        _SegmentRepository.Update(mSegment);
    }
}
