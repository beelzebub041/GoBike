namespace Packet.Base
{
    class PacketBase
    {
        public int cmdID { get; set; }

        public string data { get; set; }

    }
}

namespace Packet.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emUserRegisteredResult = 1001,
        emUserLoginResult,
        emUserLogoutResult,
        emUpdateUserInfoResult,
    }
    
    // ======================= User ======================= //
    
    // 使用者註冊結果
    class UserRegisteredResult
    {
        /*
        * 0: 註冊成功
        * 1: 帳號重複
        * 2: 密碼錯誤
        * 3: 帳號格式不符
        */
        public int result { get; set; }
    }

    // 使用者登入結果
    class UserLoginResult
    {
        /*
        * 0: 登入成功
        * 1: 帳號錯誤
        * 2: 密碼錯誤
        */
        public int result { get; set; }
    }

    // 使用者登入結果
    class UserLogoutResult
    {
        /*
        * 0: 登出成功
        * 1: 登出失敗
        */
        public int result { get; set; }
    }

    // 更新使用者資訊結果
    class UpdateUserInfoResult
    {
        /*
        * 0: 更新成功
        * 1: 更新失敗
        */
        public int result { get; set; }
    }
}

namespace Packet.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emUserRegistered = 1001,
        emUserLogin,
        emUserLogout,
        emUpdateUserInfo,
    }

    // ======================= User ======================= //

    /*
    * 使用者註冊
    */
    class UserRegistered
    {
        // Email
        public string email { get; set; }

        // 密碼
        public string password { get; set; }

        // 確認密碼
        public string checkPassword { get; set; }
    }

    /*
    * 使用者登入
    */
    class UserLogin
    {
        // Email
        public string email { get; set; }

        // 密碼
        public string password { get; set; }

    }

    /*
    * 使用者登出
    */
    class UserLogout
    {
        // Email
        public string email { get; set; }

    }

    /*
    * 更新使用者資訊
    */
    class UpsateUserInfo
    {
        // 暱稱
        public string nikeName { get; set; }

        // 生日
        public string birthday { get; set; }

        // 國家
        public string country { get; set; }

    }


}
