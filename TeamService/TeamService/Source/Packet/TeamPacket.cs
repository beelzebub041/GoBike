
namespace TeamPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateNewTeamResult = 1001,
        emUpdateTeamDataResult,
        emChangeLanderResult,
        emUpdateViceLeaderListResult,
        //emUpdateTeamMemberListResult,     不使用
        emUpdateApplyJoinListResult = 1006,
        emUpdateBulletinResult,
        emUpdateActivityResult,
        emDeleteTeamResult,
        emJoinOrLeaveTeamActivityResult,
        emJoinOrLeaveTeamResult,
        emKickTeamMemberResult,

    }

    // 建立新車隊
    class CreateNewTeamResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 建立失敗
            emResult_Success,           // 1: 建立成功
            emResult_LeaderRepeat,      // 2: 重複擔任隊長
        }

        // 結果
        public int Result { get; set; }

        // 車隊ID
        public string TeamID { get; set; }
    }

    // 更新車隊資料
    class UpdateTeamDataResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 更新失敗
            emResult_Success,                       // 1: 更新成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        // 結果
        public int Result { get; set; }

    }

    // 更換隊長結果
    class ChangeLanderResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 改變失敗
            emResult_Success,                       // 1: 改變成功
            emResult_Repeat,                        // 2: 重複擔任
            emResult_InsufficientPermissions,       // 3: 權限不足
        }

        // 結果
        public int Result { get; set; }
    }

    // 更新副隊長列表結果
    class UpdateViceLeaderListResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 更新失敗
            emResult_Success,                       // 1: 更新成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        /**
         * 更新動作
         * -1: 無動作
         * 0: 新增
         * 1: 刪除
         */
        public int Action { get; set; }


        // 結果
        public int Result { get; set; }

    }

    //// 更新隊員列表結果
    //class UpdateTeamMemberListResult
    //{
    //    // 結果定義
    //    public enum ResultDefine : int
    //    {
    //        emResult_Fail = 0,                      // 0: 更新失敗
    //        emResult_Success,                       // 1: 更新成功
    //    }

    //    /**
    //      * 更新動作
    //      * -1: 無動作
    //      * 0: 新增
    //      * 1: 刪除
    //      */
    //    public int Action { get; set; }

    //    // 結果
    //    public int Result { get; set; }

    //}

    // 更新申請加入車隊列表結果
    class UpdateApplyJoinListResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 更新失敗
            emResult_Success,                       // 1: 更新成功
        }

        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    // 更新公告結果
    class UpdateBulletinResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 更新失敗
            emResult_Success,                       // 1: 更新成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          * 2: 修改
          */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

        // 公告ID
        public string BulletinID { get; set; }

    }

    // 更新活動結果
    class UpdateActivityResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 更新失敗
            emResult_Success,                       // 1: 更新成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          * 2: 修改
          */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

        // 活動ID
        public string ActID { get; set; }

    }

    // 解散車隊結果
    class DeleteTeamResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 解散失敗
            emResult_Success,                       // 1: 解散成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        // 結果
        public int Result { get; set; }

    }

    // 加入或離開車隊活動結果
    class JoinOrLeaveTeamActivityResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 解散失敗
            emResult_Success,                       // 1: 解散成功
        }

        /*
        * -1: 離開 
        * 0: 無動作
        * 1: 加入
        */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    // 加入或離開車隊結果
    class JoinOrLeaveTeamResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 失敗
            emResult_Success,                       // 1: 成功
        }

        /*
        * -1: 離開 
        * 0: 無動作
        * 1: 加入
        */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    // 踢離車隊成員結果
    class KickTeamMemberResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,                      // 0: 失敗
            emResult_Success,                       // 1: 成功
            emResult_InsufficientPermissions,       // 2: 權限不足
        }

        // 結果定義
        public int Result { get; set; }

    }

}

namespace TeamPacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emCreateNewTeam = 1001,
        emUpdateTeamData,
        emChangeLander,
        emUpdateViceLeaderList,
        //emUpdateTeamMemberList,   不使用
        emUpdateApplyJoinList = 1006,
        emUpdateBulletin,
        emUpdateActivity,
        emDeleteTeam,
        emJoinOrLeaveTeamActivity,
        emJoinOrLeaveTeam,
        emKickTeamMember,

    }

    // ======================= Team ======================= //

    /*
    * 建立新車隊
    */
    class CreateNewTeam
    {
        // 建立車隊的會員的MemberID
        public string MemberID { get; set; }

        // 車隊名稱
        public string TeamName { get; set; }

        // 車隊簡介
        public string TeamInfo { get; set; }

        // 車隊頭像
        public string Avatar { get; set; }

        // 車隊封面
        public string FrontCover { get; set; }

        // 車隊所在地
        public int County { get; set; }

        // 是否開放搜尋
        public int SearchStatus { get; set; }

        // 加入車隊是否需要審核
        public int ExamineStatus { get; set; }

    }

    /*
    * 更新車隊資料
    */
    class UpdateTeamData
    {
        // 車隊ID
        public string TeamID { get; set; }

        // 更新車隊資料的會員ID
        public string MemberID { get; set; }

        // 車隊名稱
        public string TeamName { get; set; }

        // 車隊簡介
        public string TeamInfo { get; set; }

        // 車隊頭像
        public string Avatar { get; set; }

        // 車隊封面
        public string FrontCover { get; set; }

        // 車隊所在地
        public int County { get; set; }

        // 是否開放搜尋
        public int SearchStatus { get; set; }

        // 加入車隊是否需要審核
        public int ExamineStatus { get; set; }

    }

    /**
     * 更換隊長
     */
    class ChangeLander
    {   
        // 車隊ID
        public string TeamID { get; set; }

        // 隊長的MemberID
        public string LeaderID { get; set; }

        // 新隊長的MemberID
        public string MemberID { get; set; }

    }

    /*
    * 更新副隊長列表
    */
    class UpdateViceLeaderList
    {
        // 車隊ID
        public string TeamID { get; set; }

        /**
         * 更新動作
         * -1: 刪除
         * 0: 無動作
         * 1: 新增
         */
        public int Action { get; set; }

        // 隊長的MemberID
        public string LeaderID { get; set; }

        // 副隊長的MemberID
        public string MemberID { get; set; }

    }

    ///*
    //* 更新車隊隊員列表
    //*/
    //class UpdateTeamMemberList
    //{
    //    // 車隊ID
    //    public string TeamID { get; set; }

    //    /**
    //    * 更新動作
    //    * -1: 刪除
    //    * 0: 無動作
    //    * 1: 新增
    //    */
    //    public int Action { get; set; }

    //    // 隊員的MemberID
    //    public string MemberID { get; set; }

    //}

    /*
    * 更新申請加入車隊列表
    */
    class UpdateApplyJoinList
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emResult_Delete = -1,                // -1: 刪除
            emResult_None,                       // 0: 無動作
            emResult_Add,                        // 1: 新增
        }

        // 車隊ID
        public string TeamID { get; set; }

        // 動作
        public int Action { get; set; }

        // 隊員的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 更新公告
    */
    class UpdateBulletin
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emResult_Delete = -1,                // -1: 刪除
            emResult_None,                       // 0: 無動作
            emResult_Add,                        // 1: 新增
            emResult_Modify,                     // 2: 修改
        }

        // 動作
        public int Action { get; set; }

        // 公告ID, 修改與刪除需帶入, 新增帶空值
        public string BulletinID { get; set; }

        // 車隊ID
        public string TeamID { get; set; }

        // 發出公告的會員的會員ID
        public string MemberID { get; set; }

        // 建立日期
        public string CreateDate { get; set; }

        // 公告內容
        public string Content { get; set; }

        // 公告天數
        public int Day { get; set; }

    }

    /*
    * 更新活動
    */
    class UpdateActivity
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emResult_Delete = -1,                // -1: 刪除
            emResult_None,                       // 0: 無動作
            emResult_Add,                        // 1: 新增
            emResult_Modify,                     // 2: 修改
        }

        // 動作
        public int Action { get; set; }

        // 活動ID, 修改與刪除需帶入, 新增帶空值
        public string ActID { get; set; }

        // 建立日期
        public string CreateDate { get; set; }

        // 車隊ID
        public string TeamID { get; set; }

        // 發出活動的會員的會員ID
        public string MemberID { get; set; }

        // 加入活動的隊員的會員ID列表
        public string MemberList { get; set; }

        // 活動日期
        public string ActDate { get; set; }

        // 活動標題
        public string Title { get; set; }

        // 集合時間
        public string MeetTime { get; set; }

        // 總距離
        public string TotalDistance { get; set; }

        // 道路線圖
        public string LoadMap { get; set; }

        // 最高海拔
        public float MaxAltitude { get; set; }

        // 路線
        public string Route { get; set; }

        // 路線描述
        public string Description { get; set; }

    }

    /*
    * 解散車隊
    */
    class DeleteTeam
    {
        // 車隊ID
        public string TeamID { get; set; }

        // 隊員的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 加入或離開車隊活動
    */
    class JoinOrLeaveTeamActivity
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emResult_Delete = -1,                // -1: 刪除
            emResult_None,                       // 0: 無動作
            emResult_Add,                        // 1: 新增
        }

        // 動作
        public int Action { get; set; }

        // 車隊ID
        public string TeamID { get; set; }

        // 活動ID
        public string ActID { get; set; }

        // 加入或離開的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 加入或離開車隊
    */
    class JoinOrLeaveTeam
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emResult_Delete = -1,                // -1: 刪除
            emResult_None,                       // 0: 無動作
            emResult_Add,                        // 1: 新增
        }

        // 動作
        public int Action { get; set; }

        // 車隊ID
        public string TeamID { get; set; }

        // 加入或離開的會員的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 踢離車隊成員
    */
    class KickTeamMember
    {
        // 車隊ID
        public string TeamID { get; set; }

        // 踢人的會員的MemberID
        public string MemberID { get; set; }

        // 被踢的會員的MemberID 列表
        public string KickIdList { get; set; }
    }

}
