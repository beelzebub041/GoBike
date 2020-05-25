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
        // 會員ID
        public string MemberID { get; set; }

        // Email
        public string Email { get; set; }

        // 密碼
        public string Password { get; set; }

        // FB Token
        public string FBToken { get; set; }

        // Google Token
        public string GoogleToken { get; set; }

        // 註冊來源
        public int RegisterSource { get; set; }

        // 註冊日期
        public string RegisterDate { get; set; }

    }

    // 使用者資料
    public class UserInfo
    {
        // 會員ID
        public string MemberID { get; set; }

        // 暱稱
        public string NickName { get; set; }

        // 生日
        public string Birthday { get; set; }

        // 身高
        public float BodyHeight { get; set; }

        // 體重
        public float BodyWeight { get; set; }

        // 封面
        public string FrontCover { get; set; }

        // 玩家頭像
        public string Avatar { get; set; }

        //首頁圖片
        public string Photo { get; set; }

        // 手機認證
        public string Mobile { get; set; }

        // 性別
        public int Gender { get; set; }

        // 居住地
        public int County { get; set; }

        // 車隊列表 (紀錄車隊ID)
        public string TeamList { get; set; }

        // 好友列表 (紀錄會員ID)
        public string FriendList { get; set; }

        // 黑名單列表 (紀錄會員ID)
        public string BlackList { get; set; }

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
        public int County { get; set; }

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
        public int County { get; set; }

        // 是否開放搜尋
        public int SearchStatus { get; set; }

        // 加入車隊是否需要審核
        public int ExamineStatus { get; set; }

        // 申請加入的會員ID列表
        public string ApplyJoinList { get; set; }

        // 邀請加入的會員ID列表
        public string InviteJoinList { get; set; }

    }

    // 車隊公告
    public class TeamBulletin
    {
        // 公告ID
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


    // 車隊活動
    public class TeamActivity
    {
        // 活動ID
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

    // 車隊資料暫存區
    public class TeamDataStorageCache
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
        public int County { get; set; }

        // 是否開放搜尋
        public int SearchStatus { get; set; }

        // 加入車隊是否需要審核
        public int ExamineStatus { get; set; }

        // 申請加入的會員ID列表
        public string ApplyJoinList { get; set; }

        // 邀請加入的會員ID列表
        public string InviteJoinList { get; set; }

        // 暫存日期
        public string StorageDate { get; set; }

    }

}
