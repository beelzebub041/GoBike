
namespace UserPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emUserRegisteredResult = 1001,
        emUserLoginResult,
        emUpdateUserInfoResult,
        emUpdatePasswordResult,
        emUpdateFriendListResult,
        emUpdateBlackListResult,
        emUpdateNotifyTokenResult,
    }

    // 使用者註冊結果
    class UserRegisteredResult
    {   
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 註冊失敗
            emResult_Success,           // 1: 註冊成功
            emResult_AccountRepeat,     // 2: 帳號重複
            emResult_PwdError,          // 3: 密碼錯誤
            emResult_FormatError,       // 4: 帳號格式不符
        }

        // 結果
        public int Result { get; set; }
    }

    // 使用者登入結果
    class UserLoginResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 登入失敗
            emResult_Success,           // 1: 登入成功
            emResult_AccountError,      // 2: 帳號錯誤
            emResult_PwdError,          // 3: 密碼錯誤
        }

        // 結果
        public int Result { get; set; }

        /**
         * 會員ID
         */
        public string MemberID { get; set; }
    }

    // 更新使用者資訊結果
    class UpdateUserInfoResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 更新失敗
            emResult_Success,           // 1: 更新成功
        }

        // 結果
        public int Result { get; set; }
    }

    // 更新密碼結果
    class UpdatePasswordResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 更新失敗
            emResult_Success,           // 1: 更新成功
            emResult_OldPwdError,       // 2: 舊密碼錯誤
        }

        /**
         * 動作
         * 0: 無動作
         * 1: 更新密碼
         * 2: 忘記密碼
         */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    // 更新好友列表結果
    class UpdateFriendListResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 更新失敗
            emResult_Success,           // 1: 更新成功
        }

        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }

    // 更新黑名單列表結果
    class UpdateBlackListResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 更新失敗
            emResult_Success,           // 1: 更新成功
        }

        /**
          * 更新動作
          * -1: 無動作
          * 0: 新增
          * 1: 刪除
          */
        public int Action { get; set; }

        // 結果
        public int Result { get; set; }

    }


    // 更新推播Token結果
    class UpdateNotifyTokenResult
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

namespace UserPacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emUserRegistered = 1001,
        emUserLogin,
        emUpdateUserInfo,
        emUpdatePassword,
        emUpdateFriendList,
        emUpdateBlackList,
        emUpdateNotifyToken,
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

        // 手機規格型號
        public string SpecificationModel { get; set; }

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
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_None = 0,          // 0: 無動作
            emAction_UpdatePwd,         // 1: 更新密碼
            emAction_ForgetPwd,         // 2: 忘記密碼
        }

        // 動作
        public int Action { get; set; }

        // 使用者索引
        public string MemberID { get; set; }

        // 密碼
        public string Password { get; set; }

        // 新密碼
        public string NewPassword { get; set; }

    }

    /*
    * 更新好友列表
    */
    class UpdateFriendList
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 刪除
            emAction_None,                 // 0: 無動作
            emAction_Add,                  // 1: 新增
        }

        // 會員ID
        public string MemberID { get; set; }

        // 動作
        public int Action { get; set; }

        // 好友的會員ID
        public string FriendID { get; set; }
    }

    /*
    * 更新黑名單列表
    */
    class UpdateBlackList
    {
        // 動作定義
        public enum ActionDefine : int
        {
            emAction_Delete = -1,          // -1: 刪除
            emAction_None,                 // 0: 無動作
            emAction_Add,                  // 1: 新增
        }

        // 會員ID
        public string MemberID { get; set; }

        // 動作
        public int Action { get; set; }

        // 黑名單的會員ID
        public string BlackID { get; set; }
    }

    /*
    * 更新推播Token
    */
    class UpdateNotifyToken
    {
        // 會員ID
        public string MemberID { get; set; }

        // 使用者資料
        public string NotifyToken { get; set; }

    }

}

