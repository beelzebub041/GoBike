
namespace ClientPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emJoinServiceResult = 1001,
        emNotifyTeamBulletin,
        emNotifyTeamActivity,
    }

    /*
    * 使用者加入結果
    */
    class JoinServiceResult
    {
        /*
         * 加入結果
         * 
         * 0: 加入失敗
         * 1: 加入成功
         * 2: 帳號錯誤
         * 3: 密碼錯誤
        */
        public int Result { get; set; }

    }

    /**
     * 車隊公告通知
     */
    class NotifyTeamBulletin
    {
        // 車隊ID
        public string TeamID { get; set; }

        // 發出公告的會員的會員ID
        public string MemberID { get; set; }

        // 公告內容
        public string Content { get; set; }

        // 公告天數
        public int Day { get; set; }

    }

    /*
    * 車隊活動通知
    */
    class NotifyTeamActivity
    {
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

    }

}

namespace ClientPacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emJoinService = 1001,
    }

    /*
    * 使用者加入
    */
    class JoinService
    {
        // Email
        public string Email { get; set; }

        // 密碼
        public string Password { get; set; }

    }

}

