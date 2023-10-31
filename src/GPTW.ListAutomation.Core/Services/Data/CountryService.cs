using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface ICountryService : IListAutomationService
{
    IQueryable<Country> GetTable();
    void Delete(Country mCountry);
    Country? GetByCountryCode(string countryCode);
    void Insert(Country mCountry);
    void Update(Country mCountry);
}

public class CountryService : ICountryService
{
    private readonly IRepository<Country> _countryRepository;

    public CountryService(IRepository<Country> countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public IQueryable<Country> GetTable()
    {
        return _countryRepository.Table;
    }

    public void Delete(Country mCountry)
    {
        if (mCountry == null)
            throw new ArgumentNullException("Country");

        _countryRepository.Delete(mCountry);
    }

    public Country? GetByCountryCode(string countryCode)
    {
        if (countryCode.IsMissing()) return null;

        return GetTable().FirstOrDefault(d => d.CountryCode == countryCode);
    }

    public void Insert(Country mCountry)
    {
        if (mCountry == null)
            throw new ArgumentNullException("Country");

        _countryRepository.Insert(mCountry);
    }

    public void Update(Country mCountry)
    {
        if (mCountry == null)
            throw new ArgumentNullException("Country");

        _countryRepository.Update(mCountry);
    }
}
