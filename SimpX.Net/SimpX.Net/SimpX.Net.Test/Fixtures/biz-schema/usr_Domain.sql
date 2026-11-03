DROP TABLE IF EXISTS [usr_Domain];
CREATE TABLE [usr_Domain] (
  [DomainID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [DomainName] TEXT NOT NULL,
  [InviteID] TEXT NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [Logo] TEXT NOT NULL,
  [SiteName] TEXT NOT NULL,
  [DefaultBackground] TEXT NOT NULL,
  [DefaultTemplate] TEXT NOT NULL
);
