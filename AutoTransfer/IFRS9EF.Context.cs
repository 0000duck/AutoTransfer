﻿//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoTransfer
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class IFRS9Entities : DbContext
    {
        public IFRS9Entities()
            : base("name=IFRS9Entities")
        {
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 300;
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Grade_Mapping_Info> Grade_Mapping_Info { get; set; }
        public virtual DbSet<Bond_Rating_Parm> Bond_Rating_Parm { get; set; }
        public virtual DbSet<Rating_Info> Rating_Info { get; set; }
        public virtual DbSet<Transfer_CheckTable> Transfer_CheckTable { get; set; }
        public virtual DbSet<Rating_Info_SampleInfo> Rating_Info_SampleInfo { get; set; }
        public virtual DbSet<Bond_Account_Info> Bond_Account_Info { get; set; }
        public virtual DbSet<Econ_Domestic> Econ_Domestic { get; set; }
        public virtual DbSet<Econ_D_YYYYMMDD> Econ_D_YYYYMMDD { get; set; }
        public virtual DbSet<Econ_Foreign> Econ_Foreign { get; set; }
        public virtual DbSet<Moody_Quartly_PD_Info> Moody_Quartly_PD_Info { get; set; }
        public virtual DbSet<Loan_default_Info> Loan_default_Info { get; set; }
        public virtual DbSet<Econ_F_YYYYMMDD> Econ_F_YYYYMMDD { get; set; }
        public virtual DbSet<Bond_Ticker_Info> Bond_Ticker_Info { get; set; }
        public virtual DbSet<Bond_Category_Info> Bond_Category_Info { get; set; }
        public virtual DbSet<Group_Product_Code_Mapping> Group_Product_Code_Mapping { get; set; }
    }
}
