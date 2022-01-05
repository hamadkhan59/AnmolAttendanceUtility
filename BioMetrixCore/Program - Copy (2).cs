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
        static string MinDiff = "";
        //static string Id = "";
        //static string Name = "";
        //static string Message = "";
        //static bool isDeviceConnected = false;
        static string FormInterval = "";
        static bool statusFlag = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //System.Windows.Forms.Application.EnableVisualStyles();
            //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            //System.Windows.Forms.Application.Run(new AttendanceForm("1", "1", "1"));
            while (true)
            {
                SendAttendanceData();

                //if (statusFlag)
                //{
                //    //System.Windows.Forms.Application.Run(new AttendanceForm(Id, Name, Message, FormInterval));
                //    //System.Windows.Forms.Application.Run(new Form1());
                //    AttendanceForm form = new AttendanceForm(Id, Name, Message, FormInterval);
                //    form.Show();
                //    statusFlag = false;
                //}

            }
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

        static void SendAttendanceData()
        {
            try
            {

                string attendanceTime = "";
                bool isDeviceConnected = false;
                if (!isConfigLoaded)
                    LoadConfigs();

                int minDiff = int.Parse(MinDiff);

                //DeviceManipulator manipulator = new DeviceManipulator();
                //ZkemClient objZkeeper = new ZkemClient(RaiseDeviceEvent);
                if (objZkeeper == null)
                    objZkeeper = new ZkemClient(RaiseDeviceEvent);
                //if (!isDeviceConnected)
                //{
                LogWriter.WriteLog("Connecting device at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
                LogWriter.WriteLog("Connected device at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                LogWriter.WriteLog("Machine Number : " + machineNumber);
                //}
                //isDeviceConnected = true;

                //ICollection<MachineInfo> lstMachineInfo1 = null;
                //lstMachineInfo1 = GetMachineInfoObject();
                //sendDataToServer(lstMachineInfo1, baseAddress, userAction);

                if (isDeviceConnected)
                {

                    ICollection<MachineInfo> lstMachineInfo = null;
                    if (DateTime.Now.Hour >= 17)
                    {
                        lstMachineInfo = manipulator.GetCheckOutdata(objZkeeper, int.Parse(machineNumber));
                        //lstMachineInfo = GetMachineInfoObject();
                    }
                    else
                    {
                        lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(machineNumber));
                        //lstMachineInfo = GetMachineInfoObject();
                    }


                    //lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(machineNumber));
                    //lstMachineInfo = GetMachineInfoObject();
                    if (isAppStart)
                    {
                        lastDateTime.AddMinutes(minDiff);
                        isAppStart = false;
                    }
                    //ICollection<MachineInfo> lstMachineInfo = manipulator.GetCheckInLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                    ICollection<MachineInfo>  lstMachineInfo_filtered = lstMachineInfo.Where(x => x.TimeOnlyRecord >= lastDateTime).ToList();
                    if (lstMachineInfo_filtered != null && lstMachineInfo_filtered.Count > 0)
                    {
                        LogWriter.WriteLogStart("Fetched device logs current count : " + (lstMachineInfo_filtered == null ? 0 : lstMachineInfo_filtered.Count));
                        ICollection<MachineInfo> lstMachineInfo_filtered1 = lstMachineInfo_filtered.OrderByDescending(x => x.TimeOnlyRecord).Take(1).ToList();
                        foreach (MachineInfo info in lstMachineInfo_filtered1)
                        {
                            LogWriter.WriteLog("In Showing Staff Attendance");
                            if (info.TimeOnlyRecord != lastDateTime)
                            {
                                attendanceTime = info.TimeOnlyRecord.Hour
                                            + ":" + info.TimeOnlyRecord.Minute + ":" + info.TimeOnlyRecord.Second;
                                LogWriter.WriteLog("Now Showing Staff Attendance");

                                //Thread thread1 = new Thread(() => ShowStatus(attendanceTime, "", info.RegisterID.ToString(), info.EnployeeName));
                                //thread1.Start();

                                ShowStatus(attendanceTime, "", info.RegisterID.ToString(), info.EnployeeName);

                                //Id = info.RegisterID.ToString();
                                //Name = info.EnployeeName;
                                //Message = "Hello";
                                //statusFlag = true;

                                lastDateTime = info.TimeOnlyRecord;

                                LogWriter.WriteLog("Connecting API server");
                                Thread thread = new Thread(() => sendDataToServer(lstMachineInfo_filtered1, baseAddress, userAction));
                                thread.IsBackground = true;
                                thread.Start();
                                //sendDataToServer(lstMachineInfo_filtered1, baseAddress, userAction);
                            }
                        }
                    }

                }
                else
                {
                    LogWriter.WriteLog("Error : Unable to connect devices at IP : PORT (" + ipAddress + ":" + portNumber + ")");
                }
            }
            catch (Exception exc)
            {
                //isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }
        }

        private static void sendDataToServer(ICollection<MachineInfo> lstMachineInfo, string baseAddress, string userAction)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    int count = 0;
                    LogWriter.WriteLog("Connection is established to server.");
                    foreach (MachineInfo info in lstMachineInfo)
                    {
                        //string jsonString = "staffId=" + info.RegisterID + "&atteandanceDate=" + info.TimeOnlyRecord;
                        string jsonString = "staffId=" + info.RegisterID + "&attendanceDate=" + DateTime.Now;
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
            catch (Exception exc)
            {
                LogWriter.WriteLog("Exception Function Name : " + MethodBase.GetCurrentMethod().Name);
                LogWriter.WriteLog("Exception : " + exc.ToString());
                LogWriter.WriteLog("Exception Stack trace : " + exc.StackTrace.ToString());
                LogWriter.WriteLog("Exception Message : " + exc.Message.ToString());
            }
        }

        private static void ShowStatus(string attendanceTime, string result, string Id, string Name)
        {
            try
            {
                string message = "";

                message = "Divice Time : " + attendanceTime;

                if (message.Length > 0)
                {
                    //AttendanceForm form = new AttendanceForm(Id, Name, message, FormInterval);
                    //form.Show();
                    System.Windows.Forms.Application.Run(new AttendanceForm(Id, Name, message, FormInterval));
                    //Id = id;
                    //Name = name;
                    //Message = message;
                    //statusFlag = true;
                }
            }
            catch (Exception exc)
            {
                //isDeviceConnected = objZkeeper.Connect_Net(ipAddress, int.Parse(portNumber));
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
