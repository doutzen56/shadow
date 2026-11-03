DROP TABLE IF EXISTS [rpt_UserDateGameView];
CREATE TABLE [rpt_UserDateGameView] (
  [Type] INTEGER NOT NULL,
  [Date] TEXT NOT NULL,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Category] INTEGER NOT NULL,
  [BetMoney] REAL NOT NULL,
  [BetAmount] REAL NOT NULL,
  [Money] REAL NOT NULL,
  [OrderCount] INTEGER NOT NULL,
  [Currency] INTEGER NOT NULL,
  [JackPot] REAL NOT NULL,
  [TimeType] INTEGER,
  [JackPotCount] REAL NOT NULL
);
