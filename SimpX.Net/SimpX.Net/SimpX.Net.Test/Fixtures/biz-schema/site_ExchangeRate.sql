DROP TABLE IF EXISTS [site_ExchangeRate];
CREATE TABLE [site_ExchangeRate] (
  [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [BaseCurrency] INTEGER NOT NULL,
  [ExchangeCurrency] INTEGER NOT NULL,
  [MarketPrice] REAL NOT NULL,
  [Point] REAL NOT NULL,
  [ExchangeMode] INTEGER NOT NULL,
  [Parameter] REAL NOT NULL,
  [IsBase] INTEGER NOT NULL,
  [ExchangeFee] INTEGER NOT NULL,
  [ExchangeCharge] REAL NOT NULL,
  [ExchangeLimit] INTEGER NOT NULL,
  [IsCheck] INTEGER NOT NULL,
  [Status] INTEGER NOT NULL
);
