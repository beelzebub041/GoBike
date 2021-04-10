using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using FirebaseAdmin.Auth;

namespace Tools.FireBaseHandler
{
    class FireBaseHandler
    {
        private static FireBaseHandler instance = null;         // FireBaseHandler 實例

        private FirebaseAuth authInstance = null;

        private string keyPath = @"./Config/gobike-7a779-firebase-adminsdk-7sb34-854b55f421.json";

        private Logger logger = null;                           // Logger 物件


        private FireBaseHandler()
        {
            try
            {
                FirebaseApp defaultApp = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(keyPath),
                });

                authInstance = FirebaseAuth.GetAuth(defaultApp);
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] FireBaseHandler::FireBaseHandler, Catch Error, Msg:{ex.Message}");
            }
        }

        public static FireBaseHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FireBaseHandler();
                }

                return instance;
            }
        }

        public void SetLogger(Logger logger)
        {
            this.logger = logger;
        }

        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        public async Task<string> CheckFireBaseUserToken(string idToken)
        {
            string ret = "";

            try
            {
                FirebaseToken decodedToken = await authInstance.VerifyIdTokenAsync(idToken);

                ret = decodedToken.Uid;
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] FireBaseHandler::CheckFireBaseUserToken, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

    }
}
