using CsvHelper;
using Newtonsoft.Json;
using Report.DataStructures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Xceed.Words.NET;
using Point = System.Windows.Point;

namespace Report
{
    public static class Files
    {
        private const string Mask = "peoples_detailed_*.tsv";
        private const string GroupSplitter = "EvacuationTime: ";
        private const char TsvSplitter = '\t';
        public static SortedDictionary<double, HashSet<Step>> Load(string folderPath)
        {
            Object mLock = new object();
            SortedDictionary<double, HashSet<Step>> allSteps = new SortedDictionary<double, HashSet<Step>>();
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] files = directoryInfo.GetFiles(Mask);

                Parallel.ForEach(files, file =>
                {
                    List<string> filelines = File.ReadAllLines(Path.Combine(folderPath, file.Name)).Skip(1).ToList();
                    List<int> indices = filelines.Select((b, i) => b.Contains(GroupSplitter) ? i : -1).Where(i => i != -1).ToList();
                    indices.Add(filelines.Count);
                    IEnumerable<StepStrings> iterator = SplitAndProcessLines(filelines, indices);

                    Parallel.ForEach(iterator, stepStrings =>
                    {
                        try
                        {
                            HashSet<Step> steps = new HashSet<Step>(new PersonComparer());

                            foreach (string stepString in stepStrings.Strings) {
                                steps.Add(new Step(stepString.Split(TsvSplitter)));
                            }
                            lock (mLock)
                            {
                                allSteps.Add(stepStrings.Time, steps);
                            }
                        }
                        catch
                        {
                            //ignored
                        }
                    });
                });


                if (allSteps.Count == 0)
                    throw new Exception($"No data found in {folderPath} folder!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

            return allSteps;
        }

        private static T LoadConfig<T>(string configPath)
        {
            string configText = File.ReadAllText(configPath);
            T resultConfig = JsonConvert.DeserializeObject<T>(configText);

            return resultConfig;
        }

        public static T LoadXMLConfig<T>(string configPath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StreamReader configReader = new StreamReader(configPath);
            T resultConfig = (T)xmlSerializer.Deserialize(configReader);
            return resultConfig;
        }

        private static IEnumerable<StepStrings> SplitAndProcessLines(List<string> array, IReadOnlyList<int> indices)
        {
            for (int i = 0; i < indices.Count - 1; i++)
            {
                List<string> stringsGroup = array.GetRange(indices[i], indices[i + 1] - indices[i]);
                double time = Convert.ToDouble(stringsGroup.First().Split(' ')[1]);
                yield return new StepStrings
                {
                    Time = time,
                    Strings = stringsGroup.Skip(1)
                };
            }
        }

        private class StepStrings
        {
            public double Time { get; set; }

            public IEnumerable<string> Strings { get; set; }
        }

        public static void DumpReport(List<Point> densityVsTimeForPositiveXData,
            List<Point> densityVsTimeForNegativeXData, List<Point> intensityVsTimeData, double registrator,
            string pathToSave)
        {
            if (!Directory.Exists(pathToSave))
            {
                Directory.CreateDirectory(pathToSave);
            }

            using (CsvWriter csv = new CsvWriter(new StreamWriter(Path.Combine(pathToSave,
                $"summary_peoples_detailed-registrator-{registrator}.tsv"))))
            {
                csv.Configuration.HasHeaderRecord = false;
                csv.Configuration.Delimiter = "\t";
                csv.Configuration.HasHeaderRecord = true;

                IEnumerable<dynamic> report = from densityForPositiveX in densityVsTimeForPositiveXData
                             join densityForNegativeX in densityVsTimeForNegativeXData
                                 on densityForPositiveX.X equals densityForNegativeX.X
                             join intensity in intensityVsTimeData
                                 on densityForPositiveX.X equals intensity.X
                             select new
                             {
                                 Time = densityForPositiveX.X,
                                 posMean = $"{densityForPositiveX.Y:0.##}",
                                 negMean = $"{densityForNegativeX.Y:0.##}",
                                 Intensity = $"{intensity.Y:0.##}"
                             };

                try
                {

                    csv.WriteHeader(report.First().GetType());
                    csv.NextRecord();
                    csv.WriteRecords(report);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public static DataStructures.TestGroup LoadTestInfoList(string filePath, out TestGroup testGroupFromXML)
        {
            DataStructures.TestGroup testGroup = new DataStructures.TestGroup
            {
                Tests = new List<TestInfo>()
            };

            testGroupFromXML = new TestGroup();
            try
            {
                Regex regexForTestPath = new Regex(@"[| ](Test_\d+_[0-9\.]+|[A-Z][:\\][A-z0-9\.\\ ]+)", RegexOptions.IgnoreCase);

                IEnumerable<string> filelines = File.ReadAllLines(Path.Combine(filePath)).Skip(1); //Игнорируем строчку с версией Fenix
                string xmlPath = filelines.First();
                string folder = Path.GetDirectoryName(xmlPath);
                testGroupFromXML = LoadXMLConfig<TestGroup>(xmlPath);
                TestGroupConfig groupConfig = LoadConfig<TestGroupConfig>(Path.Combine(folder, testGroupFromXML.ConfigLocation));
                testGroup.GroupConfig = groupConfig;
                testGroup.Description = testGroupFromXML.Description;
                foreach (string line in filelines.Skip(1))
                {
                    MatchCollection testPathGroupMatches = regexForTestPath.Matches(line);
                    if (testPathGroupMatches.Count <= 0) continue;
                    Test testFromTestGroup = testGroupFromXML.Tests.TestList.FirstOrDefault(x => x.Id.Equals(testPathGroupMatches[0].Value.Trim()));
                    TestConfig configTestFromTestGroup = LoadConfig<TestConfig>(Path.Combine(folder, testFromTestGroup.ConfigLocation));
                    Config conf = new Config
                    {
                        A = groupConfig.A,
                        D0 = groupConfig.D0,
                        F = groupConfig.F,
                        Length = groupConfig.Length,
                        Width = configTestFromTestGroup.Width ?? groupConfig.Width,
                        V0 = groupConfig.V0,
                        Density = configTestFromTestGroup.Density,
                        Description = testFromTestGroup.Description
                    };
                    testGroup.Tests.Add(new TestInfo { Name = testPathGroupMatches[0].Value.Trim(), Path = testPathGroupMatches[1].Value.Trim(), TestConfig = conf });
                }

                if (testGroup.Tests.Count == 0)
                    throw new Exception($"No data found in {filePath}!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

            return testGroup;
        }

        private static void SetCellText(Cell cell, string text, Alignment alingment) {
            try
            {

                cell.Paragraphs[0].RemoveText(0);
            }
            catch { }
            cell.Paragraphs[0].FontSize(11d).InsertText(text);
            cell.Paragraphs[0].Alignment = alingment;
            cell.VerticalAlignment = Xceed.Words.NET.VerticalAlignment.Center;
        }

        private static void SetCellConfigText(Table table, Config conf, int startIndex, int cellindex)
        {
            double[] values = { conf.V0, conf.D0, conf.A };
            string[] valueSymbols = { "V\u2080, м/мин", "D\u2080, чел/м\u00B2", "a"};
            for (int i = 0; i < 3; ++i)
            {
                Cell cell = table.Rows[startIndex + i].Cells[cellindex];
                Cell cellSymbols = table.Rows[startIndex + i].Cells[1];
                string value = values[i] != 0 ? values[i].ToString() : "-";
                SetCellText(cell, $"{value}", Alignment.center);
                SetCellText(cellSymbols, $"{valueSymbols[i]}", Alignment.center);
            }
        }

        public static void CropScreenShot(string fileName) {
            Rectangle cropRect = new Rectangle(6, 416, 2104, 208);
            var src = System.Drawing.Image.FromFile(fileName);
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            src.Dispose();
            target.Save(Path.Combine(Path.GetDirectoryName(fileName), "cropped_" + Path.GetFileName(fileName)));
            File.Delete(fileName);
        }

        public static void SaveSpeedVsDensityReportToDocX(string pathToImage, DataStructures.TestGroup testGroup, TestGroup testGroupFromXML, List<Point> evacTimeVsDensity)
        {
            string documentPath = Path.GetDirectoryName(pathToImage);
            Regex regexForDescription = new Regex(@"мобильности ([М1-9]+)|контингента ""([А-яA-z0-9 -]+)""", RegexOptions.IgnoreCase);
            string contingentCandidate = regexForDescription.Matches(testGroupFromXML.Description)[0].Groups[1].Value;
            string contingent = regexForDescription.Matches(testGroupFromXML.Description)[0].Groups[2].Value;
            if (contingentCandidate != string.Empty)
                contingent = contingentCandidate;

            var files = Directory.GetFiles(Path.Combine(documentPath, "Screenshot"));
            foreach (var file in files.Where(f => !f.Contains("cropped_")))
            {
                CropScreenShot(file);
            }

            using (DocX document = DocX.Create(Path.Combine(documentPath, $@"{testGroupFromXML.Id}.docx")))
            {
                document.MarginBottom = (float)(2.4 * (72 / 2.54));
                document.MarginTop = (float)(1.4 * (72 / 2.54));
                document.MarginLeft = (float)(2.0 * (72 / 2.54));
                document.MarginRight = (float)(2.0 * (72 / 2.54));

                // заголовок
                document.InsertParagraph($"{testGroupFromXML.Id} Контингент \"{contingent}\"").StyleName = "Heading4";

                // пустая строка после заголовка
                document.InsertParagraph();

                //Ссылка для загрузки тестового проекта
                string testFile = string.Format($"{testGroupFromXML.Id}.fnx");
                string testURI = Path.Combine("https://mst.su/download/data/v3", testFile);
                document.InsertParagraph("Ссылка для загрузки тестового проекта:").FontSize(12d).Bold().Alignment = Alignment.left;
                Paragraph p = document.InsertParagraph().FontSize(12d);
                Hyperlink h = document.AddHyperlink(testFile, new Uri(testURI));
                p.AppendHyperlink(h).Color(Color.Blue).UnderlineStyle(UnderlineStyle.singleLine).FontSize(12d);

                // пустая строка после ссылки на проект
                document.InsertParagraph();

                DirectoryInfo screenshotsDirectoryInfo = new DirectoryInfo(Path.Combine(documentPath, "Screenshot"));
                string[] screenshots = screenshotsDirectoryInfo.GetFiles().OrderBy(file => file.Name).Select(file => file.FullName).ToArray();
                char letter = 'a';
                Paragraph paragraph = document.InsertParagraph($"").FontSize(10d).SpacingAfter(0.5d);
                for (int i = 0; i < evacTimeVsDensity.Count; ++i)
                {
                    try
                    {
                        Xceed.Words.NET.Image _image = document.AddImage(screenshots[i]);
                        double height = 185 * (630 / 2110d); //Сжимаем картинку, сохраняя пропорции
                        Picture _picture = _image.CreatePicture(Convert.ToInt32(height), 630);
                        paragraph.AppendPicture(_picture);

                        paragraph = document.InsertParagraph($"({(char)(letter + i)}) Плотность потока {evacTimeVsDensity[i].X}\u00A0м\u00B2⁄м\u00B2").FontSize(12d).SpacingAfter(1.15);
                        paragraph.Alignment = Alignment.center;
                        paragraph.LineSpacingAfter = 1;
                        paragraph.LineSpacingBefore = 1;

                    }
                    catch
                    {
                        // ignored
                    }
                }

                paragraph = document.InsertParagraph($"Рис.\u00A01. {testGroup.Description}").FontSize(12d);
                paragraph.Alignment = Alignment.center;
                paragraph.InsertPageBreakAfterSelf();

                Table table = document.AddTable(testGroupFromXML.Tests.TestList.Count() + 2, 6);
                table.Design = TableDesign.TableGrid;

                table = document.InsertTable(table);
                table.MergeCellsInColumn(0, 0, 1); SetCellText(table.Rows[0].Cells[0], "", Alignment.center);
                table.MergeCellsInColumn(1, 0, 1); SetCellText(table.Rows[0].Cells[1], "Плотность потока, м\u00B2/м\u00B2", Alignment.center);
                table.MergeCellsInColumn(2, 0, 1); SetCellText(table.Rows[0].Cells[2], "Количество людей",Alignment.center); 
                table.MergeCellsInColumn(5, 0, 1); SetCellText(table.Rows[0].Cells[5], "Отклонение, %",Alignment.center);

                table.Rows[0].MergeCells(3, 4);
                table.Rows[0].Cells[3].Paragraphs[0].InsertText("Время движения, с"); table.Rows[0].Cells[3].Paragraphs[0].Alignment = Alignment.center;
                table.Rows[1].Cells[3].Paragraphs[0].InsertText("Fenix+\u00A03"); table.Rows[1].Cells[3].Paragraphs[0].Alignment = Alignment.center;
                table.Rows[1].Cells[4].Paragraphs[0].InsertText("Методика"); table.Rows[1].Cells[4].Paragraphs[0].Alignment = Alignment.center;

                int headerRows = 2;
                for(int testIndex = 0; testIndex < testGroupFromXML.Tests.TestList.Count(); ++testIndex)
                {
                    int rowIndex = testIndex + headerRows;
                    Cell cell = table.Rows[rowIndex].Cells[0];// Столбец с порядковым номером
                    SetCellText(cell, (testIndex + 1).ToString(), Alignment.center);

                    cell = table.Rows[rowIndex].Cells[1]; // Столбец с плотностью
                    SetCellText(cell, testGroup.Tests[testIndex].TestConfig.Density.ToString(), Alignment.center);

                    cell = table.Rows[rowIndex].Cells[2]; // Столбец с количеством людей
                    SetCellText(cell, testGroupFromXML.Tests.TestList[testIndex].ScenarioEvacuatedPeopleCount.ToString(), Alignment.center);

                    cell = table.Rows[rowIndex].Cells[3]; // Столбец с временем Fenix+
                    SetCellText(cell, $"{evacTimeVsDensity[testIndex].Y:0.#}", Alignment.center);

                    cell = table.Rows[rowIndex].Cells[4]; // Столбец с временем по методике
                    SetCellText(cell, testGroupFromXML.Tests.TestList[testIndex].RegistratorEvacuationTime.ToString(), Alignment.center);

                    cell = table.Rows[rowIndex].Cells[5]; // Столбец с отклонением
                    double deviation = (evacTimeVsDensity[testIndex].Y - testGroupFromXML.Tests.TestList[testIndex].RegistratorEvacuationTime) * 100 / testGroupFromXML.Tests.TestList[testIndex].RegistratorEvacuationTime;
                    SetCellText(cell, $"{deviation:0.#}", Alignment.center);
                }

                paragraph = document.InsertParagraph($"Табл.\u00A01. {testGroup.Description}").FontSize(12d).SpacingAfter(2);
                paragraph.Alignment = Alignment.center;

                Xceed.Words.NET.Image image = document.AddImage(pathToImage);
                Picture picture = image.CreatePicture(420, 630);
                paragraph.AppendPicture(picture);

                paragraph = document.InsertParagraph($"Рис.\u00A02. {testGroup.Description}").FontSize(12d).SpacingAfter(1.15);
                paragraph.Alignment = Alignment.center;
                paragraph.InsertPageBreakAfterSelf();
                
                document.Save();
            }
        }

        public static void ValidationTableToDocX(string pathToTests)
        {
            string[] directories = Directory.GetDirectories(pathToTests);
            List<TestGroupConfig> configs = new List<TestGroupConfig>();
            List<string> XMLconfigs = new List<string>();
            foreach (string directory in directories)
            {
                string testResultsPath = Directory.GetFiles(directory).Where(x => Regex.IsMatch(x, $@"(Test_).*(_report_).*(\.txt)")).FirstOrDefault();
                if (testResultsPath is null) continue;
                DataStructures.TestGroup _testGroup = LoadTestInfoList(testResultsPath, out var testGroupFromXML);
                configs.Add(_testGroup.GroupConfig);
                XMLconfigs.Add(testGroupFromXML.Description);
            }

            using (DocX document = DocX.Create(Path.Combine(pathToTests, $@"Таблица по всем контингентам.docx")))
            {
                document.MarginTop = 55;
                document.MarginBottom = 50;
                Table table = document.AddTable(configs.Count()*3 + 1, 7);
                table.Design = TableDesign.TableGrid;
                
                table = document.InsertTable(table);

                SetCellText(table.Rows[0].Cells[0], "Контингент", Alignment.center);
                SetCellText(table.Rows[0].Cells[1], "Параметр", Alignment.center);
                SetCellText(table.Rows[0].Cells[2], "Горизонтальный путь", Alignment.center);
                SetCellText(table.Rows[0].Cells[3], "Лестница вниз", Alignment.center);
                SetCellText(table.Rows[0].Cells[4], "Лестница вверх", Alignment.center);
                SetCellText(table.Rows[0].Cells[5], "Пандус вниз", Alignment.center);
                SetCellText(table.Rows[0].Cells[6], "Пандус вверх", Alignment.center);

                int headerRows = 1;
                for (int testIndex = 0; testIndex < configs.Count(); ++testIndex)
                {
                    Regex regexForDescription = new Regex(@"мобильности ([М1-9]+)|контингента ""([А-я -]+)""", RegexOptions.IgnoreCase);
                    string contingentCandidate = regexForDescription.Matches(XMLconfigs[testIndex])[0].Groups[1].Value;
                    string contingent = regexForDescription.Matches(XMLconfigs[testIndex])[0].Groups[2].Value;
                    if (contingentCandidate != string.Empty)
                        contingent = contingentCandidate;

                    int rowIndex = testIndex * 3 + headerRows;
                    TestGroupConfig config = configs[testIndex];
                    table.MergeCellsInColumn(0, rowIndex, rowIndex + 2);
                    Cell cell = table.Rows[rowIndex].Cells[0];
                    SetCellText(cell,  $"{contingent}\r\n(f={config.F}\u00A0м\u00B2)", Alignment.center);

                    SetCellConfigText(table, new Config { A = config.A ,D0 = config.D0,V0 = config.V0 }, rowIndex, 2);
                    SetCellConfigText(table, config.StairsDown, rowIndex, 3);
                    SetCellConfigText(table, config.StairsUP, rowIndex, 4);
                    SetCellConfigText(table, config.RampDown, rowIndex, 5);
                    SetCellConfigText(table, config.RampUP, rowIndex, 6);
                }

                document.Save();
            }
        }
    }
}