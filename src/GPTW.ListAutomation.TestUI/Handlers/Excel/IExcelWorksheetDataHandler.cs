using System.Data;

namespace GPTW.ListAutomation.TestUI.Handlers;

public interface IExcelWorksheetDataHandler
{
    Task Process(int listSourceFileId, DataTable dt);

    WorksheetDataSource Source { get; }
}
