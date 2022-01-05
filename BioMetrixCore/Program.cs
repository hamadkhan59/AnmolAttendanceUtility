using Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace BioMetrixCore
{
    static class Program
    {
        static DateTime lastDateTime = DateTime.Now;
        static bool isAppStart = true;
        static bool isConfigLoaded = false;
        static DeviceManipulator manipulator = new DeviceManipulator();
        static ZkemClient objZkeeper;
        static string ipAddress = "";
        static string portNumber = "";
        static string machineNumber = "";
        static string baseAddress = "";
        static string userAction = "";
        static string getCountAction = "";
        static string updateCountAction = "";
        static string MinDiff = "";
        //static string Id = "";
        //static string Name = "";
        //static string Message = "";
        //static bool isDeviceConnected = false;
        static string FormInterval = "";
        static int RunningCount = 0;
        static int LogsCountDifference = 0;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SendAttendanceData();
        }

        public static void LoadConfigs()
        {
            LogWriter.WriteLogStart("Reading Configurations...............................");
            ipAddress = ConfigurationManager.AppSettings["MachineIp"].ToString();
            portNumber = ConfigurationManager.AppSettings["MachinePort"].ToString();
            machineNumber = ConfigurationManager.AppSettings["MachineNumber"].ToString();
            baseAddress = ConfigurationManager.AppSettings["BaseAddress"].ToString();
            userAction = ConfigurationManager.AppSettings["UserAction"].ToString();
            MinDiff = ConfigurationManager.AppSettings["MinDiff"].ToString();
            FormInterval = ConfigurationManager.AppSettings["FormInterval"].ToString();
            getCountAction = ConfigurationManager.AppSettings["GetBioMatrixCount"].ToString();
            updateCountAction = ConfigurationManager.AppSettings["UpdateBioMatrixCount"].ToString();
            isConfigLoaded = true;
        }
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        static ICollection<MachineInfo> GetMachineInfoObject()
        {
            ICollection<MachineInfo> lstMachineInfo = new List<MachineInfo>();

            for (int i = 0; i < 100000; i++)
            {
                MachineInfo info = new MachineInfo();
                info.DateTimeRecord = DateTime.Now.ToString();
                info.EnployeeName = "Emp" + i;
                info.MachineNumber = 1;
                info.RegisterID = 1;
                lstMachineInfo.Add(info);
            }

            return lstMachineInfo;
        }

        static async void SendAttendanceData()
        {
            try
            {

                bool isDeviceConnected = false;
                LoadConfigs();
                objZkeeper = new ZkemClient(RaiseDeviceEvent);

                LogWriter.WriteLog("Connecting device at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
                LogWriter.WriteLog("Connected device at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                LogWriter.WriteLog("Machine Number : " + machineNumber);
                Console.WriteLine("===============================================================================");
                Console.WriteLine("           Attendance Upload is started, Date : " + DateTime.Now);
                Console.WriteLine("===============================================================================");

                if (isDeviceConnected)
                {

                    ICollection<MachineInfo> lstMachineInfo_filtered = manipulator.GetAllCheckOutdata(objZkeeper, int.Parse(machineNumber));
                    //lstMachineInfo_filtered = lstMachineInfo_filtered.OrderBy(x => x.DateTimeRecord).ToList();

                    int serverCount = await GetBioMatrixCount();
                    int machineCount = (lstMachineInfo_filtered == null ? 0 : lstMachineInfo_filtered.Count);
                    LogWriter.WriteLogStart("Fetched server logs current count : " + serverCount);
                    LogWriter.WriteLogStart("Fetched device logs current count : " + machineCount);
                    RunningCount = serverCount;
                    int LogsCountDifference = machineCount - serverCount;
                    if (LogsCountDifference > 0)
                    {
                        //ICollection<MachineInfo> lstMachineInfo_filtered1 = lstMachineInfo_filtered.Where(x => x.LogID > serverCount).OrderBy(x => x.DateTimeRecord).ToList();
                        ICollection<MachineInfo> lstMachineInfo_filtered1 = lstMachineInfo_filtered.Where(x => x.LogID > serverCount).ToList();
                        LogWriter.WriteLogStart("Logs to send on the server count : " + LogsCountDifference);
                        int retVal = sendDataToServer(lstMachineInfo_filtered1, LogsCountDifference);

                        if (retVal == 0)
                        {
                            Console.WriteLine("===============================================================================");
                            Console.WriteLine("                                SUCCESS!!!");
                            Console.WriteLine("-------------------------------------------------------------------------------");
                            Console.WriteLine("          Attendance upload is finished, Date : " + DateTime.Now);
                            Console.WriteLine("===============================================================================");
                            LogWriter.WriteLogStart("Attendance upload is finished with SUCCESS, Date : " + DateTime.Now);
                        }
                        else
                        {
                            Console.WriteLine("===============================================================================");
                            Console.WriteLine("                                ERROR!!!");
                            Console.WriteLine("-------------------------------------------------------------------------------");
                            Console.WriteLine("Error in uploading the Attendnace,Please Try again later, Date : " + DateTime.Now);
                            Console.WriteLine("===============================================================================");
                            LogWriter.WriteLogStart("Attendance upload is finished with ERROR, Date : " + DateTime.Now);
                        }
                    }
                    else
                    {
                        LogWriter.WriteLogStart("No Attendance Record to upload : " + LogsCountDifference);
                        Console.WriteLine("===============================================================================");
                        Console.WriteLine("          No Attendance Record to upload, Date : " + DateTime.Now);
                        Console.WriteLine("===============================================================================");
                    }
                }
                else
                {
                    Console.WriteLine("Error : Unable to connect devices at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                    LogWriter.WriteLog("Error : Unable to connect devices at IP : PORT (" + ipAddress + ":" + portNumber + ")");

                    Console.WriteLine("===============================================================================");
                    //Console.WriteLine("          Machine Error in Attendance Upload, Date : " + DateTime.Now);
                    Console.WriteLine("Error in uploading the Attendnace,Please Try again later, Date : " + DateTime.Now);
                    Console.WriteLine("===============================================================================");
                }
            }
            catch (Exception exc)
            {
                //isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());

                Console.WriteLine("===============================================================================");
                Console.WriteLine("           Error in Attendance Upload, Date : " + DateTime.Now);
                Console.WriteLine("===============================================================================");
            }
            finally
            {
                await UpdateBioMatrixCount(RunningCount);
            }
            Console.ReadLine();
        }

        private static int sendDataToServer(ICollection<MachineInfo> lstMachineInfo, int logsDiffCount)
        {
            int retVal = 0;
            try
            {
                int count = 0;
                int lastPercentage = 0;
                using (var client = new HttpClient())
                {
                    LogWriter.WriteLog("Connection is established to server.");
                    client.BaseAddress = new Uri(baseAddress);
                    LogWriter.WriteLog("Data transfer is started : " + DateTime.Now);
                    foreach (MachineInfo info in lstMachineInfo)
                    {
                        string jsonString = "staffId=" + info.RegisterID + "&attendanceDate=" + info.DateTimeRecord;
                        //LogWriter.WriteLog("Sending data to server with parmeters : " + jsonString);
                        var response = client.GetAsync(userAction + jsonString).Result;
                        var a = response.Content.ReadAsStringAsync();
                        RunningCount++;
                        count++;
                        int percentage = (count * 100) / logsDiffCount;
                        if (percentage > 0 && percentage % 10 == 0 && percentage != lastPercentage)
                        {
                            Console.WriteLine("Attendance uploaded on the server : " + percentage +"%");
                            LogWriter.WriteLog("Attendance uploaded on the server : " + percentage +"%");
                            lastPercentage = percentage;
                        }
                    }

                    LogWriter.WriteLog("Success : Data transfer is finished : " + DateTime.Now);
                }
            }
            catch (Exception exc)
            {
                retVal = 420;
                LogWriter.WriteLog("Error in data transfer, details are below : " + DateTime.Now);
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }

            return retVal;
        }



        private static async Task<int> GetBioMatrixCount()
        {
            int count = 0;
            try
            {
                LogWriter.WriteLog("Fetching BioMatrix count from the server");

                using (var client = new HttpClient())
                {
                    LogWriter.WriteLog("Connection is established to server.");
                    
                    client.BaseAddress = new Uri(baseAddress);
                    LogWriter.WriteLog("Preparing the request");
                    var response = client.GetAsync(getCountAction).Result;
                    var responseStr = await response.Content.ReadAsStringAsync();
                    responseStr = responseStr.Replace("\"", "");
                    count = int.Parse(responseStr);
                    LogWriter.WriteLog("Received the response");
                }
                LogWriter.WriteLog("Logs count is fetched from the server : " + count);
            }
            catch (Exception exc)
            {
                LogWriter.WriteLog("Error in fetching the log count from the server, details are below");
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }

            return count;
        }

        private static async Task<string> UpdateBioMatrixCount(int count)
        {
            string funcResponse = "";
            try
            {
                LogWriter.WriteLog("Updating BioMatrix count on the server");
                using (var client = new HttpClient())
                {
                    LogWriter.WriteLog("Connection is established to server.");

                    client.BaseAddress = new Uri(baseAddress);
                    string jsonString = "count=" + count;
                    LogWriter.WriteLog("Preparing the request");
                    var response = client.GetAsync(updateCountAction + jsonString).Result;
                    funcResponse = await response.Content.ReadAsStringAsync();
                    LogWriter.WriteLog("Received the response");
                }
                LogWriter.WriteLog("Logs count is updated on the server");
            }
            catch (Exception exc)
            {
                LogWriter.WriteLog("Error in updating the logs count on the server, details are below");
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }
            return funcResponse;
        }

        private static void ShowStatus(string attendanceTime, string result, string Id, string Name)
        {
            try
            {
                string message = "";

                message = "Divice Time : " + attendanceTime;

                if (message.Length > 0)
                {
                    System.Windows.Forms.Application.Run(new AttendanceForm(Id, Name, message, FormInterval));
                }
            }
            catch (Exception exc)
            {
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }
        }

        private static void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        break;
                    }

                default:
                    break;
            }

        }
    }
}
