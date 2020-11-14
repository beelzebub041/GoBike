using System;

using DataUpdateService.Source;

using Tools;

namespace RedisDataUpdateService
{
    class Program
    {
        static void Main(string[] args)
        {
            UpdateService service = new UpdateService();

            try
            {
                if (service.Initialize())
                {
                    Logger.Instance.SaveLog($"[Info] =================== Update Start ===================");

                    service.RunUpdate();

                    Logger.Instance.SaveLog($"[Info] =================== Update End ===================");

                    Console.WriteLine("Please Enter Any Key To Stop");

                    Console.ReadLine();
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
