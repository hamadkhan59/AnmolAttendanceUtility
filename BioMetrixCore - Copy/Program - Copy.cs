using Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BioMetrixCore
{
    static class Program
    {
        static Dictionary<int, string> staffStatus = new Dictionary<int, string>();
        static DateTime lastDateTime = DateTime.Now;
        static bool isAppStart = true;
        static DeviceManipulator manipulator = new DeviceManipulator();
        static ZkemClient objZkeeper;
        static AttendanceForm form = new AttendanceForm("", "", "");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            while (true)
            {
                SendAttendanceData();
                //Thread.Sleep(100);
            }
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

        //static void SendPaymentData()
        //{
        //    try
        //    {
                
        //        string attendanceTime = "";
        //        DeviceManipulator manipulator = new DeviceManipulator();
        //        ZkemClient objZkeeper;
        //        bool isDeviceConnected = false;
        //        string ipAddress = ConfigurationManager.AppSettings["MachineIp"].ToString();
        //        string portNumber = ConfigurationManager.AppSettings["MachinePort"].ToString();
        //        string machineNumber = ConfigurationManager.AppSettings["MachineNumber"].ToString();
        //        string baseAddress = ConfigurationManager.AppSettings["BaseAddress"].ToString();
        //        string userAction = ConfigurationManager.AppSettings["UserAction"].ToString();
        //        string MinDiff = ConfigurationManager.AppSettings["MinDiff"].ToString();
        //        int minDiff = int.Parse(MinDiff);
        //        LogWriter.WriteLogStart("Reading Configurations...............................");

        //        objZkeeper = new ZkemClient(RaiseDeviceEvent);
        //        isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
        //        LogWriter.WriteLog("Connecting device at IP : PORT (" + ipAddress + ":" + portNumber + ")");

        //        if (isDeviceConnected)
        //        {
        //            LogWriter.WriteLog("Device is connected succesfully IP : PORT (" + ipAddress + ":" + portNumber + ")");

        //            ICollection<MachineInfo> lstMachineInfo = null;
        //            if (DateTime.Now.Hour >= 17)
        //            {
        //                lstMachineInfo = manipulator.GetCheckOutdata(objZkeeper, int.Parse(machineNumber));
        //            }
        //            else
        //            {
        //                lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(machineNumber));
        //            }

        //            lstMachineInfo = lstMachineInfo.Where(x => x.TimeOnlyRecord >= DateTime.Now.AddSeconds(minDiff)).ToList();
        //            LogWriter.WriteLog("Fetched device logs current count : " + (lstMachineInfo == null ? 0 : lstMachineInfo.Count));
        //            if (lstMachineInfo != null && lstMachineInfo.Count > 0)
        //            {
        //                lstMachineInfo = lstMachineInfo.OrderByDescending(x => x.TimeOnlyRecord).Take(1).ToList();
        //                using (var client = new HttpClient())
        //                {
        //                    foreach (MachineInfo info in lstMachineInfo)
        //                    {
        //                        attendanceTime = info.TimeOnlyRecord.Hour
        //                                + ":" + info.TimeOnlyRecord.Minute + ":" + info.TimeOnlyRecord.Second;
        //                        LogWriter.WriteLog("Getting Staff Attendance Status Staff Id : " + info.RegisterID);
        //                        //string message = GetStaffStatus(info.RegisterID);
        //                        LogWriter.WriteLog("Showing Staff Attendance Status");
        //                        ShowStatus(attendanceTime, "", info.RegisterID.ToString(), info.EnployeeName);
        //                    }
        //                }

        //            }

        //        }
        //        else
        //        {
        //            LogWriter.WriteLog("Error : Unable to connect devices at IP : PORT (" + ipAddress + ":" + portNumber + ")");
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        LogWriter.WriteLog("Exception" + exc.ToString());
        //    }
        //}

        static void SendAttendanceData()
        {
            try
            {

                string attendanceTime = "";
                
                bool isDeviceConnected = false;
                string ipAddress = ConfigurationManager.AppSettings["MachineIp"].ToString();
                string portNumber = ConfigurationManager.AppSettings["MachinePort"].ToString();
                string machineNumber = ConfigurationManager.AppSettings["MachineNumber"].ToString();
                string baseAddress = ConfigurationManager.AppSettings["BaseAddress"].ToString();
                string userAction = ConfigurationManager.AppSettings["UserAction"].ToString();
                string MinDiff = ConfigurationManager.AppSettings["MinDiff"].ToString();
                int minDiff = int.Parse(MinDiff);
                LogWriter.WriteLogStart("Reading Configurations...............................");

                if(objZkeeper == null)
                    objZkeeper = new ZkemClient(RaiseDeviceEvent);
                isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
                LogWriter.WriteLog("Connecting device at IP : PORT (" + ipAddress + ":" + portNumber + ")");

                if (isDeviceConnected)
                {
                    LogWriter.WriteLog("Device is connected succesfully IP : PORT (" + ipAddress + ":" + portNumber + ")");
                    
                    ICollection<MachineInfo> lstMachineInfo = null;
                    if (DateTime.Now.Hour >= 17)
                    {
                        lstMachineInfo = manipulator.GetCheckOutdata(objZkeeper, int.Parse(machineNumber));
                    }
                    else
                    {
                        lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(machineNumber));
                    }
                    if (isAppStart)
                    {
                        lastDateTime.AddMinutes(minDiff);
                        isAppStart = false;
                    }
                    //ICollection<MachineInfo> lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                    lstMachineInfo = lstMachineInfo.Where(x => x.TimeOnlyRecord >= lastDateTime).ToList();
                    LogWriter.WriteLog("Fetched device logs current count : " + (lstMachineInfo == null ? 0 : lstMachineInfo.Count));
                    if (lstMachineInfo != null && lstMachineInfo.Count > 0)
                    {
                        lstMachineInfo = lstMachineInfo.OrderByDescending(x => x.TimeOnlyRecord).Take(1).ToList();
                        LogWriter.WriteLog("Connecting API server");
                        foreach (MachineInfo info in lstMachineInfo)
                        {
                            if (info.TimeOnlyRecord != lastDateTime)
                            {
                                attendanceTime = info.TimeOnlyRecord.Hour
                                            + ":" + info.TimeOnlyRecord.Minute + ":" + info.TimeOnlyRecord.Second;
                                ShowStatus(attendanceTime, "", info.RegisterID.ToString(), info.EnployeeName);
                                lastDateTime = info.TimeOnlyRecord;
                            }
                        }
                    }

                    Thread thread = new Thread(() => sendDataToServer(lstMachineInfo, baseAddress, userAction));
                    thread.IsBackground = true;
                    thread.Start();

                }
                else
                {
                    LogWriter.WriteLog("Error : Unable to connect devices at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                }
            }
            catch (Exception exc)
            {
                LogWriter.WriteLog("Exception" + exc.ToString());
            }
        }

        private static void sendDataToServer(ICollection<MachineInfo> lstMachineInfo, string baseAddress, string userAction)
        {
            using (var client = new HttpClient())
            {
                int count = 0;
                LogWriter.WriteLog("Connection is established to server.");
                foreach (MachineInfo info in lstMachineInfo)
                {
                    string jsonString = "staffId=" + info.RegisterID + "&atteandanceDate=" + info.TimeOnlyRecord;
                    //client.BaseAddress = new Uri("http://localhost:1622/");
                    LogWriter.WriteLog("Sending data to server with parmeters : " + jsonString);
                    if (count == 0)
                        client.BaseAddress = new Uri(baseAddress);
                    //var response = client.GetAsync("api/Common/AuthenticateUser?" + jsonString).Result;
                    var response = client.GetAsync(userAction + jsonString).Result;
                    var a = response.Content.ReadAsStringAsync();
                   count++;
                }

                LogWriter.WriteLog("Success : Data transfer is finished : " + DateTime.Now);
            }
        }

        private static void ShowStatus(string attendanceTime, string result, string Id, string Name)
        {

            //string TIME_IN = "TIME_IN";
            //string TIME_OUT = "TIME_OUT";
            string message = "";

            //if (result.Contains(TIME_IN))
            //{
            //    message = "Time In : " + attendanceTime;
            //}
            //else if (result.Contains(TIME_OUT))
            //{
            //    message = "Time Out : " + attendanceTime;
            //}

            message = "Divice Time : " + attendanceTime;

            if (message.Length > 0)
            {
                
                System.Windows.Forms.Application.Run(new AttendanceForm(Id, Name, message));
                //AttendanceForm form = new AttendanceForm(Id, Name, message);
                //form.Show();
            }
        }

        private static string GetStaffStatus(int staffId)
        {
            string TIME_IN = "TIME_IN";
            string TIME_OUT = "TIME_OUT";

            if (!staffStatus.ContainsKey(staffId))
            {
                staffStatus.Add(staffId, TIME_IN);
            }

            staffStatus[staffId] = staffStatus[staffId] == TIME_IN ? TIME_OUT : TIME_IN;
            return staffStatus[staffId];
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
