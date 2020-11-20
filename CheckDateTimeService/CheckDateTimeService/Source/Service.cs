using System;
using System.Collections.Generic;
using System.Text;

using Tools;
using CheckDateTimeService.Source.DataChecker;
using System.Threading;

namespace CheckDateTimeService.Source
{
    class Service
    {
        private int checkInterval = 1000 * 60 * 60;          // 單位:ms, 目前每小時檢查一次

        private bool run = false;

        private UserServiceChecker userChecker = null;

        private RideServiceChecker rideChecker = null;

        private TeamServiceChecker teamChecker = null;

        private string version = "Check001";

        public Service()
        {
            Logger.Instance.SaveLog($"[Info] Check Data Time Service Version: {version}");

            userChecker = new UserServiceChecker();

            rideChecker = new RideServiceChecker();

            teamChecker = new TeamServiceChecker();
        }

        ~Service()
        {
            Destroy();
        }

        public bool Initialize()
        {
            bool success = false;

            try
            {
                if (userChecker.Initialize() && rideChecker.Initialize() && teamChecker.Initialize())
                {
                    run = true;

                    success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.SaveLog($"[Error] Service Initialize, Catch Error, Msg:{ex.Message}");
            }

            return success;
        }

        public bool Destroy()
        {
            bool success = false;

            if (userChecker.Destroy() && rideChecker.Destroy() && teamChecker.Destroy())
            {
                run = false;

                success = true;
            }

            return success;
        }

        public void Run()
        {
            while (run)
            {
                Logger.Instance.SaveLog($"[Info] Service Check Data Time");

                Logger.Instance.SaveLog($"[Info] =================== Check User Data Start ===================");
                userChecker.Check();
                Logger.Instance.SaveLog($"[Info] =================== Check User Data End ===================");

                Logger.Instance.SaveLog($"[Info] =================== Check Ride Data Start ===================");
                rideChecker.Check();
                Logger.Instance.SaveLog($"[Info] =================== Check Ride Data End ===================");

                Logger.Instance.SaveLog($"[Info] =================== Check Team Data Start ===================");
                teamChecker.Check();
                Logger.Instance.SaveLog($"[Info] =================== Check Team Data End ===================");

                Thread.Sleep(checkInterval);
            }
            
        }

    }
}
