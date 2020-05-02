using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataBaseDef
{
    // 使用者帳戶
    public class UserAccount
    {
        public string MemberID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FBToken { get; set; }
        public string GoogleToken { get; set; }
        public int RegisterSource { get; set; }
        public string RegisterDate { get; set; }

    }

    // 使用者資料
    public class UserInfo
    {
        public string MemberID { get; set; }
        public string Avatar { get; set; }
        public string NickName { get; set; }
        public string Birthday { get; set; }
        public float BodyHeight { get; set; }
        public float BodyWeight { get; set; }
        public string FrontCover { get; set; }
        public string Mobile { get; set; }
        public int Gender { get; set; }
        public int Country { get; set; }

    }

    // 騎乘資料
    public class RideData
    {
        // 使用者索引
        public string MemberID { get; set; }

        // 總距離
        public float TotalDistance { get; set; }

        // 總高度
        public float TotalAltitude { get; set; }

        // 總騎乘時間
        public Int64 TotalRideTime { get; set; }

    }

    // 騎乘紀錄
    public class RideRecord
    {
        // 騎乘記錄索引
        public string RideID { get; set; }

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

    // 本周騎乘資料
    public class WeekRideData
    {
        // 使用者索引
        public string MemberID { get; set; }

        // 本周開始日期
        public string WeekFirstDay { get; set; }

        // 本周結束日期
        public string WeekLastDay { get; set; }

        // 本周騎乘距離
        public float WeekDistance { get; set; }

    }

    // 車隊資料
    public class TeamData
    {
        // 車隊ID
        public string TeamID { get; set; }

        // 建立日期
        public string CreateDate { get; set; }

        // 車隊長的MemberID
        public string Leader { get; set; }

        // 副隊長的MemberID列表
        public string TeamViceLeaderIDs { get; set; }

        // 車隊成員的MemberID列表
        public string TeamMemberIDs { get; set; }

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

        // 申請加入的會員ID列表
        public string ApplyJoinList { get; set; }

        // 邀請加入的會員ID列表
        public string InviteJoinList { get; set; }

    }

}
