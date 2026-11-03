DROP TABLE IF EXISTS [usr_CurrencyExchange];
CREATE TABLE [usr_CurrencyExchange] (
  [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [OrderID] TEXT NOT NULL,
  [UserID] INTEGER NOT NULL,
  [UserName] TEXT NOT NULL,
  [BeforeCurrency] INTEGER NOT NULL,
  [BeforeMoney] REAL NOT NULL,
  [AfterCurrency] INTEGER NOT NULL,
  [AfterMoney] REAL NOT NULL,
  [Money] REAL NOT NULL,
  [Point] REAL NOT NULL,
  [ExchangeMoney] REAL NOT NULL,
  [CheckTime] TEXT NULL,
  [Checker] INTEGER NULL,
  [Status] INTEGER NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [Remark] TEXT NULL
);
