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
}

namespace Packet.ClientToServer
{
    /**
     * 
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
}
