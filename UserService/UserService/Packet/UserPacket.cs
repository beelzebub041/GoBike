
namespace Packet.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emUserRegisteredResult = 1001,
        emUserLoginResult,
        emUpdateUserInfoResult,
        emUpdatePasswordResult,
    }

    // 登入資料
    class UserLoginData
    {
        public string MemberID { get; set; }
        public string Nickname { get; set; }
        public string Avatar { get; set; }
        public string Birthday { get; set; }
        public float BodyHeight { get; set; }
        public float BodyWeight { get; set; }
        public int Country { get; set; }
        public string Email { get; set; }
        public string FBToken { get; set; }
        public string GoogleToken { get; set; }
        public string FrontCover { get; set; }
        public int Gender { get; set; }
        public string LoginDate { get; set; }
        public string Mobile { get; set; }
        public string RegisterDate { get; set; }
        public int RegisterSource { get; set; }
        public double TotalAltitude { get; set; }
        public double TotalDistance { get; set; }
        public long TotalRideTime { get; set; }
    }


    // 使用者註冊結果
    class UserRegisteredResult
    {
        /*
        * 0: 註冊失敗
        * 1: 註冊成功
        * 2: 帳號重複
        * 3: 密碼錯誤
        * 4: 帳號格式不符
        */
        public int Result { get; set; }
    }

    // 使用者登入結果
    class UserLoginResult
    {
        /*
        * 0: 登入失敗
        * 1: 登入成功
        * 2: 帳號錯誤
        * 3: 密碼錯誤
        */
        public int Result { get; set; }

        /**
         * 登入資料
         */
        public UserLoginData LoginData { get; set; }
    }

    // 更新使用者資訊結果
    class UpdateUserInfoResult
    {
        /*
        * 0: 更新失敗
        * 1: 更新成功
        */
        public int Result { get; set; }
    }

    // 更新密碼結果
    class UpdatePasswordResult
    {
        /*
        * 0: 更新失敗
        * 1: 更新成功
        * 1: 舊密碼錯誤
        */
        public int Result { get; set; }

    }
}

namespace Packet.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emUserRegistered = 1001,
        emUserLogin,
        emUpdateUserInfo,
        emUpdatePassword,
    }

    // ======================= User ======================= //

    /*
    * 使用者註冊
    */
    class UserRegistered
    {
        // Email
        public string Email { get; set; }

        // 密碼
        public string Password { get; set; }

        // 確認密碼
        public string CheckPassword { get; set; }

        // FB Token
        public string FBToken { get; set; }

        // Google Token
        public string GoogleToken { get; set; }

        // 註冊來源
        public int RegisterSource { get; set; }
    }

    /*
    * 使用者登入
    */
    class UserLogin
    {
        // Email
        public string Email { get; set; }

        // 密碼
        public string Password { get; set; }

    }

    /**
     * 更新資訊
     */
    class UpdateInfo
    {
        // 暱稱
        public string NickName { get; set; }

        // 登入資料
        public string LoginData { get; set; }

        // 生日
        public string Birthday { get; set; }

        // 身高
        public float BodyHeight { get; set; }

        // 體重
        public float BodyWeight { get; set; }

        // 封面路徑
        public string FrontCover { get; set; }

        // 頭像路徑
        public string Avatar { get; set; }

        // 手機認證
        public string Mobile { get; set; }

        // 性別
        public int Gender { get; set; }

        // 國家
        public int Country { get; set; }

    }

    /*
    * 更新使用者資訊
    */
    class UpdateUserInfo
    {
        // 使用者索引
        public string MemberID { get; set; }

        // 使用者資料
        public UpdateInfo UpdateData { get; set; }

    }

    /*
    * 更新密碼
    */
    class UpdatePassword
    {
        // 使用者索引
        public string MemberID { get; set; }

        // 密碼
        public string Password { get; set; }

        // 新密碼
        public string NewPassword { get; set; }

    }


}
