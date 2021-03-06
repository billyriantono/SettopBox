namespace NewCamd
{
    public enum NewCamdMessageType
    {
        MsgKeyblockReq1 = 128,
        MsgKeyblockReq2 = 129,
        MsgClient2ServerLogin = 224,
        MsgClient2ServerLoginAck,
        MsgClient2ServerLoginNak,
        MsgCardDataReq,
        MsgCardData,
        MsgServer2ClientName,
        MsgServer2ClientNameAck,
        MsgServer2ClientNameNak,
        MsgServer2ClientLogin,
        MsgServer2ClientLoginAck,
        MsgServer2ClientLoginNak,
        MsgAdmin,
        MsgAdminAck,
        MsgAdminLogin,
        MsgAdminLoginAck,
        MsgAdminLoginNak,
        MsgAdminCommand,
        MsgAdminCommandAck,
        MsgAdminCommandNak,
        MsgKeepalive = 253,
    }
}