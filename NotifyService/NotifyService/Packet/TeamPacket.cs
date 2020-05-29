
namespace TeamPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateNewTeamResult = 1001,
        emUpdateTeamDataResult,
        emChangeLanderResult,
        emUpdateViceLeaderListResult,
        emUpdateTeamMemberListResult,
        emUpdateApplyJoinListResult,
        emUpdateInviteJoinListResult,
        emUpdateBulletinResult,
        emUpdateActivityResult,
        emDeleteTeamResult,
        emJoinOrLeaveTeamActivityResult,
        emJoinOrLeaveTeamResult,

    }

    // 建立新車隊
    class CreateNewTeamResult
    {
        /*
        * 0: 建立失敗
        * 1: 建立成功
        * 2: 重複擔任隊長
        */
        public int Result { get; set; }

        // 車隊ID
        public string TeamID { get; set; }
    }

    // 更新車隊資料
    class UpdateTeamDataResult
    {
        /*
        * 0: 更新失敗
        * 1: 更新成功
        * 2: 權限不足
        */
        public int Result { get; set; }

    }

    // 更換隊長結果
    class ChangeLanderResult
    {
        /*
        * 0: 改變失敗
        * 1: 改變成功
        * 2: 重複擔任
        * 3: 權限不足
        */
        public int Result { get; set; }
    }

    // 更新副隊長列表結果
    class UpdateViceLeaderListResult
    {
        /**
         * 更新動作
         * -1: 無動作
         * 0: 新增
         * 1: 刪除
         */
        public int Action { get; set; }

        /*
        * 0: 更新失敗
        * 1: 更新成功
        * 2: 權限不足
        */
        public int Result { get; set; }

    }

    // 更新隊員列表結果
    class UpdateTeamMemberListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 更新失敗
        * 1: 更新成功
        */
        public int Result { get; set; }

    }

    // 更新申請加入車隊列表結果
    class UpdateApplyJoinListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }

    // 更新邀請加入車隊列表結果
    class UpdateInviteJoinListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }

    // 更新公告結果
    class UpdateBulletinResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          * 2: 修改
          */
        public int Action { get; set; }

        /*
        * 0: 更新失敗
        * 1: 更新成功
        * 2: 修改權限不足
        */
        public int Result { get; set; }

        // 公告ID
        public string BulletinID { get; set; }

    }

    // 更新活動結果
    class UpdateActivityResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          * 2: 修改
          */
        public int Action { get; set; }

        /*
        * 0: 更新失敗
        * 1: 更新成功
        * 2: 修改權限不足
        */
        public int Result { get; set; }

        // 活動ID
        public string ActID { get; set; }

    }

    // 解散車隊結果
    class DeleteTeamResult
    {
        /*
        * 0: 解散失敗
        * 1: 解散成功
        * 2: 權限不足
        */
        public int Result { get; set; }

    }

    // 加入或離開車隊活動結果
    class JoinOrLeaveTeamActivityResult
    {
        /*
        * -1: 離開 
        * 0: 無動作
        * 1: 加入
        */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }

    // 加入或離開車隊結果
    class JoinOrLeaveTeamResult
    {
        /*
        * -1: 離開 
        * 0: 無動作
        * 1: 加入
        */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
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
        emUpdateTeamMemberList,
        emUpdateApplyJoinList,
        emUpdateInviteJoinList,
        emUpdateBulletin,
        emUpdateActivity,
        emDeleteTeam,
        emJoinOrLeaveTeamActivity,
        emJoinOrLeaveTeam,

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

    /*
    * 更新車隊隊員列表
    */
    class UpdateTeamMemberList
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

        // 隊員的MemberID
        public string MemberID { get; set; }

    }

    /*
    * 更新申請加入車隊列表
    */
    class UpdateApplyJoinList
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

        // 隊員的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 更新邀請加入車隊列表
    */
    class UpdateInviteJoinList
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

        // 隊員的MemberID
        public string MemberID { get; set; }
    }

    /*
    * 更新公告
    */
    class UpdateBulletin
    {
        /**
        * 更新動作
        * -1: 刪除
        * 0: 無動作
        * 1: 新增
        * 2: 修改
        */
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
        /**
        * 更新動作
        * -1: 刪除
        * 0: 無動作
        * 1: 新增
        * 2: 修改
        */
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
        public float TotalDistance { get; set; }

        // 最高海拔
        public float MaxAltitude { get; set; }

        // 路線
        public string Route { get; set; }

        // 路線描述
        public string Description { get; set; }

        // 活動地圖
        public string Photo { get; set; }

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
        /**
         * -1: 離開
         * 0: 無動作
         * 1: 加入
         */
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
        /**
         * -1: 離開
         * 0: 無動作
         * 1: 加入
         */
        public int Action { get; set; }

        // 車隊ID
        public string TeamID { get; set; }

        // 加入或離開的會員的MemberID
        public string MemberID { get; set; }
    }

}
