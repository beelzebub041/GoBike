using System;

using CheckDateTimeService.Source;

using Tools;

namespace CheckDateTimeService
{
    class Program
    {
        static void Main(string[] args)
        {
            Service service = new Service();

            try
            {
                if (service.Initialize())
                {
                    Logger.Instance.SaveLog($"[Info] =================== Service Start ===================");

                    service.Run();

                    Console.WriteLine("Please Enter Any Key To Stop");

                    Console.ReadLine();

                    Logger.Instance.SaveLog($"[Info] =================== Service End ===================");
                }
                else
                {
                    Logger.Instance.SaveLog($"[Error] Service Initialize Fail");
                }
            }
            catch(Exception ex)
            {
                Logger.Instance.SaveLog($"[Error] Service Catch Error, Msg: {ex.Message}");

            }


        }


    }
}
