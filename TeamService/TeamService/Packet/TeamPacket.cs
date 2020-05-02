
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
    }

    // 建立新車隊
    class CreateNewTeamResult
    {
        /*
        * 0: 建立失敗
        * 1: 建立成功
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
        */
        public int Result { get; set; }

    }

    // 更換隊長結果
    class ChangeLanderResult
    {
        /*
        * 0: 改變失敗
        * 1: 改變成功
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
        public int CountyID { get; set; }

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

        // 車隊名稱
        public string TeamName { get; set; }

        // 車隊簡介
        public string TeamInfo { get; set; }

        // 車隊頭像
        public string Avatar { get; set; }

        // 車隊封面
        public string FrontCover { get; set; }

        // 車隊所在地
        public int CountyID { get; set; }

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


}
