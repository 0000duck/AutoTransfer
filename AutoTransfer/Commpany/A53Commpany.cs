namespace AutoTransfer.Commpany
{
    public class A53Commpany
    {
        #region Common

        /// <summary>
        /// 公司ID
        /// </summary>
        public string ID_BB_COMPANY { get; set; }

        /// <summary>
        /// 公司名稱
        /// </summary>
        public string LONG_COMP_NAME { get; set; }

        /// <summary>
        /// 城市(國家)
        /// </summary>
        public string COUNTRY_ISO { get; set; }

        /// <summary>
        /// INDUSTRY_GROUP
        /// </summary>
        public string INDUSTRY_GROUP { get; set; }

        /// <summary>
        /// INDUSTRY_SECTOR
        /// </summary>
        public string INDUSTRY_SECTOR { get; set; }

        #endregion Common

        #region 標普(S&P) 原A53

        /// <summary>
        /// 標普本國貨幣長期發行人信用評等 (國外)
        /// </summary>
        public string RTG_SP_LT_LC_ISSUER_CREDIT { get; set; }

        /// <summary>
        /// 標普本國貨幣長期發行人信用評等日期 (國外)
        /// </summary>
        public string RTG_SP_LT_LC_ISS_CRED_RTG_DT { get; set; }

        /// <summary>
        /// 標普長期外幣發行人信用評等日期 (國外)
        /// </summary>
        public string RTG_SP_LT_FC_ISS_CRED_RTG_DT { get; set; }

        /// <summary>
        /// 標普長期外幣發行人信用評等 (國外)
        /// </summary>
        public string RTG_SP_LT_FC_ISSUER_CREDIT { get; set; }

        #endregion 標普(S&P) 原A53

        #region 穆迪(Moody's) 原A54

        /// <summary>
        /// 穆迪長期本國銀行存款評等 (國內)
        /// </summary>
        public string RTG_MDY_LOCAL_LT_BANK_DEPOSITS { get; set; }

        /// <summary>
        /// 穆迪長期本國銀行存款評等日期 (國內)
        /// </summary>
        public string RTG_MDY_LT_LC_BANK_DEP_RTG_DT { get; set; }

        /// <summary>
        /// 穆迪外幣發行人評等 (國外)
        /// </summary>
        public string RTG_MDY_FC_CURR_ISSUER_RATING { get; set; }

        /// <summary>
        /// 穆迪外幣發行人評等日期 (國外)
        /// </summary>
        public string RTG_MDY_FC_CURR_ISSUER_RTG_DT { get; set; }

        /// <summary>
        /// 穆迪發行人評等 (國外)
        /// </summary>
        public string RTG_MDY_ISSUER { get; set; }

        /// <summary>
        /// 穆迪發行人評等日期 (國外)
        /// </summary>
        public string RTG_MDY_ISSUER_RTG_DT { get; set; }

        /// <summary>
        /// 穆迪長期評等 (國外)
        /// </summary>
        public string RTG_MOODY_LONG_TERM { get; set; }

        /// <summary>
        /// 穆迪長期評等日期 (國外)
        /// </summary>
        public string RTG_MOODY_LONG_TERM_DATE { get; set; }

        /// <summary>
        /// 穆迪優先無擔保債務評等 (國外)
        /// </summary>
        public string RTG_MDY_SEN_UNSECURED_DEBT { get; set; }

        /// <summary>
        /// 穆迪優先無擔保債務評等日期 (國外)
        /// </summary>
        public string RTG_MDY_SEN_UNSEC_RTG_DT { get; set; }

        #endregion 穆迪(Moody's) 原A54

        #region 惠譽() 原A55

        /// <summary>
        /// 惠譽長期發行人違約評等 (國外)
        /// </summary>
        public string RTG_FITCH_LT_ISSUER_DEFAULT { get; set; }

        /// <summary>
        /// 惠譽長期發行人違約評等日期 (國外)
        /// </summary>
        public string RTG_FITCH_LT_ISSUER_DFLT_RTG_DT { get; set; }

        /// <summary>
        /// 惠譽長期外幣發行人違約評等 (國外)
        /// </summary>
        public string RTG_FITCH_LT_FC_ISSUER_DEFAULT { get; set; }

        /// <summary>
        /// 惠譽長期外幣發行人違約評等日期 (國外)
        /// </summary>
        public string RTG_FITCH_LT_FC_ISS_DFLT_RTG_DT { get; set; }

        /// <summary>
        /// 惠譽長期本國貨幣發行人違約評等 (國外)
        /// </summary>
        public string RTG_FITCH_LT_LC_ISSUER_DEFAULT { get; set; }

        /// <summary>
        /// 惠譽長期本國貨幣發行人違約評等日期 (國外)
        /// </summary>
        public string RTG_FITCH_LT_LC_ISS_DFLT_RTG_DT { get; set; }

        /// <summary>
        /// 惠譽優先無擔保債務評等 (國外)
        /// </summary>
        public string RTG_FITCH_SEN_UNSECURED { get; set; }

        /// <summary>
        /// 惠譽優先無擔保債務評等日期 (國外)
        /// </summary>
        public string RTG_FITCH_SEN_UNSEC_RTG_DT { get; set; }

        /// <summary>
        /// 惠譽國內長期評等 (國內)
        /// </summary>
        public string RTG_FITCH_NATIONAL_LT { get; set; }

        /// <summary>
        /// 惠譽國內長期評等日期 (國內)
        /// </summary>
        public string RTG_FITCH_NATIONAL_LT_DT { get; set; }

        #endregion 惠譽() 原A55

        #region TRC 原A56

        /// <summary>
        /// TRC 長期評等 (國內)
        /// </summary>
        public string RTG_TRC_LONG_TERM { get; set; }

        /// <summary>
        /// TRC 長期評等日期 (國內)
        /// </summary>
        public string RTG_TRC_LONG_TERM_RTG_DT { get; set; }

        #endregion TRC 原A56
    }
}