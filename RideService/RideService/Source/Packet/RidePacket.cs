
namespace RidePacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateRideRecordResult = 1001,
        emUpdateRideGroupResult,
        emUpdateInviteListResult,
        emReplyRideGroupResult,
        emUpdateCoordinateResult,
        emNotifyRideGroupMemberResult,
    }

    /**
     * 建立騎乘紀錄結果
     */
    class CreateRideRecordResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 建立失敗
            emResult_Success,           // 1: 建立成功
        }

        // 結果
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
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 失敗
            emResult_Success,           // 1: 成功
            emResult_RideGroupRepeat,   // 2: 組隊已存在
        }

        /**
         * 更新動作
         * -1: 刪除
         * 0: 無動作
         * 1: 新增
         */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    /**
     * 更新邀請列表結果
     */
    class UpdateInviteListResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 失敗
            emResult_Success,           // 1: 成功
        }

        /**
         * 更新動作
         * -1: 刪除
         * 0: 無動作
         * 1: 新增
         */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    /**
     * 回覆組隊騎乘
     */
    class ReplyRideGroupResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 失敗
            emResult_Success,           // 1: 成功
        }

        // 結果
        public int Result { get; set; }

    }

    /**
     * 更新座標結果
     */
    class UpdateCoordinateResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 失敗
            emResult_Success,           // 1: 成功
        }

        // 結果
        public int Result { get; set; }

    }

    /**
     * 通知隊友結果
     */
    class NotifyRideGroupMemberResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 失敗
            emResult_Success,           // 1: 成功
        }

        // 結果
        public int Result { get; set; }

    }

}

namespace RidePacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emCreateRideRecord = 1001,
        emUpdateRideGroup,
        emUpdateInviteList,
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
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 刪除
            emAction_None,                 // 0: 無動作
            emAction_Add,                  // 1: 新增
        }

        // 動作
        public int Action { get; set; }


        // 建立組隊的會員ID
        public string MemberID { get; set; }

        // 邀請列表
        public string InviteList { get; set; }

    }

    /*
    * 更新邀請列表
    */
    class UpdateInviteList
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 刪除
            emAction_None,                 // 0: 無動作
            emAction_Add,                  // 1: 新增
        }

        // 動作
        public int Action { get; set; }


        // 新增/刪除組隊邀請列表的會員ID
        public string MemberID { get; set; }

        // 更新列表
        public string UpdateList { get; set; }

    }

    /*
    * 回覆組隊騎乘
    */
    class ReplyRideGroup
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 拒絕
            emAction_None,                 // 0: 無動作
            emAction_Join,                 // 1: 加入
            emAction_Leave,                // 1: 離開
        }

        // 動作
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
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 取消
            emAction_None,                 // 0: 無動作
            emAction_Add,                  // 1: 新增
        }

        // 動作
        public int Action { get; set; }


        // 發出通知的會員ID
        public string MemberID { get; set; }

    }
}
