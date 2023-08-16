using ARIDotNetCoreAutomationFrameWork.Config;
using ARIDotNetCoreAutomationFrameWork.Extensions;
using ARIDotNetCoreAutomationFrameWork.Helpers;
using Npgsql;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using TechTalk.SpecFlow;

namespace ECataLogComman.Library.Utilities
{
    public static class CommanUtility
    {

        public static string GetCurrentTimeInFormat(string format)
        {
            return DateTime.Now.ToString(format);
        }

        public static int GetRandomValue(int minValue, int maxValue)
        {
            Random rnd = new Random();
            return rnd.Next(minValue, maxValue);
        }
        public static void UploadFile(this IWebDriver driver, IWebElement element, string filePath)
        {
            IAllowsFileDetection allowsDetection = (IAllowsFileDetection)driver;
            allowsDetection.FileDetector = new LocalFileDetector();
            element.SendKeys(filePath);
        }


        #region Psrt

        public static bool VerifyListIsInAscendingOrder(ReadOnlyCollection<IWebElement> list)
        {
            List<string> originalList = new List<string>();
            foreach (var i in list)
            {
                originalList.Add(i.Text);
            }
            List<string> sortedList = new List<string>(originalList);
            sortedList.Sort();
            return sortedList.SequenceEqual(originalList);
        }
        public static bool VerifyListIsInDescendingOrder(ReadOnlyCollection<IWebElement> list)
        {
            List<string> originalList = new List<string>();
            foreach (IWebElement i in list)
            {
                originalList.Add(i.Text);
            }
            List<string> sortedList = new List<string>(originalList);
            sortedList.Sort();
            sortedList.Reverse();
            return sortedList.SequenceEqual(originalList);
        }
        public static Tuple<bool, HttpStatusCode, string> BrokenLinks(this IWebElement element)
        {
            HttpWebRequest re = (HttpWebRequest)WebRequest.Create(element.GetAttribute("href"));
            try
            {
                var response = (HttpWebResponse)re.GetResponse();
                var statusMessage = response.StatusCode;
                var status = $"URL: {element.GetAttribute("href")} status is :{response.StatusCode}";
                return new Tuple<bool, HttpStatusCode, string>(true, statusMessage, status);
            }
            catch (WebException e)
            {
                var errorResponse = (HttpWebResponse)e.Response;
                var statusMessage = errorResponse.StatusCode;
                var status = $"URL: {element.GetAttribute("href")} status is :{errorResponse.StatusCode}";
                return new Tuple<bool, HttpStatusCode, string>(true, statusMessage, status);
            }

        }
        public static bool VerifyElementTextPresent(this IWebDriver driver, IWebElement ele, string text, int time = 10)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(time));
            return wait.Until(d => ele.Text.Contains(text));
        }
        public static void WaitforProgressCompleteForDMS(this IWebDriver driver, IWebElement ele)
        {
            driver.WaitForAction(1);
            while (driver.FindElement(JQueryHelper.jQuery("div.ng-progress-bar.-active"), forIsDisplayed: true) != null)
            {
                ele.WaitTillElementDisappears(timeoutInSeconds: 180);
            }
        }
        public static string GetURLOfNewTab(this IWebDriver driver)
        {
            driver.SwitchToOtherTab(1);
            return driver.Url;
        }
        public static string DownloadDataFromNewTab(this IWebDriver driver, string name, string type = ".pdf")
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + name + type;
            WebClient webClient = new WebClient();
            webClient.DownloadFile(GetURLOfNewTab(driver), path);
            return path;

        }
        public static string GetXMLNodeValue(string xml, string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement.SelectSingleNode(path);
            return node.InnerText;
        }

        private static Random random = new Random();
        public static string GetRandomAlphaNumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string UpdatePriceInCsv(string pathOfFile, string CurrentCost)
        {
            if (CurrentCost.IndexOf('$') >= 0)
            {
                CurrentCost = CurrentCost.Split('$')[1];
            }

            string updatedPrice = "0";
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    updatedPrice = split[4] = Convert.ToString(Convert.ToDouble(CurrentCost) + 1);
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);
            return updatedPrice;
        }
        public static void UpdateMfrSkuInCSV(string filePath, string MfrSku)
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[2] = MfrSku;
                    split[3] = MfrSku;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(filePath, lines);
        }
        public static bool IsOrdered<T>(this IList<T> list, IComparer<T> comparer = null)
        {
            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            if (list.Count > 1)
            {
                for (int i = 1; i < list.Count; i++)
                {
                    if (comparer.Compare(list[i - 1], list[i]) > 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool ExecuteQueryToUpdateAttributeValueToNullOrBlank(string attribute)
        {
            NpgsqlConnection conn = new NpgsqlConnection(Settings.AppConnectionString);
            conn.Open();
            string qry = null;
            string qry2 = null;

            if (attribute == "C334459attr")
            {
                qry = @"UPDATE item i SET custom_data = '{""12"": null, ""1000"": ""UTC334459"", ""1077"": [], ""1088"": null }' WHERE i.id = 154286";
            }
            else if (attribute == "C334459attr2")
            {
                qry = @"UPDATE item i SET custom_data = '{""12"": null, ""1000"": ""UTC334459"", ""1077"": [], ""1088"": null, ""1089"": """" }' WHERE i.id = 154286";
                qry2 = @"UPDATE item_workflow SET modified = transaction_timestamp() AT TIME ZONE 'utc' where item_id = 154286";
                qry = qry + ";" + qry2;
            }
            else if (attribute == "C334459partattr")
            {
                qry = @"UPDATE item i SET custom_data = '{""1"": ""MOWER SHEAVE"", ""2"": ""0"", ""3"": 1, ""11"": null, ""12"": null, ""13"": [], ""14"": [], ""15"": null, ""16"": null, ""17"": null, ""18"": 0, ""19"": 0, ""20"": null, ""25"": 0, ""1092"": null}' WHERE i.id = 11917";
            }
            else if (attribute == "C334459partattr2")
            {
                qry = @"UPDATE item i SET custom_data = '{""1"": ""MOWER SHEAVE"", ""2"": ""0"", ""3"": 1, ""11"": null, ""12"": null, ""13"": [], ""14"": [], ""15"": null, ""16"": null, ""17"": null, ""18"": 0, ""19"": 0, ""20"": null, ""25"": 0, ""1092"": null, ""1093"": """"}' WHERE i.id = 11917";
            }
            using (var cmd = new NpgsqlCommand(qry, conn))
            {
                cmd.Prepare();
                try
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        #endregion

        #region

        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory + "Downloads";
        private static int filePrefix = 0;
      
        public static bool VerifyFileDownloaded(string fileName)
        {
            var filepath = path;
            if (Directory.Exists(filepath))
            {
                var files = new DirectoryInfo(filepath).GetFiles(fileName + "*.*");
                foreach (var p in files)
                {
                    if (Path.GetFileName(p.ToString()).StartsWith(fileName))
                    {
                        return true;
                    }
                }
                return false;
            }
            else return false;
        }
        public static bool DeleteDownloadedFiles(string fileType)
        {
            var filepath = path;
            if (Directory.Exists(filepath))
            {
                var files = new DirectoryInfo(filepath).GetFiles("*.*" + fileType);
                try
                {
                    foreach (FileInfo file in files)
                    {
                        file.Delete();
                    }
                    return true;
                }
                catch { return false; }
            }
            else
            {
                CreateDownloadDirectory();
                return true;
            }
        }
        public static bool DeleteExistingDownloadedFile(string fileName)
        {
            if (Directory.Exists(path))
            {

                if (DeleteExistingFile(fileName))
                    return true;
                return false;
            }
            else
            {
                CommanUtility.CreateDownloadDirectory();
                return true;
            }
        }

        /// <summary>
        /// This method will return file info and will take no parameter ,
        /// </summary>
        /// <returns>
        /// latest file info from all file of folder
        /// </returns>

        public static FileInfo GetLatestDownloadedFile()
        {
            Thread.Sleep(20000);
            var filepath = path;
            if (Directory.Exists(filepath))
            {
                DirectoryInfo directory = new DirectoryInfo(filepath);
                var files = directory.GetFiles()
                    .Union(directory.GetDirectories().Select(d => GetLatestDownloadedFile()))
                    .OrderByDescending(f => (f == null ? DateTime.MinValue : f.LastWriteTime))
                    .FirstOrDefault();
                return files;
            }
            else return null;

        }

        public static List<string> ReadZip(string zipFileName)
        {
            var filepath = path;
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(Path.Combine(filepath, zipFileName)))
            {
                return zip.EntryFileNames.ToList();

            }

        }
        /// <summary>
        /// This method will return file info and will take file type(html,pdf,txt) as a parameter ,
        /// </summary>
        /// <returns>
        /// latest file info from all file of folder
        /// </returns>
        public static FileInfo GetLatestDownloadedFile(string fileType)
        {
            var filepath = path;
            if (Directory.Exists(filepath))
            {
                DirectoryInfo directory = new DirectoryInfo(filepath);
                return directory.GetFiles("*." + fileType)
                       .OrderByDescending(f => f.LastWriteTime)
                       .First();
            }
            else return null;
        }
        /// <summary>
        /// This method is for total file counts in a folder
        /// </summary>
        /// <param name="fileType">type of file eg. pdf, html</param>
        /// <returns>
        /// Total file count
        /// </returns>
        public static int GetDownloadedFilesCount(string fileType)
        {
            var filepath = path;
            if (Directory.Exists(filepath))
            {
                return new DirectoryInfo(filepath).GetFiles("*." + fileType).Count();
            }
            return 0;
        }
        /// <summary>
        /// To verify counts before and after download file
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="fileCountsBeforDownload"></param>
        /// <returns></returns>
        public static bool VerifyFileCountAfterDownload(string fileType, int fileCountsBeforDownload)
        {
            return GetDownloadedFilesCount(fileType) > fileCountsBeforDownload ? true : false;
        }

        /// <summary>
        /// Method generates string of 'n' length including spaces 
        /// </summary>
        /// <param name="size">length of required string, default is 5</param>
        /// <returns>string of 'n' length</returns>
        public static string CreateSizeLengthData(int size = 5)
        {
            string legalCharacters = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = legalCharacters[random.Next(0, legalCharacters.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }
        public static string CurrentDateTimeInFormat(string dateFormat)
        {
            return DateTime.Now.ToString(dateFormat);
        }
        public static void UpdateDetailOfHierarchy(string pathOfFile)
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                filePrefix++;
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[0] = filePrefix + "_Name" + string.Format("{0:ddhhssmmss}", DateTime.Now) + i;
                    split[1] = "UniqueTag" + string.Format("{0:ddhhssmmss}", DateTime.Now) + i;
                    split[2] = filePrefix + "_Name" + string.Format("{0:ddhhssmmss}", DateTime.Now) + i;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);

        }

        public static string ReadHierarchyItemFromCSV(string pathOfFile, int cellNumber, int rowNumber = 1)
        {
            string ReturnValue = null;
            string[] lines = File.ReadAllLines(pathOfFile);
            if (lines.Length > 1)
            {
                string line = lines[rowNumber];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    return ReturnValue = split[cellNumber];
                }
            }
            return ReturnValue;
        }
        public static void UpdateParentDetailofHierarchyWithCreatedHierarchy(string parentName, string parentUniqueTag, string pathOfFile)
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[6] = parentName;
                    split[7] = parentUniqueTag;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);

        }
        /// <summary>
        /// Method adds space between each character of the string passed as parameter
        /// </summary>
        /// <param name="hierarchyName">any name</param>
        /// <returns>hierarchyName with each character separated by space </returns>
        public static string GetHierarchyNameFromPDF(string hierarchyName)
        {
            char[] cArray = hierarchyName.ToCharArray();
            return String.Join(" ", cArray);
        }
        public static bool AreTablesTheSame(DataTable tbl1, DataTable tbl2)
        {
            if (tbl1.Rows.Count != tbl2.Rows.Count || tbl1.Columns.Count != tbl2.Columns.Count)
                return false;


            for (int i = 0; i < tbl1.Rows.Count; i++)
            {
                for (int c = 0; c < tbl1.Columns.Count; c++)
                {
                    if (!Equals(tbl1.Rows[i][c], tbl2.Rows[i][c]))
                        return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Method extracts integer data from string 
        /// </summary>
        /// <param name="stringData">text (string)</param>
        /// <returns>integer type number</returns>
        public static int GetIntegerFromStringData(string stringData)
        {
            return int.Parse(Regex.Match(stringData, @"\d+").Value);
        }
        public static double GetDoubleValueFromStringData(string data)
        {
            return Math.Round(Convert.ToDouble(data.Trim().TrimStart('$')), 2);
        }
        public static string GetCurrentDateTime()
        {
            return DateTime.Now.ToString();
        }
        public static string GetCurrentUTCDateTime(string format)
        {
            return DateTime.UtcNow.ToString(format);
        }
        public static List<string> GetDownloadedCsvValues(string fileName)
        {
            string pathOfFile;
            pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;
            string[] AllLines = File.ReadAllLines(pathOfFile);
            var lines = AllLines.ToList();
            lines.RemoveAt(0);
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    string[] splitLines = line.Split(',');
                    line = string.Join(" ", splitLines);

                    lines[i] = line;
                }
            }
            return lines;
        }
        public static List<string> GetDownloadedCSVValuesForCustomAttributes(string fileName)
        {
            string pathOfFile = null;
            pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;

            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Replace("\",", " ").Replace(",\"", " ").Replace(",", " ");
                lines[i] = line;
            }
            return lines.ToList();
        }
        public static void UpdateDetailOfPartsBom(string pathOfFile, string iplName, string uniqueTag)
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[0] = iplName;
                    split[1] = uniqueTag;
                    split[3] = "PartNumber" + string.Format("{0:ddhhssmmss}", DateTime.Now) + i;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);

        }
        public static Tuple<string, string> UploadNewImageLiterature(string image = "new", string type = "pdf")
        {
            string imagePath = null;
            string literatureName = null;
            if (image == "new")
            {
                literatureName = CurrentDateTimeInFormat("ddhhssmmss");
                imagePath = AppDomain.CurrentDomain.BaseDirectory + literatureName + "." + type;
                SaveImageOfType(literatureName, imagePath);
            }
            else if (image.Contains("_existing"))
            {
                literatureName = image.Replace("_existing", "");
                imagePath = AppDomain.CurrentDomain.BaseDirectory + literatureName + "." + type;
            }
            else if (image.Contains("Rename"))
            {
                literatureName = image;
                imagePath = AppDomain.CurrentDomain.BaseDirectory + literatureName + "." + type;
            }
            else
            {
                literatureName = image;
                imagePath = AppDomain.CurrentDomain.BaseDirectory + literatureName + "." + type;
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
                SaveImageOfType(literatureName, imagePath, GenerateRandomFontSize());
            }
            return new Tuple<string, string>(imagePath, literatureName);
        }

        public static System.Drawing.Image SaveImageOfType(string imageName, string imagePath, int fontsize = 24)
        {
            System.Drawing.Font font = new System.Drawing.Font("Arial", fontsize, FontStyle.Bold);
            //first, create a dummy bitmap just to get a graphics object
            System.Drawing.Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);

            //measure the string to see how big the image needs to be
            SizeF textSize = drawing.MeasureString(imageName, font);

            //free up the dummy image and old graphics object
            img.Dispose();
            drawing.Dispose();

            //create a new image of the right size
            img = new Bitmap((int)textSize.Width, (int)textSize.Height);
            drawing = Graphics.FromImage(img);

            //paint the background
            drawing.Clear(Color.White);

            //create a brush for the text
            Brush textBrush = new SolidBrush(Color.Black);
            drawing.DrawString(imageName, font, textBrush, 0, 0);

            img.Save(imagePath);

            textBrush.Dispose();
            drawing.Dispose();
            return img;
        }
        public static bool DMRTDataReseed(string baseDb, string targetDb)
        {
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection("Server=ecatdqpsg01.ecat.leadventure.dev;Port=5432;" + "User Id=postgres;Password=arinet;Database=oem_arn6;CommandTimeout=600");
                conn.Open();
                string query = "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '" + baseDb + "' AND pid<> pg_backend_pid();" +
                     " SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '" + targetDb + "' AND pid<> pg_backend_pid();" +
                     " DROP DATABASE \"" + targetDb + "\";" +
                     " CREATE DATABASE \"" + targetDb + "\" WITH TEMPLATE '" + baseDb + "'; " +
                 "ALTER DATABASE \"" + targetDb + "\" set search_path = \"dbo\",\"$user\",public;";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                //command.CommandText = query;            
                //// Execute the query and obtain the value of the first column of the first row
                var result = command.ExecuteNonQuery();
                return result == -1 ? true : false;
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }
        public static bool VerifyDataExistsinFile(string fileName, int columnNumber, string data = null)
        {
            bool returnValue = false;
            List<string> exportFileData = GetDownloadedCSVColumnData(fileName, columnNumber);
            foreach (var code in exportFileData)
            {
                if (code.Contains(data))
                    return true;
            }
            if (!returnValue)
            {
                data = data.Replace(" ", "");
                data = data.ToLower();
                foreach (string code in exportFileData)
                {
                    if (code.ToLower().Contains(data))
                        return true;
                }
            }
            return false;

        }

        public static string GetRowHeadersOfCSVFile(string fileName)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;
            return File.ReadAllLines(pathOfFile).ToList().First();
        }
        public static Tuple<bool, int> VerifyRowHeaderOfCSVFile(string fileName, string headerName)
        {
            string pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;
            var lines = File.ReadLines(pathOfFile);
            var header = lines.First().Split(',');
            int currentcolumn = 0;
            for (int i = 0; i < lines.First().Split(',').Length; i++)
            {
                currentcolumn = i;
                if (header[i] == headerName)
                {
                    return new Tuple<bool, int>(true, currentcolumn);
                }
            }
            return new Tuple<bool, int>(false, currentcolumn);
        }
        public static List<string> GetDownloadedCSVColumnData(string fileName, int columnNumber)
        {
            string pathOfFile = null;
            pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;
            List<string> listOfRows = new List<string>();
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    lines[i] = split[columnNumber];
                    listOfRows.Add(lines[i]);
                }
            }
            return listOfRows;
        }

        public static bool DeleteExistingFile(string fileName)
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", fileName)))
            {
                try
                {
                    File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", fileName));
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }
            }
            return true;
        }
        public static void EmptyDownloadFolder()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads"));

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            catch
            {

            }
        }
        public static bool VerifyFileDownloaded(string fileName, int timeoutInSeconds = 10)
        {
            var timeout = DateTime.Now.Add(TimeSpan.FromSeconds(timeoutInSeconds));
            if (Directory.Exists(path))
            {
                var files = new DirectoryInfo(path).GetFiles(fileName + "*.*");
                foreach (var p in files)
                    while (!p.ToString().Equals(fileName))
                    {
                        if (DateTime.Now > timeout)
                        {
                            return false;
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                return true;
            }
            return false;
        }

        public static void RefreshPage(this IWebDriver driver)
        {
            driver.Navigate().Refresh();
            driver.WaitForPageLoaded();
            driver.WaitForAngularComplete(new TimeSpan(0, 0, 15));
        }
        public static string ExtractZipFile(string zipFileName, string fileName)
        {
            var filepath = path;
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(Path.Combine(filepath, zipFileName)))
            {
                ZipFile.ExtractToDirectory(Path.Combine(filepath, zipFileName), Path.Combine(filepath), true);
                return Path.Combine(filepath, fileName);
            }
        }
        public static int ZipFileCount(String zipFileName)
        {
            int count = 0;
            var filepath = path;
            using (ZipArchive archive = ZipFile.OpenRead(Path.Combine(filepath, zipFileName)))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!String.IsNullOrEmpty(entry.Name))
                        count += 1;
                }
                return count;
            }
        }
        public static bool DownloadFileFromWebClient(string url, string fileName)
        {
            try
            {
                new WebClient().DownloadFile(url, Path.Combine(path, fileName));
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool CreateDownloadDirectory()
        {
            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                    return false;
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                return true;
            }
        }
        public static void UpdateHtmlFileContent(this IWebDriver driver, string fileName)
        {
            var pagesource = driver.PageSource;
            System.IO.File.WriteAllText(Path.Combine(path, fileName), pagesource);
            driver.Navigate().Refresh();
        }
        public static void UpdateParentDetailofModelForSVin(string parentName, string parentUniqueTag, string pathOfFile)
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            string format = string.Format("{0:ddhhssmmss}", DateTime.Now);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[3] = parentName;
                    split[4] = parentUniqueTag;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);
        }
        public static string GetServerTime(string format, int interval)
        {
            try
            {
                NpgsqlConnection conn = new NpgsqlConnection("Server=ecatdqpsg01.ecat.leadventure.dev;Port=5432;" + "User Id=postgres;Password=arinet;Database=oem_arn2");
                conn.Open();
                string query = "select to_char(now() + INTERVAL '" + interval + " minutes', '" + format + "') as curtime";
                NpgsqlCommand command = new NpgsqlCommand(query, conn);
                var result = command.ExecuteScalar().ToString();
                conn.Close();
                return result;
            }
            catch
            { return null; };
        }
        public static void UpdateDetailOfImageImport(string pathOfFile, string filepath, string filename, string modelname = "", string modeltag = "")
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                filePrefix++;
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[0] = filepath;
                    split[1] = filename;
                    if (!string.IsNullOrEmpty(modelname))
                    {
                        split[3] = modelname;
                        split[4] = modeltag;
                    }
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);
        }
        public static List<string> GetWhereUsedCSVValues(string fileName)
        {
            string pathOfFile = null;
            pathOfFile = AppDomain.CurrentDomain.BaseDirectory + "Downloads\\" + fileName;

            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    string[] splitLine = line.Split(',');
                    line = string.Join(" ", splitLine[0], splitLine[1], splitLine[2]);
                    lines[i] = line;
                }
            }
            return lines.ToList();
        }

        public static int GenerateRandomFontSize()
        {
            int _min = 20;
            int _max = 99;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }
        public static void CloseNewTab(this IWebDriver driver)
        {
            driver.SwitchToOtherTab(1);
            driver.Close();
            driver.SwitchToOtherTab(0);
        }
        public static void UpdateParentDetailWithCreatedHierarchy(string parentName, string parentUniqueTag, string pathOfFile)
        {
            string[] lines = File.ReadAllLines(pathOfFile);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    split[7] = parentName;
                    split[8] = parentUniqueTag;
                    line = string.Join(",", split);
                    lines[i] = line;
                }
            }
            File.WriteAllLines(pathOfFile, lines);

        }
        #endregion
        public static string ExtractZipFile(string zipFileName)
        {
            string zippath = Path.Combine(path, zipFileName);
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(zippath))
            {
                ZipFile.ExtractToDirectory(zippath, Path.Combine(path, Path.GetFileNameWithoutExtension(zippath)));
                return Path.Combine(path, Path.GetFileNameWithoutExtension(zippath));
            }
        }

        public static bool PatternMatching(string actualCriteria, string expactedCriteria)
        {
            Regex pattern = new Regex(actualCriteria);
            return pattern.Match(expactedCriteria).Success;
        }
        public static string GetItemGUIDForImage(string ImageID)
        {
            SqlConnection dbcon = Settings.ApplicationCon.DBConnect("Data Source = ecatdsql01.ecat.leadventure.dev; Initial Catalog = DM_QA_ARN2_STG; Integrated Security = False; User Id = partstream; Password = PwVry94a628Z3x2m22T3; MultipleActiveResultSets = True");
            DataTable dt = dbcon.ExecuteQuery(string.Format("select i.guid from dm.item i join dm.item_base b on i.id = b.id where b.guid = '" + ImageID + "'"));
            dbcon.DBClose();
            if (dt.Rows.Count > 0)
                return Convert.ToString(dt.Rows[0][0]);
            else
                return null;
        }
        public static string RemoveAllExceptAlphanumeric(string data)
        {
            return Regex.Replace(data, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
        }
        public static string ValidateDateFormat(this IWebElement element)
        {
            string date = "13-13-2021";
            element.EnterText(date);
            return element.GetAttribute("value").Split('-')[2];
        }
        public static bool VerifyURLOpened(IWebDriver driver, string URL, int tabNumber)
        {
            driver.SwitchToOtherTab(tabNumber);
            string urlOpened = driver.Url;
            return urlOpened.Contains(URL);
        }
        public static string RemoveSymbol(string data)
        {
            var regex = new Regex(@"([\d,.]+)");

            var match = regex.Match(data);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return null;
            }
        }
        public static Dictionary<string, string> ToDictionary(Table table)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var row in table.Rows)
            {
                dictionary.Add(row[0], row[1]);
            }
            if (dictionary.Count > 1)
                return dictionary;
            else
                return null;
        }
        public static double RoundValue(double value, int decimalPlace)
        {
            return Math.Round(value, decimalPlace);
        }
        public static bool CompareDateTimeWithToleranceInSeconds(DateTime dateTime, DateTime otherDateTime, long toleranceInSeconds = -1)
        {
            if (toleranceInSeconds < 0)
            {
                toleranceInSeconds = 0;
            }
            return Math.Abs((dateTime - otherDateTime).TotalSeconds) < toleranceInSeconds;
        }

        public static string GetTextValueWithGivenLength(string text, string length)
        {
            var textValue = text;
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(length))
            {
                textValue = string.Concat(Enumerable.Repeat(text, int.Parse(length)));
            }
            return textValue;
        }
        public static List<string> GetDownloadedNonHeaderCSVColumnData(string fileNameWithPath, int columnNumber)
        {
            List<string> columnValues = new List<string>();
            string[] lines = File.ReadAllLines(fileNameWithPath);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains(","))
                {
                    var split = line.Split(',');
                    lines[i] = split[columnNumber];
                    columnValues.Add(lines[i].Trim());
                }
            }
            return columnValues;
        }



        public static void OpenNewTabAndSwitch(this IWebDriver driver)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
            driver.SwitchTo().Window(driver.WindowHandles.Last());
        }
        public static string RemoveAllWhiteSpaces(string data)
        {
            return Regex.Replace(data, @"\s+", "");
        }
        //for ChromeDriver
        public static void WaitForFileDownloadCompletionInLocal(this IWebDriver driver, string filepath, int maxWaitTimeInSec = 100)
        {
            driver.WaitForCondition(dir => { return Directory.GetFiles(filepath).Any(i => i.EndsWith(".crdownload")) == false; }, maxWaitTimeInSec);
        }

        public static string GetFullyQuailifiedDownloadedFileName(string fileName)
        {
            if (Directory.Exists(path))
            {
                var files = new DirectoryInfo(path).GetFiles();
                foreach (var file in files)
                {
                    if (file.Name.Contains(fileName))
                    {
                        return file.Name;
                    }
                }
            }
            return null;
        }
        public static bool GetAPIStatus(string url)
        {
            HttpStatusCode result = default(HttpStatusCode);

            var request = HttpWebRequest.Create(url);
            request.Method = "HEAD";
            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        result = response.StatusCode;
                        response.Close();
                    }
                }
                if (result == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }
            catch (WebException e)
            {
                return false;
            }
        }
        public static void WaitForDownloadInRemote(string url, int waitInSeconds = 100)
        {
            var startTime = DateTime.Now;

            while (DateTime.Now - startTime < TimeSpan.FromSeconds(waitInSeconds))
            {
                bool status = GetAPIStatus(url);
                if (status == true)
                    break;
            }
        }
        public static string ReplaceHttpInAUTURL()
        {
            return "https://" + Settings.AUT.Replace("http://", "");
        }
        public static List<string> GetBrowserError(this IWebDriver driver)
        {
            ILogs logs = driver.Manage().Logs;
            var logEntries = logs.GetLog(LogType.Browser); // LogType: Browser, Server, Driver, Client and Profiler
            List<string> errorLogs = logEntries.Where(x => x.Level == LogLevel.Severe).Select(x => x.Message).ToList();
            return errorLogs;
        }

        public static void SelectDropDownByRandomIndex(IWebElement element, int startIndex, int endIndex)
        {
            var dropDown = new SelectElement(element);
            var selectedOptionIndex = dropDown.Options.IndexOf(dropDown.SelectedOption);
            Random random = new Random();

            int randomIndex = random.Next(startIndex, endIndex);
            if (randomIndex == selectedOptionIndex)
            {
                randomIndex = random.Next(startIndex, endIndex);
            }
            dropDown.SelectByIndex(randomIndex);
        }
        public static void UploadFileToRemoteServerUsingSftp(string ftpServer, string username, string password, string sourcePath, string destinationPath)
        {
            try
            {
                using (var client = new SftpClient(ftpServer, port: 22, username, password))
                {
                    client.Connect();
                    using (var fileStream = new FileStream(sourcePath, FileMode.Open))
                    {
                        client.UploadFile(fileStream, destinationPath);
                    }
                   ;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static void DeleteFileFromRemoteServerUsingSftp(string ftpServer, string username, string password, string filePath)
        {
            try
            {
                using (var client = new SftpClient(ftpServer,port: 22, username, password))
                {
                    client.Connect();
                   if(client.Exists(filePath))
                    {
                        client.DeleteFile(filePath);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static bool VerifyFileOnRemoteServerUsingSftp(string ftpServer, string username, string password, string filePath)
        {
            try
            {
                using (var client = new SftpClient(ftpServer, port: 22, username, password))
                {
                    client.Connect();
                    return client.Exists(filePath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static void WaitForActionMiliSecond(int miliSecondWait)
        {
            Thread.Sleep(miliSecondWait);
        }
    }
}
