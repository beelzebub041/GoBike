using System;
using System.Collections.Generic;
using System.Text;

using Tools;
using RedisDataUpdateService.Source.DataUpdater;

namespace RedisDataUpdateService.Source
{
    class Service
    {
        private UserServiceUpdater userUpdater = null;

        private RideServiceUpdater rideUpdater = null;

        private TeamServiceUpdater teamUpdater = null;

        private string version = "Update002";

        public Service()
        {
            userUpdater = new UserServiceUpdater();

            rideUpdater = new RideServiceUpdater();

            teamUpdater = new TeamServiceUpdater();

            Logger.Instance.SaveLog($"[Info] Redis Data Update Service Version: {version}");
        }

        ~Service()
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
