using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools;
using Connect;
using SqlSugar;
using StackExchange.Redis;

using DataBaseDef;

namespace Tools.PostAlgorithm
{
    public enum AlgorithmType : int
    {
        ByDateTime = 0,         // 依時間新到舊排序
    }

    class PostAlgorithm
    {
        // ==================== Delegate ==================== //

        /// <summary>
        /// AlgorithmFunction的委派函式
        /// </summary>
        /// <param name="jsonInfo"> jsoncInfo </param>
        /// <returns> 回傳Post List </returns>
        private delegate List<string> AlgorithmFunction(string jsonInfo);

        // ============================================ //

        /// <summary>
        /// AlgorithmFunction列表, Key: AlgorithmType, Value: callBack Function
        /// </summary>
        private Dictionary<AlgorithmType, AlgorithmFunction> algorithmFuncList = null;

        /// <summary>
        /// Logger物件
        /// </summary>
        private Tools.Logger logger = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public PostAlgorithm()
        {
            algorithmFuncList = new Dictionary<AlgorithmType, AlgorithmFunction>();
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~PostAlgorithm()
        {

        }

        /// <summary>
        /// 取得 Sql物件
        /// </summary>
        /// <returns> Sql物件 </returns>
        private SqlSugarClient GetSql()
        {
            return DataBaseConnect.Instance.GetSql();
        }

        /// <summary>
        /// 取得Redis 物件
        /// </summary>
        /// <param name="idx"> Redis資料庫索引</param>
        /// <returns> Redis物件 </returns>
        private IDatabase GetRedis(int idx)
        {
            return RedisConnect.Instance.GetRedis(idx);
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
            else
            {
                Console.WriteLine(msg);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logger"> Logger物件 </param>
        /// <returns> 是否初始化成功 </returns>
        public bool Initialize(Tools.Logger logger)
        {
            bool ret = true;

            this.logger = logger;

            AddAlgorithmFunc(AlgorithmType.ByDateTime, this.GetPostShowListByDateTime);

            ret = true;

            SaveLog("[Info] MessageProcessor::Initialize, Initialize Success");

            return ret;
        }

        /// <summary>
        /// 註冊Algorithm Function
        /// </summary>
        /// <param name="msgId"> 訊息編號 </param>
        /// <param name="func"> 對應的函式 </param>
        private void AddAlgorithmFunc(AlgorithmType type, AlgorithmFunction func)
        {
            if (!algorithmFuncList.ContainsKey(type))
            {
                algorithmFuncList.Add(type, func);
            }
        }

        /// <summary>
        /// 排序貼文列表, 依時間新到舊
        /// </summary>
        /// <param name="infoList"> 貼文列表 <時間, 貼文ID> </param>
        /// <returns> 排序後的列表 </returns>
        private Dictionary<string, DateTime> SortPost(Dictionary<string, DateTime> infoList)
        {
            Dictionary<string, DateTime> ret = new Dictionary<string, DateTime>();

            try
            {
                ret = infoList.OrderByDescending(Data => Data.Value).ToDictionary(keyvalue => keyvalue.Key, keyvalue => keyvalue.Value);
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] PostAlgorithm::SortPost, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 合併貼文列表
        /// </summary>
        /// <param name="target"> 目標列表 </param>
        /// <param name="source"> 來源列表 </param>
        /// <param name="MaxCount"> 列表的最大數量, -1表示不限制 </param>
        /// <returns> 合併後的列表 </returns>
        private Dictionary<string, DateTime>  MergePost(Dictionary<string, DateTime> target, Dictionary<string, DateTime> source, int MaxCount = -1)
        {
            Dictionary<string, DateTime> ret = new Dictionary<string, DateTime>();

            try
            {
                foreach (KeyValuePair<string, DateTime> info in source)
                {
                    target.Add(info.Key, info.Value);
                }

                ret = this.SortPost(target);

                if (MaxCount > -1 && ret.Any())
                {
                    Dictionary<string, DateTime> temp = new Dictionary<string, DateTime>();

                    foreach (KeyValuePair<string, DateTime> info in ret)
                    {
                        if (temp.Count() >= MaxCount)
                        {
                            break;
                        }
                        else
                        {
                            temp.Add(info.Key, info.Value);
                        }
                    }

                    ret = temp;
                }
            }
            catch(Exception ex)
            {
                SaveLog($"[Error] PostAlgorithm::MergePost, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        private List<string> GetPostShowListByDateTime(string jsonInfo)
        {
            List<string> ret = new List<string>();

            try
            {
                JObject jsInfo = JObject.Parse(jsonInfo);

                if (jsInfo.ContainsKey("MemberID"))
                {
                    string memberID = jsInfo["MemberID"].ToString();

                    List<string> memberPostList = new List<string>();

                    List<string> friendList = new List<string>();

                    int maxCount = -1;

                    if (jsInfo.ContainsKey("FriendList"))
                    {
                        JArray jsList = JArray.Parse(jsInfo["FriendList"].ToString());
                        friendList = jsList.ToObject<List<string>>();
                    }

                    if (jsInfo.ContainsKey("MaxCount"))
                    {
                        maxCount = Convert.ToInt32(jsInfo["MaxCount"].ToString());
                    }

                    List<PostInfo> postInfoList = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memberID).OrderBy(it => it.CreateDate, SqlSugar.OrderByType.Desc).Take(10).ToList();

                    for (int idx = 0; idx < friendList.Count(); idx++)
                    {
                        List<PostInfo> friendPostInfoList = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == friendList[idx]).OrderBy(it => it.CreateDate, SqlSugar.OrderByType.Desc).Take(10).ToList();

                        postInfoList.AddRange(friendPostInfoList);
                    }

                    postInfoList = postInfoList.OrderByDescending(data => data.CreateDate).ToList();

                    if (maxCount == -1)
                    {
                        maxCount = postInfoList.Count();
                    }

                    for (int idx = 0; idx < postInfoList.Count() && idx < maxCount; idx++)
                    {
                        ret.Add(postInfoList[idx].PostID);
                    }

                }
                else
                {
                    SaveLog($"[Error] PostAlgorithm::GetPostShowListByDateTime, Json Info Error, Can Not Find Member: MemberID ");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] PostAlgorithm::GetPostShowListByDateTime, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 取得會員塗鴉牆的顯示列表
        /// </summary>
        /// <param name="type"> 演算法類型 </param>
        /// <param name="jsonInfo"> 演算法所需資料 </param>
        /// <returns> 塗鴉牆列表 </returns>
        public List<string> GetMemberPostShowList(AlgorithmType type, string jsonInfo)
        {
            List<string> ret = new List<string>();

            try
            {
                if (algorithmFuncList.ContainsKey(type))
                {
                    ret = algorithmFuncList[type](jsonInfo);
                }
                else
                {
                    SaveLog($"[Warning] PostAlgorithm::GetMemberPostShowList Can't Find Algorithm Type:{type}");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] PostAlgorithm::GetMemberPostShowList, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

    }
}
