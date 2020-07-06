
namespace RidePacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateRideRecordResult = 1001,
        emUpdateRideGroupResult,
        emReplyRideGroupResult,
        emUpdateCoordinateResult,
        emNotifyRideGroupMemberResult,
    }

    /**
     * 建立騎乘紀錄結果
     */
    class CreateRideRecordResult
    {
        /*
        * 0: 建立失敗
        * 1: 建立成功
        */
        public int Result { get; set; }

        // 總距離
        public float TotalDistance { get; set; }

        // 總高度
        public float TotalAltitude { get; set; }

        // 總騎乘時間
        public long TotalRideTime { get; set; }
    }

    /**
     * 更新組隊騎乘結果
     */
    class UpdateRideGroupResult
    {
        /**
         * 更新動作
         * -1: 刪除
         * 0: 無動作
         * 1: 新增
         */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        * 2: 組隊已存在
        */
        public int Result { get; set; }

    }

    /**
     * 回覆組隊騎乘
     */
    class ReplyRideGroupResult
    {
        /*
        * 0: 回覆失敗
        * 1: 回覆成功
        */
        public int Result { get; set; }

    }

    /**
     * 更新座標結果
     */
    class UpdateCoordinateResult
    {
        /*
        * 0: 更新失敗
        * 1: 更新成功
        */
        public int Result { get; set; }

    }

    /**
     * 通知隊友結果
     */
    class NotifyRideGroupMemberResult
    {
        /*
        * 0: 通知失敗
        * 1: 通知成功
        */
        public int Result { get; set; }

    }

}

namespace RidePacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emCreateRideRecord = 1001,
        emUpdateRideGroup,
        emReplyRideGroup,
        emUpdateCoordinate,
        emNotifyRideGroupMember,
    }

    /*
    * 建立騎乘紀錄
    */
    class CreateRideRecord
    {
        // 使用者索引
        public string MemberID { get; set; }

        // 建立日期
        public string CreateDate { get; set; }

        // 標題
        public string Title { get; set; }

        // 封面圖片
        public string Photo { get; set; }

        // 騎乘時間
        public long Time { get; set; }

        // 騎乘距離
        public float Distance { get; set; }

        // 騎乘坡度
        public float Altitude { get; set; }

        // 等級
        public int Level { get; set; }

        // 鄉鎮地區
        public int County { get; set; }

        // 騎乘路線
        public string Route { get; set; }

        // 分享內容
        public string ShareContent { get; set; }

        // 分享類型
        public int SharedType { get; set; }
        
    }

    /*
    * 更新組隊騎乘
    */
    class UpdateRideGroup
    {
        /**
         * 更新動作
         * -1: 刪除
         * 0: 無動作
         * 1: 新增
         */
        public int Action { get; set; }


        // 建立組隊的會員ID
        public string MemberID { get; set; }

        // 邀請列表
        public string InviteList { get; set; }

    }

    /*
    * 回覆組隊騎乘
    */
    class ReplyRideGroup
    {
        /**
        * 更新動作
        * -1: 拒絕
        * 0: 無動作
        * 1: 加入
        */
        public int Action { get; set; }


        // 執行動作的會員ID
        public string MemberID { get; set; }

    }

    /*
    * 更新座標
    */
    class UpdateCoordinate
    {
        // 更新座標的會員ID
        public string MemberID { get; set; }

        // 座標X
        public string CoordinateX { get; set; }

        // 座標Y
        public string CoordinateY { get; set; }

    }

    /*
    * 通知隊友
    */
    class NotifyRideGroupMember
    {
        /**
        * 更新動作
        * -1: 取消通知
        * 0: 無動作
        * 1: 新增通知
        */
        public int Action { get; set; }


        // 發出通知的會員ID
        public string MemberID { get; set; }

    }
}
