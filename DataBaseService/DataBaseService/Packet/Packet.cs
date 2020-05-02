namespace Packet.Base
{
    class PacketBase
    {
        public string Name { get; set; }

        public string Data { get; set; }

    }
}

namespace Packet.ServerToClient
{
    class S2C_UserRegisteredResult
    {
        /**
         * 註冊結果
         * JSON範例
            {
	        
			    "Name":"S2C_UserRegisteredResult",
			    "Data":
			    {
				    "result":1
			    }
	        
            }
         * 
         * 0: 註冊成功
         * 1: 帳號重複
         * 2: 密碼錯誤
         * 3: 帳號格式不符
         */
        public int Result { get; set; }
    }

    class S2C_UserLoginResult
    {
        /**
         * 登入結果
         * JSON範例
            {
	        
			    "Name":"S2C_UserLoginResult",
			    "Data":
			    {
				    "result":1
			    }
	        
            }
         * 
         * 0: 登入失敗
         * 1: 登入成功
         * 2: 帳號或密碼錯誤
         */
        public int Result { get; set; }
    }
}

namespace Packet.ClientToServer
{
    /**
     * 使用者註冊
     * 
     * JSON範例
        {
		    "Name":"C2S_UserRegistered",
		    "Data":
		    {
			    "account":"123456@hotmail.com",
			    "password":"Aa123456",
			    "checkPassword":"Aa123456"
		    }

	    }
     */
    class C2S_UserRegistered
    {
        // 帳號
        public string Account { get; set; }

        // 密碼
        public string Password { get; set; }

        // 確認密碼
        public string CheckPassword { get; set; }
    }

    /**
     * 使用者登入
     * 
     * JSON範例
        {
		    "Name":"C2S_UserLogin",
		    "Data":
		    {
			    "account":"123456@hotmail.com",
			    "password":"Aa123456"
		    }

	    }
     */
    class C2S_UserLogin
    {
        // 帳號
        public string Account { get; set; }

        // 密碼
        public string Password { get; set; }

    }
}
