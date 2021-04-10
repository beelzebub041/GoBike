
namespace PostPacket.ServerToClient
{
    public enum S2C_CmdID : int
    {
        emCreateNewPostResult = 1001,
        emUpdatePostResult,
        emDeletePostResult,
        emAddLikeResult,
        emReduceLikeResult,
    }

    // 新增貼文結果
    class CreateNewPostResult
    {   
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 新增失敗
            emResult_Success,           // 1: 新增成功
        }

        // 結果
        public int Result { get; set; }
    }

    // 更新貼文結果
    class UpdatePostResult
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

    // 刪除貼文結果
    class DeletePostResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 刪除失敗
            emResult_Success,           // 1: 刪除成功
        }

        // 結果
        public int Result { get; set; }
    }

    // 新增點讚數結果
    class AddLikeResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 新增失敗
            emResult_Success,           // 1: 新增成功
        }

        // 結果
        public int Result { get; set; }

    }

    // 減少點讚數結果
    class ReduceLikeResult
    {
        // 結果定義
        public enum ResultDefine : int
        {
            emResult_Fail = 0,          // 0: 減少失敗
            emResult_Success,           // 1: 減少成功
        }

        // 結果
        public int Result { get; set; }

    }

}

namespace PostPacket.ClientToServer
{
    public enum C2S_CmdID : int
    {
        emCreateNewPost = 1001,
        emUpdatePost,
        emDeletePost,
        emAddLike,
        emReduceLike,
    }

    // ======================= User ======================= //

    /*
    * 建立貼文
    */
    class CreateNewPost
    {
        // 會員ID
        public string MemberID { get; set; }

        // 地點
        public string Place { get; set; }

        // 圖片
        public string Photo { get; set; }

        // 內文
        public string Content { get; set; }

    }

    /*
    * 更新貼文
    */
    class UpdatePost
    {
        // 會員ID
        public string MemberID { get; set; }

        // 貼文ID
        public string PostID { get; set; }

        // 地點
        public string Place { get; set; }

        // 圖片
        public string Photo { get; set; }

        // 內文
        public string Content { get; set; }

    }

    /**
     * 刪除貼文
     */
    class DeletePost
    {
        // 會員ID
        public string MemberID { get; set; }

        // 貼文ID
        public string PostID { get; set; }

    }

    /*
    * 新增點讚數
    */
    class AddLike
    {
        // 貼文ID
        public string PostID { get; set; }

        // 回覆者的會員ID
        public string MemberID { get; set; }

    }

    /*
    * 減少點讚數
    */
    class ReduceLike
    {
        // 貼文ID
        public string PostID { get; set; }

        // 回覆者的會員ID
        public string MemberID { get; set; }

    }

}

