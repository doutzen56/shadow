DROP TABLE IF EXISTS [usr_IdCard];
CREATE TABLE [usr_IdCard] (
  [CardID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [UserName] TEXT NOT NULL,
  [CardNumber] TEXT NOT NULL,
  [CardName] TEXT NOT NULL,
  [CardFront] TEXT NOT NULL,
  [CardBack] TEXT NOT NULL,
  [Status] INTEGER NOT NULL,
  [Remark] TEXT NOT NULL,
  [CheckDateTime] TEXT,
  [Checker] INTEGER,
  [CreateAt] TEXT NOT NULL,
  [CardCity] TEXT NOT NULL,
  [CardCreateAt] TEXT NOT NULL,
  [CardType] INTEGER NOT NULL,
  [CardBirthday] TEXT NOT NULL,
  [Country] TEXT,
  [Gender] INTEGER
);
