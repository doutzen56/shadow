DROP TABLE IF EXISTS [Site];
CREATE TABLE [Site] (
  [SiteID] INTEGER NOT NULL,
  [SiteName] TEXT NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [Status] INTEGER NOT NULL,
  [Currency] INTEGER NOT NULL,
  [APIKey] TEXT NOT NULL,
  [Setting] TEXT NOT NULL,
  [APPSetting] TEXT NOT NULL,
  [PCSetting] TEXT NOT NULL,
  [GameCredit] REAL NOT NULL,
  [NotifyCredit] INTEGER NOT NULL,
  [AdminDomain] TEXT NOT NULL,
  [NotifyAt] TEXT NOT NULL,
  [Wallet] INTEGER NOT NULL,
  [IsCurrencySelect] INTEGER NOT NULL,
  [HomeTemplateName] TEXT NOT NULL
);
