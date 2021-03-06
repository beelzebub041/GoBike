﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Tools;
using Tools.WeekProcess;
using Tools.RedisHashTransfer;
using Tools.FireBaseHandler;
using Tools.PostAlgorithm;

using Service.Source.Define;
using DataBaseDef;
using Connect;
using SqlSugar;
using StackExchange.Redis;
using Tools.NotifyMessage;

using PostPacket.ClientToServer;
using PostPacket.ServerToClient;
using FirebaseAdmin.Auth;

namespace Service.Source
{
    class MessageFunction
    {
        /// <summary>
        /// 貼文演算法物件
        /// </summary>
        private PostAlgorithm algorithm = null;

        /// <summary>
        /// hash 轉換器
        /// </summary>
        private RedisHashTransfer hashTransfer = null;

        /// <summary>
        /// 週時間處理
        /// </summary>
        private WeekProcess weekProcess = null;

        /// <summary>
        /// 推播物件
        /// </summary>
        private NotifyMessage ntMsg = null;

        /// <summary>
        /// Logger 物件
        /// </summary>
        private Logger logger = null;

        /// <summary>
        /// 建構式
        /// </summary>
        public MessageFunction()
        {
            algorithm = new PostAlgorithm();

            hashTransfer = new RedisHashTransfer();

            weekProcess = new WeekProcess();

            ntMsg = new NotifyMessage();
        }

        /// <summary>
        /// 解構式
        /// </summary>
        ~MessageFunction()
        {

        }

        /// <summary>
        ///  初始化
        /// </summary>
        /// <returns> 是否成功初始化 </returns>
        public bool Initialize(Logger logger)
        {
            bool ret = false;

            try
            {
                this.logger = logger;

                if (ntMsg.Initialize(logger) && algorithm.Initialize(logger))
                {
                    ret = true;

                    FireBaseHandler.Instance.SetLogger(logger);

                    SaveLog("[Info] MessageFcunction::Initialize, Initialize Success");
                }
                else
                {
                    SaveLog($"[Info] MessageFcunction::Initialize, Fail");
                }
            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::Initialize, Catch Error, Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 銷毀
        /// </summary>
        /// <returns> 是否成功銷毀 </returns>
        public bool Destory()
        {
            bool ret = true;

            return ret;

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
        /// 新增會員好友的塗鴉牆列表
        /// </summary>
        /// <param name="memeberID"></param>
        /// <param name="post"></param>
        private bool AddFriendPost(string memeberID, string postID)
        {
            bool ret = false;

            try
            {
                UserInfo userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memeberID).Single();

                if (userInfo != null)
                {
                    JArray jsData = JArray.Parse(userInfo.FriendList);

                    List<string> updateList = jsData.ToObject<List<string>>();

                    // 需同時更新自己的塗鴉牆顯示列表
                    updateList.Add(memeberID);

                    for (int idx = 0; idx < updateList.Count(); idx++)
                    {
                        string sKey = $"PostShowList_" + updateList[idx];

                        if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyExists(sKey))
                        {
                            string postShowListInfo = GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringGet(sKey);

                            JArray jsPostList = JArray.Parse(postShowListInfo);
                            List<string> showList = jsPostList.ToObject<List<string>>();

                            showList.Insert(0, postID);

                            if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet(sKey, JArray.FromObject(showList).ToString()))
                            {
                                ret = true;

                                SaveLog($"[Info] MessageFcunction::AddFriendPost Add Post List:{sKey} Success");

                            }
                            else
                            {
                                SaveLog($"[Warning] MessageFcunction::AddFriendPost Add Post List:{sKey} Fail");
                            }


                        }
                        else
                        {

                            List<string> showList = new List<string>();
                            showList.Add(postID);

                            if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet(sKey, JArray.FromObject(showList).ToString()))
                            {
                                ret = true;

                                SaveLog($"[Info] MessageFcunction::AddFriendPost Create Post Show List Success");

                            }
                            else
                            {
                                SaveLog($"[Warning] MessageFcunction::AddFriendPost Create Post Show List Fail");
                            }

                        }
                    }
                }
                else
                {
                    SaveLog($"[Info] MessageFcunction::AddFriendPost Can Not Find Member:{memeberID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::AddFriendPost Catch Error Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 刪除會員好友的塗鴉牆列表
        /// </summary>
        /// <param name="memeberID"></param>
        /// <param name="post"></param>
        private bool DeleteFriendPost(string memeberID, string postID)
        {
            bool ret = false;

            try
            {
                UserInfo userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == memeberID).Single();

                if (userInfo != null)
                {
                    JArray jsData = JArray.Parse(userInfo.FriendList);

                    List<string> updateList = jsData.ToObject<List<string>>();

                    // 需同時更新自己的塗鴉牆顯示列表
                    updateList.Add(memeberID);

                    for (int idx = 0; idx < updateList.Count(); idx++)
                    {
                        string sKey = $"PostShowList_" + updateList[idx];

                        if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyExists(sKey))
                        {
                            string postShowListInfo = GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringGet(sKey);

                            JArray jsPostList = JArray.Parse(postShowListInfo);
                            List<string> showList = jsPostList.ToObject<List<string>>();

                            if (showList.Contains(postID))
                            {
                                showList.Remove(postID);

                                if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet(sKey, JArray.FromObject(showList).ToString()))
                                {
                                    ret = true;

                                    SaveLog($"[Info] MessageFcunction::DeleteFriendPost Delete Post List:{sKey} Success");

                                }
                                else
                                {
                                    SaveLog($"[Warning] MessageFcunction::DeleteFriendPost Delete Post List:{sKey} Fail");
                                }
                            }
                            else
                            {
                                SaveLog($"[Warning] MessageFcunction::DeleteFriendPost Delete Post List Fail");
                            }

                        }
                        else
                        {
                            SaveLog($"[Warning] MessageFcunction::DeleteFriendPost Can Not Find Post List:{sKey}");

                        }
                    }
                }
                else
                {
                    SaveLog($"[Info] MessageFcunction::DeleteFriendPost Can Not Find Member:{memeberID}");
                }

            }
            catch (Exception ex)
            {
                SaveLog($"[Error] MessageFcunction::DeleteFriendPost Catch Error Msg:{ex.Message}");
            }

            return ret;
        }

        /// <summary>
        /// 建立新貼文
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnCreateNewPost(string data)
        {
            string ret = "";

            CreateNewPost packet = JsonConvert.DeserializeObject<CreateNewPost>(data);

            CreateNewPostResult rData = new CreateNewPostResult();

            PostInfo newPost = new PostInfo();

            try
            {
                UserAccount account = GetSql().Queryable<UserAccount>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 會員存在
                if (account != null)
                {
                    string dateTime = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");

                    string guidAll = Guid.NewGuid().ToString();

                    string[] guidList = guidAll.Split('-');

                    // 建立貼文
                    newPost.PostID     = "DbPst-" + guidList[0];        // 取GUID前8碼
                    newPost.MemberID   = packet.MemberID;
                    newPost.Photo      = packet.Photo != null ? packet.Photo : "[]";
                    newPost.Content    = packet.Content;
                    newPost.LikeList   = "[]";
                    newPost.CreateDate = dateTime;

                    // 設定DB 交易的起始點
                    GetSql().BeginTran();

                    // 寫入資料庫
                    if (GetSql().Insertable(newPost).With(SqlSugar.SqlWith.TabLockX).ExecuteCommand() > 0)
                    {
                        rData.Result = (int)CreateNewPostResult.ResultDefine.emResult_Success;

                        SaveLog($"[Info] MessageFcunction::OnCreateNewPost Create New Post Success");
                    }
                    else
                    {
                        rData.Result = (int)CreateNewPostResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] MessageFcunction::OnCreateNewPost Member:{packet.MemberID}'s Post Fail");
                    }

                }
                else
                {
                    rData.Result = (int)CreateNewPostResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnCreateNewPost Can Not Find Member:{packet.MemberID}");

                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)CreateNewPostResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnCreateNewPost Catch Error Msg:{ex.Message}");
            }

            if (rData.Result == (int)CreateNewPostResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                // 建立Redis 會員貼文
                {
                    JObject jsNewPost = new JObject();
                    jsNewPost.Add("MemberID", newPost.MemberID);
                    jsNewPost.Add("CreateDate", newPost.CreateDate);
                    jsNewPost.Add("Place", newPost.Place);
                    jsNewPost.Add("Photo", newPost.Photo);
                    jsNewPost.Add("Content", newPost.Content);

                    JArray jsLikeList = new JArray();
                    jsNewPost.Add("LikeList", jsLikeList.ToString());

                    GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostInfo_" + newPost.PostID, jsNewPost.ToString());
                }

                // 建立Redis 會員的貼文ID列表
                {
                    string sPostList = "[]";
                    
                    if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyExists($"PostList_" + newPost.MemberID))
                    {
                        sPostList = GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringGet($"PostList_" + newPost.MemberID);
                    }

                    JArray jsMemberPostList = JArray.Parse(sPostList);
                    List<string> memberPostList = jsMemberPostList.ToObject<List<string>>();

                    memberPostList.Add(newPost.PostID);

                    JArray jsNewList = JArray.FromObject(memberPostList);

                    GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostList_" + newPost.MemberID, jsNewList.ToString());

                }

                AddFriendPost(newPost.MemberID, newPost.PostID);

            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emCreateNewPostResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 更新貼文
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUpdatePost(string data)
        {
            string ret = "";

            UpdatePost packet = JsonConvert.DeserializeObject<UpdatePost>(data);

            UpdatePostResult rData = new UpdatePostResult();

            PostInfo post = null;

            try
            {
                post = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).Single();

                //貼文存在
                if (post != null)
                {
                    // 貼文所有者
                    if (post.MemberID == packet.MemberID)
                    {
                        post.Photo = packet.Photo == null ? post.Photo : packet.Photo;
                        post.Content = packet.Content == null ? post.Content : packet.Content;

                        // 設定DB 交易的起始點
                        GetSql().BeginTran();

                        if (GetSql().Updateable<PostInfo>(post).With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)UpdatePostResult.ResultDefine.emResult_Success;

                            SaveLog($"[Info] MessageFcunction::OnUpdatePost Update Member:{packet.MemberID}'s Post:{packet.PostID} Info Success");
                        }
                        else
                        {
                            rData.Result = (int)UpdatePostResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFcunction::OnUpdatePost Update Member:{packet.MemberID}'s Post:{packet.PostID} Info Fail");
                        }
                    }
                    else 
                    {
                        rData.Result = (int)UpdatePostResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFcunction::OnUpdatePost Member:{packet.MemberID} Not Post:{packet.PostID} Owner");
                    }
                }
                else
                {
                    rData.Result = (int)UpdatePostResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnUpdatePost Can Not Find Member:{packet.MemberID}'s Post:{packet.PostID}");
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)UpdatePostResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnUpdatePost Catch Error Msg:{ex.Message}");
            }

            if (rData.Result == (int)UpdatePostResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                // 更新Redis 會員貼文
                {
                    JObject jsNewPost = new JObject();
                    jsNewPost.Add("MemberID", post.MemberID);
                    jsNewPost.Add("CreateDate", post.CreateDate);
                    jsNewPost.Add("Place", post.Place);
                    jsNewPost.Add("Photo", post.Photo);
                    jsNewPost.Add("Content", post.Content);
                    jsNewPost.Add("LikeList", post.LikeList);

                    GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostInfo_" + post.PostID, jsNewPost.ToString());
                }
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePostResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 刪除貼文
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnDeletePost(string data)
        {
            string ret = "";

            DeletePost packet = JsonConvert.DeserializeObject<DeletePost>(data);

            DeletePostResult rData = new DeletePostResult();

            PostInfo post = null;

            try
            {
                post = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).Single();

                //貼文存在
                if (post != null)
                {
                    // 貼文所有者
                    if (post.MemberID == packet.MemberID)
                    {
                        // 設定DB 交易的起始點
                        GetSql().BeginTran();

                        if (GetSql().Deleteable<PostInfo>().With(SqlSugar.SqlWith.TabLockX).Where(it => it.PostID == packet.PostID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)DeletePostResult.ResultDefine.emResult_Success;

                            SaveLog($"[Info] MessageFcunction::OnDeletePost Delete Member:{packet.MemberID}'s Post:{packet.PostID} Info Success");
                        }
                        else
                        {
                            rData.Result = (int)DeletePostResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFcunction::OnDeletePost Delete Member:{packet.MemberID}'s Post:{packet.PostID} Info Fail");
                        }
                    }
                    else
                    {
                        rData.Result = (int)DeletePostResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Info] MessageFcunction::OnDeletePost Member:{packet.MemberID} Not Post:{packet.PostID} Owner");
                    }
                }
                else
                {
                    rData.Result = (int)DeletePostResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnDeletePost Can Not Find Member:{packet.MemberID}'s Post:{packet.PostID}");
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)DeletePostResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnDeletePost Catch Error Msg:{ex.Message}");
            }

            if (rData.Result == (int)DeletePostResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                // 刪除Redus 會員的貼文
                {
                    if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyExists($"PostInfo_" + packet.PostID))
                    {
                        GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyDelete($"PostInfo_" + packet.PostID);
                    }
                }

                // 刪除Redis 會員的貼文列表
                {
                    if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).KeyExists($"PostList_" + packet.MemberID))
                    {
                        string sPostList = GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringGet($"PostList_" + packet.MemberID);

                        JArray jsMemberPostList = JArray.Parse(sPostList);
                        List<string> memberPostList = jsMemberPostList.ToObject<List<string>>();

                        if (memberPostList.Contains(packet.PostID))
                        {
                            memberPostList.Remove(packet.PostID);

                        }

                        JArray jsNewList = JArray.FromObject(memberPostList);

                        GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostList_" + packet.MemberID, jsNewList.ToString());
                    }
                }

                DeleteFriendPost(packet.MemberID, packet.PostID);

            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emDeletePostResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 新增點讚數
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnAddLike(string data)
        {
            string ret = "";

            AddLike packet = JsonConvert.DeserializeObject<AddLike>(data);

            AddLikeResult rData = new AddLikeResult();

            PostInfo post = null;

            try
            {
                post = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).Single();

                //貼文存在
                if (post != null)
                {
                    JArray jsData = JArray.Parse(post.LikeList);

                    List<string> likeList = jsData.ToObject<List<string>>();

                    if (!likeList.Contains(packet.MemberID))
                    {
                        likeList.Add(packet.MemberID);

                        JArray jsNew = JArray.FromObject(likeList);

                        // 設定DB 交易的起始點
                        GetSql().BeginTran();

                        if (GetSql().Updateable<PostInfo>().SetColumns(it => new PostInfo() { LikeList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)AddLikeResult.ResultDefine.emResult_Success;

                            post.LikeList = jsNew.ToString();

                            SaveLog($"[Info] MessageFcunction::OnAddLike Update Post:{packet.PostID} Like Success");
                        }
                        else
                        {
                            rData.Result = (int)AddLikeResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFcunction::OnAddLike Update Post:{packet.PostID} Like Fail");
                        }
                    }
                    else
                    {
                        rData.Result = (int)AddLikeResult.ResultDefine.emResult_Fail;
                    }

                }
                else
                {
                    rData.Result = (int)AddLikeResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnAddLike Can Not Find Post:{packet.PostID}");
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)AddLikeResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnAddLike Catch Error Msg:{ex.Message}");
            }

            if (rData.Result == (int)AddLikeResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                // 更新Redis 會員貼文
                {
                    JObject jsNewPost = new JObject();
                    jsNewPost.Add("MemberID", post.MemberID);
                    jsNewPost.Add("CreateDate", post.CreateDate);
                    jsNewPost.Add("Place", post.Place);
                    jsNewPost.Add("Photo", post.Photo);
                    jsNewPost.Add("Content", post.Content);
                    jsNewPost.Add("LikeList", post.LikeList);

                    GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostInfo_" + post.PostID, jsNewPost.ToString());
                }

            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emAddLikeResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 減少點讚數
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnReduceLike(string data)
        {
            string ret = "";

            ReduceLike packet = JsonConvert.DeserializeObject<ReduceLike>(data);

            ReduceLikeResult rData = new ReduceLikeResult();

            PostInfo post = null;

            try
            {
                post = GetSql().Queryable<PostInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).Single();

                //貼文存在
                if (post != null)
                {
                    JArray jsData = JArray.Parse(post.LikeList);

                    List<string> likeList = jsData.ToObject<List<string>>();

                    if (likeList.Contains(packet.MemberID))
                    {
                        likeList.Remove(packet.MemberID);

                        JArray jsNew = JArray.FromObject(likeList);

                        // 設定DB 交易的起始點
                        GetSql().BeginTran();

                        if (GetSql().Updateable<PostInfo>().SetColumns(it => new PostInfo() { LikeList = jsNew.ToString() }).With(SqlSugar.SqlWith.RowLock).Where(it => it.PostID == packet.PostID).ExecuteCommand() > 0)
                        {
                            rData.Result = (int)ReduceLikeResult.ResultDefine.emResult_Success;

                            post.LikeList = jsNew.ToString();

                            SaveLog($"[Info] MessageFcunction::OnReduceLike Update Post:{packet.PostID} Like Success");
                        }
                        else
                        {
                            rData.Result = (int)ReduceLikeResult.ResultDefine.emResult_Fail;

                            SaveLog($"[Info] MessageFcunction::OnReduceLike Update Post:{packet.PostID} Like Fail");
                        }
                    }
                    else
                    {
                        rData.Result = (int)ReduceLikeResult.ResultDefine.emResult_Fail;
                    }

                }
                else
                {
                    rData.Result = (int)ReduceLikeResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnReduceLike Can Not Find Post:{packet.PostID}");
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)ReduceLikeResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnReduceLike Catch Error Msg:{ex.Message}");
            }

            if (rData.Result == (int)ReduceLikeResult.ResultDefine.emResult_Success)
            {
                // DB 交易提交
                GetSql().CommitTran();

                // 更新Redis 會員貼文
                {
                    JObject jsNewPost = new JObject();
                    jsNewPost.Add("MemberID", post.MemberID);
                    jsNewPost.Add("CreateDate", post.CreateDate);
                    jsNewPost.Add("Place", post.Place);
                    jsNewPost.Add("Photo", post.Photo);
                    jsNewPost.Add("Content", post.Content);
                    jsNewPost.Add("LikeList", post.LikeList);

                    GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostInfo_" + post.PostID, jsNewPost.ToString());
                }
            }
            else
            {
                // DB 交易失敗, 啟動Rollback
                GetSql().RollbackTran();
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emReduceLikeResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

        /// <summary>
        /// 更新塗鴉牆列表
        /// </summary>
        /// <param name="data"> 封包資料 </param>
        /// <returns> 結果 </returns>
        public string OnUpdatePostShowList(string data)
        {
            string ret = "";

            UpdatePostShowList packet = JsonConvert.DeserializeObject<UpdatePostShowList>(data);

            UpdatePostShowListResult rData = new UpdatePostShowListResult();

            try
            {
                UserInfo userInfo = GetSql().Queryable<UserInfo>().With(SqlSugar.SqlWith.RowLock).Where(it => it.MemberID == packet.MemberID).Single();

                // 會員存在
                if (userInfo != null)
                {
                    JArray jaFriendList = JArray.Parse(userInfo.FriendList);

                    JObject jsInfo = new JObject();
                    jsInfo.Add("MemberID", userInfo.MemberID);
                    jsInfo.Add("FriendList", jaFriendList);
                    jsInfo.Add("MaxCount", 50);

                    List<string> postShowList = algorithm.GetMemberPostShowList(AlgorithmType.ByDateTime, jsInfo.ToString());

                    if (GetRedis((int)Connect.RedisDB.emRedisDB_Post).StringSet($"PostShowList_{userInfo.MemberID}", JArray.FromObject(postShowList).ToString()))
                    {
                        rData.Result = (int)UpdatePostShowListResult.ResultDefine.emResult_Success;

                        SaveLog($"[Info] MessageFcunction::OnUpdatePostShowList Create Post Show List Success");

                    }
                    else
                    {
                        rData.Result = (int)UpdatePostShowListResult.ResultDefine.emResult_Fail;

                        SaveLog($"[Warning] MessageFcunction::OnUpdatePostShowList Create Post Show List Fail");
                    }

                }
                else
                {
                    rData.Result = (int)UpdatePostShowListResult.ResultDefine.emResult_Fail;

                    SaveLog($"[Info] MessageFcunction::OnUpdatePostShowList Can Not Find Member:{packet.MemberID}");
                }

            }
            catch (Exception ex)
            {
                rData.Result = (int)UpdatePostShowListResult.ResultDefine.emResult_Fail;

                SaveLog($"[Error] MessageFcunction::OnUpdatePostShowList Catch Error Msg:{ex.Message}");
            }

            JObject jsMain = new JObject();
            jsMain.Add("CmdID", (int)S2C_CmdID.emUpdatePostShowListResult);
            jsMain.Add("Data", JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(rData)));

            ret = jsMain.ToString();

            return ret;
        }

    }

}
