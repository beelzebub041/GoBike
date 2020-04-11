using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SqlSugar;

namespace UserService.Def
{   
    // 使用者帳戶
    public class UserAccount
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FBToken { get; set; }
        public string GoogleToken { get; set; }
        public int RegisterSource { get; set; }
        public string RegisterDate { get; set; }
        public string LoginDate { get; set; }

    }

    // 使用者資料
    public class UserInfo
    {
        public string Index { get; set; }
        public string NickName { get; set; }
        public string Birthday { get; set; }
        public float BodyHeight { get; set; }
        public float BodyWeight { get; set; }
        public string FrontCoverUrl { get; set; }
        public string PhotoUrl { get; set; }
        public string Mobile { get; set; }
        public int Gender { get; set; }
        public int Country { get; set; }

    }

    // 騎乘資料
    public class RideData
    {
        public string Index { get; set; }
        public float TotalDistance { get; set; }
        public float TotalAltitude { get; set; }
        public Int64 TotalRideTime { get; set; }

    }
}
