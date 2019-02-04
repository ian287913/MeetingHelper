namespace Controller.Message
{
    public enum MessageType
    {
        /// <summary>
        /// 成功進入房間。攜帶資訊(主席名稱)。
        /// </summary>
        Success,
        /// <summary>
        /// 拒絕服務。
        /// </summary>
        Forbidden,
        /// <summary>
        /// 密碼錯誤。攜帶資訊(剩餘次數)。
        /// </summary>
        Unauthorized,
        /// <summary>
        /// 名稱重複。
        /// </summary>
        Conflict,
        /// <summary>
        /// 加入房間。攜帶資訊(使用者名稱, 密碼)。
        /// </summary>
        JoinRoom,
        /// <summary>
        /// 有人成功進入房間。攜帶資訊(使用者名稱)。
        /// </summary>
        SbJoin,
        /// <summary>
        /// 有人離開房間。攜帶資訊(使用者名稱)。
        /// </summary>
        SbLeave,
        /// <summary>
        /// U2H 要求使用者清單(包括自己)；H2U 主機傳送清單給使用者。攜帶資訊(使用者名稱清單)。
        /// </summary>
        UserList,
        /// <summary>
        /// U2H 要求發言權；H2U 給予發言權。攜帶資訊(使用者名稱)。
        /// </summary>
        MicCapture,
        /// <summary>
        /// 失去發言權。
        /// </summary>
        MicMissing,
        /// <summary>
        /// 目前發言者名稱。攜帶資訊(使用者名稱)。
        /// </summary>
        MicOwner,
        /// <summary>
        /// 允許發言權要求。攜帶資訊(使用者名稱)。
        /// </summary>
        Accept,
        /// <summary>
        /// 取消發言權要求。攜帶資訊(使用者名稱)。
        /// </summary>
        Refuse,
        /// <summary>
        /// 有人請求發言。攜帶資訊(使用者名稱)。
        /// </summary>
        Request
    }
}
