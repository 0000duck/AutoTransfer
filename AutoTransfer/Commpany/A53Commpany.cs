
namespace AutoTransfer.Commpany
{
    public class A53Commpany : CommpanyCommon
    {
        /// <summary>
        /// (國內)標普本國貨幣長期發行人信用評等
        /// </summary>
        public string RTG_SP_LT_LC_ISSUER_CREDIT { get; set; }

        /// <summary>
        /// (國內)標普本國貨幣長期發行人信用評等日期
        /// </summary>
        public string RTG_SP_LT_LC_ISS_CRED_RTG_DT { get; set; }

        /// <summary>
        /// 標普長期外幣發行人信用評等
        /// </summary>
        public string RTG_SP_LT_FC_ISSUER_CREDIT { get; set; }

        /// <summary>
        /// 標普長期外幣發行人信用評等日期
        /// </summary>
        public string RTG_SP_LT_FC_ISS_CRED_RTG_DT { get; set; }
    }
}
