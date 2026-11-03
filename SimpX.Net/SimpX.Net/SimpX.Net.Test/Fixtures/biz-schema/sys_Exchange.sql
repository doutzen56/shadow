DROP TABLE IF EXISTS [sys_Exchange];
CREATE TABLE [sys_Exchange] (
  [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [BaseCurrency] INTEGER NOT NULL,
  [ExchangeCurrency] INTEGER NOT NULL,
  [MarketPrice] REAL NOT NULL,
  [Status] INTEGER NOT NULL,
  [UpdateAt] TEXT NOT NULL
);
