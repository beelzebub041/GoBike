using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Grpc.Core;
using PostProto;

using PostPacket.ClientToServer;

using Service.Source.Define.StructClass;
using Tools;

namespace Connect
{

    class GRpcImpl : Post.PostBase
    {
        // ==================== Delegate ==================== //

        public delegate void AddQueueDelegate(MsgInfo info);

        private AddQueueDelegate addQueue = null;

        // ===========================================

        /// <summary>
        /// Logger物件
        /// </summary>
        private Tools.Logger logger = null;

        public GRpcImpl()
        {

        }

        ~GRpcImpl()
        {

        }

        /// <summary>
        /// 儲存Log
        /// </summary>
        /// <param name="msg"> 訊息 </param>
        private void SaveLog(string msg)
        {
            if (logger != null)
            {
                logger.AddLog(msg);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns> 是否初始化成功</returns>
        public bool Initialize(Tools.Logger logger, AddQueueDelegate addQueue)
        {
            bool ret = false;

            try
            {
                this.logger = logger;

                this.addQueue = addQueue;

                ret = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

                SaveLog("[Error] gRpcImpl::Initialize, Initialize Fail");
            }

            return ret;
        }

        // 實現SayHello方法
        public override Task<CreateResult> CreateNewPost(NewPostInfo request, ServerCallContext context)
        {
            CreateResult ret = new CreateResult();
            ret.Result = 0;

            try
            {
                CreateNewPost rData = new CreateNewPost
                {
                    MemberID = request.MemberID,

                    Photo = request.Photo,

                    Content = request.Content
                };

                string sData = JsonConvert.SerializeObject(rData);

                JObject jsMain = new JObject();

                jsMain.Add("CmdID", (int)C2S_CmdID.emCreateNewPost);
                jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(sData));

                MsgInfo info = new MsgInfo(jsMain.ToString(), this.SendToClient);

                this.addQueue?.Invoke(info);

                ret.Result = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

                SaveLog($"[Error] gRpcImpl::CreateNewPost, Catch Error, Msg:{ex.Message}");

            }

            return Task.FromResult(ret);
        }

        private void SendToClient(string msg)
        {
            // Do Nothing
        }
    }
}
