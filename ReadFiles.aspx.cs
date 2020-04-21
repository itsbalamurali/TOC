﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Threading;
using System.Data;
using System.IO;
using System.Timers;
//using System.Security.AccessControl;
//using System.Security.Principal;

namespace TOC
{
    public partial class ReadFiles : System.Web.UI.Page
    {
        private static string FOLDER_PATH = "C:\\Myfiles\\";
        private static string ORDERS_FILE_NAME = "orders.csv";
        private static string MY_ORDERS_FILE_NAME = "myorders.csv";
        private static string LOG_FILE_NAME = "LogFile.csv";
        private static string GROUPS_AND_CHANNELS = "groupschannels.csv";
        private static string ORDERS_FILE_PATH = "C:\\autotrader\\data\\order\\" + ORDERS_FILE_NAME;
        //private static string MY_ORDERS_FILE_PATH = "C:\\inetpub\\wwwroot\\sendorders\\";
        private static string MY_ORDERS_FILE_PATH1 = "C:\\Abhay\\SFDC\\Git\\Others\\TOC\\TOC\\";
        private static string MY_ORDERS_FILE_PATH2 = "C:\\autotrader\\data\\order\\";
        private static string MY_ORDERS_FILE_PATH3 = "C:\\Myfiles\\";
        private static string MY_ORDERS_FILE_PATH4 = "C:\\inetpub\\wwwroot\\sendorders\\";
        private static string GROUPS_AND_CHANNELS_FILE_PATH = FOLDER_PATH + GROUPS_AND_CHANNELS;
        private static string FileWatcherPath = @"C:\autotrader\data\order\";
        //DataTable oldOrdersdt = new DataTable();
        //DataTable newOrdersdt = new DataTable();
        //DataTable alreadySentCallsdt = new DataTable();
        private static System.Timers.Timer aTimer;
        private int TimeoutMillis = 5000; //1000 is 1 sec
        private int TimerInterval = 60000; //60 seconds
        System.Threading.Timer m_timer = null;
        List<String> files = new List<string>();
        private string[] strGroupsChannelsList;
        TimeSpan timePreTradingStarting = new TimeSpan(8, 59, 0);
        TimeSpan timeTradingStarting = new TimeSpan(9, 14, 0);
        TimeSpan timeStartClosePosition = new TimeSpan(15, 0, 0);
        TimeSpan timeClosePosition = new TimeSpan(15, 15, 0);
        TimeSpan timeEquityReport = new TimeSpan(15, 35, 0);
        TimeSpan timeCommodityReport = new TimeSpan(23, 59, 0);
        //TimeSpan timeStartClosePosition = new TimeSpan(13, 25, 0);
        //TimeSpan timeClosePosition = new TimeSpan(13, 26, 0);
        //TimeSpan timeEquityReport = new TimeSpan(23, 55, 0);
        //TimeSpan timeCommodityReport = new TimeSpan(13, 28, 0);
        DataTable logdt = new DataTable();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                //Page.LoadComplete += Page_LoadComplete;
                AddLogColumns(logdt);

                var wh = new AutoResetEvent(false);
                var fileSystemWatcher = new FileSystemWatcher();
                fileSystemWatcher.Path = FileWatcherPath;
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                //fileSystemWatcher.NotifyFilter = System.IO.NotifyFilters.FileName;
                //fileSystemWatcher.Filter = "*.csv";
                //ThreadPool.SetMaxThreads(1, 1);
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Filter = "orders.csv";
                //wh.Set();

                if (m_timer == null)
                {
                    m_timer = new System.Threading.Timer(new System.Threading.TimerCallback(OnWatchedFileChange), null, Timeout.Infinite, Timeout.Infinite);
                }

                //Get the list of groups and channels where Telegram message will be posted if these 
                //group and channels have added the bot
                strGroupsChannelsList = ReadGroupsChannelsCsvFile();
                string groupchannelId = string.Empty;
                foreach (var groupId in strGroupsChannelsList)
                {
                    groupchannelId += groupId + " ";
                }
                lblGroupsChannels.Text = groupchannelId;

                ///Timer events
                // Create a timer and set a two second interval.
                aTimer = new System.Timers.Timer();
                aTimer.Interval = TimerInterval;

                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;

                // Have the timer fire repeated events (true is the default)
                aTimer.AutoReset = true;

                // Start the timer
                aTimer.Enabled = true;
            }

            if (logdt.Columns.Count == 0)
            {
                AddLogColumns(logdt);
            }
        }

        private void AddLogColumns(DataTable logdt)
        {
            logdt.Columns.Add("DateTime");
            logdt.Columns.Add("Activity");
            logdt.Columns.Add("ExceptionName");
            logdt.Columns.Add("ExceptionDesc");
        }
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            TimeSpan addOneMinute = new TimeSpan(0, 1, 0);
            if (e.SignalTime.TimeOfDay > timePreTradingStarting && e.SignalTime.TimeOfDay < timePreTradingStarting.Add(addOneMinute))
            {
                PreTradingStarting();
            }
            if (e.SignalTime.TimeOfDay > timeTradingStarting && e.SignalTime.TimeOfDay < timeTradingStarting.Add(addOneMinute))
            {
                TradingStarting();
            }
            if (e.SignalTime.TimeOfDay > timeStartClosePosition && e.SignalTime.TimeOfDay < timeStartClosePosition.Add(addOneMinute))
            {
                StartClosePosition();
            }
            if (e.SignalTime.TimeOfDay > timeClosePosition && e.SignalTime.TimeOfDay < timeClosePosition.Add(addOneMinute))
            {
                ClosePosition();
            }
            if (e.SignalTime.TimeOfDay > timeEquityReport && e.SignalTime.TimeOfDay < timeEquityReport.Add(addOneMinute))
            {
                EquityReport();
            }
            if (e.SignalTime.TimeOfDay > timeCommodityReport && e.SignalTime.TimeOfDay < timeCommodityReport.Add(addOneMinute))
            {
                CommodityReport();
            }
            //throw new NotImplementedException();
        }

        private void PreTradingStarting()
        {
            string teleMessage = string.Empty;
            teleMessage = "PRE-TRADING OF MARKETS IS STARTING.\n";
            if (strGroupsChannelsList == null)
            {
                strGroupsChannelsList = ReadGroupsChannelsCsvFile();
            }
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }

        private void TradingStarting()
        {
            string teleMessage = string.Empty;
            teleMessage = "STOCK MARKETS ARE OPEN FOR TRADING NOW.\n";
            if (strGroupsChannelsList == null)
            {
                strGroupsChannelsList = ReadGroupsChannelsCsvFile();
            }
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }

        private void CommodityReport()
        {
            string teleMessage = string.Empty;
            string exhange = enumExchange.MCX.ToString();
            teleMessage = "COMMODITY REPORT :-\n";
            teleMessage += GetReport(exhange);

            if (strGroupsChannelsList == null)
            {
                strGroupsChannelsList = ReadGroupsChannelsCsvFile();
            }
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }

        private void EquityReport()
        {
            string teleMessage = string.Empty;
            string exhange = enumExchange.NSE.ToString();
            teleMessage = "EQUITY REPORT :-\n";
            teleMessage += GetReport(exhange);

            if (strGroupsChannelsList == null)
            {
                strGroupsChannelsList = ReadGroupsChannelsCsvFile();
            }
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }
        private string GetReport(string exhange)
        {
            string teleMessage = string.Empty;
            DataTable mySentOrderdt = new DataTable();
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH1))
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH1 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH2))
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH2 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH3))
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH3 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH4))
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH4 + MY_ORDERS_FILE_NAME));
            }


            DataTable dtCsv = new DataTable();
            dtCsv.Columns.Add("symbol");
            dtCsv.Columns.Add("BuyDate");
            dtCsv.Columns.Add("BuyTradeType");
            dtCsv.Columns.Add("BuyPrice");
            dtCsv.Columns.Add("SellDate");
            dtCsv.Columns.Add("SellTradeType");
            dtCsv.Columns.Add("SellPrice");
            dtCsv.Columns.Add("qty");
            dtCsv.Columns.Add("PositionStatus");
            dtCsv.Columns.Add("PositionType");
            dtCsv.Columns.Add("ProfitLossPercent");
            bool isPresent = false;
            //bool isSellPresent = false;
            foreach (DataRow sentOrdersRow in mySentOrderdt.Rows)
            {
                if (sentOrdersRow["exchange"].ToString().Equals(exhange))
                {
                    foreach (DataRow row in dtCsv.Rows)
                    {
                        if (string.Compare(row["symbol"].ToString().Trim(), sentOrdersRow["symbol"].ToString().Trim()) == 0)
                        {
                            isPresent = true;
                            if (string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0 &&
                                string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) != 0 &&
                                string.Compare(sentOrdersRow["TradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) == 0)
                            {
                                //DataRow newdr = dtCsv.NewRow();
                                row["symbol"] = sentOrdersRow["symbol"].ToString().Trim();
                                row["SellTradeType"] = sentOrdersRow["TradeType"].ToString().Trim();
                                row["SellPrice"] = sentOrdersRow["priceStr"];
                                row["qty"] = sentOrdersRow["qty"];
                                row["PositionStatus"] = enumPositionStatus.Close.ToString();
                                row["PositionType"] = enumPositionType.Long.ToString();
                            }
                            else if (string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) != 0 &&
                                    string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) == 0 &&
                                    string.Compare(sentOrdersRow["TradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0)
                            {
                                row["symbol"] = sentOrdersRow["symbol"].ToString().Trim();
                                row["BuyTradeType"] = sentOrdersRow["TradeType"].ToString().Trim();
                                row["BuyPrice"] = sentOrdersRow["priceStr"];
                                row["qty"] = sentOrdersRow["qty"];
                                row["PositionStatus"] = enumPositionStatus.Close.ToString();
                                row["PositionType"] = enumPositionType.Short.ToString();
                            }
                            if (string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0 &&
                                string.Compare(row["SellTradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) == 0)
                            {
                                if (string.Compare(row["PositionType"].ToString().Trim(), enumPositionType.Long.ToString()) == 0)
                                {
                                    row["ProfitLossPercent"] = Math.Round(((Convert.ToDouble(row["SellPrice"]) - Convert.ToDouble(row["BuyPrice"])) * 100 / Convert.ToDouble(row["BuyPrice"])), 2);
                                }
                                else if (string.Compare(row["PositionType"].ToString().Trim(), enumPositionType.Short.ToString()) == 0)
                                {
                                    row["ProfitLossPercent"] = Math.Round(((Convert.ToDouble(row["BuyPrice"]) - Convert.ToDouble(row["SellPrice"])) * 100 / Convert.ToDouble(row["SellPrice"])), 2);
                                }
                            }
                        }
                        continue;
                    }
                }
                if (!isPresent)
                {
                    DataRow newdr = dtCsv.NewRow();
                    if (string.Compare(sentOrdersRow["TradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) == 0)
                    {
                        newdr["symbol"] = sentOrdersRow["symbol"].ToString().Trim();
                        newdr["SellTradeType"] = sentOrdersRow["TradeType"].ToString().Trim();
                        newdr["SellPrice"] = sentOrdersRow["priceStr"];
                        newdr["qty"] = sentOrdersRow["qty"];
                        newdr["PositionStatus"] = enumPositionStatus.Open.ToString();
                        newdr["PositionType"] = enumPositionType.Short.ToString();
                    }
                    else if (string.Compare(sentOrdersRow["TradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0)
                    {
                        newdr["symbol"] = sentOrdersRow["symbol"].ToString().Trim();
                        newdr["BuyTradeType"] = sentOrdersRow["TradeType"].ToString().Trim();
                        newdr["BuyPrice"] = sentOrdersRow["priceStr"];
                        newdr["qty"] = sentOrdersRow["qty"];
                        newdr["PositionStatus"] = enumPositionStatus.Open.ToString();
                        newdr["PositionType"] = enumPositionType.Long.ToString();
                    }
                    dtCsv.Rows.Add(newdr);
                }
            }

            foreach (DataRow row in dtCsv.Rows)
            {
                if (string.Compare(row["PositionStatus"].ToString().Trim(), enumPositionStatus.Open.ToString()) == 0)
                {
                    if (string.Compare(row["BuyTradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0)
                    {
                        teleMessage += row["symbol"] + ": " + "BuyPrice: " + Math.Round(Convert.ToDouble(row["BuyPrice"]), 2) + " Position: " + row["PositionStatus"] + "\n";
                    }
                    else
                    {
                        teleMessage += row["symbol"] + ": " + "SellPrice: " + Math.Round(Convert.ToDouble(row["SellPrice"]), 2) + " Position: " + row["PositionStatus"] + "\n";
                    }
                }
                else
                {
                    double percentage = Convert.ToDouble(row["ProfitLossPercent"]);
                    if (percentage > 0)
                    {
                        teleMessage += row["symbol"] + ": " + "BuyPrice: " + Math.Round(Convert.ToDouble(row["BuyPrice"]), 2) + " SellPrice: " + Math.Round(Convert.ToDouble(row["SellPrice"]), 2) + " Profit: " + row["ProfitLossPercent"] + "%\n";
                    }
                    else
                    {
                        teleMessage += row["symbol"] + ": " + "BuyPrice: " + Math.Round(Convert.ToDouble(row["BuyPrice"]), 2) + " SellPrice: " + Math.Round(Convert.ToDouble(row["SellPrice"]), 2) + " Loss: " + row["ProfitLossPercent"] + "%\n";
                    }
                }
            }
            return teleMessage;
        }

        private void ClosePosition()
        {
            string teleMessage = string.Empty;
            teleMessage = "PLEASE CLOSE YOUR INTRADAY PROSITIONS NOW (OR CONVERT TO DELIVERY)\n" +
                "Some brokers charge addtional fees for closing your open intraday positions.";
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }

        private void StartClosePosition()
        {
            string teleMessage = string.Empty;
            teleMessage = "PLEASE START CLOSING YOUR INTRADAY PROSITIONS (OR CONVERT TO DELIVERY) AND AVOID TAKING ANY ADDITIONAL POSITION\n" +
                "Some brokers charge addtional fees for closing your open intraday positions.";
            Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Mutex mutex = new Mutex(false, "FSW");
            mutex.WaitOne();

            if (e.Name == ORDERS_FILE_NAME && !files.Contains(e.Name))
            {
                files.Add(e.Name);
            }

            mutex.ReleaseMutex();
            m_timer.Change(TimeoutMillis, Timeout.Infinite);
        }

        //add your file processing here
        private void OnWatchedFileChange(object state)
        {
            List<String> backup = new List<string>();
            Mutex mutex = new Mutex(false, "FSW");
            mutex.WaitOne();
            backup.AddRange(files);
            files.Clear();

            foreach (string file in backup)
            {
                FindNewOrders();
            }

            mutex.ReleaseMutex();

            ViewState["LogTable"] = logdt;
            ShowLog();

            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH3))
            {
                WriteDataTable(logdt, MY_ORDERS_FILE_PATH3 + LOG_FILE_NAME);
            }
        }

        protected void btnReadOrders_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable newdt = new DataTable();
                newdt = ReadOrdersCsvFile();

                //PrepareTelegramMessage(newdt, olddt);

                //olddt = newdt.Copy();

                gvFileData.DataSource = newdt;
                gvFileData.DataBind();
            }
            catch (Exception ex)
            {
                //lblerror.Text = ex.Message;
            }
        }

        private void FindNewOrders()
        {
            DataTable currentOrdersdt = RemoveBlankRows(ReadOrdersCsvFile());
            logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "FindNewOrders", "ReadOrdersCsvFile", "Row count: " + currentOrdersdt.Rows.Count);

            DataTable mySentOrderdt = new DataTable();
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH1) && mySentOrderdt.Rows.Count == 0)
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH1 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH2) && mySentOrderdt.Rows.Count == 0)
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH2 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH3) && mySentOrderdt.Rows.Count == 0)
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH3 + MY_ORDERS_FILE_NAME));
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH4) && mySentOrderdt.Rows.Count == 0)
            {
                mySentOrderdt = RemoveBlankRows(ReadMyOrdersCsvFile(MY_ORDERS_FILE_PATH4 + MY_ORDERS_FILE_NAME));
            }

            logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "FindNewOrders", "ReadMyOrdersCsvFile", "Row count: " + mySentOrderdt.Rows.Count);
            //newOrdersdt = currentOrdersdt.Clone();

            DataTable addtoSentOrderdt = currentOrdersdt.Clone();
            string teleMessage = string.Empty;

            //compare new file data with old file data to find new orders in the file
            foreach (DataRow currentOrdersRow in currentOrdersdt.Rows)
            {
                //set the flag to false for each new order
                bool isMessageNotAlreadySent = true;

                if (string.Compare(currentOrdersRow["AT_PLACE_ORDER_CMD"].ToString().Trim(), "PLACE_ORDER") == 0 &&
                currentOrdersRow["symbol"].ToString().Length > 1 &&
                ((string.Compare(currentOrdersRow["tradeType"].ToString().Trim(), enumTransactionType.BUY.ToString()) == 0) ||
                (string.Compare(currentOrdersRow["tradeType"].ToString().Trim(), enumTransactionType.SELL.ToString()) == 0)))
                {
                    foreach (DataRow SentOrderRow in mySentOrderdt.Rows)
                    {
                        if (string.Compare(currentOrdersRow["AT_PLACE_ORDER_CMD"].ToString().Trim(), SentOrderRow["AT_PLACE_ORDER_CMD"].ToString().Trim()) == 0 &&
                            string.Compare(currentOrdersRow["symbol"].ToString().Trim(), SentOrderRow["symbol"].ToString().Trim()) == 0 &&
                            string.Compare(currentOrdersRow["tradeType"].ToString().Trim(), SentOrderRow["tradeType"].ToString().Trim()) == 0 &&
                            string.Compare(currentOrdersRow["priceStr"].ToString().Trim(), SentOrderRow["priceStr"].ToString().Trim()) == 0)
                        {
                            isMessageNotAlreadySent = false;
                        }
                    }
                }
                else
                {
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "FindNewOrders", "currentOrdersRow else condition", "Row details: " + mySentOrderdt);
                }

                if (isMessageNotAlreadySent)
                {
                    if (currentOrdersRow["tradeType"] != null &&
                        currentOrdersRow["symbol"] != null &&
                        currentOrdersRow["targetStr"] != null &&
                        currentOrdersRow["stoplossStr"] != null &&
                        currentOrdersRow["trailingStoplossStr"] != null &&
                        currentOrdersRow["priceStr"] != null)
                    {
                        //Telegram message to be sent...
                        teleMessage = "TREND ALERT:-\n" +
                                        currentOrdersRow["tradeType"].ToString() + " " + currentOrdersRow["symbol"].ToString() + " AT " + Math.Round(Convert.ToDouble(currentOrdersRow["priceStr"]), 1) + "\n" +
                                        "TARGET " + Math.Round(Convert.ToDouble(currentOrdersRow["targetStr"]), 1) + "\n" +
                                        "STOP LOSS " + Math.Round(Convert.ToDouble(currentOrdersRow["stoplossStr"]), 1) + "\n" +
                                        "TRAILING SL " + Math.Round(Convert.ToDouble(currentOrdersRow["trailingStoplossStr"]), 1) + " POINTS";

                        //Send telegram message
                        Telegram.SendTelegramMessage(teleMessage, strGroupsChannelsList);

                        //Add sent messages to the list
                        addtoSentOrderdt.Rows.Add(currentOrdersRow.ItemArray);
                    }
                    else
                    {
                        logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "FindNewOrders", "isMessageNotAlreadySent is false", "Row details: " + mySentOrderdt);
                    }
                }
            }

            mySentOrderdt.Merge(addtoSentOrderdt);
            mySentOrderdt.AcceptChanges();
            //string filePath = MY_ORDERS_FILE_PATH;

            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH1))
            {
                WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH1 + MY_ORDERS_FILE_NAME);
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH2))
            {
                WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH2 + MY_ORDERS_FILE_NAME);
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH3))
            {
                WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH3 + MY_ORDERS_FILE_NAME);
            }
            if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH4))
            {
                WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH4 + MY_ORDERS_FILE_NAME);
            }
            //if (hasWriteAccessToFolder(MY_ORDERS_FILE_PATH4))
            //{
            //    WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH4 + MY_ORDERS_FILE_NAME);
            //}
            //WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH1 + MY_ORDERS_FILE_NAME);
            //WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH2 + MY_ORDERS_FILE_NAME);
            //WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH3 + MY_ORDERS_FILE_NAME);
            //WriteDataTable(mySentOrderdt, MY_ORDERS_FILE_PATH4 + MY_ORDERS_FILE_NAME);
        }

        private bool hasWriteAccessToFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return false;
            try
            {
                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(folderPath);
                //logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "hasWriteAccessToFolder ", "Have folder access ", "Folder Path: " + folderPath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "hasWriteAccessToFolder ", "Exception ", ex.Message + "Folder Path: " + folderPath);
                return false;
            }
        }

        //public static bool HasFolderWritePermission(string folderPath)
        //{
        //    if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        //        return false;
        //    try
        //    {
        //        DirectorySecurity security = Directory.GetAccessControl(folderPath);
        //        SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
        //        foreach (AuthorizationRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        //        {
        //            if (rule.IdentityReference == users)
        //            {
        //                FileSystemAccessRule rights = ((FileSystemAccessRule)rule);
        //                if (rights.AccessControlType == AccessControlType.Allow)
        //                {
        //                    if (rights.FileSystemRights == (rights.FileSystemRights | FileSystemRights.Modify)) return true;
        //                }
        //            }
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public void WriteDataTable(DataTable sourceTable, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    IEnumerable<String> items = null;
                    foreach (DataRow row in sourceTable.Rows)
                    {
                        items = row.ItemArray.Select(o => QuoteValue(o?.ToString() ?? String.Empty));
                        writer.WriteLine(String.Join(",", items));
                    }
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "WriteDataTable", "Write to file", "filePath: " + filePath);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "WriteDataTable", "Exception", ex.Message);
            }
        }

        private DataTable RemoveBlankRows(DataTable withBlankRowsdt)
        {
            DataTable withoutBlankRowsdt = withBlankRowsdt.Clone();
            foreach (DataRow row in withBlankRowsdt.Rows)
            {
                if (row["AT_PLACE_ORDER_CMD"].ToString().Trim().Length >= 3 ||
                    row["symbol"].ToString().Trim().Length >= 3 ||
                    row["tradeType"].ToString().Trim().Length >= 3 ||
                    row["productType"].ToString().Trim().Length >= 3 ||
                    row["orderType"].ToString().Trim().Length >= 3)
                {
                    withoutBlankRowsdt.Rows.Add(row.ItemArray);
                }
                else
                {
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "RemoveBlankRows", "Blank Row", row);
                }
            }
            return withoutBlankRowsdt;
        }

        private static string QuoteValue(string value)
        {
            string str = String.Concat("", value.Replace("\r\r", "\r"), "");
            return str;
        }

        public string[] ReadGroupsChannelsCsvFile()
        {
            string[] rowValues = { };
            string Fulltext;
            try
            {
                using (StreamReader sr = new StreamReader(GROUPS_AND_CHANNELS_FILE_PATH))
                {
                    while (!sr.EndOfStream)
                    {
                        Fulltext = sr.ReadToEnd().ToString(); //read full file text  
                        string[] rows = Fulltext.Split('\n'); //split full file text into rows  

                        for (int i = 0; i < rows.Count(); i++)
                        {
                            rowValues = rows[i].Split(','); //split each row with comma to get individual values
                        }
                    }
                    sr.Close();
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadGroupsChannelsCsvFile", "Reader", "Path: " + GROUPS_AND_CHANNELS_FILE_PATH);
                }
            }
            catch (Exception ex)
            {
                logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadGroupsChannelsCsvFile", "Exception", ex.Message);
            }
            return rowValues;
        }

        public DataTable ReadMyOrdersCsvFile(string filePath)
        {
            DataTable dtCsv = AddColumns();
            string Fulltext;
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    while (!sr.EndOfStream)
                    {
                        Fulltext = sr.ReadToEnd().ToString(); //read full file text  
                        string[] rows = Fulltext.Split('\n'); //split full file text into rows 

                        //if (dtCsv.Columns.Count == 0)
                        //{
                        //    dtCsv = AddColumns();
                        //}

                        for (int i = 0; i < rows.Count(); i++)
                        {
                            string[] rowValues = rows[i].Split(','); //split each row with comma to get individual values
                            {
                                DataRow dr = dtCsv.NewRow();
                                for (int k = 0; k < rowValues.Count(); k++)
                                {
                                    dr[k] = rowValues[k].ToString().Replace("\r", "");
                                }
                                dtCsv.Rows.Add(dr); //add other rows
                            }
                        }
                    }
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadMyOrdersCsvFile", "Reader", "Path: " + filePath);
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadMyOrdersCsvFile", "Exception", ex.Message);
            }
            return dtCsv;
        }

        private DataTable AddColumns()
        {
            DataTable dtCsv = new DataTable();
            dtCsv.Columns.Add("AT_PLACE_ORDER_CMD");
            dtCsv.Columns.Add("id");
            dtCsv.Columns.Add("symbol");
            dtCsv.Columns.Add("tradeType");
            dtCsv.Columns.Add("productType");
            dtCsv.Columns.Add("orderType");
            dtCsv.Columns.Add("qty");
            dtCsv.Columns.Add("priceStr");
            dtCsv.Columns.Add("triggerPriceStr");
            dtCsv.Columns.Add("disclosedQuantity");
            dtCsv.Columns.Add("exchange");
            dtCsv.Columns.Add("instrument");
            dtCsv.Columns.Add("optionType");
            dtCsv.Columns.Add("strikePriceStr");
            dtCsv.Columns.Add("expiry");
            dtCsv.Columns.Add("clientId");
            dtCsv.Columns.Add("validity");
            dtCsv.Columns.Add("traderType");
            dtCsv.Columns.Add("marketProtectionPctStr");
            dtCsv.Columns.Add("strategyId");
            dtCsv.Columns.Add("publishTime");
            dtCsv.Columns.Add("commentsStr");
            dtCsv.Columns.Add("variety");
            dtCsv.Columns.Add("targetStr");
            dtCsv.Columns.Add("stoplossStr");
            dtCsv.Columns.Add("trailingStoplossStr");
            return dtCsv;
        }

        public DataTable ReadOrdersCsvFile()
        {
            DataTable dtCsv = new DataTable();
            string Fulltext;
            try
            {
                using (StreamReader sr = new StreamReader(ORDERS_FILE_PATH))
                {
                    while (!sr.EndOfStream)
                    {
                        Fulltext = sr.ReadToEnd().ToString(); //read full file text  
                        string[] rows = Fulltext.Split('\n'); //split full file text into rows

                        dtCsv = AddColumns();

                        //dtCsv.Columns.Add("AT_PLACE_ORDER_CMD");
                        //dtCsv.Columns.Add("id");
                        //dtCsv.Columns.Add("symbol");
                        //dtCsv.Columns.Add("tradeType");
                        //dtCsv.Columns.Add("productType");
                        //dtCsv.Columns.Add("orderType");
                        //dtCsv.Columns.Add("qty");
                        //dtCsv.Columns.Add("priceStr");
                        //dtCsv.Columns.Add("triggerPriceStr");
                        //dtCsv.Columns.Add("disclosedQuantity");
                        //dtCsv.Columns.Add("exchange");
                        //dtCsv.Columns.Add("instrument");
                        //dtCsv.Columns.Add("optionType");
                        //dtCsv.Columns.Add("strikePriceStr");
                        //dtCsv.Columns.Add("expiry");
                        //dtCsv.Columns.Add("clientId");
                        //dtCsv.Columns.Add("validity");
                        //dtCsv.Columns.Add("traderType");
                        //dtCsv.Columns.Add("marketProtectionPctStr");
                        //dtCsv.Columns.Add("strategyId");
                        //dtCsv.Columns.Add("publishTime");
                        //dtCsv.Columns.Add("commentsStr");
                        //dtCsv.Columns.Add("variety");
                        //dtCsv.Columns.Add("targetStr");
                        //dtCsv.Columns.Add("stoplossStr");
                        //dtCsv.Columns.Add("trailingStoplossStr");

                        for (int i = 0; i < rows.Count(); i++)
                        {
                            string[] rowValues = rows[i].Split(','); //split each row with comma to get individual values
                            {
                                DataRow dr = dtCsv.NewRow();
                                for (int k = 0; k < rowValues.Count(); k++)
                                {
                                    dr[k] = rowValues[k].ToString();
                                }
                                dtCsv.Rows.Add(dr); //add other rows
                            }
                        }
                    }
                    logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadOrdersCsvFile", "Reader", "Path: " + ORDERS_FILE_PATH);
                }
            }
            catch (Exception ex)
            {
                logdt.Rows.Add(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), "ReadOrdersCsvFile", "Exception", ex.Message);
            }
            return dtCsv;
        }

        protected void btnEquityReport_Click(object sender, EventArgs e)
        {
            EquityReport();
        }

        protected void btnShowLogs_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void Page_LoadComplete(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void ShowLog()
        {
            if (ViewState["LogTable"] != null)
            {
                logdt = (DataTable)ViewState["LogTable"];
            }
            gvLog.DataSource = logdt;
            gvLog.DataBind();
        }
    }
}