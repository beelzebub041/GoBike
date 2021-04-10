
namespace Service.Source.Define
{
    public enum NotifyID : int
    {
        None = -1,

        System = 0,                     // 系統公告, 公告GoBike相關事項
        Team_ApplyJoinTeam,             // 車隊, 會員申請加入車隊
        Team_MemberJoinTeam,            // 車隊, 會員加入車隊
        Team_MemberJoined,              // 車隊, 會員已加入車隊
        Team_MemberLeaveTeam,           // 車隊, 會員離開車隊
        Team_MemberLeaved,              // 車隊, 會員已離開車隊
        Team_ChangeLeader,              // 車隊, 更換隊長
        Team_NewBulletin,               // 車隊, 新公告
        Team_NewActivity,               // 車隊, 新活動
        Team_ModifyActivityContent,     // 車隊, 更動活動內容
        Ride_RideGroupInvite,           // 騎乘, 組隊邀請
        Ride_JoinRideGroup,             // 騎乘, 加入組隊
        Ride_RefuseRideGroup,           // 騎乘, 拒絕組隊
        Ride_LeaveRideGroup,            // 騎乘, 離開組隊
        Ride_UpdateCoordinate,          // 騎乘, 更新座標
        Ride_NotifyWarning,             // 騎乘, 緊急通知
        Ride_CancelNotifyWarning,       // 騎乘, 取消緊急通知
        User_AddFriend,                 // 使用者, 新增好友
        Team_DisbandTeam,               // 車隊, 解散車隊

    }
}
