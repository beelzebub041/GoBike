using System;
using System.Collections.Generic;
using System.Text;

using Tools;
using RedisDataUpdateService.Source.DataUpdater;

namespace DataUpdateService.Source
{
    class UpdateService
    {
        private UserServiceUpdater userUpdater = null;

        private RideServiceUpdater rideUpdater = null;

        private TeamServiceUpdater teamUpdater = null;

        private string version = "Update001";

        public UpdateService()
        {
            userUpdater = new UserServiceUpdater();

            rideUpdater = new RideServiceUpdater();

            teamUpdater = new TeamServiceUpdater();

            Logger.Instance.SaveLog($"Redis Data Update Service Version: {version}");
        }

        ~UpdateService()
        {
            Destroy();
        }

        public bool Initialize()
        {
            bool success = false;

            if (userUpdater.Initialize() && rideUpdater.Initialize() && teamUpdater.Initialize())
            {
                success = true;
            }

            return success;
        }

        public bool Destroy()
        {
            bool success = false;

            if (userUpdater.Destroy() && rideUpdater.Destroy() && teamUpdater.Destroy())
            {
                success = true;
            }

            return success;
        }

        public void RunUpdate()
        {
            Logger.Instance.SaveLog($"[Info] Service Run Redis Data Update");

            userUpdater.Update();

            rideUpdater.Update();

            teamUpdater.Update();

        }
    }
}
