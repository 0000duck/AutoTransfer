namespace AutoTransfer.Sample
{
    public class A53Sample
    {
        #region Common

        /// <summary>
        /// 發行人
        /// </summary>
        public string ISSUER_EQUITY_TICKER { get; set; }

        /// <summary>
        /// 債券評等日期
        /// </summary>
        public string ISSUE_DT { get; set; }

        /// <summary>
        /// 債券名稱
        /// </summary>
        public string ISSUER { get; set; }

        /// <summary>
        /// 擔保人
        /// </summary>
        public string GUARANTOR_EQY_TICKER { get; set; }

        /// <summary>
        /// 擔保人名稱
        /// </summary>
        public string GUARANTOR_NAME { get; set; }

        #endregion Common

        #region 標普(S&P) 原A53

        /// <summary>
        /// SP國外評等 (國外)
        /// </summary>
        public string RTG_SP { get; set; }

        /// <summary>
        /// SP國外評等日期 (國外)
        /// </summary>
        public string SP_EFF_DT { get; set; }

        #endregion 標普(S&P) 原A53

        #region 穆迪(Moody's) 原A54

        /// <summary>
        /// Moody's國外評等 (國外)
        /// </summary>
        public string RTG_MOODY { get; set; }

        /// <summary>
        /// Moody's國外評等日期 (國外)
        /// </summary>
        public string MOODY_EFF_DT { get; set; }

        #endregion 穆迪(Moody's) 原A54

        #region 惠譽() 原A55

        /// <summary>
        /// 惠譽國內評等 (國內)
        /// </summary>
        public string RTG_FITCH_NATIONAL { get; set; }

        /// <summary>
        /// 惠譽國內評等日期 (國內)
        /// </summary>
        public string RTG_FITCH_NATIONAL_DT { get; set; }

        /// <summary>
        /// 惠譽評等 (國外)
        /// </summary>
        public string RTG_FITCH { get; set; }

        /// <summary>
        /// 惠譽評等日期 (國外)
        /// </summary>
        public string FITCH_EFF_DT { get; set; }

        #endregion 惠譽() 原A55

        #region TRC 原A56

        /// <summary>
        /// TRC 評等 (國內)
        /// </summary>
        public string RTG_TRC { get; set; }

        /// <summary>
        /// TRC 評等日期 (國內)
        /// </summary>
        public string TRC_EFF_DT { get; set; }

        #endregion TRC 原A56

        #region A95 Security_Des

        /// <summary>
        /// A95 Security_Des
        /// </summary>
        public string Security_Des { get; set; }

        #endregion A95 Security_Des

        /// <summary>
        /// PARENT_TICKER_EXCHANGE add in 2017/11/08 SMF 條件符合取代使用
        /// </summary>
        public string PARENT_TICKER_EXCHANGE { get; set; }

        /// <summary>
        /// COLLAT_TYP 新增抓Bloomberg欄位 COLLAT_TYP add in 2017/12/25 for D67 Use
        /// </summary>
        public string COLLAT_TYP { get; set; }

        #region D63 欄位相關 add in 2018/01/11

        /// <summary>
        /// D63.Net_Debt
        /// </summary>
        public string NET_DEBT { get; set; }

        /// <summary>
        /// D63.Total_Asset & D63.BS_TOT_ASSET
        /// </summary>
        //public string BS_TOT_ASSET { get; set; } //移到D63OtherSample

        /// <summary>
        /// D63.CFO
        /// </summary>
        public string TRAIL_12M_CASH_FROM_OPER { get; set; }

        /// <summary>
        /// D63.Total_Equity
        /// </summary>
        //public string TOTAL_EQUITY { get; set; } //移到D63OtherSample

        /// <summary>
        /// D63.SHORT_AND_LONG_TERM_DEBT
        /// </summary>
        public string SHORT_AND_LONG_TERM_DEBT { get; set; }

        /// <summary>
        /// D63.Int_Expense (1)
        /// </summary>
        public string TRAIL_12M_INT_EXP { get; set; }

        /// <summary>
        /// D63.Int_Expense (2)
        /// </summary>
        public string TRAIL_12M_ACT_CASH_PAID_FOR_INT { get; set; }

        #endregion
    }
}