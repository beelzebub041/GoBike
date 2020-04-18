
namespace Packet.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emUpdateRideDataResult = 1001,
        emCreateRideRecordResult,
        //emRespondRideRecordIdList,
        //emRespondRideRecord,
    }

    /**
     * 更新騎乘資料結果
     */
    class UpdateRideDataResult
    {
        /*
        * 0: 更新失敗
        * 1: 更新成功
        */
        public int Result { get; set; }
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
    }

    ///**
    // * 回應騎乘紀錄ID列表
    // */
    //class RespondRideRecordIdList
    //{
    //    // 使用者索引
    //    public string UserID { get; set; }

    //    // 騎乘紀錄ID列表
    //    public string[] RideIDList { get; set; }     
    //}

    ///**
    // * 回應騎乘紀錄
    // */
    //class RespondRideRecord
    //{
    //    // 騎乘ID
    //    public string RideID { get; set; }

    //    // 使用者索引
    //    public string UserID { get; set; }

    //    // 建立日期
    //    public string CreateDate { get; set; }

    //    // 標題
    //    public string Title { get; set; }

    //    // 封面圖片
    //    public string Photo { get; set; }

    //    // 騎乘時間
    //    public long Time { get; set; }

    //    // 騎乘距離
    //    public float Distance { get; set; }

    //    // 騎乘坡度
    //    public float Altitude { get; set; }

    //    // 等級
    //    public int Level { get; set; }

    //    // 鄉鎮地區
    //    public int CountyID { get; set; }

    //    // 騎乘路線
    //    public string Route { get; set; }

    //    // 分享內容
    //    public string ShareContent { get; set; }

    //    // 分享類型
    //    public int SharedType { get; set; }
        
    //}
}

namespace Packet.ClientToServer
{
    public enum C2S_CmdID : int
    {
        UpdateRideData = 1001,
        CreateRideRecord,
        //GetRideRecordIdList,
        //GetRideRecord,
    }

    /*
    * 更新騎乘資料
    */
    class UpdateRideData
    {
        // Email
        public string Email { get; set; }

        // 使用者索引
        public string UserID { get; set; }

        // 總距離
        public float TotalDistance { get; set; }

        // 總高度
        public float TotalAltitude { get; set; }

        // 總騎乘時間
        public long TotalRideTime { get; set; }
        
    }

    /*
    * 建立騎乘紀錄
    */
    class CreateRideRecord
    {
        // 使用者索引
        public string UserID { get; set; }

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

    ///*
    //* 取得騎乘紀錄ID列表
    //*/
    //class GetRideRecordIdList
    //{
    //    // 使用者索引
    //    public string UserID { get; set; }

    //}

    ///*
    //* 取得騎乘紀錄
    //*/
    //class GetRideRecord
    //{
    //    // 使用者索引
    //    public string UserID { get; set; }

    //    // 騎乘ID
    //    public string RideID { get; set; }

    //}


}
