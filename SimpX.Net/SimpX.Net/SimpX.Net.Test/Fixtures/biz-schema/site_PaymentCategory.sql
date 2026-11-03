DROP TABLE IF EXISTS [site_PaymentCategory];
CREATE TABLE [site_PaymentCategory] (
  [CateID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [CateName] TEXT NOT NULL,
  [Sort] INTEGER NOT NULL,
  [Cover] TEXT NOT NULL,
  [IsRecommend] INTEGER NOT NULL,
  [Count] INTEGER NOT NULL,
  [Description] TEXT NOT NULL,
  [CateType] INTEGER NOT NULL
);
