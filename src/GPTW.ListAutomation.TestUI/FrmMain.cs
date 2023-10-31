using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Handlers;
using GPTW.ListAutomation.TestUI.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.TestUI
{
    public partial class FrmMain : Form
    {
        #region Members

        private readonly IEnumerable<IExcelWorksheetDataHandler> _excelWorksheetDataHandlers;
        private readonly IListCompanyService _listCompanyService;
        private readonly IListAlgorithmTemplateService _listAlgorithmTemplateService;
        private readonly IListAutomationResultService _listAutomationResultService;
        private readonly IListAlgorithmHandler _listAlgorithmHandler;
        private readonly ILogger<FrmMain> _logger;

        private List<ListAlgorithmTemplateModel> _listAlgorithmTemplates;

        #endregion

        #region Ctor.

        public FrmMain(
            IEnumerable<IExcelWorksheetDataHandler> excelWorksheetDataHandlers,
            IListCompanyService listCompanyService,
            IListAlgorithmTemplateService listAlgorithmTemplateService,
            IListAutomationResultService listAutomationResultService,
            IListAlgorithmHandler listAlgorithmHandler,
            ILogger<FrmMain> logger)
        {
            InitializeComponent();

            _excelWorksheetDataHandlers = excelWorksheetDataHandlers;
            _listCompanyService = listCompanyService;
            _listAlgorithmTemplateService = listAlgorithmTemplateService;
            _listAutomationResultService = listAutomationResultService;
            _listAlgorithmHandler = listAlgorithmHandler;
            _logger = logger;
        }

        #endregion

        #region Form Methods

        private void FrmMain_Load(object sender, EventArgs e)
        {
            _listAlgorithmTemplates = _listAlgorithmTemplateService.GetListAlgorithmTemplates();

            cbAlgorithmTemplate.DataSource = _listAlgorithmTemplates;
            cbAlgorithmTemplate.DisplayMember = "TemplateName";
            cbAlgorithmTemplate.ValueMember = "TemplateId";
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
            // Force the process to end and exit
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            System.Environment.Exit(0);
        }

        #endregion

        #region Import ORG/Demographics/Comments

        private void btnSelect_Click(object sender, EventArgs e)
        {
            ControlInvoker.Invoke(this, delegate
            {
                this.lblStatus.Text = "";
                this.txtFileName.Text = "";
            });

            var selectedFilePath = "";

            var t = new Thread((ThreadStart)(() =>
            {
                var openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "Excel Worksheets|*.xls;*.xlsx";
                openFileDialog1.Title = "Select Excel file";
                openFileDialog1.Multiselect = false;

                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.Cancel)
                    return;

                selectedFilePath = openFileDialog1.FileName;
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            ControlInvoker.Invoke(this, delegate
            {
                this.txtFileName.Text = selectedFilePath;
            });
        }

        private async void btnImport_Click(object sender, EventArgs e)
        {
            if (!this.ValidateExcelFile())
            {
                return;
            }

            this.BeforeImporting();

            Aspose.Cells.Workbook _wk = new Aspose.Cells.Workbook(this.txtFileName.Text);

            try
            {
                var listSourceFileId = int.Parse(txtListSourceFileId.Text);

                foreach (var ws in _wk.Worksheets)
                {
                    if (!CultureSurveyConstants.WorksheetsForImport.Any(name => ws.Name.ToLower() == name)) continue;

                    ControlInvoker.Invoke(this, delegate
                    {
                        this.lblStatus.Text = $"Loading worksheet - {ws.Name}";
                    });

                    DataTable dt;
                    using (_logger.MeasureOperation($"ExportDataTableAsString - {ws.Name}"))
                    {
                        dt = ws.Cells.ExportDataTableAsString(0, 0, ws.Cells.MaxDataRow + 1, ws.Cells.MaxDataColumn + 1, true);
                    }

                    var excelWorksheetDataHandler = _excelWorksheetDataHandlers.SingleOrDefault(s => s.Source.ToString().ToLower() == ws.Name.ToLower());

                    if (excelWorksheetDataHandler != null && dt.Rows.Count > 0)
                    {
                        ControlInvoker.Invoke(this, delegate
                        {
                            this.lblStatus.Text = $"Importing {excelWorksheetDataHandler.Source} data to db";
                        });

                        await excelWorksheetDataHandler.Process(listSourceFileId, dt);
                    }
                }

                ControlInvoker.Invoke(this, delegate
                {
                    if (_wk.Worksheets.Any())
                    {
                        this.lblStatus.Text = "Import was successful!";
                    }
                    else
                    {
                        this.lblStatus.Text = "No Worksheets!";
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                this.AfterImporting();
            }
        }

        private bool ValidateExcelFile()
        {
            if (this.txtFileName.Text.IsMissing())
            {
                MessageBox.Show("Please select Excel file");
                return false;
            }
            else if (!File.Exists(this.txtFileName.Text))
            {
                MessageBox.Show("Please check that the selected Excel file does not exist");
                return false;
            }
            else if (!this.txtFileName.Text.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
                && !this.txtFileName.Text.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please check the selected Excel file");
                return false;
            }
            else if (this.txtListSourceFileId.Text.IsMissing())
            {
                MessageBox.Show("ListSourceFileId is required");
                this.txtListSourceFileId.Focus();
                return false;
            }
            else if (!int.TryParse(this.txtListSourceFileId.Text, out var a))
            {
                MessageBox.Show("Invalid ListSourceFileId");
                this.txtListSourceFileId.Focus();
                return false;
            }

            return true;
        }

        private void BeforeImporting()
        {
            ControlInvoker.Invoke(this, delegate
            {
                this.btnSelect.Enabled = false;
                this.btnImport.Enabled = false;
                this.btnExport.Enabled = false;
                this.btnImportCultureBrief.Enabled = false;
                this.lblStatus.Text = "Loading excel file...";
            });
        }

        private void AfterImporting()
        {
            ControlInvoker.Invoke(this, delegate
            {
                this.btnSelect.Enabled = true;
                this.btnImport.Enabled = true;
                this.btnExport.Enabled = true;
                this.btnImportCultureBrief.Enabled = true;
            });
        }

        #endregion

        #region Import Culture Brief 

        private async void btnImportCultureBrief_Click(object sender, EventArgs e)
        {
            if (!this.ValidateExcelFile())
            {
                return;
            }

            this.BeforeImporting();

            Aspose.Cells.Workbook _wk = new Aspose.Cells.Workbook(this.txtFileName.Text);

            try
            {
                if (_wk.Worksheets.Any())
                {
                    var ws = _wk.Worksheets[0];

                    ControlInvoker.Invoke(this, delegate
                    {
                        this.lblStatus.Text = $"Loading worksheet - {ws.Name}";
                    });

                    DataTable dt;
                    using (_logger.MeasureOperation($"ExportDataTableAsString - CultureBrief"))
                    {
                        dt = ws.Cells.ExportDataTableAsString(0, 0, ws.Cells.MaxDataRow + 1, ws.Cells.MaxDataColumn + 1, true);
                    }

                    var excelWorksheetDataHandler = _excelWorksheetDataHandlers.SingleOrDefault(s => s.Source == WorksheetDataSource.CultureBrief);

                    if (excelWorksheetDataHandler != null && dt.Rows.Count > 0)
                    {
                        ControlInvoker.Invoke(this, delegate
                        {
                            this.lblStatus.Text = $"Importing {excelWorksheetDataHandler.Source} data to db";
                        });

                        var listSourceFileId = int.Parse(txtListSourceFileId.Text);
                        await excelWorksheetDataHandler.Process(listSourceFileId, dt);
                    }
                }

                ControlInvoker.Invoke(this, delegate
                {
                    if (_wk.Worksheets.Any())
                    {
                        this.lblStatus.Text = "Import was successful!";
                    }
                    else
                    {
                        this.lblStatus.Text = "No Worksheets!";
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                this.AfterImporting();
            }
        }

        #endregion

        #region Export ORG Samples

        private void btnExport_Click(object sender, EventArgs e)
        {
            ControlInvoker.Invoke(this, delegate
            {
                this.btnSelect.Enabled = false;
                this.btnImport.Enabled = false;
                this.btnExport.Enabled = false;
                this.lblStatus.Text = "Generating data...";
            });

            try
            {
                Aspose.Cells.Workbook wk = new Aspose.Cells.Workbook();
                var orgWorksheet = wk.Worksheets[0];
                orgWorksheet.Name = "ORG";
                var demographicsWorksheet = wk.Worksheets.Add("Demographics");
                var commentsWorksheet = wk.Worksheets.Add("Comments");

                var demographicsExportColumns = CultureSurveyConstants.DemographicsExportColumns.Select(o => o.Key).ToArray();

                for (var eng = 0; eng < 4; eng++)
                {
                    var engagement_id = Utils.GenerateRandomCode(6);
                    var client_id = Utils.GenerateRandomCode(6);
                    var survey_ver_id = "202296";

                    for (var i = eng * 1000; i < (eng + 1) * 1000; i++)
                    {
                        var respondent_key = Utils.GenerateRandomCode(8);

                        #region ORG Worksheet

                        for (var j = 0; j < 4 + CultureSurveyConstants.RespondentColumns.Length; j++)
                        {
                            var cellValue = "";
                            if (i == 0) // header line
                            {
                                if (j == 0)
                                {
                                    cellValue = "engagement_id";
                                }
                                else if (j == 1)
                                {
                                    cellValue = "client_id";
                                }
                                else if (j == 2)
                                {
                                    cellValue = "survey_ver_id";
                                }
                                else if (j == 3)
                                {
                                    cellValue = "respondent_key";
                                }
                                else
                                {
                                    cellValue = CultureSurveyConstants.RespondentColumns[j - 4].ToString();
                                }
                            }
                            else
                            {
                                if (j == 0)
                                {
                                    cellValue = engagement_id;
                                }
                                else if (j == 1)
                                {
                                    cellValue = client_id;
                                }
                                else if (j == 2)
                                {
                                    cellValue = survey_ver_id;
                                }
                                else if (j == 3)
                                {
                                    cellValue = respondent_key;
                                }
                                else
                                {
                                    cellValue = Utils.GetIndexRandomNum(1, 6);
                                }
                            }
                            orgWorksheet.Cells.Rows[i][j].Value = cellValue;
                        }

                        #endregion

                        #region Demographics Worksheet

                        for (var j = 0; j < 4 + demographicsExportColumns.Length; j++)
                        {
                            var cellValue = "";
                            if (i == 0) // header line
                            {
                                if (j == 0)
                                {
                                    cellValue = "engagement_id";
                                }
                                else if (j == 1)
                                {
                                    cellValue = "client_id";
                                }
                                else if (j == 2)
                                {
                                    cellValue = "survey_ver_id";
                                }
                                else if (j == 3)
                                {
                                    cellValue = "respondent_key";
                                }
                                else
                                {
                                    cellValue = demographicsExportColumns[j - 4].ToString();
                                }
                            }
                            else
                            {
                                if (j == 0)
                                {
                                    cellValue = engagement_id;
                                }
                                else if (j == 1)
                                {
                                    cellValue = client_id;
                                }
                                else if (j == 2)
                                {
                                    cellValue = survey_ver_id;
                                }
                                else if (j == 3)
                                {
                                    cellValue = respondent_key;
                                }
                                else if (j == 4) // Country/Region
                                {
                                    cellValue = "United States [US]";
                                }
                                else if (j == 5) // Age
                                {
                                    var ageIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.AgeValues.Length));
                                    cellValue = CultureSurveyConstants.AgeValues[ageIndex];
                                }
                                else if (j == 6) // Gender
                                {
                                    var genderIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.GenderValues.Length));
                                    cellValue = CultureSurveyConstants.GenderValues[genderIndex];
                                }
                                else if (j == 8) // LGBT/LGBTQ+
                                {
                                    var lgbtIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.LgbtValues.Length));
                                    cellValue = CultureSurveyConstants.LgbtValues[lgbtIndex];
                                }
                                else if (j == 9) // Race/ Ethnicity
                                {
                                    var raceIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.RaceEthnicityValues.Length));
                                    cellValue = CultureSurveyConstants.RaceEthnicityValues[raceIndex];
                                }
                                else if (j == 10) // Responsibility
                                {
                                    var responsibilityIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.ResponsibilityValues.Length));
                                    cellValue = CultureSurveyConstants.ResponsibilityValues[responsibilityIndex];
                                }
                                else if (j == 11) // Tenure
                                {
                                    var tenureIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.TenureValues.Length));
                                    cellValue = CultureSurveyConstants.TenureValues[tenureIndex];
                                }
                                else if (j == 12) // Work Status
                                {
                                    var workStatusIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.WorkStatusValues.Length));
                                    cellValue = CultureSurveyConstants.WorkStatusValues[workStatusIndex];
                                }
                                else if (j == 15) // Birth Year
                                {
                                    var birthYearIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.BirthYearValues.Length));
                                    cellValue = CultureSurveyConstants.BirthYearValues[birthYearIndex];
                                }
                                else if (j == 16) // Confidence
                                {
                                    var confidenceIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.ConfidenceValues.Length));
                                    cellValue = CultureSurveyConstants.ConfidenceValues[confidenceIndex];
                                }
                                else if (j == 17) // Disabilities
                                {
                                    var disabilitiesIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.DisabilitiesValues.Length));
                                    cellValue = CultureSurveyConstants.DisabilitiesValues[disabilitiesIndex];
                                }
                                else if (j == 18) // Managerial Level
                                {
                                    var managerialLevelIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.ManagerialLevelValues.Length));
                                    cellValue = CultureSurveyConstants.ManagerialLevelValues[managerialLevelIndex];
                                }
                                else if (j == 19) // Meaningful Innovation Opportunities
                                {
                                    var meaningfulInnovationOpportunitiesIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.MeaningfulInnovationOpportunitiesValues.Length));
                                    cellValue = CultureSurveyConstants.MeaningfulInnovationOpportunitiesValues[meaningfulInnovationOpportunitiesIndex];
                                }
                                else if (j == 20) // Pay Type
                                {
                                    var payTypeIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.PayTypeValues.Length));
                                    cellValue = CultureSurveyConstants.PayTypeValues[payTypeIndex];
                                }
                                else if (j == 21) // Pay Type
                                {
                                    var zipCodeIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.ZipCodeValues.Length));
                                    cellValue = CultureSurveyConstants.ZipCodeValues[zipCodeIndex];
                                }
                                else
                                {
                                    cellValue = "";
                                }
                            }
                            demographicsWorksheet.Cells.Rows[i][j].Value = cellValue;
                        }

                        #endregion

                        #region Comments Worksheet

                        var questionIndex = int.Parse(Utils.GetIndexRandomNum(0, CultureSurveyConstants.Questions.Length));
                        var question = CultureSurveyConstants.Questions[questionIndex];

                        var answers = CultureSurveyConstants.QuestionAnswers[questionIndex];
                        var answerIndex = int.Parse(Utils.GetIndexRandomNum(0, answers.Length));
                        var answer = answers[answerIndex];

                        for (var j = 0; j < 4 + CultureSurveyConstants.CommentsColumns.Length; j++)
                        {
                            var cellValue = "";
                            if (i == 0) // header line
                            {
                                if (j == 0)
                                {
                                    cellValue = "engagement_id";
                                }
                                else if (j == 1)
                                {
                                    cellValue = "client_id";
                                }
                                else if (j == 2)
                                {
                                    cellValue = "survey_ver_id";
                                }
                                else if (j == 3)
                                {
                                    cellValue = "respondent_key";
                                }
                                else
                                {
                                    cellValue = CultureSurveyConstants.CommentsColumns[j - 4].ToString();
                                }
                            }
                            else
                            {
                                if (j == 0)
                                {
                                    cellValue = engagement_id;
                                }
                                else if (j == 1)
                                {
                                    cellValue = client_id;
                                }
                                else if (j == 2)
                                {
                                    cellValue = survey_ver_id;
                                }
                                else if (j == 3)
                                {
                                    cellValue = respondent_key;
                                }
                                else if (j == 4) // question
                                {
                                    cellValue = question;
                                }
                                else if (j == 5) // response
                                {
                                    cellValue = answer;
                                }
                                else
                                {
                                    cellValue = "";
                                }
                            }
                            commentsWorksheet.Cells.Rows[i][j].Value = cellValue;
                        }

                        #endregion
                    }
                }

                var filePath = AppContext.BaseDirectory + "ORG_Samples";
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                var fileName = $"ORG_Sample_Data_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xls";
                filePath = Path.Combine(filePath, fileName);
                wk.Save(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                ControlInvoker.Invoke(this, delegate
                {
                    this.btnSelect.Enabled = true;
                    this.btnImport.Enabled = true;
                    this.btnExport.Enabled = true;
                    this.lblStatus.Text = "Export was successful!";
                });
            }
        }

        #endregion

        #region Process List Algorithm

        private async void btnProcess_Click(object sender, EventArgs e)
        {
            ControlInvoker.Invoke(this, delegate
            {
                this.lblAlgStatus.Text = "";
            });

            var listCompanyId = -1;
            if (string.IsNullOrWhiteSpace(this.txtListCompanyId.Text))
            {
                MessageBox.Show("ListCompanyId is required");
                this.txtListCompanyId.Focus();
                return;
            }
            else if (!int.TryParse(this.txtListCompanyId.Text, out listCompanyId))
            {
                MessageBox.Show("Invalid ListCompanyId");
                this.txtListCompanyId.Focus();
                return;
            }

            var listCompany = _listCompanyService.GetListCompanyById(listCompanyId);
            if (listCompany == null)
            {
                MessageBox.Show("List Company doesn't exist");
                this.txtListCompanyId.Focus();
                return;
            }

            var listAlgorithmTemplate = _listAlgorithmTemplates.First(o => o.TemplateId == (int)cbAlgorithmTemplate.SelectedValue);

            var path = AppContext.BaseDirectory;
            var filepath = Path.Combine(path, "AlgorithmFiles", listAlgorithmTemplate.ManifestFileXml);

            if (!File.Exists(filepath))
            {
                MessageBox.Show("Algorithm File doesn't exist");
                return;
            }

            try
            {
                var countryRegionId = txtCountryRegionId.Text;

                ControlInvoker.Invoke(this, delegate
                {
                    this.lblAlgStatus.Text = "Processing...";
                    this.btnProcess.Enabled = false;
                });

                await _listAlgorithmHandler.Process(listCompany, filepath, countryRegionId);

                ControlInvoker.Invoke(this, delegate
                {
                    this.lblAlgStatus.Text = $"{listAlgorithmTemplate.ManifestFileXml} has been processed!";
                    this.btnProcess.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                ControlInvoker.Invoke(this, delegate
                {
                    this.lblAlgStatus.Text = "";
                    this.btnProcess.Enabled = true;
                });
            }
        }

        #endregion

        #region Export List Algorithm

        private async void btnExportListAlgorithm_Click(object sender, EventArgs e)
        {
            var listRequestId = -1;
            if (this.txtListRequestId.Text.IsMissing())
            {
                MessageBox.Show("ListRequestId is required");
                this.txtListRequestId.Focus();
                return;
            }
            else if (!int.TryParse(this.txtListRequestId.Text, out listRequestId))
            {
                MessageBox.Show("Invalid ListRequestId");
                this.txtListRequestId.Focus();
                return;
            }

            ControlInvoker.Invoke(this, delegate
            {
                this.btnExportListAlgorithm.Enabled = false;
                this.lblAlgStatus.Text = "Generating data...";
            });

            try
            {
                Aspose.Cells.Workbook wk = new Aspose.Cells.Workbook();
                var sheet = wk.Worksheets[0];

                var dt = await _listAutomationResultService.GetListAutomationResultByListRequestId(listRequestId);

                // Header
                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    sheet.Cells[0, i].PutValue(dt.Columns[i].ColumnName);
                }

                // Content
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        sheet.Cells[i + 1, j].PutValue(dt.Rows[i][j].ToString());
                    }
                }

                var filePath = AppContext.BaseDirectory + "ListAlgorithmResults";
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                var fileName = $"ListAlgorithmResult_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xls";
                filePath = Path.Combine(filePath, fileName);
                wk.Save(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                ControlInvoker.Invoke(this, delegate
                {
                    this.btnExportListAlgorithm.Enabled = true;
                    this.lblAlgStatus.Text = "Export was successful!";
                });
            }
        }

        #endregion
    }
}
