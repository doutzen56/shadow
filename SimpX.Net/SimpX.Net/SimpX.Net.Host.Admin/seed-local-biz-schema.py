# -*- coding: utf-8 -*-
"""把测试用业务 DDL 灌进 Host.Admin 本机 Sqlite（不写 BetWin30）。"""
from __future__ import annotations

import re
import sqlite3
from pathlib import Path

SCHEMA_DIR = Path(r"F:\SimpX.Net\SimpX.Net\SimpX.Net.Test\Fixtures\biz-schema")
DB_PATHS = [
    Path(r"F:\SimpX.Net\SimpX.Net\SimpX.Net.Host.Admin\SimpX.Net.db"),
    Path(r"F:\SimpX.Net\SimpX.Net\SimpX.Net.Host.SuperAdmin\SimpX.Net.db"),
]
# 实体表名大小写与导出文件不一致时，创建时改名
RENAME = {
    "site_Currency": "Site_Currency",
    "site_ExchangeRate": "Site_ExchangeRate",
}
# 已有业务数据，不重建
PRESERVE = {"Site", "site_Host"}


def table_exists(conn: sqlite3.Connection, name: str) -> bool:
    row = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    ).fetchone()
    return row is not None


def apply_sql_file(conn: sqlite3.Connection, path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    # 统一改名
    for old, new in RENAME.items():
        text = text.replace(f"[{old}]", f"[{new}]")
        text = text.replace(f"DROP TABLE IF EXISTS [{old}]", f"DROP TABLE IF EXISTS [{new}]")
        text = text.replace(f"CREATE TABLE [{old}]", f"CREATE TABLE [{new}]")

    m = re.search(r"CREATE TABLE \[([^\]]+)\]", text)
    if not m:
        return f"skip {path.name}: no CREATE"
    table = m.group(1)
    if table in PRESERVE and table_exists(conn, table):
        return f"preserve {table}"
    conn.executescript(text)
    return f"applied {table}"


EXTRA_DDL = [
    """
CREATE TABLE IF NOT EXISTS [site_Config] (
  [SiteID] INTEGER NOT NULL PRIMARY KEY,
  [Bank] TEXT, [Register] TEXT, [Mail] TEXT, [BlackRegion] TEXT, [AgentJoin] TEXT,
  [SMS] TEXT, [MoneyType] TEXT, [GameSetting] TEXT, [AdminIP] TEXT, [PCStyle] TEXT,
  [MobileStyle] TEXT, [WarningSoundString] TEXT, [VIPString] TEXT, [VirtualCoin] TEXT,
  [Login] TEXT, [BackgroundType] TEXT, [Festival] TEXT, [FestivalStatus] TEXT,
  [AreaCode] TEXT, [Telegram] TEXT, [RegisterTitle] TEXT, [RegisterContent] TEXT,
  [PhoneCodeMessage] TEXT, [Risk] TEXT, [Jackpot] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_BlackMobile] (
  [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [Mobile] TEXT NOT NULL,
  [Demo] TEXT,
  [IsLock] TEXT,
  [CreateAt] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [risk_UserIP] (
  [RiskID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [IP] TEXT NOT NULL,
  [Date] TEXT NOT NULL
);
""",
    """
CREATE TABLE IF NOT EXISTS [risk_UserDevice] (
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [DeviceID] TEXT NOT NULL,
  [Date] TEXT NOT NULL
);
""",
    """
CREATE TABLE IF NOT EXISTS [risk_UserDanger] (
  [RiskID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [Date] TEXT NOT NULL,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Context] TEXT,
  [CreateAt] TEXT,
  [Type] TEXT NOT NULL,
  [Detail] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [sys_MoneyTypeIsProfit] (
  [Type] INTEGER NOT NULL PRIMARY KEY,
  [IsProfit] INTEGER NOT NULL
);
""",
    """
CREATE TABLE IF NOT EXISTS [rpt_UserDateGame] (
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Date] TEXT NOT NULL,
  [Type] INTEGER NOT NULL,
  [Category] INTEGER NOT NULL,
  [BetMoney] REAL NOT NULL,
  [Money] REAL NOT NULL
);
""",
    """
CREATE TABLE IF NOT EXISTS [site_MoneyLog] (
  [LogID] INTEGER NOT NULL PRIMARY KEY,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Money] REAL NOT NULL,
  [LockMoney] REAL NOT NULL,
  [Balance] REAL NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [Type] INTEGER NOT NULL,
  [SourceID] TEXT,
  [IP] TEXT,
  [LogDesc] TEXT,
  [TableID] INTEGER,
  [SubType] INTEGER,
  [Currency] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [sys_GameSetting] (
  [Type] INTEGER NOT NULL PRIMARY KEY,
  [GameName] TEXT,
  [Setting] TEXT,
  [Status] INTEGER NOT NULL DEFAULT 1,
  [DefaultRate] REAL NOT NULL DEFAULT 0,
  [Currencys] TEXT,
  [Languages] TEXT,
  [GameCategorys] TEXT,
  [Maintenance] TEXT,
  [Embedded] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [MoneyLog] (
  [LogID] INTEGER NOT NULL PRIMARY KEY,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Money] REAL NOT NULL,
  [LockMoney] REAL NOT NULL DEFAULT 0,
  [Balance] REAL NOT NULL DEFAULT 0,
  [CreateAt] TEXT,
  [Type] INTEGER NOT NULL,
  [SourceID] TEXT,
  [IP] TEXT,
  [LogDesc] TEXT,
  [TableID] INTEGER NOT NULL DEFAULT 0,
  [SubType] INTEGER NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_MoneyReview] (
  [ReviewID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Money] REAL NOT NULL,
  [Type] INTEGER NOT NULL,
  [Flow] REAL NOT NULL DEFAULT 0,
  [MinFlow] REAL NOT NULL DEFAULT 0,
  [CreateAt] TEXT NOT NULL,
  [CreatorID] INTEGER NOT NULL DEFAULT 0,
  [SourceID] TEXT,
  [Action] INTEGER NOT NULL,
  [ReviewerID] INTEGER NOT NULL DEFAULT 0,
  [ReviewAt] TEXT,
  [Status] INTEGER NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [LogDesc] TEXT,
  [EventID] INTEGER NOT NULL DEFAULT 0,
  [EventIDNoTrans] INTEGER NOT NULL DEFAULT 0,
  [PayID] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_RechargeOrder] (
  [OrderID] TEXT NOT NULL PRIMARY KEY,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [Money] REAL NOT NULL,
  [Fee] REAL NOT NULL DEFAULT 0,
  [Cost] REAL NOT NULL DEFAULT 0,
  [CreateAt] TEXT NOT NULL,
  [PayID] INTEGER NOT NULL DEFAULT 0,
  [IsPay] INTEGER NOT NULL DEFAULT 0,
  [PayAt] TEXT,
  [Paid] REAL NOT NULL DEFAULT 0,
  [SystemID] TEXT,
  [AdminID] INTEGER NOT NULL DEFAULT 0,
  [Count] INTEGER NOT NULL DEFAULT 0,
  [Remark] TEXT,
  [IP] TEXT,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [FromCurrency] INTEGER NOT NULL DEFAULT 0,
  [IsCheckFail] INTEGER NOT NULL DEFAULT 0,
  [ActiveID] TEXT,
  [IsDeleted] INTEGER NOT NULL DEFAULT 0,
  [IsProxyRecharge] INTEGER NOT NULL DEFAULT 0,
  [PayAccount] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [sys_Payment] (
  [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [GroupID] INTEGER NOT NULL DEFAULT 0,
  [Name] TEXT,
  [IsOpen] INTEGER NOT NULL DEFAULT 1,
  [Fields] TEXT,
  [Bank] TEXT,
  [Sign] TEXT,
  [Submit] TEXT,
  [Callback] TEXT,
  [SpecialType] INTEGER NOT NULL DEFAULT 0
);
""",

    """
CREATE TABLE IF NOT EXISTS [site_Admin] (
  [AdminID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserName] TEXT NOT NULL,
  [Name] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_WithdrawOrder] (
  [OrderID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [AccountBank] INTEGER NOT NULL DEFAULT 0,
  [AccountName] TEXT,
  [AccountNumber] TEXT,
  [Money] REAL NOT NULL DEFAULT 0,
  [Status] INTEGER NOT NULL DEFAULT 0,
  [Fee] REAL NOT NULL DEFAULT 0,
  [Paid] REAL NOT NULL DEFAULT 0,
  [PayAt] TEXT,
  [ChannelID] INTEGER NOT NULL DEFAULT 0,
  [Description] TEXT,
  [Mark] TEXT,
  [IP] TEXT,
  [Cost] REAL NOT NULL DEFAULT 0,
  [SystemID] TEXT,
  [AdminID] INTEGER NOT NULL DEFAULT 0,
  [CashierID] INTEGER NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [FromCurrency] INTEGER NOT NULL DEFAULT 0,
  [AuditAt] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_BankRechargeOrder] (
  [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [PayID] INTEGER NOT NULL DEFAULT 0,
  [SiteID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [CreateAt] TEXT NOT NULL,
  [CheckAt] TEXT,
  [Status] INTEGER NOT NULL DEFAULT 0,
  [Name] TEXT,
  [Account] TEXT,
  [Money] REAL NOT NULL DEFAULT 0,
  [Paid] REAL NOT NULL DEFAULT 0,
  [OrderID] TEXT,
  [Description] TEXT,
  [AdminID] INTEGER NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [PayOrderID] TEXT,
  [IsDeleted] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_RebatePlan] (
  [PlanID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [PlanName] TEXT,
  [CreateAt] TEXT NOT NULL,
  [AdminID] INTEGER NOT NULL DEFAULT 0,
  [Money] REAL NOT NULL DEFAULT 0,
  [Paid] REAL NOT NULL DEFAULT 0,
  [Success] INTEGER NOT NULL DEFAULT 0,
  [Faild] INTEGER NOT NULL DEFAULT 0,
  [Total] INTEGER NOT NULL DEFAULT 0,
  [StartAt] TEXT,
  [EndAt] TEXT,
  [FlowRate] REAL NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [FromCurrency] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [usr_RebateLog] (
  [LogID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [SiteID] INTEGER NOT NULL,
  [PlanID] INTEGER NOT NULL,
  [UserID] INTEGER NOT NULL,
  [StartAt] TEXT,
  [EndAt] TEXT,
  [Money] REAL NOT NULL DEFAULT 0,
  [CreateAt] TEXT NOT NULL,
  [CheckAt] TEXT,
  [Detail] TEXT,
  [Type] TEXT,
  [Status] INTEGER NOT NULL DEFAULT 0,
  [FaildMsg] TEXT,
  [RebateID] INTEGER NOT NULL DEFAULT 0,
  [Currency] INTEGER NOT NULL DEFAULT 0,
  [SortID] INTEGER NOT NULL DEFAULT 0
);
""",
    """
CREATE TABLE IF NOT EXISTS [sys_Withdraw] (
  [SettingID] INTEGER PRIMARY KEY AUTOINCREMENT,
  [Name] TEXT,
  [IsOpen] INTEGER NOT NULL DEFAULT 1,
  [Fields] TEXT,
  [Bank] TEXT,
  [Sign] TEXT,
  [Submit] TEXT,
  [Callback] TEXT
);
""",
    """
CREATE TABLE IF NOT EXISTS [sys_BankAccountBlackList] (
  [BlackIP] INTEGER PRIMARY KEY AUTOINCREMENT,
  [AccountName] TEXT,
  [AccountNumber] TEXT,
  [Remark] TEXT,
  [CreateAt] TEXT
);
""",
]


def seed_min(conn: sqlite3.Connection) -> None:
    if not conn.execute("SELECT 1 FROM Site WHERE SiteID=1001").fetchone():
        cols = {r[1] for r in conn.execute("PRAGMA table_info(Site)").fetchall()}
        now = "2026-10-31 00:00:00"
        row = {
            "SiteID": 1001,
            "SiteName": "local-dev",
            "CreateAt": now,
            "Status": 0,
            "Currency": 0,
            "APIKey": "00000000-0000-0000-0000-000000000000",
            "Setting": "{}",
            "APPSetting": "{}",
            "PCSetting": "{}",
            "GameCredit": 0,
            "NotifyCredit": 0,
            "AdminDomain": "",
            "NotifyAt": now,
            "Wallet": 0,
            "IsCurrencySelect": 0,
            "HomeTemplateName": "",
        }
        use = [c for c in row if c in cols]
        conn.execute(
            f"INSERT INTO Site({','.join(use)}) VALUES ({','.join('?' for _ in use)})",
            [row[c] for c in use],
        )
    else:
        conn.execute("UPDATE Site SET Status=0 WHERE SiteID=1001")
    # 定站
    for host in ("127.0.0.1", "localhost", "www.test.com"):
        exists = conn.execute(
            "SELECT 1 FROM site_Host WHERE Host=? AND SiteID=1001 LIMIT 1", (host,)
        ).fetchone()
        if not exists:
            conn.execute(
                "INSERT INTO site_Host(Host, SiteID, IsEnable) VALUES (?, 1001, 1)",
                (host,),
            )
        else:
            conn.execute(
                "UPDATE site_Host SET IsEnable=1 WHERE Host=? AND SiteID=1001",
                (host,),
            )

    if not conn.execute("SELECT 1 FROM site_Config WHERE SiteID=1001").fetchone():
        conn.execute(
            "INSERT INTO site_Config(SiteID, Bank, Register, Mail, BlackRegion, AgentJoin, SMS, MoneyType, GameSetting, AdminIP, PCStyle, MobileStyle, WarningSoundString, VIPString, VirtualCoin, Login, BackgroundType, Festival, FestivalStatus, AreaCode, Telegram, RegisterTitle, RegisterContent, PhoneCodeMessage, Risk, Jackpot) VALUES (1001,'','','','','','','','','','','','','','','','','','','','','','','','','')"
        )

    # 基准币 / 一个币对，方便汇率列表
    if not conn.execute("SELECT 1 FROM Site_Currency WHERE SiteID=1001 LIMIT 1").fetchone():
        conn.execute(
            "INSERT INTO Site_Currency(SiteID, Currency, IsOpen, Sort, Accuracy, SysOpen, IsRegister) VALUES (1001, 1, 1, 0, 2, 1, 1)"
        )
        conn.execute(
            "INSERT INTO Site_Currency(SiteID, Currency, IsOpen, Sort, Accuracy, SysOpen, IsRegister) VALUES (1001, 2, 1, 1, 2, 1, 1)"
        )
    if not conn.execute("SELECT 1 FROM Site_ExchangeRate WHERE SiteID=1001 LIMIT 1").fetchone():
        conn.execute(
            """INSERT INTO Site_ExchangeRate(
              SiteID, BaseCurrency, ExchangeCurrency, MarketPrice, Point, ExchangeMode, Parameter,
              IsBase, ExchangeFee, ExchangeCharge, ExchangeLimit, IsCheck, Status
            ) VALUES (1001, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1)"""
        )
        conn.execute(
            """INSERT INTO Site_ExchangeRate(
              SiteID, BaseCurrency, ExchangeCurrency, MarketPrice, Point, ExchangeMode, Parameter,
              IsBase, ExchangeFee, ExchangeCharge, ExchangeLimit, IsCheck, Status
            ) VALUES (1001, 1, 2, 7.1, 7.1, 0, 0, 0, 0, 0, 0, 0, 1)"""
        )
    if not conn.execute("SELECT 1 FROM sys_GameSetting LIMIT 1").fetchone():
        conn.execute(
            """INSERT INTO sys_GameSetting(
              Type, GameName, Setting, Status, DefaultRate, Currencys, Languages, GameCategorys, Maintenance, Embedded
            ) VALUES (1, 'DemoGame', '{}', 1, 1, '1,2', 'zh-CN', '1', '', 0)"""
        )
    if not conn.execute("SELECT 1 FROM sys_Withdraw LIMIT 1").fetchone():
        conn.execute(
            "INSERT INTO sys_Withdraw(Name, IsOpen, Fields, Bank, Sign, Submit, Callback) VALUES ('DemoWithdraw', 1, '', '', '', '', '')"
        )
    if not conn.execute("SELECT 1 FROM sys_BankAccountBlackList LIMIT 1").fetchone():
        conn.execute(
            "INSERT INTO sys_BankAccountBlackList(AccountName, AccountNumber, Remark, CreateAt) VALUES ('黑户样例', '6222029988776655', 'seed', datetime('now'))"
        )
    if not conn.execute("SELECT 1 FROM sys_Payment WHERE Name='DemoPay' LIMIT 1").fetchone():
        exists_any = conn.execute("SELECT 1 FROM sys_Payment LIMIT 1").fetchone()
        if not exists_any:
            conn.execute(
                "INSERT INTO sys_Payment(GroupID, Name, IsOpen, Fields, Bank, Sign, Submit, Callback, SpecialType) VALUES (1, 'DemoPay', 1, '', '', '', '', '', 0)"
            )
    conn.commit()


def seed_db(db: Path) -> None:
    print("===", db)
    conn = sqlite3.connect(str(db))
    try:
        files = sorted(
            p for p in SCHEMA_DIR.glob("*.sql") if p.name != "_all.sql"
        )
        for p in files:
            msg = apply_sql_file(conn, p)
            print(" ", msg)
        for ddl in EXTRA_DDL:
            conn.executescript(ddl)
        print("  extras ok")
        seed_min(conn)
        print("  seed Site1001 Status=0 + Host + Currency ok")
        # 核对关键表
        need = [
            "Site_Currency",
            "Site_ExchangeRate",
            "Users",
            "usr_Group",
            "site_Domain",
            "site_Config",
            "usr_CurrencyExchange",
        ]
        tabs = {
            r[0]
            for r in conn.execute(
                "SELECT name FROM sqlite_master WHERE type='table'"
            ).fetchall()
        }
        for n in need:
            print(" ", n, "YES" if n in tabs else "NO")
        print(
            "  Site",
            conn.execute("SELECT SiteID, Status FROM Site WHERE SiteID=1001").fetchall(),
        )
    finally:
        conn.close()


def main() -> None:
    for db in DB_PATHS:
        if not db.exists():
            print("missing", db)
            continue
        seed_db(db)


if __name__ == "__main__":
    main()
