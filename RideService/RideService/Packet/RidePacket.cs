
namespace Packet.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateRideRecordResult = 1001,
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

}

namespace Packet.ClientToServer
{
    public enum C2S_CmdID : int
    {
        CreateRideRecord = 1001,
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
        public int CountyID { get; set; }

        // 騎乘路線
        public string Route { get; set; }

        // 分享內容
        public string ShareContent { get; set; }

        // 分享類型
        public int SharedType { get; set; }
        
    }

}
