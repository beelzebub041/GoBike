
namespace UserPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emUserRegisteredResult = 1001,
        emUserLoginResult,
        emUpdateUserInfoResult,
        emUpdatePasswordResult,
        emUpdateTeamListResult,
        emUpdateFriendListResult,
        emUpdateBlackListResult
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
         * 會員ID
         */
        public string MemberID { get; set; }
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

    // 更新車隊列表結果
    class UpdateTeamListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }

    // 更新好友列表結果
    class UpdateFriendListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }

    // 更新黑名單列表結果
    class UpdateBlackListResult
    {
        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        /*
        * 0: 失敗
        * 1: 成功
        */
        public int Result { get; set; }

    }


}

namespace UserPacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emUserRegistered = 1001,
        emUserLogin,
        emUpdateUserInfo,
        emUpdatePassword,
        emUpdateTeamList,
        emUpdateFriendList,
        emUpdateBlackList,
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

        // 首頁圖片路徑
        public string Photo { get; set; }

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
        // 會員ID
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

    /*
    * 更新車隊列表
    */
    class UpdateTeamList
    {
        // 會員ID
        public string MemberID { get; set; }

        /**
        * 更新動作
        * -1: 刪除
        * 0: 無動作
        * 1: 新增
        */
        public int Action { get; set; }

        // 車隊ID
        public string TeamID { get; set; }
    }

    /*
    * 更新好友列表
    */
    class UpdateFriendList
    {
        // 會員ID
        public string MemberID { get; set; }

        /**
        * 更新動作
        * -1: 刪除
        * 0: 無動作
        * 1: 新增
        */
        public int Action { get; set; }

        // 好友的會員ID
        public string FriendID { get; set; }
    }

    /*
    * 更新黑名單列表
    */
    class UpdateBlackList
    {
        // 會員ID
        public string MemberID { get; set; }

        /**
        * 更新動作
        * -1: 刪除
        * 0: 無動作
        * 1: 新增
        */
        public int Action { get; set; }

        // 黑名單的會員ID
        public string BlackID { get; set; }
    }


}

